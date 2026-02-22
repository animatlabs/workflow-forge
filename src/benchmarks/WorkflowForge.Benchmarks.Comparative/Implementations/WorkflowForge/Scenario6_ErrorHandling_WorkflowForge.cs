using WorkflowForge.Abstractions;
using WorkflowForge.Benchmarks.Comparative.Scenarios;
using WorkflowForge.Extensions;
using WorkflowForge.Operations;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;

public class Scenario6_ErrorHandling_WorkflowForge : IWorkflowScenario
{
    private readonly ScenarioParameters _parameters;

    public string Name => "Error Handling";
    public string Description => "Handle exceptions with compensation";

    public Scenario6_ErrorHandling_WorkflowForge(ScenarioParameters parameters)
    {
        _parameters = parameters;
    }

    public Task SetupAsync()
    {
        return Task.CompletedTask;
    }

    public async Task<ScenarioResult> ExecuteAsync()
    {
        using var foundry = global::WorkflowForge.WorkflowForge.CreateFoundry("ErrorHandling");

        foundry.Properties["compensated"] = false;

        var errorOp = new ErrorTestOperation();
        foundry.WithOperation(errorOp);

        try
        {
            await foundry.ForgeAsync();
        }
        catch (InvalidOperationException)
        {
            // Perform compensation
            await errorOp.RestoreAsync("error_context", foundry);
        }

        var compensated = foundry.Properties.TryGetValue("compensated", out var compensatedValue) && compensatedValue is bool compensatedFlag && compensatedFlag;

        return new ScenarioResult
        {
            Success = compensated,
            OperationsExecuted = 1,
            OutputData = "Error handled with compensation",
            Metadata = { ["FrameworkName"] = "WorkflowForge" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private class ErrorTestOperation : WorkflowOperationBase
    {
        public override string Name => "ErrorTest";

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Benchmark error");
        }

        public override Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            foundry.Properties["compensated"] = true;
            return Task.CompletedTask;
        }
    }
}