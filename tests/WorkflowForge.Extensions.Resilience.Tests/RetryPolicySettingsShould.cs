using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using WorkflowForge.Extensions.Resilience.Configurations;

namespace WorkflowForge.Extensions.Resilience.Tests;

public class RetryPolicySettingsShould
{
    [Fact]
    public void HaveCorrectDefaults_GivenDefaultConstructor()
    {
        var settings = new RetryPolicySettings();

        Assert.Equal(RetryStrategyType.None, settings.StrategyType);
        Assert.Equal(3, settings.MaxAttempts);
        Assert.Equal(TimeSpan.FromMilliseconds(100), settings.BaseDelay);
        Assert.Equal(TimeSpan.FromSeconds(30), settings.MaxDelay);
        Assert.Equal(2.0, settings.BackoffMultiplier);
        Assert.True(settings.UseJitter);
    }

    [Fact]
    public void ReturnNoValidationErrors_GivenValidSettings()
    {
        var settings = new RetryPolicySettings();
        var context = new ValidationContext(settings);

        var results = settings.Validate(context).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void ReturnValidationError_GivenMaxDelayLessThanBaseDelay()
    {
        var settings = new RetryPolicySettings
        {
            BaseDelay = TimeSpan.FromSeconds(10),
            MaxDelay = TimeSpan.FromSeconds(1)
        };
        var context = new ValidationContext(settings);

        var results = settings.Validate(context).ToList();

        Assert.Contains(results, r => r.MemberNames.Contains("MaxDelay"));
    }

    [Fact]
    public void ReturnValidationError_GivenExponentialBackoffWithLowMultiplier()
    {
        var settings = new RetryPolicySettings
        {
            StrategyType = RetryStrategyType.ExponentialBackoff,
            BackoffMultiplier = 0.5
        };
        var context = new ValidationContext(settings);

        var results = settings.Validate(context).ToList();

        Assert.Contains(results, r => r.MemberNames.Contains("BackoffMultiplier"));
    }

    [Fact]
    public void ReturnValidationError_GivenNonNoneStrategyWithZeroAttempts()
    {
        var settings = new RetryPolicySettings
        {
            StrategyType = RetryStrategyType.ExponentialBackoff,
            MaxAttempts = 0,
            BackoffMultiplier = 2.0
        };
        var context = new ValidationContext(settings);

        var results = settings.Validate(context).ToList();

        Assert.Contains(results, r => r.MemberNames.Contains("MaxAttempts"));
    }

    [Fact]
    public void ReturnNoValidationErrors_GivenNoneStrategyWithZeroAttempts()
    {
        var settings = new RetryPolicySettings
        {
            StrategyType = RetryStrategyType.None,
            MaxAttempts = 0
        };
        var context = new ValidationContext(settings);

        var results = settings.Validate(context).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void ProduceIndependentCopy_GivenClone()
    {
        var original = new RetryPolicySettings
        {
            StrategyType = RetryStrategyType.ExponentialBackoff,
            MaxAttempts = 5,
            BaseDelay = TimeSpan.FromSeconds(2),
            MaxDelay = TimeSpan.FromMinutes(1),
            BackoffMultiplier = 3.0,
            UseJitter = false
        };

        var clone = original.Clone();

        Assert.Equal(RetryStrategyType.ExponentialBackoff, clone.StrategyType);
        Assert.Equal(5, clone.MaxAttempts);
        Assert.Equal(TimeSpan.FromSeconds(2), clone.BaseDelay);
        Assert.Equal(TimeSpan.FromMinutes(1), clone.MaxDelay);
        Assert.Equal(3.0, clone.BackoffMultiplier);
        Assert.False(clone.UseJitter);

        // Mutate clone, verify original unchanged
        clone.MaxAttempts = 99;
        Assert.Equal(5, original.MaxAttempts);
    }

    [Fact]
    public void ReturnCorrectPreset_GivenNoRetry()
    {
        var preset = RetryPolicySettings.NoRetry;

        Assert.Equal(RetryStrategyType.None, preset.StrategyType);
        Assert.Equal(0, preset.MaxAttempts);
    }

    [Fact]
    public void ReturnCorrectPreset_GivenDefaultExponentialBackoff()
    {
        var preset = RetryPolicySettings.DefaultExponentialBackoff;

        Assert.Equal(RetryStrategyType.ExponentialBackoff, preset.StrategyType);
        Assert.Equal(3, preset.MaxAttempts);
        Assert.Equal(TimeSpan.FromMilliseconds(100), preset.BaseDelay);
        Assert.Equal(TimeSpan.FromSeconds(30), preset.MaxDelay);
        Assert.Equal(2.0, preset.BackoffMultiplier);
        Assert.True(preset.UseJitter);
    }
}
