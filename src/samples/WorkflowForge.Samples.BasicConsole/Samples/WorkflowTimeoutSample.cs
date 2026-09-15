using System.Diagnostics;
using WorkflowForge.Middleware;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Shows workflow-level timeout, which bounds the whole run rather than a single operation.
/// Contrast with CancellationAndTimeoutSample, which bounds individual operations.
/// </summary>
public class WorkflowTimeoutSample : ISample
{
    public string Name => "Workflow Timeout";
    public string Description => "Bounding the total run time of a workflow with WorkflowTimeoutMiddleware";

    public async Task RunAsync()
    {
        Console.WriteLine("Demonstrating workflow-level timeout...");

        await RunWithTimeoutAsync(TimeSpan.FromSeconds(2), "Generous timeout");
        await RunWithTimeoutAsync(TimeSpan.FromMilliseconds(250), "Tight timeout");
    }

    private static async Task RunWithTimeoutAsync(TimeSpan timeout, string label)
    {
        Console.WriteLine($"\n--- {label} ({timeout.TotalMilliseconds:F0}ms) ---");

        using var foundry = WorkflowForge.CreateFoundry("WorkflowTimeoutDemo");
        using var smith = WorkflowForge.CreateSmith();

        smith.AddWorkflowMiddleware(new WorkflowTimeoutMiddleware(timeout, foundry.Logger));

        var workflow = WorkflowForge.CreateWorkflow("SlowPipeline")
            .AddOperation("Step1", async (_, ct) => await Task.Delay(200, ct))
            .AddOperation("Step2", async (_, ct) => await Task.Delay(200, ct))
            .AddOperation("Step3", async (_, ct) => await Task.Delay(200, ct))
            .Build();

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await smith.ForgeAsync(workflow, foundry);
            Console.WriteLine($"   Completed in {stopwatch.ElapsedMilliseconds}ms");
        }
        catch (TimeoutException ex)
        {
            Console.WriteLine($"   Timed out after {stopwatch.ElapsedMilliseconds}ms: {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"   Cancelled by the timeout after {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}
