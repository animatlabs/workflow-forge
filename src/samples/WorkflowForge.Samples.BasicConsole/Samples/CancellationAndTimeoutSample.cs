using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Middleware;
using WorkflowForge.Operations;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates cancellation tokens and operation timeouts.
/// </summary>
public class CancellationAndTimeoutSample : ISample
{
    public string Name => "Cancellation + Timeout";
    public string Description => "Shows timeout middleware and cancellation tokens";

    public async Task RunAsync()
    {
        Console.WriteLine("Demonstrating operation timeouts...");

        using (var foundry = WorkflowForge.CreateFoundry("TimeoutDemo"))
        {
            foundry.AddMiddleware(new OperationTimeoutMiddleware(TimeSpan.FromMilliseconds(100), foundry.Logger));
            foundry.WithOperation(new SlowOperation("SlowOperation", 300));

            try
            {
                await foundry.ForgeAsync();
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine($"Timeout triggered as expected: {ex.Message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Demonstrating cancellation tokens...");

        using (var foundry = WorkflowForge.CreateFoundry("CancellationDemo"))
        {
            foundry.WithOperation(new SlowOperation("CancelableOperation", 500));
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

            try
            {
                await foundry.ForgeAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Operation cancelled as expected.");
            }
        }
    }

    private sealed class SlowOperation : WorkflowOperationBase
    {
        private readonly int _delayMs;

        public SlowOperation(string name, int delayMs)
        {
            Name = name;
            _delayMs = delayMs;
        }

        public override string Name { get; }

        protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            foundry.Logger.LogInformation($"Starting {Name} with {_delayMs}ms delay");
            await Task.Delay(_delayMs, cancellationToken);
            foundry.Logger.LogInformation($"{Name} completed");
            return inputData;
        }
    }
}