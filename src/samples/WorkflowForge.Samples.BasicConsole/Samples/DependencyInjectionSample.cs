using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.DependencyInjection;
using WorkflowForge.Loggers;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates configuring WorkflowForge with dependency injection
/// and running workflows via IWorkflowSmith resolved from DI.
/// </summary>
public class DependencyInjectionSample : ISample
{
    public string Name => "Dependency Injection";
    public string Description => "Configure WorkflowForge via DI and run using IWorkflowSmith";

    public async Task RunAsync()
    {
        Console.WriteLine("Demonstrating WorkflowForge dependency injection setup...");

        var services = new ServiceCollection();
        var configuration = Program.Configuration;
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IWorkflowForgeLogger>(_ => new ConsoleLogger("WF-DI"));

        services.AddWorkflowForge(configuration);
        services.AddWorkflowSmith();

        services.AddSingleton<IOrderIdGenerator, OrderIdGenerator>();

        using var provider = services.BuildServiceProvider();
        var smith = provider.GetRequiredService<IWorkflowSmith>();

        var workflow = WorkflowForge.CreateWorkflow("DiConfiguredWorkflow")
            .AddOperation(new GenerateOrderIdOperation())
            .AddOperation(new ProcessOrderOperation())
            .Build();

        await smith.ForgeAsync(workflow);

        Console.WriteLine("DI workflow completed.");
    }

    private interface IOrderIdGenerator
    {
        string Create();
    }

    private sealed class OrderIdGenerator : IOrderIdGenerator
    {
        public string Create() => $"ORD-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }

    private sealed class GenerateOrderIdOperation : IWorkflowOperation
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name => "GenerateOrderId";
        public bool SupportsRestore => false;

        public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            var generator = foundry.ServiceProvider?.GetRequiredService<IOrderIdGenerator>()
                ?? throw new InvalidOperationException("IOrderIdGenerator not registered.");

            var orderId = generator.Create();
            foundry.Properties["order_id"] = orderId;
            Console.WriteLine($"Generated order id: {orderId}");
            return Task.FromResult<object?>(orderId);
        }

        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public void Dispose()
        { }
    }

    private sealed class ProcessOrderOperation : IWorkflowOperation
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name => "ProcessOrder";
        public bool SupportsRestore => false;

        public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            var orderId = inputData as string ?? foundry.Properties["order_id"]?.ToString() ?? "unknown";
            Console.WriteLine($"Processing order {orderId} via DI-configured smith.");
            return Task.FromResult(inputData);
        }

        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public void Dispose()
        { }
    }
}