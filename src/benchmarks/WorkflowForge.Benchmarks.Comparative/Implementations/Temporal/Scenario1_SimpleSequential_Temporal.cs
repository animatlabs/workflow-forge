#if !NET48
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Temporal;

public class Scenario1_SimpleSequential_Temporal : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Simple Sequential Workflow";
    public string Description => $"Execute {_parameters.OperationCount} simple sequential operations using Temporal";

    public Scenario1_SimpleSequential_Temporal(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var operationsExecuted = 0;
        for (var i = 0; i < _parameters.OperationCount; i++)
        {
            await SimulateActivityAsync($"Step{i + 1}");
            operationsExecuted++;
        }
        return new ScenarioResult
        {
            Success = true,
            OperationsExecuted = operationsExecuted,
            OutputData = $"Completed {operationsExecuted} sequential operations",
            Metadata = { ["FrameworkName"] = "Temporal", ["Mode"] = "Simulated" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private static Task SimulateActivityAsync(string activityName) { _ = activityName.Length; return Task.CompletedTask; }
}
#endif
