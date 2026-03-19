#if !NET48
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Dapr;

public class Scenario2_DataPassing_Dapr : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Data Passing Workflow";
    public string Description => $"Pass data through {_parameters.OperationCount} activities using Dapr Workflow";

    public Scenario2_DataPassing_Dapr(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var data = "initial";
        for (var i = 0; i < _parameters.OperationCount; i++)
        {
            data = await SimulateTransformActivityAsync(data, i);
        }
        return new ScenarioResult
        {
            Success = true,
            OperationsExecuted = _parameters.OperationCount,
            OutputData = data,
            Metadata = { ["FrameworkName"] = "Dapr", ["Mode"] = "Simulated" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private static Task<string> SimulateTransformActivityAsync(string input, int step)
        => Task.FromResult($"{input}|step{step}");
}
#endif
