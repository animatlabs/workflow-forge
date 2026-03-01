using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Constants;
using WorkflowForge.Middleware;
using WorkflowForge.Options.Middleware;
using WorkflowForge.Testing;
using Xunit;

using TimingMiddlewareCore = WorkflowForge.Middleware.TimingMiddleware;

namespace WorkflowForge.Tests.Middleware;

/// <summary>
/// Unit tests for WorkflowForge TimingMiddleware - timing measurement and foundry property storage.
/// </summary>
public class TimingMiddlewareTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange - use typed null to force the options overload
        TimingMiddlewareOptions? nullOptions = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TimingMiddlewareCore(nullOptions!));
    }

    [Fact]
    public void Constructor_WithValidOptions_Initializes()
    {
        // Arrange
        var options = new TimingMiddlewareOptions();

        // Act
        var middleware = new TimingMiddlewareCore(options);

        // Assert
        Assert.NotNull(middleware);
    }

    [Fact]
    public void Constructor_BackwardCompatibility_DefaultCtor_Initializes()
    {
        // Act - use full namespace to avoid conflict with test TimingMiddleware in MiddlewareTests
        var middleware = new TimingMiddlewareCore();

        // Assert
        Assert.NotNull(middleware);
    }

    #endregion Constructor Tests

    #region ExecuteAsync_Success

    [Fact]
    public async Task ExecuteAsync_WhenNextSucceeds_ReturnsResult()
    {
        // Arrange
        var options = new TimingMiddlewareOptions();
        var middleware = new TimingMiddlewareCore(options);
        var foundry = new FakeWorkflowFoundry();
        var operation = CreateMockOperation("TestOp");
        var expectedResult = "result";

        // Act
        var result = await middleware.ExecuteAsync(
            operation,
            foundry,
            null,
            _ => Task.FromResult<object?>(expectedResult));

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNextSucceeds_StoresTimingDuration()
    {
        // Arrange
        var options = new TimingMiddlewareOptions { IncludeDetailedTimings = false };
        var middleware = new TimingMiddlewareCore(options);
        var foundry = new FakeWorkflowFoundry();
        var operation = CreateMockOperation("TestOp");

        // Act
        await middleware.ExecuteAsync(
            operation,
            foundry,
            null,
            async _ =>
            {
                await Task.Delay(25);
                return (object?)"result";
            });

        // Assert
        Assert.True(foundry.Properties.ContainsKey(FoundryPropertyKeys.TimingDuration));
        var duration = foundry.Properties[FoundryPropertyKeys.TimingDuration];
        Assert.NotNull(duration);
        var durationMs = Convert.ToInt64(duration);
        Assert.True(durationMs >= 20, $"Expected at least 20ms, got {durationMs}ms");
    }

    [Fact]
    public async Task ExecuteAsync_WhenNextSucceeds_AndIncludeDetailedTimingsTrue_StoresAllTimingProperties()
    {
        // Arrange
        var options = new TimingMiddlewareOptions { IncludeDetailedTimings = true };
        var middleware = new TimingMiddlewareCore(options);
        var foundry = new FakeWorkflowFoundry();
        var operation = CreateMockOperation("TestOp");

        // Act
        await middleware.ExecuteAsync(
            operation,
            foundry,
            null,
            async _ =>
            {
                await Task.Delay(10);
                return (object?)"result";
            });

        // Assert
        Assert.True(foundry.Properties.ContainsKey(FoundryPropertyKeys.TimingDuration));
        Assert.True(foundry.Properties.ContainsKey(FoundryPropertyKeys.TimingStartTime));
        Assert.True(foundry.Properties.ContainsKey(FoundryPropertyKeys.TimingEndTime));
        Assert.True(foundry.Properties.ContainsKey(FoundryPropertyKeys.TimingDurationTicks));
    }

    [Fact]
    public async Task ExecuteAsync_WhenNextSucceeds_AndIncludeDetailedTimingsFalse_StoresOnlyDuration()
    {
        // Arrange
        var options = new TimingMiddlewareOptions { IncludeDetailedTimings = false };
        var middleware = new TimingMiddlewareCore(options);
        var foundry = new FakeWorkflowFoundry();
        var operation = CreateMockOperation("TestOp");

        // Act
        await middleware.ExecuteAsync(
            operation,
            foundry,
            null,
            _ => Task.FromResult<object?>("result"));

        // Assert
        Assert.True(foundry.Properties.ContainsKey(FoundryPropertyKeys.TimingDuration));
        Assert.False(foundry.Properties.ContainsKey(FoundryPropertyKeys.TimingStartTime));
        Assert.False(foundry.Properties.ContainsKey(FoundryPropertyKeys.TimingEndTime));
        Assert.False(foundry.Properties.ContainsKey(FoundryPropertyKeys.TimingDurationTicks));
    }

    #endregion ExecuteAsync_Success

    #region ExecuteAsync_Failure

    [Fact]
    public async Task ExecuteAsync_WhenNextThrows_PropagatesException()
    {
        // Arrange
        var options = new TimingMiddlewareOptions();
        var middleware = new TimingMiddlewareCore(options);
        var foundry = new FakeWorkflowFoundry();
        var operation = CreateMockOperation("TestOp");
        var expectedEx = new InvalidOperationException("Test failure");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.ExecuteAsync(
                operation,
                foundry,
                null,
                _ => throw expectedEx));

        Assert.Same(expectedEx, ex);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNextThrows_StoresTimingDuration()
    {
        // Arrange
        var options = new TimingMiddlewareOptions { IncludeDetailedTimings = false };
        var middleware = new TimingMiddlewareCore(options);
        var foundry = new FakeWorkflowFoundry();
        var operation = CreateMockOperation("TestOp");

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.ExecuteAsync(
                operation,
                foundry,
                null,
                _ => throw new InvalidOperationException("Fail")));

        // Assert - timing should be recorded even on failure
        Assert.True(foundry.Properties.ContainsKey(FoundryPropertyKeys.TimingDuration));
    }

    [Fact]
    public async Task ExecuteAsync_WhenNextThrows_AndIncludeDetailedTimingsTrue_StoresStartTimeAndFailedFlag()
    {
        // Arrange
        var options = new TimingMiddlewareOptions { IncludeDetailedTimings = true };
        var middleware = new TimingMiddlewareCore(options);
        var foundry = new FakeWorkflowFoundry();
        var operation = CreateMockOperation("TestOp");

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.ExecuteAsync(
                operation,
                foundry,
                null,
                _ => throw new InvalidOperationException("Fail")));

        // Assert
        Assert.True(foundry.Properties.ContainsKey(FoundryPropertyKeys.TimingDuration));
        Assert.True(foundry.Properties.ContainsKey(FoundryPropertyKeys.TimingStartTime));
        Assert.True(foundry.Properties.ContainsKey(FoundryPropertyKeys.TimingFailed));
        Assert.True((bool)foundry.Properties[FoundryPropertyKeys.TimingFailed]!);
    }

    #endregion ExecuteAsync_Failure

    #region ExecuteAsync_Cancellation

    [Fact]
    public async Task ExecuteAsync_WhenNextThrowsOperationCanceledException_Propagates()
    {
        // Arrange
        var options = new TimingMiddlewareOptions();
        var middleware = new TimingMiddlewareCore(options);
        var foundry = new FakeWorkflowFoundry();
        var operation = CreateMockOperation("TestOp");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            middleware.ExecuteAsync(
                operation,
                foundry,
                null,
                ct =>
                {
                    ct.ThrowIfCancellationRequested();
                    return Task.FromResult<object?>(null);
                },
                cts.Token));
    }

    #endregion ExecuteAsync_Cancellation

    #region Helper Methods

    private static IWorkflowOperation CreateMockOperation(string name)
    {
        var mock = new Mock<IWorkflowOperation>();
        mock.Setup(x => x.Name).Returns(name);
        mock.Setup(x => x.Id).Returns(Guid.NewGuid());
        return mock.Object;
    }

    #endregion Helper Methods
}
