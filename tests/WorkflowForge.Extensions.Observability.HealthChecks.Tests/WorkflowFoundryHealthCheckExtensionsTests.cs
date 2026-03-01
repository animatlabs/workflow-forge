using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Observability.HealthChecks;
using WorkflowForge.Loggers;

namespace WorkflowForge.Extensions.Observability.HealthChecks.Tests;

/// <summary>
/// Tests for WorkflowFoundryHealthCheckExtensions.
/// </summary>
public class WorkflowFoundryHealthCheckExtensionsShould
{
    [Fact]
    public void ThrowArgumentNullException_GivenCreateHealthCheckServiceWithNullFoundry()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((IWorkflowFoundry)null!).CreateHealthCheckService());
    }

    [Fact]
    public void ReturnHealthCheckService_GivenCreateHealthCheckServiceWithValidFoundry()
    {
        var foundryMock = new Mock<IWorkflowFoundry>();
        foundryMock.Setup(f => f.Logger).Returns(new ConsoleLogger("Test"));

        var service = foundryMock.Object.CreateHealthCheckService();

        Assert.NotNull(service);
    }

    [Fact]
    public void ReturnServiceWithInterval_GivenCreateHealthCheckServiceWithCheckInterval()
    {
        var foundryMock = new Mock<IWorkflowFoundry>();
        foundryMock.Setup(f => f.Logger).Returns(new ConsoleLogger("Test"));
        var interval = TimeSpan.FromSeconds(30);

        var service = foundryMock.Object.CreateHealthCheckService(interval);

        Assert.NotNull(service);
    }

    [Fact]
    public void ReturnServiceWithoutPeriodicChecks_GivenCreateHealthCheckServiceWithNullInterval()
    {
        var foundryMock = new Mock<IWorkflowFoundry>();
        foundryMock.Setup(f => f.Logger).Returns(new ConsoleLogger("Test"));

        var service = foundryMock.Object.CreateHealthCheckService(checkInterval: null);

        Assert.NotNull(service);
    }

    [Fact]
    public async Task ThrowArgumentNullException_GivenCheckFoundryHealthAsyncWithNullFoundry()
    {
        var logger = new ConsoleLogger("Test");
        var healthCheckService = new HealthCheckService(logger, registerBuiltInHealthChecks: false);

        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await ((IWorkflowFoundry)null!).CheckFoundryHealthAsync(healthCheckService));
    }

    [Fact]
    public async Task ThrowArgumentNullException_GivenCheckFoundryHealthAsyncWithNullHealthCheckService()
    {
        var foundryMock = new Mock<IWorkflowFoundry>();
        foundryMock.Setup(f => f.Logger).Returns(new ConsoleLogger("Test"));

        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await foundryMock.Object.CheckFoundryHealthAsync(null!));
    }

    [Fact]
    public async Task ReturnOverallStatus_GivenCheckFoundryHealthAsyncWithValidArgs()
    {
        var foundryMock = new Mock<IWorkflowFoundry>();
        foundryMock.Setup(f => f.Logger).Returns(new ConsoleLogger("Test"));
        var logger = new ConsoleLogger("Test");
        var healthCheckService = new HealthCheckService(logger, registerBuiltInHealthChecks: false);
        healthCheckService.RegisterHealthCheck(new MemoryHealthCheck());

        var status = await foundryMock.Object.CheckFoundryHealthAsync(healthCheckService);

        Assert.True(status == HealthStatus.Healthy || status == HealthStatus.Degraded,
            $"Expected Healthy or Degraded, got {status}");
    }

    [Fact]
    public async Task RespectCancellation_GivenCheckFoundryHealthAsyncWithCancellationToken()
    {
        var foundryMock = new Mock<IWorkflowFoundry>();
        foundryMock.Setup(f => f.Logger).Returns(new ConsoleLogger("Test"));
        var logger = new ConsoleLogger("Test");
        var healthCheckService = new HealthCheckService(logger, registerBuiltInHealthChecks: false);
        healthCheckService.RegisterHealthCheck(new MemoryHealthCheck());

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await foundryMock.Object.CheckFoundryHealthAsync(healthCheckService, cts.Token));
    }
}
