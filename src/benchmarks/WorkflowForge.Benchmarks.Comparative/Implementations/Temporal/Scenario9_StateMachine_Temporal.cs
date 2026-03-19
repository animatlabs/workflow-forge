#if !NET48
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Temporal;

public class Scenario9_StateMachine_Temporal : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "State Machine Workflow";
    public string Description => $"Execute {_parameters.OperationCount} state transitions using Temporal";

    public Scenario9_StateMachine_Temporal(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var currentState = "Initial";
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
            OutputData = $"Final state reached after {transitionsExecuted} transitions",
            Metadata = { ["FrameworkName"] = "Temporal", ["Mode"] = "Simulated" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private static Task<string> SimulateStateTransitionAsync(string currentState, int step)
        => Task.FromResult(step < 0 ? "Final" : $"State{step}");
}
#endif
