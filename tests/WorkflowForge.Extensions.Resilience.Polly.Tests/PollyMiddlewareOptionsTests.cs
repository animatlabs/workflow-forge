using System;
using System.Collections.Generic;
using WorkflowForge.Extensions.Resilience.Polly.Options;
using Xunit;

namespace WorkflowForge.Extensions.Resilience.Polly.Tests;

public class PollyMiddlewareOptionsTests
{
    [Fact]
    public void Constructor_Default_SetsDefaultSectionName()
    {
        var options = new PollyMiddlewareOptions();

        Assert.Equal(PollyMiddlewareOptions.DefaultSectionName, options.SectionName);
    }

    [Fact]
    public void Constructor_CustomSectionName_SetsSectionName()
    {
        var options = new PollyMiddlewareOptions("Custom:Polly");

        Assert.Equal("Custom:Polly", options.SectionName);
    }

    [Fact]
    public void Default_Retry_IsEnabled()
    {
        var options = new PollyMiddlewareOptions();

        Assert.True(options.Retry.IsEnabled);
        Assert.Equal(3, options.Retry.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(1), options.Retry.BaseDelay);
        Assert.True(options.Retry.UseJitter);
    }

    [Fact]
    public void Default_CircuitBreaker_IsDisabled()
    {
        var options = new PollyMiddlewareOptions();

        Assert.False(options.CircuitBreaker.IsEnabled);
        Assert.Equal(5, options.CircuitBreaker.FailureThreshold);
        Assert.Equal(TimeSpan.FromSeconds(30), options.CircuitBreaker.BreakDuration);
    }

    [Fact]
    public void Default_Timeout_IsDisabled()
    {
        var options = new PollyMiddlewareOptions();

        Assert.False(options.Timeout.IsEnabled);
        Assert.Equal(TimeSpan.FromSeconds(30), options.Timeout.DefaultTimeout);
    }

    [Fact]
    public void Default_RateLimiter_IsDisabled()
    {
        var options = new PollyMiddlewareOptions();

        Assert.False(options.RateLimiter.IsEnabled);
        Assert.Equal(100, options.RateLimiter.PermitLimit);
    }

    [Fact]
    public void Default_EnableComprehensivePolicies_IsFalse()
    {
        var options = new PollyMiddlewareOptions();

        Assert.False(options.EnableComprehensivePolicies);
    }

    [Fact]
    public void Default_EnableDetailedLogging_IsTrue()
    {
        var options = new PollyMiddlewareOptions();

        Assert.True(options.EnableDetailedLogging);
    }

    [Fact]
    public void Default_Enabled_IsTrue()
    {
        var options = new PollyMiddlewareOptions();

        Assert.True(options.Enabled);
    }

    [Fact]
    public void Default_DefaultTags_IsEmpty()
    {
        var options = new PollyMiddlewareOptions();

        Assert.NotNull(options.DefaultTags);
        Assert.Empty(options.DefaultTags);
    }

    [Fact]
    public void PropertySetters_CanSetAllProperties()
    {
        var options = new PollyMiddlewareOptions
        {
            Enabled = false,
            EnableComprehensivePolicies = true,
            EnableDetailedLogging = false,
            DefaultTags = new Dictionary<string, string> { ["key"] = "value" }
        };

        options.Retry.IsEnabled = false;
        options.Retry.MaxRetryAttempts = 5;
        options.Retry.BaseDelay = TimeSpan.FromSeconds(2);
        options.CircuitBreaker.IsEnabled = true;
        options.CircuitBreaker.FailureThreshold = 10;
        options.Timeout.IsEnabled = true;
        options.Timeout.DefaultTimeout = TimeSpan.FromMinutes(1);
        options.RateLimiter.IsEnabled = true;
        options.RateLimiter.PermitLimit = 50;

        Assert.False(options.Enabled);
        Assert.True(options.EnableComprehensivePolicies);
        Assert.False(options.EnableDetailedLogging);
        Assert.Single(options.DefaultTags);
        Assert.Equal("value", options.DefaultTags["key"]);
        Assert.False(options.Retry.IsEnabled);
        Assert.Equal(5, options.Retry.MaxRetryAttempts);
        Assert.True(options.CircuitBreaker.IsEnabled);
        Assert.Equal(10, options.CircuitBreaker.FailureThreshold);
        Assert.True(options.Timeout.IsEnabled);
        Assert.Equal(TimeSpan.FromMinutes(1), options.Timeout.DefaultTimeout);
        Assert.True(options.RateLimiter.IsEnabled);
        Assert.Equal(50, options.RateLimiter.PermitLimit);
    }

    [Fact]
    public void Validate_WhenRetryDisabled_ReturnsEmpty()
    {
        var options = new PollyMiddlewareOptions { Retry = { IsEnabled = false } };

        var errors = options.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WhenRetryEnabledWithValidSettings_ReturnsEmpty()
    {
        var options = new PollyMiddlewareOptions
        {
            Retry = { IsEnabled = true, MaxRetryAttempts = 5, BaseDelay = TimeSpan.FromSeconds(1) }
        };

        var errors = options.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WhenRetryMaxAttemptsOutOfRange_ReturnsError()
    {
        var options = new PollyMiddlewareOptions
        {
            Retry = { IsEnabled = true, MaxRetryAttempts = 101 }
        };

        var errors = options.Validate();

        Assert.Single(errors);
        Assert.Contains("MaxRetryAttempts", errors[0]);
    }

    [Fact]
    public void Validate_WhenRetryBaseDelayOutOfRange_ReturnsError()
    {
        var options = new PollyMiddlewareOptions
        {
            Retry = { IsEnabled = true, BaseDelay = TimeSpan.FromMinutes(15) }
        };

        var errors = options.Validate();

        Assert.Single(errors);
        Assert.Contains("BaseDelay", errors[0]);
    }

    [Fact]
    public void Validate_WhenCircuitBreakerDisabled_ReturnsEmpty()
    {
        var options = new PollyMiddlewareOptions { CircuitBreaker = { IsEnabled = false } };

        var errors = options.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WhenCircuitBreakerFailureThresholdOutOfRange_ReturnsError()
    {
        var options = new PollyMiddlewareOptions
        {
            CircuitBreaker = { IsEnabled = true, FailureThreshold = 1001 }
        };

        var errors = options.Validate();

        Assert.Single(errors);
        Assert.Contains("FailureThreshold", errors[0]);
    }

    [Fact]
    public void Validate_WhenCircuitBreakerBreakDurationOutOfRange_ReturnsError()
    {
        var options = new PollyMiddlewareOptions
        {
            CircuitBreaker = { IsEnabled = true, BreakDuration = TimeSpan.FromHours(2) }
        };

        var errors = options.Validate();

        Assert.Single(errors);
        Assert.Contains("BreakDuration", errors[0]);
    }

    [Fact]
    public void Validate_WhenTimeoutDisabled_ReturnsEmpty()
    {
        var options = new PollyMiddlewareOptions { Timeout = { IsEnabled = false } };

        var errors = options.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WhenTimeoutDefaultTimeoutOutOfRange_ReturnsError()
    {
        var options = new PollyMiddlewareOptions
        {
            Timeout = { IsEnabled = true, DefaultTimeout = TimeSpan.FromDays(2) }
        };

        var errors = options.Validate();

        Assert.Single(errors);
        Assert.Contains("DefaultTimeout", errors[0]);
    }

    [Fact]
    public void Validate_WhenRateLimiterDisabled_ReturnsEmpty()
    {
        var options = new PollyMiddlewareOptions { RateLimiter = { IsEnabled = false } };

        var errors = options.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WhenRateLimiterPermitLimitOutOfRange_ReturnsError()
    {
        var options = new PollyMiddlewareOptions
        {
            RateLimiter = { IsEnabled = true, PermitLimit = 0 }
        };

        var errors = options.Validate();

        Assert.Single(errors);
        Assert.Contains("PermitLimit", errors[0]);
    }

    [Fact]
    public void Validate_WhenMultipleInvalid_ReturnsAllErrors()
    {
        var options = new PollyMiddlewareOptions
        {
            Retry = { IsEnabled = true, MaxRetryAttempts = 150 },
            CircuitBreaker = { IsEnabled = true, FailureThreshold = 0 },
            Timeout = { IsEnabled = true, DefaultTimeout = TimeSpan.Zero },
            RateLimiter = { IsEnabled = true, PermitLimit = 2000000 }
        };

        var errors = options.Validate();

        Assert.True(errors.Count >= 2);
    }

    [Fact]
    public void Clone_CreatesDeepCopy()
    {
        var options = new PollyMiddlewareOptions
        {
            Enabled = false,
            Retry = { MaxRetryAttempts = 7 },
            DefaultTags = new Dictionary<string, string> { ["tag"] = "value" }
        };

        var clone = (PollyMiddlewareOptions)options.Clone();

        Assert.NotSame(options, clone);
        Assert.Equal(options.Enabled, clone.Enabled);
        Assert.Equal(options.Retry.MaxRetryAttempts, clone.Retry.MaxRetryAttempts);
        Assert.NotSame(options.DefaultTags, clone.DefaultTags);
        Assert.Equal(options.DefaultTags["tag"], clone.DefaultTags["tag"]);
    }

    [Fact]
    public void PollyRetrySettings_Clone_CreatesCopy()
    {
        var settings = new PollyRetrySettings
        {
            IsEnabled = true,
            MaxRetryAttempts = 5,
            BaseDelay = TimeSpan.FromSeconds(2),
            UseJitter = false
        };

        var clone = settings.Clone();

        Assert.NotSame(settings, clone);
        Assert.Equal(settings.IsEnabled, clone.IsEnabled);
        Assert.Equal(settings.MaxRetryAttempts, clone.MaxRetryAttempts);
        Assert.Equal(settings.BaseDelay, clone.BaseDelay);
        Assert.Equal(settings.UseJitter, clone.UseJitter);
    }

    [Fact]
    public void PollyCircuitBreakerSettings_Clone_CreatesCopy()
    {
        var settings = new PollyCircuitBreakerSettings
        {
            IsEnabled = true,
            FailureThreshold = 10,
            BreakDuration = TimeSpan.FromMinutes(1),
            MinimumThroughput = 20
        };

        var clone = settings.Clone();

        Assert.NotSame(settings, clone);
        Assert.Equal(settings.IsEnabled, clone.IsEnabled);
        Assert.Equal(settings.FailureThreshold, clone.FailureThreshold);
        Assert.Equal(settings.BreakDuration, clone.BreakDuration);
        Assert.Equal(settings.MinimumThroughput, clone.MinimumThroughput);
    }

    [Fact]
    public void PollyTimeoutSettings_Clone_CreatesCopy()
    {
        var settings = new PollyTimeoutSettings
        {
            IsEnabled = true,
            DefaultTimeout = TimeSpan.FromMinutes(5),
            UseOptimisticTimeout = false
        };

        var clone = settings.Clone();

        Assert.NotSame(settings, clone);
        Assert.Equal(settings.IsEnabled, clone.IsEnabled);
        Assert.Equal(settings.DefaultTimeout, clone.DefaultTimeout);
        Assert.Equal(settings.UseOptimisticTimeout, clone.UseOptimisticTimeout);
    }

    [Fact]
    public void PollyRateLimiterSettings_Clone_CreatesCopy()
    {
        var settings = new PollyRateLimiterSettings
        {
            IsEnabled = true,
            PermitLimit = 200,
            Window = TimeSpan.FromSeconds(30),
            QueueLimit = 10
        };

        var clone = settings.Clone();

        Assert.NotSame(settings, clone);
        Assert.Equal(settings.IsEnabled, clone.IsEnabled);
        Assert.Equal(settings.PermitLimit, clone.PermitLimit);
        Assert.Equal(settings.Window, clone.Window);
        Assert.Equal(settings.QueueLimit, clone.QueueLimit);
    }
}
