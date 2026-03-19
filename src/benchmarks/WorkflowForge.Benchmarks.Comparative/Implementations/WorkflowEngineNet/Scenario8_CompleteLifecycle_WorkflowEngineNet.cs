#if !NET48
using OptimaJet.Workflow.Core.Model;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowEngineNet;

/// <summary>
/// Scenario 8: Complete Lifecycle - WorkflowEngine.NET
/// Measures full create → build scheme → execute → dispose lifecycle.
/// </summary>
public class Scenario8_CompleteLifecycle_WorkflowEngineNet : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Complete Lifecycle";
    public string Description => "Full workflow lifecycle: scheme creation → execution → completion";

    public Scenario8_CompleteLifecycle_WorkflowEngineNet(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        // Build scheme
        var definition = WorkflowEngineNetInfrastructure.BuildLinearScheme("S8_Lifecycle", _parameters.OperationCount);

        // Execute
        var state = new WorkflowState(definition);
        for (var i = 0; i < _parameters.OperationCount; i++)
            await state.ExecuteNextCommandAsync();
        await state.ExecuteFinishCommandAsync();

        return new ScenarioResult
        {
            Success = state.IsComplete,
            OperationsExecuted = state.StepsExecuted,
            OutputData = "Complete lifecycle finished",
            Metadata = { ["FrameworkName"] = "WorkflowEngineNet", ["Mode"] = "StateMachineSimulation", ["SchemeBuiltWith"] = "ProcessDefinitionBuilder" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;
}
#endif
