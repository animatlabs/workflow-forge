using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Loggers;

namespace WorkflowForge.Extensions.Observability.HealthChecks.Tests;

public class HealthCheckResultShould
{
    [Fact]
    public void SetPropertiesCorrectly_GivenHealthCheckResultConstructorWithValidParameters()
    {
        // Arrange
        const HealthStatus status = HealthStatus.Healthy;
        const string description = "Test description";
        var data = new Dictionary<string, object> { { "key", "value" } };
        var exception = new InvalidOperationException("Test exception");
        var duration = TimeSpan.FromMilliseconds(100);

        // Act
        var result = new HealthCheckResult(status, description, exception, data, duration);

        // Assert
        Assert.Equal(status, result.Status);
        Assert.Equal(description, result.Description);
        Assert.Equal(data, result.Data);
        Assert.Equal(exception, result.Exception);
        Assert.Equal(duration, result.Duration);
    }

    [Fact]
    public void CreateEmptyDictionary_GivenConstructorWithNullData()
    {
        // Arrange & Act
        var result = new HealthCheckResult(HealthStatus.Healthy, "Test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Theory]
    [InlineData(HealthStatus.Healthy)]
    [InlineData(HealthStatus.Degraded)]
    [InlineData(HealthStatus.Unhealthy)]
    public void SetCorrectly_GivenStatusWithValidValues(HealthStatus status)
    {
        // Arrange & Act
        var result = new HealthCheckResult(status, "Test");

        // Assert
        Assert.Equal(status, result.Status);
    }

    [Fact]
    public void ReturnHealthyResult_GivenHealthyFactory()
    {
        // Arrange & Act
        var result = HealthCheckResult.Healthy("All good");

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("All good", result.Description);
    }
}
