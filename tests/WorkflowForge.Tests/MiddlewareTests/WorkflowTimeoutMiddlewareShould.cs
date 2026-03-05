using System;
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
}
