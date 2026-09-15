using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Operations;
using WorkflowForge.Testing;

namespace WorkflowForge.Tests.OperationsTests;

/// <summary>
/// The operation builds an eight-entry property dictionary and a scope purely to carry its own
/// message, so both are skipped when the configured level is disabled.
/// </summary>
public class LoggingOperationLevelGateShould
{
    [Theory]
    [InlineData(WorkflowForgeLogLevel.Trace)]
    [InlineData(WorkflowForgeLogLevel.Debug)]
    [InlineData(WorkflowForgeLogLevel.Information)]
    [InlineData(WorkflowForgeLogLevel.Warning)]
    [InlineData(WorkflowForgeLogLevel.Error)]
    [InlineData(WorkflowForgeLogLevel.Critical)]
    public async Task EmitAtTheRequestedLevel_WhenThatLevelIsEnabled(WorkflowForgeLogLevel level)
    {
        var logger = new RecordingLogger { EnabledFrom = WorkflowForgeLogLevel.Trace };
        using var foundry = new FakeWorkflowFoundry { Logger = logger };
        var operation = new LoggingOperation("hello", level);

        var result = await operation.ForgeAsync("input", foundry, CancellationToken.None);

        Assert.Equal("input", result);
        Assert.Single(logger.At(level));
        Assert.Equal("hello", logger.At(level).Single().Message);
        Assert.Contains("LoggingOperation", logger.Scopes);
    }

    [Fact]
    public async Task EmitNothingAndOpenNoScope_WhenTheLevelIsDisabled()
    {
        var logger = new RecordingLogger { EnabledFrom = WorkflowForgeLogLevel.Error };
        using var foundry = new FakeWorkflowFoundry { Logger = logger };
        var operation = LoggingOperation.Debug("suppressed");

        var result = await operation.ForgeAsync("input", foundry, CancellationToken.None);

        Assert.Equal("input", result);
        Assert.Empty(logger.Entries);
        Assert.Empty(logger.Scopes);
    }

    [Fact]
    public async Task NotBuildTheDebugPayload_WhenDebugIsDisabledOnADelay()
    {
        var logger = new RecordingLogger { EnabledFrom = WorkflowForgeLogLevel.Warning };
        using var foundry = new FakeWorkflowFoundry { Logger = logger };
        var operation = DelayOperation.FromMilliseconds(1);

        var result = await operation.ForgeAsync("input", foundry, CancellationToken.None);

        Assert.Equal("input", result);
        Assert.Empty(logger.At(WorkflowForgeLogLevel.Debug));
    }

    [Fact]
    public async Task LogStartAndCompletion_WhenDebugIsEnabledOnADelay()
    {
        var logger = new RecordingLogger { EnabledFrom = WorkflowForgeLogLevel.Trace };
        using var foundry = new FakeWorkflowFoundry { Logger = logger };
        var operation = DelayOperation.FromMilliseconds(1);

        await operation.ForgeAsync("input", foundry, CancellationToken.None);

        Assert.Equal(2, logger.At(WorkflowForgeLogLevel.Debug).Count());
    }
}
