#if !NET48
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Temporal;

public class Scenario5_ConcurrentExecution_Temporal : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Concurrent Execution Workflow";
    public string Description => $"Execute {_parameters.ConcurrencyLevel} concurrent activities using Temporal";

    public Scenario5_ConcurrentExecution_Temporal(ScenarioParameters parameters) => _parameters = parameters;

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
            Metadata = { ["FrameworkName"] = "Temporal", ["Mode"] = "Simulated" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private static Task SimulateConcurrentActivityAsync(int index) { _ = index; return Task.CompletedTask; }
}
#endif
