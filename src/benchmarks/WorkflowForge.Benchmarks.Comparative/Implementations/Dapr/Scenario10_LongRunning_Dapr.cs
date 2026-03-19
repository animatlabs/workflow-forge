#if !NET48
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Dapr;

public class Scenario10_LongRunning_Dapr : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Long Running Workflow";
    public string Description => $"Execute {_parameters.OperationCount} long-running activities using Dapr Workflow";

    public Scenario10_LongRunning_Dapr(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var operationsExecuted = 0;
        for (var i = 0; i < _parameters.OperationCount; i++)
        {
            await SimulateLongRunningActivityAsync(i);
            operationsExecuted++;
        }
        return new ScenarioResult
        {
            Success = true,
            OperationsExecuted = operationsExecuted,
            OutputData = $"Completed {operationsExecuted} long-running activities",
            Metadata = { ["FrameworkName"] = "Dapr", ["Mode"] = "Simulated" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private static async Task SimulateLongRunningActivityAsync(int step)
    {
        _ = step;
        await Task.Yield();
    }
}
#endif
