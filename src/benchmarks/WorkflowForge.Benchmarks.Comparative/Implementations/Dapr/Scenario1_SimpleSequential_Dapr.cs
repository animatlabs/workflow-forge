#if !NET48
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Dapr;

public class Scenario1_SimpleSequential_Dapr : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Simple Sequential Workflow";
    public string Description => $"Execute {_parameters.OperationCount} simple sequential operations using Dapr Workflow";

    public Scenario1_SimpleSequential_Dapr(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var operationsExecuted = 0;
        for (var i = 0; i < _parameters.OperationCount; i++)
        {
            await SimulateActivityAsync($"activity-{i}");
            operationsExecuted++;
        }
        return new ScenarioResult
        {
            Success = true,
            OperationsExecuted = operationsExecuted,
            OutputData = $"Completed {operationsExecuted} sequential operations",
            Metadata = { ["FrameworkName"] = "Dapr", ["Mode"] = "Simulated" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private static Task SimulateActivityAsync(string activityName) { _ = activityName.Length; return Task.CompletedTask; }
}
#endif
