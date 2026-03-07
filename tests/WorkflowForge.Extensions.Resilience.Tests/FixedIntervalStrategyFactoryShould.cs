using System;
using WorkflowForge.Extensions.Resilience.Strategies;

namespace WorkflowForge.Extensions.Resilience.Tests;

public class FixedIntervalStrategyFactoryShould
{
    [Fact]
    public void CreateStrategy_GivenDefault()
    {
        var strategy = FixedIntervalStrategy.Default();

        Assert.NotNull(strategy);
    }

    [Fact]
    public void CreateStrategy_GivenDefaultWithCustomAttempts()
    {
        var strategy = FixedIntervalStrategy.Default(maxAttempts: 5);

        Assert.NotNull(strategy);
    }

    [Fact]
    public void CreateStrategy_GivenFast()
    {
        var strategy = FixedIntervalStrategy.Fast();

        Assert.NotNull(strategy);
    }

    [Fact]
    public void CreateStrategy_GivenFastWithCustomAttempts()
    {
        var strategy = FixedIntervalStrategy.Fast(maxAttempts: 10);

        Assert.NotNull(strategy);
    }

    [Fact]
    public void CreateStrategy_GivenSlow()
    {
        var strategy = FixedIntervalStrategy.Slow();

        Assert.NotNull(strategy);
    }

    [Fact]
    public void CreateStrategy_GivenSlowWithLogger()
    {
        var logger = WorkflowForgeLoggers.Null;
        var strategy = FixedIntervalStrategy.Slow(logger: logger);

        Assert.NotNull(strategy);
    }

    [Fact]
    public void ThrowArgumentOutOfRangeException_GivenNegativeInterval()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new FixedIntervalStrategy(TimeSpan.FromMilliseconds(-1), 3));
    }

    [Fact]
    public void ThrowArgumentOutOfRangeException_GivenZeroMaxAttempts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new FixedIntervalStrategy(TimeSpan.FromSeconds(1), 0));
    }

    [Fact]
    public void ReturnZeroDelay_GivenFirstAttempt()
    {
        var strategy = FixedIntervalStrategy.Default();

        var delay = strategy.GetRetryDelay(1, null);

        Assert.Equal(TimeSpan.Zero, delay);
    }

    [Fact]
    public void ReturnFixedDelay_GivenSubsequentAttempt()
    {
        var strategy = new FixedIntervalStrategy(TimeSpan.FromSeconds(1), 3);

        var delay = strategy.GetRetryDelay(2, null);

        Assert.Equal(TimeSpan.FromSeconds(1), delay);
    }
}
