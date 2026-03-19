#if !NET48
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowEngineNet;

public class Scenario1_SimpleSequential_WorkflowEngineNet : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Simple Sequential Workflow";
    public string Description => $"Execute {_parameters.OperationCount} simple sequential operations using WorkflowEngine.NET";

    public Scenario1_SimpleSequential_WorkflowEngineNet(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var operationsExecuted = 0;
        for (var i = 0; i < _parameters.OperationCount; i++)
        {
            await SimulateCommandAsync($"Command{i + 1}");
            operationsExecuted++;
        }
        return new ScenarioResult
        {
            Success = true,
            OperationsExecuted = operationsExecuted,
            OutputData = $"Completed {operationsExecuted} sequential operations",
            Metadata = { ["FrameworkName"] = "WorkflowEngineNet", ["Mode"] = "Simulated" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private static Task SimulateCommandAsync(string commandName) { _ = commandName.Length; return Task.CompletedTask; }
}
#endif
