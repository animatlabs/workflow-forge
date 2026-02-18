using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Operations;
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

    private sealed class SuccessOperation : WorkflowOperationBase
    {
        private readonly string _label;

        public SuccessOperation(string label) => _label = label;

        public override string Name => $"Success:{_label}";

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            foundry.Logger.LogInformation($"{Name} executed");
            if (_label == "Final")
            {
                foundry.SetProperty("final.ran", true);
            }
            return Task.FromResult(inputData);
        }
    }

    private sealed class FailingOperation : WorkflowOperationBase
    {
        private readonly string _label;

        public FailingOperation(string label) => _label = label;

        public override string Name => $"Fail:{_label}";

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException($"Simulated failure at {_label}");
        }
    }
}