#if !NET48
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Temporal;

public class Scenario11_ParallelExecution_Temporal : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Parallel Execution Workflow";
    public string Description => $"Execute {_parameters.OperationCount} operations across {_parameters.ConcurrencyLevel} parallel branches using Temporal";

    public Scenario11_ParallelExecution_Temporal(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var batchSize = Math.Max(1, _parameters.OperationCount / _parameters.ConcurrencyLevel);
        var branches = Enumerable.Range(0, _parameters.ConcurrencyLevel)
            .Select(branch => SimulateBranchAsync(branch, batchSize))
            .ToArray();
        await Task.WhenAll(branches);

        return new ScenarioResult
        {
            Success = true,
            OperationsExecuted = _parameters.ConcurrencyLevel * batchSize,
            OutputData = $"Completed {_parameters.ConcurrencyLevel} parallel branches",
            Metadata = { ["FrameworkName"] = "Temporal", ["Mode"] = "Simulated" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private static async Task SimulateBranchAsync(int branchId, int operationCount)
    {
        for (var i = 0; i < operationCount; i++)
        {
            _ = branchId + i;
            await Task.CompletedTask;
        }
    }
}
#endif
