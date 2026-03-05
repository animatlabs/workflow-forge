using WorkflowForge.Abstractions;

namespace WorkflowForge.Tests.LoggersTests;

public class WorkflowForgeLoggersShould
{
    [Fact]
    public void ReturnNonNullLogger_GivenNullProperty()
    {
        var logger = WorkflowForgeLoggers.Null;

        Assert.NotNull(logger);
        Assert.IsAssignableFrom<IWorkflowForgeLogger>(logger);
    }

    [Fact]
    public void ReturnSameInstance_GivenMultipleNullAccesses()
    {
        var logger1 = WorkflowForgeLoggers.Null;
        var logger2 = WorkflowForgeLoggers.Null;

        Assert.Same(logger1, logger2);
    }

    [Fact]
    public void ReturnNonNullLogger_GivenConsoleWithDefaultPrefix()
    {
        var logger = WorkflowForgeLoggers.Console();

        Assert.NotNull(logger);
        Assert.IsAssignableFrom<IWorkflowForgeLogger>(logger);
    }

    [Fact]
    public void ReturnNonNullLogger_GivenConsoleWithCustomPrefix()
    {
        var logger = WorkflowForgeLoggers.Console("CustomPrefix");

        Assert.NotNull(logger);
        Assert.IsAssignableFrom<IWorkflowForgeLogger>(logger);
    }

    [Fact]
    public void ReturnDifferentInstances_GivenMultipleConsoleCalls()
    {
        var logger1 = WorkflowForgeLoggers.Console();
        var logger2 = WorkflowForgeLoggers.Console();

        Assert.NotSame(logger1, logger2);
    }
}
