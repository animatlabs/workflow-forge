using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Operations;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates output-to-input chaining between operations.
/// </summary>
public class OutputChainingSample : ISample
{
    public string Name => "Output Chaining";
    public string Description => "Shows operation output becoming the next input";

    public async Task RunAsync()
    {
        Console.WriteLine("Demonstrating output chaining between operations...");

        using var foundry = WorkflowForge.CreateFoundry("OutputChainingDemo");

        foundry
            .WithOperation(new SeedNumberOperation())
            .WithOperation(new MultiplyOperation(3))
            .WithOperation(new FormatResultOperation());

        await foundry.ForgeAsync();
    }

    private sealed class SeedNumberOperation : WorkflowOperationBase
    {
        public override string Name => "SeedNumber";

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            var value = 7;
            Console.WriteLine($"Seed value: {value}");
            return Task.FromResult<object?>(value);
        }
    }

    private sealed class MultiplyOperation : WorkflowOperationBase
    {
        private readonly int _multiplier;

        public MultiplyOperation(int multiplier) => _multiplier = multiplier;

        public override string Name => "Multiply";

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            var value = inputData is int number ? number : 0;
            var result = value * _multiplier;
            Console.WriteLine($"Multiply {value} x {_multiplier} = {result}");
            return Task.FromResult<object?>(result);
        }
    }

    private sealed class FormatResultOperation : WorkflowOperationBase
    {
        public override string Name => "FormatResult";

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            var value = inputData is int number ? number : 0;
            var message = $"Final result: {value}";
            Console.WriteLine(message);
            foundry.Properties["final_message"] = message;
            return Task.FromResult<object?>(message);
        }
    }
}