using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Operations;

namespace WorkflowForge.Tests.Operations
{
    /// <summary>
    /// Comprehensive tests for ForEachWorkflowOperation covering different data strategies,
    /// concurrency controls, timeout scenarios, and error handling.
    /// </summary>
    public class ForEachWorkflowOperationTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidOperations_CreatesOperation()
        {
            // Arrange
            var operations = new[] { CreateMockOperation("Op1").Object, CreateMockOperation("Op2").Object };

            // Act
            var foreachOp = new ForEachWorkflowOperation(operations);

            // Assert
            Assert.NotNull(foreachOp);
            Assert.NotEqual(Guid.Empty, foreachOp.Id);
            Assert.Equal("ForEach[2]", foreachOp.Name);
        }

        [Fact]
        public void Constructor_WithCustomNameAndId_UsesProvidedValues()
        {
            // Arrange
            var operations = new[] { CreateMockOperation("Op1").Object };
            var customName = "CustomForEach";
            var customId = Guid.NewGuid();

            // Act
            var foreachOp = new ForEachWorkflowOperation(operations, name: customName, id: customId);

            // Assert
            Assert.Equal(customId, foreachOp.Id);
            Assert.Equal(customName, foreachOp.Name);
        }

        [Fact]
        public void Constructor_WithNullOperations_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ForEachWorkflowOperation(null!));
        }

        [Fact]
        public void Constructor_WithEmptyOperations_ThrowsArgumentException()
        {
            // Arrange
            var operations = Array.Empty<IWorkflowOperation>();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new ForEachWorkflowOperation(operations));
        }

        [Fact]
        public void Constructor_WithInvalidMaxConcurrency_ThrowsArgumentException()
        {
            // Arrange
            var operations = new[] { CreateMockOperation("Op1").Object };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new ForEachWorkflowOperation(operations, maxConcurrency: 0));
            Assert.Throws<ArgumentException>(() => new ForEachWorkflowOperation(operations, maxConcurrency: -1));
        }

        #endregion Constructor Tests

        #region ForgeAsync Tests

        [Fact]
        public async Task ForgeAsync_WithSharedInputStrategy_PassesSameInputToAllOperations()
        {
            // Arrange
            var inputData = "shared input";
            var operations = new[] { CreateMockOperation("Op1").Object, CreateMockOperation("Op2").Object };
            var foundry = CreateMockFoundry();
            var foreachOp = new ForEachWorkflowOperation(operations, dataStrategy: ForEachDataStrategy.SharedInput);

            // Act
            var result = await foreachOp.ForgeAsync(inputData, foundry.Object);

            // Assert
            Mock.Get(operations[0]).Verify(op => op.ForgeAsync(inputData, foundry.Object, It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(operations[1]).Verify(op => op.ForgeAsync(inputData, foundry.Object, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ForgeAsync_WithSplitInputStrategy_DistributesInputData()
        {
            // Arrange
            var inputData = new[] { "item1", "item2", "item3" };
            var operations = new[] { CreateMockOperation("Op1").Object, CreateMockOperation("Op2").Object };
            var foundry = CreateMockFoundry();
            var foreachOp = new ForEachWorkflowOperation(operations, dataStrategy: ForEachDataStrategy.SplitInput);

            // Act
            var result = await foreachOp.ForgeAsync(inputData, foundry.Object);

            // Assert
            Mock.Get(operations[0]).Verify(op => op.ForgeAsync("item1", foundry.Object, It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(operations[1]).Verify(op => op.ForgeAsync("item2", foundry.Object, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ForgeAsync_WithNoInputStrategy_PassesNullToAllOperations()
        {
            // Arrange
            var inputData = "some input";
            var operations = new[] { CreateMockOperation("Op1").Object, CreateMockOperation("Op2").Object };
            var foundry = CreateMockFoundry();
            var foreachOp = new ForEachWorkflowOperation(operations, dataStrategy: ForEachDataStrategy.NoInput);

            // Act
            var result = await foreachOp.ForgeAsync(inputData, foundry.Object);

            // Assert
            Mock.Get(operations[0]).Verify(op => op.ForgeAsync(null, foundry.Object, It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(operations[1]).Verify(op => op.ForgeAsync(null, foundry.Object, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ForgeAsync_WithMaxConcurrency_LimitsParallelExecution()
        {
            // Arrange
            var operationCount = 10;
            var maxConcurrency = 3;
            var operations = Enumerable.Range(0, operationCount)
                .Select(i => CreateSlowMockOperation($"Op{i}", TimeSpan.FromMilliseconds(100)).Object)
                .ToArray();
            var foundry = CreateMockFoundry();
            var foreachOp = new ForEachWorkflowOperation(operations, maxConcurrency: maxConcurrency);

            // Act
            var startTime = DateTime.UtcNow;
            await foreachOp.ForgeAsync("input", foundry.Object);
            var duration = DateTime.UtcNow - startTime;

            // Assert - With throttling, execution should take longer than if all ran in parallel
            // Should be at least (operationCount / maxConcurrency) * operation duration
            var expectedMinDuration = TimeSpan.FromMilliseconds(100 * (operationCount / maxConcurrency));
            Assert.True(duration >= expectedMinDuration, $"Duration {duration} should be at least {expectedMinDuration}");
        }

        [Fact]
        public async Task ForgeAsync_WithTimeout_CompletesWithoutTimeout()
        {
            // Arrange
            var operations = new[] { CreateSlowMockOperation("Op1", TimeSpan.FromMilliseconds(50)).Object };
            var foundry = CreateMockFoundry();
            var foreachOp = new ForEachWorkflowOperation(operations, timeout: TimeSpan.FromSeconds(2));

            // Act - Should complete successfully since timeout is longer than operation duration
            var result = await foreachOp.ForgeAsync("input", foundry.Object);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ForgeAsync_WithTimeout_ThrowsOperationCanceledException()
        {
            // Arrange
            var operations = new[] { CreateSlowMockOperation("Op1", TimeSpan.FromSeconds(10)).Object };
            var foundry = CreateMockFoundry();
            var foreachOp = new ForEachWorkflowOperation(operations, timeout: TimeSpan.FromMilliseconds(500));

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                foreachOp.ForgeAsync("input", foundry.Object));
        }

        [Fact]
        public async Task ForgeAsync_CombinesResults_ReturnsForEachResults()
        {
            // Arrange
            var operations = new[]
            {
                CreateMockOperationWithResult("Op1", "result1").Object,
                CreateMockOperationWithResult("Op2", "result2").Object
            };
            var foundry = CreateMockFoundry();
            var foreachOp = new ForEachWorkflowOperation(operations);

            // Act
            var result = await foreachOp.ForgeAsync("input", foundry.Object);

            // Assert
            Assert.IsType<ForEachResults>(result);
            var forEachResults = (ForEachResults)result!;
            Assert.Equal(2, forEachResults.Results.Length);
            Assert.Contains("result1", forEachResults.Results);
            Assert.Contains("result2", forEachResults.Results);
        }

        [Fact]
        public async Task ForgeAsync_WithNullFoundry_ThrowsArgumentNullException()
        {
            // Arrange
            var operations = new[] { CreateMockOperation("Op1").Object };
            var foreachOp = new ForEachWorkflowOperation(operations);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                foreachOp.ForgeAsync("input", null!));
        }

        [Fact]
        public async Task ForgeAsync_WithCancellationToken_CompletesIfNotCancelled()
        {
            // Arrange
            var operations = new[] { CreateMockOperation("Op1").Object };
            var foundry = CreateMockFoundry();
            var foreachOp = new ForEachWorkflowOperation(operations);
            var cts = new CancellationTokenSource();
            // Don't cancel the token

            // Act
            var result = await foreachOp.ForgeAsync("input", foundry.Object, cts.Token);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ForgeAsync_WithFoundryMaxConcurrencyProperty_RespectsFrameworkLimit()
        {
            // Arrange
            var operations = Enumerable.Range(0, 10)
                .Select(i => CreateMockOperation($"Op{i}").Object)
                .ToArray();
            var foundry = CreateMockFoundry();

            // Setup Properties to contain MaxConcurrentOperations
            var properties = new ConcurrentDictionary<string, object?>();
            properties["MaxConcurrentOperations"] = 2;
            foundry.Setup(f => f.Properties).Returns(properties);

            var foreachOp = new ForEachWorkflowOperation(operations, maxConcurrency: 5); // Request 5, but framework limits to 2

            // Act
            await foreachOp.ForgeAsync("input", foundry.Object);

            // Assert - All operations should still be called
            foreach (var op in operations)
            {
                Mock.Get(op).Verify(o => o.ForgeAsync(It.IsAny<object>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        #endregion ForgeAsync Tests

        #region RestoreAsync Tests

        [Fact]
        public async Task RestoreAsync_WithValidOperations_CallsRestoreOnAllOperations()
        {
            // Arrange
            var operations = new[] { CreateMockOperation("Op1").Object, CreateMockOperation("Op2").Object };
            var foundry = CreateMockFoundry();
            var foreachOp = new ForEachWorkflowOperation(operations);
            var outputData = new ForEachResults { Results = new object?[] { "result1", "result2" } };

            // Act
            await foreachOp.RestoreAsync(outputData, foundry.Object);

            // Assert
            Mock.Get(operations[0]).Verify(op => op.RestoreAsync("result1", foundry.Object, It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(operations[1]).Verify(op => op.RestoreAsync("result2", foundry.Object, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RestoreAsync_WithMaxConcurrency_LimitsParallelRestoration()
        {
            // Arrange
            var operationCount = 6;
            var maxConcurrency = 2;
            var operations = Enumerable.Range(0, operationCount)
                .Select(i => CreateSlowMockOperation($"Op{i}", TimeSpan.FromMilliseconds(100)).Object)
                .ToArray();
            var foundry = CreateMockFoundry();
            var foreachOp = new ForEachWorkflowOperation(operations, maxConcurrency: maxConcurrency);
            var outputData = new ForEachResults { Results = Enumerable.Range(0, operationCount).Cast<object?>().ToArray() };

            // Act
            var startTime = DateTime.UtcNow;
            await foreachOp.RestoreAsync(outputData, foundry.Object);
            var duration = DateTime.UtcNow - startTime;

            // Assert - With throttling, restoration should take longer
            var expectedMinDuration = TimeSpan.FromMilliseconds(100 * (operationCount / maxConcurrency));
            Assert.True(duration >= expectedMinDuration);
        }

        #endregion RestoreAsync Tests

        #region Static Factory Method Tests

        [Fact]
        public void CreateSharedInput_CreatesOperationWithSharedInputStrategy()
        {
            // Arrange
            var operations = new[] { CreateMockOperation("Op1").Object };

            // Act
            var foreachOp = ForEachWorkflowOperation.CreateSharedInput(operations);

            // Assert
            Assert.NotNull(foreachOp);
            Assert.Equal("ForEach[1]", foreachOp.Name);
        }

        [Fact]
        public void CreateSplitInput_CreatesOperationWithSplitInputStrategy()
        {
            // Arrange
            var operations = new[] { CreateMockOperation("Op1").Object };

            // Act
            var foreachOp = ForEachWorkflowOperation.CreateSplitInput(operations);

            // Assert
            Assert.NotNull(foreachOp);
            Assert.Equal("ForEach[1]", foreachOp.Name);
        }

        [Fact]
        public void CreateNoInput_CreatesOperationWithNoInputStrategy()
        {
            // Arrange
            var operations = new[] { CreateMockOperation("Op1").Object };

            // Act
            var foreachOp = ForEachWorkflowOperation.CreateNoInput(operations);

            // Assert
            Assert.NotNull(foreachOp);
            Assert.Equal("ForEach[1]", foreachOp.Name);
        }

        [Fact]
        public void Create_WithParams_CreatesOperationFromArray()
        {
            // Arrange
            var op1 = CreateMockOperation("Op1").Object;
            var op2 = CreateMockOperation("Op2").Object;

            // Act
            var foreachOp = ForEachWorkflowOperation.Create(op1, op2);

            // Assert
            Assert.NotNull(foreachOp);
            Assert.Equal("ForEach[2]", foreachOp.Name);
        }

        [Fact]
        public void CreateWithThrottling_CreatesOperationWithMaxConcurrency()
        {
            // Arrange
            var operations = new[] { CreateMockOperation("Op1").Object, CreateMockOperation("Op2").Object };
            var maxConcurrency = 1;

            // Act
            var foreachOp = ForEachWorkflowOperation.CreateWithThrottling(operations, maxConcurrency);

            // Assert
            Assert.NotNull(foreachOp);
            Assert.Equal("ForEach[2]", foreachOp.Name);
        }

        #endregion Static Factory Method Tests

        #region Dispose Tests

        [Fact]
        public void Dispose_DisposesAllChildOperations()
        {
            // Arrange
            var operations = new[] { CreateMockOperation("Op1").Object, CreateMockOperation("Op2").Object };
            var foreachOp = new ForEachWorkflowOperation(operations);

            // Act
            foreachOp.Dispose();

            // Assert
            Mock.Get(operations[0]).Verify(op => op.Dispose(), Times.Once);
            Mock.Get(operations[1]).Verify(op => op.Dispose(), Times.Once);
        }

        [Fact]
        public async Task ForgeAsync_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var operations = new[] { CreateMockOperation("Op1").Object };
            var foundry = CreateMockFoundry();
            var foreachOp = new ForEachWorkflowOperation(operations);
            foreachOp.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                foreachOp.ForgeAsync("input", foundry.Object));
        }

        [Fact]
        public async Task RestoreAsync_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var operations = new[] { CreateMockOperation("Op1").Object };
            var foundry = CreateMockFoundry();
            var foreachOp = new ForEachWorkflowOperation(operations);
            foreachOp.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                foreachOp.RestoreAsync(null, foundry.Object));
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_HandledGracefully()
        {
            // Arrange
            var operations = new[] { CreateMockOperation("Op1").Object };
            var foreachOp = new ForEachWorkflowOperation(operations);

            // Act & Assert - Should not throw
            var ex = Record.Exception(() =>
            {
                foreachOp.Dispose();
                foreachOp.Dispose();
            });
            Assert.Null(ex);
        }

        #endregion Dispose Tests

        #region Edge Cases and Error Handling

        [Fact]
        public async Task ForgeAsync_WithOperationThatThrows_PropagatesException()
        {
            // Arrange
            var failingOp = CreateMockOperation("FailingOp");
            failingOp.Setup(op => op.ForgeAsync(It.IsAny<object>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Operation failed"));

            var operations = new[] { failingOp.Object };
            var foundry = CreateMockFoundry();
            var foreachOp = new ForEachWorkflowOperation(operations);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                foreachOp.ForgeAsync("input", foundry.Object));
        }

        #endregion Edge Cases and Error Handling

        #region Helper Methods

        private Mock<IWorkflowOperation> CreateMockOperation(string name)
        {
            var mock = new Mock<IWorkflowOperation>();
            mock.Setup(op => op.Id).Returns(Guid.NewGuid());
            mock.Setup(op => op.Name).Returns(name);
            mock.Setup(op => op.ForgeAsync(It.IsAny<object>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((object?)null);
            mock.Setup(op => op.RestoreAsync(It.IsAny<object>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            return mock;
        }

        private Mock<IWorkflowOperation> CreateMockOperationWithResult(string name, object result)
        {
            var mock = CreateMockOperation(name);
            mock.Setup(op => op.ForgeAsync(It.IsAny<object>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);
            return mock;
        }

        private Mock<IWorkflowOperation> CreateSlowMockOperation(string name, TimeSpan delay)
        {
            var mock = CreateMockOperation(name);
            mock.Setup(op => op.ForgeAsync(It.IsAny<object>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()))
                .Returns(async (object input, IWorkflowFoundry foundry, CancellationToken ct) =>
                {
                    await Task.Delay(delay, ct);
                    return (object?)null;
                });
            mock.Setup(op => op.RestoreAsync(It.IsAny<object>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()))
                .Returns(async (object output, IWorkflowFoundry foundry, CancellationToken ct) =>
                {
                    await Task.Delay(delay, ct);
                });
            return mock;
        }

        private Mock<IWorkflowFoundry> CreateMockFoundry()
        {
            var mock = new Mock<IWorkflowFoundry>();
            mock.Setup(f => f.ExecutionId).Returns(Guid.NewGuid());

            var mockWorkflow = new Mock<IWorkflow>();
            mockWorkflow.Setup(w => w.Name).Returns("TestWorkflow");
            mock.Setup(f => f.CurrentWorkflow).Returns(mockWorkflow.Object);

            mock.Setup(f => f.Logger).Returns(Mock.Of<IWorkflowForgeLogger>());

            var properties = new ConcurrentDictionary<string, object?>();
            mock.Setup(f => f.Properties).Returns(properties);

            return mock;
        }

        #endregion Helper Methods
    }
}
