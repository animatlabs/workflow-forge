#if !NET48
using OptimaJet.Workflow.Core.Model;
using OptimaJet.Workflow.Core.Model.Builder;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowEngineNet;

/// <summary>
/// Scenario 6: Error Handling - WorkflowEngine.NET
/// Simulates exception handling via compensating transitions (Error → Compensate → Complete).
/// </summary>
public class Scenario6_ErrorHandling_WorkflowEngineNet : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;
    private ProcessDefinition _definition = null!;

    public string Name => "Error Handling";
    public string Description => "Handle exceptions with compensation using WorkflowEngine.NET state machine";

    public Scenario6_ErrorHandling_WorkflowEngineNet(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync()
    {
        var builder = ProcessDefinitionBuilder.Create("S6_ErrorHandling");

        ActivityDefinition start, error, compensate, complete;
        CommandDefinition execute, onError, finish;

        builder.CreateActivity("Start").Initial().Ref(out start);
        builder.CreateActivity("ErrorActivity").Ref(out error);
        builder.CreateActivity("Compensate").Ref(out compensate);
        builder.CreateActivity("Complete").Final().Ref(out complete);

        builder.CreateOrUpdateCommand("Execute").Ref(out execute);
        builder.CreateOrUpdateCommand("OnError").Ref(out onError);
        builder.CreateOrUpdateCommand("Finish").Ref(out finish);

        builder.CreateTransition("To_Error", start, error).TriggeredByCommand(execute);
        builder.CreateTransition("To_Compensate", error, compensate).TriggeredByCommand(onError);
        builder.CreateTransition("To_Complete", compensate, complete).TriggeredByCommand(finish);

        _definition = builder.ProcessDefinition;
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var state = new WorkflowState(_definition);
        var compensated = false;

        await state.ExecuteCommandAsync("Execute");

        try
        {
            throw new InvalidOperationException("Benchmark error");
        }
        catch (InvalidOperationException)
        {
            await state.ExecuteCommandAsync("OnError");
            compensated = true;
        }

        await state.ExecuteCommandAsync("Finish");

        return new ScenarioResult
        {
            Success = compensated && state.IsComplete,
            OperationsExecuted = state.StepsExecuted,
            OutputData = "Error handled with compensation",
            Metadata = { ["FrameworkName"] = "WorkflowEngineNet", ["Mode"] = "StateMachineSimulation", ["SchemeBuiltWith"] = "ProcessDefinitionBuilder" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;
}
#endif
