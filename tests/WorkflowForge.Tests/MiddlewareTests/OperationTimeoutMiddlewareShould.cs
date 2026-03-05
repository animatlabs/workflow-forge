using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge;
using WorkflowForge.Abstractions;
using WorkflowForge.Constants;
using WorkflowForge.Middleware;
using WorkflowForge.Operations;

namespace WorkflowForge.Tests.MiddlewareTests;

/// <summary>
/// Unit tests for OperationTimeoutMiddleware covering constructor validation and timeout enforcement.
/// </summary>
public class OperationTimeoutMiddlewareShould
{
    [Fact]
    public void ThrowArgumentException_GivenNegativeTimeout()
    {
        // Arrange
        var logger = Mock.Of<IWorkflowForgeLogger>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new OperationTimeoutMiddleware(TimeSpan.FromMilliseconds(-1), logger));
    }

    [Fact]
    public async Task ThrowTimeoutException_GivenOperationExceedsTimeout()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var logger = Mock.Of<IWorkflowForgeLogger>();
        var middleware = new OperationTimeoutMiddleware(TimeSpan.FromMilliseconds(50), logger);
        var operation = new DelegateWorkflowOperation<object, string>("SlowOp", async (input, f, ct) =>
        {
            await Task.Delay(200);
            return "result";
        });

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(() =>
            middleware.ExecuteAsync(
                operation,
                foundry,
                new object(),
                async ct => (object?)await operation.ForgeAsync(new object(), foundry, ct)));

        Assert.True(foundry.Properties.TryGetValue(FoundryPropertyKeys.OperationTimedOut, out var timedOut) && (bool)timedOut!);
    }

    private static WorkflowFoundry CreateTestFoundry()
    {
        var executionId = Guid.NewGuid();
        var properties = new ConcurrentDictionary<string, object?>();
        return new WorkflowFoundry(executionId, properties);
    }
}
