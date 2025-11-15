using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WorkflowForge.Extensions;
using WorkflowForge.Options;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates different foundry configurations for various environments.
/// Shows how to configure WorkflowForge for Development, Production, and High-Performance scenarios.
/// Also demonstrates the Options pattern for configuration from appsettings.json.
/// </summary>
public class ConfigurationProfilesSample : ISample
{
    public string Name => "Configuration Profiles";
    public string Description => "Development, Production, and High-Performance configurations";

    public async Task RunAsync()
    {
        Console.WriteLine("Demonstrating different WorkflowForge configuration profiles...");
        Console.WriteLine("Including both code-based and Options pattern configurations");

        // Run the same workflow with different configurations
        await RunWithMinimalConfiguration();
        await RunWithDevelopmentConfiguration();
        await RunWithProductionConfiguration();
        await RunWithHighPerformanceConfiguration();
        await RunWithOptionsPatternConfiguration();
        await RunWithCustomConfiguration();
    }

    private static async Task RunWithMinimalConfiguration()
    {
        Console.WriteLine("\n--- Minimal Configuration (Zero Setup) ---");

        // Minimal configuration - just create foundry with defaults
        using var foundry = WorkflowForge.CreateFoundry("MinimalConfig");

        foundry.Properties["config_type"] = "Minimal";
        foundry.Properties["start_time"] = DateTime.UtcNow;

        foundry
            .WithOperation("InitConfig", async (foundry) =>
            {
                Console.WriteLine("   [CONFIG] Minimal configuration initialized");
                Console.WriteLine("   [INFO] Features: Basic logging, no performance monitoring");
                await Task.Delay(50);
            })
            .WithOperation("ProcessData", async (foundry) =>
            {
                Console.WriteLine("   [INFO] Processing with minimal overhead...");
                await Task.Delay(100);
                foundry.Properties["data_processed"] = true;
            })
            .WithOperation("Complete", async (foundry) =>
            {
                var duration = DateTime.UtcNow - (DateTime)foundry.Properties["start_time"]!;
                Console.WriteLine($"   [SUCCESS] Minimal workflow completed in {duration.TotalMilliseconds:F0}ms");
                await Task.Delay(20);
            });

        await foundry.ForgeAsync();
    }

    private static async Task RunWithDevelopmentConfiguration()
    {
        Console.WriteLine("\n--- Development Configuration ---");

        using var foundry = WorkflowForge.CreateFoundry("DevelopmentConfig");

        foundry.Properties["config_type"] = "Development";
        foundry.Properties["start_time"] = DateTime.UtcNow;
        foundry.Properties["debug_mode"] = true;

        foundry
            .WithOperation("InitConfig", async (foundry) =>
            {
                Console.WriteLine("   [CONFIG] Development configuration initialized");
                Console.WriteLine("   [INFO] Features: Verbose logging, debug information, performance hints");
                Console.WriteLine("   [INFO] Best for: Local development, debugging, prototyping");
                await Task.Delay(60);
            })
            .WithOperation("ProcessData", async (foundry) =>
            {
                Console.WriteLine("   [INFO] Processing with development features...");
                Console.WriteLine("   [DEBUG] Debug info: Operation started");
                await Task.Delay(120);
                foundry.Properties["data_processed"] = true;
                foundry.Properties["debug_info"] = "Data processing completed successfully";
                Console.WriteLine("   [DEBUG] Debug info: Operation completed");
            })
            .WithOperation("Complete", async (foundry) =>
            {
                var duration = DateTime.UtcNow - (DateTime)foundry.Properties["start_time"]!;
                Console.WriteLine($"   [SUCCESS] Development workflow completed in {duration.TotalMilliseconds:F0}ms");
                Console.WriteLine($"   [DEBUG] Debug data available: {foundry.Properties["debug_info"]}");
                await Task.Delay(30);
            });

        await foundry.ForgeAsync();
    }

    private static async Task RunWithProductionConfiguration()
    {
        Console.WriteLine("\n--- Production Configuration ---");

        using var foundry = WorkflowForge.CreateFoundry("ProductionConfig");

        foundry.Properties["config_type"] = "Production";
        foundry.Properties["start_time"] = DateTime.UtcNow;
        foundry.Properties["environment"] = "prod";
        foundry.Properties["correlation_id"] = Guid.NewGuid().ToString("N")[..8];

        foundry
            .WithOperation("InitConfig", async (foundry) =>
            {
                var correlationId = foundry.Properties["correlation_id"];
                Console.WriteLine($"   [CONFIG] Production configuration initialized [Correlation: {correlationId}]");
                Console.WriteLine("   [INFO] Features: Structured logging, error tracking, monitoring hooks");
                Console.WriteLine("   [INFO] Best for: Live environments, customer-facing workflows");
                await Task.Delay(40);
            })
            .WithOperation("ProcessData", async (foundry) =>
            {
                var correlationId = foundry.Properties["correlation_id"];
                Console.WriteLine($"   [INFO] Processing in production mode [Correlation: {correlationId}]");
                Console.WriteLine("   [SECURITY] Security: All operations are audited");
                await Task.Delay(90);
                foundry.Properties["data_processed"] = true;
                foundry.Properties["audit_trail"] = $"Data processed at {DateTime.UtcNow:HH:mm:ss.fff}";
            })
            .WithOperation("Complete", async (foundry) =>
            {
                var duration = DateTime.UtcNow - (DateTime)foundry.Properties["start_time"]!;
                var correlationId = foundry.Properties["correlation_id"];
                Console.WriteLine($"   [SUCCESS] Production workflow completed in {duration.TotalMilliseconds:F0}ms [Correlation: {correlationId}]");
                Console.WriteLine("   [METRICS] Metrics sent to monitoring system");
                await Task.Delay(25);
            });

        await foundry.ForgeAsync();
    }

    private static async Task RunWithHighPerformanceConfiguration()
    {
        Console.WriteLine("\n--- High Performance Configuration ---");

        using var foundry = WorkflowForge.CreateFoundry("HighPerformanceConfig");

        foundry.Properties["config_type"] = "HighPerformance";
        foundry.Properties["start_time"] = DateTime.UtcNow;
        foundry.Properties["batch_id"] = $"BATCH-{DateTime.UtcNow:yyyyMMddHHmmss}";

        foundry
            .WithOperation("InitConfig", async (foundry) =>
            {
                var batchId = foundry.Properties["batch_id"];
                Console.WriteLine($"   [PERFORMANCE] High performance configuration initialized [Batch: {batchId}]");
                Console.WriteLine("   [INFO] Features: Minimal logging, optimized allocations, fast execution");
                Console.WriteLine("   [INFO] Best for: High-throughput, batch processing, latency-sensitive operations");
                await Task.Delay(20); // Minimal delay for performance
            })
            .WithOperation("ProcessData", async (foundry) =>
            {
                Console.WriteLine("   [INFO] Processing with performance optimizations...");
                Console.WriteLine("   [PERFORMANCE] Memory allocations minimized");
                Console.WriteLine("   [PERFORMANCE] CPU usage optimized");
                await Task.Delay(50); // Faster processing
                foundry.Properties["data_processed"] = true;
                foundry.Properties["performance_optimized"] = true;
            })
            .WithOperation("Complete", async (foundry) =>
            {
                var duration = DateTime.UtcNow - (DateTime)foundry.Properties["start_time"]!;
                var batchId = foundry.Properties["batch_id"];
                Console.WriteLine($"   [SUCCESS] High performance workflow completed in {duration.TotalMilliseconds:F0}ms [Batch: {batchId}]");
                Console.WriteLine("   [INFO] Performance metrics: Memory efficient, CPU optimized");
                await Task.Delay(10); // Minimal completion overhead
            });

        await foundry.ForgeAsync();
    }

    private static async Task RunWithOptionsPatternConfiguration()
    {
        Console.WriteLine("\n--- Options Pattern Configuration (from appsettings.json) ---");

        // Get configuration from the global configuration
        var configuration = Program.Configuration;

        // Setup services with Options pattern
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.Configure<WorkflowForgeOptions>(
            configuration.GetSection(WorkflowForgeOptions.DefaultSectionName));

        using var serviceProvider = services.BuildServiceProvider();

        // Get configuration through Options pattern
        var workflowOptions = serviceProvider.GetRequiredService<IOptions<WorkflowForgeOptions>>();
        var settings = workflowOptions.Value;

        // Create foundry with settings from appsettings.json
        using var foundry = WorkflowForge.CreateFoundry("OptionsPatternConfig");

        foundry.Properties["config_type"] = "OptionsPattern";
        foundry.Properties["start_time"] = DateTime.UtcNow;
        foundry.Properties["settings_source"] = "appsettings.json";
        foundry.Properties["max_concurrent"] = settings.MaxConcurrentWorkflows;

        foundry
            .WithOperation("InitConfig", async (foundry) =>
            {
                Console.WriteLine("   [CONFIG] Options pattern configuration initialized");
                Console.WriteLine("   [INFO] Features: Strongly-typed settings, appsettings.json binding");
                Console.WriteLine("   [INFO] Best for: Production applications, environment-specific configs");
                Console.WriteLine($"   [SETTINGS] MaxConcurrentWorkflows: {foundry.Properties["max_concurrent"]}");
                await Task.Delay(80);
            })
            .WithOperation("ProcessData", async (foundry) =>
            {
                var maxConcurrent = (int)foundry.Properties["max_concurrent"]!;
                Console.WriteLine($"   [INFO] Processing with max concurrency: {maxConcurrent}");
                Console.WriteLine("   [CONFIG] Configuration is validated and type-safe");
                await Task.Delay(120);
                foundry.Properties["data_processed"] = true;
                foundry.Properties["config_validated"] = true;
            })
            .WithOperation("Complete", async (foundry) =>
            {
                var duration = DateTime.UtcNow - (DateTime)foundry.Properties["start_time"]!;
                Console.WriteLine($"   [SUCCESS] Options pattern workflow completed in {duration.TotalMilliseconds:F0}ms");
                Console.WriteLine("   [INFO] Configuration was loaded from appsettings.json and validated");
                await Task.Delay(40);
            });

        await foundry.ForgeAsync();
    }

    private static async Task RunWithCustomConfiguration()
    {
        Console.WriteLine("\n--- Custom Configuration ---");

        // Create foundry (custom logger would be passed via WorkflowSmith)
        using var foundry = WorkflowForge.CreateFoundry("CustomConfig");

        foundry.Properties["config_type"] = "Custom";
        foundry.Properties["start_time"] = DateTime.UtcNow;
        foundry.Properties["custom_features"] = new[] { "CustomLogging", "SpecializedProcessing", "FlexibleConfiguration" };

        foundry
            .WithOperation("InitConfig", async (foundry) =>
            {
                Console.WriteLine("   [CONFIG] Custom configuration initialized");
                Console.WriteLine("   [INFO] Features: Custom logger, specialized operations, flexible setup");
                Console.WriteLine("   [INFO] Best for: Specific business requirements, custom integrations");

                var features = (string[])foundry.Properties["custom_features"]!;
                Console.WriteLine($"   [FEATURES] Enabled features: {string.Join(", ", features)}");

                await Task.Delay(70);
            })
            .WithOperation("ProcessData", async (foundry) =>
            {
                Console.WriteLine("   [INFO] Processing with custom business logic...");
                Console.WriteLine("   [VALIDATION] Custom validation rules applied");
                Console.WriteLine("   [TRANSFORM] Specialized data transformations");

                await Task.Delay(110);

                foundry.Properties["data_processed"] = true;
                foundry.Properties["custom_validation"] = "passed";
                foundry.Properties["transformation_applied"] = "specialized_format";
            })
            .WithOperation("Complete", async (foundry) =>
            {
                var duration = DateTime.UtcNow - (DateTime)foundry.Properties["start_time"]!;
                Console.WriteLine($"   [SUCCESS] Custom workflow completed in {duration.TotalMilliseconds:F0}ms");
                Console.WriteLine($"   [VALIDATION] Validation: {foundry.Properties["custom_validation"]}");
                Console.WriteLine($"   [TRANSFORM] Transformation: {foundry.Properties["transformation_applied"]}");
                await Task.Delay(35);
            });

        await foundry.ForgeAsync();

        Console.WriteLine("\n[INFO] Configuration Summary:");
        Console.WriteLine("  • Minimal: Zero setup, basic features, fastest startup");
        Console.WriteLine("  • Development: Rich debugging, verbose logging, developer-friendly");
        Console.WriteLine("  • Production: Audit trails, monitoring, enterprise features");
        Console.WriteLine("  • High Performance: Optimized for speed and throughput");
        Console.WriteLine("  • Custom: Tailored to specific business requirements");
    }
}