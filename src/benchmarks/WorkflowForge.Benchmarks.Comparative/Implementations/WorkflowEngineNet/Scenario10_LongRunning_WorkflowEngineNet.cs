#if !NET48
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowEngineNet;

public class Scenario10_LongRunning_WorkflowEngineNet : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Long Running Workflow";
    public string Description => $"Execute {_parameters.OperationCount} long-running commands using WorkflowEngine.NET";

    public Scenario10_LongRunning_WorkflowEngineNet(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var operationsExecuted = 0;
        for (var i = 0; i < _parameters.OperationCount; i++)
        {
            await SimulateLongRunningCommandAsync(i);
            operationsExecuted++;
        }
        return new ScenarioResult
        {
            Success = true,
            OperationsExecuted = operationsExecuted,
            OutputData = $"Completed {operationsExecuted} long-running commands",
            Metadata = { ["FrameworkName"] = "WorkflowEngineNet", ["Mode"] = "Simulated" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private static async Task SimulateLongRunningCommandAsync(int step)
    {
        _ = step;
        await Task.Yield();
    }
}
#endif
