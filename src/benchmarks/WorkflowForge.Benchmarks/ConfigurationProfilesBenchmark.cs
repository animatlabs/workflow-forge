using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WorkflowForge.Abstractions;
using WorkflowForge.Configurations;
using WorkflowForge.Extensions;
using WorkflowForge.Extensions.Observability.Performance.Configurations;
using WorkflowForge.Extensions.Resilience.Polly.Configurations;

namespace WorkflowForge.Benchmarks;

/// <summary>
/// Benchmarks different WorkflowForge configuration profiles to measure:
/// - Configuration loading overhead
/// - Options pattern performance
/// - Foundry creation time
/// - Configuration validation impact
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Monitoring, iterationCount: 25)]
[MarkdownExporter]
[HtmlExporter]
public class ConfigurationProfilesBenchmark
{
    private IConfiguration _configuration = null!;
    private IServiceProvider _serviceProvider = null!;
    private WorkflowForgeConfiguration _workflowSettings = null!;
    private PollySettings _pollySettings = null!;
    private PerformanceSettings _performanceSettings = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Setup configuration from appsettings.json
        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false);

        _configuration = configurationBuilder.Build();

        // Setup dependency injection with Options pattern
        var services = new ServiceCollection();
        services.AddSingleton(_configuration);
        services.Configure<WorkflowForgeConfiguration>(
            _configuration.GetSection(WorkflowForgeConfiguration.SectionName));
        services.Configure<PollySettings>(
            _configuration.GetSection("WorkflowForge:Polly"));
        services.Configure<PerformanceSettings>(
            _configuration.GetSection("WorkflowForge:Performance"));

        _serviceProvider = services.BuildServiceProvider();

        // Pre-load settings for direct comparison
        _workflowSettings = _serviceProvider.GetRequiredService<IOptions<WorkflowForgeConfiguration>>().Value;
        _pollySettings = _serviceProvider.GetRequiredService<IOptions<PollySettings>>().Value;
        _performanceSettings = _serviceProvider.GetRequiredService<IOptions<PerformanceSettings>>().Value;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    [Benchmark(Baseline = true)]
    public IWorkflowFoundry MinimalConfiguration()
    {
        return WorkflowForge.CreateFoundry("MinimalConfig");
    }

    [Benchmark]
    public IWorkflowFoundry DevelopmentConfiguration()
    {
        var config = FoundryConfiguration.Development();
        return WorkflowForge.CreateFoundry("DevelopmentConfig", config);
    }

    [Benchmark]
    public IWorkflowFoundry ProductionConfiguration()
    {
        var config = FoundryConfiguration.ForProduction();
        return WorkflowForge.CreateFoundry("ProductionConfig", config);
    }

    [Benchmark]
    public IWorkflowFoundry HighPerformanceConfiguration()
    {
        var config = FoundryConfiguration.HighPerformance();
        return WorkflowForge.CreateFoundry("HighPerformanceConfig", config);
    }

    [Benchmark]
    public IWorkflowFoundry OptionsPatternConfiguration()
    {
        var workflowOptions = _serviceProvider.GetRequiredService<IOptions<WorkflowForgeConfiguration>>();
        var pollyOptions = _serviceProvider.GetRequiredService<IOptions<PollySettings>>();

        var config = new FoundryConfiguration
        {
            MaxRetryAttempts = pollyOptions.Value.Retry.MaxRetryAttempts,
            EnableDetailedTiming = true
        };

        return WorkflowForge.CreateFoundry("OptionsPatternConfig", config);
    }

    [Benchmark]
    public IWorkflowFoundry OptionsPatternWithValidation()
    {
        var workflowOptions = _serviceProvider.GetRequiredService<IOptions<WorkflowForgeConfiguration>>();
        var pollyOptions = _serviceProvider.GetRequiredService<IOptions<PollySettings>>();

        // Perform validation
        var validationResults = pollyOptions.Value.Validate(
            new System.ComponentModel.DataAnnotations.ValidationContext(pollyOptions.Value));

        if (validationResults.Any())
        {
            throw new InvalidOperationException("Configuration validation failed");
        }

        var config = new FoundryConfiguration
        {
            MaxRetryAttempts = pollyOptions.Value.Retry.MaxRetryAttempts,
            EnableDetailedTiming = true
        };

        return WorkflowForge.CreateFoundry("ValidatedConfig", config);
    }

    [Benchmark]
    public WorkflowForgeConfiguration ConfigurationBinding()
    {
        var settings = new WorkflowForgeConfiguration();
        _configuration.GetSection(WorkflowForgeConfiguration.SectionName).Bind(settings);
        return settings;
    }

    [Benchmark]
    public WorkflowForgeConfiguration OptionsPatternAccess()
    {
        var options = _serviceProvider.GetRequiredService<IOptions<WorkflowForgeConfiguration>>();
        return options.Value;
    }

    [Benchmark]
    public WorkflowForgeConfiguration CachedSettingsAccess()
    {
        return _workflowSettings;
    }

    [Benchmark]
    public async Task<string> SimpleWorkflowWithMinimalConfig()
    {
        using var foundry = WorkflowForge.CreateFoundry("BenchmarkWorkflow");

        foundry.WithOperation("SimpleOperation", async (foundry) =>
        {
            await Task.Delay(1);
            foundry.Properties["result"] = "Completed";
        });

        await foundry.ForgeAsync();
        return "Success";
    }

    [Benchmark]
    public async Task<string> SimpleWorkflowWithOptionsConfig()
    {
        var config = new FoundryConfiguration
        {
            MaxRetryAttempts = _pollySettings.Retry.MaxRetryAttempts,
            EnableDetailedTiming = false // Disable for performance
        };

        using var foundry = WorkflowForge.CreateFoundry("BenchmarkWorkflow", config);

        foundry.WithOperation("SimpleOperation", async (foundry) =>
        {
            await Task.Delay(1);
            foundry.Properties["result"] = "Completed";
        });

        await foundry.ForgeAsync();
        return "Success";
    }
}