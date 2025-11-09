using WorkflowForge.Benchmarks.Comparative.Scenarios;
using WorkflowForge.Configurations;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;

public class Scenario7_CreationOverhead_WorkflowForge : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private FoundryConfiguration _config = null!;

    public string Name => "Creation Overhead";
    public string Description => "Measure foundry creation time";

    public Scenario7_CreationOverhead_WorkflowForge(ScenarioParameters parameters)
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
        // Just create foundry, don't execute
        using var foundry = global::WorkflowForge.WorkflowForge.CreateFoundry("CreationTest", _config);

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