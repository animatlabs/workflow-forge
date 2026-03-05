using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Extensions.Resilience.Strategies;
using WorkflowForge.Loggers;

namespace WorkflowForge.Extensions.Resilience.Tests;

public class ExponentialBackoffStrategyShould
{
    [Fact]
    public void SetPropertiesCorrectly_GivenValidParameters()
    {
        // Arrange
        var baseDelay = TimeSpan.FromMilliseconds(100);
        var maxDelay = TimeSpan.FromSeconds(10);
        const int maxAttempts = 3;
        const double multiplier = 2.0;

        // Act
        var strategy = new ExponentialBackoffStrategy(baseDelay, maxDelay, maxAttempts, multiplier);

        // Assert
        Assert.Equal("ExponentialBackoff", strategy.Name);
    }

    [Fact]
    public void ThrowArgumentException_GivenInvalidBaseDelay()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExponentialBackoffStrategy(TimeSpan.FromMilliseconds(-1), TimeSpan.FromSeconds(10), 3));
    }

    [Fact]
    public void ThrowArgumentException_GivenInvalidMaxDelay()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExponentialBackoffStrategy(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5), 3));
    }

    [Fact]
    public void ThrowArgumentException_GivenInvalidMaxAttempts()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExponentialBackoffStrategy(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(10), 0));
    }

    [Fact]
    public async Task ReturnTrue_GivenShouldRetryAsyncWithNullException()
    {
        // Arrange
        var strategy = new ExponentialBackoffStrategy(
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromSeconds(10),
            3);

        // Act
        var result = await strategy.ShouldRetryAsync(1, null, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ReturnFalse_GivenExponentialBackoffShouldRetryAsyncWithOperationCanceledException()
    {
        // Arrange
        var strategy = new ExponentialBackoffStrategy(
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromSeconds(10),
            3);
        var exception = new OperationCanceledException();

        // Act
        var result = await strategy.ShouldRetryAsync(1, exception, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ReturnTrue_GivenExponentialBackoffShouldRetryAsyncWithRegularException()
    {
        // Arrange
        var strategy = new ExponentialBackoffStrategy(
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromSeconds(10),
            3);
        var exception = new InvalidOperationException();

        // Act
        var result = await strategy.ShouldRetryAsync(1, exception, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CalculateExponentialBackoff_GivenGetRetryDelay()
    {
        // Arrange
        var baseDelay = TimeSpan.FromMilliseconds(100);
        var maxDelay = TimeSpan.FromSeconds(10);
        const int maxAttempts = 5;
        const double multiplier = 2.0;
        var strategy = new ExponentialBackoffStrategy(baseDelay, maxDelay, maxAttempts, multiplier, enableJitter: false);

        // Act
        var delay1 = strategy.GetRetryDelay(1, null); // Should be 0 for first attempt
        var delay2 = strategy.GetRetryDelay(2, null); // Should be base delay
        var delay3 = strategy.GetRetryDelay(3, null); // Should be base * multiplier

        // Assert
        Assert.Equal(TimeSpan.Zero, delay1);
        Assert.Equal(TimeSpan.FromMilliseconds(100), delay2);
        Assert.Equal(TimeSpan.FromMilliseconds(200), delay3);
    }

    [Fact]
    public void RespectMaxDelay_GivenGetRetryDelay()
    {
        // Arrange
        var baseDelay = TimeSpan.FromSeconds(1);
        var maxDelay = TimeSpan.FromSeconds(2);
        const int maxAttempts = 10;
        const double multiplier = 10.0;
        var strategy = new ExponentialBackoffStrategy(baseDelay, maxDelay, maxAttempts, multiplier);

        // Act
        var delay = strategy.GetRetryDelay(5, null);

        // Assert
        Assert.True(delay <= maxDelay);
    }

    [Fact]
    public async Task ExecuteOnce_GivenExponentialBackoffExecuteAsyncSuccessfulOperation()
    {
        // Arrange
        var strategy = new ExponentialBackoffStrategy(
            TimeSpan.FromMilliseconds(10),
            TimeSpan.FromSeconds(1),
            3);
        var executionCount = 0;

        // Act
        await strategy.ExecuteAsync(() =>
        {
            executionCount++;
            return Task.CompletedTask;
        }, CancellationToken.None);

        // Assert
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task RetryWithBackoff_GivenExecuteAsyncFailingOperationWithRetries()
    {
        // Arrange
        var strategy = new ExponentialBackoffStrategy(
            TimeSpan.FromMilliseconds(10),
            TimeSpan.FromSeconds(1),
            2); // Only 2 attempts
        var executionCount = 0;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await strategy.ExecuteAsync(() =>
            {
                executionCount++;
                throw new InvalidOperationException("Test exception");
            }, CancellationToken.None);
        });

        Assert.Equal(2, executionCount); // Should try twice
    }

    [Fact]
    public async Task ReturnValue_GivenExponentialBackoffExecuteAsyncGenericSuccessfulOperation()
    {
        // Arrange
        var strategy = new ExponentialBackoffStrategy(
            TimeSpan.FromMilliseconds(10),
            TimeSpan.FromSeconds(1),
            3);
        const string expectedResult = "test result";

        // Act
        var result = await strategy.ExecuteAsync(() => Task.FromResult(expectedResult), CancellationToken.None);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void ThrowArgumentOutOfRangeException_GivenMultiplierExactlyOne()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExponentialBackoffStrategy(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(10),
                3,
                backoffMultiplier: 1.0));
    }

    [Fact]
    public async Task RetryOnTimeoutException_GivenForTransientErrors()
    {
        var strategy = ExponentialBackoffStrategy.ForTransientErrors(maxAttempts: 2);
        var executionCount = 0;

        await Assert.ThrowsAsync<TimeoutException>(async () =>
        {
            await strategy.ExecuteAsync(() =>
            {
                executionCount++;
                throw new TimeoutException("timed out");
            }, CancellationToken.None);
        });

        Assert.Equal(2, executionCount);
    }

    [Fact]
    public async Task RetryOnInvalidOperationExceptionWithTimeoutMessage_GivenForTransientErrors()
    {
        var strategy = ExponentialBackoffStrategy.ForTransientErrors(maxAttempts: 2);
        var executionCount = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await strategy.ExecuteAsync(() =>
            {
                executionCount++;
                throw new InvalidOperationException("Connection timeout occurred");
            }, CancellationToken.None);
        });

        Assert.Equal(2, executionCount);
    }

    [Fact]
    public async Task RetryOnInvalidOperationExceptionWithNetworkMessage_GivenForTransientErrors()
    {
        var strategy = ExponentialBackoffStrategy.ForTransientErrors(maxAttempts: 2);
        var executionCount = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await strategy.ExecuteAsync(() =>
            {
                executionCount++;
                throw new InvalidOperationException("Network error detected");
            }, CancellationToken.None);
        });

        Assert.Equal(2, executionCount);
    }

    [Fact]
    public async Task NotRetryOnNonTransientException_GivenForTransientErrors()
    {
        var strategy = ExponentialBackoffStrategy.ForTransientErrors(maxAttempts: 3);
        var executionCount = 0;

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await strategy.ExecuteAsync(() =>
            {
                executionCount++;
                throw new ArgumentException("bad argument");
            }, CancellationToken.None);
        });

        Assert.Equal(1, executionCount);
    }

    [Fact]
    public void ApplyJitter_GivenGetRetryDelayWithJitterEnabled()
    {
        var strategy = new ExponentialBackoffStrategy(
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromSeconds(60),
            5,
            backoffMultiplier: 2.0,
            enableJitter: true);

        var delays = new HashSet<double>();
        for (var i = 0; i < 20; i++)
        {
            var delay = strategy.GetRetryDelay(3, null);
            delays.Add(delay.TotalMilliseconds);
        }

        Assert.True(delays.Count > 1, "Jitter should produce varying delays");
    }

    [Fact]
    public void CapJitteredDelay_GivenGetRetryDelayWithTinyMaxDelay()
    {
        var strategy = new ExponentialBackoffStrategy(
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(150),
            5,
            backoffMultiplier: 2.0,
            enableJitter: true);

        for (var i = 0; i < 50; i++)
        {
            var delay = strategy.GetRetryDelay(4, null);
            Assert.True(delay <= TimeSpan.FromMilliseconds(150),
                $"Jittered delay {delay.TotalMilliseconds}ms should not exceed max delay 150ms");
            Assert.True(delay >= TimeSpan.Zero,
                $"Jittered delay {delay.TotalMilliseconds}ms should not be negative");
        }
    }

    [Fact]
    public void CreateWithLogger_GivenForTransientErrorsFactory()
    {
        var logger = new ConsoleLogger();

        var strategy = ExponentialBackoffStrategy.ForTransientErrors(maxAttempts: 3, logger: logger);

        Assert.Equal("ExponentialBackoff", strategy.Name);
    }

    [Fact]
    public void CreateWithLogger_GivenDefaultFactory()
    {
        var logger = new ConsoleLogger();

        var strategy = ExponentialBackoffStrategy.Default(maxAttempts: 3, logger: logger);

        Assert.Equal("ExponentialBackoff", strategy.Name);
    }
}
