using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Extensions.Resilience.Strategies;

namespace WorkflowForge.Extensions.Resilience.Tests;

public class ExponentialBackoffStrategyTests
{
    [Fact]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
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
    public void Constructor_WithInvalidBaseDelay_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExponentialBackoffStrategy(TimeSpan.FromMilliseconds(-1), TimeSpan.FromSeconds(10), 3));
    }

    [Fact]
    public void Constructor_WithInvalidMaxDelay_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExponentialBackoffStrategy(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5), 3));
    }

    [Fact]
    public void Constructor_WithInvalidMaxAttempts_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExponentialBackoffStrategy(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(10), 0));
    }

    [Fact]
    public async Task ShouldRetryAsync_WithNullException_ReturnsTrue()
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
    public async Task ShouldRetryAsync_WithOperationCanceledException_ReturnsFalse()
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
    public async Task ShouldRetryAsync_WithRegularException_ReturnsTrue()
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
    public void GetRetryDelay_CalculatesExponentialBackoff()
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
    public void GetRetryDelay_RespectsMaxDelay()
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
    public async Task ExecuteAsync_SuccessfulOperation_ExecutesOnce()
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
    public async Task ExecuteAsync_FailingOperationWithRetries_RetriesWithBackoff()
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
    public async Task ExecuteAsync_Generic_SuccessfulOperation_ReturnsValue()
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
}

public class FixedIntervalStrategyTests
{
    [Fact]
    public void Constructor_WithValidInterval_SetsPropertiesCorrectly()
    {
        // Arrange
        var interval = TimeSpan.FromSeconds(1);
        const int maxAttempts = 3;

        // Act
        var strategy = new FixedIntervalStrategy(interval, maxAttempts);

        // Assert
        Assert.Contains("FixedInterval", strategy.Name);
    }

    [Fact]
    public void Constructor_WithNegativeInterval_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new FixedIntervalStrategy(TimeSpan.FromMilliseconds(-1), 3));
    }

    [Fact]
    public void GetRetryDelay_AlwaysReturnsSameInterval()
    {
        // Arrange
        var interval = TimeSpan.FromSeconds(1);
        var strategy = new FixedIntervalStrategy(interval, 5);

        // Act
        var delay1 = strategy.GetRetryDelay(1, null); // Should be 0 for first attempt
        var delay2 = strategy.GetRetryDelay(2, null); // Should be interval
        var delay3 = strategy.GetRetryDelay(3, null); // Should be interval

        // Assert
        Assert.Equal(TimeSpan.Zero, delay1);
        Assert.Equal(interval, delay2);
        Assert.Equal(interval, delay3);
    }

    [Fact]
    public async Task ShouldRetryAsync_WithOperationCanceledException_ReturnsFalse()
    {
        // Arrange
        var strategy = new FixedIntervalStrategy(TimeSpan.FromSeconds(1), 3);
        var exception = new OperationCanceledException();

        // Act
        var result = await strategy.ShouldRetryAsync(1, exception, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ShouldRetryAsync_WithRegularException_ReturnsTrue()
    {
        // Arrange
        var strategy = new FixedIntervalStrategy(TimeSpan.FromSeconds(1), 3);
        var exception = new InvalidOperationException();

        // Act
        var result = await strategy.ShouldRetryAsync(1, exception, CancellationToken.None);

        // Assert
        Assert.True(result);
    }
}

public class RandomIntervalStrategyTests
{
    [Fact]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        const int maxAttempts = 3;
        var minDelay = TimeSpan.FromMilliseconds(100);
        var maxDelay = TimeSpan.FromSeconds(5);

        // Act
        var strategy = new RandomIntervalStrategy(maxAttempts, minDelay, maxDelay);

        // Assert
        Assert.Contains("RandomInterval", strategy.Name);
    }

    [Fact]
    public void Constructor_WithInvalidMinDelay_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RandomIntervalStrategy(3, TimeSpan.FromMilliseconds(-1), TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void Constructor_WithInvalidMaxDelay_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RandomIntervalStrategy(3, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void GetRetryDelay_ReturnsRandomValueWithinRange()
    {
        // Arrange
        const int maxAttempts = 10;
        var minDelay = TimeSpan.FromMilliseconds(100);
        var maxDelay = TimeSpan.FromMilliseconds(500);
        var strategy = new RandomIntervalStrategy(maxAttempts, minDelay, maxDelay);

        // Act
        var delays = new List<TimeSpan>();
        for (int i = 2; i <= 6; i++) // Start from 2 since first attempt returns 0
        {
            delays.Add(strategy.GetRetryDelay(i, null));
        }

        // Assert
        Assert.All(delays, delay =>
        {
            Assert.True(delay >= minDelay);
            Assert.True(delay <= maxDelay);
        });

        // Verify we get different values (randomness)
        var uniqueDelays = delays.Distinct().Count();
        Assert.True(uniqueDelays > 1, "Expected random delays to produce different values");
    }

    [Fact]
    public async Task ShouldRetryAsync_WithOperationCanceledException_ReturnsFalse()
    {
        // Arrange
        var strategy = new RandomIntervalStrategy(
            3,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromSeconds(1));
        var exception = new OperationCanceledException();

        // Act
        var result = await strategy.ShouldRetryAsync(1, exception, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ShouldRetryAsync_WithRegularException_ReturnsTrue()
    {
        // Arrange
        var strategy = new RandomIntervalStrategy(
            3,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromSeconds(1));
        var exception = new InvalidOperationException();

        // Act
        var result = await strategy.ShouldRetryAsync(1, exception, CancellationToken.None);

        // Assert
        Assert.True(result);
    }
}