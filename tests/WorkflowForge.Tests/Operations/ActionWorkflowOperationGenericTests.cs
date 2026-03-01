using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Exceptions;
using WorkflowForge.Operations;

namespace WorkflowForge.Tests.Operations;

/// <summary>
/// Tests for ActionWorkflowOperation&lt;TInput&gt; generic variant, RestoreAsync with restoreFunc,
/// and error paths not covered by ActionWorkflowOperationEnhancedTests.
/// </summary>
public class ActionWorkflowOperationGenericShould
{
    #region ActionWorkflowOperation<TInput> - Constructor and ForgeAsync

    [Fact]
    public void SetName_GivenGenericValidParams()
    {
        var action = new Func<string, IWorkflowFoundry, CancellationToken, Task>((_, _, _) => Task.CompletedTask);
        var operation = new ActionWorkflowOperation<string>("TypedAction", action);

        Assert.Equal("TypedAction", operation.Name);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenGenericNullName()
    {
        var action = new Func<string, IWorkflowFoundry, CancellationToken, Task>((_, _, _) => Task.CompletedTask);

        Assert.Throws<ArgumentNullException>(() =>
            new ActionWorkflowOperation<string>(null!, action));
    }

    [Fact]
    public void ThrowArgumentNullException_GivenGenericNullAction()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ActionWorkflowOperation<string>("Test", null!));
    }

    [Fact]
    public async Task ExecuteActionAndReturnInput_GivenGenericForgeAsync()
    {
        var captured = "";
        var action = new Func<string, IWorkflowFoundry, CancellationToken, Task>((input, _, _) =>
        {
            captured = input;
            return Task.CompletedTask;
        });

        var operation = new ActionWorkflowOperation<string>("Test", action);
        var foundry = new Mock<IWorkflowFoundry>().Object;

        var result = await operation.ForgeAsync("hello", foundry);

        Assert.Equal("hello", captured);
        Assert.Equal("hello", result);
    }

    [Fact]
    public async Task WrapInWorkflowOperationException_GivenGenericActionThrows()
    {
        var action = new Func<string, IWorkflowFoundry, CancellationToken, Task>((_, _, _) =>
            throw new InvalidOperationException("Action failed"));

        var operation = new ActionWorkflowOperation<string>("Test", action);
        var foundry = new Mock<IWorkflowFoundry>().Object;

        var ex = await Assert.ThrowsAsync<WorkflowOperationException>(() =>
            operation.ForgeAsync("input", foundry));

        Assert.Contains("Action failed", ex.InnerException?.Message);
    }

    [Fact]
    public async Task PropagateWithoutWrapping_GivenGenericOperationCanceled()
    {
        var action = new Func<string, IWorkflowFoundry, CancellationToken, Task>((_, _, ct) =>
            throw new OperationCanceledException(ct));

        var operation = new ActionWorkflowOperation<string>("Test", action);
        var foundry = new Mock<IWorkflowFoundry>().Object;

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            operation.ForgeAsync("input", foundry));
    }

    #endregion

    #region ActionWorkflowOperation<TInput> - RestoreAsync

    [Fact]
    public async Task ReturnImmediately_GivenGenericNullRestoreFunc()
    {
        var action = new Func<string, IWorkflowFoundry, CancellationToken, Task>((_, _, _) => Task.CompletedTask);
        var operation = new ActionWorkflowOperation<string>("Test", action);
        var foundry = new Mock<IWorkflowFoundry>().Object;

        await operation.RestoreAsync("output", foundry);
    }

    [Fact]
    public async Task InvokeRestore_GivenGenericRestoreFunc()
    {
        var restored = false;
        var action = new Func<string, IWorkflowFoundry, CancellationToken, Task>((_, _, _) => Task.CompletedTask);
        var restoreAction = new Func<string, IWorkflowFoundry, CancellationToken, Task>((output, _, _) =>
        {
            restored = true;
            Assert.Equal("output", output);
            return Task.CompletedTask;
        });

        var operation = new ActionWorkflowOperation<string>("Test", action, restoreAction);
        var foundry = new Mock<IWorkflowFoundry>().Object;

        await operation.RestoreAsync("output", foundry);

        Assert.True(restored);
    }

    [Fact]
    public async Task ThrowWorkflowRestoreException_GivenGenericRestoreThrows()
    {
        var action = new Func<string, IWorkflowFoundry, CancellationToken, Task>((_, _, _) => Task.CompletedTask);
        var restoreAction = new Func<string, IWorkflowFoundry, CancellationToken, Task>((_, _, _) =>
            throw new InvalidOperationException("Restore failed"));

        var operation = new ActionWorkflowOperation<string>("Test", action, restoreAction);
        var foundry = new Mock<IWorkflowFoundry>().Object;

        var ex = await Assert.ThrowsAsync<WorkflowRestoreException>(() =>
            operation.RestoreAsync("output", foundry));

        Assert.Contains("Restore failed", ex.InnerException?.Message);
    }

    #endregion

    #region ActionWorkflowOperation (non-generic) - RestoreAsync with restoreFunc

    [Fact]
    public async Task InvokeRestore_GivenRestoreAsyncWithRestoreFunc()
    {
        var restored = false;
        var action = new Func<object?, IWorkflowFoundry, CancellationToken, Task>((_, _, _) => Task.CompletedTask);
        var restoreAction = new Func<object?, IWorkflowFoundry, CancellationToken, Task>((output, _, _) =>
        {
            restored = true;
            Assert.Equal("output", output);
            return Task.CompletedTask;
        });

        var operation = new ActionWorkflowOperation("Test", action, restoreAction);
        var foundry = new Mock<IWorkflowFoundry>().Object;

        await operation.RestoreAsync("output", foundry);

        Assert.True(restored);
    }

    [Fact]
    public async Task ThrowWorkflowRestoreException_GivenRestoreThrows()
    {
        var action = new Func<object?, IWorkflowFoundry, CancellationToken, Task>((_, _, _) => Task.CompletedTask);
        var restoreAction = new Func<object?, IWorkflowFoundry, CancellationToken, Task>((_, _, _) =>
            throw new InvalidOperationException("Restore failed"));

        var operation = new ActionWorkflowOperation("Test", action, restoreAction);
        var foundry = new Mock<IWorkflowFoundry>();
        foundry.Setup(f => f.ExecutionId).Returns(Guid.NewGuid());
        foundry.Setup(f => f.CurrentWorkflow).Returns((IWorkflow?)null);

        var ex = await Assert.ThrowsAsync<WorkflowRestoreException>(() =>
            operation.RestoreAsync("output", foundry.Object));

        Assert.Contains("Restore failed", ex.InnerException?.Message);
    }

    [Fact]
    public async Task IncludeFoundryContextInException_GivenActionThrows()
    {
        var action = new Func<object?, IWorkflowFoundry, CancellationToken, Task>((_, _, _) =>
            throw new InvalidOperationException("Fail"));

        var operation = new ActionWorkflowOperation("Test", action);
        var foundry = new Mock<IWorkflowFoundry>();
        var execId = Guid.NewGuid();
        foundry.Setup(f => f.ExecutionId).Returns(execId);
        var mockWorkflow = new Mock<IWorkflow>();
        mockWorkflow.Setup(w => w.Id).Returns(Guid.NewGuid());
        foundry.Setup(f => f.CurrentWorkflow).Returns(mockWorkflow.Object);

        var ex = await Assert.ThrowsAsync<WorkflowOperationException>(() =>
            operation.ForgeAsync("input", foundry.Object));

        Assert.Equal(execId, ex.ExecutionId);
        Assert.Contains("Test", ex.Message);
    }

    #endregion
}
