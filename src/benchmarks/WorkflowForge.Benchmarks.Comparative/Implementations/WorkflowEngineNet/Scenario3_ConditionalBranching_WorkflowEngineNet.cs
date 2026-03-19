#if !NET48
using OptimaJet.Workflow.Core.Model;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowEngineNet;

/// <summary>
/// Scenario 3: Conditional Branching - WorkflowEngine.NET
/// Uses real branching scheme with GoTrue/GoFalse commands (50/50 split).
/// </summary>
public class Scenario3_ConditionalBranching_WorkflowEngineNet : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private ProcessDefinition _definition = null!;

    public string Name => "Conditional Branching";
    public string Description => $"Execute {_parameters.OperationCount} conditional operations (50/50 split)";

    public Scenario3_ConditionalBranching_WorkflowEngineNet(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync()
    {
        _definition = WorkflowEngineNetInfrastructure.BuildBranchingScheme("S3_Branching");
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var trueCount = 0;
        var falseCount = 0;

        for (var i = 0; i < _parameters.OperationCount; i++)
        {
            var state = new WorkflowState(_definition);
            var shouldBeTrue = i % 2 == 0;
            await state.ExecuteCommandAsync(shouldBeTrue ? "GoTrue" : "GoFalse");
            await state.ExecuteCommandAsync("Finish");
            if (shouldBeTrue) trueCount++; else falseCount++;
        }

        return new ScenarioResult
        {
            Success = (trueCount + falseCount) == _parameters.OperationCount,
            OperationsExecuted = _parameters.OperationCount,
            OutputData = $"True: {trueCount}, False: {falseCount}",
            Metadata = { ["FrameworkName"] = "WorkflowEngineNet", ["Mode"] = "StateMachineSimulation", ["SchemeBuiltWith"] = "ProcessDefinitionBuilder" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;
}
#endif
