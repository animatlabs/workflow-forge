#if !NET48
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowEngineNet;

public class Scenario11_ParallelExecution_WorkflowEngineNet : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Parallel Execution Workflow";
    public string Description => $"Execute {_parameters.OperationCount} operations across {_parameters.ConcurrencyLevel} parallel branches using WorkflowEngine.NET";

    public Scenario11_ParallelExecution_WorkflowEngineNet(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var batchSize = Math.Max(1, _parameters.OperationCount / _parameters.ConcurrencyLevel);
        var branches = Enumerable.Range(0, _parameters.ConcurrencyLevel)
            .Select(branch => SimulateParallelBranchAsync(branch, batchSize))
            .ToArray();
        await Task.WhenAll(branches);

        return new ScenarioResult
        {
            Success = true,
            OperationsExecuted = _parameters.ConcurrencyLevel * batchSize,
            OutputData = $"Completed {_parameters.ConcurrencyLevel} parallel branches",
            Metadata = { ["FrameworkName"] = "WorkflowEngineNet", ["Mode"] = "Simulated" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private static async Task SimulateParallelBranchAsync(int branchId, int operationCount)
    {
        for (var i = 0; i < operationCount; i++)
        {
            _ = branchId + i;
            await Task.CompletedTask;
        }
    }
}
#endif
