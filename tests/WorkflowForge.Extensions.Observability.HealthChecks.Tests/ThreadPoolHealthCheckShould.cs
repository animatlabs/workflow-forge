using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Loggers;

namespace WorkflowForge.Extensions.Observability.HealthChecks.Tests;

public class ThreadPoolHealthCheckShould
{
    [Fact]
    public void SetNameCorrectly_GivenConstruction()
    {
        // Arrange & Act
        var healthCheck = new ThreadPoolHealthCheck();

        // Assert
        Assert.Equal("ThreadPool", healthCheck.Name);
    }

    [Fact]
    public async Task ReturnHealthyWithThreadPoolInfo_GivenCheckHealthAsync()
    {
        // Arrange
        var healthCheck = new ThreadPoolHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        Assert.True(
            result.Status == HealthStatus.Healthy || result.Status == HealthStatus.Degraded,
            $"ThreadPool check returned unexpected status: {result.Status}");
        Assert.NotNull(result.Description);
        Assert.Contains("threads", result.Description, StringComparison.OrdinalIgnoreCase);
        Assert.True(result.Data != null && result.Data.ContainsKey("WorkerThreads"));
        Assert.True(result.Data != null && result.Data.ContainsKey("CompletionPortThreads"));
        Assert.True(result.Data != null && result.Data.ContainsKey("MaxWorkerThreads"));
        Assert.True(result.Data != null && result.Data.ContainsKey("MaxCompletionPortThreads"));
    }

    [Fact]
    public async Task ThrowOperationCanceledException_GivenCheckHealthAsyncWithCancellation()
    {
        // Arrange
        var healthCheck = new ThreadPoolHealthCheck();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            healthCheck.CheckHealthAsync(cts.Token));
    }
}
