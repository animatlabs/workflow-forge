#if !NET48
using OptimaJet.Workflow.Core.Model;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowEngineNet;

/// <summary>
/// Scenario 1: Simple Sequential - WorkflowEngine.NET (OptimaJet)
/// Uses ProcessDefinitionBuilder API to define a real linear state-machine scheme.
/// Execution uses an in-memory state machine (full runtime requires external DB).
/// </summary>
public class Scenario1_SimpleSequential_WorkflowEngineNet : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private ProcessDefinition _definition = null!;

    public string Name => "Simple Sequential Workflow";
    public string Description => $"Execute {_parameters.OperationCount} sequential state transitions (WorkflowEngine.NET state machine)";

    public Scenario1_SimpleSequential_WorkflowEngineNet(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync()
    {
        _definition = WorkflowEngineNetInfrastructure.BuildLinearScheme("S1_Linear", _parameters.OperationCount);
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
            OutputData = $"Completed {state.StepsExecuted} state transitions",
            Metadata = { ["FrameworkName"] = "WorkflowEngineNet", ["Mode"] = "StateMachineSimulation", ["SchemeBuiltWith"] = "ProcessDefinitionBuilder" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;
}
#endif
