#if !NET48
using OptimaJet.Workflow.Core.Model;
using OptimaJet.Workflow.Core.Model.Builder;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowEngineNet;

/// <summary>
/// Scenario 12: Event-Driven - WorkflowEngine.NET
/// Simulates an external event (timer/signal) that triggers a workflow state transition.
/// </summary>
public class Scenario12_EventDriven_WorkflowEngineNet : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private ProcessDefinition _definition = null!;

    public string Name => "Event-Driven";
    public string Description => $"Workflow waiting for external event with {_parameters.DelayMilliseconds}ms delay";

    public Scenario12_EventDriven_WorkflowEngineNet(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync()
    {
        var builder = ProcessDefinitionBuilder.Create("S12_EventDriven");
        ActivityDefinition waiting, handling, complete;
        CommandDefinition eventSignal, finish;

        builder.CreateActivity("Waiting").Initial().Ref(out waiting);
        builder.CreateActivity("Handling").Ref(out handling);
        builder.CreateActivity("Complete").Final().Ref(out complete);
        builder.CreateOrUpdateCommand("EventSignal").Ref(out eventSignal);
        builder.CreateOrUpdateCommand("Finish").Ref(out finish);

        builder.CreateTransition("Signal_Received", waiting, handling).TriggeredByCommand(eventSignal);
        builder.CreateTransition("Handle_Complete", handling, complete).TriggeredByCommand(finish);

        _definition = builder.ProcessDefinition;
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var state = new WorkflowState(_definition);
        using var gate = new ManualResetEventSlim(false);

        _ = Task.Run(() =>
        {
            Thread.Sleep(_parameters.DelayMilliseconds);
            gate.Set();
        });

        var signaled = gate.Wait(TimeSpan.FromSeconds(2));
        if (signaled)
        {
            await state.ExecuteCommandAsync("EventSignal");
            await state.ExecuteCommandAsync("Finish");
        }

        return new ScenarioResult
        {
            Success = signaled && state.IsComplete,
            OperationsExecuted = state.StepsExecuted,
            OutputData = signaled ? "Event received and handled" : "Event timed out",
            Metadata = { ["FrameworkName"] = "WorkflowEngineNet", ["Mode"] = "StateMachineSimulation", ["SchemeBuiltWith"] = "ProcessDefinitionBuilder" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;
}
#endif
