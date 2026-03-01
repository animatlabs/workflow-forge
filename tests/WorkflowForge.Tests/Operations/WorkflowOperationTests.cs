using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Exceptions;
using WorkflowForge.Operations;

namespace WorkflowForge.Tests.Operations;

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
}

public class ActionWorkflowOperationShould
{
    [Fact]
    public void SetProperties_GivenValidParameters()
    {
        // Arrange
        const string name = "TestAction";
        var action = new Func<object?, IWorkflowFoundry, CancellationToken, Task>((input, foundry, ct) => Task.CompletedTask);

        // Act
        var operation = new ActionWorkflowOperation(name, action);

        // Assert
        Assert.Equal(name, operation.Name);
        Assert.NotEqual(Guid.Empty, operation.Id);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullName()
    {
        // Arrange
        var action = new Func<object?, IWorkflowFoundry, CancellationToken, Task>((input, foundry, ct) => Task.CompletedTask);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ActionWorkflowOperation(null!, action));
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullAction()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ActionWorkflowOperation("Test", (Func<object?, IWorkflowFoundry, CancellationToken, Task>)null!));
    }

    [Fact]
    public async Task ExecuteActionAndReturnInput_GivenForgeAsync()
    {
        // Arrange
        var executed = false;
        var action = new Func<object?, IWorkflowFoundry, CancellationToken, Task>((input, foundry, ct) =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        var operation = new ActionWorkflowOperation("TestAction", action);
        var foundry = new Mock<IWorkflowFoundry>().Object;
        var inputData = "test input";

        // Act
        var result = await operation.ForgeAsync(inputData, foundry, CancellationToken.None);

        // Assert
        Assert.True(executed);
        Assert.Equal(inputData, result);
    }

    [Fact]
    public async Task ThrowWorkflowOperationException_GivenException()
    {
        // Arrange
        var action = new Func<object?, IWorkflowFoundry, CancellationToken, Task>((input, foundry, ct) =>
        {
            throw new InvalidOperationException("Test exception");
        });

        var operation = new ActionWorkflowOperation("TestAction", action);
        var foundry = new Mock<IWorkflowFoundry>().Object;

        // Act & Assert
        await Assert.ThrowsAsync<WorkflowOperationException>(() =>
            operation.ForgeAsync("input", foundry, CancellationToken.None));
    }
}

public class ConditionalWorkflowOperationShould
{
    [Fact]
    public void SetProperties_GivenValidParameters()
    {
        // Arrange
        var condition = new Func<object?, IWorkflowFoundry, CancellationToken, Task<bool>>((input, foundry, ct) => Task.FromResult(true));
        var trueOperation = new Mock<IWorkflowOperation>().Object;

        // Act
        var operation = new ConditionalWorkflowOperation(condition, trueOperation);

        // Assert
        Assert.NotEqual(Guid.Empty, operation.Id);
        Assert.Equal("ConditionalOperation", operation.Name);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullCondition()
    {
        // Arrange
        var trueOperation = new Mock<IWorkflowOperation>().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ConditionalWorkflowOperation((Func<object?, IWorkflowFoundry, CancellationToken, Task<bool>>)null!, trueOperation));
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullTrueOperation()
    {
        // Arrange
        var condition = new Func<object?, IWorkflowFoundry, CancellationToken, Task<bool>>((input, foundry, ct) => Task.FromResult(true));

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ConditionalWorkflowOperation(condition, null!));
    }

    [Fact]
    public async Task ExecuteTrueOperation_GivenConditionTrue()
    {
        // Arrange
        var condition = new Func<object?, IWorkflowFoundry, CancellationToken, Task<bool>>((input, foundry, ct) => Task.FromResult(true));
        var trueOperation = new Mock<IWorkflowOperation>();
        trueOperation.Setup(x => x.ForgeAsync(It.IsAny<object?>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync("TrueResult");

        var operation = new ConditionalWorkflowOperation(condition, trueOperation.Object);
        var foundry = new Mock<IWorkflowFoundry>().Object;

        // Act
        var result = await operation.ForgeAsync("test", foundry, CancellationToken.None);

        // Assert
        Assert.Equal("TrueResult", result);
        trueOperation.Verify(x => x.ForgeAsync("test", foundry, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReturnNull_GivenConditionFalse()
    {
        // Arrange
        var condition = new Func<object?, IWorkflowFoundry, CancellationToken, Task<bool>>((input, foundry, ct) => Task.FromResult(false));
        var trueOperation = new Mock<IWorkflowOperation>();
        var operation = new ConditionalWorkflowOperation(condition, trueOperation.Object);
        var foundry = new Mock<IWorkflowFoundry>().Object;

        // Act
        var result = await operation.ForgeAsync("test", foundry, CancellationToken.None);

        // Assert
        Assert.Null(result);
        trueOperation.Verify(x => x.ForgeAsync(It.IsAny<object?>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteFalseOperation_GivenConditionFalseWithFalseOperation()
    {
        // Arrange
        var condition = new Func<object?, IWorkflowFoundry, CancellationToken, Task<bool>>((input, foundry, ct) => Task.FromResult(false));
        var trueOperation = new Mock<IWorkflowOperation>();
        var falseOperation = new Mock<IWorkflowOperation>();
        falseOperation.Setup(x => x.ForgeAsync(It.IsAny<object?>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync("FalseResult");

        var operation = new ConditionalWorkflowOperation(condition, trueOperation.Object, falseOperation.Object);
        var foundry = new Mock<IWorkflowFoundry>().Object;

        // Act
        var result = await operation.ForgeAsync("test", foundry, CancellationToken.None);

        // Assert
        Assert.Equal("FalseResult", result);
        falseOperation.Verify(x => x.ForgeAsync("test", foundry, It.IsAny<CancellationToken>()), Times.Once);
        trueOperation.Verify(x => x.ForgeAsync(It.IsAny<object?>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

public class DelegateWorkflowOperationShould
{
    [Fact]
    public void SetProperties_GivenValidParameters()
    {
        // Arrange
        const string name = "TestDelegate";
        var executeFunc = new Func<object?, IWorkflowFoundry, CancellationToken, Task<object?>>((input, foundry, ct) => Task.FromResult<object?>("result"));

        // Act
        var operation = new DelegateWorkflowOperation(name, executeFunc);

        // Assert
        Assert.Equal(name, operation.Name);
        Assert.NotEqual(Guid.Empty, operation.Id);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullName()
    {
        // Arrange
        var executeFunc = new Func<object?, IWorkflowFoundry, CancellationToken, Task<object?>>((input, foundry, ct) => Task.FromResult<object?>("result"));

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DelegateWorkflowOperation(null!, executeFunc));
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullExecuteFunc()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DelegateWorkflowOperation("Test", null!));
    }

    [Fact]
    public async Task ExecuteFunctionAndReturnResult_GivenForgeAsync()
    {
        // Arrange
        var executeFunc = new Func<object?, IWorkflowFoundry, CancellationToken, Task<object?>>((input, foundry, ct) =>
            Task.FromResult<object?>($"Processed: {input}"));

        var operation = new DelegateWorkflowOperation("TestDelegate", executeFunc);
        var foundry = new Mock<IWorkflowFoundry>().Object;
        var inputData = "test input";

        // Act
        var result = await operation.ForgeAsync(inputData, foundry, CancellationToken.None);

        // Assert
        Assert.Equal("Processed: test input", result);
    }

    [Fact]
    public async Task ThrowWorkflowOperationException_GivenException()
    {
        // Arrange
        var executeFunc = new Func<object?, IWorkflowFoundry, CancellationToken, Task<object?>>((input, foundry, ct) =>
        {
            throw new InvalidOperationException("Test exception");
        });

        var operation = new DelegateWorkflowOperation("TestDelegate", executeFunc);
        var foundry = new Mock<IWorkflowFoundry>().Object;

        // Act & Assert
        await Assert.ThrowsAsync<WorkflowOperationException>(() =>
            operation.ForgeAsync("input", foundry, CancellationToken.None));
    }
}
