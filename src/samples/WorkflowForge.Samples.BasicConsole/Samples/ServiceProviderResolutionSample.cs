using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Abstractions;
using WorkflowForge.Loggers;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates resolving services inside operations via foundry.ServiceProvider.
/// </summary>
public class ServiceProviderResolutionSample : ISample
{
    public string Name => "Service Provider Resolution";
    public string Description => "Resolve services inside operations via foundry.ServiceProvider";

    public async Task RunAsync()
    {
        Console.WriteLine("Demonstrating service resolution inside operations...");

        var services = new ServiceCollection();
        services.AddSingleton<IWorkflowForgeLogger>(_ => new ConsoleLogger("WF-Services"));
        services.AddSingleton<IPriceCalculator, PriceCalculator>();
        using var provider = services.BuildServiceProvider();

        var smith = WorkflowForge.CreateSmith(provider.GetRequiredService<IWorkflowForgeLogger>(), provider);

        var workflow = WorkflowForge.CreateWorkflow("ServiceProviderDemo")
            .AddOperation(new CalculateTotalOperation())
            .Build();

        await smith.ForgeAsync(workflow);
    }

    private interface IPriceCalculator
    {
        decimal CalculateTotal(decimal subtotal, decimal taxRate);
    }

    private sealed class PriceCalculator : IPriceCalculator
    {
        public decimal CalculateTotal(decimal subtotal, decimal taxRate) => subtotal + (subtotal * taxRate);
    }

    private sealed class CalculateTotalOperation : IWorkflowOperation
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name => "CalculateTotal";
        public bool SupportsRestore => false;

        public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            var calculator = foundry.ServiceProvider?.GetRequiredService<IPriceCalculator>()
                ?? throw new InvalidOperationException("IPriceCalculator not registered.");

            var subtotal = 120m;
            var total = calculator.CalculateTotal(subtotal, 0.08m);
            Console.WriteLine($"Subtotal: {subtotal}, Total with tax: {total}");
            return Task.FromResult<object?>(total);
        }

        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public void Dispose()
        { }
    }
}
