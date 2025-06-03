using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Loggers;
using WorkflowForge.Operations;
using Xunit;
using Moq;

namespace WorkflowForge.Tests.Operations;

/// <summary>
/// Comprehensive tests for DelayOperation covering functionality, timing, cancellation, and edge cases.
/// </summary>
public class DelayOperationTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithTimeSpan_SetsProperties()
    {
        // Arrange
        var delay = TimeSpan.FromMilliseconds(100);

        // Act
        var operation = new DelayOperation(delay);

        // Assert
        Assert.NotEqual(Guid.Empty, operation.Id);
        Assert.Equal("Delay 100ms", operation.Name);
        Assert.False(operation.SupportsRestore);
    }

    [Fact]
    public void Constructor_WithTimeSpanAndName_SetsCustomName()
    {
        // Arrange
        var delay = TimeSpan.FromSeconds(1);
        const string customName = "Custom Delay";

        // Act
        var operation = new DelayOperation(delay, customName);

        // Assert
        Assert.NotEqual(Guid.Empty, operation.Id);
        Assert.Equal(customName, operation.Name);
        Assert.False(operation.SupportsRestore);
    }

    [Fact]
    public void Constructor_WithZeroDelay_SetsName()
    {
        // Arrange
        var delay = TimeSpan.Zero;

        // Act
        var operation = new DelayOperation(delay);

        // Assert
        Assert.Equal("Delay 0ms", operation.Name);
    }

    [Fact]
    public void Constructor_WithNegativeDelay_SetsName()
    {
        // Arrange
        var delay = TimeSpan.FromMilliseconds(-100);

        // Act
        var operation = new DelayOperation(delay);

        // Assert
        Assert.Equal("Delay -100ms", operation.Name);
    }

    #endregion

    #region Static Factory Methods Tests

    [Fact]
    public void FromMilliseconds_CreatesDelayOperation()
    {
        // Arrange
        const int milliseconds = 250;

        // Act
        var operation = DelayOperation.FromMilliseconds(milliseconds);

        // Assert
        Assert.NotNull(operation);
        Assert.Equal("Delay 250ms", operation.Name);
    }

    [Fact]
    public void FromSeconds_CreatesDelayOperation()
    {
        // Arrange
        const int seconds = 2;

        // Act
        var operation = DelayOperation.FromSeconds(seconds);

        // Assert
        Assert.NotNull(operation);
        Assert.Equal("Delay 2000ms", operation.Name);
    }

    [Fact]
    public void FromMinutes_CreatesDelayOperation()
    {
        // Arrange
        const int minutes = 1;

        // Act
        var operation = DelayOperation.FromMinutes(minutes);

        // Assert
        Assert.NotNull(operation);
        Assert.Equal("Delay 60000ms", operation.Name);
    }

    [Fact]
    public void FromMilliseconds_WithZero_CreatesOperation()
    {
        // Act
        var operation = DelayOperation.FromMilliseconds(0);

        // Assert
        Assert.NotNull(operation);
        Assert.Equal("Delay 0ms", operation.Name);
    }

    [Fact]
    public void FromSeconds_WithZero_CreatesOperation()
    {
        // Act
        var operation = DelayOperation.FromSeconds(0);

        // Assert
        Assert.NotNull(operation);
        Assert.Equal("Delay 0ms", operation.Name);
    }

    [Fact]
    public void FromMinutes_WithZero_CreatesOperation()
    {
        // Act
        var operation = DelayOperation.FromMinutes(0);

        // Assert
        Assert.NotNull(operation);
        Assert.Equal("Delay 0ms", operation.Name);
    }

    #endregion

    #region ForgeAsync Tests

    [Fact]
    public async Task ForgeAsync_WithNullFoundry_ThrowsArgumentNullException()
    {
        // Arrange
        var operation = new DelayOperation(TimeSpan.FromMilliseconds(10));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            operation.ForgeAsync("input", null!, CancellationToken.None));
    }

    [Fact]
    public async Task ForgeAsync_WithValidFoundry_ReturnsInputData()
    {
        // Arrange
        var operation = new DelayOperation(TimeSpan.FromMilliseconds(10));
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
        var operation = new DelayOperation(TimeSpan.FromMilliseconds(10));
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
        var operation = new DelayOperation(TimeSpan.FromMilliseconds(10));
        var foundry = CreateMockFoundry();
        var inputData = new { Name = "Test", Value = 42 };

        // Act
        var result = await operation.ForgeAsync(inputData, foundry.Object);

        // Assert
        Assert.Same(inputData, result);
    }

    [Fact]
    public async Task ForgeAsync_ActuallyDelays()
    {
        // Arrange
        var delayMs = 50;
        var operation = new DelayOperation(TimeSpan.FromMilliseconds(delayMs));
        var foundry = CreateMockFoundry();
        var stopwatch = Stopwatch.StartNew();

        // Act
        await operation.ForgeAsync("input", foundry.Object);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds >= delayMs - 10); // Allow 10ms tolerance
    }

    [Fact]
    public async Task ForgeAsync_WithZeroDelay_CompletesImmediately()
    {
        // Arrange
        var operation = new DelayOperation(TimeSpan.Zero);
        var foundry = CreateMockFoundry();
        var stopwatch = Stopwatch.StartNew();

        // Act
        await operation.ForgeAsync("input", foundry.Object);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 50); // Should complete very quickly
    }

    [Fact]
    public async Task ForgeAsync_LogsStartAndCompletion()
    {
        // Arrange
        var operation = new DelayOperation(TimeSpan.FromMilliseconds(10));
        var foundry = CreateMockFoundry();

        // Act
        await operation.ForgeAsync("input", foundry.Object);

        // Assert
        foundry.Verify(f => f.Logger.LogDebug(It.IsAny<Dictionary<string, string>>(), "Starting delay operation"), Times.Once);
        foundry.Verify(f => f.Logger.LogDebug(It.IsAny<Dictionary<string, string>>(), "Completed delay operation"), Times.Once);
    }

    [Fact]
    public async Task ForgeAsync_LogsCorrectProperties()
    {
        // Arrange
        var operation = new DelayOperation(TimeSpan.FromMilliseconds(100), "TestDelay");
        var foundry = CreateMockFoundry();
        var executionId = Guid.NewGuid();
        var workflowName = "TestWorkflow";
        foundry.Setup(f => f.ExecutionId).Returns(executionId);
        
        var mockWorkflow = new Mock<IWorkflow>();
        mockWorkflow.Setup(w => w.Name).Returns(workflowName);
        foundry.Setup(f => f.CurrentWorkflow).Returns(mockWorkflow.Object);

        Dictionary<string, string>? loggedProperties = null;
        foundry.Setup(f => f.Logger.LogDebug(It.IsAny<IDictionary<string, string>>(), It.IsAny<string>(), It.IsAny<object[]>()))
              .Callback<IDictionary<string, string>, string, object[]>((props, _, __) => loggedProperties = new Dictionary<string, string>(props));

        // Act
        await operation.ForgeAsync("test-input", foundry.Object);

        // Assert
        Assert.NotNull(loggedProperties);
        Assert.Equal(operation.Id.ToString(), loggedProperties["OperationId"]);
        Assert.Equal("TestDelay", loggedProperties["OperationName"]);
        Assert.Equal(executionId.ToString(), loggedProperties["WorkflowId"]);
        Assert.Equal(workflowName, loggedProperties["WorkflowName"]);
        Assert.Equal("100", loggedProperties["DelayMs"]);
        Assert.Equal("String", loggedProperties["InputType"]);
    }

    [Fact]
    public async Task ForgeAsync_WithNullInput_LogsNullInputType()
    {
        // Arrange
        var operation = new DelayOperation(TimeSpan.FromMilliseconds(10));
        var foundry = CreateMockFoundry();

        Dictionary<string, string>? loggedProperties = null;
        foundry.Setup(f => f.Logger.LogDebug(It.IsAny<IDictionary<string, string>>(), It.IsAny<string>(), It.IsAny<object[]>()))
              .Callback<IDictionary<string, string>, string, object[]>((props, _, __) => loggedProperties = new Dictionary<string, string>(props));

        // Act
        await operation.ForgeAsync(null, foundry.Object);

        // Assert
        Assert.NotNull(loggedProperties);
        Assert.Equal("null", loggedProperties["InputType"]);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ForgeAsync_WithCancellation_ThrowsTaskCanceledException()
    {
        // Arrange
        var operation = new DelayOperation(TimeSpan.FromSeconds(10));
        var foundry = CreateMockFoundry();
        using var cts = new CancellationTokenSource();

        // Act
        var task = operation.ForgeAsync("input", foundry.Object, cts.Token);
        cts.Cancel();

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => task);
    }

    [Fact]
    public async Task ForgeAsync_WithPreCancelledToken_ThrowsTaskCanceledException()
    {
        // Arrange
        var operation = new DelayOperation(TimeSpan.FromMilliseconds(100));
        var foundry = CreateMockFoundry();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => 
            operation.ForgeAsync("input", foundry.Object, cts.Token));
    }

    [Fact]
    public async Task ForgeAsync_CancellationDuringDelay_StopsExecution()
    {
        // Arrange
        var operation = new DelayOperation(TimeSpan.FromSeconds(2));
        var foundry = CreateMockFoundry();
        using var cts = new CancellationTokenSource();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var task = operation.ForgeAsync("input", foundry.Object, cts.Token);
        await Task.Delay(100); // Let it start
        cts.Cancel();

        try
        {
            await task;
        }
        catch (TaskCanceledException)
        {
            // Expected
        }

        stopwatch.Stop();

        // Assert - Should complete much faster than 2 seconds
        Assert.True(stopwatch.ElapsedMilliseconds < 1000);
    }

    #endregion

    #region RestoreAsync Tests

    [Fact]
    public async Task RestoreAsync_ThrowsNotSupportedException()
    {
        // Arrange
        var operation = new DelayOperation(TimeSpan.FromMilliseconds(10));
        var foundry = CreateMockFoundry();

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() => 
            operation.RestoreAsync("output", foundry.Object));
    }

    [Fact]
    public async Task RestoreAsync_WithNullFoundry_ThrowsNotSupportedException()
    {
        // Arrange
        var operation = new DelayOperation(TimeSpan.FromMilliseconds(10));

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
        var operation = new DelayOperation(TimeSpan.FromMilliseconds(10));

        // Act & Assert - Should not throw
        operation.Dispose();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var operation = new DelayOperation(TimeSpan.FromMilliseconds(10));

        // Act & Assert - Should not throw
        operation.Dispose();
        operation.Dispose();
        operation.Dispose();
    }

    [Fact]
    public async Task ForgeAsync_AfterDispose_StillWorks()
    {
        // Arrange
        var operation = new DelayOperation(TimeSpan.FromMilliseconds(10));
        var foundry = CreateMockFoundry();
        operation.Dispose();

        // Act
        var result = await operation.ForgeAsync("input", foundry.Object);

        // Assert
        Assert.Equal("input", result);
    }

    #endregion

    #region Edge Cases and Performance Tests

    [Fact]
    public async Task ForgeAsync_WithVeryLargeDelay_CanBeCancelled()
    {
        // Arrange
        var operation = new DelayOperation(TimeSpan.FromDays(1));
        var foundry = CreateMockFoundry();
        using var cts = new CancellationTokenSource();

        // Act
        var task = operation.ForgeAsync("input", foundry.Object, cts.Token);
        cts.CancelAfter(50);

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => task);
    }

    [Fact]
    public async Task ForgeAsync_MultipleInstancesConcurrently()
    {
        // Arrange
        var operations = new[]
        {
            new DelayOperation(TimeSpan.FromMilliseconds(50)),
            new DelayOperation(TimeSpan.FromMilliseconds(75)),
            new DelayOperation(TimeSpan.FromMilliseconds(100))
        };
        var foundry = CreateMockFoundry();

        // Act
        var tasks = operations.Select(op => op.ForgeAsync($"input-{op.Name}", foundry.Object));
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(3, results.Length);
        Assert.All(results, Assert.NotNull);
    }

    [Fact]
    public void Id_GeneratesUniqueValues()
    {
        // Arrange & Act
        var operation1 = new DelayOperation(TimeSpan.FromMilliseconds(10));
        var operation2 = new DelayOperation(TimeSpan.FromMilliseconds(10));

        // Assert
        Assert.NotEqual(operation1.Id, operation2.Id);
        Assert.NotEqual(Guid.Empty, operation1.Id);
        Assert.NotEqual(Guid.Empty, operation2.Id);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(500)]
    public async Task ForgeAsync_VariousDelays_CompletesWithinReasonableTime(int delayMs)
    {
        // Arrange
        var operation = new DelayOperation(TimeSpan.FromMilliseconds(delayMs));
        var foundry = CreateMockFoundry();
        var stopwatch = Stopwatch.StartNew();

        // Act
        await operation.ForgeAsync("input", foundry.Object);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds >= delayMs - 15); // Allow some tolerance
        Assert.True(stopwatch.ElapsedMilliseconds <= delayMs + 100); // Reasonable upper bound
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