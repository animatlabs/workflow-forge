using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Operations;
using WorkflowForge.Testing;

namespace WorkflowForge.Tests.Testing
{
    /// <summary>
    /// Tests for FakeWorkflowFoundry test double.
    /// </summary>
    public class FakeWorkflowFoundryShould
    {
        #region Constructor Tests

        [Fact]
        public void InitializeWithDefaults_GivenConstructor()
        {
            // Act
            var foundry = new FakeWorkflowFoundry();

            // Assert
            Assert.NotEqual(Guid.Empty, foundry.ExecutionId);
            Assert.NotNull(foundry.Properties);
            Assert.NotNull(foundry.Logger);
            Assert.NotNull(foundry.Options);
            Assert.Null(foundry.ServiceProvider);
            Assert.Null(foundry.CurrentWorkflow);
            Assert.False(foundry.IsFrozen);
        }

        [Fact]
        public void AllowSetting_GivenExecutionId()
        {
            // Arrange
            var foundry = new FakeWorkflowFoundry();
            var newId = Guid.NewGuid();

            // Act
            foundry.ExecutionId = newId;

            // Assert
            Assert.Equal(newId, foundry.ExecutionId);
        }

        #endregion Constructor Tests

        #region Operation Tracking Tests

        [Fact]
        public void TrackOperation_GivenAddOperation()
        {
            // Arrange
            var foundry = new FakeWorkflowFoundry();
            var operation = new SimpleTestOperation("Test");

            // Act
            foundry.AddOperation(operation);

            // Assert
            Assert.Single(foundry.Operations);
            Assert.Same(operation, foundry.Operations[0]);
        }

        [Fact]
        public async Task TrackExecutedOperations_GivenForgeAsync()
        {
            // Arrange
            var foundry = new FakeWorkflowFoundry();
            var op1 = new SimpleTestOperation("Op1");
            var op2 = new SimpleTestOperation("Op2");
            foundry.AddOperation(op1);
            foundry.AddOperation(op2);

            // Act
            await foundry.ForgeAsync();

            // Assert
            Assert.Equal(2, foundry.ExecutedOperations.Count);
            Assert.Contains(op1, foundry.ExecutedOperations);
            Assert.Contains(op2, foundry.ExecutedOperations);
        }

        [Fact]
        public void ManuallyTrackOperation_GivenTrackExecution()
        {
            // Arrange
            var foundry = new FakeWorkflowFoundry();
            var operation = new SimpleTestOperation("Test");

            // Act
            foundry.TrackExecution(operation);

            // Assert
            Assert.Single(foundry.ExecutedOperations);
            Assert.Same(operation, foundry.ExecutedOperations[0]);
        }

        #endregion Operation Tracking Tests

        #region Properties Tests

        [Fact]
        public async Task AllowSetAndRead_GivenProperties()
        {
            // Arrange
            var foundry = new FakeWorkflowFoundry();
            var operation = new PropertySettingOperation("Test", "myKey", "myValue");
            foundry.AddOperation(operation);

            // Act
            await foundry.ForgeAsync();

            // Assert
            Assert.True(foundry.Properties.ContainsKey("myKey"));
            Assert.Equal("myValue", foundry.Properties["myKey"]);
        }

        #endregion Properties Tests

        #region Reset Tests

        [Fact]
        public async Task ClearAllState_GivenReset()
        {
            // Arrange
            var foundry = new FakeWorkflowFoundry();
            foundry.AddOperation(new SimpleTestOperation("Test"));
            foundry.Properties["key"] = "value";
            await foundry.ForgeAsync();

            // Act
            foundry.Reset();

            // Assert
            Assert.Empty(foundry.Operations);
            Assert.Empty(foundry.ExecutedOperations);
            Assert.Empty(foundry.Properties);
            Assert.Null(foundry.CurrentWorkflow);
            Assert.False(foundry.IsFrozen);
        }

        #endregion Reset Tests

        #region Event Tests

        [Fact]
        public async Task RaiseOperationEvents_GivenForgeAsync()
        {
            // Arrange
            var foundry = new FakeWorkflowFoundry();
            var operation = new SimpleTestOperation("Test");
            foundry.AddOperation(operation);

            var startedRaised = false;
            var completedRaised = false;

            foundry.OperationStarted += (s, e) => startedRaised = true;
            foundry.OperationCompleted += (s, e) => completedRaised = true;

            // Act
            await foundry.ForgeAsync();

            // Assert
            Assert.True(startedRaised);
            Assert.True(completedRaised);
        }

        [Fact]
        public async Task RaiseFailedEvent_GivenForgeAsyncOnException()
        {
            // Arrange
            var foundry = new FakeWorkflowFoundry();
            var operation = new FailingTestOperation("Test");
            foundry.AddOperation(operation);

            var failedRaised = false;
            foundry.OperationFailed += (s, e) => failedRaised = true;

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => foundry.ForgeAsync());
            Assert.True(failedRaised);
        }

        #endregion Event Tests

        #region Dispose Tests

        [Fact]
        public void SetDisposedState_GivenDispose()
        {
            // Arrange
            var foundry = new FakeWorkflowFoundry();

            // Act
            foundry.Dispose();

            // Assert - should throw on subsequent operations
            Assert.Throws<ObjectDisposedException>(() => foundry.AddOperation(new SimpleTestOperation("Test")));
        }

        #endregion Dispose Tests

        #region Test Operations

        private class SimpleTestOperation : WorkflowOperationBase
        {
            public SimpleTestOperation(string name) => Name = name;

            public override string Name { get; }

            protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken ct)
            {
                return Task.FromResult<object?>("result");
            }
        }

        private class PropertySettingOperation : WorkflowOperationBase
        {
            private readonly string _key;
            private readonly string _value;

            public PropertySettingOperation(string name, string key, string value)
            {
                Name = name;
                _key = key;
                _value = value;
            }

            public override string Name { get; }

            protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken ct)
            {
                foundry.Properties[_key] = _value;
                return Task.FromResult<object?>(null);
            }
        }

        private class FailingTestOperation : WorkflowOperationBase
        {
            public FailingTestOperation(string name) => Name = name;

            public override string Name { get; }

            protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken ct)
            {
                throw new InvalidOperationException("Intentional failure");
            }
        }

        #endregion Test Operations
    }
}