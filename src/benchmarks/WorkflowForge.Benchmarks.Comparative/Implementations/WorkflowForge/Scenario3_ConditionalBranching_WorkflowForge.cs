using WorkflowForge.Benchmarks.Comparative.Scenarios;
using WorkflowForge.Extensions;
using WorkflowForge.Operations;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;

/// <summary>
/// Scenario 3: Conditional Branching - WorkflowForge Implementation
/// Tests: If-then-else logic performance with 50/50 split
/// </summary>
public class Scenario3_ConditionalBranching_WorkflowForge : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Conditional Branching";
    public string Description => $"Execute {_parameters.OperationCount} conditional operations (50/50 true/false)";

    public Scenario3_ConditionalBranching_WorkflowForge(ScenarioParameters parameters)
    {
        _parameters = parameters;
    }

    public Task SetupAsync()
    {
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        using var foundry = global::WorkflowForge.WorkflowForge.CreateFoundry("ConditionalBranching");

        foundry.Properties["true_count"] = 0;
        foundry.Properties["false_count"] = 0;

        // Add conditional operations with alternating true/false
        for (int i = 0; i < _parameters.OperationCount; i++)
        {
            var operationIndex = i;
            var shouldBeTrue = i % 2 == 0;

            var thenOperation = new DelegateWorkflowOperation($"Then_{operationIndex}", async (input, foundry, token) =>
            {
                await Task.Yield();
                var count = foundry.Properties.TryGetValue("true_count", out var trueValue) && trueValue is int trueCount
                    ? trueCount
                    : 0;
                foundry.Properties["true_count"] = count + 1;
                return "Then executed";
            });

            var elseOperation = new DelegateWorkflowOperation($"Else_{operationIndex}", async (input, foundry, token) =>
            {
                await Task.Yield();
                var count = foundry.Properties.TryGetValue("false_count", out var falseValue) && falseValue is int falseCount
                    ? falseCount
                    : 0;
                foundry.Properties["false_count"] = count + 1;
                return "Else executed";
            });

            var conditionalOp = new ConditionalWorkflowOperation(
                (input, foundry, token) => Task.FromResult(shouldBeTrue),
                thenOperation,
                elseOperation
            );

            foundry.WithOperation(conditionalOp);
        }

        await foundry.ForgeAsync();

        var trueCount = foundry.Properties.TryGetValue("true_count", out var trueFinalValue) && trueFinalValue is int finalTrueCount
            ? finalTrueCount
            : 0;
        var falseCount = foundry.Properties.TryGetValue("false_count", out var falseFinalValue) && falseFinalValue is int finalFalseCount
            ? finalFalseCount
            : 0;
        var success = (trueCount + falseCount) == _parameters.OperationCount;

        return new ScenarioResult
        {
            Success = success,
            OperationsExecuted = _parameters.OperationCount,
            OutputData = $"True: {trueCount}, False: {falseCount}",
            Metadata = { ["FrameworkName"] = "WorkflowForge" }
        };
    }

    public Task CleanupAsync()
    {
        return Task.CompletedTask;
    }
}