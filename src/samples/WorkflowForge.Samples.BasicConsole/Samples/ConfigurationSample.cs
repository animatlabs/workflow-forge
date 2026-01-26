using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Audit;
using WorkflowForge.Extensions.Audit.Options;
using WorkflowForge.Extensions.Persistence;
using WorkflowForge.Extensions.Persistence.Abstractions;
using WorkflowForge.Extensions.Persistence.Recovery;
using WorkflowForge.Extensions.Persistence.Recovery.Options;
using WorkflowForge.Extensions.Resilience.Polly;
using WorkflowForge.Extensions.Resilience.Polly.Options;
using WorkflowForge.Extensions.Validation;
using WorkflowForge.Extensions.Validation.Options;
using WF = WorkflowForge;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates comprehensive configuration-driven workflow setup using appsettings.json.
/// Shows how to enable/disable extensions and configure them via configuration files.
/// </summary>
public class ConfigurationSample : ISample
{
    public string Name => "Configuration-Driven Workflows";
    public string Description => "Demonstrates enabling/disabling extensions via appsettings.json";

    public async Task RunAsync()
    {
        Console.WriteLine("Configuration-Driven Workflow Sample");
        Console.WriteLine("=====================================");
        Console.WriteLine();
        Console.WriteLine("This sample demonstrates:");
        Console.WriteLine("1. Loading configuration from appsettings.json");
        Console.WriteLine("2. Enabling/disabling extensions via configuration");
        Console.WriteLine("3. Using IOptions pattern for type-safe configuration");
        Console.WriteLine("4. Configuration validation on startup");
        Console.WriteLine();

        // Setup DI container with configuration
        var services = new ServiceCollection();
        var configuration = Program.Configuration;
        services.AddSingleton<IConfiguration>(configuration);

        // Register all extension configurations
        services.AddAuditConfiguration(configuration);
        services.AddValidationConfiguration(configuration);
        services.AddPersistenceConfiguration(configuration);
        services.AddRecoveryConfiguration(configuration);
        services.AddWorkflowForgePolly(configuration);

        var serviceProvider = services.BuildServiceProvider();

        // Demonstrate configuration-driven workflow execution
        await DemonstrateEnabledExtensions(serviceProvider);
        await DemonstrateDisabledExtensions(serviceProvider);
        await DemonstrateConfigurationValidation(serviceProvider);
    }

    private static async Task DemonstrateEnabledExtensions(IServiceProvider serviceProvider)
    {
        Console.WriteLine("1. Enabled Extensions Demo");
        Console.WriteLine("   -----------------------");

        // Get configuration for enabled extensions
        var validationOptions = serviceProvider.GetRequiredService<IOptions<ValidationMiddlewareOptions>>().Value;
        var auditOptions = serviceProvider.GetRequiredService<IOptions<AuditMiddlewareOptions>>().Value;
        var pollyOptions = serviceProvider.GetRequiredService<IOptions<PollyMiddlewareOptions>>().Value;

        Console.WriteLine($"   Validation: {(validationOptions.Enabled ? "✅ Enabled" : "❌ Disabled")}");
        Console.WriteLine($"   Audit: {(auditOptions.Enabled ? "✅ Enabled" : "❌ Disabled")}");
        Console.WriteLine($"   Polly: {(pollyOptions.Enabled ? "✅ Enabled" : "❌ Disabled")}");
        Console.WriteLine();

        // Create workflow with enabled extensions
        using var foundry = WF.WorkflowForge.CreateFoundry("ConfigDemo");

        if (validationOptions.Enabled)
        {
            // Validation would be added here if we had a validator
            Console.WriteLine("   ✓ Validation middleware would be added");
        }

        if (auditOptions.Enabled)
        {
            // Audit would be added here if we had an audit provider
            Console.WriteLine("   ✓ Audit middleware would be added");
        }

        if (pollyOptions.Enabled)
        {
            // Polly would be added here
            Console.WriteLine("   ✓ Polly resilience middleware would be added");
        }

        var workflow = WF.WorkflowForge.CreateWorkflow("ConfigWorkflow")
            .AddOperation(new ConfigLogOperation("Op1", "Operation executed"))
            .Build();

        var smith = WF.WorkflowForge.CreateSmith();
        await smith.ForgeAsync(workflow, foundry);

        Console.WriteLine("   ✓ Workflow executed successfully");
        Console.WriteLine();
    }

    private static async Task DemonstrateDisabledExtensions(IServiceProvider serviceProvider)
    {
        Console.WriteLine("2. Disabled Extensions Demo");
        Console.WriteLine("   ------------------------");

        var persistenceOptions = serviceProvider.GetRequiredService<IOptions<PersistenceOptions>>().Value;
        var recoveryOptions = serviceProvider.GetRequiredService<IOptions<RecoveryMiddlewareOptions>>().Value;

        Console.WriteLine($"   Persistence: {(persistenceOptions.Enabled ? "✅ Enabled" : "❌ Disabled")}");
        Console.WriteLine($"   Recovery: {(recoveryOptions.Enabled ? "✅ Enabled" : "❌ Disabled")}");
        Console.WriteLine();

        using var foundry = WF.WorkflowForge.CreateFoundry("DisabledDemo");

        // These extensions check Enabled flag internally and skip registration if disabled
        var mockProvider = new InMemoryPersistenceProvider();
        foundry.UsePersistence(mockProvider, persistenceOptions);

        if (!persistenceOptions.Enabled)
        {
            Console.WriteLine("   ✓ Persistence middleware was NOT added (disabled in config)");
        }

        var workflow = WF.WorkflowForge.CreateWorkflow("DisabledWorkflow")
            .AddOperation(new ConfigLogOperation("Op1", "Operation executed"))
            .Build();

        var smith = WF.WorkflowForge.CreateSmith();

        if (recoveryOptions.Enabled)
        {
            await smith.ForgeWithRecoveryAsync(
                workflow,
                foundry,
                mockProvider,
                Guid.NewGuid(),
                Guid.NewGuid(),
                recoveryOptions);
        }
        else
        {
            await smith.ForgeAsync(workflow, foundry);
            Console.WriteLine("   ✓ Recovery was skipped (disabled in config)");
        }

        Console.WriteLine();
    }

    private static async Task DemonstrateConfigurationValidation(IServiceProvider serviceProvider)
    {
        Console.WriteLine("3. Configuration Validation Demo");
        Console.WriteLine("   ------------------------------");

        var validationOptions = serviceProvider.GetRequiredService<IOptions<ValidationMiddlewareOptions>>().Value;
        var pollyOptions = serviceProvider.GetRequiredService<IOptions<PollyMiddlewareOptions>>().Value;
        var persistenceOptions = serviceProvider.GetRequiredService<IOptions<PersistenceOptions>>().Value;

        // Validate configurations
        var validationErrors = validationOptions.Validate();
        var pollyErrors = pollyOptions.Validate();
        var persistenceErrors = persistenceOptions.Validate();

        Console.WriteLine($"   Validation config errors: {validationErrors.Count}");
        Console.WriteLine($"   Polly config errors: {pollyErrors.Count}");
        Console.WriteLine($"   Persistence config errors: {persistenceErrors.Count}");

        if (validationErrors.Count > 0)
        {
            Console.WriteLine("   ⚠️  Validation configuration issues:");
            foreach (var error in validationErrors)
            {
                Console.WriteLine($"      - {error}");
            }
        }

        if (pollyErrors.Count > 0)
        {
            Console.WriteLine("   ⚠️  Polly configuration issues:");
            foreach (var error in pollyErrors)
            {
                Console.WriteLine($"      - {error}");
            }
        }

        if (persistenceErrors.Count > 0)
        {
            Console.WriteLine("   ⚠️  Persistence configuration issues:");
            foreach (var error in persistenceErrors)
            {
                Console.WriteLine($"      - {error}");
            }
        }

        if (validationErrors.Count == 0 && pollyErrors.Count == 0 && persistenceErrors.Count == 0)
        {
            Console.WriteLine("   ✓ All configurations are valid!");
        }

        Console.WriteLine();
        Console.WriteLine("Key Takeaways:");
        Console.WriteLine("- Extensions can be enabled/disabled via appsettings.json");
        Console.WriteLine("- Configuration is validated on startup");
        Console.WriteLine("- No code changes needed to toggle features");
        Console.WriteLine("- Restart application to apply configuration changes");
    }

    private class InMemoryPersistenceProvider : IWorkflowPersistenceProvider
    {
        private readonly Dictionary<(Guid, Guid), WorkflowExecutionSnapshot> _store = new();

        public Task SaveAsync(WorkflowExecutionSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            _store[(snapshot.FoundryExecutionId, snapshot.WorkflowId)] = snapshot;
            return Task.CompletedTask;
        }

        public Task<WorkflowExecutionSnapshot?> TryLoadAsync(Guid foundryExecutionId, Guid workflowId, CancellationToken cancellationToken = default)
        {
            _store.TryGetValue((foundryExecutionId, workflowId), out var snapshot);
            return Task.FromResult<WorkflowExecutionSnapshot?>(snapshot);
        }

        public Task DeleteAsync(Guid foundryExecutionId, Guid workflowId, CancellationToken cancellationToken = default)
        {
            _store.Remove((foundryExecutionId, workflowId));
            return Task.CompletedTask;
        }
    }

    private sealed class ConfigLogOperation : IWorkflowOperation
    {
        private readonly string _message;

        public ConfigLogOperation(string name, string message)
        {
            Name = name;
            _message = message;
        }

        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; }
        public bool SupportsRestore => false;

        public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            foundry.Logger.LogInformation(_message);
            return Task.FromResult(inputData);
        }

        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public void Dispose()
        { }
    }
}