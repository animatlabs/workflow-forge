using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Operations;

namespace WorkflowForge.Tests.Operations;

/// <summary>
/// Comprehensive tests for LoggingOperation covering all logging levels, message formatting, and edge cases.
/// </summary>
public class LoggingOperationShould
{
    #region Constructor Tests

    [Fact]
    public void SetProperties_GivenMessage()
    {
        // Arrange
        const string message = "Test log message";

        // Act
        var operation = new LoggingOperation(message);

        // Assert
        Assert.NotEqual(Guid.Empty, operation.Id);
        Assert.Equal("Log: Test log message", operation.Name);
    }

    [Fact]
    public void SetProperties_GivenMessageAndLevel()
    {
        // Arrange
        const string message = "Debug message";
        const WorkflowForgeLogLevel level = WorkflowForgeLogLevel.Debug;

        // Act
        var operation = new LoggingOperation(message, level);

        // Assert
        Assert.NotEqual(Guid.Empty, operation.Id);
        Assert.Equal("Log: Debug message", operation.Name);
    }

    [Fact]
    public void SetCustomName_GivenMessageLevelAndName()
    {
        // Arrange
        const string message = "Custom message";
        const WorkflowForgeLogLevel level = WorkflowForgeLogLevel.Warning;
        const string customName = "CustomLogger";

        // Act
        var operation = new LoggingOperation(message, level, customName);

        // Assert
        Assert.NotEqual(Guid.Empty, operation.Id);
        Assert.Equal(customName, operation.Name);
    }

    [Fact]
    public void ThrowArgumentException_GivenNullMessage()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LoggingOperation(null!, WorkflowForgeLogLevel.Information, "Test"));
    }

    [Fact]
    public void UseDefaultName_GivenNullName()
    {
        // Act
        var operation1 = new LoggingOperation("message", WorkflowForgeLogLevel.Information, null);
        var operation2 = new LoggingOperation("message", WorkflowForgeLogLevel.Information, "");
        var operation3 = new LoggingOperation("message", WorkflowForgeLogLevel.Information, " ");

        // Assert - Null name should use default "Log: {message}" format
        Assert.Equal("Log: message", operation1.Name);
        Assert.Equal("", operation2.Name); // Empty string is preserved
        Assert.Equal(" ", operation3.Name); // Whitespace is preserved
    }

    #endregion Constructor Tests

    #region ForgeAsync Tests

    [Fact]
    public async Task ThrowArgumentNullException_GivenNullFoundry()
    {
        // Arrange
        var operation = new LoggingOperation("Test message");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            operation.ForgeAsync("input", null!, CancellationToken.None));
    }

    [Fact]
    public async Task ReturnInputData_GivenValidFoundry()
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
    public async Task ReturnNull_GivenNullInputData()
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
    public async Task ReturnInputData_GivenComplexInputData()
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

    #endregion ForgeAsync Tests

    #region Logging Level Tests

    [Fact]
    public async Task LogDebugMessage_GivenDebugLevel()
    {
        // Arrange
        const string message = "Debug test message";
        var operation = new LoggingOperation(message, WorkflowForgeLogLevel.Debug);
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
    public async Task LogInfoMessage_GivenInformationLevel()
    {
        // Arrange
        const string message = "Information test message";
        var operation = new LoggingOperation(message, WorkflowForgeLogLevel.Information);
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
    public async Task LogWarningMessage_GivenWarningLevel()
    {
        // Arrange
        const string message = "Warning test message";
        var operation = new LoggingOperation(message, WorkflowForgeLogLevel.Warning);
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
    public async Task LogErrorMessage_GivenErrorLevel()
    {
        // Arrange
        const string message = "Error test message";
        var operation = new LoggingOperation(message, WorkflowForgeLogLevel.Error);
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
    public async Task LogInfoMessage_GivenDefaultLevel()
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

    #endregion Logging Level Tests

    #region Properties Logging Tests

    [Fact]
    public async Task LogCorrectProperties_GivenForgeAsync()
    {
        // Arrange
        const string message = "Test message with properties";
        var operation = new LoggingOperation(message, WorkflowForgeLogLevel.Information, "TestLogger");
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
    public async Task LogNullInputType_GivenNullInput()
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
    public async Task LogCorrectInputType_GivenComplexInput()
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

    #endregion Properties Logging Tests

    #region Cancellation Tests

    [Fact]
    public async Task StillComplete_GivenCancellation()
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
    public async Task StillComplete_GivenPreCancelledToken()
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

    #endregion Cancellation Tests

    #region RestoreAsync Tests

    [Fact]
    public async Task CompleteSuccessfully_GivenRestoreAsync()
    {
        // Arrange
        var operation = new LoggingOperation("Test message");
        var foundry = CreateMockFoundry();

        // Act
        var task = operation.RestoreAsync("output", foundry.Object);

        // Assert
        await task;
        Assert.True(task.Status == TaskStatus.RanToCompletion);
    }

    [Fact]
    public async Task CompleteSuccessfully_GivenRestoreAsyncWithNullFoundry()
    {
        // Arrange - Base implementation returns Task.CompletedTask (no-op), does not validate foundry
        var operation = new LoggingOperation("Test message");

        // Act
        var task = operation.RestoreAsync("output", null!);

        // Assert
        await task;
        Assert.True(task.Status == TaskStatus.RanToCompletion);
    }

    #endregion RestoreAsync Tests

    #region Dispose Tests

    [Fact]
    public void NotThrow_GivenDispose()
    {
        // Arrange
        var operation = new LoggingOperation("Test message");

        // Act & Assert - Should not throw
        operation.Dispose();
    }

    [Fact]
    public void AllowMultipleCalls_GivenDispose()
    {
        // Arrange
        var operation = new LoggingOperation("Test message");

        // Act & Assert - Should not throw
        operation.Dispose();
        operation.Dispose();
        operation.Dispose();
    }

    [Fact]
    public async Task StillWork_GivenForgeAsyncAfterDispose()
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

    #endregion Dispose Tests

    #region Edge Cases and Performance Tests

    [Fact]
    public async Task LogSuccessfully_GivenVeryLongMessage()
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
    public async Task LogSuccessfully_GivenSpecialCharacters()
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
    public async Task RunMultipleInstancesConcurrently_GivenForgeAsync()
    {
        // Arrange
        var operations = new[]
        {
            new LoggingOperation("Message 1", WorkflowForgeLogLevel.Debug),
            new LoggingOperation("Message 2", WorkflowForgeLogLevel.Information),
            new LoggingOperation("Message 3", WorkflowForgeLogLevel.Warning),
            new LoggingOperation("Message 4", WorkflowForgeLogLevel.Error)
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
    public void GenerateUniqueValues_GivenId()
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
    [InlineData(WorkflowForgeLogLevel.Debug)]
    [InlineData(WorkflowForgeLogLevel.Information)]
    [InlineData(WorkflowForgeLogLevel.Warning)]
    [InlineData(WorkflowForgeLogLevel.Error)]
    public async Task LogCorrectly_GivenAllLogLevels(WorkflowForgeLogLevel level)
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
            case WorkflowForgeLogLevel.Debug:
                foundry.Verify(f => f.Logger.LogDebug(It.IsAny<Dictionary<string, string>>(), message), Times.Once);
                break;

            case WorkflowForgeLogLevel.Information:
                foundry.Verify(f => f.Logger.LogInformation(It.IsAny<Dictionary<string, string>>(), message), Times.Once);
                break;

            case WorkflowForgeLogLevel.Warning:
                foundry.Verify(f => f.Logger.LogWarning(It.IsAny<Dictionary<string, string>>(), message), Times.Once);
                break;

            case WorkflowForgeLogLevel.Error:
                foundry.Verify(f => f.Logger.LogError(It.IsAny<Dictionary<string, string>>(), message), Times.Once);
                break;
        }
    }

    #endregion Edge Cases and Performance Tests

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

    #endregion Helper Methods
}
