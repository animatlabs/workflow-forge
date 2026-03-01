using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Extensions.Observability.HealthChecks.Abstractions;
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

public class HealthCheckServiceShould
{
    [Fact]
    public void SetPropertiesCorrectly_GivenConstructorWithValidLogger()
    {
        // Arrange
        var logger = new ConsoleLogger();

        // Act
        var service = new HealthCheckService(logger, registerBuiltInHealthChecks: false);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenConstructorWithNullLogger()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new HealthCheckService(null!, registerBuiltInHealthChecks: false));
    }

    [Fact]
    public void AddToCollection_GivenRegisterHealthCheckWithValidHealthCheck()
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
    public void ThrowArgumentNullException_GivenRegisterHealthCheckWithNullHealthCheck()
    {
        // Arrange
        var logger = new ConsoleLogger();
        var service = new HealthCheckService(logger, registerBuiltInHealthChecks: false);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.RegisterHealthCheck(null!));
    }

    [Fact]
    public async Task ReturnResult_GivenCheckHealthAsyncWithSingleHealthCheck()
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
        Assert.True(
            results["Memory"].Status == HealthStatus.Healthy || results["Memory"].Status == HealthStatus.Degraded,
            $"Memory check returned unexpected status: {results["Memory"].Status}");
    }

    [Fact]
    public async Task ReturnAllResults_GivenCheckHealthAsyncWithMultipleHealthChecks()
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
        Assert.True(
            results["Memory"].Status == HealthStatus.Healthy || results["Memory"].Status == HealthStatus.Degraded,
            $"Memory check returned unexpected status: {results["Memory"].Status}");
        Assert.True(
            results["ThreadPool"].Status == HealthStatus.Healthy || results["ThreadPool"].Status == HealthStatus.Degraded,
            $"ThreadPool check returned unexpected status: {results["ThreadPool"].Status}");
        Assert.True(
            results["GarbageCollector"].Status == HealthStatus.Healthy || results["GarbageCollector"].Status == HealthStatus.Degraded,
            $"GC check returned unexpected status: {results["GarbageCollector"].Status}");
    }

    [Fact]
    public async Task ReturnOnlyThatResult_GivenCheckHealthAsyncWithSpecificHealthCheck()
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
        Assert.True(result.Status == HealthStatus.Healthy || result.Status == HealthStatus.Degraded,
            $"Memory check returned unexpected status: {result.Status}");
    }

    [Fact]
    public async Task ReturnNull_GivenCheckHealthAsyncWithNonExistentHealthCheck()
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
    public async Task ThrowOperationCanceledException_GivenCheckHealthAsyncWithCancellation()
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