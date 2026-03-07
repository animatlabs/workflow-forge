using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Exceptions;
using WorkflowForge.Operations;

namespace WorkflowForge.Tests.OperationsTests;

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
