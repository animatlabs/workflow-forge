using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Extensions.Observability.HealthChecks;
using WorkflowForge.Extensions.Observability.HealthChecks.Abstractions;
using WorkflowForge.Loggers;

namespace WorkflowForge.Extensions.Observability.HealthChecks.Tests;

public class HealthCheckServiceShould
{
    [Fact]
    public async Task ReturnUnhealthyStatus_GivenUnhealthyCheckResult()
    {
        // Arrange
        using var service = new HealthCheckService(new ConsoleLogger(), registerBuiltInHealthChecks: false);
        service.RegisterHealthCheck(new StaticStatusHealthCheck("UnhealthyCheck", HealthStatus.Unhealthy));

        // Act
        await service.CheckHealthAsync();

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, service.OverallStatus);
    }

    [Fact]
    public async Task ReturnDegradedStatus_GivenDegradedCheckResult()
    {
        // Arrange
        using var service = new HealthCheckService(new ConsoleLogger(), registerBuiltInHealthChecks: false);
        service.RegisterHealthCheck(new StaticStatusHealthCheck("DegradedCheck", HealthStatus.Degraded));

        // Act
        await service.CheckHealthAsync();

        // Assert
        Assert.Equal(HealthStatus.Degraded, service.OverallStatus);
    }

    private sealed class StaticStatusHealthCheck : IHealthCheck
    {
        private readonly HealthStatus _status;

        public StaticStatusHealthCheck(string name, HealthStatus status)
        {
            Name = name;
            _status = status;
        }

        public string Name { get; }
        public string Description => "Returns a fixed status for testing";

        public Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new HealthCheckResult(_status, "static status"));
        }
    }
}
