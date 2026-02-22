using WorkflowForge.Abstractions;
using WorkflowForge.Loggers;
using WorkflowForge.Operations;

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
            foundry.Logger.LogInformation("[WorkflowTiming] Starting {WorkflowName}", workflow.Name);
            await next().ConfigureAwait(false);
            var duration = DateTimeOffset.UtcNow - start;
            foundry.Logger.LogInformation("[WorkflowTiming] Completed {WorkflowName} in {DurationMs}ms", workflow.Name, duration.TotalMilliseconds.ToString("F0"));
        }
    }

    private sealed class WorkflowAuditMiddleware : IWorkflowMiddleware
    {
        public async Task ExecuteAsync(IWorkflow workflow, IWorkflowFoundry foundry, Func<Task> next, CancellationToken cancellationToken)
        {
            foundry.Logger.LogInformation("[WorkflowAudit] Audit start for {WorkflowName}", workflow.Name);
            await next().ConfigureAwait(false);
            foundry.Logger.LogInformation("[WorkflowAudit] Audit end for {WorkflowName}", workflow.Name);
        }
    }

    private sealed class StepOperation : WorkflowOperationBase
    {
        public StepOperation(string name) => Name = name;

        public override string Name { get; }

        protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            foundry.Logger.LogInformation("Executing {OperationName}", Name);
            await Task.Delay(50, cancellationToken);
            return inputData;
        }
    }
}