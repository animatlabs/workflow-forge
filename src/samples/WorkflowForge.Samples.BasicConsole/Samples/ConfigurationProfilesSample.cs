using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Operations;
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
            .WithOperation(new ConfigInitOperation(
                label: "Minimal",
                infoLines: new[] { "Features: Basic logging, no performance monitoring" },
                delayMs: 50))
            .WithOperation(new ConfigProcessOperation(
                message: "Processing with minimal overhead...",
                infoLines: Array.Empty<string>(),
                delayMs: 100,
                propertiesToSet: new Dictionary<string, object?> { ["data_processed"] = true }))
            .WithOperation(new ConfigCompleteOperation(
                label: "Minimal",
                infoLines: Array.Empty<string>(),
                delayMs: 20));

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
            .WithOperation(new ConfigInitOperation(
                label: "Development",
                infoLines: new[]
                {
                    "Features: Verbose logging, debug information, performance hints",
                    "Best for: Local development, debugging, prototyping"
                },
                delayMs: 60))
            .WithOperation(new ConfigProcessOperation(
                message: "Processing with development features...",
                infoLines: new[] { "Debug info: Operation started" },
                delayMs: 120,
                propertiesToSet: new Dictionary<string, object?>
                {
                    ["data_processed"] = true,
                    ["debug_info"] = "Data processing completed successfully"
                },
                completionInfoLine: "Debug info: Operation completed"))
            .WithOperation(new ConfigCompleteOperation(
                label: "Development",
                infoLines: Array.Empty<string>(),
                delayMs: 30,
                propertyDisplays: new[]
                {
                    new PropertyDisplay("Debug data available", "debug_info")
                }));

        await foundry.ForgeAsync();
    }

    private static async Task RunWithProductionConfiguration()
    {
        Console.WriteLine("\n--- Production Configuration ---");

        using var foundry = WorkflowForge.CreateFoundry("ProductionConfig");

        foundry.Properties["config_type"] = "Production";
        foundry.Properties["start_time"] = DateTime.UtcNow;
        foundry.Properties["environment"] = "prod";
        foundry.Properties["correlation_id"] = Guid.NewGuid().ToString("N").Substring(0, 8);

        foundry
            .WithOperation(new ConfigInitOperation(
                label: "Production",
                infoLines: new[]
                {
                    "Features: Structured logging, error tracking, monitoring hooks",
                    "Best for: Live environments, customer-facing workflows"
                },
                delayMs: 40,
                headerKey: "correlation_id",
                headerLabel: "Correlation"))
            .WithOperation(new ConfigProcessOperation(
                message: "Processing in production mode",
                infoLines: new[] { "Security: All operations are audited" },
                delayMs: 90,
                propertiesToSet: new Dictionary<string, object?>
                {
                    ["data_processed"] = true,
                    ["audit_trail"] = $"Data processed at {DateTime.UtcNow:HH:mm:ss.fff}"
                },
                headerKey: "correlation_id",
                headerLabel: "Correlation"))
            .WithOperation(new ConfigCompleteOperation(
                label: "Production",
                infoLines: new[] { "Metrics sent to monitoring system" },
                delayMs: 25,
                headerKey: "correlation_id",
                headerLabel: "Correlation"));

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
            .WithOperation(new ConfigInitOperation(
                label: "High performance",
                infoLines: new[]
                {
                    "Features: Minimal logging, optimized allocations, fast execution",
                    "Best for: High-throughput, batch processing, latency-sensitive operations"
                },
                delayMs: 20,
                headerKey: "batch_id",
                headerLabel: "Batch"))
            .WithOperation(new ConfigProcessOperation(
                message: "Processing with performance optimizations...",
                infoLines: new[]
                {
                    "Memory allocations minimized",
                    "CPU usage optimized"
                },
                delayMs: 50,
                propertiesToSet: new Dictionary<string, object?>
                {
                    ["data_processed"] = true,
                    ["performance_optimized"] = true
                }))
            .WithOperation(new ConfigCompleteOperation(
                label: "High performance",
                infoLines: new[] { "Performance metrics: Memory efficient, CPU optimized" },
                delayMs: 10,
                headerKey: "batch_id",
                headerLabel: "Batch"));

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
            .WithOperation(new ConfigInitOperation(
                label: "Options pattern",
                infoLines: new[]
                {
                    "Features: Strongly-typed settings, appsettings.json binding",
                    "Best for: Production applications, environment-specific configs",
                    $"Settings: MaxConcurrentWorkflows = {foundry.Properties["max_concurrent"]}"
                },
                delayMs: 80))
            .WithOperation(new ConfigProcessOperation(
                message: $"Processing with max concurrency: {foundry.Properties["max_concurrent"]}",
                infoLines: new[] { "Configuration is validated and type-safe" },
                delayMs: 120,
                propertiesToSet: new Dictionary<string, object?>
                {
                    ["data_processed"] = true,
                    ["config_validated"] = true
                }))
            .WithOperation(new ConfigCompleteOperation(
                label: "Options pattern",
                infoLines: new[] { "Configuration was loaded from appsettings.json and validated" },
                delayMs: 40));

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
            .WithOperation(new ConfigInitOperation(
                label: "Custom",
                infoLines: new[]
                {
                    "Features: Custom logger, specialized operations, flexible setup",
                    "Best for: Specific business requirements, custom integrations"
                },
                delayMs: 70,
                listKey: "custom_features",
                listLabel: "Enabled features"))
            .WithOperation(new ConfigProcessOperation(
                message: "Processing with custom business logic...",
                infoLines: new[]
                {
                    "Custom validation rules applied",
                    "Specialized data transformations"
                },
                delayMs: 110,
                propertiesToSet: new Dictionary<string, object?>
                {
                    ["data_processed"] = true,
                    ["custom_validation"] = "passed",
                    ["transformation_applied"] = "specialized_format"
                }))
            .WithOperation(new ConfigCompleteOperation(
                label: "Custom",
                infoLines: Array.Empty<string>(),
                delayMs: 35,
                propertyDisplays: new[]
                {
                    new PropertyDisplay("Validation", "custom_validation"),
                    new PropertyDisplay("Transformation", "transformation_applied")
                }));

        await foundry.ForgeAsync();

        Console.WriteLine("\n[INFO] Configuration Summary:");
        Console.WriteLine("  • Minimal: Zero setup, basic features, fastest startup");
        Console.WriteLine("  • Development: Rich debugging, verbose logging, developer-friendly");
        Console.WriteLine("  • Production: Audit trails, monitoring, enterprise features");
        Console.WriteLine("  • High Performance: Optimized for speed and throughput");
        Console.WriteLine("  • Custom: Tailored to specific business requirements");
    }

    private readonly struct PropertyDisplay
    {
        public PropertyDisplay(string label, string key)
        {
            Label = label;
            Key = key;
        }

        public string Label { get; }
        public string Key { get; }
    }

    private sealed class ConfigInitOperation : WorkflowOperationBase
    {
        private readonly string _label;
        private readonly string[] _infoLines;
        private readonly int _delayMs;
        private readonly string? _headerKey;
        private readonly string? _headerLabel;
        private readonly string? _listKey;
        private readonly string? _listLabel;

        public ConfigInitOperation(
            string label,
            string[] infoLines,
            int delayMs,
            string? headerKey = null,
            string? headerLabel = null,
            string? listKey = null,
            string? listLabel = null)
        {
            _label = label;
            _infoLines = infoLines;
            _delayMs = delayMs;
            _headerKey = headerKey;
            _headerLabel = headerLabel;
            _listKey = listKey;
            _listLabel = listLabel;
        }

        public override string Name => "InitConfig";

        protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            var headerSuffix = BuildHeader(foundry, _headerKey, _headerLabel);
            Console.WriteLine($"   [CONFIG] {_label} configuration initialized{headerSuffix}");

            foreach (var info in _infoLines)
            {
                Console.WriteLine($"   [INFO] {info}");
            }

            if (!string.IsNullOrWhiteSpace(_listKey)
                && foundry.Properties.TryGetValue(_listKey!, out var listObj)
                && listObj is string[] items)
            {
                Console.WriteLine($"   [FEATURES] {_listLabel}: {string.Join(", ", items)}");
            }

            await Task.Delay(_delayMs, cancellationToken);
            return inputData;
        }
    }

    private sealed class ConfigProcessOperation : WorkflowOperationBase
    {
        private readonly string _message;
        private readonly string[] _infoLines;
        private readonly int _delayMs;
        private readonly Dictionary<string, object?> _propertiesToSet;
        private readonly string? _completionInfoLine;
        private readonly string? _headerKey;
        private readonly string? _headerLabel;

        public ConfigProcessOperation(
            string message,
            string[] infoLines,
            int delayMs,
            Dictionary<string, object?> propertiesToSet,
            string? completionInfoLine = null,
            string? headerKey = null,
            string? headerLabel = null)
        {
            _message = message;
            _infoLines = infoLines;
            _delayMs = delayMs;
            _propertiesToSet = propertiesToSet;
            _completionInfoLine = completionInfoLine;
            _headerKey = headerKey;
            _headerLabel = headerLabel;
        }

        public override string Name => "ProcessData";

        protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            var headerSuffix = BuildHeader(foundry, _headerKey, _headerLabel);
            Console.WriteLine($"   [INFO] {_message}{headerSuffix}");

            foreach (var info in _infoLines)
            {
                var prefix = info.StartsWith("Security", StringComparison.OrdinalIgnoreCase) ? "   [SECURITY]" :
                    info.StartsWith("Debug", StringComparison.OrdinalIgnoreCase) ? "   [DEBUG]" :
                    info.StartsWith("Memory", StringComparison.OrdinalIgnoreCase) || info.StartsWith("CPU", StringComparison.OrdinalIgnoreCase)
                        ? "   [PERFORMANCE]"
                        : "   [INFO]";
                Console.WriteLine($"{prefix} {info}");
            }

            await Task.Delay(_delayMs, cancellationToken);

            foreach (var kvp in _propertiesToSet)
            {
                foundry.Properties[kvp.Key] = kvp.Value;
            }

            if (!string.IsNullOrWhiteSpace(_completionInfoLine))
            {
                Console.WriteLine($"   [DEBUG] {_completionInfoLine}");
            }

            return inputData;
        }
    }

    private sealed class ConfigCompleteOperation : WorkflowOperationBase
    {
        private readonly string _label;
        private readonly string[] _infoLines;
        private readonly int _delayMs;
        private readonly string? _headerKey;
        private readonly string? _headerLabel;
        private readonly PropertyDisplay[]? _propertyDisplays;

        public ConfigCompleteOperation(
            string label,
            string[] infoLines,
            int delayMs,
            string? headerKey = null,
            string? headerLabel = null,
            PropertyDisplay[]? propertyDisplays = null)
        {
            _label = label;
            _infoLines = infoLines;
            _delayMs = delayMs;
            _headerKey = headerKey;
            _headerLabel = headerLabel;
            _propertyDisplays = propertyDisplays;
        }

        public override string Name => "Complete";

        protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            var duration = DateTime.UtcNow - (DateTime)foundry.Properties["start_time"]!;
            var headerSuffix = BuildHeader(foundry, _headerKey, _headerLabel);
            Console.WriteLine($"   [SUCCESS] {_label} workflow completed in {duration.TotalMilliseconds:F0}ms{headerSuffix}");

            foreach (var info in _infoLines)
            {
                Console.WriteLine($"   [INFO] {info}");
            }

            if (_propertyDisplays != null)
            {
                foreach (var display in _propertyDisplays)
                {
                    if (foundry.Properties.TryGetValue(display.Key, out var value))
                    {
                        Console.WriteLine($"   [INFO] {display.Label}: {value}");
                    }
                }
            }

            await Task.Delay(_delayMs, cancellationToken);
            return inputData;
        }
    }

    private static string BuildHeader(IWorkflowFoundry foundry, string? headerKey, string? headerLabel)
    {
        if (string.IsNullOrWhiteSpace(headerKey) || string.IsNullOrWhiteSpace(headerLabel))
        {
            return string.Empty;
        }

        return foundry.Properties.TryGetValue(headerKey, out var value) && value != null
            ? $" [{headerLabel}: {value}]"
            : string.Empty;
    }
}