using WorkflowForge.Benchmarks.Comparative.Scenarios;
using WorkflowForge.Extensions;
using WorkflowForge.Operations;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;

/// <summary>
/// Scenario 4: Loop/ForEach Processing - WorkflowForge Implementation
/// Tests: Collection iteration performance
/// </summary>
public class Scenario4_LoopProcessing_WorkflowForge : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Loop/ForEach Processing";
    public string Description => $"Process {_parameters.ItemCount} items in collection";

    public Scenario4_LoopProcessing_WorkflowForge(ScenarioParameters parameters)
    {
        _parameters = parameters;
    }

    public Task SetupAsync()
    {
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        using var foundry = global::WorkflowForge.WorkflowForge.CreateFoundry("LoopProcessing");

        // Generate collection to process
        var items = Enumerable.Range(1, _parameters.ItemCount).Select(i => $"Item_{i}").ToArray();

        foundry.Properties["processed_count"] = 0;

        // Create ForEach operation
        var processOperation = new DelegateWorkflowOperation("ProcessItem", async (input, foundry, token) =>
        {
            await Task.Yield();
            var count = foundry.Properties.GetValueOrDefault("processed_count", 0);
            foundry.Properties["processed_count"] = (int)count + 1;
            return "Processed";
        });

        var forEachOp = new ForEachWorkflowOperation(
            new[] { processOperation },
            TimeSpan.FromMinutes(1),
            ForEachDataStrategy.SharedInput,
            null,
            "ProcessItems"
        );

        foundry.WithOperation(forEachOp);

        await forEachOp.ForgeAsync(items, foundry);

        var processedCount = (int)foundry.Properties.GetValueOrDefault("processed_count", 0);
        var success = processedCount == _parameters.ItemCount;

        return new ScenarioResult
        {
            Success = success,
            OperationsExecuted = processedCount,
            OutputData = $"Processed {processedCount} items",
            Metadata = { ["FrameworkName"] = "WorkflowForge" }
        };
    }

    public Task CleanupAsync()
    {
        return Task.CompletedTask;
    }
}