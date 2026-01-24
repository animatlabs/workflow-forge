using System.Threading;
using WorkflowForge.Benchmarks.Comparative.Scenarios;
using WorkflowForge.Extensions;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;

public class Scenario10_LongRunning_WorkflowForge : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Long Running";
    public string Description => $"Execute {_parameters.OperationCount} delayed operations";

    public Scenario10_LongRunning_WorkflowForge(ScenarioParameters parameters)
    {
        _parameters = parameters;
    }

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        using var foundry = global::WorkflowForge.WorkflowForge.CreateFoundry("LongRunning");
        foundry.Properties["completed"] = 0;

        for (int i = 0; i < _parameters.OperationCount; i++)
        {
            foundry.WithOperation($"Delay_{i}", foundry =>
            {
                Thread.Sleep(_parameters.DelayMilliseconds);
                var completed = foundry.Properties.TryGetValue("completed", out var currentValue) && currentValue is int currentCount
                    ? currentCount
                    : 0;
                foundry.Properties["completed"] = completed + 1;
                return Task.CompletedTask;
            });
        }

        await foundry.ForgeAsync();

        var executed = foundry.Properties.TryGetValue("completed", out var completedValue) && completedValue is int executedCount
            ? executedCount
            : 0;
        return new ScenarioResult
        {
            Success = executed == _parameters.OperationCount,
            OperationsExecuted = executed,
            OutputData = $"Completed {executed} delayed operations",
            Metadata = { ["FrameworkName"] = "WorkflowForge" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;
}
