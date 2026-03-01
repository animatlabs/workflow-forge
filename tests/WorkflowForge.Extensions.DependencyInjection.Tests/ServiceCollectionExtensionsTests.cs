using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.DependencyInjection;
using WorkflowForge.Loggers;
using WorkflowForge.Options;
using WorkflowForge.Options.Middleware;

namespace WorkflowForge.Extensions.DependencyInjection.Tests;

/// <summary>
/// Comprehensive tests for ServiceCollectionExtensions in WorkflowForge.Extensions.DependencyInjection.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddWorkflowForge_WithNullServices_ThrowsArgumentNullException()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddWorkflowForge(config));
    }

    [Fact]
    public void AddWorkflowForge_WithNullConfiguration_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddWorkflowForge((IConfiguration)null!));
    }

    [Fact]
    public void AddWorkflowForge_WithConfiguration_RegistersOptions()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["WorkflowForge:MaxConcurrentWorkflows"] = "10",
                ["WorkflowForge:ContinueOnError"] = "true"
            })
            .Build();

        services.AddWorkflowForge(config);
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<WorkflowForgeOptions>>().Value;

        Assert.Equal(10, options.MaxConcurrentWorkflows);
        Assert.True(options.ContinueOnError);
    }

    [Fact]
    public void AddWorkflowForge_WithNullConfiguration_ThrowsArgumentNullException_WhenOverloadWithConfig()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddWorkflowForge(null!, null, null, null, null));
    }

    [Fact]
    public void AddWorkflowForge_WithCustomSectionNames_BindsCorrectly()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MyApp:Core:MaxConcurrentWorkflows"] = "5",
                ["MyApp:Timing:Enabled"] = "false",
                ["MyApp:Logging:MinimumLevel"] = "Warning",
                ["MyApp:ErrorHandling:Enabled"] = "true"
            })
            .Build();

        services.AddWorkflowForge(config,
            coreSectionName: "MyApp:Core",
            timingSectionName: "MyApp:Timing",
            loggingSectionName: "MyApp:Logging",
            errorHandlingSectionName: "MyApp:ErrorHandling");

        var provider = services.BuildServiceProvider();
        var coreOptions = provider.GetRequiredService<IOptions<WorkflowForgeOptions>>().Value;
        var timingOptions = provider.GetRequiredService<IOptions<TimingMiddlewareOptions>>().Value;
        var loggingOptions = provider.GetRequiredService<IOptions<LoggingMiddlewareOptions>>().Value;

        Assert.Equal(5, coreOptions.MaxConcurrentWorkflows);
        Assert.False(timingOptions.Enabled);
        Assert.Equal("Warning", loggingOptions.MinimumLevel);
    }

    [Fact]
    public void AddWorkflowForge_WithNullServices_ThrowsArgumentNullException_WhenOverloadWithActions()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddWorkflowForge(config => config.MaxConcurrentWorkflows = 5));
    }

    [Fact]
    public void AddWorkflowForge_WithConfigureCore_RegistersConfiguredOptions()
    {
        var services = new ServiceCollection();

        services.AddWorkflowForge(
            configureCore: opts => opts.MaxConcurrentWorkflows = 7);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<WorkflowForgeOptions>>().Value;

        Assert.Equal(7, options.MaxConcurrentWorkflows);
    }

    [Fact]
    public void AddWorkflowForge_WithConfigureTiming_RegistersConfiguredOptions()
    {
        var services = new ServiceCollection();

        services.AddWorkflowForge(
            configureCore: null,
            configureTiming: opts => opts.Enabled = false);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<TimingMiddlewareOptions>>().Value;

        Assert.False(options.Enabled);
    }

    [Fact]
    public void AddWorkflowForge_WithConfigureLogging_RegistersConfiguredOptions()
    {
        var services = new ServiceCollection();

        services.AddWorkflowForge(
            configureCore: null,
            configureTiming: null,
            configureLogging: opts => opts.MinimumLevel = "Debug");

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<LoggingMiddlewareOptions>>().Value;

        Assert.Equal("Debug", options.MinimumLevel);
    }

    [Fact]
    public void AddWorkflowForge_WithConfigureErrorHandling_RegistersConfiguredOptions()
    {
        var services = new ServiceCollection();

        services.AddWorkflowForge(
            configureCore: null,
            configureTiming: null,
            configureLogging: null,
            configureErrorHandling: opts => opts.Enabled = false);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ErrorHandlingMiddlewareOptions>>().Value;

        Assert.False(options.Enabled);
    }

    [Fact]
    public void AddWorkflowForge_WithAllNullActions_StillRegistersOptions()
    {
        var services = new ServiceCollection();

        services.AddWorkflowForge(
            configureCore: null,
            configureTiming: null,
            configureLogging: null,
            configureErrorHandling: null);

        var provider = services.BuildServiceProvider();
        var coreOptions = provider.GetRequiredService<IOptions<WorkflowForgeOptions>>().Value;
        var timingOptions = provider.GetRequiredService<IOptions<TimingMiddlewareOptions>>().Value;
        var loggingOptions = provider.GetRequiredService<IOptions<LoggingMiddlewareOptions>>().Value;
        var errorHandlingOptions = provider.GetRequiredService<IOptions<ErrorHandlingMiddlewareOptions>>().Value;

        Assert.NotNull(coreOptions);
        Assert.NotNull(timingOptions);
        Assert.NotNull(loggingOptions);
        Assert.NotNull(errorHandlingOptions);
    }

    [Fact]
    public void AddWorkflowForge_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

        var result = services.AddWorkflowForge(config);

        Assert.Same(services, result);
    }

    [Fact]
    public void AddWorkflowSmith_WithNullServices_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddWorkflowSmith());
    }

    [Fact]
    public void AddWorkflowSmith_RegistersWorkflowSmith()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWorkflowForgeLogger>(_ => new ConsoleLogger("Test"));
        services.AddWorkflowForge(config => config.MaxConcurrentWorkflows = 2);

        services.AddWorkflowSmith();

        var provider = services.BuildServiceProvider();
        var smith = provider.GetRequiredService<IWorkflowSmith>();

        Assert.NotNull(smith);
    }

    [Fact]
    public void AddWorkflowSmith_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWorkflowForgeLogger>(_ => new ConsoleLogger("Test"));
        services.AddWorkflowForge();

        var result = services.AddWorkflowSmith();

        Assert.Same(services, result);
    }

    [Fact]
    public void AddWorkflowSmith_WithoutLogger_Throws()
    {
        var services = new ServiceCollection();
        services.AddWorkflowForge(config => config.MaxConcurrentWorkflows = 2);
        services.AddWorkflowSmith();

        var provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IWorkflowSmith>());
    }
}
