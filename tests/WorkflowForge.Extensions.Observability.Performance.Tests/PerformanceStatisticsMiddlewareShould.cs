using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Extensions.Observability.Performance.Tests;

public class PerformanceStatisticsMiddlewareShould
{
    private static IWorkflowOperation Operation() =>
        Mock.Of<IWorkflowOperation>(o => o.Name == "Op" && o.Id == Guid.NewGuid());

    [Fact]
    public async Task RecordSuccess_UsingExplicitStatistics()
    {
        var stats = new FoundryPerformanceStatistics();
        var middleware = new PerformanceStatisticsMiddleware(stats);
        var foundry = Mock.Of<IWorkflowFoundry>();

        var result = await middleware.ExecuteAsync(
            Operation(), foundry, null, _ => Task.FromResult<object?>("result"));

        Assert.Equal("result", result);
        Assert.Equal(1, stats.TotalOperations);
        Assert.Equal(1, stats.SuccessfulOperations);
        Assert.Equal(0, stats.FailedOperations);
    }

    [Fact]
    public async Task RecordFailureAndRethrow_WhenNextThrows()
    {
        var stats = new FoundryPerformanceStatistics();
        var middleware = new PerformanceStatisticsMiddleware(stats);
        var foundry = Mock.Of<IWorkflowFoundry>();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.ExecuteAsync(Operation(), foundry, null,
                _ => throw new InvalidOperationException("boom")));

        Assert.Equal(1, stats.TotalOperations);
        Assert.Equal(0, stats.SuccessfulOperations);
        Assert.Equal(1, stats.FailedOperations);
    }

    [Fact]
    public async Task PassThroughWithoutRecording_WhenNoStatisticsPresent()
    {
        var middleware = new PerformanceStatisticsMiddleware(); // resolves from properties
        var foundry = Mock.Of<IWorkflowFoundry>(f => f.Properties == new ConcurrentDictionary<string, object?>());

        var result = await middleware.ExecuteAsync(
            Operation(), foundry, null, _ => Task.FromResult<object?>("passed"));

        Assert.Equal("passed", result);
    }

    [Fact]
    public async Task ResolveStatisticsFromFoundryProperties()
    {
        var stats = new FoundryPerformanceStatistics();
        var properties = new ConcurrentDictionary<string, object?>();
        properties["PerformanceStatistics"] = stats;
        var middleware = new PerformanceStatisticsMiddleware();
        var foundry = Mock.Of<IWorkflowFoundry>(f => f.Properties == properties);

        await middleware.ExecuteAsync(Operation(), foundry, null, _ => Task.FromResult<object?>("ok"));

        Assert.Equal(1, stats.TotalOperations);
        Assert.Equal(1, stats.SuccessfulOperations);
    }

    [Fact]
    public void ThrowArgumentNullException_ForNullStatisticsConstructor()
    {
        Assert.Throws<ArgumentNullException>(() => new PerformanceStatisticsMiddleware(null!));
    }

    [Fact]
    public async Task ThrowArgumentNullException_ForNullOperationOrNext()
    {
        var middleware = new PerformanceStatisticsMiddleware(new FoundryPerformanceStatistics());
        var foundry = Mock.Of<IWorkflowFoundry>();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            middleware.ExecuteAsync(null!, foundry, null, _ => Task.FromResult<object?>(null)));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            middleware.ExecuteAsync(Operation(), foundry, null, null!));
    }
}
