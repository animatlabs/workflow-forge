using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Operations;

namespace WorkflowForge.Tests;

public class WorkflowOperationsShould
{
    [Fact]
    public void ReturnActionOperation_GivenValidAction()
    {
        // Arrange
        Action<IWorkflowFoundry> action = foundry => { };

        // Act
        var operation = new ActionWorkflowOperation("TestAction", (input, foundry, ct) =>
        {
            action(foundry);
            return Task.CompletedTask;
        });

        // Assert
        Assert.NotNull(operation);
        Assert.IsType<ActionWorkflowOperation>(operation);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullAction()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ActionWorkflowOperation("TestAction", null!));
    }

    [Fact]
    public void ReturnTypedActionOperation_GivenValidTypedAction()
    {
        // Arrange
        Action<string, IWorkflowFoundry> action = (input, foundry) => { };

        // Act
        var operation = new ActionWorkflowOperation("TestTypedAction", (input, foundry, ct) =>
        {
            if (input is string strInput)
                action(strInput, foundry);
            return Task.CompletedTask;
        });

        // Assert
        Assert.NotNull(operation);
        Assert.IsType<ActionWorkflowOperation>(operation);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullTypedAction()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ActionWorkflowOperation("TestTypedAction", null!));
    }

    [Fact]
    public void ReturnDelegateOperation_GivenValidFunc()
    {
        // Arrange
        Func<IWorkflowFoundry, CancellationToken, Task<object?>> func =
            (foundry, cancellationToken) => Task.FromResult<object?>("result");

        // Act
        var operation = new DelegateWorkflowOperation("TestDelegate", (input, foundry, ct) => func(foundry, ct));

        // Assert
        Assert.NotNull(operation);
        Assert.IsType<DelegateWorkflowOperation>(operation);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullFunc()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DelegateWorkflowOperation("TestDelegate", null!));
    }

    [Fact]
    public void ReturnTypedDelegateOperation_GivenValidTypedFunc()
    {
        // Arrange
        Func<string, IWorkflowFoundry, CancellationToken, Task<int>> func =
            (input, foundry, cancellationToken) => Task.FromResult(42);

        // Act
        var operation = new DelegateWorkflowOperation<string, int>("TestTypedDelegate", func);

        // Assert
        Assert.NotNull(operation);
        Assert.IsType<DelegateWorkflowOperation<string, int>>(operation);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullTypedFunc()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DelegateWorkflowOperation<string, int>("TestTypedDelegate", null!));
    }

    [Fact]
    public void ReturnDelayOperation_GivenValidDuration()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(1);

        // Act
        var operation = new DelayOperation(duration);

        // Assert
        Assert.NotNull(operation);
        Assert.IsType<DelayOperation>(operation);
    }

    [Fact]
    public void ReturnDelayOperation_GivenZeroDuration()
    {
        // Arrange
        var duration = TimeSpan.Zero;

        // Act
        var operation = new DelayOperation(duration);

        // Assert
        Assert.NotNull(operation);
        Assert.IsType<DelayOperation>(operation);
    }

    [Fact]
    public void ThrowArgumentException_GivenNegativeDuration()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(-1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new DelayOperation(duration));
    }

    [Fact]
    public void ReturnConditionalOperation_GivenValidPredicate()
    {
        // Arrange
        Func<IWorkflowFoundry, bool> predicate = foundry => true;
        var trueOperation = new Mock<IWorkflowOperation>().Object;
        var falseOperation = new Mock<IWorkflowOperation>().Object;

        // Act
        var operation = new ConditionalWorkflowOperation((input, foundry) => predicate(foundry), trueOperation, falseOperation, "TestConditional");

        // Assert
        Assert.NotNull(operation);
        Assert.IsType<ConditionalWorkflowOperation>(operation);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullPredicate()
    {
        // Arrange
        var trueOperation = new Mock<IWorkflowOperation>().Object;
        var falseOperation = new Mock<IWorkflowOperation>().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ConditionalWorkflowOperation((Func<object?, IWorkflowFoundry, bool>)null!, trueOperation, falseOperation, "TestConditional"));
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullTrueOperation()
    {
        // Arrange
        Func<IWorkflowFoundry, bool> predicate = foundry => true;
        var falseOperation = new Mock<IWorkflowOperation>().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ConditionalWorkflowOperation((input, foundry) => predicate(foundry), null!, falseOperation, "TestConditional"));
    }

    [Fact]
    public void ReturnLoggingOperation_GivenValidMessage()
    {
        // Arrange
        const string message = "Test message";

        // Act
        var operation = LoggingOperation.Info(message);

        // Assert
        Assert.NotNull(operation);
        Assert.IsType<LoggingOperation>(operation);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullMessage()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => LoggingOperation.Info(null!));
    }

    [Fact]
    public void ReturnForEachOperation_GivenValidItems()
    {
        // Arrange
        var operations = new[] {
            new Mock<IWorkflowOperation>().Object,
            new Mock<IWorkflowOperation>().Object,
            new Mock<IWorkflowOperation>().Object
        };

        // Act
        var operation = new ForEachWorkflowOperation(operations, name: "TestForEach");

        // Assert
        Assert.NotNull(operation);
        Assert.IsType<ForEachWorkflowOperation>(operation);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullItems()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ForEachWorkflowOperation(null!, name: "TestForEach"));
    }

    [Fact]
    public void ThrowArgumentException_GivenNullProcessor()
    {
        // Arrange
        var operations = Array.Empty<IWorkflowOperation>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new ForEachWorkflowOperation(operations, name: "TestForEach"));
    }
}