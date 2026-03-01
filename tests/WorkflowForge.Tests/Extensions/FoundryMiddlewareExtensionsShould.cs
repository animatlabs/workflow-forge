using System;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using TimingMiddlewareCore = WorkflowForge.Middleware.TimingMiddleware;
using WorkflowForge.Middleware;
using WorkflowForge.Options.Middleware;
using WorkflowForge.Testing;
using Xunit;

namespace WorkflowForge.Tests.Extensions;

/// <summary>
/// Unit tests for FoundryMiddlewareExtensions - UseLogging, UseTiming, UseErrorHandling, UseDefaultMiddleware.
/// </summary>
public class FoundryMiddlewareExtensionsShould
{
    #region UseLogging

    [Fact]
    public void AddLoggingMiddleware_GivenFoundryLogger()
    {
        // Arrange
        var foundry = new FakeWorkflowFoundry();

        // Act
        var result = foundry.UseLogging();

        // Assert
        Assert.Same(foundry, result);
        Assert.Single(foundry.Middlewares);
        Assert.IsType<LoggingMiddleware>(foundry.Middlewares[0]);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullFoundryForUseLogging()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            FoundryMiddlewareExtensions.UseLogging(null!));
    }

    [Fact]
    public void AddLoggingMiddleware_GivenCustomLogger()
    {
        // Arrange
        var foundry = new FakeWorkflowFoundry();
        var logger = TestNullLogger.Instance;

        // Act
        var result = foundry.UseLogging(logger);

        // Assert
        Assert.Same(foundry, result);
        Assert.Single(foundry.Middlewares);
        Assert.IsType<LoggingMiddleware>(foundry.Middlewares[0]);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullFoundryAndLogger()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            FoundryMiddlewareExtensions.UseLogging(null!, TestNullLogger.Instance));
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullLogger()
    {
        // Arrange
        var foundry = new FakeWorkflowFoundry();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            foundry.UseLogging(null!));
    }

    #endregion UseLogging

    #region UseTiming

    [Fact]
    public void AddTimingMiddleware_GivenUseTiming()
    {
        // Arrange
        var foundry = new FakeWorkflowFoundry();

        // Act
        var result = foundry.UseTiming();

        // Assert
        Assert.Same(foundry, result);
        Assert.Single(foundry.Middlewares);
        Assert.IsType<TimingMiddlewareCore>(foundry.Middlewares[0]);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullFoundryForUseTiming()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            FoundryMiddlewareExtensions.UseTiming(null!));
    }

    [Fact]
    public void AddTimingMiddleware_GivenTimeProvider()
    {
        // Arrange
        var foundry = new FakeWorkflowFoundry();
        var timeProvider = new Mock<ISystemTimeProvider>().Object;

        // Act
        var result = foundry.UseTiming(timeProvider);

        // Assert
        Assert.Same(foundry, result);
        Assert.Single(foundry.Middlewares);
        Assert.IsType<TimingMiddlewareCore>(foundry.Middlewares[0]);
    }

    #endregion UseTiming

    #region UseErrorHandling

    [Fact]
    public void AddErrorHandlingMiddleware_GivenUseErrorHandling()
    {
        // Arrange
        var foundry = new FakeWorkflowFoundry();

        // Act
        var result = foundry.UseErrorHandling();

        // Assert
        Assert.Same(foundry, result);
        Assert.Single(foundry.Middlewares);
        Assert.IsType<ErrorHandlingMiddleware>(foundry.Middlewares[0]);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullFoundryForUseErrorHandling()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            FoundryMiddlewareExtensions.UseErrorHandling(null!));
    }

    [Fact]
    public void AddErrorHandlingMiddleware_GivenRethrowFalse()
    {
        // Arrange
        var foundry = new FakeWorkflowFoundry();

        // Act
        var result = foundry.UseErrorHandling(rethrowExceptions: false);

        // Assert
        Assert.Same(foundry, result);
        Assert.Single(foundry.Middlewares);
        Assert.IsType<ErrorHandlingMiddleware>(foundry.Middlewares[0]);
    }

    [Fact]
    public void AddErrorHandlingMiddleware_GivenDefaultReturnValue()
    {
        // Arrange
        var foundry = new FakeWorkflowFoundry();

        // Act
        var result = foundry.UseErrorHandling(defaultReturnValue: "fallback");

        // Assert
        Assert.Same(foundry, result);
        Assert.Single(foundry.Middlewares);
    }

    #endregion UseErrorHandling

    #region UseDefaultMiddleware

    [Fact]
    public void ThrowArgumentNullException_GivenNullFoundryForUseDefaultMiddleware()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            FoundryMiddlewareExtensions.UseDefaultMiddleware(null!));
    }

    [Fact]
    public void AddAllEnabledMiddleware_GivenDefaults()
    {
        // Arrange
        var foundry = new FakeWorkflowFoundry();

        // Act
        var result = foundry.UseDefaultMiddleware();

        // Assert
        Assert.Same(foundry, result);
        // Order: ErrorHandling (outermost), Timing, Logging (innermost)
        Assert.Equal(3, foundry.Middlewares.Count);
        Assert.IsType<ErrorHandlingMiddleware>(foundry.Middlewares[0]);
        Assert.IsType<TimingMiddlewareCore>(foundry.Middlewares[1]);
        Assert.IsType<LoggingMiddleware>(foundry.Middlewares[2]);
    }

    [Fact]
    public void AddOnlyTimingAndLogging_GivenErrorHandlingDisabled()
    {
        // Arrange
        var foundry = new FakeWorkflowFoundry();
        var errorOptions = new ErrorHandlingMiddlewareOptions { Enabled = false };

        // Act
        var result = foundry.UseDefaultMiddleware(errorHandlingOptions: errorOptions);

        // Assert
        Assert.Same(foundry, result);
        Assert.Equal(2, foundry.Middlewares.Count);
        Assert.IsType<TimingMiddlewareCore>(foundry.Middlewares[0]);
        Assert.IsType<LoggingMiddleware>(foundry.Middlewares[1]);
    }

    [Fact]
    public void AddOnlyErrorHandlingAndLogging_GivenTimingDisabled()
    {
        // Arrange
        var foundry = new FakeWorkflowFoundry();
        var timingOptions = new TimingMiddlewareOptions { Enabled = false };

        // Act
        var result = foundry.UseDefaultMiddleware(timingOptions: timingOptions);

        // Assert
        Assert.Same(foundry, result);
        Assert.Equal(2, foundry.Middlewares.Count);
        Assert.IsType<ErrorHandlingMiddleware>(foundry.Middlewares[0]);
        Assert.IsType<LoggingMiddleware>(foundry.Middlewares[1]);
    }

    [Fact]
    public void AddOnlyErrorHandlingAndTiming_GivenLoggingDisabled()
    {
        // Arrange
        var foundry = new FakeWorkflowFoundry();
        var loggingOptions = new LoggingMiddlewareOptions { Enabled = false };

        // Act
        var result = foundry.UseDefaultMiddleware(loggingOptions: loggingOptions);

        // Assert
        Assert.Same(foundry, result);
        Assert.Equal(2, foundry.Middlewares.Count);
        Assert.IsType<ErrorHandlingMiddleware>(foundry.Middlewares[0]);
        Assert.IsType<TimingMiddlewareCore>(foundry.Middlewares[1]);
    }

    [Fact]
    public void ThrowArgumentException_GivenInvalidLoggingOptions()
    {
        // Arrange
        var foundry = new FakeWorkflowFoundry();
        var loggingOptions = new LoggingMiddlewareOptions { MinimumLevel = "InvalidLevel" };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            foundry.UseDefaultMiddleware(loggingOptions: loggingOptions));
        Assert.Equal("loggingOptions", ex.ParamName);
    }

    [Fact]
    public void UseDefaults_GivenNullOptions()
    {
        // Arrange
        var foundry = new FakeWorkflowFoundry();

        // Act
        var result = foundry.UseDefaultMiddleware(
            errorHandlingOptions: null,
            timingOptions: null,
            loggingOptions: null);

        // Assert
        Assert.Same(foundry, result);
        Assert.Equal(3, foundry.Middlewares.Count);
    }

    #endregion UseDefaultMiddleware

    #region Method Chaining

    [Fact]
    public void ReturnFoundryForChaining_GivenUseLogging()
    {
        // Arrange
        var foundry = new FakeWorkflowFoundry();

        // Act
        var result = foundry.UseLogging().UseTiming();

        // Assert
        Assert.Same(foundry, result);
        Assert.Equal(2, foundry.Middlewares.Count);
    }

    #endregion Method Chaining
}
