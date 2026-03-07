using System;
using WorkflowForge.Extensions.Resilience.Strategies;

namespace WorkflowForge.Extensions.Resilience.Tests;

public class ExponentialBackoffStrategyFactoryShould
{
    [Fact]
    public void CreateStrategy_GivenDefault()
    {
        var strategy = ExponentialBackoffStrategy.Default();

        Assert.NotNull(strategy);
    }

    [Fact]
    public void CreateStrategy_GivenDefaultWithCustomAttempts()
    {
        var strategy = ExponentialBackoffStrategy.Default(maxAttempts: 5);

        Assert.NotNull(strategy);
    }

    [Fact]
    public void CreateStrategy_GivenForTransientErrors()
    {
        var strategy = ExponentialBackoffStrategy.ForTransientErrors();

        Assert.NotNull(strategy);
    }

    [Fact]
    public void CreateStrategy_GivenForTransientErrorsWithCustomAttempts()
    {
        var strategy = ExponentialBackoffStrategy.ForTransientErrors(maxAttempts: 10);

        Assert.NotNull(strategy);
    }

    [Fact]
    public void ThrowArgumentOutOfRangeException_GivenNegativeBaseDelay()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExponentialBackoffStrategy(
                TimeSpan.FromMilliseconds(-1),
                TimeSpan.FromSeconds(30),
                3));
    }

    [Fact]
    public void ThrowArgumentOutOfRangeException_GivenMaxDelayLessThanBaseDelay()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExponentialBackoffStrategy(
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(1),
                3));
    }

    [Fact]
    public void ThrowArgumentOutOfRangeException_GivenZeroMaxAttempts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExponentialBackoffStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(30),
                0));
    }

    [Fact]
    public void ThrowArgumentOutOfRangeException_GivenMultiplierLessThanOne()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExponentialBackoffStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(30),
                3,
                backoffMultiplier: 0.5));
    }

    [Fact]
    public void ReturnZeroDelay_GivenFirstAttempt()
    {
        var strategy = ExponentialBackoffStrategy.Default();

        var delay = strategy.GetRetryDelay(1, null);

        Assert.Equal(TimeSpan.Zero, delay);
    }

    [Fact]
    public void ReturnPositiveDelay_GivenSecondAttempt()
    {
        var strategy = new ExponentialBackoffStrategy(
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromSeconds(30),
            3,
            enableJitter: false);

        var delay = strategy.GetRetryDelay(2, null);

        Assert.True(delay > TimeSpan.Zero);
    }

    [Fact]
    public void CapDelayAtMaxDelay_GivenLargeAttemptNumber()
    {
        var maxDelay = TimeSpan.FromSeconds(1);
        var strategy = new ExponentialBackoffStrategy(
            TimeSpan.FromMilliseconds(100),
            maxDelay,
            10,
            enableJitter: false);

        var delay = strategy.GetRetryDelay(20, null);

        Assert.True(delay <= maxDelay);
    }
}
