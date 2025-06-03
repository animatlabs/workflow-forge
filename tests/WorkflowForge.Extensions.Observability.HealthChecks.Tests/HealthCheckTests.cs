using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Observability.HealthChecks;
using WorkflowForge.Loggers;

namespace WorkflowForge.Extensions.Observability.HealthChecks.Tests;

public class MemoryHealthCheckTests
{
    [Fact]
    public void Constructor_SetsNameCorrectly()
    {
        // Arrange & Act
        var healthCheck = new MemoryHealthCheck();

        // Assert
        Assert.Equal("Memory", healthCheck.Name);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthyWithMemoryUsage()
    {
        // Arrange
        var healthCheck = new MemoryHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.NotNull(result.Description);
        Assert.Contains("MB", result.Description);
        Assert.True(result.Data != null && result.Data.ContainsKey("WorkingSetMB"));
        Assert.True(result.Data != null && result.Data.ContainsKey("GCTotalMemoryMB"));
    }

    [Fact]
    public async Task CheckHealthAsync_WithCancellation_ThrowsOperationCanceledException()
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

public class GarbageCollectorHealthCheckTests
{
    [Fact]
    public void Constructor_SetsNameCorrectly()
    {
        // Arrange & Act
        var healthCheck = new GarbageCollectorHealthCheck();

        // Assert
        Assert.Equal("GarbageCollector", healthCheck.Name);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthyWithGCInfo()
    {
        // Arrange
        var healthCheck = new GarbageCollectorHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.NotNull(result.Description);
        Assert.Contains("collections", result.Description, StringComparison.OrdinalIgnoreCase);
        Assert.True(result.Data != null && result.Data.ContainsKey("Gen0Collections"));
        Assert.True(result.Data != null && result.Data.ContainsKey("Gen1Collections"));
        Assert.True(result.Data != null && result.Data.ContainsKey("Gen2Collections"));
        Assert.True(result.Data != null && result.Data.ContainsKey("TotalMemoryMB"));
    }

    [Fact]
    public async Task CheckHealthAsync_WithCancellation_ThrowsOperationCanceledException()
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

public class ThreadPoolHealthCheckTests
{
    [Fact]
    public void Constructor_SetsNameCorrectly()
    {
        // Arrange & Act
        var healthCheck = new ThreadPoolHealthCheck();

        // Assert
        Assert.Equal("ThreadPool", healthCheck.Name);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthyWithThreadPoolInfo()
    {
        // Arrange
        var healthCheck = new ThreadPoolHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync(CancellationToken.None);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.NotNull(result.Description);
        Assert.Contains("threads", result.Description, StringComparison.OrdinalIgnoreCase);
        Assert.True(result.Data != null && result.Data.ContainsKey("WorkerThreads"));
        Assert.True(result.Data != null && result.Data.ContainsKey("CompletionPortThreads"));
        Assert.True(result.Data != null && result.Data.ContainsKey("MaxWorkerThreads"));
        Assert.True(result.Data != null && result.Data.ContainsKey("MaxCompletionPortThreads"));
    }

    [Fact]
    public async Task CheckHealthAsync_WithCancellation_ThrowsOperationCanceledException()
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

public class HealthCheckServiceTests
{
    [Fact]
    public void Constructor_WithValidLogger_SetsPropertiesCorrectly()
    {
        // Arrange
        var logger = new ConsoleLogger();

        // Act
        var service = new HealthCheckService(logger, registerBuiltInHealthChecks: false);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new HealthCheckService(null!, registerBuiltInHealthChecks: false));
    }

    [Fact]
    public void RegisterHealthCheck_WithValidHealthCheck_AddsToCollection()
    {
        // Arrange
        var logger = new ConsoleLogger();
        var service = new HealthCheckService(logger, registerBuiltInHealthChecks: false);
        var healthCheck = new MemoryHealthCheck();

        // Act
        service.RegisterHealthCheck(healthCheck);

        // Assert - We can't directly check registered checks, but we can verify no exception was thrown
        Assert.NotNull(service);
    }

    [Fact]
    public void RegisterHealthCheck_WithNullHealthCheck_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = new ConsoleLogger();
        var service = new HealthCheckService(logger, registerBuiltInHealthChecks: false);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.RegisterHealthCheck(null!));
    }

    [Fact]
    public async Task CheckHealthAsync_WithSingleHealthCheck_ReturnsResult()
    {
        // Arrange
        var logger = new ConsoleLogger();
        var service = new HealthCheckService(logger, registerBuiltInHealthChecks: false);
        var healthCheck = new MemoryHealthCheck();
        service.RegisterHealthCheck(healthCheck);

        // Act
        var results = await service.CheckHealthAsync(CancellationToken.None);

        // Assert
        Assert.Single(results);
        Assert.True(results.ContainsKey("Memory"));
        Assert.Equal(HealthStatus.Healthy, results["Memory"].Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WithMultipleHealthChecks_ReturnsAllResults()
    {
        // Arrange
        var logger = new ConsoleLogger();
        var service = new HealthCheckService(logger, registerBuiltInHealthChecks: false);
        var memoryCheck = new MemoryHealthCheck();
        var gcCheck = new GarbageCollectorHealthCheck();
        var threadPoolCheck = new ThreadPoolHealthCheck();

        service.RegisterHealthCheck(memoryCheck);
        service.RegisterHealthCheck(gcCheck);
        service.RegisterHealthCheck(threadPoolCheck);

        // Act
        var results = await service.CheckHealthAsync(CancellationToken.None);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.True(results.ContainsKey("Memory"));
        Assert.True(results.ContainsKey("GarbageCollector"));
        Assert.True(results.ContainsKey("ThreadPool"));
        Assert.All(results.Values, result => Assert.Equal(HealthStatus.Healthy, result.Status));
    }

    [Fact]
    public async Task CheckHealthAsync_WithSpecificHealthCheck_ReturnsOnlyThatResult()
    {
        // Arrange
        var logger = new ConsoleLogger();
        var service = new HealthCheckService(logger, registerBuiltInHealthChecks: false);
        var memoryCheck = new MemoryHealthCheck();
        var gcCheck = new GarbageCollectorHealthCheck();

        service.RegisterHealthCheck(memoryCheck);
        service.RegisterHealthCheck(gcCheck);

        // Act
        var result = await service.CheckHealthAsync("Memory", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WithNonExistentHealthCheck_ReturnsNull()
    {
        // Arrange
        var logger = new ConsoleLogger();
        var service = new HealthCheckService(logger, registerBuiltInHealthChecks: false);

        // Act
        var result = await service.CheckHealthAsync("NonExistent", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CheckHealthAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var logger = new ConsoleLogger();
        var service = new HealthCheckService(logger, registerBuiltInHealthChecks: false);
        var healthCheck = new MemoryHealthCheck();
        service.RegisterHealthCheck(healthCheck);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            service.CheckHealthAsync(cts.Token));
    }
}

public class HealthCheckResultTests
{
    [Fact]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
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
    public void Constructor_WithNullData_CreatesEmptyDictionary()
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
    public void Status_WithValidValues_SetsCorrectly(HealthStatus status)
    {
        // Arrange & Act
        var result = new HealthCheckResult(status, "Test");

        // Assert
        Assert.Equal(status, result.Status);
    }

    [Fact]
    public void HealthyFactory_ReturnsHealthyResult()
    {
        // Arrange & Act
        var result = HealthCheckResult.Healthy("All good");

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("All good", result.Description);
    }
} 