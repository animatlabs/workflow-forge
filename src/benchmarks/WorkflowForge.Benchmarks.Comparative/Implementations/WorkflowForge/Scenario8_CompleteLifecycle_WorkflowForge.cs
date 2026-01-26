using WorkflowForge.Benchmarks.Comparative.Scenarios;
using WorkflowForge.Extensions;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;

public class Scenario8_CompleteLifecycle_WorkflowForge : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Complete Lifecycle";
    public string Description => "Full create→execute→cleanup cycle";

    public Scenario8_CompleteLifecycle_WorkflowForge(ScenarioParameters parameters)
    {
        _parameters = parameters;
    }

    public Task SetupAsync()
    {
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        // Create
        using var foundry = global::WorkflowForge.WorkflowForge.CreateFoundry("Lifecycle");

        // Configure
        foundry.WithOperation("Op1", async (foundry) =>
        {
            await Task.Yield();
            foundry.Properties["executed"] = true;
        });

        // Execute
        await foundry.ForgeAsync();

        // Cleanup happens via Dispose
        var executed = foundry.Properties.TryGetValue("executed", out var executedValue) && executedValue is bool executedFlag && executedFlag;

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