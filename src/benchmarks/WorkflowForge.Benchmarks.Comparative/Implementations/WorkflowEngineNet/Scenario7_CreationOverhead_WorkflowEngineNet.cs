#if !NET48
using OptimaJet.Workflow.Core.Model;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowEngineNet;

/// <summary>
/// Scenario 7: Creation Overhead - WorkflowEngine.NET
/// Measures the cost of building a workflow scheme via ProcessDefinitionBuilder.
/// </summary>
public class Scenario7_CreationOverhead_WorkflowEngineNet : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Creation Overhead";
    public string Description => "Measure ProcessDefinitionBuilder scheme creation overhead";

    public Scenario7_CreationOverhead_WorkflowEngineNet(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync() => Task.CompletedTask;

    public Task<ScenarioResult> ExecuteAsync()
    {
        var definition = WorkflowEngineNetInfrastructure.BuildLinearScheme("S7_Creation", _parameters.OperationCount);

        return Task.FromResult(new ScenarioResult
        {
            Success = definition.Activities.Count == _parameters.OperationCount + 2,
            OperationsExecuted = _parameters.OperationCount,
            OutputData = $"Created scheme with {definition.Activities.Count} activities, {definition.Transitions.Count} transitions",
            Metadata = { ["FrameworkName"] = "WorkflowEngineNet", ["Mode"] = "StateMachineSimulation", ["SchemeBuiltWith"] = "ProcessDefinitionBuilder" }
        });
    }

    public Task CleanupAsync() => Task.CompletedTask;
}
#endif
