using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WorkflowForge;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Extensions.Resilience.Polly.Configurations;
using WorkflowForge.Extensions.Observability.Performance;

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
        services.Configure<WorkflowForgeSettings>(
            configuration.GetSection(WorkflowForgeSettings.SectionName));
        services.Configure<PollySettings>(
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
        var workflowOptions = serviceProvider.GetRequiredService<IOptions<WorkflowForgeSettings>>();
        var workflowSettings = workflowOptions.Value;
        
        Console.WriteLine($"[IOptions] AutoRestore: {workflowSettings.AutoRestore}");
        Console.WriteLine($"[IOptions] MaxConcurrentOperations: {workflowSettings.MaxConcurrentOperations}");

        // Method 2: IOptionsSnapshot<T> - For scoped scenarios with reload support
        var pollySnapshot = serviceProvider.GetService<IOptionsSnapshot<PollySettings>>();
        if (pollySnapshot != null)
        {
            var pollySettings = pollySnapshot.Value;
            Console.WriteLine($"[IOptionsSnapshot] Polly.IsEnabled: {pollySettings.IsEnabled}");
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
        var workflowOptions = serviceProvider.GetRequiredService<IOptions<WorkflowForgeSettings>>();
        var pollyOptions = serviceProvider.GetRequiredService<IOptions<PollySettings>>();
        
        // Create foundry with configuration-based settings
        var foundryConfig = new FoundryConfiguration
        {
            // Use configuration values to set foundry options
            MaxRetryAttempts = pollyOptions.Value.Retry.MaxRetryAttempts
        };

        using var foundry = WorkflowForge.CreateFoundry("OptionsPatternWorkflow", foundryConfig);
        
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

        var pollyOptions = serviceProvider.GetRequiredService<IOptions<PollySettings>>();
        var pollySettings = pollyOptions.Value;

        // Show validation using built-in validation
        var validationResults = pollySettings.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(pollySettings));
        
        if (validationResults.Any())
        {
            Console.WriteLine("[VALIDATION] Configuration validation errors found:");
            foreach (var result in validationResults)
            {
                Console.WriteLine($"   [ERROR] {result.ErrorMessage}");
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
            workflowSettingsObj is WorkflowForgeSettings workflowSettings)
        {
            Console.WriteLine($"   [CONFIG] AutoRestore setting: {workflowSettings.AutoRestore}");
            Console.WriteLine($"   [CONFIG] MaxConcurrentOperations: {workflowSettings.MaxConcurrentOperations}");
        }

        if (foundry.Properties.TryGetValue("polly_settings", out var pollySettingsObj) &&
            pollySettingsObj is PollySettings pollySettings)
        {
            Console.WriteLine($"   [CONFIG] Polly enabled: {pollySettings.IsEnabled}");
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

    public void Dispose() { }
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
            settingsObj is WorkflowForgeSettings settings)
        {
            if (settings.MaxConcurrentOperations <= 0)
            {
                throw new InvalidOperationException("MaxConcurrentOperations must be greater than 0");
            }
            
            if (settings.MaxConcurrentOperations > Environment.ProcessorCount * 4)
            {
                Console.WriteLine("   [WARNING] MaxConcurrentOperations is very high - may impact performance");
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

    public void Dispose() { }
} 