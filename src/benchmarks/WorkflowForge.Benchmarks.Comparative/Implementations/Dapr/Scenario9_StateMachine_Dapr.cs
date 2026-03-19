#if !NET48
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Dapr;

public class Scenario9_StateMachine_Dapr : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "State Machine Workflow";
    public string Description => $"Execute {_parameters.OperationCount} state transitions using Dapr Workflow";

    public Scenario9_StateMachine_Dapr(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var currentState = "Pending";
        var transitionsExecuted = 0;

        for (var i = 0; i < _parameters.OperationCount; i++)
        {
            currentState = await SimulateStateTransitionAsync(currentState, i);
            transitionsExecuted++;
        }

        await SimulateStateTransitionAsync(currentState, -1);

        return new ScenarioResult
        {
            Success = true,
            OperationsExecuted = transitionsExecuted,
            OutputData = $"Reached final state after {transitionsExecuted} transitions",
            Metadata = { ["FrameworkName"] = "Dapr", ["Mode"] = "Simulated" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private static Task<string> SimulateStateTransitionAsync(string current, int step)
        => Task.FromResult(step < 0 ? "Completed" : $"Running-{step}");
}
#endif
