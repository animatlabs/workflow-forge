using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Extensions.Resilience.Strategies;
using WorkflowForge.Loggers;

namespace WorkflowForge.Extensions.Resilience.Tests;

public class FixedIntervalStrategyShould
{
    [Fact]
    public void SetPropertiesCorrectly_GivenValidInterval()
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
    public void ThrowArgumentException_GivenNegativeInterval()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new FixedIntervalStrategy(TimeSpan.FromMilliseconds(-1), 3));
    }

    [Fact]
    public void ReturnSameInterval_GivenGetRetryDelay()
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
    public async Task ReturnFalse_GivenFixedIntervalShouldRetryAsyncWithOperationCanceledException()
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
    public async Task ReturnTrue_GivenFixedIntervalShouldRetryAsyncWithRegularException()
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
