using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Persistence.Abstractions;
using Xunit;

namespace WorkflowForge.Extensions.Persistence.Tests;

/// <summary>
/// Asserts that each persistence option changes what the provider is asked to do.
/// </summary>
public class PersistenceOptionBehaviourShould
{
    private sealed class RecordingProvider : IWorkflowPersistenceProvider
    {
        private readonly Dictionary<(Guid, Guid), WorkflowExecutionSnapshot> _store = new();

        public List<int> SavedNextIndexes { get; } = new();

        public int Deletes { get; private set; }

        public Task SaveAsync(WorkflowExecutionSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            SavedNextIndexes.Add(snapshot.NextOperationIndex);
            _store[(snapshot.FoundryExecutionId, snapshot.WorkflowId)] = snapshot;
            return Task.CompletedTask;
        }

        public Task<WorkflowExecutionSnapshot?> TryLoadAsync(Guid f, Guid w, CancellationToken cancellationToken = default)
        {
            _store.TryGetValue((f, w), out var s);
            return Task.FromResult<WorkflowExecutionSnapshot?>(s);
        }

        public Task DeleteAsync(Guid f, Guid w, CancellationToken cancellationToken = default)
        {
            Deletes++;
            _store.Remove((f, w));
            return Task.CompletedTask;
        }
    }

    private static IWorkflow ThreeStepWorkflow(string name) =>
        WorkflowForge.CreateWorkflow(name)
            .AddOperation("A", (_, _) => Task.CompletedTask)
            .AddOperation("B", (_, _) => Task.CompletedTask)
            .AddOperation("C", (_, _) => Task.CompletedTask)
            .Build();

    private static async Task RunAsync(string name, RecordingProvider provider, PersistenceOptions options)
    {
        var workflow = ThreeStepWorkflow(name);
        using var foundry = WorkflowForge.CreateFoundry(name);
        foundry.UsePersistence(provider, options);
        using var smith = WorkflowForge.CreateSmith();
        await smith.ForgeAsync(workflow, foundry);
    }

    [Fact]
    public async Task CheckpointEveryOperation_GivenPersistOnOperationCompleteIsTrue()
    {
        var provider = new RecordingProvider();

        await RunAsync("PersistAll", provider, new PersistenceOptions
        {
            InstanceId = "persist-all",
            WorkflowKey = "PersistAll"
        });

        Assert.Equal(new[] { 1, 2, 3 }, provider.SavedNextIndexes.ToArray());
    }

    [Fact]
    public async Task CheckpointOnlyTheFinalOperation_GivenPersistOnOperationCompleteIsFalse()
    {
        var provider = new RecordingProvider();

        await RunAsync("PersistFinalOnly", provider, new PersistenceOptions
        {
            InstanceId = "persist-final-only",
            WorkflowKey = "PersistFinalOnly",
            PersistOnOperationComplete = false
        });

        Assert.Equal(new[] { 3 }, provider.SavedNextIndexes.ToArray());
    }

    [Fact]
    public async Task SkipTheFinalCheckpoint_GivenPersistOnWorkflowCompleteIsFalse()
    {
        var provider = new RecordingProvider();

        await RunAsync("PersistNoFinal", provider, new PersistenceOptions
        {
            InstanceId = "persist-no-final",
            WorkflowKey = "PersistNoFinal",
            PersistOnWorkflowComplete = false
        });

        Assert.Equal(new[] { 1, 2 }, provider.SavedNextIndexes.ToArray());
        Assert.Equal(1, provider.Deletes);
    }

    [Fact]
    public async Task WriteNothing_GivenBothCompletionTriggersAreFalse()
    {
        var provider = new RecordingProvider();

        await RunAsync("PersistNone", provider, new PersistenceOptions
        {
            InstanceId = "persist-none",
            WorkflowKey = "PersistNone",
            PersistOnOperationComplete = false,
            PersistOnWorkflowComplete = false
        });

        Assert.Empty(provider.SavedNextIndexes);
    }

    [Fact]
    public async Task CheckpointTheFailedOperation_GivenPersistOnFailureIsTrue()
    {
        var provider = new RecordingProvider();
        var workflow = WorkflowForge.CreateWorkflow("PersistFailure")
            .AddOperation("A", (_, _) => Task.CompletedTask)
            .AddOperation("B", (_, _) => throw new InvalidOperationException("boom"))
            .Build();

        using var foundry = WorkflowForge.CreateFoundry("PersistFailure");
        foundry.UsePersistence(provider, new PersistenceOptions
        {
            InstanceId = "persist-failure",
            WorkflowKey = "PersistFailure"
        });
        using var smith = WorkflowForge.CreateSmith();

        await Assert.ThrowsAnyAsync<Exception>(() => smith.ForgeAsync(workflow, foundry));

        // 1 from A completing, then 1 again from B failing so a resume re-runs B.
        Assert.Equal(new[] { 1, 1 }, provider.SavedNextIndexes.ToArray());
    }

    [Fact]
    public async Task NotCheckpointTheFailedOperation_GivenPersistOnFailureIsFalse()
    {
        var provider = new RecordingProvider();
        var workflow = WorkflowForge.CreateWorkflow("PersistNoFailure")
            .AddOperation("A", (_, _) => Task.CompletedTask)
            .AddOperation("B", (_, _) => throw new InvalidOperationException("boom"))
            .Build();

        using var foundry = WorkflowForge.CreateFoundry("PersistNoFailure");
        foundry.UsePersistence(provider, new PersistenceOptions
        {
            InstanceId = "persist-no-failure",
            WorkflowKey = "PersistNoFailure",
            PersistOnFailure = false
        });
        using var smith = WorkflowForge.CreateSmith();

        await Assert.ThrowsAnyAsync<Exception>(() => smith.ForgeAsync(workflow, foundry));

        Assert.Equal(new[] { 1 }, provider.SavedNextIndexes.ToArray());
    }
}
