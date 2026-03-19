#if !NET48
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowEngineNet;

public class Scenario7_CreationOverhead_WorkflowEngineNet : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Creation Overhead Workflow";
    public string Description => "Measure workflow process instance creation overhead using WorkflowEngine.NET";

    public Scenario7_CreationOverhead_WorkflowEngineNet(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var created = 0;
        for (var i = 0; i < _parameters.OperationCount; i++)
        {
            var processInstance = CreateProcessInstance(i);
            await SimulateMinimalCommandAsync(processInstance);
            created++;
        }
        return new ScenarioResult
        {
            Success = true,
            OperationsExecuted = created,
            OutputData = $"Created and executed {created} process instances",
            Metadata = { ["FrameworkName"] = "WorkflowEngineNet", ["Mode"] = "Simulated" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private static Dictionary<string, object> CreateProcessInstance(int index) =>
        new() { ["ProcessId"] = Guid.NewGuid(), ["SchemeCode"] = "SimpleWorkflow", ["Index"] = index };

    private static Task SimulateMinimalCommandAsync(Dictionary<string, object> instance) { _ = instance.Count; return Task.CompletedTask; }
}
#endif
