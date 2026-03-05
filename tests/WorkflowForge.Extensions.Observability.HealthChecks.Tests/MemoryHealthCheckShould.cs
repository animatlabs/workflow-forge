using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Loggers;

namespace WorkflowForge.Extensions.Observability.HealthChecks.Tests;

public class MemoryHealthCheckShould
{
    [Fact]
    public void SetNameCorrectly_GivenConstruction()
    {
        // Arrange & Act
        var healthCheck = new MemoryHealthCheck();

        // Assert
        Assert.Equal("Memory", healthCheck.Name);
    }

    [Fact]
    public async Task ReturnHealthyWithMemoryUsage_GivenCheckHealthAsync()
    {
        // Arrange
        var healthCheck = new MemoryHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        Assert.True(
            result.Status == HealthStatus.Healthy || result.Status == HealthStatus.Degraded,
            $"Memory check returned unexpected status: {result.Status}");
        Assert.NotNull(result.Description);
        Assert.Contains("MB", result.Description);
        Assert.True(result.Data != null && result.Data.ContainsKey("WorkingSetMB"));
        Assert.True(result.Data != null && result.Data.ContainsKey("GCTotalMemoryMB"));
    }

    [Fact]
    public async Task ThrowOperationCanceledException_GivenCheckHealthAsyncWithCancellation()
    {
        // Arrange
        var healthCheck = new MemoryHealthCheck();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            healthCheck.CheckHealthAsync(cts.Token));
    }
}
