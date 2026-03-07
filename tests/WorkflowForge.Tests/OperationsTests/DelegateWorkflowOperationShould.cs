using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Exceptions;
using WorkflowForge.Operations;

namespace WorkflowForge.Tests.OperationsTests;

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
