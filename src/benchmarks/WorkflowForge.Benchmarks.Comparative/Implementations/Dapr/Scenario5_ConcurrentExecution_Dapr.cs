#if !NET48
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Dapr;

public class Scenario5_ConcurrentExecution_Dapr : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Concurrent Execution Workflow";
    public string Description => $"Execute {_parameters.ConcurrencyLevel} concurrent activities using Dapr Workflow";

    public Scenario5_ConcurrentExecution_Dapr(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var tasks = Enumerable.Range(0, _parameters.ConcurrencyLevel)
            .Select(i => SimulateConcurrentActivityAsync(i))
            .ToArray();
        await Task.WhenAll(tasks);
        return new ScenarioResult
        {
            Success = true,
            OperationsExecuted = _parameters.ConcurrencyLevel,
            OutputData = $"Completed {_parameters.ConcurrencyLevel} concurrent activities",
            Metadata = { ["FrameworkName"] = "Dapr", ["Mode"] = "Simulated" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private static Task SimulateConcurrentActivityAsync(int index) { _ = index; return Task.CompletedTask; }
}
#endif
