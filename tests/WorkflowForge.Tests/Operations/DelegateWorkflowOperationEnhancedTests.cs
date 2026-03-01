using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Exceptions;
using WorkflowForge.Operations;

namespace WorkflowForge.Tests.Operations;

/// <summary>
/// Comprehensive tests for DelegateWorkflowOperation covering RestoreAsync,
/// factory methods, generic variant, null handling, cancellation, and error paths.
/// </summary>
public class DelegateWorkflowOperationEnhancedShould
{
    #region RestoreAsync

    [Fact]
    public async Task ReturnImmediately_GivenNullRestoreFunc()
    {
        var operation = new DelegateWorkflowOperation("Test", (input, _, _) => Task.FromResult<object?>(input));
        var foundry = new Mock<IWorkflowFoundry>().Object;

        await operation.RestoreAsync("output", foundry);
        // Should not throw
    }

    [Fact]
    public async Task InvokeRestore_GivenRestoreFunc()
    {
        var restored = false;
        var operation = new DelegateWorkflowOperation(
            "Test",
            (input, _, _) => Task.FromResult<object?>(input),
            (output, _, _) => { restored = true; return Task.CompletedTask; });

        var foundry = new Mock<IWorkflowFoundry>().Object;
        await operation.RestoreAsync("output", foundry);

        Assert.True(restored);
    }

    [Fact]
    public async Task ThrowWorkflowRestoreException_GivenRestoreThrows()
    {
        var operation = new DelegateWorkflowOperation(
            "Test",
            (input, _, _) => Task.FromResult<object?>(input),
            (_, _, _) => throw new InvalidOperationException("Restore failed"));

        var foundry = new Mock<IWorkflowFoundry>();
        foundry.Setup(f => f.ExecutionId).Returns(Guid.NewGuid());
        foundry.Setup(f => f.CurrentWorkflow).Returns((IWorkflow?)null);

        var ex = await Assert.ThrowsAsync<WorkflowRestoreException>(() =>
            operation.RestoreAsync("output", foundry.Object));

        Assert.Contains("Restore failed", ex.InnerException?.Message);
    }

    [Fact]
    public async Task PropagateWithoutWrapping_GivenRestoreOperationCanceled()
    {
        var operation = new DelegateWorkflowOperation(
            "Test",
            (input, _, _) => Task.FromResult<object?>(input),
            (_, _, ct) => throw new OperationCanceledException(ct));

        var foundry = new Mock<IWorkflowFoundry>().Object;

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            operation.RestoreAsync("output", foundry));
    }

    #endregion

    #region ForgeAsync - Error paths

    [Fact]
    public async Task PropagateWithoutWrapping_GivenForgeAsyncOperationCanceled()
    {
        var operation = new DelegateWorkflowOperation("Test", (_, _, ct) =>
            throw new OperationCanceledException(ct));

        var foundry = new Mock<IWorkflowFoundry>().Object;

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            operation.ForgeAsync("input", foundry));
    }

    [Fact]
    public async Task HandleCorrectly_GivenNullInput()
    {
        object? captured = "not null";
        var operation = new DelegateWorkflowOperation("Test", (input, _, _) =>
        {
            captured = input;
            return Task.FromResult<object?>(input);
        });

        var foundry = new Mock<IWorkflowFoundry>().Object;
        var result = await operation.ForgeAsync(null, foundry);

        Assert.Null(captured);
        Assert.Null(result);
    }

    #endregion

    #region Factory Methods - FromSync, FromAsync, FromAction, FromAsyncAction

    [Fact]
    public async Task ExecuteSyncFunc_GivenFromSync()
    {
        var operation = DelegateWorkflowOperation.FromSync("Test", input => $"Processed: {input}");

        var foundry = new Mock<IWorkflowFoundry>().Object;
        var result = await operation.ForgeAsync("hello", foundry);

        Assert.Equal("Processed: hello", result);
    }

    [Fact]
    public async Task ExecuteAsyncFunc_GivenFromAsync()
    {
        var operation = DelegateWorkflowOperation.FromAsync("Test", async input =>
        {
            await Task.Yield();
            return $"Async: {input}";
        });

        var foundry = new Mock<IWorkflowFoundry>().Object;
        var result = await operation.ForgeAsync("world", foundry);

        Assert.Equal("Async: world", result);
    }

    [Fact]
    public async Task ReturnNull_GivenFromAction()
    {
        var executed = false;
        var operation = DelegateWorkflowOperation.FromAction("Test", input =>
        {
            executed = true;
            Assert.Equal("input", input);
        });

        var foundry = new Mock<IWorkflowFoundry>().Object;
        var result = await operation.ForgeAsync("input", foundry);

        Assert.True(executed);
        Assert.Null(result);
    }

    [Fact]
    public async Task ReturnNull_GivenFromAsyncAction()
    {
        var executed = false;
        var operation = DelegateWorkflowOperation.FromAsyncAction("Test", async input =>
        {
            await Task.Yield();
            executed = true;
        });

        var foundry = new Mock<IWorkflowFoundry>().Object;
        var result = await operation.ForgeAsync("input", foundry);

        Assert.True(executed);
        Assert.Null(result);
    }

    #endregion

    #region Generic DelegateWorkflowOperation<TInput, TOutput>

    [Fact]
    public async Task ExecuteTypedFunc_GivenGenericForgeAsync()
    {
        var operation = new DelegateWorkflowOperation<string, int>("Test", (input, _, _) =>
            Task.FromResult(input.Length));

        var foundry = new Mock<IWorkflowFoundry>().Object;
        var result = await operation.ForgeAsync("hello", foundry);

        Assert.Equal(5, result);
    }

    [Fact]
    public async Task WrapInWorkflowOperationException_GivenGenericThrows()
    {
        var operation = new DelegateWorkflowOperation<string, int>("Test", (_, _, _) =>
            throw new DivideByZeroException("div by zero"));

        var foundry = new Mock<IWorkflowFoundry>().Object;

        var ex = await Assert.ThrowsAsync<WorkflowOperationException>(() =>
            operation.ForgeAsync("x", foundry));

        Assert.IsType<DivideByZeroException>(ex.InnerException);
    }

    [Fact]
    public async Task InvokeRestore_GivenGenericRestoreFunc()
    {
        var restored = false;
        var operation = new DelegateWorkflowOperation<string, int>(
            "Test",
            (input, _, _) => Task.FromResult(input.Length),
            (output, _, _) => { restored = true; return Task.CompletedTask; });

        var foundry = new Mock<IWorkflowFoundry>().Object;
        await operation.RestoreAsync(42, foundry);

        Assert.True(restored);
    }

    [Fact]
    public async Task CreateTypedOperation_GivenGenericFromSync()
    {
        var operation = DelegateWorkflowOperation<string, int>.FromSync("Test", s => s.Length);

        var foundry = new Mock<IWorkflowFoundry>().Object;
        var result = await operation.ForgeAsync("test", foundry);

        Assert.Equal(4, result);
    }

    [Fact]
    public async Task CreateTypedOperation_GivenGenericFromAsync()
    {
        var operation = DelegateWorkflowOperation<string, string>.FromAsync("Test", async s =>
        {
            await Task.Yield();
            return s.ToUpperInvariant();
        });

        var foundry = new Mock<IWorkflowFoundry>().Object;
        var result = await operation.ForgeAsync("hello", foundry);

        Assert.Equal("HELLO", result);
    }

    [Fact]
    public async Task CreateOperationWithFoundryAccess_GivenGenericWithFoundry()
    {
        Guid? capturedId = null;
        var operation = DelegateWorkflowOperation<string, string>.WithFoundry("Test", (input, foundry) =>
        {
            capturedId = foundry.ExecutionId;
            return Task.FromResult(input);
        });

        var foundry = new Mock<IWorkflowFoundry>();
        var execId = Guid.NewGuid();
        foundry.Setup(f => f.ExecutionId).Returns(execId);

        await operation.ForgeAsync("x", foundry.Object);

        Assert.Equal(execId, capturedId);
    }

    #endregion

    #region WorkflowOperations factory

    [Fact]
    public void ReturnDelegateOperation_GivenWorkflowOperationsCreate()
    {
        var operation = WorkflowOperations.Create("Test", input => input);

        Assert.IsType<DelegateWorkflowOperation>(operation);
        Assert.Equal("Test", operation.Name);
    }

    [Fact]
    public void ReturnDelegateOperation_GivenWorkflowOperationsCreateAsync()
    {
        var operation = WorkflowOperations.CreateAsync("Test", input => Task.FromResult<object?>(input));

        Assert.IsType<DelegateWorkflowOperation>(operation);
    }

    [Fact]
    public void ReturnDelegateOperation_GivenWorkflowOperationsCreateAction()
    {
        var operation = WorkflowOperations.CreateAction("Test", _ => { });

        Assert.IsType<DelegateWorkflowOperation>(operation);
    }

    [Fact]
    public void ReturnDelegateOperation_GivenWorkflowOperationsCreateAsyncAction()
    {
        var operation = WorkflowOperations.CreateAsyncAction("Test", _ => Task.CompletedTask);

        Assert.IsType<DelegateWorkflowOperation>(operation);
    }

    [Fact]
    public void ReturnTypedDelegateOperation_GivenWorkflowOperationsCreateTyped()
    {
        var operation = WorkflowOperations.Create<string, int>("Test", s => s.Length);

        Assert.IsType<DelegateWorkflowOperation<string, int>>(operation);
    }

    [Fact]
    public void ReturnTypedDelegateOperation_GivenWorkflowOperationsCreateAsyncTyped()
    {
        var operation = WorkflowOperations.CreateAsync<string, string>("Test", s => Task.FromResult(s));

        Assert.IsType<DelegateWorkflowOperation<string, string>>(operation);
    }

    #endregion
}
