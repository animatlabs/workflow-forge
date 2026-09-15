using WorkflowForge.Extensions.Logging.Serilog;
using WorkflowForge.Operations;

namespace WorkflowForge.Extensions.Logging.Serilog.Tests;

public class WorkflowForgeSerilogLogLevelMapperShould
{
    [Theory]
    [InlineData(WorkflowForgeLogLevel.Trace, 0)]
    [InlineData(WorkflowForgeLogLevel.Debug, 1)]
    [InlineData(WorkflowForgeLogLevel.Information, 2)]
    [InlineData(WorkflowForgeLogLevel.Warning, 3)]
    [InlineData(WorkflowForgeLogLevel.Error, 4)]
    [InlineData(WorkflowForgeLogLevel.Critical, 5)]
    public void MapToSerilogLevel_GivenKnownWorkflowForgeLevel(WorkflowForgeLogLevel level, int expectedOrdinal)
    {
        var mapped = WorkflowForgeSerilogLogLevelMapper.ToSerilogLevel(level);
        Assert.Equal(expectedOrdinal, (int)mapped);
    }

    [Fact]
    public void MapToInformation_GivenUndefinedWorkflowForgeLevel()
    {
        var undefined = (WorkflowForgeLogLevel)999;
        Assert.Equal(2, (int)WorkflowForgeSerilogLogLevelMapper.ToSerilogLevel(undefined));
    }
}
