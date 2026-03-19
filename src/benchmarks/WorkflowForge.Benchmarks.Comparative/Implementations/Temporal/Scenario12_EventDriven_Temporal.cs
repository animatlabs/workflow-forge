#if !NET48
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Temporal;

public class Scenario12_EventDriven_Temporal : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Event-Driven Workflow";
    public string Description => "Execute event-driven workflow pattern using Temporal";

    public Scenario12_EventDriven_Temporal(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var tcs = new TaskCompletionSource<string>();

        var workflowTask = SimulateWaitForSignalAsync(tcs.Task);

        await SimulateSendSignalAsync(tcs, "ExternalEvent");

        var result = await workflowTask;
        return new ScenarioResult
        {
            Success = true,
            OperationsExecuted = 1,
            OutputData = result,
            Metadata = { ["FrameworkName"] = "Temporal", ["Mode"] = "Simulated" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private static async Task<string> SimulateWaitForSignalAsync(Task<string> signalTask)
        => await signalTask;

    private static Task SimulateSendSignalAsync(TaskCompletionSource<string> tcs, string signalName)
    {
        tcs.SetResult($"Signal '{signalName}' received and processed");
        return Task.CompletedTask;
    }
}
#endif
