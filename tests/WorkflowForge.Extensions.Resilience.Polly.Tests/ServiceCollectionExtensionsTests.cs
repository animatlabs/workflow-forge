using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Resilience.Abstractions;
using WorkflowForge.Extensions.Resilience.Polly;
using WorkflowForge.Extensions.Resilience.Polly.Options;
using WorkflowForge.Testing;
using Xunit;

namespace WorkflowForge.Extensions.Resilience.Polly.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddWorkflowForgePolly_RegistersPollyMiddlewareOptions()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWorkflowForgeLogger>(TestNullLogger.Instance);

        var result = services.AddWorkflowForgePolly();

        Assert.Same(services, result);
        var provider = services.BuildServiceProvider();
        var options = provider.GetService<PollyMiddlewareOptions>();
        Assert.NotNull(options);
    }

    [Fact]
    public void AddWorkflowForgePolly_WithConfigureAction_AppliesConfiguration()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWorkflowForgeLogger>(TestNullLogger.Instance);

        services.AddWorkflowForgePolly(opts =>
        {
            opts.Enabled = false;
            opts.Retry.MaxRetryAttempts = 7;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<PollyMiddlewareOptions>();
        Assert.False(options.Enabled);
        Assert.Equal(7, options.Retry.MaxRetryAttempts);
    }

    [Fact]
    public void AddWorkflowForgePolly_RegistersPollyMiddleware()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWorkflowForgeLogger>(TestNullLogger.Instance);

        services.AddWorkflowForgePolly();

        var provider = services.BuildServiceProvider();
        var middleware = provider.GetService<PollyMiddleware>();
        Assert.NotNull(middleware);
    }

    [Fact]
    public void AddWorkflowForgePolly_RegistersIWorkflowResilienceStrategy()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWorkflowForgeLogger>(TestNullLogger.Instance);

        services.AddWorkflowForgePolly();

        var provider = services.BuildServiceProvider();
        var strategy = provider.GetService<IWorkflowResilienceStrategy>();
        Assert.NotNull(strategy);
    }

    [Fact]
    public async Task AddWorkflowForgePolly_ResolvedStrategy_ExecutesSuccessfully()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWorkflowForgeLogger>(TestNullLogger.Instance);

        services.AddWorkflowForgePolly();

        var provider = services.BuildServiceProvider();
        var strategy = provider.GetRequiredService<IWorkflowResilienceStrategy>();

        var result = await strategy.ExecuteAsync(() => System.Threading.Tasks.Task.FromResult(42), default);

        Assert.Equal(42, result);
    }

    [Fact]
    public void AddWorkflowForgePolly_ResolvedMiddleware_HasCorrectName()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWorkflowForgeLogger>(TestNullLogger.Instance);

        services.AddWorkflowForgePolly();

        var provider = services.BuildServiceProvider();
        var middleware = provider.GetRequiredService<PollyMiddleware>();

        Assert.NotNull(middleware.Name);
        Assert.Contains("Polly", middleware.Name);
    }

    [Fact]
    public void AddWorkflowForgePolly_WithConfiguration_BindsSettings()
    {
        var configData = new Dictionary<string, string?>
        {
            ["WorkflowForge:Polly:Enabled"] = "false",
            ["WorkflowForge:Polly:Retry:MaxRetryAttempts"] = "10",
            ["WorkflowForge:Polly:EnableComprehensivePolicies"] = "true"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IWorkflowForgeLogger>(TestNullLogger.Instance);

        services.AddWorkflowForgePolly(configuration, "WorkflowForge:Polly");

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<PollyMiddlewareOptions>();
        Assert.False(options.Enabled);
        Assert.Equal(10, options.Retry.MaxRetryAttempts);
        Assert.True(options.EnableComprehensivePolicies);
    }

    [Fact]
    public void AddWorkflowForgePolly_WithConfiguration_CustomSectionName_BindsCorrectly()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Custom:Polly:Retry:MaxRetryAttempts"] = "15"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IWorkflowForgeLogger>(TestNullLogger.Instance);

        services.AddWorkflowForgePolly(configuration, "Custom:Polly");

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<PollyMiddlewareOptions>();
        Assert.Equal(15, options.Retry.MaxRetryAttempts);
    }

    [Fact]
    public void AddWorkflowForgePolly_CalledMultipleTimes_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWorkflowForgeLogger>(TestNullLogger.Instance);

        services.AddWorkflowForgePolly();
        services.AddWorkflowForgePolly();
        services.AddWorkflowForgePolly();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<PollyMiddlewareOptions>();
        Assert.NotNull(options);
    }

    [Fact]
    public void AddWorkflowForgePolly_WithEnableComprehensivePolicies_ResolvesComprehensiveMiddleware()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWorkflowForgeLogger>(TestNullLogger.Instance);

        services.AddWorkflowForgePolly(opts =>
        {
            opts.EnableComprehensivePolicies = true;
            opts.Retry.IsEnabled = true;
            opts.CircuitBreaker.IsEnabled = true;
            opts.Timeout.IsEnabled = true;
        });

        var provider = services.BuildServiceProvider();
        var middleware = provider.GetRequiredService<PollyMiddleware>();

        Assert.Equal("PollyComprehensive", middleware.Name);
    }

    [Fact]
    public void AddWorkflowForgePolly_WithNullConfigureAction_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWorkflowForgeLogger>(TestNullLogger.Instance);

        services.AddWorkflowForgePolly(null);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<PollyMiddlewareOptions>();
        Assert.NotNull(options);
    }
}
