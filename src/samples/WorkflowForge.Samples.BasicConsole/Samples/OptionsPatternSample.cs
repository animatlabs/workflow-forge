using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Extensions.Observability.Performance.Configurations;
using WorkflowForge.Extensions.Resilience.Polly.Options;
using WorkflowForge.Options;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates the Options pattern for configuration binding from appsettings.json.
/// Shows how to use strongly-typed configuration classes with dependency injection.
/// This is the recommended approach for production applications.
/// </summary>
public class OptionsPatternSample : ISample
{
    public string Name => "Options Pattern Configuration";
    public string Description => "Demonstrates Options pattern with appsettings.json binding";

    public async Task RunAsync()
    {
        Console.WriteLine("Demonstrating the Options pattern for configuration...");
        Console.WriteLine("This shows how to bind appsettings.json to strongly-typed classes");
        Console.WriteLine();

        // Setup configuration and services
        var services = new ServiceCollection();
        var configuration = Program.Configuration;

        // Register configuration using Options pattern
        services.AddSingleton(configuration);
        services.Configure<WorkflowForgeOptions>(
            configuration.GetSection(WorkflowForgeOptions.DefaultSectionName));
        services.Configure<PollyMiddlewareOptions>(
            configuration.GetSection("WorkflowForge:Polly"));
        services.Configure<PerformanceSettings>(
            configuration.GetSection("WorkflowForge:Performance"));

        using var serviceProvider = services.BuildServiceProvider();

        // Demonstrate different ways to access configuration
        await DemonstrateConfigurationAccess(serviceProvider);
        await DemonstrateFoundryWithConfiguration(serviceProvider);
        await DemonstrateConfigurationValidation(serviceProvider);
    }

    private static async Task DemonstrateConfigurationAccess(IServiceProvider serviceProvider)
    {
        Console.WriteLine("--- Configuration Access Patterns ---");

        // Method 1: IOptions<T> - Standard pattern
        var workflowOptions = serviceProvider.GetRequiredService<IOptions<WorkflowForgeOptions>>();
        var workflowSettings = workflowOptions.Value;

        Console.WriteLine($"[IOptions] MaxConcurrentWorkflows: {workflowSettings.MaxConcurrentWorkflows}");
        Console.WriteLine($"[IOptions] MaxConcurrentWorkflows: {workflowSettings.MaxConcurrentWorkflows}");

        // Method 2: IOptionsSnapshot<T> - For scoped scenarios with reload support
        var pollySnapshot = serviceProvider.GetService<IOptionsSnapshot<PollyMiddlewareOptions>>();
        if (pollySnapshot != null)
        {
            var pollySettings = pollySnapshot.Value;
            Console.WriteLine($"[IOptionsSnapshot] Polly.Enabled: {pollySettings.Enabled}");
            Console.WriteLine($"[IOptionsSnapshot] Retry.MaxRetryAttempts: {pollySettings.Retry.MaxRetryAttempts}");
        }

        // Method 3: Direct configuration binding
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var performanceSettings = new PerformanceSettings();
        configuration.GetSection("WorkflowForge:Performance").Bind(performanceSettings);

        Console.WriteLine($"[Direct Bind] Performance.MaxDegreeOfParallelism: {performanceSettings.MaxDegreeOfParallelism}");
        Console.WriteLine($"[Direct Bind] Performance.EnableObjectPooling: {performanceSettings.EnableObjectPooling}");

        await Task.Delay(100);
    }

    private static async Task DemonstrateFoundryWithConfiguration(IServiceProvider serviceProvider)
    {
        Console.WriteLine("\n--- Using Configuration in Workflow Operations ---");

        // Get configuration through Options pattern
        var workflowOptions = serviceProvider.GetRequiredService<IOptions<WorkflowForgeOptions>>();
        var pollyOptions = serviceProvider.GetRequiredService<IOptions<PollyMiddlewareOptions>>();

        // Create foundry with configuration-based settings
        using var foundry = WorkflowForge.CreateFoundry("OptionsPatternWorkflow");

        foundry.Properties["workflow_settings"] = workflowOptions.Value;
        foundry.Properties["polly_settings"] = pollyOptions.Value;

        foundry
            .WithOperation(new ConfigurationAwareOperation())
            .WithOperation(new SettingsValidationOperation());

        await foundry.ForgeAsync();
    }

    private static async Task DemonstrateConfigurationValidation(IServiceProvider serviceProvider)
    {
        Console.WriteLine("\n--- Configuration Validation ---");

        var pollyOptions = serviceProvider.GetRequiredService<IOptions<PollyMiddlewareOptions>>();
        var pollySettings = pollyOptions.Value;

        // Show validation using built-in validation
        var validationErrors = pollySettings.Validate();

        if (validationErrors.Any())
        {
            Console.WriteLine("[VALIDATION] Configuration validation errors found:");
            foreach (var error in validationErrors)
            {
                Console.WriteLine($"   [ERROR] {error}");
            }
        }
        else
        {
            Console.WriteLine("[VALIDATION] All configuration settings are valid");
        }

        await Task.Delay(50);
    }
}

/// <summary>
/// Example operation that uses configuration from the Options pattern
/// </summary>
public class ConfigurationAwareOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "ConfigurationAware";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("   [INFO] Processing with configuration-aware operation...");

        // Access configuration through foundry properties
        if (foundry.Properties.TryGetValue("workflow_settings", out var workflowSettingsObj) &&
            workflowSettingsObj is WorkflowForgeOptions workflowSettings)
        {
            Console.WriteLine($"   [CONFIG] MaxConcurrentWorkflows: {workflowSettings.MaxConcurrentWorkflows}");
            Console.WriteLine($"   [CONFIG] MaxConcurrentWorkflows: {workflowSettings.MaxConcurrentWorkflows}");
        }

        if (foundry.Properties.TryGetValue("polly_settings", out var pollySettingsObj) &&
            pollySettingsObj is PollyMiddlewareOptions pollySettings)
        {
            Console.WriteLine($"   [CONFIG] Polly enabled: {pollySettings.Enabled}");
            Console.WriteLine($"   [CONFIG] Retry attempts: {pollySettings.Retry.MaxRetryAttempts}");
            Console.WriteLine($"   [CONFIG] Circuit breaker enabled: {pollySettings.CircuitBreaker.IsEnabled}");
        }

        await Task.Delay(150, cancellationToken);
        return "Configuration processed successfully";
    }

    public Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    { }
}

/// <summary>
/// Example operation that validates settings at runtime
/// </summary>
public class SettingsValidationOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "SettingsValidation";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("   [INFO] Validating runtime settings...");

        // Example of runtime configuration validation
        if (foundry.Properties.TryGetValue("workflow_settings", out var settingsObj) &&
            settingsObj is WorkflowForgeOptions settings)
        {
            if (settings.MaxConcurrentWorkflows <= 0)
            {
                throw new InvalidOperationException("MaxConcurrentWorkflows must be greater than 0");
            }

            if (settings.MaxConcurrentWorkflows > Environment.ProcessorCount * 4)
            {
                Console.WriteLine("   [WARNING] MaxConcurrentWorkflows is very high - may impact performance");
            }
        }

        Console.WriteLine("   [SUCCESS] All settings validated successfully");
        await Task.Delay(100, cancellationToken);
        return "Settings validation completed";
    }

    public Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    { }
}