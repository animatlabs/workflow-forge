using System;
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
