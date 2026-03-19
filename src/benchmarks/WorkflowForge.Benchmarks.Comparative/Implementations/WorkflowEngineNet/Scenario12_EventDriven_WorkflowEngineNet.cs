#if !NET48
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowEngineNet;

public class Scenario12_EventDriven_WorkflowEngineNet : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Event-Driven Workflow";
    public string Description => "Execute event-driven workflow pattern using WorkflowEngine.NET";

    public Scenario12_EventDriven_WorkflowEngineNet(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var tcs = new TaskCompletionSource<string>();

        var workflowTask = SimulateWaitForTimerOrEventAsync(tcs.Task);
        await SimulateTriggerEventAsync(tcs, "process-event");

        var result = await workflowTask;
        return new ScenarioResult
        {
            Success = true,
            OperationsExecuted = 1,
            OutputData = result,
            Metadata = { ["FrameworkName"] = "WorkflowEngineNet", ["Mode"] = "Simulated" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private static async Task<string> SimulateWaitForTimerOrEventAsync(Task<string> eventTask)
        => await eventTask;

    private static Task SimulateTriggerEventAsync(TaskCompletionSource<string> tcs, string eventName)
    {
        tcs.SetResult($"Process event '{eventName}' triggered and handled");
        return Task.CompletedTask;
    }
}
#endif
