using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Loggers;
using WorkflowForge.Operations;
using Xunit;
using Moq;

namespace WorkflowForge.Tests.Operations;

/// <summary>
/// Comprehensive tests for LoggingOperation covering all logging levels, message formatting, and edge cases.
/// </summary>
public class LoggingOperationTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithMessage_SetsProperties()
    {
        // Arrange
        const string message = "Test log message";

        // Act
        var operation = new LoggingOperation(message);

        // Assert
        Assert.NotEqual(Guid.Empty, operation.Id);
        Assert.Equal("Log: Test log message", operation.Name);
        Assert.False(operation.SupportsRestore);
    }

    [Fact]
    public void Constructor_WithMessageAndLevel_SetsProperties()
    {
        // Arrange
        const string message = "Debug message";
        const LogLevel level = LogLevel.Debug;

        // Act
        var operation = new LoggingOperation(message, level);

        // Assert
        Assert.NotEqual(Guid.Empty, operation.Id);
        Assert.Equal("Log: Debug message", operation.Name);
        Assert.False(operation.SupportsRestore);
    }

    [Fact]
    public void Constructor_WithMessageLevelAndName_SetsCustomName()
    {
        // Arrange
        const string message = "Custom message";
        const LogLevel level = LogLevel.Warning;
        const string customName = "CustomLogger";

        // Act
        var operation = new LoggingOperation(message, level, customName);

        // Assert
        Assert.NotEqual(Guid.Empty, operation.Id);
        Assert.Equal(customName, operation.Name);
        Assert.False(operation.SupportsRestore);
    }

    [Fact]
    public void Constructor_WithNullMessage_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LoggingOperation(null!, LogLevel.Information, "Test"));
    }

    [Fact]
    public void Constructor_WithNullName_UsesDefaultName()
    {
        // Act
        var operation1 = new LoggingOperation("message", LogLevel.Information, null);
        var operation2 = new LoggingOperation("message", LogLevel.Information, "");
        var operation3 = new LoggingOperation("message", LogLevel.Information, " ");

        // Assert - Null name should use default "Log: {message}" format
        Assert.Equal("Log: message", operation1.Name);
        Assert.Equal("", operation2.Name); // Empty string is preserved
        Assert.Equal(" ", operation3.Name); // Whitespace is preserved
    }

    #endregion

    #region ForgeAsync Tests

    [Fact]
    public async Task ForgeAsync_WithNullFoundry_ThrowsArgumentNullException()
    {
        // Arrange
        var operation = new LoggingOperation("Test message");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            operation.ForgeAsync("input", null!, CancellationToken.None));
    }

    [Fact]
    public async Task ForgeAsync_WithValidFoundry_ReturnsInputData()
    {
        // Arrange
        var operation = new LoggingOperation("Test message");
        var foundry = CreateMockFoundry();
        const string inputData = "test-input";

        // Act
        var result = await operation.ForgeAsync(inputData, foundry.Object);

        // Assert
        Assert.Equal(inputData, result);
    }

    [Fact]
    public async Task ForgeAsync_WithNullInputData_ReturnsNull()
    {
        // Arrange
        var operation = new LoggingOperation("Test message");
        var foundry = CreateMockFoundry();

        // Act
        var result = await operation.ForgeAsync(null, foundry.Object);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ForgeAsync_WithComplexInputData_ReturnsInputData()
    {
        // Arrange
        var operation = new LoggingOperation("Test message");
        var foundry = CreateMockFoundry();
        var inputData = new { Name = "Test", Value = 42 };

        // Act
        var result = await operation.ForgeAsync(inputData, foundry.Object);

        // Assert
        Assert.Same(inputData, result);
    }

    #endregion

    #region Logging Level Tests

    [Fact]
    public async Task ForgeAsync_WithDebugLevel_LogsDebugMessage()
    {
        // Arrange
        const string message = "Debug test message";
        var operation = new LoggingOperation(message, LogLevel.Debug);
        var foundry = CreateMockFoundry();

        // Act
        await operation.ForgeAsync("input", foundry.Object);

        // Assert
        foundry.Verify(f => f.Logger.LogDebug(It.IsAny<Dictionary<string, string>>(), message), Times.Once);
        foundry.Verify(f => f.Logger.LogInformation(It.IsAny<Dictionary<string, string>>(), It.IsAny<string>()), Times.Never);
        foundry.Verify(f => f.Logger.LogWarning(It.IsAny<Dictionary<string, string>>(), It.IsAny<string>()), Times.Never);
        foundry.Verify(f => f.Logger.LogError(It.IsAny<Dictionary<string, string>>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ForgeAsync_WithInformationLevel_LogsInfoMessage()
    {
        // Arrange
        const string message = "Information test message";
        var operation = new LoggingOperation(message, LogLevel.Information);
        var foundry = CreateMockFoundry();

        // Act
        await operation.ForgeAsync("input", foundry.Object);

        // Assert
        foundry.Verify(f => f.Logger.LogInformation(It.IsAny<Dictionary<string, string>>(), message), Times.Once);
        foundry.Verify(f => f.Logger.LogDebug(It.IsAny<Dictionary<string, string>>(), It.IsAny<string>()), Times.Never);
        foundry.Verify(f => f.Logger.LogWarning(It.IsAny<Dictionary<string, string>>(), It.IsAny<string>()), Times.Never);
        foundry.Verify(f => f.Logger.LogError(It.IsAny<Dictionary<string, string>>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ForgeAsync_WithWarningLevel_LogsWarningMessage()
    {
        // Arrange
        const string message = "Warning test message";
        var operation = new LoggingOperation(message, LogLevel.Warning);
        var foundry = CreateMockFoundry();

        // Act
        await operation.ForgeAsync("input", foundry.Object);

        // Assert
        foundry.Verify(f => f.Logger.LogWarning(It.IsAny<Dictionary<string, string>>(), message), Times.Once);
        foundry.Verify(f => f.Logger.LogDebug(It.IsAny<Dictionary<string, string>>(), It.IsAny<string>()), Times.Never);
        foundry.Verify(f => f.Logger.LogInformation(It.IsAny<Dictionary<string, string>>(), It.IsAny<string>()), Times.Never);
        foundry.Verify(f => f.Logger.LogError(It.IsAny<Dictionary<string, string>>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ForgeAsync_WithErrorLevel_LogsErrorMessage()
    {
        // Arrange
        const string message = "Error test message";
        var operation = new LoggingOperation(message, LogLevel.Error);
        var foundry = CreateMockFoundry();

        // Act
        await operation.ForgeAsync("input", foundry.Object);

        // Assert
        foundry.Verify(f => f.Logger.LogError(It.IsAny<Dictionary<string, string>>(), message), Times.Once);
        foundry.Verify(f => f.Logger.LogDebug(It.IsAny<Dictionary<string, string>>(), It.IsAny<string>()), Times.Never);
        foundry.Verify(f => f.Logger.LogInformation(It.IsAny<Dictionary<string, string>>(), It.IsAny<string>()), Times.Never);
        foundry.Verify(f => f.Logger.LogWarning(It.IsAny<Dictionary<string, string>>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ForgeAsync_WithDefaultLevel_LogsInfoMessage()
    {
        // Arrange
        const string message = "Default level message";
        var operation = new LoggingOperation(message); // Default should be Information
        var foundry = CreateMockFoundry();

        // Act
        await operation.ForgeAsync("input", foundry.Object);

        // Assert
        foundry.Verify(f => f.Logger.LogInformation(It.IsAny<Dictionary<string, string>>(), message), Times.Once);
    }

    #endregion

    #region Properties Logging Tests

    [Fact]
    public async Task ForgeAsync_LogsCorrectProperties()
    {
        // Arrange
        const string message = "Test message with properties";
        var operation = new LoggingOperation(message, LogLevel.Information, "TestLogger");
        var foundry = CreateMockFoundry();
        var executionId = Guid.NewGuid();
        var workflowName = "TestWorkflow";
        foundry.Setup(f => f.ExecutionId).Returns(executionId);
        
        var mockWorkflow = new Mock<IWorkflow>();
        mockWorkflow.Setup(w => w.Name).Returns(workflowName);
        foundry.Setup(f => f.CurrentWorkflow).Returns(mockWorkflow.Object);

        Dictionary<string, string>? loggedProperties = null;
        foundry.Setup(f => f.Logger.LogInformation(It.IsAny<IDictionary<string, string>>(), It.IsAny<string>(), It.IsAny<object[]>()))
              .Callback<IDictionary<string, string>, string, object[]>((props, _, __) => loggedProperties = new Dictionary<string, string>(props));

        // Act
        await operation.ForgeAsync("test-input", foundry.Object);

        // Assert
        Assert.NotNull(loggedProperties);
        Assert.Equal(operation.Id.ToString(), loggedProperties["OperationId"]);
        Assert.Equal("TestLogger", loggedProperties["OperationName"]);
        Assert.Equal(executionId.ToString(), loggedProperties["WorkflowId"]);
        Assert.Equal(workflowName, loggedProperties["WorkflowName"]);
        Assert.Equal("String", loggedProperties["InputType"]);
    }

    [Fact]
    public async Task ForgeAsync_WithNullInput_LogsNullInputType()
    {
        // Arrange
        var operation = new LoggingOperation("Test message");
        var foundry = CreateMockFoundry();

        Dictionary<string, string>? loggedProperties = null;
        foundry.Setup(f => f.Logger.LogInformation(It.IsAny<IDictionary<string, string>>(), It.IsAny<string>(), It.IsAny<object[]>()))
              .Callback<IDictionary<string, string>, string, object[]>((props, _, __) => loggedProperties = new Dictionary<string, string>(props));

        // Act
        await operation.ForgeAsync(null, foundry.Object);

        // Assert
        Assert.NotNull(loggedProperties);
        Assert.Equal("null", loggedProperties["InputType"]);
    }

    [Fact]
    public async Task ForgeAsync_WithComplexInput_LogsCorrectInputType()
    {
        // Arrange
        var operation = new LoggingOperation("Test message");
        var foundry = CreateMockFoundry();
        var inputData = new List<string> { "item1", "item2" };

        Dictionary<string, string>? loggedProperties = null;
        foundry.Setup(f => f.Logger.LogInformation(It.IsAny<IDictionary<string, string>>(), It.IsAny<string>(), It.IsAny<object[]>()))
              .Callback<IDictionary<string, string>, string, object[]>((props, _, __) => loggedProperties = new Dictionary<string, string>(props));

        // Act
        await operation.ForgeAsync(inputData, foundry.Object);

        // Assert
        Assert.NotNull(loggedProperties);
        Assert.Equal("List`1", loggedProperties["InputType"]);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ForgeAsync_WithCancellation_StillCompletes()
    {
        // Arrange
        var operation = new LoggingOperation("Test message");
        var foundry = CreateMockFoundry();
        using var cts = new CancellationTokenSource();

        // Act
        cts.Cancel();
        var result = await operation.ForgeAsync("input", foundry.Object, cts.Token);

        // Assert - Logging operation should complete even with cancelled token
        Assert.Equal("input", result);
        foundry.Verify(f => f.Logger.LogInformation(It.IsAny<Dictionary<string, string>>(), "Test message"), Times.Once);
    }

    [Fact]
    public async Task ForgeAsync_WithPreCancelledToken_StillCompletes()
    {
        // Arrange
        var operation = new LoggingOperation("Test message");
        var foundry = CreateMockFoundry();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await operation.ForgeAsync("input", foundry.Object, cts.Token);

        // Assert - Logging operation should complete even with pre-cancelled token
        Assert.Equal("input", result);
        foundry.Verify(f => f.Logger.LogInformation(It.IsAny<Dictionary<string, string>>(), "Test message"), Times.Once);
    }

    #endregion

    #region RestoreAsync Tests

    [Fact]
    public async Task RestoreAsync_ThrowsNotSupportedException()
    {
        // Arrange
        var operation = new LoggingOperation("Test message");
        var foundry = CreateMockFoundry();

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() => 
            operation.RestoreAsync("output", foundry.Object));
    }

    [Fact]
    public async Task RestoreAsync_WithNullFoundry_ThrowsNotSupportedException()
    {
        // Arrange
        var operation = new LoggingOperation("Test message");

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() => 
            operation.RestoreAsync("output", null!));
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        // Arrange
        var operation = new LoggingOperation("Test message");

        // Act & Assert - Should not throw
        operation.Dispose();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var operation = new LoggingOperation("Test message");

        // Act & Assert - Should not throw
        operation.Dispose();
        operation.Dispose();
        operation.Dispose();
    }

    [Fact]
    public async Task ForgeAsync_AfterDispose_StillWorks()
    {
        // Arrange
        var operation = new LoggingOperation("Test message");
        var foundry = CreateMockFoundry();
        operation.Dispose();

        // Act
        var result = await operation.ForgeAsync("input", foundry.Object);

        // Assert
        Assert.Equal("input", result);
        foundry.Verify(f => f.Logger.LogInformation(It.IsAny<Dictionary<string, string>>(), "Test message"), Times.Once);
    }

    #endregion

    #region Edge Cases and Performance Tests

    [Fact]
    public async Task ForgeAsync_WithVeryLongMessage_LogsSuccessfully()
    {
        // Arrange
        var longMessage = new string('A', 10000);
        var operation = new LoggingOperation(longMessage);
        var foundry = CreateMockFoundry();

        // Act
        var result = await operation.ForgeAsync("input", foundry.Object);

        // Assert
        Assert.Equal("input", result);
        foundry.Verify(f => f.Logger.LogInformation(It.IsAny<Dictionary<string, string>>(), longMessage), Times.Once);
    }

    [Fact]
    public async Task ForgeAsync_WithSpecialCharacters_LogsSuccessfully()
    {
        // Arrange
        const string specialMessage = "Test message with special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?";
        var operation = new LoggingOperation(specialMessage);
        var foundry = CreateMockFoundry();

        // Act
        var result = await operation.ForgeAsync("input", foundry.Object);

        // Assert
        Assert.Equal("input", result);
        foundry.Verify(f => f.Logger.LogInformation(It.IsAny<Dictionary<string, string>>(), specialMessage), Times.Once);
    }

    [Fact]
    public async Task ForgeAsync_MultipleInstancesConcurrently()
    {
        // Arrange
        var operations = new[]
        {
            new LoggingOperation("Message 1", LogLevel.Debug),
            new LoggingOperation("Message 2", LogLevel.Information),
            new LoggingOperation("Message 3", LogLevel.Warning),
            new LoggingOperation("Message 4", LogLevel.Error)
        };
        var foundry = CreateMockFoundry();

        // Act
        var tasks = operations.Select(op => op.ForgeAsync($"input-{op.Name}", foundry.Object));
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(4, results.Length);
        Assert.All(results, Assert.NotNull);
        foundry.Verify(f => f.Logger.LogDebug(It.IsAny<Dictionary<string, string>>(), "Message 1"), Times.Once);
        foundry.Verify(f => f.Logger.LogInformation(It.IsAny<Dictionary<string, string>>(), "Message 2"), Times.Once);
        foundry.Verify(f => f.Logger.LogWarning(It.IsAny<Dictionary<string, string>>(), "Message 3"), Times.Once);
        foundry.Verify(f => f.Logger.LogError(It.IsAny<Dictionary<string, string>>(), "Message 4"), Times.Once);
    }

    [Fact]
    public void Id_GeneratesUniqueValues()
    {
        // Arrange & Act
        var operation1 = new LoggingOperation("Message 1");
        var operation2 = new LoggingOperation("Message 2");

        // Assert
        Assert.NotEqual(operation1.Id, operation2.Id);
        Assert.NotEqual(Guid.Empty, operation1.Id);
        Assert.NotEqual(Guid.Empty, operation2.Id);
    }

    [Theory]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    public async Task ForgeAsync_AllLogLevels_LogCorrectly(LogLevel level)
    {
        // Arrange
        var message = $"Test message for {level}";
        var operation = new LoggingOperation(message, level);
        var foundry = CreateMockFoundry();

        // Act
        await operation.ForgeAsync("input", foundry.Object);

        // Assert
        switch (level)
        {
            case LogLevel.Debug:
                foundry.Verify(f => f.Logger.LogDebug(It.IsAny<Dictionary<string, string>>(), message), Times.Once);
                break;
            case LogLevel.Information:
                foundry.Verify(f => f.Logger.LogInformation(It.IsAny<Dictionary<string, string>>(), message), Times.Once);
                break;
            case LogLevel.Warning:
                foundry.Verify(f => f.Logger.LogWarning(It.IsAny<Dictionary<string, string>>(), message), Times.Once);
                break;
            case LogLevel.Error:
                foundry.Verify(f => f.Logger.LogError(It.IsAny<Dictionary<string, string>>(), message), Times.Once);
                break;
        }
    }

    #endregion

    #region Helper Methods

    private static Mock<IWorkflowFoundry> CreateMockFoundry()
    {
        var foundry = new Mock<IWorkflowFoundry>();
        var logger = new Mock<IWorkflowForgeLogger>();
        
        foundry.Setup(f => f.Logger).Returns(logger.Object);
        foundry.Setup(f => f.ExecutionId).Returns(Guid.NewGuid());
        
        var mockWorkflow = new Mock<IWorkflow>();
        mockWorkflow.Setup(w => w.Name).Returns("TestWorkflow");
        foundry.Setup(f => f.CurrentWorkflow).Returns(mockWorkflow.Object);

        return foundry;
    }

    #endregion
} 