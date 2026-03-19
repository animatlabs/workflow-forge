#if !NET48
using System.Collections.Concurrent;
using OptimaJet.Workflow.Core.Model;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowEngineNet;

/// <summary>
/// Scenario 11: Parallel Execution - WorkflowEngine.NET
/// Executes multiple workflow branches concurrently using the parallel scheme.
/// </summary>
public class Scenario11_ParallelExecution_WorkflowEngineNet : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private ProcessDefinition _definition = null!;

    public string Name => "Parallel Execution";
    public string Description => $"Execute {_parameters.OperationCount} parallel workflow branches";

    public Scenario11_ParallelExecution_WorkflowEngineNet(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync()
    {
        _definition = WorkflowEngineNetInfrastructure.BuildLinearScheme("S11_Parallel", _parameters.OperationCount);
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var results = new ConcurrentBag<int>();
        var semaphore = new SemaphoreSlim(_parameters.ConcurrencyLevel);

        var tasks = Enumerable.Range(0, _parameters.OperationCount).Select(async i =>
        {
            await semaphore.WaitAsync();
            try
            {
                var state = new WorkflowState(_definition);
                for (var j = 0; j < _parameters.OperationCount; j++)
                    await state.ExecuteNextCommandAsync();
                await state.ExecuteFinishCommandAsync();
                results.Add(i);
            }
            finally { semaphore.Release(); }
        });

        await Task.WhenAll(tasks);

        return new ScenarioResult
        {
            Success = results.Count == _parameters.OperationCount,
            OperationsExecuted = results.Count,
            OutputData = $"Completed {results.Count} parallel branches",
            Metadata = { ["FrameworkName"] = "WorkflowEngineNet", ["Mode"] = "StateMachineSimulation", ["SchemeBuiltWith"] = "ProcessDefinitionBuilder" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;
}
#endif
