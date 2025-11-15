using WorkflowForge.Benchmarks.Comparative.Scenarios;
using WorkflowForge.Extensions;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;

public class Scenario5_ConcurrentExecution_WorkflowForge : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Concurrent Execution";
    public string Description => $"Execute {_parameters.ConcurrencyLevel} concurrent workflows";

    public Scenario5_ConcurrentExecution_WorkflowForge(ScenarioParameters parameters)
    {
        _parameters = parameters;
    }

    public Task SetupAsync()
    {
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var tasks = new List<Task>();
        var completedCount = 0;

        for (int i = 0; i < _parameters.ConcurrencyLevel; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                using var foundry = global::WorkflowForge.WorkflowForge.CreateFoundry($"Concurrent_{i}");

                for (int j = 0; j < 10; j++)
                {
                    foundry.WithOperation($"Op_{j}", async (foundry) =>
                    {
                        await Task.Yield();
                        foundry.Properties[$"op_{j}"] = j;
                    });
                }

                await foundry.ForgeAsync();
                Interlocked.Increment(ref completedCount);
            }));
        }

        await Task.WhenAll(tasks);

        return new ScenarioResult
        {
            Success = completedCount == _parameters.ConcurrencyLevel,
            OperationsExecuted = completedCount * 10,
            OutputData = $"{completedCount} workflows completed",
            Metadata = { ["FrameworkName"] = "WorkflowForge" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;
}