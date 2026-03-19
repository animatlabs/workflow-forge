#if !NET48
using OptimaJet.Workflow.Core.Model;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowEngineNet;

/// <summary>
/// Scenario 9: State Machine - WorkflowEngine.NET
/// WorkflowEngine.NET is inherently state-machine based; this tests N command-driven transitions.
/// </summary>
public class Scenario9_StateMachine_WorkflowEngineNet : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private ProcessDefinition _definition = null!;

    public string Name => "State Machine";
    public string Description => $"Execute {_parameters.OperationCount} state transitions (native WorkflowEngine.NET model)";

    public Scenario9_StateMachine_WorkflowEngineNet(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync()
    {
        _definition = WorkflowEngineNetInfrastructure.BuildLinearScheme("S9_StateMachine", _parameters.OperationCount);
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var state = new WorkflowState(_definition);
        for (var i = 0; i < _parameters.OperationCount; i++)
            await state.ExecuteNextCommandAsync();
        await state.ExecuteFinishCommandAsync();

        return new ScenarioResult
        {
            Success = state.IsComplete,
            OperationsExecuted = state.StepsExecuted,
            OutputData = $"Final state: {state.CurrentActivityName}",
            Metadata = { ["FrameworkName"] = "WorkflowEngineNet", ["Mode"] = "StateMachineSimulation", ["SchemeBuiltWith"] = "ProcessDefinitionBuilder" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;
}
#endif
