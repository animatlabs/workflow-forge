using System;

namespace WorkflowForge.Extensions.Observability.Performance.Tests;

public class OperationStatisticsShould
{
    [Fact]
    public void ExposeNameAndId_FromConstructor()
    {
        var stats = new OperationStatistics("MyOp", "op-id-1");

        Assert.Equal("MyOp", stats.OperationName);
        Assert.Equal("op-id-1", stats.OperationId);
    }

    [Fact]
    public void ThrowArgumentNullException_ForNullNameOrId()
    {
        Assert.Throws<ArgumentNullException>(() => new OperationStatistics(null!, "id"));
        Assert.Throws<ArgumentNullException>(() => new OperationStatistics("name", null!));
    }

    [Fact]
    public void ReturnZeroDefaults_WhenNoExecutionsRecorded()
    {
        var stats = new OperationStatistics("MyOp", "op-id-1");

        Assert.Equal(0, stats.ExecutionCount);
        Assert.Equal(0, stats.SuccessfulExecutions);
        Assert.Equal(0, stats.FailedExecutions);
        Assert.Equal(0.0, stats.SuccessRate);
        Assert.Equal(TimeSpan.Zero, stats.AverageExecutionTime);
        Assert.Equal(TimeSpan.Zero, stats.MinimumExecutionTime);
        Assert.Equal(TimeSpan.Zero, stats.MaximumExecutionTime);
        Assert.Equal(0, stats.TotalMemoryAllocated);
        Assert.Equal(0, stats.AverageMemoryPerExecution);
    }
}
