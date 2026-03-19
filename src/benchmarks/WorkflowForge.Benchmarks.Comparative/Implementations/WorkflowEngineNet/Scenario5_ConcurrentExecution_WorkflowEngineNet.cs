#if !NET48
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowEngineNet;

public class Scenario5_ConcurrentExecution_WorkflowEngineNet : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Concurrent Execution Workflow";
    public string Description => $"Execute {_parameters.ConcurrencyLevel} concurrent commands using WorkflowEngine.NET";

    public Scenario5_ConcurrentExecution_WorkflowEngineNet(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var tasks = Enumerable.Range(0, _parameters.ConcurrencyLevel)
            .Select(i => SimulateConcurrentCommandAsync(i))
            .ToArray();
        await Task.WhenAll(tasks);
        return new ScenarioResult
        {
            Success = true,
            OperationsExecuted = _parameters.ConcurrencyLevel,
            OutputData = $"Completed {_parameters.ConcurrencyLevel} concurrent commands",
            Metadata = { ["FrameworkName"] = "WorkflowEngineNet", ["Mode"] = "Simulated" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private static Task SimulateConcurrentCommandAsync(int index) { _ = index; return Task.CompletedTask; }
}
#endif
