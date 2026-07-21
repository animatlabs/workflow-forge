using System;
using System.Linq;
using System.Threading.Tasks;

namespace WorkflowForge.Extensions.Observability.Performance.Tests;

public class FoundryPerformanceStatisticsShould
{
    [Fact]
    public void ReturnZeroDefaults_WhenNothingRecorded()
    {
        var stats = new FoundryPerformanceStatistics();

        Assert.Equal(0, stats.TotalOperations);
        Assert.Equal(0, stats.SuccessfulOperations);
        Assert.Equal(0, stats.FailedOperations);
        Assert.Equal(0.0, stats.SuccessRate);
        Assert.Equal(TimeSpan.Zero, stats.AverageDuration);
        Assert.Equal(TimeSpan.Zero, stats.MinimumDuration);
        Assert.Equal(TimeSpan.Zero, stats.MaximumDuration);
        Assert.Equal(0, stats.TotalMemoryAllocated);
        Assert.Equal(0, stats.AverageMemoryPerOperation);
        Assert.Equal(0.0, stats.OperationsPerSecond);
        Assert.True(stats.TotalDuration >= TimeSpan.Zero);
        Assert.Empty(stats.GetAllOperationStatistics());
        Assert.Null(stats.GetOperationStatistics("missing"));
    }

    [Fact]
    public void AggregateCountsAndRates_AcrossMixedRecords()
    {
        var stats = new FoundryPerformanceStatistics();

        stats.Record("A", "id-a", TimeSpan.FromMilliseconds(10), success: true, memoryAllocated: 100);
        stats.Record("B", "id-b", TimeSpan.FromMilliseconds(20), success: true, memoryAllocated: 200);
        stats.Record("C", "id-c", TimeSpan.FromMilliseconds(30), success: false, memoryAllocated: 300);

        Assert.Equal(3, stats.TotalOperations);
        Assert.Equal(2, stats.SuccessfulOperations);
        Assert.Equal(1, stats.FailedOperations);
        Assert.Equal(2.0 / 3.0, stats.SuccessRate, 5);
        Assert.Equal(TimeSpan.FromMilliseconds(20), stats.AverageDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(10), stats.MinimumDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(30), stats.MaximumDuration);
        Assert.Equal(600, stats.TotalMemoryAllocated);
        Assert.Equal(200, stats.AverageMemoryPerOperation);
        Assert.Equal(3, stats.GetAllOperationStatistics().Count);
    }

    [Fact]
    public void AggregatePerOperation_WhenSameOperationRecordedMultipleTimes()
    {
        var stats = new FoundryPerformanceStatistics();

        stats.Record("A", "id-a", TimeSpan.FromMilliseconds(10), success: true, memoryAllocated: 100);
        stats.Record("A", "id-a", TimeSpan.FromMilliseconds(30), success: false, memoryAllocated: 300);
        stats.Record("B", "id-b", TimeSpan.FromMilliseconds(50), success: true, memoryAllocated: 500);

        Assert.Equal(2, stats.GetAllOperationStatistics().Count);

        var a = stats.GetOperationStatistics("A");
        Assert.NotNull(a);
        Assert.Equal(2, a!.ExecutionCount);
        Assert.Equal(1, a.SuccessfulExecutions);
        Assert.Equal(1, a.FailedExecutions);
        Assert.Equal(0.5, a.SuccessRate);
        Assert.Equal(TimeSpan.FromMilliseconds(20), a.AverageExecutionTime);
        Assert.Equal(TimeSpan.FromMilliseconds(10), a.MinimumExecutionTime);
        Assert.Equal(TimeSpan.FromMilliseconds(30), a.MaximumExecutionTime);
        Assert.Equal(400, a.TotalMemoryAllocated);
        Assert.Equal(200, a.AverageMemoryPerExecution);

        Assert.Null(stats.GetOperationStatistics("does-not-exist"));
    }

    [Fact]
    public void ClampNegativeDurationAndMemory_ToZero()
    {
        var stats = new FoundryPerformanceStatistics();

        stats.Record("A", "id-a", TimeSpan.FromTicks(-100), success: true, memoryAllocated: -50);

        Assert.Equal(1, stats.TotalOperations);
        Assert.Equal(TimeSpan.Zero, stats.MinimumDuration);
        Assert.Equal(TimeSpan.Zero, stats.MaximumDuration);
        Assert.Equal(0, stats.TotalMemoryAllocated);
    }

    [Fact]
    public void ThrowArgumentNullException_ForNullOperationNameOrId()
    {
        var stats = new FoundryPerformanceStatistics();

        Assert.Throws<ArgumentNullException>(() => stats.Record(null!, "id", TimeSpan.Zero, true, 0));
        Assert.Throws<ArgumentNullException>(() => stats.Record("name", null!, TimeSpan.Zero, true, 0));
        Assert.Throws<ArgumentNullException>(() => stats.GetOperationStatistics(null!));
    }

    [Fact]
    public void RecordConsistently_UnderConcurrentAccess()
    {
        var stats = new FoundryPerformanceStatistics();

        Parallel.For(0, 1000, i =>
            stats.Record("Op", "id", TimeSpan.FromMilliseconds(1), success: i % 2 == 0, memoryAllocated: 10));

        Assert.Equal(1000, stats.TotalOperations);
        Assert.Equal(500, stats.SuccessfulOperations);
        Assert.Equal(500, stats.FailedOperations);
        Assert.Equal(10_000, stats.TotalMemoryAllocated);

        var op = stats.GetOperationStatistics("Op");
        Assert.NotNull(op);
        Assert.Equal(1000, op!.ExecutionCount);
    }
}
