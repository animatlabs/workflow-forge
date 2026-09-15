using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Resilience.Polly.Options;
using WorkflowForge.Testing;
using Xunit;

namespace WorkflowForge.Extensions.Resilience.Polly.Tests;

/// <summary>
/// Asserts that the Polly options change what the composed pipeline actually does.
/// </summary>
public class PollyOptionBehaviourShould
{
    private static async Task<int> CountAttemptsAsync(PollyMiddlewareOptions options)
    {
        var middleware = PollyMiddleware.FromOptions(options, TestNullLogger.Instance);
        var attempts = 0;

        using var foundry = WorkflowForge.CreateFoundry("PollyBehaviour");
        var workflow = WorkflowForge.CreateWorkflow("PollyBehaviour")
            .AddOperation("flaky", (_, _) =>
            {
                attempts++;
                throw new InvalidOperationException("always fails");
            })
            .Build();

        foundry.AddMiddleware(middleware);
        using var smith = WorkflowForge.CreateSmith();

        await Assert.ThrowsAnyAsync<Exception>(() => smith.ForgeAsync(workflow, foundry));
        return attempts;
    }

    [Fact]
    public async Task RetryTheConfiguredNumberOfTimes_GivenRetryEnabled()
    {
        var attempts = await CountAttemptsAsync(new PollyMiddlewareOptions
        {
            Retry = { IsEnabled = true, MaxRetryAttempts = 2, BaseDelay = TimeSpan.Zero }
        });

        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task RunTheOperationOnce_GivenRetryDisabled()
    {
        var attempts = await CountAttemptsAsync(new PollyMiddlewareOptions
        {
            Retry = { IsEnabled = false }
        });

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task RunTheOperationOnce_GivenTheWholeExtensionDisabled()
    {
        var attempts = await CountAttemptsAsync(new PollyMiddlewareOptions
        {
            Enabled = false,
            Retry = { IsEnabled = true, MaxRetryAttempts = 5, BaseDelay = TimeSpan.Zero }
        });

        Assert.Equal(1, attempts);
    }

    [Fact]
    public void NameThePipelineAfterTheEnabledStrategies()
    {
        var options = new PollyMiddlewareOptions
        {
            Retry = { IsEnabled = true },
            Timeout = { IsEnabled = true },
            CircuitBreaker = { IsEnabled = true }
        };

        var middleware = PollyMiddleware.FromOptions(options, TestNullLogger.Instance);

        Assert.Equal("Polly(timeout,retry,circuitBreaker)", middleware.Name);
    }

    [Fact]
    public async Task EnforceTheConfiguredTimeout_GivenTimeoutEnabled()
    {
        var options = new PollyMiddlewareOptions
        {
            Retry = { IsEnabled = false },
            Timeout = { IsEnabled = true, DefaultTimeout = TimeSpan.FromMilliseconds(100) }
        };

        var middleware = PollyMiddleware.FromOptions(options, TestNullLogger.Instance);

        using var foundry = WorkflowForge.CreateFoundry("PollyTimeout");
        var workflow = WorkflowForge.CreateWorkflow("PollyTimeout")
            .AddOperation("slow", async (_, ct) => await Task.Delay(TimeSpan.FromSeconds(10), ct))
            .Build();

        foundry.AddMiddleware(middleware);
        using var smith = WorkflowForge.CreateSmith();

        var started = DateTime.UtcNow;
        await Assert.ThrowsAnyAsync<Exception>(() => smith.ForgeAsync(workflow, foundry));

        Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void BindTheDefaultSection_GivenNoSectionNameIsSupplied()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWorkflowForgeLogger>(TestNullLogger.Instance);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
            {
                [PollyMiddlewareOptions.DefaultSectionName + ":Retry:MaxRetryAttempts"] = "7",
                [PollyMiddlewareOptions.DefaultSectionName + ":Timeout:IsEnabled"] = "true"
            })
            .Build();

        services.AddWorkflowForgePolly(configuration);
        using var provider = services.BuildServiceProvider();

        var settings = provider.GetRequiredService<PollyMiddlewareOptions>();
        Assert.Equal(7, settings.Retry.MaxRetryAttempts);
        Assert.True(settings.Timeout.IsEnabled);
    }
}
