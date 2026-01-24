using WorkflowForge.Benchmarks.Comparative.Scenarios;
using WorkflowForge.Extensions;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;

public class Scenario9_StateMachine_WorkflowForge : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "State Machine";
    public string Description => $"Execute {_parameters.OperationCount} state transitions";

    public Scenario9_StateMachine_WorkflowForge(ScenarioParameters parameters)
    {
        _parameters = parameters;
    }

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        using var foundry = global::WorkflowForge.WorkflowForge.CreateFoundry("StateMachine");
        foundry.Properties["state"] = 0;

        for (int i = 0; i < _parameters.OperationCount; i++)
        {
            var transitionIndex = i;
            foundry.WithOperation($"Transition_{transitionIndex}", foundry =>
            {
                    var currentState = foundry.Properties.TryGetValue("state", out var stateValue) && stateValue is int state
                        ? state
                        : 0;
                    foundry.Properties["state"] = currentState + 1;
                foundry.Properties["last_transition"] = transitionIndex;
                return Task.CompletedTask;
            });
        }

        await foundry.ForgeAsync();

        var finalState = foundry.Properties.TryGetValue("state", out var finalStateValue) && finalStateValue is int finalStateCount
            ? finalStateCount
            : 0;
        return new ScenarioResult
        {
            Success = finalState == _parameters.OperationCount,
            OperationsExecuted = finalState,
            OutputData = $"Final state: {finalState}",
            Metadata = { ["FrameworkName"] = "WorkflowForge" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;
}
