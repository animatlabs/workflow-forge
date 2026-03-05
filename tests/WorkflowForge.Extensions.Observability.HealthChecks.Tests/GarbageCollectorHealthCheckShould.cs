using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Loggers;

namespace WorkflowForge.Extensions.Observability.HealthChecks.Tests;

public class GarbageCollectorHealthCheckShould
{
    [Fact]
    public void SetNameCorrectly_GivenConstruction()
    {
        // Arrange & Act
        var healthCheck = new GarbageCollectorHealthCheck();

        // Assert
        Assert.Equal("GarbageCollector", healthCheck.Name);
    }

    [Fact]
    public async Task ReturnHealthyWithGCInfo_GivenCheckHealthAsync()
    {
        // Arrange
        var healthCheck = new GarbageCollectorHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        Assert.True(
            result.Status == HealthStatus.Healthy || result.Status == HealthStatus.Degraded,
            $"GC check returned unexpected status: {result.Status}");
        Assert.NotNull(result.Description);
        Assert.Contains("collections", result.Description, StringComparison.OrdinalIgnoreCase);
        Assert.True(result.Data != null && result.Data.ContainsKey("Gen0Collections"));
        Assert.True(result.Data != null && result.Data.ContainsKey("Gen1Collections"));
        Assert.True(result.Data != null && result.Data.ContainsKey("Gen2Collections"));
        Assert.True(result.Data != null && result.Data.ContainsKey("TotalMemoryMB"));
    }

    [Fact]
    public async Task ThrowOperationCanceledException_GivenCheckHealthAsyncWithCancellation()
    {
        // Arrange
        var healthCheck = new GarbageCollectorHealthCheck();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            healthCheck.CheckHealthAsync(cts.Token));
    }
}
