using WorkflowForge.Abstractions;
using WorkflowForge.Benchmarks.Comparative.Scenarios;
using WorkflowForge.Extensions;

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

        var compensated = (bool)foundry.Properties.GetValueOrDefault("compensated", false);

        return new ScenarioResult
        {
            Success = compensated,
            OperationsExecuted = 1,
            OutputData = "Error handled with compensation",
            Metadata = { ["FrameworkName"] = "WorkflowForge" }
        };
    }

    public Task CleanupAsync() => Task.CompletedTask;

    private class ErrorTestOperation : IWorkflowOperation
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name => "ErrorTest";
        public bool SupportsRestore => true;

        public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Benchmark error");
        }

        public Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            foundry.Properties["compensated"] = true;
            return Task.CompletedTask;
        }

        public void Dispose()
        { }
    }
}