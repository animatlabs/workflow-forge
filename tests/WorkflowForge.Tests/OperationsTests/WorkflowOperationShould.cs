using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Exceptions;
using WorkflowForge.Operations;

namespace WorkflowForge.Tests.OperationsTests;

public class WorkflowOperationBaseShould
{
    [Fact]
    public void GenerateUniqueGuid_GivenId()
    {
        // Arrange & Act
        var operation1 = new TestOperation("Test1");
        var operation2 = new TestOperation("Test2");

        // Assert
        Assert.NotEqual(operation1.Id, operation2.Id);
        Assert.NotEqual(Guid.Empty, operation1.Id);
        Assert.NotEqual(Guid.Empty, operation2.Id);
    }

    [Fact]
    public void ReturnCorrectName_GivenName()
    {
        // Arrange
        const string expectedName = "TestOperation";
        var operation = new TestOperation(expectedName);

        // Act & Assert
        Assert.Equal(expectedName, operation.Name);
    }

    [Fact]
    public async Task ThrowArgumentNullException_GivenNullFoundry()
    {
        // Arrange
        var operation = new TestOperation("Test");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            operation.ForgeAsync("input", null!, CancellationToken.None));
    }

    [Fact]
    public async Task BeCancellable_GivenCancellationToken()
    {
        // Arrange
        var operation = new DelayedTestOperation("Test", TimeSpan.FromSeconds(5));
        var foundry = new Mock<IWorkflowFoundry>().Object;
        using var cts = new CancellationTokenSource();

        // Act
        var task = operation.ForgeAsync("input", foundry, cts.Token);
        cts.Cancel();

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => task);
    }

    [Fact]
    public async Task ExecuteSuccessfully_GivenForgeAsync()
    {
        // Arrange
        var operation = new TestOperation("Test");
        var foundry = new Mock<IWorkflowFoundry>().Object;

        // Act
        var result = await operation.ForgeAsync("test input", foundry, CancellationToken.None);

        // Assert
        Assert.Equal("TEST INPUT", result);
    }

    [Fact]
    public async Task ReturnCompletedTask_GivenRestoreAsyncDefaultImplementation()
    {
        // Arrange
        var operation = new TestOperation("Test");
        var foundry = new Mock<IWorkflowFoundry>().Object;

        // Act
        var task = operation.RestoreAsync("output", foundry, CancellationToken.None);

        // Assert
        await task;
        Assert.True(task.Status == TaskStatus.RanToCompletion);
    }

    [Fact]
    public void AllowMultipleCalls_GivenDispose()
    {
        // Arrange
        var operation = new TestOperation("Test");

        // Act & Assert - Should not throw
        operation.Dispose();
        operation.Dispose();
        operation.Dispose();
    }

    // Test operation for testing purposes
    private class TestOperation : WorkflowOperationBase
    {
        public TestOperation(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public override string Name { get; }

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (foundry == null)
                throw new ArgumentNullException(nameof(foundry));

            var result = inputData?.ToString()?.ToUpper() ?? "";
            return Task.FromResult<object?>(result);
        }
    }

    [Fact]
    public async Task ThrowInvalidOperationException_GivenTypedForgeAsyncWithMismatchedInput()
    {
        var operation = new TypedTestOperation("Typed");
        var foundry = new Mock<IWorkflowFoundry>().Object;

        IWorkflowOperation untyped = operation;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            untyped.ForgeAsync("not-an-int", foundry, CancellationToken.None));
    }

    [Fact]
    public async Task AcceptNullInput_GivenTypedForgeAsyncWithNullableReferenceInput()
    {
        var operation = new NullableRefTypedTestOperation("NullableRef");
        var foundry = new Mock<IWorkflowFoundry>().Object;

        IWorkflowOperation untyped = operation;

        var result = await untyped.ForgeAsync(null, foundry, CancellationToken.None);

        Assert.Equal("null-input", result);
    }

    [Fact]
    public async Task ThrowInvalidOperationException_GivenTypedRestoreAsyncWithMismatchedOutput()
    {
        var operation = new TypedTestOperation("Typed");
        var foundry = new Mock<IWorkflowFoundry>().Object;

        IWorkflowOperation untyped = operation;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            untyped.RestoreAsync(42, foundry, CancellationToken.None));
    }

    [Fact]
    public async Task AcceptNullOutput_GivenTypedRestoreAsyncWithNullableReferenceOutput()
    {
        var operation = new NullableRefTypedTestOperation("NullableRef");
        var foundry = new Mock<IWorkflowFoundry>().Object;

        IWorkflowOperation untyped = operation;

        await untyped.RestoreAsync(null, foundry, CancellationToken.None);
    }

    // Test operation with delay for cancellation testing
    private class DelayedTestOperation : WorkflowOperationBase
    {
        private readonly TimeSpan _delay;

        public DelayedTestOperation(string name, TimeSpan delay)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _delay = delay;
        }

        public override string Name { get; }

        protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (foundry == null)
                throw new ArgumentNullException(nameof(foundry));

            await Task.Delay(_delay, cancellationToken);
            return inputData;
        }
    }

    private class TypedTestOperation : WorkflowOperationBase<int, string>
    {
        public TypedTestOperation(string name) { Name = name; }
        public override string Name { get; }

        protected override Task<string> ForgeAsyncCore(int inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
            => Task.FromResult($"result-{inputData}");
    }

    private class NullableRefTypedTestOperation : WorkflowOperationBase<string?, string>
    {
        public NullableRefTypedTestOperation(string name) { Name = name; }
        public override string Name { get; }

        protected override Task<string> ForgeAsyncCore(string? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
            => Task.FromResult(inputData ?? "null-input");
    }
}
