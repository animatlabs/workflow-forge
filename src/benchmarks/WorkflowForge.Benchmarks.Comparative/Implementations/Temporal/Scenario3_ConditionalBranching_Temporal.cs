#if !NET48
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.Temporal;

public class Scenario3_ConditionalBranching_Temporal : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Conditional Branching Workflow";
    public string Description => $"Execute {_parameters.OperationCount} conditional branches using Temporal";

    public Scenario3_ConditionalBranching_Temporal(ScenarioParameters parameters) => _parameters = parameters;

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var trueCount = 0;
        var falseCount = 0;
        for (var i = 0; i < _parameters.OperationCount; i++)
        {
            if (i % 2 == 0)
            {
                await SimulateActivityAsync("BranchTrue");
                trueCount++;
            }
            else
            {
                await SimulateActivityAsync("BranchFalse");
                falseCount++;
            }
        }
        return new ScenarioResult
        {
            Success = true,
            OperationsExecuted = _parameters.OperationCount,
            OutputData = $"TrueBranch={trueCount}, FalseBranch={falseCount}",
            Metadata = { ["FrameworkName"] = "Temporal", ["Mode"] = "Simulated" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private static Task SimulateActivityAsync(string branch) { _ = branch.Length; return Task.CompletedTask; }
}
#endif
