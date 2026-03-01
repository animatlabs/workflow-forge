using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Operations;

namespace WorkflowForge.Tests.Operations;

/// <summary>
/// Comprehensive tests for DelayOperation covering functionality, timing, cancellation, and edge cases.
/// </summary>
public class DelayOperationShould
{
    #region Constructor Tests

    [Fact]
    public void SetProperties_GivenTimeSpan()
    {
        // Arrange
        var delay = TimeSpan.FromMilliseconds(100);

        // Act
        var operation = new DelayOperation(delay);

        // Assert
        Assert.NotEqual(Guid.Empty, operation.Id);
        Assert.Equal("Delay 100ms", operation.Name);
    }

    [Fact]
    public void SetCustomName_GivenTimeSpanAndName()
    {
        // Arrange
        var delay = TimeSpan.FromSeconds(1);
        const string customName = "Custom Delay";

        // Act
        var operation = new DelayOperation(delay, customName);

        // Assert
        Assert.NotEqual(Guid.Empty, operation.Id);
        Assert.Equal(customName, operation.Name);
    }

    [Fact]
    public void SetName_GivenZeroDelay()
    {
        // Arrange
        var delay = TimeSpan.Zero;

        // Act
        var operation = new DelayOperation(delay);

        // Assert
        Assert.Equal("Delay 0ms", operation.Name);
    }

    #endregion Constructor Tests

    #region Static Factory Methods Tests

    [Fact]
    public void CreateDelayOperation_GivenFromMilliseconds()
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
    public void CreateDelayOperation_GivenFromSeconds()
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
    public void CreateDelayOperation_GivenFromMinutes()
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
    public void CreateOperation_GivenFromMillisecondsWithZero()
    {
        // Act
        var operation = DelayOperation.FromMilliseconds(0);

        // Assert
        Assert.NotNull(operation);
        Assert.Equal("Delay 0ms", operation.Name);
    }

    [Fact]
    public void CreateOperation_GivenFromSecondsWithZero()
    {
        // Act
        var operation = DelayOperation.FromSeconds(0);

        // Assert
        Assert.NotNull(operation);
        Assert.Equal("Delay 0ms", operation.Name);
    }

    [Fact]
    public void CreateOperation_GivenFromMinutesWithZero()
    {
        // Act
        var operation = DelayOperation.FromMinutes(0);

        // Assert
        Assert.NotNull(operation);
        Assert.Equal("Delay 0ms", operation.Name);
    }

    #endregion Static Factory Methods Tests

    #region ForgeAsync Tests

    [Fact]
    public async Task ThrowArgumentNullException_GivenNullFoundry()
    {
        // Arrange
        var operation = new DelayOperation(TimeSpan.FromMilliseconds(10));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            operation.ForgeAsync("input", null!, CancellationToken.None));
    }

    [Fact]
    public async Task ReturnInputData_GivenValidFoundry()
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
    public async Task ReturnNull_GivenNullInputData()
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
    public async Task ReturnInputData_GivenComplexInputData()
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
    public async Task Delay_GivenForgeAsync()
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
        Assert.True(stopwatch.ElapsedMilliseconds >= delayMs - 20, $"Expected >= {delayMs - 20}ms, got {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task CompleteImmediately_GivenZeroDelay()
    {
        // Arrange
        var operation = new DelayOperation(TimeSpan.Zero);
        var foundry = CreateMockFoundry();
        var stopwatch = Stopwatch.StartNew();

        // Act
        await operation.ForgeAsync("input", foundry.Object);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 200, $"Expected < 200ms for zero delay, got {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task LogStartAndCompletion_GivenForgeAsync()
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
    public async Task LogCorrectProperties_GivenForgeAsync()
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
    public async Task LogNullInputType_GivenNullInput()
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

    #endregion ForgeAsync Tests

    #region Cancellation Tests

    [Fact]
    public async Task ThrowTaskCanceledException_GivenCancellation()
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
    public async Task ThrowTaskCanceledException_GivenPreCancelledToken()
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
    public async Task StopExecution_GivenCancellationDuringDelay()
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
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, $"Expected < 2000ms after cancellation, got {stopwatch.ElapsedMilliseconds}ms");
    }

    #endregion Cancellation Tests

    #region RestoreAsync Tests

    [Fact]
    public async Task CompleteSuccessfully_GivenRestoreAsync()
    {
        // Arrange
        var operation = new DelayOperation(TimeSpan.FromMilliseconds(10));
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
        var operation = new DelayOperation(TimeSpan.FromMilliseconds(10));

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
        var operation = new DelayOperation(TimeSpan.FromMilliseconds(10));

        // Act & Assert - Should not throw
        operation.Dispose();
    }

    [Fact]
    public void AllowMultipleCalls_GivenDispose()
    {
        // Arrange
        var operation = new DelayOperation(TimeSpan.FromMilliseconds(10));

        // Act & Assert - Should not throw
        operation.Dispose();
        operation.Dispose();
        operation.Dispose();
    }

    [Fact]
    public async Task StillWork_GivenForgeAsyncAfterDispose()
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

    #endregion Dispose Tests

    #region Edge Cases and Performance Tests

    [Fact]
    public async Task BeCancellable_GivenVeryLargeDelay()
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
    public async Task RunMultipleInstancesConcurrently_GivenForgeAsync()
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
    public void GenerateUniqueValues_GivenId()
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
    public async Task CompleteWithinReasonableTime_GivenVariousDelays(int delayMs)
    {
        // Arrange
        var operation = new DelayOperation(TimeSpan.FromMilliseconds(delayMs));
        var foundry = CreateMockFoundry();
        var stopwatch = Stopwatch.StartNew();

        // Act
        await operation.ForgeAsync("input", foundry.Object);
        stopwatch.Stop();

        // Assert
        var lowerBound = Math.Max(0, delayMs - 50);
        Assert.True(stopwatch.ElapsedMilliseconds >= lowerBound, $"Expected >= {lowerBound}ms, got {stopwatch.ElapsedMilliseconds}ms");
        var upperBound = delayMs + Math.Max(2000, delayMs * 3);
        Assert.True(stopwatch.ElapsedMilliseconds <= upperBound, $"Expected <= {upperBound}ms, got {stopwatch.ElapsedMilliseconds}ms");
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
