#if !NET48
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Dapr;

public class Scenario12_EventDriven_Dapr : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Event-Driven Workflow";
    public string Description => "Execute event-driven workflow pattern using Dapr Workflow";

    public Scenario12_EventDriven_Dapr(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var tcs = new TaskCompletionSource<string>();

        var workflowTask = SimulateWaitForEventAsync(tcs.Task);
        await SimulateRaiseEventAsync(tcs, "workflow-event");

        var result = await workflowTask;
        return new ScenarioResult
        {
            Success = true,
            OperationsExecuted = 1,
            OutputData = result,
            Metadata = { ["FrameworkName"] = "Dapr", ["Mode"] = "Simulated" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private static async Task<string> SimulateWaitForEventAsync(Task<string> eventTask)
        => await eventTask;

    private static Task SimulateRaiseEventAsync(TaskCompletionSource<string> tcs, string eventName)
    {
        tcs.SetResult($"Event '{eventName}' raised and processed");
        return Task.CompletedTask;
    }
}
#endif
