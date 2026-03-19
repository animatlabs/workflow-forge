#if !NET48
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Dapr;

public class Scenario7_CreationOverhead_Dapr : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Creation Overhead Workflow";
    public string Description => "Measure workflow context creation overhead using Dapr Workflow";

    public Scenario7_CreationOverhead_Dapr(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var created = 0;
        for (var i = 0; i < _parameters.OperationCount; i++)
        {
            var context = CreateWorkflowContext(i);
            await SimulateMinimalActivityAsync(context);
            created++;
        }
        return new ScenarioResult
        {
            Success = true,
            OperationsExecuted = created,
            OutputData = $"Created and executed {created} workflow contexts",
            Metadata = { ["FrameworkName"] = "Dapr", ["Mode"] = "Simulated" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private static Dictionary<string, object> CreateWorkflowContext(int index) =>
        new() { ["InstanceId"] = $"dapr-wf-{index}", ["StartTime"] = DateTime.UtcNow };

    private static Task SimulateMinimalActivityAsync(Dictionary<string, object> context) { _ = context.Count; return Task.CompletedTask; }
}
#endif
