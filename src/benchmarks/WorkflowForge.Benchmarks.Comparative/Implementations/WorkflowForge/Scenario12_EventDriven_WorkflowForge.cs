using WorkflowForge.Benchmarks.Comparative.Scenarios;
using WorkflowForge.Extensions;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;

public class Scenario12_EventDriven_WorkflowForge : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Event-Driven";
    public string Description => "Wait for external event then continue";

    public Scenario12_EventDriven_WorkflowForge(ScenarioParameters parameters)
    {
        _parameters = parameters;
    }

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        using var foundry = global::WorkflowForge.WorkflowForge.CreateFoundry("EventDriven");
        using var gate = new ManualResetEventSlim(false);

        _ = Task.Run(() =>
        {
            Thread.Sleep(_parameters.DelayMilliseconds);
            gate.Set();
        });

        foundry.Properties["event_received"] = false;
        foundry.Properties["handled"] = false;

        foundry.WithOperation("WaitForEvent", foundry =>
        {
            var signaled = gate.Wait(TimeSpan.FromSeconds(1));
            foundry.Properties["event_received"] = signaled;
            return Task.CompletedTask;
        });

        foundry.WithOperation("HandleEvent", foundry =>
        {
            var received = foundry.Properties.TryGetValue("event_received", out var receivedValue) && receivedValue is bool receivedFlag && receivedFlag;
            if (received)
            {
                foundry.Properties["handled"] = true;
            }
            return Task.CompletedTask;
        });

        await foundry.ForgeAsync();

        var handled = foundry.Properties.TryGetValue("handled", out var handledValue) && handledValue is bool handledFlag && handledFlag;
        return new ScenarioResult
        {
            Success = handled,
            OperationsExecuted = handled ? 2 : 1,
            OutputData = handled ? "Event handled" : "Event timed out",
            Metadata = { ["FrameworkName"] = "WorkflowForge" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;
}