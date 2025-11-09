using WorkflowForge.Benchmarks.Comparative.Scenarios;
using WorkflowForge.Configurations;
using WorkflowForge.Extensions;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;

/// <summary>
/// Scenario 1: Simple Sequential Workflow - WorkflowForge Implementation
/// Tests: Basic workflow execution with N sequential operations
/// </summary>
public class Scenario1_SimpleSequential_WorkflowForge : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private FoundryConfiguration _config = null!;

    public string Name => "Simple Sequential Workflow";
    public string Description => $"Execute {_parameters.OperationCount} simple sequential operations";

    public Scenario1_SimpleSequential_WorkflowForge(ScenarioParameters parameters)
    {
        _parameters = parameters;
    }

    public Task SetupAsync()
    {
        // Use high-performance configuration for fair comparison
        _config = FoundryConfiguration.HighPerformance();
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        using var foundry = global::WorkflowForge.WorkflowForge.CreateFoundry("SimpleSequential", _config);

        // Add N sequential operations
        for (int i = 0; i < _parameters.OperationCount; i++)
        {
            var operationIndex = i;
            foundry.WithOperation($"Operation_{operationIndex}", async (foundry) =>
            {
                // Minimal async work to match other frameworks
                await Task.Yield();
                foundry.Properties[$"result_{operationIndex}"] = $"Result_{operationIndex}";
            });
        }

        // Execute workflow
        await foundry.ForgeAsync();

        // Validate results
        var success = foundry.Properties.Count >= _parameters.OperationCount;

        return new ScenarioResult
        {
            Success = success,
            OperationsExecuted = _parameters.OperationCount,
            OutputData = $"Completed {_parameters.OperationCount} operations",
            Metadata = { ["FrameworkName"] = "WorkflowForge" }
        };
    }

    public Task CleanupAsync()
    {
        return Task.CompletedTask;
    }
}