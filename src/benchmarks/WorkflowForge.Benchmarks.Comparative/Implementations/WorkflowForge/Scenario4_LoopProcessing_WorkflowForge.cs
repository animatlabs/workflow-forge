using WorkflowForge.Benchmarks.Comparative.Scenarios;
using WorkflowForge.Extensions;

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

        for (int i = 0; i < items.Length; i++)
        {
            var operationIndex = i;
            var item = items[i];
            foundry.WithOperation($"ProcessItem_{operationIndex}", async (foundry) =>
            {
                await Task.Yield();
                var count = foundry.Properties.TryGetValue("processed_count", out var countValue) && countValue is int processedCount
                    ? processedCount
                    : 0;
                foundry.Properties["processed_count"] = count + 1;
                foundry.Properties[$"processed_{operationIndex}"] = item;
            });
        }

        await foundry.ForgeAsync();

        var processedCount = foundry.Properties.TryGetValue("processed_count", out var finalValue) && finalValue is int totalCount
            ? totalCount
            : 0;
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