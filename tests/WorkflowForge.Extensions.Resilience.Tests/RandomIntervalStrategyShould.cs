using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Extensions.Resilience.Strategies;
using WorkflowForge.Loggers;

namespace WorkflowForge.Extensions.Resilience.Tests;

public class RandomIntervalStrategyShould
{
    [Fact]
    public void SetPropertiesCorrectly_GivenRandomIntervalValidParameters()
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
    public void ThrowArgumentException_GivenRandomIntervalInvalidMinDelay()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RandomIntervalStrategy(3, TimeSpan.FromMilliseconds(-1), TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void ThrowArgumentException_GivenRandomIntervalInvalidMaxDelay()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RandomIntervalStrategy(3, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void ReturnRandomValueWithinRange_GivenGetRetryDelay()
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
    public async Task ReturnFalse_GivenRandomIntervalShouldRetryAsyncWithOperationCanceledException()
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
    public async Task ReturnTrue_GivenRandomIntervalShouldRetryAsyncWithRegularException()
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

    [Fact]
    public void ThrowArgumentException_GivenRandomIntervalInvalidMaxAttempts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RandomIntervalStrategy(0, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void ReturnZero_GivenGetRetryDelayForFirstAttempt()
    {
        var strategy = new RandomIntervalStrategy(3, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));

        var delay = strategy.GetRetryDelay(1, null);

        Assert.Equal(TimeSpan.Zero, delay);
    }

    [Fact]
    public void ReturnValidStrategy_GivenDefaultWithDefaultParameters()
    {
        var strategy = RandomIntervalStrategy.Default();

        Assert.NotNull(strategy);
        Assert.Contains("RandomInterval", strategy.Name);
    }

    [Fact]
    public void ReturnStrategyWithCorrectAttempts_GivenDefaultWithCustomMaxAttempts()
    {
        var strategy = RandomIntervalStrategy.Default(maxAttempts: 5);

        Assert.NotNull(strategy);
    }

    [Fact]
    public void ReturnStrategyWithLogger_GivenDefaultWithLogger()
    {
        var logger = new ConsoleLogger("Test");
        var strategy = RandomIntervalStrategy.Default(logger: logger);

        Assert.NotNull(strategy);
    }

    [Fact]
    public void ReturnValidStrategy_GivenHighThroughput()
    {
        var strategy = RandomIntervalStrategy.HighThroughput();

        Assert.NotNull(strategy);
        Assert.Contains("RandomInterval", strategy.Name);
    }

    [Fact]
    public void ReturnStrategy_GivenHighThroughputWithCustomMaxAttempts()
    {
        var strategy = RandomIntervalStrategy.HighThroughput(maxAttempts: 7);

        Assert.NotNull(strategy);
    }

    [Fact]
    public void ReturnValidStrategy_GivenWithJitterValidParameters()
    {
        var baseInterval = TimeSpan.FromMilliseconds(500);
        var strategy = RandomIntervalStrategy.WithJitter(baseInterval, jitterPercent: 0.2);

        Assert.NotNull(strategy);
    }

    [Fact]
    public void ThrowArgumentOutOfRangeException_GivenWithJitterInvalidJitterPercent()
    {
        var baseInterval = TimeSpan.FromMilliseconds(500);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RandomIntervalStrategy.WithJitter(baseInterval, jitterPercent: -0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RandomIntervalStrategy.WithJitter(baseInterval, jitterPercent: 1.5));
    }

    [Fact]
    public void ReturnStrategy_GivenWithJitterZeroJitter()
    {
        var baseInterval = TimeSpan.FromMilliseconds(500);
        var strategy = RandomIntervalStrategy.WithJitter(baseInterval, jitterPercent: 0);

        Assert.NotNull(strategy);
    }

    [Fact]
    public void ReturnStrategy_GivenWithJitterFullJitter()
    {
        var baseInterval = TimeSpan.FromMilliseconds(500);
        var strategy = RandomIntervalStrategy.WithJitter(baseInterval, jitterPercent: 1.0);

        Assert.NotNull(strategy);
    }

    [Fact]
    public void ReturnValidStrategy_GivenCreateWithValidIntervals()
    {
        var minInterval = TimeSpan.FromMilliseconds(50);
        var maxInterval = TimeSpan.FromMilliseconds(200);

        var strategy = RandomIntervalStrategy.Create(minInterval, maxInterval);

        Assert.NotNull(strategy);
        Assert.Contains("RandomInterval", strategy.Name);
    }

    [Fact]
    public void ReturnStrategy_GivenCreateWithEqualMinMax()
    {
        var interval = TimeSpan.FromMilliseconds(100);

        var strategy = RandomIntervalStrategy.Create(interval, interval);

        Assert.NotNull(strategy);
    }

    [Fact]
    public async Task ExecuteOnce_GivenRandomIntervalExecuteAsyncSuccessfulOperation()
    {
        var strategy = RandomIntervalStrategy.Default(maxAttempts: 3);
        var executionCount = 0;

        await strategy.ExecuteAsync(() =>
        {
            executionCount++;
            return Task.CompletedTask;
        }, CancellationToken.None);

        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task RetryWithRandomDelay_GivenExecuteAsyncFailingOperation()
    {
        var strategy = RandomIntervalStrategy.Default(maxAttempts: 3);
        var executionCount = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await strategy.ExecuteAsync(() =>
            {
                executionCount++;
                throw new InvalidOperationException("Test");
            }, CancellationToken.None);
        });

        Assert.Equal(3, executionCount);
    }

    [Fact]
    public async Task ReturnValue_GivenRandomIntervalExecuteAsyncGenericSuccessfulOperation()
    {
        var strategy = RandomIntervalStrategy.Default(maxAttempts: 3);
        const string expected = "result";

        var result = await strategy.ExecuteAsync(() => Task.FromResult(expected), CancellationToken.None);

        Assert.Equal(expected, result);
    }
}
