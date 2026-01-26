using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Options;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates ContinueOnError behavior and aggregate exception handling.
/// </summary>
public class ContinueOnErrorSample : ISample
{
    public string Name => "Continue On Error";
    public string Description => "Shows ContinueOnError with aggregated failures";

    public async Task RunAsync()
    {
        Console.WriteLine("Demonstrating ContinueOnError behavior...");

        var options = new WorkflowForgeOptions { ContinueOnError = true };
        using var foundry = WorkflowForge.CreateFoundry("ContinueOnErrorDemo", options: options);

        foundry
            .WithOperation(new SuccessOperation("First"))
            .WithOperation(new FailingOperation("FailurePoint"))
            .WithOperation(new SuccessOperation("Final"));

        try
        {
            await foundry.ForgeAsync();
        }
        catch (AggregateException ex)
        {
            Console.WriteLine($"AggregateException captured with {ex.InnerExceptions.Count} error(s).");
        }

        Console.WriteLine($"Final operation executed: {foundry.GetPropertyOrDefault("final.ran", false)}");
    }

    private sealed class SuccessOperation : IWorkflowOperation
    {
        private readonly string _label;

        public SuccessOperation(string label) => _label = label;

        public Guid Id { get; } = Guid.NewGuid();
        public string Name => $"Success:{_label}";
        public bool SupportsRestore => false;

        public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            foundry.Logger.LogInformation($"{Name} executed");
            if (_label == "Final")
            {
                foundry.SetProperty("final.ran", true);
            }
            return Task.FromResult(inputData);
        }

        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public void Dispose()
        { }
    }

    private sealed class FailingOperation : IWorkflowOperation
    {
        private readonly string _label;

        public FailingOperation(string label) => _label = label;

        public Guid Id { get; } = Guid.NewGuid();
        public string Name => $"Fail:{_label}";
        public bool SupportsRestore => false;

        public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException($"Simulated failure at {_label}");
        }

        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public void Dispose()
        { }
    }
}