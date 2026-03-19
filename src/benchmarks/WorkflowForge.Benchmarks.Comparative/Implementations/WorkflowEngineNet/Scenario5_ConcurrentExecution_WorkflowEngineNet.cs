#if !NET48
using System.Collections.Concurrent;
using OptimaJet.Workflow.Core.Model;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowEngineNet;

/// <summary>
/// Scenario 5: Concurrent Execution - WorkflowEngine.NET
/// Runs multiple workflow instances concurrently (thread-safe state machine).
/// </summary>
public class Scenario5_ConcurrentExecution_WorkflowEngineNet : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private ProcessDefinition _definition = null!;

    public string Name => "Concurrent Execution";
    public string Description => $"Execute {_parameters.ConcurrencyLevel} workflows concurrently";

    public Scenario5_ConcurrentExecution_WorkflowEngineNet(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync()
    {
        _definition = WorkflowEngineNetInfrastructure.BuildLinearScheme("S5_Concurrent", _parameters.OperationCount);
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var results = new ConcurrentBag<bool>();
        var tasks = Enumerable.Range(0, _parameters.ConcurrencyLevel).Select(async _ =>
        {
            var state = new WorkflowState(_definition);
            for (var i = 0; i < _parameters.OperationCount; i++)
                await state.ExecuteNextCommandAsync();
            await state.ExecuteFinishCommandAsync();
            results.Add(state.IsComplete);
        });
        await Task.WhenAll(tasks);

        return new ScenarioResult
        {
            Success = results.All(r => r),
            OperationsExecuted = results.Count,
            OutputData = $"Completed {results.Count} concurrent workflows",
            Metadata = { ["FrameworkName"] = "WorkflowEngineNet", ["Mode"] = "StateMachineSimulation", ["SchemeBuiltWith"] = "ProcessDefinitionBuilder" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;
}
#endif
