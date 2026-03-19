#if !NET48
using OptimaJet.Workflow.Core.Model;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowEngineNet;

/// <summary>
/// Scenario 10: Long Running - WorkflowEngine.NET
/// Simulates a workflow with a timed wait between state transitions.
/// </summary>
public class Scenario10_LongRunning_WorkflowEngineNet : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private ProcessDefinition _definition = null!;

    public string Name => "Long Running Workflow";
    public string Description => $"Workflow with {_parameters.DelayMilliseconds}ms delay between transitions";

    public Scenario10_LongRunning_WorkflowEngineNet(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync()
    {
        _definition = WorkflowEngineNetInfrastructure.BuildLinearScheme("S10_LongRunning", 3);
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var state = new WorkflowState(_definition);
        await state.ExecuteNextCommandAsync();
        await Task.Delay(_parameters.DelayMilliseconds);
        await state.ExecuteNextCommandAsync();
        await state.ExecuteFinishCommandAsync();

        return new ScenarioResult
        {
            Success = state.IsComplete,
            OperationsExecuted = state.StepsExecuted,
            OutputData = $"Long-running workflow with {_parameters.DelayMilliseconds}ms delay",
            Metadata = { ["FrameworkName"] = "WorkflowEngineNet", ["Mode"] = "StateMachineSimulation", ["SchemeBuiltWith"] = "ProcessDefinitionBuilder" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;
}
#endif
