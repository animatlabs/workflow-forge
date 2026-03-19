#if !NET48
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowEngineNet;

public class Scenario9_StateMachine_WorkflowEngineNet : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "State Machine Workflow";
    public string Description => $"Execute {_parameters.OperationCount} state transitions using WorkflowEngine.NET";

    public Scenario9_StateMachine_WorkflowEngineNet(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var currentActivity = "Initial";
        var transitionsExecuted = 0;

        for (var i = 0; i < _parameters.OperationCount; i++)
        {
            currentActivity = await SimulateCommandTransitionAsync(currentActivity, $"Command{i}");
            transitionsExecuted++;
        }

        await SimulateCommandTransitionAsync(currentActivity, "Complete");

        return new ScenarioResult
        {
            Success = true,
            OperationsExecuted = transitionsExecuted,
            OutputData = $"Completed {transitionsExecuted} state transitions",
            Metadata = { ["FrameworkName"] = "WorkflowEngineNet", ["Mode"] = "Simulated" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private static Task<string> SimulateCommandTransitionAsync(string fromActivity, string command)
        => Task.FromResult(command == "Complete" ? "Final" : $"Activity-{command}");
}
#endif
