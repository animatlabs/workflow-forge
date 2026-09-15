using System;
using WorkflowForge.Extensions.Resilience.Strategies;
using WorkflowForge.Loggers;

namespace WorkflowForge.Extensions.Resilience.Tests;

public class RetryMiddlewareFactoryShould
{
    [Fact]
    public void CreateMiddleware_GivenWithFixedInterval()
    {
        var logger = NullLogger.Instance;
        var middleware = RetryMiddleware.WithFixedInterval(logger, TimeSpan.FromMilliseconds(100));

        Assert.NotNull(middleware);
    }

    [Fact]
    public void CreateMiddleware_GivenWithFixedIntervalAndCustomAttempts()
    {
        var logger = NullLogger.Instance;
        var middleware = RetryMiddleware.WithFixedInterval(logger, TimeSpan.FromMilliseconds(100), maxAttempts: 5);

        Assert.NotNull(middleware);
    }

    [Fact]
    public void CreateMiddleware_GivenWithFixedIntervalAndPredicate()
    {
        var logger = NullLogger.Instance;
        var middleware = RetryMiddleware.WithFixedInterval(logger, TimeSpan.FromMilliseconds(100),
            retryPredicate: ex => ex is InvalidOperationException);

        Assert.NotNull(middleware);
    }

    [Fact]
    public void CreateMiddleware_GivenWithExponentialBackoff()
    {
        var logger = NullLogger.Instance;
        var middleware = RetryMiddleware.WithExponentialBackoff(
            logger,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromSeconds(30));

        Assert.NotNull(middleware);
    }

    [Fact]
    public void CreateMiddleware_GivenWithExponentialBackoffAndCustomAttempts()
    {
        var logger = NullLogger.Instance;
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
        var logger = NullLogger.Instance;
        var middleware = RetryMiddleware.WithRandomInterval(
            logger,
            TimeSpan.FromMilliseconds(50),
            TimeSpan.FromMilliseconds(500));

        Assert.NotNull(middleware);
    }

    [Fact]
    public void CreateMiddleware_GivenWithRandomIntervalAndCustomAttempts()
    {
        var logger = NullLogger.Instance;
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
        var logger = NullLogger.Instance;
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
