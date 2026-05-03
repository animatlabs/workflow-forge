# WorkflowForge.Extensions.Observability.OpenTelemetry

Emit `ActivitySource` traces and metrics for workflows and operations; OpenTelemetry bits are ILRepacked so apps see fewer dependency conflicts.

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

var smith = WorkflowForge.CreateSmith();
var workflow = WorkflowForge.CreateWorkflow("TracedWorkflow")
    .AddOperation(new ActionWorkflowOperation("ProcessOrder", async (input, foundry, ct) => { /* ... */ }))
    .Build();

// Operations will create spans and record metrics
await smith.ForgeAsync(workflow, foundry);

// Access the OpenTelemetry service for custom instrumentation
var otelService = foundry.GetOpenTelemetryService();
using var activity = foundry.StartActivity("CustomOperation");
```

## Key points

- One span per operation by default; W3C Trace Context for propagation.
- Tags cover names, durations, and success or failure.
- Host-level OpenTelemetry SDK exporters (Jaeger, Zipkin, OTLP, console, etc.) pick up `WorkflowForge` activities.
- Public API remains WorkflowForge and BCL; OTEL is merged internally.

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

[OpenTelemetry extension options](../../../docs/core/configuration.md#opentelemetry-extension)

## Host-level exporter configuration

WorkflowForge emits `ActivitySource` events that any OpenTelemetry exporter can collect. Configure exporters in the host:

```csharp
// In your application startup (requires OpenTelemetry SDK packages)
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("WorkflowForge")
        .AddConsoleExporter()
        .AddOtlpExporter());
```

Supported exporters (separate OpenTelemetry packages):

- **Jaeger**: `OpenTelemetry.Exporter.Jaeger`
- **Zipkin**: `OpenTelemetry.Exporter.Zipkin`
- **OTLP**: `OpenTelemetry.Exporter.OpenTelemetryProtocol`
- **Console**: `OpenTelemetry.Exporter.Console`

## Span structure

WorkflowForge creates the following span hierarchy:

```
Workflow: OrderProcessing
  ├─ Operation: ValidateOrder (12µs)
  ├─ Operation: ChargePayment (145µs)
  ├─ Operation: ReserveInventory (87µs)
  └─ Operation: CreateShipment (234µs)
```

Each span includes:

- Operation name
- Duration
- Success/failure status
- Custom tags (workflow properties)
- Error details (if failed)

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

WorkflowForge propagates trace context across:

- Operations within a workflow
- Nested workflows
- HTTP calls (with propagation headers)
- Message queues (with context metadata)

## Visualization

View traces in:

- **Jaeger UI**: http://localhost:16686
- **Zipkin UI**: http://localhost:9411
- **Application Insights**: Azure Portal
- **Grafana Tempo**: Grafana dashboard

## Links

- [Getting Started](../../../docs/getting-started/getting-started.md)
- [Configuration Guide](../../../docs/core/configuration.md#opentelemetry-extension)
- [Extensions Overview](../../../docs/extensions/index.md)
- [Sample 15: OpenTelemetry](../../samples/WorkflowForge.Samples.BasicConsole/README.md)
