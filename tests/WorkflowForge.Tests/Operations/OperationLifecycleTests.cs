using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Operations;
using WorkflowForge.Testing;

namespace WorkflowForge.Tests.Operations
{
    /// <summary>
    /// Tests for operation lifecycle hooks (OnBeforeExecuteAsync, OnAfterExecuteAsync).
    /// </summary>
    public class OperationLifecycleTests
    {
        [Fact]
        public async Task ForgeAsync_Should_CallOnBeforeExecuteAsync_BeforeCore()
        {
            // Arrange
            var operation = new TrackingOperation();
            var foundry = new FakeWorkflowFoundry();

            // Act
            await operation.ForgeAsync("input", foundry, CancellationToken.None);

            // Assert
            Assert.Equal(3, operation.CallOrder.Count);
            Assert.Equal("OnBefore", operation.CallOrder[0]);
            Assert.Equal("Core", operation.CallOrder[1]);
            Assert.Equal("OnAfter", operation.CallOrder[2]);
        }

        [Fact]
        public async Task OnBeforeExecuteAsync_Should_ReceiveInputData()
        {
            // Arrange
            var operation = new TrackingOperation();
            var foundry = new FakeWorkflowFoundry();
            var input = "test-input";

            // Act
            await operation.ForgeAsync(input, foundry, CancellationToken.None);

            // Assert
            Assert.Equal(input, operation.ReceivedBeforeInput);
        }

        [Fact]
        public async Task OnAfterExecuteAsync_Should_ReceiveInputAndOutputData()
        {
            // Arrange
            var operation = new TrackingOperation();
            var foundry = new FakeWorkflowFoundry();
            var input = "test-input";

            // Act
            await operation.ForgeAsync(input, foundry, CancellationToken.None);

            // Assert
            Assert.Equal(input, operation.ReceivedAfterInput);
            Assert.Equal("processed-test-input", operation.ReceivedAfterOutput);
        }

        [Fact]
        public async Task OnAfterExecuteAsync_Should_NotBeCalled_WhenCoreThrows()
        {
            // Arrange
            var operation = new FailingOperation();
            var foundry = new FakeWorkflowFoundry();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => operation.ForgeAsync("input", foundry, CancellationToken.None));

            Assert.True(operation.BeforeCalled);
            Assert.False(operation.AfterCalled);
        }

        [Fact]
        public async Task TypedOperation_Should_CallTypedHooks()
        {
            // Arrange
            var operation = new TypedTrackingOperation();
            var foundry = new FakeWorkflowFoundry();

            // Act
            await operation.ForgeAsync(42, foundry, CancellationToken.None);

            // Assert
            Assert.Equal(3, operation.CallOrder.Count);
            Assert.Equal("TypedOnBefore", operation.CallOrder[0]);
            Assert.Equal("TypedCore", operation.CallOrder[1]);
            Assert.Equal("TypedOnAfter", operation.CallOrder[2]);
            Assert.Equal(42, operation.ReceivedInput);
            Assert.Equal(84, operation.ReceivedOutput);
        }

        #region Test Operations

        private class TrackingOperation : WorkflowOperationBase
        {
            public override string Name => "TrackingOperation";
            public List<string> CallOrder { get; } = new List<string>();
            public object? ReceivedBeforeInput { get; private set; }
            public object? ReceivedAfterInput { get; private set; }
            public object? ReceivedAfterOutput { get; private set; }

            protected override Task OnBeforeExecuteAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken ct)
            {
                CallOrder.Add("OnBefore");
                ReceivedBeforeInput = inputData;
                return Task.CompletedTask;
            }

            protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken ct)
            {
                CallOrder.Add("Core");
                return Task.FromResult<object?>($"processed-{inputData}");
            }

            protected override Task OnAfterExecuteAsync(object? inputData, object? outputData, IWorkflowFoundry foundry, CancellationToken ct)
            {
                CallOrder.Add("OnAfter");
                ReceivedAfterInput = inputData;
                ReceivedAfterOutput = outputData;
                return Task.CompletedTask;
            }
        }

        private class FailingOperation : WorkflowOperationBase
        {
            public override string Name => "FailingOperation";
            public bool BeforeCalled { get; private set; }
            public bool AfterCalled { get; private set; }

            protected override Task OnBeforeExecuteAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken ct)
            {
                BeforeCalled = true;
                return Task.CompletedTask;
            }

            protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken ct)
            {
                throw new InvalidOperationException("Intentional failure");
            }

            protected override Task OnAfterExecuteAsync(object? inputData, object? outputData, IWorkflowFoundry foundry, CancellationToken ct)
            {
                AfterCalled = true;
                return Task.CompletedTask;
            }
        }

        private class TypedTrackingOperation : WorkflowOperationBase<int, int>
        {
            public override string Name => "TypedTrackingOperation";
            public List<string> CallOrder { get; } = new List<string>();
            public int ReceivedInput { get; private set; }
            public int ReceivedOutput { get; private set; }

            protected override Task OnBeforeExecuteAsync(int input, IWorkflowFoundry foundry, CancellationToken ct)
            {
                CallOrder.Add("TypedOnBefore");
                ReceivedInput = input;
                return Task.CompletedTask;
            }

            protected override Task<int> ForgeAsyncCore(int input, IWorkflowFoundry foundry, CancellationToken ct)
            {
                CallOrder.Add("TypedCore");
                return Task.FromResult(input * 2);
            }

            protected override Task OnAfterExecuteAsync(int input, int output, IWorkflowFoundry foundry, CancellationToken ct)
            {
                CallOrder.Add("TypedOnAfter");
                ReceivedOutput = output;
                return Task.CompletedTask;
            }
        }

        #endregion
    }
}
