using System.Threading.Tasks;
using Xunit;

namespace WorkflowForge.Extensions.Observability.Performance.Tests;

public class PerformanceMonitoringResetShould
{
    [Fact]
    public async Task StartFromZero_WhenMonitoringIsReEnabled()
    {
        var workflow = WorkflowForge.CreateWorkflow("PerfReset")
            .AddOperation("one", (_, _) => Task.CompletedTask)
            .AddOperation("two", (_, _) => Task.CompletedTask)
            .Build();

        using var foundry = WorkflowForge.CreateFoundry("PerfReset");
        Assert.True(foundry.EnablePerformanceMonitoring());

        using var smith = WorkflowForge.CreateSmith();
        await smith.ForgeAsync(workflow, foundry);

        var first = foundry.GetPerformanceStatistics();
        Assert.NotNull(first);
        Assert.Equal(2, first!.TotalOperations);

        Assert.True(foundry.EnablePerformanceMonitoring());

        var second = foundry.GetPerformanceStatistics();
        Assert.NotNull(second);
        Assert.NotSame(first, second);
        Assert.Equal(0, second!.TotalOperations);
    }

    [Fact]
    public async Task StopRecording_WhenMonitoringIsDisabled()
    {
        var workflow = WorkflowForge.CreateWorkflow("PerfDisable")
            .AddOperation("one", (_, _) => Task.CompletedTask)
            .Build();

        using var foundry = WorkflowForge.CreateFoundry("PerfDisable");
        Assert.True(foundry.EnablePerformanceMonitoring());
        Assert.True(foundry.DisablePerformanceMonitoring());

        using var smith = WorkflowForge.CreateSmith();
        await smith.ForgeAsync(workflow, foundry);

        Assert.Null(foundry.GetPerformanceStatistics());
    }
}
