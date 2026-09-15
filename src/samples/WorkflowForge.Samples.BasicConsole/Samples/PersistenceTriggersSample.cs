using WorkflowForge.Extensions.Persistence;
using WorkflowForge.Extensions.Persistence.Abstractions;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Shows how PersistOnOperationComplete, PersistOnWorkflowComplete and PersistOnFailure change
/// when a checkpoint is written, by counting the saves a recording provider receives.
/// </summary>
public class PersistenceTriggersSample : ISample
{
    public string Name => "Persistence Triggers";
    public string Description => "When each PersistOn* option writes a checkpoint";

    public async Task RunAsync()
    {
        Console.WriteLine("Comparing persistence triggers on a three-step workflow...");
        Console.WriteLine();

        await DescribeSuccessAsync("All triggers on (default)", new PersistenceOptions
        {
            InstanceId = "sample-all",
            WorkflowKey = "PersistenceTriggers"
        });

        await DescribeSuccessAsync("PersistOnOperationComplete = false", new PersistenceOptions
        {
            InstanceId = "sample-final-only",
            WorkflowKey = "PersistenceTriggers",
            PersistOnOperationComplete = false
        });

        await DescribeSuccessAsync("PersistOnWorkflowComplete = false", new PersistenceOptions
        {
            InstanceId = "sample-no-final",
            WorkflowKey = "PersistenceTriggers",
            PersistOnWorkflowComplete = false
        });

        await DescribeFailureAsync("PersistOnFailure = true (default)", new PersistenceOptions
        {
            InstanceId = "sample-failure",
            WorkflowKey = "PersistenceTriggersFailure"
        });

        await DescribeFailureAsync("PersistOnFailure = false", new PersistenceOptions
        {
            InstanceId = "sample-no-failure",
            WorkflowKey = "PersistenceTriggersFailure",
            PersistOnFailure = false
        });
    }

    private static async Task DescribeSuccessAsync(string label, PersistenceOptions options)
    {
        var provider = new RecordingProvider();

        var workflow = WorkflowForge.CreateWorkflow("PersistenceTriggers")
            .AddOperation("Reserve", (_, _) => Task.CompletedTask)
            .AddOperation("Charge", (_, _) => Task.CompletedTask)
            .AddOperation("Ship", (_, _) => Task.CompletedTask)
            .Build();

        using var foundry = WorkflowForge.CreateFoundry("PersistenceTriggers");
        foundry.UsePersistence(provider, options);

        using var smith = WorkflowForge.CreateSmith();
        await smith.ForgeAsync(workflow, foundry);

        Console.WriteLine($"{label}:");
        Console.WriteLine($"   Checkpoints written at NextOperationIndex: [{string.Join(", ", provider.SavedNextIndexes)}]");
        Console.WriteLine($"   Snapshot deleted on completion: {provider.Deletes > 0}");
        Console.WriteLine();
    }

    private static async Task DescribeFailureAsync(string label, PersistenceOptions options)
    {
        var provider = new RecordingProvider();

        var workflow = WorkflowForge.CreateWorkflow("PersistenceTriggersFailure")
            .AddOperation("Reserve", (_, _) => Task.CompletedTask)
            .AddOperation("Charge", (_, _) => throw new InvalidOperationException("card declined"))
            .Build();

        using var foundry = WorkflowForge.CreateFoundry("PersistenceTriggersFailure");
        foundry.UsePersistence(provider, options);

        using var smith = WorkflowForge.CreateSmith();
        try
        {
            await smith.ForgeAsync(workflow, foundry);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{label}:");
            Console.WriteLine($"   Workflow failed as expected: {ex.GetBaseException().Message}");
        }

        Console.WriteLine($"   Checkpoints written at NextOperationIndex: [{string.Join(", ", provider.SavedNextIndexes)}]");
        Console.WriteLine("   A repeated index means the failed step is re-run on resume.");
        Console.WriteLine();
    }

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

        public Task<WorkflowExecutionSnapshot?> TryLoadAsync(Guid foundryExecutionId, Guid workflowId, CancellationToken cancellationToken = default)
        {
            _store.TryGetValue((foundryExecutionId, workflowId), out var snapshot);
            return Task.FromResult<WorkflowExecutionSnapshot?>(snapshot);
        }

        public Task DeleteAsync(Guid foundryExecutionId, Guid workflowId, CancellationToken cancellationToken = default)
        {
            Deletes++;
            _store.Remove((foundryExecutionId, workflowId));
            return Task.CompletedTask;
        }
    }
}
