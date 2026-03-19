#if !NET48
using OptimaJet.Workflow.Core.Model;
using OptimaJet.Workflow.Core.Model.Builder;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowEngineNet;

/// <summary>
/// Scenario 2: Data Passing - WorkflowEngine.NET
/// Models data flowing through N steps via workflow parameters.
/// </summary>
public class Scenario2_DataPassing_WorkflowEngineNet : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private ProcessDefinition _definition = null!;

    public string Name => "Data Passing Workflow";
    public string Description => $"Pass data through {_parameters.OperationCount} steps (WorkflowEngine.NET parameters)";

    public Scenario2_DataPassing_WorkflowEngineNet(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync()
    {
        _definition = WorkflowEngineNetInfrastructure.BuildLinearScheme("S2_DataPassing", _parameters.OperationCount);
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var state = new WorkflowState(_definition);
        state.Data["input"] = "initial_value";

        for (var i = 0; i < _parameters.OperationCount; i++)
        {
            var current = state.Data.TryGetValue("input", out var v) ? v?.ToString() : "";
            state.Data[$"step_{i}"] = $"transformed_{current}_{i}";
            state.Data["input"] = state.Data[$"step_{i}"];
            await state.ExecuteNextCommandAsync();
        }
        await state.ExecuteFinishCommandAsync();

        return new ScenarioResult
        {
            Success = state.IsComplete,
            OperationsExecuted = state.StepsExecuted,
            OutputData = state.Data.TryGetValue("input", out var final) ? final?.ToString() : "",
            Metadata = { ["FrameworkName"] = "WorkflowEngineNet", ["Mode"] = "StateMachineSimulation", ["SchemeBuiltWith"] = "ProcessDefinitionBuilder" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;
}
#endif
