#if !NET48
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Temporal;

public class Scenario4_LoopProcessing_Temporal : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Loop Processing Workflow";
    public string Description => $"Process {_parameters.ItemCount} items using Temporal";

    public Scenario4_LoopProcessing_Temporal(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var processed = 0;
        for (var i = 0; i < _parameters.ItemCount; i++)
        {
            await SimulateProcessItemAsync(i);
            processed++;
        }
        return new ScenarioResult
        {
            Success = true,
            OperationsExecuted = processed,
            OutputData = $"Processed {processed} items",
            Metadata = { ["FrameworkName"] = "Temporal", ["Mode"] = "Simulated" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private static Task SimulateProcessItemAsync(int itemId) { _ = itemId; return Task.CompletedTask; }
}
#endif
