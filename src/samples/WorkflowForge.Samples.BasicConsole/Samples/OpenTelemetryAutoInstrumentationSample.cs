using System.Diagnostics;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Observability.OpenTelemetry;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Shows the span-per-operation instrumentation that EnableOpenTelemetry registers, and how a
/// consumer subscribes to it. A real application registers the OpenTelemetry SDK and calls
/// AddSource(serviceName) / AddMeter(serviceName); this sample uses a plain ActivityListener so it
/// needs no SDK packages.
/// </summary>
public class OpenTelemetryAutoInstrumentationSample : ISample
{
    private const string ServiceName = "WorkflowForge.Samples.AutoInstrumentation";

    public string Name => "OpenTelemetry Auto-Instrumentation";
    public string Description => "One span per operation, nested under a workflow span, with no manual StartActivity calls";

    public async Task RunAsync()
    {
        Console.WriteLine("Demonstrating automatic OpenTelemetry instrumentation...");

        var spans = new List<Activity>();

        // A consumer would call .AddSource(ServiceName) on their TracerProvider instead.
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ServiceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = spans.Add
        };
        ActivitySource.AddActivityListener(listener);

        using var foundry = WorkflowForge.CreateFoundry("OpenTelemetryAutoInstrumentation");

        var enabled = foundry.EnableOpenTelemetry(new WorkflowForgeOpenTelemetryOptions
        {
            ServiceName = ServiceName,
            ServiceVersion = "2.2.0",
            EnableTracing = true,
            EnableMetrics = true,
            EnableSystemMetrics = false
        });

        Console.WriteLine($"   OpenTelemetry enabled: {enabled}");
        Console.WriteLine("   No StartActivity calls in the operations below - the middleware creates the spans.");

        var workflow = WorkflowForge.CreateWorkflow("OrderPipeline")
            .AddOperation("ValidateOrder", (_, _) => Task.CompletedTask)
            .AddOperation("ChargePayment", (_, ct) => Task.Delay(20, ct))
            .AddOperation("DispatchShipment", (_, _) => Task.CompletedTask)
            .Build();

        using var smith = WorkflowForge.CreateSmith();

        // The workflow-level middleware is what parents the operation spans.
        var workflowMiddleware = foundry.CreateOpenTelemetryWorkflowMiddleware();
        if (workflowMiddleware != null)
        {
            smith.AddWorkflowMiddleware(workflowMiddleware);
        }

        await smith.ForgeAsync(workflow, foundry);

        Console.WriteLine($"   Spans captured: {spans.Count}");
        foreach (var span in spans)
        {
            var parent = spans.FirstOrDefault(s => s.SpanId == span.ParentSpanId);
            var prefix = parent == null ? "   - " : "     - ";
            Console.WriteLine($"{prefix}{span.DisplayName} [{span.Status}]{(parent == null ? string.Empty : $" (child of {parent.DisplayName})")}");
        }

        // Disabling releases the ActivitySource and Meter the service owns.
        Console.WriteLine($"   OpenTelemetry disabled: {foundry.DisableOpenTelemetry()}");
    }
}
