using WorkflowForge.Abstractions;
using WorkflowForge.Loggers;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates workflow-level middleware (smith-level) vs operation middleware.
/// </summary>
public class WorkflowMiddlewareSample : ISample
{
    public string Name => "Workflow Middleware";
    public string Description => "Shows workflow-level middleware wrapping execution";

    public async Task RunAsync()
    {
        Console.WriteLine("Demonstrating workflow-level middleware...");

        var smith = WorkflowForge.CreateSmith(new ConsoleLogger("WF-MW"));
        smith.AddWorkflowMiddleware(new WorkflowTimingMiddleware());
        smith.AddWorkflowMiddleware(new WorkflowAuditMiddleware());

        var workflow = WorkflowForge.CreateWorkflow("WorkflowMiddlewareDemo")
            .AddOperation(new StepOperation("StepA"))
            .AddOperation(new StepOperation("StepB"))
            .Build();

        await smith.ForgeAsync(workflow);

        Console.WriteLine("Workflow middleware demo completed.");
    }

    private sealed class WorkflowTimingMiddleware : IWorkflowMiddleware
    {
        public async Task ExecuteAsync(IWorkflow workflow, IWorkflowFoundry foundry, Func<Task> next, CancellationToken cancellationToken)
        {
            var start = DateTimeOffset.UtcNow;
            foundry.Logger.LogInformation($"[WorkflowTiming] Starting {workflow.Name}");
            await next().ConfigureAwait(false);
            var duration = DateTimeOffset.UtcNow - start;
            foundry.Logger.LogInformation($"[WorkflowTiming] Completed {workflow.Name} in {duration.TotalMilliseconds:F0}ms");
        }
    }

    private sealed class WorkflowAuditMiddleware : IWorkflowMiddleware
    {
        public async Task ExecuteAsync(IWorkflow workflow, IWorkflowFoundry foundry, Func<Task> next, CancellationToken cancellationToken)
        {
            foundry.Logger.LogInformation($"[WorkflowAudit] Audit start for {workflow.Name}");
            await next().ConfigureAwait(false);
            foundry.Logger.LogInformation($"[WorkflowAudit] Audit end for {workflow.Name}");
        }
    }

    private sealed class StepOperation : IWorkflowOperation
    {
        public StepOperation(string name) => Name = name;

        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; }
        public bool SupportsRestore => false;

        public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            foundry.Logger.LogInformation($"Executing {Name}");
            await Task.Delay(50, cancellationToken);
            return inputData;
        }

        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public void Dispose()
        { }
    }
}
