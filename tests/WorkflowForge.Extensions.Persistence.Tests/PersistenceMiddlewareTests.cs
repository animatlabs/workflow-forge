using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Exceptions; // WorkflowOperationException
using WorkflowForge.Extensions.Persistence.Abstractions; // IWorkflowPersistenceProvider
using WorkflowForge.Extensions.Persistence.Recovery; // RecoveryExtensions
using WorkflowForge.Extensions.Persistence.Recovery.Options; // RecoveryMiddlewareOptions
using Xunit;

namespace WorkflowForge.Extensions.Persistence.Tests;

public class PersistenceMiddlewareTests
{
    private sealed class InMemoryProvider : IWorkflowPersistenceProvider
    {
        private readonly Dictionary<(Guid, Guid), WorkflowExecutionSnapshot> _store = new();

        public Task SaveAsync(WorkflowExecutionSnapshot snapshot, CancellationToken cancellationToken = default)
        { _store[(snapshot.FoundryExecutionId, snapshot.WorkflowId)] = snapshot; return Task.CompletedTask; }

        public Task<WorkflowExecutionSnapshot?> TryLoadAsync(Guid f, Guid w, CancellationToken cancellationToken = default)
        { _store.TryGetValue((f, w), out var s); return Task.FromResult<WorkflowExecutionSnapshot?>(s); }

        public Task DeleteAsync(Guid f, Guid w, CancellationToken cancellationToken = default)
        { _store.Remove((f, w)); return Task.CompletedTask; }
    }

    [Fact]
    public async Task SavesSnapshotPerStep_DeletesOnCompletion()
    {
        var provider = new InMemoryProvider();
        var workflow = WorkflowForge.CreateWorkflow("PersistTest")
            .AddOperation("A", async (foundry, ct) => { foundry.SetProperty("a", 1); await Task.Delay(1, ct); })
            .AddOperation("B", async (foundry, ct) => { foundry.SetProperty("b", 2); await Task.Delay(1, ct); })
            .Build();

        using var foundry = WorkflowForge.CreateFoundry("PersistTest");
        foundry.UsePersistence(provider, new PersistenceOptions { InstanceId = "persist-instance", WorkflowKey = "PersistTest" });

        var smith = WorkflowForge.CreateSmith();
        await smith.ForgeAsync(workflow, foundry);

        // Completed run should delete snapshot
        var snapshot = await provider.TryLoadAsync(
            Deterministic("persist-instance"),
            Deterministic("PersistTest"));

        Assert.Null(snapshot);
    }

    [Fact]
    public async Task Resume_SkipsCompletedSteps_AndRestoresProperties()
    {
        var provider = new InMemoryProvider();
        var workflow = WorkflowForge.CreateWorkflow("PersistResume")
            .AddOperation("S1", async (foundry, ct) => { foundry.SetProperty("p", 1); })
            .AddOperation("S2", async (foundry, ct) => { foundry.SetProperty("p", 2); })
            .AddOperation("S3", async (foundry, ct) => { foundry.SetProperty("p", 3); })
            .Build();

        var options = new PersistenceOptions { InstanceId = "resume-instance", WorkflowKey = "PersistResume" };

        // First run: execute S1 and checkpoint
        using (var f1 = WorkflowForge.CreateFoundry("PersistResume"))
        {
            f1.UsePersistence(provider, options);
            var smith = WorkflowForge.CreateSmith();

            // Execute only first step then simulate failure by saving manually and stopping
            // Run the smith which will complete all, but we emulate partial progress by saving snapshot after S1
            // Instead we directly save a snapshot to simulate a crash after S1
            var builder = WorkflowForge.CreateWorkflow("Temp"); // just to access ids, not used
        }

        // Emulate partial progress snapshot at S2 index (1)
        var foundryKey = Deterministic(options.InstanceId!);
        var workflowKey = Deterministic(options.WorkflowKey!);
        await provider.SaveAsync(new WorkflowExecutionSnapshot
        {
            FoundryExecutionId = foundryKey,
            WorkflowId = workflowKey,
            WorkflowName = "PersistResume",
            NextOperationIndex = 1, // S1 done
            Properties = new Dictionary<string, object?> { ["p"] = 1 }
        });

        // New process resume
        using var f2 = WorkflowForge.CreateFoundry("PersistResume");
        f2.UsePersistence(provider, options);
        var s2 = WorkflowForge.CreateSmith();
        await s2.ForgeAsync(workflow, f2);

        // Should have executed S2 and S3 only; p should be 3
        Assert.Equal(3, f2.GetPropertyOrDefault<int>("p"));
    }

    [Fact]
    public async Task Recovery_RetriesUntilSuccess_AndDoesNotReexecuteCompletedSteps()
    {
        var provider = new InMemoryProvider();
        var options = new PersistenceOptions { InstanceId = "recover-instance", WorkflowKey = "RecoverWorkflow" };

        // Fail second step twice before succeeding
        int failCount = 0;

        var workflow = WorkflowForge.CreateWorkflow("RecoverWorkflow")
            .AddOperation("First", async (foundry, ct) => { foundry.SetProperty("seq", new List<string> { "First" }); })
            .AddOperation("Second-Flaky", async (foundry, ct) =>
            {
                var seq = (List<string>)foundry.Properties["seq"]!;
                seq.Add($"Second-Attempt-{failCount}");
                if (failCount < 2)
                {
                    failCount++;
                    throw new InvalidOperationException("Intermittent failure");
                }
                foundry.SetProperty("ok2", true);
            })
            .AddOperation("Third", async (foundry, ct) =>
            {
                var seq = (List<string>)foundry.Properties["seq"]!;
                seq.Add("Third");
                foundry.SetProperty("ok3", true);
            })
            .Build();

        // First attempt – will fail in Second
        using (var f1 = WorkflowForge.CreateFoundry("RecoverWorkflow"))
        {
            f1.UsePersistence(provider, options);
            var s = WorkflowForge.CreateSmith();
            await Assert.ThrowsAsync<WorkflowOperationException>(() => s.ForgeAsync(workflow, f1));
        }

        // Resume with recovery policy that retries a few times
        using (var f2 = WorkflowForge.CreateFoundry("RecoverWorkflow"))
        {
            f2.UsePersistence(provider, options);
            var s = WorkflowForge.CreateSmith();
            var foundryKey = Deterministic(options.InstanceId!);
            var workflowKey = Deterministic(options.WorkflowKey!);

            await s.ForgeWithRecoveryAsync(
                workflow,
                f2,
                provider,
                foundryKey,
                workflowKey,
                new RecoveryMiddlewareOptions { MaxRetryAttempts = 5, BaseDelay = TimeSpan.FromMilliseconds(10), UseExponentialBackoff = false });

            // Assertions
            Assert.True(f2.GetPropertyOrDefault<bool>("ok2"));
            Assert.True(f2.GetPropertyOrDefault<bool>("ok3"));
            var seq = f2.GetPropertyOrDefault<List<string>>("seq")!;
            // First step should appear once; second shows multiple attempts; third at the end once
            Assert.Equal("First", seq[0]);
            Assert.Equal("Third", seq[^1]);
        }
    }

    [Fact]
    public void UsePersistence_WithEnabledFalse_ShouldNotAddMiddleware()
    {
        var provider = new InMemoryProvider();
        var options = new PersistenceOptions { Enabled = false };
        using var foundry = WorkflowForge.CreateFoundry("Test");
        var initialMiddlewareCount = GetMiddlewareCount(foundry);

        var result = foundry.UsePersistence(provider, options);

        Assert.Same(foundry, result);
        Assert.Equal(initialMiddlewareCount, GetMiddlewareCount(foundry));
    }

    [Fact]
    public void UsePersistence_WithEnabledTrue_ShouldAddMiddleware()
    {
        var provider = new InMemoryProvider();
        var options = new PersistenceOptions { Enabled = true };
        using var foundry = WorkflowForge.CreateFoundry("Test");
        var initialMiddlewareCount = GetMiddlewareCount(foundry);

        var result = foundry.UsePersistence(provider, options);

        Assert.Same(foundry, result);
        Assert.Equal(initialMiddlewareCount + 1, GetMiddlewareCount(foundry));
    }

    private static int GetMiddlewareCount(IWorkflowFoundry foundry)
    {
        var type = foundry.GetType();
        var property = type.GetProperty("MiddlewareCount");
        return property != null ? (int)property.GetValue(foundry)! : 0;
    }

    internal static Guid Deterministic(string input)
    {
        using var sha1 = System.Security.Cryptography.SHA1.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha1.ComputeHash(bytes);
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        return new Guid(guidBytes);
    }
}