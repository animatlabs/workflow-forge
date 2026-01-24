using System.Collections.Concurrent;
using System.Collections.Generic;
using WorkflowForge.Abstractions;
using WorkflowForge.Benchmarks.Comparative.Scenarios;
using WorkflowForge.Extensions;
using WorkflowForge.Operations;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;

public class Scenario11_ParallelExecution_WorkflowForge : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Parallel Execution";
    public string Description => $"Execute {_parameters.OperationCount} parallel branches";

    public Scenario11_ParallelExecution_WorkflowForge(ScenarioParameters parameters)
    {
        _parameters = parameters;
    }

    public Task SetupAsync() => Task.CompletedTask;

    public async Task<ScenarioResult> ExecuteAsync()
    {
        var results = new ConcurrentBag<int>();
        using var foundry = global::WorkflowForge.WorkflowForge.CreateFoundry("ParallelExecution");

        var operations = new List<IWorkflowOperation>();
        for (int i = 0; i < _parameters.OperationCount; i++)
        {
            var branchIndex = i;
            operations.Add(new ActionWorkflowOperation($"Branch_{branchIndex}", (input, foundry, token) =>
            {
                results.Add(branchIndex);
                return Task.CompletedTask;
            }));
        }

        var parallelOperation = ForEachWorkflowOperation.CreateSharedInput(
            operations,
            maxConcurrency: _parameters.ConcurrencyLevel,
            name: "ParallelBranches");

        foundry.AddOperation(parallelOperation);
        await foundry.ForgeAsync();

        return new ScenarioResult
        {
            Success = results.Count == _parameters.OperationCount,
            OperationsExecuted = results.Count,
            OutputData = $"Completed {results.Count} parallel branches",
            Metadata = { ["FrameworkName"] = "WorkflowForge" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;
}
