using System;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Resilience.Strategies;

namespace WorkflowForge.Extensions.Resilience.Tests;

public class RetryMiddlewareFactoryShould
{
    [Fact]
    public void CreateMiddleware_GivenWithFixedInterval()
    {
        var logger = WorkflowForgeLoggers.Null;
        var middleware = RetryMiddleware.WithFixedInterval(logger, TimeSpan.FromMilliseconds(100));

        Assert.NotNull(middleware);
    }

    [Fact]
    public void CreateMiddleware_GivenWithFixedIntervalAndCustomAttempts()
    {
        var logger = WorkflowForgeLoggers.Null;
        var middleware = RetryMiddleware.WithFixedInterval(logger, TimeSpan.FromMilliseconds(100), maxAttempts: 5);

        Assert.NotNull(middleware);
    }

    [Fact]
    public void CreateMiddleware_GivenWithFixedIntervalAndPredicate()
    {
        var logger = WorkflowForgeLoggers.Null;
        var middleware = RetryMiddleware.WithFixedInterval(logger, TimeSpan.FromMilliseconds(100),
            retryPredicate: ex => ex is InvalidOperationException);

        Assert.NotNull(middleware);
    }

    [Fact]
    public void CreateMiddleware_GivenWithExponentialBackoff()
    {
        var logger = WorkflowForgeLoggers.Null;
        var middleware = RetryMiddleware.WithExponentialBackoff(
            logger,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromSeconds(30));

        Assert.NotNull(middleware);
    }

    [Fact]
    public void CreateMiddleware_GivenWithExponentialBackoffAndCustomAttempts()
    {
        var logger = WorkflowForgeLoggers.Null;
        var middleware = RetryMiddleware.WithExponentialBackoff(
            logger,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromSeconds(30),
            maxAttempts: 5);

        Assert.NotNull(middleware);
    }

    [Fact]
    public void CreateMiddleware_GivenWithRandomInterval()
    {
        var logger = WorkflowForgeLoggers.Null;
        var middleware = RetryMiddleware.WithRandomInterval(
            logger,
            TimeSpan.FromMilliseconds(50),
            TimeSpan.FromMilliseconds(500));

        Assert.NotNull(middleware);
    }

    [Fact]
    public void CreateMiddleware_GivenWithRandomIntervalAndCustomAttempts()
    {
        var logger = WorkflowForgeLoggers.Null;
        var middleware = RetryMiddleware.WithRandomInterval(
            logger,
            TimeSpan.FromMilliseconds(50),
            TimeSpan.FromMilliseconds(500),
            maxAttempts: 5);

        Assert.NotNull(middleware);
    }

    [Fact]
    public void CreateMiddleware_GivenDefault()
    {
        var logger = WorkflowForgeLoggers.Null;
        var middleware = RetryMiddleware.Default(logger);

        Assert.NotNull(middleware);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullStrategy()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new RetryMiddleware(null!));
    }
}

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
