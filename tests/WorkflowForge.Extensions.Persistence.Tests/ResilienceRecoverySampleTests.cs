using System;
using System.Threading.Tasks;
using WorkflowForge.Extensions.Persistence.Recovery;
using WorkflowForge.Extensions.Persistence.Recovery.Options;
using WorkflowForge.Extensions.Resilience;
using Xunit;
using PersistenceAbstractions = global::WorkflowForge.Extensions.Persistence.Abstractions;

namespace WorkflowForge.Extensions.Persistence.Tests;

public class ResilienceRecoverySampleTests
{
    [Fact]
    public async Task RecoveryPlusResilience_CompletesAfterResume()
    {
        // Arrange: a flaky workflow that fails first two attempts of its flaky step
        var workflow = WorkflowForge.CreateWorkflow()
            .WithName("ResilienceRecoveryDemo_Test")
            .AddOperation("Init", async (foundry, ct) =>
            {
                if (!foundry.TryGetProperty<DateTimeOffset>("startedAt", out _))
                {
                    foundry.SetProperty("startedAt", DateTimeOffset.UtcNow);
                }
            })
            .AddOperation("Flaky", async (foundry, ct) =>
            {
                var startedAt = foundry.GetPropertyOrDefault("startedAt", DateTimeOffset.UtcNow);
                var elapsed = DateTimeOffset.UtcNow - startedAt;
                if (elapsed < TimeSpan.FromMilliseconds(50))
                {
                    throw new InvalidOperationException($"Flaky failed due to transient condition ({elapsed.TotalMilliseconds:F0}ms)");
                }
            })
            .AddOperation("Finalize", async (foundry, ct) =>
            {
                foundry.SetProperty("done", true);
            })
            .Build();

        var provider = new InMemoryPersistenceProvider();
        var options = new PersistenceOptions { InstanceId = "res-rec-instance-test", WorkflowKey = "res-rec-demo-test" };
        // Keys will be taken from the snapshot saved by middleware to avoid mismatch

        // First run: persistence + small retry; expect failure and snapshot
        using (var f1 = WorkflowForge.CreateFoundry("ResilienceRecoveryDemo_Test"))
        {
            f1.UsePersistence(provider, options);
            var retry = RetryMiddleware.WithExponentialBackoff(
                f1.Logger,
                initialDelay: TimeSpan.FromMilliseconds(10),
                maxDelay: TimeSpan.FromMilliseconds(30),
                maxAttempts: 2);
            f1.AddMiddleware(retry);

            var smith = WorkflowForge.CreateSmith();
            await Assert.ThrowsAnyAsync<Exception>(async () => await smith.ForgeAsync(workflow, f1));
            Assert.True(provider.Count > 0);
        }

        // Give the transient window time to elapse
        await Task.Delay(120);

        // Second run: recovery + retry completes
        using (var f2 = WorkflowForge.CreateFoundry("ResilienceRecoveryDemo_Test"))
        {
            f2.UsePersistence(provider, options);
            var retry = RetryMiddleware.WithExponentialBackoff(
                f2.Logger,
                initialDelay: TimeSpan.FromMilliseconds(10),
                maxDelay: TimeSpan.FromMilliseconds(30),
                maxAttempts: 2);
            f2.AddMiddleware(retry);

            var smith = WorkflowForge.CreateSmith();
            var key = provider.FirstKey()!.Value;
            await smith.ForgeWithRecoveryAsync(
                workflow,
                f2,
                provider,
                key.foundryKey,
                key.workflowKey,
                new RecoveryMiddlewareOptions { MaxRetryAttempts = 2, BaseDelay = TimeSpan.FromMilliseconds(10), UseExponentialBackoff = true });

            Assert.True(f2.GetPropertyOrDefault("done", false));
            // Transient window should have elapsed across resume, so completion expected
        }
    }

    private sealed class InMemoryPersistenceProvider : PersistenceAbstractions.IWorkflowPersistenceProvider
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<(Guid, Guid), PersistenceAbstractions.WorkflowExecutionSnapshot> _store = new();

        public Task SaveAsync(PersistenceAbstractions.WorkflowExecutionSnapshot snapshot, System.Threading.CancellationToken cancellationToken = default)
        {
            _store[(snapshot.FoundryExecutionId, snapshot.WorkflowId)] = snapshot;
            return Task.CompletedTask;
        }

        public Task<PersistenceAbstractions.WorkflowExecutionSnapshot?> TryLoadAsync(Guid foundryExecutionId, Guid workflowId, System.Threading.CancellationToken cancellationToken = default)
        {
            _store.TryGetValue((foundryExecutionId, workflowId), out var snapshot);
            return Task.FromResult<PersistenceAbstractions.WorkflowExecutionSnapshot?>(snapshot);
        }

        public Task DeleteAsync(Guid foundryExecutionId, Guid workflowId, System.Threading.CancellationToken cancellationToken = default)
        {
            _store.TryRemove((foundryExecutionId, workflowId), out _);
            return Task.CompletedTask;
        }

        public bool HasSnapshot(Guid foundryKey, Guid workflowKey) => _store.ContainsKey((foundryKey, workflowKey));

        public int Count => _store.Count;

        public (Guid foundryKey, Guid workflowKey)? FirstKey()
        {
            foreach (var kv in _store)
            {
                return (kv.Key.Item1, kv.Key.Item2);
            }
            return null;
        }
    }

    private static Guid DeterministicGuid(string input)
    {
        using var sha1 = System.Security.Cryptography.SHA1.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha1.ComputeHash(bytes);
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        return new Guid(guidBytes);
    }
}