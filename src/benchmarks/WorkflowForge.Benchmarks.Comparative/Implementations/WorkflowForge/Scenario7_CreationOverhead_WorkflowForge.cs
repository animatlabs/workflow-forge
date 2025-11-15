using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;

public class Scenario7_CreationOverhead_WorkflowForge : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Creation Overhead";
    public string Description => "Measure foundry creation time";

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
        // Just create foundry, don't execute
        using var foundry = global::WorkflowForge.WorkflowForge.CreateFoundry("CreationTest");

        await Task.CompletedTask;

        return new ScenarioResult
        {
            Success = foundry != null,
            OperationsExecuted = 0,
            OutputData = "Foundry created",
            Metadata = { ["FrameworkName"] = "WorkflowForge" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;
}