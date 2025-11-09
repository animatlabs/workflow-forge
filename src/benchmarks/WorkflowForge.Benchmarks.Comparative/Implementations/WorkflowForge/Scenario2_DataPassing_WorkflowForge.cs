using WorkflowForge.Benchmarks.Comparative.Scenarios;
using WorkflowForge.Configurations;
using WorkflowForge.Extensions;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;

/// <summary>
/// Scenario 2: Data Passing Workflow - WorkflowForge Implementation
/// Tests: Context data access overhead and modification patterns
/// </summary>
public class Scenario2_DataPassing_WorkflowForge : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private FoundryConfiguration _config = null!;

    public string Name => "Data Passing Workflow";
    public string Description => $"Read, modify, and write {_parameters.OperationCount} context values";

    public Scenario2_DataPassing_WorkflowForge(ScenarioParameters parameters)
    {
        _parameters = parameters;
    }

    public Task SetupAsync()
    {
        _config = FoundryConfiguration.HighPerformance();
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        using var foundry = global::WorkflowForge.WorkflowForge.CreateFoundry("DataPassing", _config);

        // Initialize some data
        foundry.Properties["initial_value"] = 0;

        // Add operations that read, modify, and write data
        for (int i = 0; i < _parameters.OperationCount; i++)
        {
            var operationIndex = i;
            foundry.WithOperation($"DataOp_{operationIndex}", async (foundry) =>
            {
                await Task.Yield();

                // Read existing data
                var currentValue = foundry.Properties.GetValueOrDefault("initial_value", 0);

                // Modify data
                var newValue = (int)currentValue + 1;

                // Write back
                foundry.Properties["initial_value"] = newValue;
                foundry.Properties[$"operation_{operationIndex}_result"] = $"Processed_{newValue}";
            });
        }

        await foundry.ForgeAsync();

        var finalValue = foundry.Properties.GetValueOrDefault("initial_value", 0);
        var success = (int)finalValue == _parameters.OperationCount;

        return new ScenarioResult
        {
            Success = success,
            OperationsExecuted = _parameters.OperationCount,
            OutputData = $"Final value: {finalValue}",
            Metadata = { ["FrameworkName"] = "WorkflowForge" }
        };
    }

    public Task CleanupAsync()
    {
        return Task.CompletedTask;
    }
}