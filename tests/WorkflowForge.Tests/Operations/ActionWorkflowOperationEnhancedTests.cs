using WorkflowForge.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Operations;

namespace WorkflowForge.Tests.Operations
{
    /// <summary>
    /// Enhanced comprehensive tests for ActionWorkflowOperation covering edge cases,
    /// error scenarios, and advanced functionality.
    /// </summary>
    public class ActionWorkflowOperationEnhancedTests
    {
        #region Constructor Edge Cases

        [Fact]
        public void Constructor_WithVeryLongName_HandlesLongNames()
        {
            // Arrange
            var longName = new string('A', 10000);
            Func<object?, IWorkflowFoundry, CancellationToken, Task> action = (_, _, _) => Task.CompletedTask;

            // Act
            var operation = new ActionWorkflowOperation(longName, action);

            // Assert
            Assert.Equal(longName, operation.Name);
        }

        [Fact]
        public void Constructor_WithSpecialCharactersInName_HandlesSpecialCharacters()
        {
            // Arrange
            var specialName = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~";
            Func<object?, IWorkflowFoundry, CancellationToken, Task> action = (_, _, _) => Task.CompletedTask;

            // Act
            var operation = new ActionWorkflowOperation(specialName, action);

            // Assert
            Assert.Equal(specialName, operation.Name);
        }

        [Fact]
        public void Constructor_WithEmptyName_AllowsEmptyName()
        {
            // Arrange
            Func<object?, IWorkflowFoundry, CancellationToken, Task> action = (_, _, _) => Task.CompletedTask;

            // Act
            var operation = new ActionWorkflowOperation("", action);

            // Assert
            Assert.Equal("", operation.Name);
        }

        [Fact]
        public void Constructor_WithWhitespaceOnlyName_AllowsWhitespace()
        {
            // Arrange
            var whitespaceName = "   \t\n\r   ";
            Func<object?, IWorkflowFoundry, CancellationToken, Task> action = (_, _, _) => Task.CompletedTask;

            // Act
            var operation = new ActionWorkflowOperation(whitespaceName, action);

            // Assert
            Assert.Equal(whitespaceName, operation.Name);
        }

        #endregion Constructor Edge Cases

        #region ForgeAsync Advanced Tests

        [Fact]
        public async Task ForgeAsync_WithComplexInputData_PassesDataCorrectly()
        {
            // Arrange
            var complexInput = new
            {
                Id = Guid.NewGuid(),
                Name = "Test",
                Values = new[] { 1, 2, 3 },
                Metadata = new Dictionary<string, object> { { "key", "value" } }
            };

            object? capturedInput = null;
            Func<object?, IWorkflowFoundry, CancellationToken, Task> action = (input, foundry, ct) =>
            {
                capturedInput = input;
                return Task.CompletedTask;
            };

            var operation = new ActionWorkflowOperation("ComplexTest", action);
            var foundry = CreateMockFoundry();

            // Act
            await operation.ForgeAsync(complexInput, foundry.Object);

            // Assert
            Assert.Same(complexInput, capturedInput);
        }

        [Fact]
        public async Task ForgeAsync_WithNullInputData_HandlesNullCorrectly()
        {
            // Arrange
            object? capturedInput = "not null";
            Func<object?, IWorkflowFoundry, CancellationToken, Task> action = (input, foundry, ct) =>
            {
                capturedInput = input;
                return Task.CompletedTask;
            };

            var operation = new ActionWorkflowOperation("NullTest", action);
            var foundry = CreateMockFoundry();

            // Act
            await operation.ForgeAsync(null, foundry.Object);

            // Assert
            Assert.Null(capturedInput);
        }

        [Fact]
        public async Task ForgeAsync_WithCancellationRequested_PropagatesCancellation()
        {
            // Arrange
            var cancellationRequested = false;
            Func<object?, IWorkflowFoundry, CancellationToken, Task> action = (input, foundry, ct) =>
            {
                cancellationRequested = ct.IsCancellationRequested;
                return Task.CompletedTask;
            };

            var operation = new ActionWorkflowOperation("CancellationTest", action);
            var foundry = CreateMockFoundry();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            await operation.ForgeAsync("input", foundry.Object, cts.Token);

            // Assert
            Assert.True(cancellationRequested);
        }

        [Fact]
        public async Task ForgeAsync_WithActionThatThrows_PropagatesException()
        {
            // Arrange
            Func<object?, IWorkflowFoundry, CancellationToken, Task> action = (_, _, _) =>
                throw new InvalidOperationException("Test exception");

            var operation = new ActionWorkflowOperation("ThrowingAction", action);
            var foundry = CreateMockFoundry();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<WorkflowOperationException>(() => operation.ForgeAsync("input", foundry.Object));
            Assert.IsType<InvalidOperationException>(exception.InnerException);
            Assert.Contains("Test exception", exception.InnerException.Message);
        }

        [Fact]
        public async Task ForgeAsync_WithAsyncActionThatThrows_PropagatesException()
        {
            // Arrange
            var expectedException = new TaskCanceledException("Async test exception");
            Func<object?, IWorkflowFoundry, CancellationToken, Task> action = async (_, _, ct) =>
            {
                await Task.Delay(1, ct);
                throw expectedException;
            };

            var operation = new ActionWorkflowOperation("AsyncThrowingAction", action);
            var foundry = CreateMockFoundry();

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<TaskCanceledException>(() =>
                operation.ForgeAsync("input", foundry.Object));

            Assert.Same(expectedException, actualException);
        }

        [Fact]
        public async Task ForgeAsync_WithLongRunningAction_CompletesSuccessfully()
        {
            // Arrange
            var delay = TimeSpan.FromMilliseconds(100);
            var startTime = DateTime.UtcNow;
            DateTime endTime = DateTime.MinValue;

            Func<object?, IWorkflowFoundry, CancellationToken, Task> action = async (_, _, ct) =>
            {
                await Task.Delay(delay, ct);
                endTime = DateTime.UtcNow;
            };

            var operation = new ActionWorkflowOperation("LongRunningAction", action);
            var foundry = CreateMockFoundry();

            // Act
            await operation.ForgeAsync("input", foundry.Object);

            // Assert
            var actualDuration = endTime - startTime;
            Assert.True(actualDuration >= delay, $"Expected at least {delay}, but was {actualDuration}");
        }

        [Fact]
        public async Task ForgeAsync_WithFoundryInteraction_CanAccessFoundryProperties()
        {
            // Arrange
            Guid capturedFlowId = Guid.Empty;
            string? capturedFlowName = null;

            Func<object?, IWorkflowFoundry, CancellationToken, Task> action = (input, foundry, ct) =>
            {
                capturedFlowId = foundry.ExecutionId;
                capturedFlowName = foundry.CurrentWorkflow?.Name;
                return Task.CompletedTask;
            };

            var operation = new ActionWorkflowOperation("FoundryInteractionTest", action);
            var foundry = CreateMockFoundry();
            var expectedFlowId = Guid.NewGuid();
            var expectedFlowName = "TestWorkflow";
            foundry.Setup(f => f.ExecutionId).Returns(expectedFlowId);

            var mockWorkflow = new Mock<IWorkflow>();
            mockWorkflow.Setup(w => w.Id).Returns(expectedFlowId);
            mockWorkflow.Setup(w => w.Name).Returns(expectedFlowName);
            foundry.Setup(f => f.CurrentWorkflow).Returns(mockWorkflow.Object);

            // Act
            await operation.ForgeAsync("input", foundry.Object);

            // Assert
            Assert.Equal(expectedFlowId, capturedFlowId);
            Assert.Equal(expectedFlowName, capturedFlowName);
        }

        #endregion ForgeAsync Advanced Tests

        #region RestoreAsync Tests

        [Fact]
        public async Task RestoreAsync_Always_ThrowsNotSupportedException()
        {
            // Arrange
            Func<object?, IWorkflowFoundry, CancellationToken, Task> action = (_, _, _) => Task.CompletedTask;
            var operation = new ActionWorkflowOperation("RestoreTest", action);
            var foundry = CreateMockFoundry();

            // Act & Assert
            await Assert.ThrowsAsync<NotSupportedException>(() =>
                operation.RestoreAsync("output", foundry.Object));
        }

        [Fact]
        public async Task RestoreAsync_WithNullOutputData_ThrowsNotSupportedException()
        {
            // Arrange
            Func<object?, IWorkflowFoundry, CancellationToken, Task> action = (_, _, _) => Task.CompletedTask;
            var operation = new ActionWorkflowOperation("RestoreNullTest", action);
            var foundry = CreateMockFoundry();

            // Act & Assert
            await Assert.ThrowsAsync<NotSupportedException>(() =>
                operation.RestoreAsync(null, foundry.Object));
        }

        #endregion RestoreAsync Tests

        #region Properties Tests

        [Fact]
        public void SupportsRestore_Always_ReturnsFalse()
        {
            // Arrange
            Func<object?, IWorkflowFoundry, CancellationToken, Task> action = (_, _, _) => Task.CompletedTask;
            var operation = new ActionWorkflowOperation("RestorePropertyTest", action);

            // Act & Assert
            Assert.False(operation.SupportsRestore);
        }

        [Fact]
        public void Id_IsUniqueForEachInstance()
        {
            // Arrange
            Func<object?, IWorkflowFoundry, CancellationToken, Task> action = (_, _, _) => Task.CompletedTask;

            // Act
            var operation1 = new ActionWorkflowOperation("Test1", action);
            var operation2 = new ActionWorkflowOperation("Test2", action);

            // Assert
            Assert.NotEqual(operation1.Id, operation2.Id);
            Assert.NotEqual(Guid.Empty, operation1.Id);
            Assert.NotEqual(Guid.Empty, operation2.Id);
        }

        #endregion Properties Tests

        #region Dispose Tests

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            Func<object?, IWorkflowFoundry, CancellationToken, Task> action = (_, _, _) => Task.CompletedTask;
            var operation = new ActionWorkflowOperation("DisposeTest", action);

            // Act & Assert - Should not throw
            operation.Dispose();
            operation.Dispose();
            operation.Dispose();
        }

        [Fact]
        public async Task ForgeAsync_AfterDispose_StillWorksAsExpected()
        {
            // Arrange
            Func<object?, IWorkflowFoundry, CancellationToken, Task> action = (_, _, _) => Task.CompletedTask;
            var operation = new ActionWorkflowOperation("DisposedTest", action);
            var foundry = CreateMockFoundry();
            operation.Dispose();

            // Act - ActionWorkflowOperation doesn't track disposal state, so it still works
            var result = await operation.ForgeAsync("input", foundry.Object);

            // Assert
            Assert.Equal("input", result);
        }

        [Fact]
        public async Task RestoreAsync_AfterDispose_ThrowsNotSupportedException()
        {
            // Arrange
            Func<object?, IWorkflowFoundry, CancellationToken, Task> action = (_, _, _) => Task.CompletedTask;
            var operation = new ActionWorkflowOperation("DisposedRestoreTest", action);
            var foundry = CreateMockFoundry();
            operation.Dispose();

            // Act & Assert - ActionWorkflowOperation always throws NotSupportedException for restore
            await Assert.ThrowsAsync<NotSupportedException>(() =>
                operation.RestoreAsync("output", foundry.Object));
        }

        #endregion Dispose Tests

        #region Concurrent Execution Tests

        [Fact]
        public async Task ForgeAsync_ConcurrentExecution_HandledCorrectly()
        {
            // Arrange
            var concurrentExecutions = 10;
            var executionCounter = 0;
            var completedExecutions = 0;

            Func<object?, IWorkflowFoundry, CancellationToken, Task> action = async (input, foundry, ct) =>
            {
                Interlocked.Increment(ref executionCounter);
                await Task.Delay(50, ct); // Simulate some work
                Interlocked.Increment(ref completedExecutions);
            };

            var operation = new ActionWorkflowOperation("ConcurrentTest", action);
            var foundry = CreateMockFoundry();

            // Act
            var tasks = new Task[concurrentExecutions];
            for (int i = 0; i < concurrentExecutions; i++)
            {
                int index = i;
                tasks[i] = operation.ForgeAsync($"input{index}", foundry.Object);
            }

            await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(concurrentExecutions, executionCounter);
            Assert.Equal(concurrentExecutions, completedExecutions);
        }

        #endregion Concurrent Execution Tests

        #region Memory and Performance Tests

        [Fact]
        public async Task ForgeAsync_WithLargeInputData_HandlesLargeData()
        {
            // Arrange
            var largeData = new byte[1024 * 1024]; // 1MB of data
            new Random().NextBytes(largeData);

            byte[]? capturedData = null;
            Func<object?, IWorkflowFoundry, CancellationToken, Task> action = (input, foundry, ct) =>
            {
                capturedData = input as byte[];
                return Task.CompletedTask;
            };

            var operation = new ActionWorkflowOperation("LargeDataTest", action);
            var foundry = CreateMockFoundry();

            // Act
            await operation.ForgeAsync(largeData, foundry.Object);

            // Assert
            Assert.Same(largeData, capturedData);
            Assert.Equal(largeData.Length, capturedData!.Length);
        }

        #endregion Memory and Performance Tests

        #region Helper Methods

        private Mock<IWorkflowFoundry> CreateMockFoundry()
        {
            var mock = new Mock<IWorkflowFoundry>();
            mock.Setup(f => f.ExecutionId).Returns(Guid.NewGuid());

            var mockWorkflow = new Mock<IWorkflow>();
            mockWorkflow.Setup(w => w.Name).Returns("TestWorkflow");
            mock.Setup(f => f.CurrentWorkflow).Returns(mockWorkflow.Object);

            mock.Setup(f => f.Logger).Returns(Mock.Of<IWorkflowForgeLogger>());
            return mock;
        }

        #endregion Helper Methods
    }
}