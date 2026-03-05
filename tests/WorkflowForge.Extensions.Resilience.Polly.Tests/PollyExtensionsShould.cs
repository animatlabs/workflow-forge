using System;
using WorkflowForge.Extensions.Resilience.Polly.Options;
using WorkflowForge.Testing;

namespace WorkflowForge.Extensions.Resilience.Polly.Tests;

public class PollyExtensionsShould
{
    [Fact]
    public void NotAddMiddleware_GivenUsePollyFromSettingsWhenDisabled()
    {
        var foundry = new FakeWorkflowFoundry();
        var options = new PollyMiddlewareOptions
        {
            Enabled = false
        };

        var result = foundry.UsePollyFromSettings(options);

        Assert.Same(foundry, result);
        Assert.Empty(foundry.Middlewares);
    }

    [Fact]
    public void AddComprehensiveMiddleware_GivenUsePollyFromSettingsWhenComprehensiveEnabled()
    {
        var foundry = new FakeWorkflowFoundry();
        var options = new PollyMiddlewareOptions
        {
            Enabled = true,
            EnableComprehensivePolicies = true,
            Retry = { MaxRetryAttempts = 2 },
            CircuitBreaker = { FailureThreshold = 3, BreakDuration = TimeSpan.FromSeconds(1) },
            Timeout = { DefaultTimeout = TimeSpan.FromMilliseconds(100) }
        };

        var result = foundry.UsePollyFromSettings(options);

        Assert.Same(foundry, result);
        Assert.Single(foundry.Middlewares);
        var middleware = Assert.IsType<PollyMiddleware>(foundry.Middlewares[0]);
        Assert.Contains("Comprehensive", middleware.Name);
    }

    [Fact]
    public void AddOnlyEnabledMiddlewares_GivenUsePollyFromSettingsWhenIndividualPoliciesConfigured()
    {
        var foundry = new FakeWorkflowFoundry();
        var options = new PollyMiddlewareOptions
        {
            Enabled = true,
            EnableComprehensivePolicies = false,
            Retry =
            {
                IsEnabled = true,
                MaxRetryAttempts = 3,
                BaseDelay = TimeSpan.FromMilliseconds(10)
            },
            CircuitBreaker = { IsEnabled = false },
            Timeout =
            {
                IsEnabled = true,
                DefaultTimeout = TimeSpan.FromMilliseconds(50)
            }
        };

        var result = foundry.UsePollyFromSettings(options);

        Assert.Same(foundry, result);
        Assert.Equal(2, foundry.Middlewares.Count);
        Assert.Contains(foundry.Middlewares, m => Assert.IsType<PollyMiddleware>(m).Name.StartsWith("PollyRetry", StringComparison.Ordinal));
        Assert.Contains(foundry.Middlewares, m => Assert.IsType<PollyMiddleware>(m).Name.StartsWith("PollyTimeout", StringComparison.Ordinal));
    }
}
