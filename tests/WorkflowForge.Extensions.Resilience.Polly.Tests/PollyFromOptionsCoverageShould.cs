using System;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Resilience.Abstractions;
using WorkflowForge.Extensions.Resilience.Polly.Options;
using WorkflowForge.Testing;
using Xunit;

namespace WorkflowForge.Extensions.Resilience.Polly.Tests;

public class PollyFromOptionsCoverageShould
{
    [Fact]
    public void ThrowArgumentNullException_GivenNullOptions()
        => Assert.Throws<ArgumentNullException>(() => PollyMiddleware.FromOptions(null!, TestNullLogger.Instance));

    [Fact]
    public void ThrowArgumentNullException_GivenNullLogger()
        => Assert.Throws<ArgumentNullException>(() => PollyMiddleware.FromOptions(new PollyMiddlewareOptions(), null!));

    [Fact]
    public void NameThePipelineDisabled_GivenTheExtensionIsOff()
    {
        var middleware = PollyMiddleware.FromOptions(
            new PollyMiddlewareOptions { Enabled = false },
            TestNullLogger.Instance);

        Assert.Equal("PollyDisabled", middleware.Name);
    }

    [Fact]
    public void UseTheComprehensivePipeline_GivenEnableComprehensivePolicies()
    {
        var middleware = PollyMiddleware.FromOptions(
            new PollyMiddlewareOptions { EnableComprehensivePolicies = true },
            TestNullLogger.Instance);

        Assert.Equal("PollyComprehensive", middleware.Name);
    }

    [Theory]
    [InlineData("Exponential")]
    [InlineData("Linear")]
    [InlineData("Constant")]
    [InlineData("something-else")]
    [InlineData(null)]
    public void BuildARetryPipeline_GivenAnyBackoffType(string? backoffType)
    {
        var options = new PollyMiddlewareOptions
        {
            Retry = { IsEnabled = true, BackoffType = backoffType! }
        };

        var middleware = PollyMiddleware.FromOptions(options, TestNullLogger.Instance);

        Assert.Equal("Polly(retry)", middleware.Name);
    }

    [Fact]
    public void BuildACircuitBreakerOnlyPipeline_GivenOnlyTheBreakerIsEnabled()
    {
        var options = new PollyMiddlewareOptions
        {
            Retry = { IsEnabled = false },
            CircuitBreaker = { IsEnabled = true, FailureThreshold = 20, MinimumThroughput = 1 }
        };

        var middleware = PollyMiddleware.FromOptions(options, TestNullLogger.Instance);

        Assert.Equal("Polly(circuitBreaker)", middleware.Name);
    }

    [Fact]
    public void ResolveEveryRegisteredService_GivenAddWorkflowForgePolly()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWorkflowForgeLogger>(TestNullLogger.Instance);
        services.AddWorkflowForgePolly(opts => opts.Retry.MaxRetryAttempts = 4);

        using var provider = services.BuildServiceProvider();

        Assert.Equal(4, provider.GetRequiredService<PollyMiddlewareOptions>().Retry.MaxRetryAttempts);
        Assert.NotNull(provider.GetRequiredService<PollyMiddleware>());
        Assert.NotNull(provider.GetRequiredService<IWorkflowResilienceStrategy>());
    }

    [Fact]
    public void ResolveANoOpStrategy_GivenTheExtensionIsDisabled()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWorkflowForgeLogger>(TestNullLogger.Instance);
        services.AddWorkflowForgePolly(opts => opts.Enabled = false);

        using var provider = services.BuildServiceProvider();

        Assert.Equal("NoOp", provider.GetRequiredService<IWorkflowResilienceStrategy>().Name);
    }

    [Fact]
    public void ResolveAComprehensiveStrategy_GivenComprehensivePoliciesAreEnabled()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWorkflowForgeLogger>(TestNullLogger.Instance);
        services.AddWorkflowForgePolly(opts => opts.EnableComprehensivePolicies = true);

        using var provider = services.BuildServiceProvider();

        Assert.NotEqual("NoOp", provider.GetRequiredService<IWorkflowResilienceStrategy>().Name);
    }
}
