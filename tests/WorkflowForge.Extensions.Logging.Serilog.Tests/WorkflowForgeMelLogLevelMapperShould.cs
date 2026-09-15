using Microsoft.Extensions.Logging;
using WorkflowForge.Extensions.Logging.Serilog;
using WorkflowForge.Operations;

namespace WorkflowForge.Extensions.Logging.Serilog.Tests;

public class WorkflowForgeMelLogLevelMapperShould
{
    [Theory]
    [InlineData(WorkflowForgeLogLevel.Trace, LogLevel.Trace)]
    [InlineData(WorkflowForgeLogLevel.Debug, LogLevel.Debug)]
    [InlineData(WorkflowForgeLogLevel.Information, LogLevel.Information)]
    [InlineData(WorkflowForgeLogLevel.Warning, LogLevel.Warning)]
    [InlineData(WorkflowForgeLogLevel.Error, LogLevel.Error)]
    [InlineData(WorkflowForgeLogLevel.Critical, LogLevel.Critical)]
    public void MapToMelLevel_GivenKnownWorkflowForgeLevel(WorkflowForgeLogLevel level, LogLevel expected)
    {
        Assert.Equal(expected, WorkflowForgeMelLogLevelMapper.ToMelLevel(level));
    }

    [Fact]
    public void MapToInformation_GivenUndefinedWorkflowForgeLevel()
    {
        var undefined = (WorkflowForgeLogLevel)999;
        Assert.Equal(LogLevel.Information, WorkflowForgeMelLogLevelMapper.ToMelLevel(undefined));
    }
}
