using WorkflowForge.Abstractions;
using WorkflowForge.Options;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates compensation behaviors and error handling strategies.
/// </summary>
public class CompensationBehaviorSample : ISample
{
    public string Name => "Compensation Behaviors";
    public string Description => "Shows compensation success and compensation failure handling";

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

    private sealed class CompensatableOperation : IWorkflowOperation
    {
        private readonly string _label;
        private readonly bool _simulateRestoreFailure;

        public CompensatableOperation(string label, bool simulateRestoreFailure)
        {
            _label = label;
            _simulateRestoreFailure = simulateRestoreFailure;
        }

        public Guid Id { get; } = Guid.NewGuid();
        public string Name => _label;
        public bool SupportsRestore => true;

        public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            foundry.Logger.LogInformation($"{Name} executed");
            return Task.FromResult(inputData);
        }

        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            foundry.Logger.LogInformation($"{Name} restore invoked");
            if (_simulateRestoreFailure && Name == "StepB")
            {
                throw new InvalidOperationException($"{Name} restore failed intentionally");
            }
            return Task.CompletedTask;
        }

        public void Dispose()
        { }
    }

    private sealed class FailingOperation : IWorkflowOperation
    {
        private readonly string _label;

        public FailingOperation(string label) => _label = label;

        public Guid Id { get; } = Guid.NewGuid();
        public string Name => _label;
        public bool SupportsRestore => true;

        public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Primary operation failed to trigger compensation.");
        }

        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public void Dispose()
        { }
    }
}