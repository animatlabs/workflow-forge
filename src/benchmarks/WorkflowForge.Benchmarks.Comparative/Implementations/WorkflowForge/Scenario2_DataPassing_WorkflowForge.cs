using WorkflowForge.Benchmarks.Comparative.Scenarios;
using WorkflowForge.Extensions;
using WorkflowForge.Operations;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;

/// <summary>
/// Scenario 2: Data Passing Workflow - WorkflowForge Implementation
/// Tests: Context data access overhead and modification patterns
/// </summary>
public class Scenario2_DataPassing_WorkflowForge : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Data Passing Workflow";
    public string Description => $"Pass and increment data across {_parameters.OperationCount} operations";

    public Scenario2_DataPassing_WorkflowForge(ScenarioParameters parameters)
    {
        _parameters = parameters;
    }

    public Task SetupAsync()
    {
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        using var foundry = global::WorkflowForge.WorkflowForge.CreateFoundry("DataPassing");

        // Add operations that pass output to the next operation
        for (int i = 0; i < _parameters.OperationCount; i++)
        {
            var operationIndex = i;
            var operation = new DelegateWorkflowOperation<object?, int>($"DataOp_{operationIndex}", async (input, f, ct) =>
            {
                await Task.Yield();
                var currentValue = input is int value ? value : 0;
                return currentValue + 1;
            });

            foundry.AddOperation(operation);
        }

        await foundry.ForgeAsync();

        // Use the public orchestrator-level API instead of internal constants
        var lastIndex = _parameters.OperationCount - 1;
        var lastOperationName = $"DataOp_{lastIndex}";
        var finalValue = foundry.GetOperationOutput<int>(lastIndex, lastOperationName);
        var success = finalValue == _parameters.OperationCount;

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