#if !NET48
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowEngineNet;

public class Scenario2_DataPassing_WorkflowEngineNet : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Data Passing Workflow";
    public string Description => $"Pass data through {_parameters.OperationCount} commands using WorkflowEngine.NET";

    public Scenario2_DataPassing_WorkflowEngineNet(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var parameters = new Dictionary<string, object> { ["Input"] = "start" };
        for (var i = 0; i < _parameters.OperationCount; i++)
        {
            parameters = await SimulateCommandWithDataAsync(parameters, i);
        }
        return new ScenarioResult
        {
            Success = true,
            OperationsExecuted = _parameters.OperationCount,
            OutputData = parameters["Input"]?.ToString(),
            Metadata = { ["FrameworkName"] = "WorkflowEngineNet", ["Mode"] = "Simulated" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private static Task<Dictionary<string, object>> SimulateCommandWithDataAsync(Dictionary<string, object> parameters, int step)
    {
        var result = new Dictionary<string, object>(parameters) { ["Input"] = $"{parameters["Input"]}->cmd{step}" };
        return Task.FromResult(result);
    }
}
#endif
