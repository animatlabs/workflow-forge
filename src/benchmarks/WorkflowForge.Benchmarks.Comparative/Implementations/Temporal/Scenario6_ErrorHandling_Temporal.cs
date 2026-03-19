#if !NET48
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Temporal;

public class Scenario6_ErrorHandling_Temporal : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Error Handling Workflow";
    public string Description => "Execute workflow with error handling and compensation using Temporal";

    public Scenario6_ErrorHandling_Temporal(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var operationsExecuted = 0;
        for (var i = 0; i < _parameters.OperationCount; i++)
        {
            try
            {
                await SimulateActivityWithPossibleErrorAsync(i);
                operationsExecuted++;
            }
            catch (InvalidOperationException)
            {
                await SimulateCompensationActivityAsync(i);
                operationsExecuted++;
            }
        }
        return new ScenarioResult
        {
            Success = true,
            OperationsExecuted = operationsExecuted,
            OutputData = $"Completed {operationsExecuted} operations with error handling",
            Metadata = { ["FrameworkName"] = "Temporal", ["Mode"] = "Simulated" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private static Task SimulateActivityWithPossibleErrorAsync(int step)
    {
        if (step % 5 == 4) throw new InvalidOperationException("Simulated transient error");
        return Task.CompletedTask;
    }

    private static Task SimulateCompensationActivityAsync(int step) { _ = step; return Task.CompletedTask; }
}
#endif
