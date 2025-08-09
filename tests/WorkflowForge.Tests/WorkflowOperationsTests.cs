using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Operations;

namespace WorkflowForge.Tests;

public class WorkflowOperationsTests
{
    [Fact]
    public void Action_WithValidAction_ReturnsActionOperation()
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
    public void Action_WithNullAction_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ActionWorkflowOperation("TestAction", null!));
    }

    [Fact]
    public void Action_WithValidTypedAction_ReturnsTypedActionOperation()
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
    public void Action_WithNullTypedAction_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ActionWorkflowOperation("TestTypedAction", null!));
    }

    [Fact]
    public void Delegate_WithValidFunc_ReturnsDelegateOperation()
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
    public void Delegate_WithNullFunc_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DelegateWorkflowOperation("TestDelegate", null!));
    }

    [Fact]
    public void Delegate_WithValidTypedFunc_ReturnsTypedDelegateOperation()
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
    public void Delegate_WithNullTypedFunc_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DelegateWorkflowOperation<string, int>("TestTypedDelegate", null!));
    }

    [Fact]
    public void Delay_WithValidDuration_ReturnsDelayOperation()
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
    public void Delay_WithZeroDuration_ReturnsDelayOperation()
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
    public void Delay_WithNegativeDuration_ThrowsArgumentException()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(-1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new DelayOperation(duration));
    }

    [Fact]
    public void Conditional_WithValidPredicate_ReturnsConditionalOperation()
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
    public void Conditional_WithNullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        var trueOperation = new Mock<IWorkflowOperation>().Object;
        var falseOperation = new Mock<IWorkflowOperation>().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ConditionalWorkflowOperation((Func<object?, IWorkflowFoundry, bool>)null!, trueOperation, falseOperation, "TestConditional"));
    }

    [Fact]
    public void Conditional_WithNullTrueOperation_ThrowsArgumentNullException()
    {
        // Arrange
        Func<IWorkflowFoundry, bool> predicate = foundry => true;
        var falseOperation = new Mock<IWorkflowOperation>().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ConditionalWorkflowOperation((input, foundry) => predicate(foundry), null!, falseOperation, "TestConditional"));
    }

    [Fact]
    public void Log_WithValidMessage_ReturnsLoggingOperation()
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
    public void Log_WithNullMessage_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => LoggingOperation.Info(null!));
    }

    [Fact]
    public void ForEach_WithValidItems_ReturnsForEachOperation()
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
    public void ForEach_WithNullItems_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ForEachWorkflowOperation(null!, name: "TestForEach"));
    }

    [Fact]
    public void ForEach_WithNullProcessor_ThrowsArgumentNullException()
    {
        // Arrange
        var operations = Array.Empty<IWorkflowOperation>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new ForEachWorkflowOperation(operations, name: "TestForEach"));
    }
}