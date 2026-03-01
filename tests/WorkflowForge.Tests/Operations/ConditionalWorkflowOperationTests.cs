using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Operations;

namespace WorkflowForge.Tests.Operations;

/// <summary>
/// Comprehensive tests for ConditionalWorkflowOperation covering all constructors,
/// condition evaluation, true/false branches, RestoreAsync, Dispose, and factory methods.
/// Complements the basic tests in WorkflowOperationTests.cs.
/// </summary>
public class ConditionalWorkflowOperationEnhancedTests
{
    #region Constructor - Async with input data

    [Fact]
    public void Constructor_AsyncWithInputData_WithCustomNameAndId_SetsProperties()
    {
        var condition = new Func<object?, IWorkflowFoundry, CancellationToken, Task<bool>>((_, _, _) => Task.FromResult(true));
        var trueOp = new Mock<IWorkflowOperation>().Object;
        var id = Guid.NewGuid();

        var operation = new ConditionalWorkflowOperation(condition, trueOp, null, "CustomName", id);

        Assert.Equal("CustomName", operation.Name);
        Assert.Equal(id, operation.Id);
    }

    #endregion

    #region Constructor - Simple async (no input)

    [Fact]
    public void Constructor_SimpleAsync_WithValidParams_CreatesOperation()
    {
        var condition = new Func<IWorkflowFoundry, CancellationToken, Task<bool>>((_, _) => Task.FromResult(true));
        var trueOp = new Mock<IWorkflowOperation>().Object;

        var operation = new ConditionalWorkflowOperation(condition, trueOp);

        Assert.Equal("ConditionalOperation", operation.Name);
    }

    [Fact]
    public void Constructor_SimpleAsync_WithNullCondition_ThrowsArgumentNullException()
    {
        var trueOp = new Mock<IWorkflowOperation>().Object;

        Assert.Throws<ArgumentNullException>(() =>
            new ConditionalWorkflowOperation((Func<IWorkflowFoundry, CancellationToken, Task<bool>>)null!, trueOp));
    }

    [Fact]
    public void Constructor_SimpleAsync_WithFalseOperation_AcceptsFalseOp()
    {
        var condition = new Func<IWorkflowFoundry, CancellationToken, Task<bool>>((_, _) => Task.FromResult(false));
        var trueOp = new Mock<IWorkflowOperation>().Object;
        var falseOp = new Mock<IWorkflowOperation>().Object;

        var operation = new ConditionalWorkflowOperation(condition, trueOp, falseOp);

        Assert.NotNull(operation);
    }

    #endregion

    #region Constructor - Sync condition

    [Fact]
    public void Constructor_SyncCondition_WithValidParams_CreatesOperation()
    {
        var condition = new Func<object?, IWorkflowFoundry, bool>((input, _) => input is int i && i > 0);
        var trueOp = new Mock<IWorkflowOperation>().Object;

        var operation = new ConditionalWorkflowOperation(condition, trueOp);

        Assert.NotNull(operation);
    }

    [Fact]
    public void Constructor_SyncCondition_WithNullCondition_ThrowsArgumentNullException()
    {
        var trueOp = new Mock<IWorkflowOperation>().Object;

        Assert.Throws<ArgumentNullException>(() =>
            new ConditionalWorkflowOperation((Func<object?, IWorkflowFoundry, bool>)null!, trueOp));
    }

    #endregion

    #region ForgeAsync - Condition evaluation

    [Fact]
    public async Task ForgeAsync_SimpleAsyncCondition_WhenTrue_ExecutesTrueBranch()
    {
        var condition = new Func<IWorkflowFoundry, CancellationToken, Task<bool>>((_, _) => Task.FromResult(true));
        var trueOp = new Mock<IWorkflowOperation>();
        trueOp.Setup(x => x.ForgeAsync(It.IsAny<object?>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("TrueResult");

        var operation = new ConditionalWorkflowOperation(condition, trueOp.Object);
        var foundry = new Mock<IWorkflowFoundry>().Object;

        var result = await operation.ForgeAsync("input", foundry);

        Assert.Equal("TrueResult", result);
        trueOp.Verify(x => x.ForgeAsync("input", foundry, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForgeAsync_SimpleAsyncCondition_WhenFalse_ExecutesFalseBranch()
    {
        var condition = new Func<IWorkflowFoundry, CancellationToken, Task<bool>>((_, _) => Task.FromResult(false));
        var trueOp = new Mock<IWorkflowOperation>().Object;
        var falseOp = new Mock<IWorkflowOperation>();
        falseOp.Setup(x => x.ForgeAsync(It.IsAny<object?>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("FalseResult");

        var operation = new ConditionalWorkflowOperation(condition, trueOp, falseOp.Object);
        var foundry = new Mock<IWorkflowFoundry>().Object;

        var result = await operation.ForgeAsync("input", foundry);

        Assert.Equal("FalseResult", result);
    }

    [Fact]
    public async Task ForgeAsync_WhenConditionFalseAndNoFalseOp_ReturnsNull()
    {
        var condition = new Func<object?, IWorkflowFoundry, CancellationToken, Task<bool>>((_, _, _) => Task.FromResult(false));
        var trueOp = new Mock<IWorkflowOperation>().Object;

        var operation = new ConditionalWorkflowOperation(condition, trueOp);
        var foundry = new Mock<IWorkflowFoundry>().Object;

        var result = await operation.ForgeAsync("input", foundry);

        Assert.Null(result);
    }

    [Fact]
    public async Task ForgeAsync_WithNullFoundry_ThrowsArgumentNullException()
    {
        var condition = new Func<object?, IWorkflowFoundry, CancellationToken, Task<bool>>((_, _, _) => Task.FromResult(true));
        var trueOp = new Mock<IWorkflowOperation>().Object;
        var operation = new ConditionalWorkflowOperation(condition, trueOp);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            operation.ForgeAsync("input", null!));
    }

    [Fact]
    public async Task ForgeAsync_WithCancellation_PropagatesToCondition()
    {
        var cts = new CancellationTokenSource();
        var conditionCalled = false;
        var condition = new Func<object?, IWorkflowFoundry, CancellationToken, Task<bool>>((_, _, ct) =>
        {
            conditionCalled = true;
            Assert.True(ct.IsCancellationRequested);
            return Task.FromResult(true);
        });
        var trueOp = new Mock<IWorkflowOperation>();
        trueOp.Setup(x => x.ForgeAsync(It.IsAny<object?>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("ok");

        var operation = new ConditionalWorkflowOperation(condition, trueOp.Object);
        var foundry = new Mock<IWorkflowFoundry>().Object;
        cts.Cancel();

        var result = await operation.ForgeAsync("input", foundry, cts.Token);

        Assert.True(conditionCalled);
        Assert.Equal("ok", result);
    }

    #endregion

    #region RestoreAsync

    [Fact]
    public async Task RestoreAsync_WhenLastConditionWasTrue_RestoresTrueOperation()
    {
        var condition = new Func<object?, IWorkflowFoundry, CancellationToken, Task<bool>>((_, _, _) => Task.FromResult(true));
        var trueOp = new Mock<IWorkflowOperation>();
        trueOp.Setup(x => x.ForgeAsync(It.IsAny<object?>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("output");
        trueOp.Setup(x => x.RestoreAsync(It.IsAny<object?>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var operation = new ConditionalWorkflowOperation(condition, trueOp.Object);
        var foundry = new Mock<IWorkflowFoundry>().Object;

        await operation.ForgeAsync("input", foundry);
        await operation.RestoreAsync("output", foundry);

        trueOp.Verify(x => x.RestoreAsync("output", foundry, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RestoreAsync_WhenLastConditionWasFalse_RestoresFalseOperation()
    {
        var condition = new Func<object?, IWorkflowFoundry, CancellationToken, Task<bool>>((_, _, _) => Task.FromResult(false));
        var trueOp = new Mock<IWorkflowOperation>().Object;
        var falseOp = new Mock<IWorkflowOperation>();
        falseOp.Setup(x => x.ForgeAsync(It.IsAny<object?>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("falseOutput");
        falseOp.Setup(x => x.RestoreAsync(It.IsAny<object?>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var operation = new ConditionalWorkflowOperation(condition, trueOp, falseOp.Object);
        var foundry = new Mock<IWorkflowFoundry>().Object;

        await operation.ForgeAsync("input", foundry);
        await operation.RestoreAsync("falseOutput", foundry);

        falseOp.Verify(x => x.RestoreAsync("falseOutput", foundry, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RestoreAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
        var condition = new Func<object?, IWorkflowFoundry, CancellationToken, Task<bool>>((_, _, _) => Task.FromResult(true));
        var trueOp = new Mock<IWorkflowOperation>().Object;
        var operation = new ConditionalWorkflowOperation(condition, trueOp);
        var foundry = new Mock<IWorkflowFoundry>().Object;

        operation.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            operation.RestoreAsync("output", foundry));
    }

    [Fact]
    public async Task RestoreAsync_WithNullFoundry_ThrowsArgumentNullException()
    {
        var condition = new Func<object?, IWorkflowFoundry, CancellationToken, Task<bool>>((_, _, _) => Task.FromResult(true));
        var trueOp = new Mock<IWorkflowOperation>().Object;
        var operation = new ConditionalWorkflowOperation(condition, trueOp);

        await operation.ForgeAsync("input", new Mock<IWorkflowFoundry>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            operation.RestoreAsync("output", null!));
    }

    #endregion

    #region Dispose

    [Fact]
    public async Task ForgeAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
        var condition = new Func<object?, IWorkflowFoundry, CancellationToken, Task<bool>>((_, _, _) => Task.FromResult(true));
        var trueOp = new Mock<IWorkflowOperation>().Object;
        var operation = new ConditionalWorkflowOperation(condition, trueOp);
        var foundry = new Mock<IWorkflowFoundry>().Object;

        operation.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            operation.ForgeAsync("input", foundry));
    }

    [Fact]
    public void Dispose_DisposesChildOperations()
    {
        var condition = new Func<object?, IWorkflowFoundry, CancellationToken, Task<bool>>((_, _, _) => Task.FromResult(true));
        var trueOp = new Mock<IWorkflowOperation>();
        var falseOp = new Mock<IWorkflowOperation>();

        var operation = new ConditionalWorkflowOperation(condition, trueOp.Object, falseOp.Object);
        operation.Dispose();

        trueOp.Verify(x => x.Dispose(), Times.Once);
        falseOp.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_WhenChildThrows_SwallowsException()
    {
        var condition = new Func<object?, IWorkflowFoundry, CancellationToken, Task<bool>>((_, _, _) => Task.FromResult(true));
        var trueOp = new Mock<IWorkflowOperation>();
        trueOp.Setup(x => x.Dispose()).Throws(new InvalidOperationException("Dispose failed"));

        var operation = new ConditionalWorkflowOperation(condition, trueOp.Object);

        // Should not throw
        operation.Dispose();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var condition = new Func<object?, IWorkflowFoundry, CancellationToken, Task<bool>>((_, _, _) => Task.FromResult(true));
        var trueOp = new Mock<IWorkflowOperation>().Object;

        var operation = new ConditionalWorkflowOperation(condition, trueOp);

        operation.Dispose();
        operation.Dispose();
    }

    #endregion

    #region Factory Methods

    [Fact]
    public void CreateDataAware_ReturnsConditionalOperation()
    {
        var condition = new Func<object?, IWorkflowFoundry, CancellationToken, Task<bool>>((_, _, _) => Task.FromResult(true));
        var trueOp = new Mock<IWorkflowOperation>().Object;

        var operation = ConditionalWorkflowOperation.CreateDataAware(condition, trueOp);

        Assert.NotNull(operation);
        Assert.IsType<ConditionalWorkflowOperation>(operation);
    }

    [Fact]
    public async Task CreateTyped_WithTypedCondition_ExecutesCorrectly()
    {
        var condition = new Func<int, IWorkflowFoundry, CancellationToken, Task<bool>>((input, _, _) =>
            Task.FromResult(input > 10));
        var trueOp = new Mock<IWorkflowOperation>();
        trueOp.Setup(x => x.ForgeAsync(It.IsAny<object?>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("high");
        var falseOp = new Mock<IWorkflowOperation>();
        falseOp.Setup(x => x.ForgeAsync(It.IsAny<object?>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("low");

        var operation = ConditionalWorkflowOperation.CreateTyped(condition, trueOp.Object, falseOp.Object);
        var foundry = new Mock<IWorkflowFoundry>().Object;

        var resultHigh = await operation.ForgeAsync(15, foundry);
        Assert.Equal("high", resultHigh);

        var resultLow = await operation.ForgeAsync(5, foundry);
        Assert.Equal("low", resultLow);
    }

    [Fact]
    public void Create_WithSyncCondition_ReturnsConditionalOperation()
    {
        var condition = new Func<IWorkflowFoundry, bool>(_ => true);
        var trueOp = new Mock<IWorkflowOperation>().Object;
        var falseOp = new Mock<IWorkflowOperation>().Object;

        var operation = ConditionalWorkflowOperation.Create(condition, trueOp, falseOp);

        Assert.NotNull(operation);
        Assert.IsType<ConditionalWorkflowOperation>(operation);
    }

    [Fact]
    public async Task Create_WithSyncCondition_ExecutesCorrectBranch()
    {
        var condition = new Func<IWorkflowFoundry, bool>(f => f != null);
        var trueOp = new Mock<IWorkflowOperation>();
        trueOp.Setup(x => x.ForgeAsync(It.IsAny<object?>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");

        var operation = ConditionalWorkflowOperation.Create(condition, trueOp.Object);
        var foundry = new Mock<IWorkflowFoundry>().Object;

        var result = await operation.ForgeAsync("input", foundry);

        Assert.Equal("true", result);
    }

    #endregion
}
