#if !NET48
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowEngineNet;

public class Scenario3_ConditionalBranching_WorkflowEngineNet : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Conditional Branching Workflow";
    public string Description => $"Execute {_parameters.OperationCount} conditional transitions using WorkflowEngine.NET";

    public Scenario3_ConditionalBranching_WorkflowEngineNet(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var pathACount = 0;
        var pathBCount = 0;
        for (var i = 0; i < _parameters.OperationCount; i++)
        {
            if (await SimulateConditionCheckAsync(i))
            {
                await SimulateCommandAsync("PathA");
                pathACount++;
            }
            else
            {
                await SimulateCommandAsync("PathB");
                pathBCount++;
            }
        }
        return new ScenarioResult
        {
            Success = true,
            OperationsExecuted = _parameters.OperationCount,
            OutputData = $"PathA={pathACount}, PathB={pathBCount}",
            Metadata = { ["FrameworkName"] = "WorkflowEngineNet", ["Mode"] = "Simulated" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private static Task<bool> SimulateConditionCheckAsync(int step) => Task.FromResult(step % 2 == 0);
    private static Task SimulateCommandAsync(string path) { _ = path.Length; return Task.CompletedTask; }
}
#endif
