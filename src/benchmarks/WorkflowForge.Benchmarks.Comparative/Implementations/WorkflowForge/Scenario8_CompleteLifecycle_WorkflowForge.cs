using WorkflowForge.Benchmarks.Comparative.Scenarios;
using WorkflowForge.Configurations;
using WorkflowForge.Extensions;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;

public class Scenario8_CompleteLifecycle_WorkflowForge : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private FoundryConfiguration _config = null!;

    public string Name => "Complete Lifecycle";
    public string Description => "Full create→execute→cleanup cycle";

    public Scenario8_CompleteLifecycle_WorkflowForge(ScenarioParameters parameters)
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
        // Create
        using var foundry = global::WorkflowForge.WorkflowForge.CreateFoundry("Lifecycle", _config);

        // Configure
        foundry.WithOperation("Op1", async (foundry) =>
        {
            await Task.Yield();
            foundry.Properties["executed"] = true;
        });

        // Execute
        await foundry.ForgeAsync();

        // Cleanup happens via Dispose
        var executed = (bool)foundry.Properties.GetValueOrDefault("executed", false);

        return new ScenarioResult
        {
            Success = executed,
            OperationsExecuted = 1,
            OutputData = "Lifecycle complete",
            Metadata = { ["FrameworkName"] = "WorkflowForge" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;
}