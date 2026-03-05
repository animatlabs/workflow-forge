using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Exceptions;
using WorkflowForge.Operations;

namespace WorkflowForge.Tests.OperationsTests;

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
