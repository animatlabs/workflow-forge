using System;
using WorkflowForge.Options;
using WorkflowForge.Loggers;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge;
using WorkflowForge.Abstractions;
using WorkflowForge.Constants;
using WorkflowForge.Middleware;

namespace WorkflowForge.Tests.MiddlewareTests;

/// <summary>
/// Unit tests for WorkflowTimeoutMiddleware covering constructor validation and timeout enforcement.
/// </summary>
public class WorkflowTimeoutMiddlewareShould
{
    [Fact]
    public void ThrowArgumentException_GivenNegativeTimeout()
    {
        // Arrange
        var logger = Mock.Of<IWorkflowForgeLogger>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new WorkflowTimeoutMiddleware(TimeSpan.FromMilliseconds(-1), logger));
    }

    [Fact]
    public async Task ThrowTimeoutException_GivenWorkflowExceedsTimeout()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var logger = Mock.Of<IWorkflowForgeLogger>();
        var middleware = new WorkflowTimeoutMiddleware(TimeSpan.FromMilliseconds(50), logger);
        var workflow = Mock.Of<IWorkflow>(w => w.Name == "SlowWorkflow");

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(() =>
            middleware.ExecuteAsync(workflow, foundry, async () => await Task.Delay(200)));

        Assert.True(foundry.Properties.TryGetValue(FoundryPropertyKeys.WorkflowTimedOut, out var timedOut) && (bool)timedOut!);
    }

    private static WorkflowFoundry CreateTestFoundry()
    {
        var executionId = Guid.NewGuid();
        var properties = new ConcurrentDictionary<string, object?>();
        return new WorkflowFoundry(executionId, properties);
    }

    [Fact]
    public async Task AbortWorkflowPromptly_GivenTimeoutShorterThanWorkflow()
    {
        using var smith = WorkflowForge.CreateSmith();
        smith.AddWorkflowMiddleware(new WorkflowTimeoutMiddleware(
            TimeSpan.FromMilliseconds(200),
            NullLogger.Instance));

        var workflow = WorkflowForge.CreateWorkflow("SlowWorkflow")
            .AddOperation("Slow", async (_, token) => await Task.Delay(TimeSpan.FromSeconds(10), token))
            .Build();

        var stopwatch = Stopwatch.StartNew();
        await Assert.ThrowsAsync<TimeoutException>(() => smith.ForgeAsync(workflow));
        stopwatch.Stop();

        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(5),
            $"Timeout should abort the workflow, but it took {stopwatch.Elapsed}.");
    }

    [Fact]
    public async Task CompleteNormally_GivenWorkflowFasterThanTimeout()
    {
        using var smith = WorkflowForge.CreateSmith();
        smith.AddWorkflowMiddleware(new WorkflowTimeoutMiddleware(
            TimeSpan.FromSeconds(30),
            NullLogger.Instance));

        var executed = false;
        var workflow = WorkflowForge.CreateWorkflow("FastWorkflow")
            .AddOperation("Fast", (_, _) =>
            {
                executed = true;
                return Task.CompletedTask;
            })
            .Build();

        await smith.ForgeAsync(workflow);

        Assert.True(executed);
    }
}
