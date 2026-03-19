#if !NET48
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Temporal;

public class Scenario8_CompleteLifecycle_Temporal : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Complete Lifecycle Workflow";
    public string Description => "Measure complete workflow lifecycle (setup, run, cleanup) using Temporal";

    public Scenario8_CompleteLifecycle_Temporal(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var registry = new Dictionary<string, Func<Task>> { ["MainWorkflow"] = () => SimulateWorkflowBodyAsync() };
        _ = registry.Count;

        await SimulateWorkflowStartAsync("lifecycle-wf");
        await SimulateWorkflowBodyAsync();
        await SimulateWorkflowCompleteAsync("lifecycle-wf");

        return new ScenarioResult
        {
            Success = true,
            OperationsExecuted = 1,
            OutputData = "Complete lifecycle executed",
            Metadata = { ["FrameworkName"] = "Temporal", ["Mode"] = "Simulated" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private static Task SimulateWorkflowStartAsync(string workflowId) { _ = workflowId.Length; return Task.CompletedTask; }
    private static async Task SimulateWorkflowBodyAsync() { await Task.CompletedTask; }
    private static Task SimulateWorkflowCompleteAsync(string workflowId) { _ = workflowId.Length; return Task.CompletedTask; }
}
#endif
