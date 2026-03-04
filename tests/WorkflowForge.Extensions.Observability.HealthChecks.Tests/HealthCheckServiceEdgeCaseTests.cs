using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Extensions.Observability.HealthChecks.Abstractions;
using WorkflowForge.Loggers;

namespace WorkflowForge.Extensions.Observability.HealthChecks.Tests;

public class HealthCheckServiceEdgeCasesShould
{
    [Fact]
    public async Task ReturnEmptyAndHealthy_GivenNoRegisteredChecks()
    {
        using var service = new HealthCheckService(new ConsoleLogger(), registerBuiltInHealthChecks: false);

        var results = await service.CheckHealthAsync();

        Assert.Empty(results);
        Assert.Equal(HealthStatus.Healthy, service.OverallStatus);
    }

    [Fact]
    public async Task ReportDegradedOverallStatus_GivenDegradedResult()
    {
        using var service = new HealthCheckService(new ConsoleLogger(), registerBuiltInHealthChecks: false);
        service.RegisterHealthCheck(new StaticHealthCheck("DegradedOne", HealthStatus.Degraded));

        var results = await service.CheckHealthAsync();

        Assert.Single(results);
        Assert.Equal(HealthStatus.Degraded, service.OverallStatus);
    }

    [Fact]
    public async Task ReportUnhealthyOverallStatus_GivenUnhealthyResult()
    {
        using var service = new HealthCheckService(new ConsoleLogger(), registerBuiltInHealthChecks: false);
        service.RegisterHealthCheck(new StaticHealthCheck("HealthyOne", HealthStatus.Healthy));
        service.RegisterHealthCheck(new StaticHealthCheck("UnhealthyOne", HealthStatus.Unhealthy));

        var results = await service.CheckHealthAsync();

        Assert.Equal(2, results.Count);
        Assert.Equal(HealthStatus.Unhealthy, service.OverallStatus);
    }

    [Fact]
    public async Task ReturnUnhealthyResult_GivenCheckThrows()
    {
        using var service = new HealthCheckService(new ConsoleLogger(), registerBuiltInHealthChecks: false);
        service.RegisterHealthCheck(new ThrowingHealthCheck("Throwing"));

        var results = await service.CheckHealthAsync();

        Assert.Single(results);
        Assert.Equal(HealthStatus.Unhealthy, results["Throwing"].Status);
        Assert.NotNull(results["Throwing"].Exception);
    }

    [Fact]
    public async Task ThrowArgumentException_GivenBlankNameForSingleCheck()
    {
        using var service = new HealthCheckService(new ConsoleLogger(), registerBuiltInHealthChecks: false);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CheckHealthAsync(" "));
    }

    [Fact]
    public void ThrowObjectDisposedException_GivenRegisterAfterDispose()
    {
        var service = new HealthCheckService(new ConsoleLogger(), registerBuiltInHealthChecks: false);
        service.Dispose();

        Assert.Throws<ObjectDisposedException>(() => service.RegisterHealthCheck(new StaticHealthCheck("X", HealthStatus.Healthy)));
    }

    [Fact]
    public async Task ThrowObjectDisposedException_GivenCheckAfterDispose()
    {
        var service = new HealthCheckService(new ConsoleLogger(), registerBuiltInHealthChecks: false);
        service.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => service.CheckHealthAsync());
    }

    [Fact]
    public void ReturnFalse_GivenUnregisterInvalidName()
    {
        using var service = new HealthCheckService(new ConsoleLogger(), registerBuiltInHealthChecks: false);

        Assert.False(service.UnregisterHealthCheck(""));
    }

    [Fact]
    public void ReturnTrueThenFalse_GivenUnregisterExistingName()
    {
        using var service = new HealthCheckService(new ConsoleLogger(), registerBuiltInHealthChecks: false);
        service.RegisterHealthCheck(new StaticHealthCheck("Removable", HealthStatus.Healthy));

        Assert.True(service.UnregisterHealthCheck("Removable"));
        Assert.False(service.UnregisterHealthCheck("Removable"));
    }

    [Fact]
    public void NotThrow_GivenDisposeHealthCheckThrows()
    {
        var service = new HealthCheckService(new ConsoleLogger(), registerBuiltInHealthChecks: false);
        service.RegisterHealthCheck(new DisposableThrowingHealthCheck("DisposableThrow"));

        service.Dispose();
        service.Dispose();
    }

    [Fact]
    public async Task RunBuiltInHealthChecks_GivenRegisterBuiltInHealthChecksTrue()
    {
        using var service = new HealthCheckService(new ConsoleLogger(), registerBuiltInHealthChecks: true);

        var results = await service.CheckHealthAsync();

        Assert.NotEmpty(results);
        Assert.True(results.ContainsKey("Memory"));
        Assert.True(results.ContainsKey("GarbageCollector"));
        Assert.True(results.ContainsKey("ThreadPool"));
    }

    [Fact]
    public void InitializeWithCheckInterval_GivenCheckInterval()
    {
        // Creating with a check interval should not throw
        using var service = new HealthCheckService(
            new ConsoleLogger(),
            checkInterval: TimeSpan.FromSeconds(60),
            registerBuiltInHealthChecks: false);

        Assert.NotNull(service);
    }

    [Fact]
    public void ReturnFalse_GivenUnregisterWhenDisposed()
    {
        var service = new HealthCheckService(new ConsoleLogger(), registerBuiltInHealthChecks: false);
        service.RegisterHealthCheck(new StaticHealthCheck("Existing", HealthStatus.Healthy));
        service.Dispose();

        var result = service.UnregisterHealthCheck("Existing");

        Assert.False(result);
    }

    private sealed class StaticHealthCheck : IHealthCheck
    {
        public StaticHealthCheck(string name, HealthStatus status)
        {
            Name = name;
            Status = status;
        }

        private HealthStatus Status { get; }
        public string Name { get; }
        public string Description => "static";

        public Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new HealthCheckResult(Status, "static"));
        }
    }

    private sealed class ThrowingHealthCheck : IHealthCheck
    {
        public ThrowingHealthCheck(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public string Description => "throws";

        public Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("health check failure");
        }
    }

    private sealed class DisposableThrowingHealthCheck : IHealthCheck, IDisposable
    {
        public DisposableThrowingHealthCheck(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public string Description => "disposable";

        public Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HealthCheckResult.Healthy("ok"));
        }

        public void Dispose()
        {
            throw new InvalidOperationException("dispose failure");
        }
    }
}
