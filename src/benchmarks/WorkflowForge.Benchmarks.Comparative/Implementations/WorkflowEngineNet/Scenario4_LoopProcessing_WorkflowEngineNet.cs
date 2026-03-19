#if !NET48
using OptimaJet.Workflow.Core.Model;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowEngineNet;

/// <summary>
/// Scenario 4: Loop Processing - WorkflowEngine.NET
/// Iterates over ItemCount items using sequential state transitions.
/// </summary>
public class Scenario4_LoopProcessing_WorkflowEngineNet : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private ProcessDefinition _definition = null!;

    public string Name => "Loop Processing";
    public string Description => $"Process {_parameters.ItemCount} items via state transitions";

    public Scenario4_LoopProcessing_WorkflowEngineNet(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync()
    {
        _definition = WorkflowEngineNetInfrastructure.BuildLinearScheme("S4_Loop", _parameters.ItemCount);
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var state = new WorkflowState(_definition);
        for (var i = 0; i < _parameters.ItemCount; i++)
        {
            state.Data[$"item_{i}"] = i * 2;
            await state.ExecuteNextCommandAsync();
        }
        await state.ExecuteFinishCommandAsync();

        return new ScenarioResult
        {
            Success = state.IsComplete,
            OperationsExecuted = state.StepsExecuted,
            OutputData = $"Processed {_parameters.ItemCount} items",
            Metadata = { ["FrameworkName"] = "WorkflowEngineNet", ["Mode"] = "StateMachineSimulation", ["SchemeBuiltWith"] = "ProcessDefinitionBuilder" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;
}
#endif
