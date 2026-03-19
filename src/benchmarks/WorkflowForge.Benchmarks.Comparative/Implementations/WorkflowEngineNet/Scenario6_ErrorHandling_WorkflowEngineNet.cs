#if !NET48
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowEngineNet;

public class Scenario6_ErrorHandling_WorkflowEngineNet : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Error Handling Workflow";
    public string Description => "Execute workflow with error handling and rollback using WorkflowEngine.NET";

    public Scenario6_ErrorHandling_WorkflowEngineNet(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var operationsExecuted = 0;
        for (var i = 0; i < _parameters.OperationCount; i++)
        {
            try
            {
                await SimulateCommandWithPossibleErrorAsync(i);
                operationsExecuted++;
            }
            catch (InvalidOperationException)
            {
                await SimulateRollbackCommandAsync(i);
                operationsExecuted++;
            }
        }
        return new ScenarioResult
        {
            Success = true,
            OperationsExecuted = operationsExecuted,
            OutputData = $"Completed {operationsExecuted} operations with error handling",
            Metadata = { ["FrameworkName"] = "WorkflowEngineNet", ["Mode"] = "Simulated" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private static Task SimulateCommandWithPossibleErrorAsync(int step)
    {
        if (step % 5 == 4) throw new InvalidOperationException("Simulated command error");
        return Task.CompletedTask;
    }

    private static Task SimulateRollbackCommandAsync(int step) { _ = step; return Task.CompletedTask; }
}
#endif
