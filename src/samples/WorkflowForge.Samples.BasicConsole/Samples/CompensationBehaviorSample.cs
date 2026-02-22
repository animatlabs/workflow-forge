using WorkflowForge.Abstractions;
using WorkflowForge.Operations;
using WorkflowForge.Options;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates compensation behaviors and error handling strategies.
/// All operations participate in compensation automatically: those with restore logic
/// perform rollback; others return Task.CompletedTask (no-op). This sample shows a
/// mixed workflow where some operations have actual restore logic and some do not.
/// </summary>
public class CompensationBehaviorSample : ISample
{
    public string Name => "Compensation Behaviors";
    public string Description => "Shows mixed-workflow compensation: all operations participate; some restore, some no-op";

    public async Task RunAsync()
    {
        Console.WriteLine("Scenario 1: Compensation succeeds.");
        await RunScenario(simulateRestoreFailure: false, throwOnCompensationError: false);

        Console.WriteLine();
        Console.WriteLine("Scenario 2: Compensation fails and throws.");
        try
        {
            await RunScenario(simulateRestoreFailure: true, throwOnCompensationError: true);
        }
        catch (AggregateException ex)
        {
            Console.WriteLine($"Compensation errors bubbled up: {ex.InnerExceptions.Count} error(s).");
        }
    }

    private static async Task RunScenario(bool simulateRestoreFailure, bool throwOnCompensationError)
    {
        var options = new WorkflowForgeOptions
        {
            FailFastCompensation = true,
            ThrowOnCompensationError = throwOnCompensationError
        };

        using var foundry = WorkflowForge.CreateFoundry("CompensationDemo", options: options);

        var workflow = WorkflowForge.CreateWorkflow("CompensationWorkflow")
            .AddOperation(new CompensatableOperation("StepA", simulateRestoreFailure))
            .AddOperation(new CompensatableOperation("StepB", simulateRestoreFailure))
            .AddOperation(new NoOpRestoreOperation("StepC")) // No restore logic - returns Task.CompletedTask
            .AddOperation(new FailingOperation("FailurePoint"))
            .Build();

        var smith = WorkflowForge.CreateSmith();

        try
        {
            await smith.ForgeAsync(workflow, foundry);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Workflow failed as expected: {ex.GetType().Name} - {ex.Message}");
        }
    }

    private sealed class CompensatableOperation : WorkflowOperationBase
    {
        private readonly string _label;
        private readonly bool _simulateRestoreFailure;

        public CompensatableOperation(string label, bool simulateRestoreFailure)
        {
            _label = label;
            _simulateRestoreFailure = simulateRestoreFailure;
        }

        public override string Name => _label;

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            foundry.Logger.LogInformation("{OperationName} executed", Name);
            return Task.FromResult(inputData);
        }

        public override Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            foundry.Logger.LogInformation("{OperationName} restore invoked", Name);
            if (_simulateRestoreFailure && Name == "StepB")
            {
                throw new InvalidOperationException($"{Name} restore failed intentionally");
            }
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Operation with no restore logic - RestoreAsync returns Task.CompletedTask.
    /// All operations participate in compensation; no-op operations are safely skipped.
    /// </summary>
    private sealed class NoOpRestoreOperation : WorkflowOperationBase
    {
        private readonly string _label;

        public NoOpRestoreOperation(string label) => _label = label;

        public override string Name => _label;

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            foundry.Logger.LogInformation("{OperationName} executed (no restore logic)", Name);
            return Task.FromResult(inputData);
        }
    }

    private sealed class FailingOperation : WorkflowOperationBase
    {
        private readonly string _label;

        public FailingOperation(string label) => _label = label;

        public override string Name => _label;

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Primary operation failed to trigger compensation.");
        }
    }
}