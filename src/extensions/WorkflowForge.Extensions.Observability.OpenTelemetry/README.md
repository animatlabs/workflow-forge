# WorkflowForge.Extensions.Observability.OpenTelemetry

Emit `ActivitySource` traces and `Meter` metrics for workflows and operations. Your application owns the OpenTelemetry SDK; this package depends only on `System.Diagnostics.DiagnosticSource`.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Observability.OpenTelemetry.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Observability.OpenTelemetry/)

## Install

```bash
dotnet add package WorkflowForge.Extensions.Observability.OpenTelemetry
```

Targets .NET Standard 2.0 or later.

## Quick Start

```csharp
using WorkflowForge;
using WorkflowForge.Extensions.Observability.OpenTelemetry;

// Create foundry and enable OpenTelemetry
using var foundry = WorkflowForge.CreateFoundry("TracedWorkflow");
foundry.EnableOpenTelemetry(new WorkflowForgeOpenTelemetryOptions
{
    ServiceName = "OrderService",
    EnableTracing = true,
    EnableMetrics = true
});

using var smith = WorkflowForge.CreateSmith();

// Optional: one workflow span that the per-operation spans nest under.
var workflowMiddleware = foundry.CreateOpenTelemetryWorkflowMiddleware();
if (workflowMiddleware != null)
{
    smith.AddWorkflowMiddleware(workflowMiddleware);
}

var workflow = WorkflowForge.CreateWorkflow("TracedWorkflow")
    .AddOperation("ProcessOrder", (foundry, ct) => Task.CompletedTask)
    .Build();

// EnableOpenTelemetry registered middleware that creates a span and records
// metrics for every operation - no instrumentation code in the operations.
await smith.ForgeAsync(workflow, foundry);
```

## Key points

- `EnableOpenTelemetry` registers middleware that creates one span per operation automatically.
- Add `CreateOpenTelemetryWorkflowMiddleware()` to the smith for a parent workflow span.
- Tags cover operation and workflow names, execution id, and success or failure.
- Spans and metrics are published under the `ServiceName` you configure. Subscribe with
  `.AddSource(serviceName)` and `.AddMeter(serviceName)`.
- `EnableSystemMetrics` adds process memory, GC and thread-pool gauges; `EnableOperationMetrics`
  controls the per-operation counters and histograms.
- Public API is WorkflowForge and BCL only. No OpenTelemetry SDK package is pulled in.

## Configuration

### Programmatic

```csharp
using var foundry = WorkflowForge.CreateFoundry("TracedWorkflow");

// Enable with custom options
foundry.EnableOpenTelemetry(new WorkflowForgeOpenTelemetryOptions
{
    ServiceName = "MyService",
    EnableTracing = true,
    EnableMetrics = true
});

// The extension uses System.Diagnostics.ActivitySource internally.
// Any host-level OpenTelemetry SDK configuration will automatically
// collect activities emitted by WorkflowForge.
```

[OpenTelemetry extension options](https://animatlabs.com/workflow-forge/core/configuration/#opentelemetry-extension)

## Host-level exporter configuration

WorkflowForge emits `ActivitySource` events that any OpenTelemetry exporter can collect. Configure exporters in the host:

```csharp
// In your application startup (requires OpenTelemetry SDK packages).
// The source and meter names are the ServiceName you passed to EnableOpenTelemetry.
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("OrderService")
        .AddConsoleExporter()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddMeter("OrderService")
        .AddOtlpExporter());
```

Supported exporters (separate OpenTelemetry packages):

- **Jaeger**: `OpenTelemetry.Exporter.Jaeger`
- **Zipkin**: `OpenTelemetry.Exporter.Zipkin`
- **OTLP**: `OpenTelemetry.Exporter.OpenTelemetryProtocol`
- **Console**: `OpenTelemetry.Exporter.Console`

## Span structure

With `CreateOpenTelemetryWorkflowMiddleware()` registered on the smith, operation spans nest under
the workflow span:

```
OrderProcessing
  ├─ ValidateOrder
  ├─ ChargePayment
  ├─ ReserveInventory
  └─ CreateShipment
```

Without it, operation spans are still created and nest under whatever `Activity.Current` your
application has open.

Each operation span carries:

- `workflowforge.operation.id` and `workflowforge.operation.name`
- `workflowforge.workflow.name` and `workflowforge.execution.id`
- `ActivityStatusCode.Ok` or `Error`, plus `exception.type` on failure

## Custom spans

```csharp
using var activity = foundry.StartActivity("CustomOperation");
activity?.SetTag("order.id", orderId);
activity?.SetTag("customer.id", customerId);

try
{
    // ... operation logic ...
    activity?.SetStatus(ActivityStatusCode.Ok);
}
catch (Exception ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    throw;
}
```

## Context propagation

Spans are created through `ActivitySource`, so they participate in the ambient `Activity.Current`
chain: operations nest under the workflow span, and the workflow span nests under whatever your
application already has open. Propagation across process boundaries - HTTP headers, queue metadata -
is handled by the OpenTelemetry SDK and the relevant instrumentation packages in your application,
not by this package.

## Visualization

View traces in:

- **Jaeger UI**: http://localhost:16686
- **Zipkin UI**: http://localhost:9411
- **Application Insights**: Azure Portal
- **Grafana Tempo**: Grafana dashboard

## Links

- [Getting Started](https://animatlabs.com/workflow-forge/getting-started/getting-started/)
- [Configuration Guide](https://animatlabs.com/workflow-forge/core/configuration/#opentelemetry-extension)
- [Extensions Overview](https://animatlabs.com/workflow-forge/extensions/)
- [Sample 15: OpenTelemetry](https://github.com/animatlabs/workflow-forge/blob/main/src/samples/WorkflowForge.Samples.BasicConsole/README.md)
