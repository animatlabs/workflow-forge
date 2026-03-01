using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Events;
using WorkflowForge.Operations;
using WorkflowForge.Testing;

namespace WorkflowForge.Tests.Events
{
    /// <summary>
    /// Tests for operation restore event argument classes.
    /// </summary>
    public class OperationRestoreEventArgsShould
    {
        #region OperationRestoreStartedEventArgs Tests

        [Fact]
        public void SetAllProperties_GivenOperationRestoreStartedEventArgsConstruction()
        {
            // Arrange
            using var foundry = new FakeWorkflowFoundry();
            var operation = new TestOperation("RestoreOp");

            // Act
            var args = new OperationRestoreStartedEventArgs(operation, foundry);

            // Assert
            Assert.Same(operation, args.Operation);
            Assert.Same(foundry, args.Foundry);
            Assert.Equal(args.Timestamp, args.StartedAt);
        }

        [Fact]
        public void ThrowArgumentNullException_GivenNullOperation_OperationRestoreStarted()
        {
            // Arrange
            using var foundry = new FakeWorkflowFoundry();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new OperationRestoreStartedEventArgs(null!, foundry));
        }

        #endregion OperationRestoreStartedEventArgs Tests

        #region OperationRestoreCompletedEventArgs Tests

        [Fact]
        public void SetAllProperties_GivenOperationRestoreCompletedEventArgsConstruction()
        {
            // Arrange
            using var foundry = new FakeWorkflowFoundry();
            var operation = new TestOperation("RestoreOp");
            var duration = TimeSpan.FromMilliseconds(150);

            // Act
            var args = new OperationRestoreCompletedEventArgs(operation, foundry, duration);

            // Assert
            Assert.Same(operation, args.Operation);
            Assert.Same(foundry, args.Foundry);
            Assert.Equal(duration, args.Duration);
            Assert.Equal(args.Timestamp, args.CompletedAt);
        }

        [Fact]
        public void ThrowArgumentNullException_GivenNullOperation_OperationRestoreCompleted()
        {
            // Arrange
            using var foundry = new FakeWorkflowFoundry();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new OperationRestoreCompletedEventArgs(null!, foundry, TimeSpan.Zero));
        }

        #endregion OperationRestoreCompletedEventArgs Tests

        #region OperationRestoreFailedEventArgs Tests

        [Fact]
        public void SetAllProperties_GivenOperationRestoreFailedEventArgsConstruction()
        {
            // Arrange
            using var foundry = new FakeWorkflowFoundry();
            var operation = new TestOperation("RestoreOp");
            var exception = new InvalidOperationException("Restore failed");
            var duration = TimeSpan.FromMilliseconds(50);

            // Act
            var args = new OperationRestoreFailedEventArgs(operation, foundry, exception, duration);

            // Assert
            Assert.Same(operation, args.Operation);
            Assert.Same(foundry, args.Foundry);
            Assert.Same(exception, args.Exception);
            Assert.Equal(duration, args.Duration);
            Assert.Equal(args.Timestamp, args.FailedAt);
        }

        [Fact]
        public void SetNullException_GivenNullException_OperationRestoreFailed()
        {
            // Arrange
            using var foundry = new FakeWorkflowFoundry();
            var operation = new TestOperation("RestoreOp");

            // Act
            var args = new OperationRestoreFailedEventArgs(operation, foundry, null, TimeSpan.Zero);

            // Assert
            Assert.Null(args.Exception);
        }

        [Fact]
        public void ThrowArgumentNullException_GivenNullOperation_OperationRestoreFailed()
        {
            // Arrange
            using var foundry = new FakeWorkflowFoundry();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new OperationRestoreFailedEventArgs(null!, foundry, null, TimeSpan.Zero));
        }

        #endregion OperationRestoreFailedEventArgs Tests

        #region Test Helpers

        private class TestOperation : WorkflowOperationBase
        {
            public TestOperation(string name) => Name = name;

            public override string Name { get; }

            protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken ct)
            {
                return Task.FromResult<object?>(null);
            }
        }

        #endregion Test Helpers
    }
}
