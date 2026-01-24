using WorkflowForge.Benchmarks.Comparative.Scenarios;
using WorkflowForge.Extensions;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;

public class Scenario7_CreationOverhead_WorkflowForge : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Creation Overhead";
    public string Description => "Measure workflow definition creation time";

    public Scenario7_CreationOverhead_WorkflowForge(ScenarioParameters parameters)
    {
        _parameters = parameters;
    }

    public Task SetupAsync()
    {
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        const int operationCount = 10;
        // Create foundry and define operations, but do not execute.
        using var foundry = global::WorkflowForge.WorkflowForge.CreateFoundry("CreationTest");

        for (int i = 0; i < operationCount; i++)
        {
            foundry.WithOperation($"NoOp_{i}", _ => Task.CompletedTask);
        }

        await Task.CompletedTask;

        return new ScenarioResult
        {
            Success = foundry != null,
            OperationsExecuted = 0,
            OutputData = $"Foundry created with {operationCount} operations",
            Metadata = { ["FrameworkName"] = "WorkflowForge" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;
}