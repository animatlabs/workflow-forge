using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Constants;
using WorkflowForge.Testing;
using WorkflowForge.Middleware;
using WorkflowForge.Options.Middleware;
using Xunit;

namespace WorkflowForge.Tests.Middleware;

/// <summary>
/// Unit tests for ErrorHandlingMiddleware - error wrapping, rethrow, and swallow behavior.
/// </summary>
public class ErrorHandlingMiddlewareTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new ErrorHandlingMiddlewareOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ErrorHandlingMiddleware(null!, options));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = TestNullLogger.Instance;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ErrorHandlingMiddleware(logger, null!));
    }

    [Fact]
    public void Constructor_WithValidArgs_Initializes()
    {
        // Arrange
        var logger = TestNullLogger.Instance;
        var options = new ErrorHandlingMiddlewareOptions();

        // Act
        var middleware = new ErrorHandlingMiddleware(logger, options);

        // Assert - no exception thrown
        Assert.NotNull(middleware);
    }

    [Fact]
    public void Constructor_BackwardCompatibility_WithRethrowFlag_Initializes()
    {
        // Arrange
        var logger = TestNullLogger.Instance;

        // Act
        var middleware = new ErrorHandlingMiddleware(logger, rethrowExceptions: false);

        // Assert - no exception thrown
        Assert.NotNull(middleware);
    }

    #endregion Constructor Tests

    #region ExecuteAsync_Success

    [Fact]
    public async Task ExecuteAsync_WhenNextSucceeds_ReturnsResult()
    {
        // Arrange
        var logger = TestNullLogger.Instance;
        var options = new ErrorHandlingMiddlewareOptions { RethrowExceptions = true };
        var middleware = new ErrorHandlingMiddleware(logger, options);
        var foundry = new FakeWorkflowFoundry();
        var operation = CreateMockOperation("TestOp");
        var expectedResult = "success-result";

        // Act
        var result = await middleware.ExecuteAsync(
            operation,
            foundry,
            null,
            _ => Task.FromResult<object?>(expectedResult));

        // Assert
        Assert.Equal(expectedResult, result);
    }

    #endregion ExecuteAsync_Success

    #region ExecuteAsync_Exception_Rethrow

    [Fact]
    public async Task ExecuteAsync_WhenNextThrows_AndRethrowTrue_PropagatesException()
    {
        // Arrange
        var logger = TestNullLogger.Instance;
        var options = new ErrorHandlingMiddlewareOptions { RethrowExceptions = true };
        var middleware = new ErrorHandlingMiddleware(logger, options);
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
    public async Task ExecuteAsync_WhenNextThrows_AndRethrowTrue_StoresErrorPropertiesInFoundry()
    {
        // Arrange
        var logger = TestNullLogger.Instance;
        var options = new ErrorHandlingMiddlewareOptions { RethrowExceptions = true };
        var middleware = new ErrorHandlingMiddleware(logger, options);
        var foundry = new FakeWorkflowFoundry();
        var operation = CreateMockOperation("TestOp");
        var expectedEx = new InvalidOperationException("Test failure message");

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.ExecuteAsync(
                operation,
                foundry,
                null,
                _ => throw expectedEx));

        // Assert - error properties should be stored before rethrow
        Assert.True(foundry.Properties.ContainsKey(FoundryPropertyKeys.ErrorMessage));
        Assert.Equal("Test failure message", foundry.Properties[FoundryPropertyKeys.ErrorMessage]);
        Assert.True(foundry.Properties.ContainsKey(FoundryPropertyKeys.ErrorType));
        Assert.Equal("InvalidOperationException", foundry.Properties[FoundryPropertyKeys.ErrorType]);
        Assert.True(foundry.Properties.ContainsKey(FoundryPropertyKeys.ErrorException));
        Assert.Same(expectedEx, foundry.Properties[FoundryPropertyKeys.ErrorException]);
        Assert.True(foundry.Properties.ContainsKey(FoundryPropertyKeys.ErrorTimestamp));
    }

    [Fact]
    public async Task ExecuteAsync_WhenNextThrows_AndIncludeStackTracesTrue_StoresStackTrace()
    {
        // Arrange
        var logger = TestNullLogger.Instance;
        var options = new ErrorHandlingMiddlewareOptions { RethrowExceptions = true, IncludeStackTraces = true };
        var middleware = new ErrorHandlingMiddleware(logger, options);
        var foundry = new FakeWorkflowFoundry();
        var operation = CreateMockOperation("TestOp");

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.ExecuteAsync(
                operation,
                foundry,
                null,
                _ => throw new InvalidOperationException("Test")));

        // Assert
        Assert.True(foundry.Properties.ContainsKey(FoundryPropertyKeys.ErrorStackTrace));
        Assert.NotNull(foundry.Properties[FoundryPropertyKeys.ErrorStackTrace]);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNextThrows_AndIncludeStackTracesFalse_DoesNotStoreStackTrace()
    {
        // Arrange
        var logger = TestNullLogger.Instance;
        var options = new ErrorHandlingMiddlewareOptions { RethrowExceptions = true, IncludeStackTraces = false };
        var middleware = new ErrorHandlingMiddleware(logger, options);
        var foundry = new FakeWorkflowFoundry();
        var operation = CreateMockOperation("TestOp");

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.ExecuteAsync(
                operation,
                foundry,
                null,
                _ => throw new InvalidOperationException("Test")));

        // Assert
        Assert.False(foundry.Properties.ContainsKey(FoundryPropertyKeys.ErrorStackTrace));
    }

    #endregion ExecuteAsync_Exception_Rethrow

    #region ExecuteAsync_Exception_Swallow

    [Fact]
    public async Task ExecuteAsync_WhenNextThrows_AndRethrowFalse_ReturnsDefaultValue()
    {
        // Arrange
        var logger = TestNullLogger.Instance;
        var options = new ErrorHandlingMiddlewareOptions { RethrowExceptions = false };
        var middleware = new ErrorHandlingMiddleware(logger, options);
        var foundry = new FakeWorkflowFoundry();
        var operation = CreateMockOperation("TestOp");

        // Act
        var result = await middleware.ExecuteAsync(
            operation,
            foundry,
            null,
            _ => throw new InvalidOperationException("Test failure"));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNextThrows_AndRethrowFalse_WithCustomDefaultReturnValue_ReturnsCustomValue()
    {
        // Arrange
        var logger = TestNullLogger.Instance;
        var defaultReturn = "fallback-value";
        var options = new ErrorHandlingMiddlewareOptions { RethrowExceptions = false };
        var middleware = new ErrorHandlingMiddleware(logger, options, defaultReturnValue: defaultReturn);
        var foundry = new FakeWorkflowFoundry();
        var operation = CreateMockOperation("TestOp");

        // Act
        var result = await middleware.ExecuteAsync(
            operation,
            foundry,
            null,
            _ => throw new InvalidOperationException("Test failure"));

        // Assert
        Assert.Equal(defaultReturn, result);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNextThrows_AndRethrowFalse_StillStoresErrorProperties()
    {
        // Arrange
        var logger = TestNullLogger.Instance;
        var options = new ErrorHandlingMiddlewareOptions { RethrowExceptions = false };
        var middleware = new ErrorHandlingMiddleware(logger, options);
        var foundry = new FakeWorkflowFoundry();
        var operation = CreateMockOperation("TestOp");

        // Act
        await middleware.ExecuteAsync(
            operation,
            foundry,
            null,
            _ => throw new ArgumentException("Arg error"));

        // Assert
        Assert.True(foundry.Properties.ContainsKey(FoundryPropertyKeys.ErrorMessage));
        Assert.Equal("Arg error", foundry.Properties[FoundryPropertyKeys.ErrorMessage]);
        Assert.True(foundry.Properties.ContainsKey(FoundryPropertyKeys.ErrorType));
        Assert.Equal("ArgumentException", foundry.Properties[FoundryPropertyKeys.ErrorType]);
    }

    #endregion ExecuteAsync_Exception_Swallow

    #region ExecuteAsync_Cancellation

    [Fact]
    public async Task ExecuteAsync_WhenNextThrowsOperationCanceledException_PropagatesWithoutLogging()
    {
        // Arrange
        var logger = TestNullLogger.Instance;
        var options = new ErrorHandlingMiddlewareOptions { RethrowExceptions = true };
        var middleware = new ErrorHandlingMiddleware(logger, options);
        var foundry = new FakeWorkflowFoundry();
        var operation = CreateMockOperation("TestOp");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - OperationCanceledException should propagate (not wrapped)
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

    [Fact]
    public async Task ExecuteAsync_WhenNextThrowsTaskCanceledException_PropagatesWithoutLogging()
    {
        // Arrange
        var logger = TestNullLogger.Instance;
        var options = new ErrorHandlingMiddlewareOptions { RethrowExceptions = true };
        var middleware = new ErrorHandlingMiddleware(logger, options);
        var foundry = new FakeWorkflowFoundry();
        var operation = CreateMockOperation("TestOp");

        // Act & Assert - TaskCanceledException should propagate (not wrapped)
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            middleware.ExecuteAsync(
                operation,
                foundry,
                null,
                _ => throw new TaskCanceledException()));
    }

    // Note: OperationCanceledException and TaskCanceledException are re-thrown without
    // storing error properties - they propagate immediately for cancellation flow
    [Fact]
    public async Task ExecuteAsync_WhenNextThrowsOperationCanceledException_DoesNotStoreErrorProperties()
    {
        // Arrange
        var logger = TestNullLogger.Instance;
        var options = new ErrorHandlingMiddlewareOptions { RethrowExceptions = true };
        var middleware = new ErrorHandlingMiddleware(logger, options);
        var foundry = new FakeWorkflowFoundry();
        var operation = CreateMockOperation("TestOp");

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            middleware.ExecuteAsync(
                operation,
                foundry,
                null,
                _ => throw new OperationCanceledException()));

        // Cancellation exceptions are re-thrown without storing error properties
        Assert.False(foundry.Properties.ContainsKey(FoundryPropertyKeys.ErrorMessage));
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
