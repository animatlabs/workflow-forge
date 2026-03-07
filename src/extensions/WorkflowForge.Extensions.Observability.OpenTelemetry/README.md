# WorkflowForge.Extensions.Observability.OpenTelemetry

Distributed tracing extension for WorkflowForge with OpenTelemetry integration for comprehensive observability.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Observability.OpenTelemetry.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Observability.OpenTelemetry/)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=coverage)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)

## Dependency Isolation

**This extension internalizes OpenTelemetry with ILRepack.** This means:

- Reduced dependency conflicts for OpenTelemetry
- Public APIs stay WorkflowForge/BCL only
- Microsoft/System assemblies remain external

## Installation

```bash
dotnet add package WorkflowForge.Extensions.Observability.OpenTelemetry
```

**Requires**: .NET Standard 2.0 or later

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

## Key Features

- **Distributed Tracing**: Track workflows across services
- **Automatic Spans**: Operation-level span creation
- **Context Propagation**: W3C Trace Context support
- **Multiple Exporters**: Jaeger, Zipkin, Console, OTLP
- **Rich Metadata**: Operation names, durations, results
- **Full OpenTelemetry API**: Access entire ecosystem

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

See [Configuration Guide](../../../docs/core/configuration.md#opentelemetry-extension) for complete options.

## Host-Level Exporter Configuration

WorkflowForge emits `ActivitySource` events that any OpenTelemetry exporter can collect. Configure exporters at the host application level:

```csharp
// In your application startup (requires OpenTelemetry SDK packages)
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("WorkflowForge")
        .AddConsoleExporter()
        .AddOtlpExporter());
```

Supported exporters (via separate OpenTelemetry packages):
- **Jaeger**: `OpenTelemetry.Exporter.Jaeger`
- **Zipkin**: `OpenTelemetry.Exporter.Zipkin`
- **OTLP**: `OpenTelemetry.Exporter.OpenTelemetryProtocol`
- **Console**: `OpenTelemetry.Exporter.Console`

## Span Structure

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

## Custom Spans

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

## Context Propagation

WorkflowForge automatically propagates trace context across:
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

## Documentation

- **[Getting Started](../../../docs/getting-started/getting-started.md)**
- **[Configuration Guide](../../../docs/core/configuration.md#opentelemetry-extension)**
- **[Extensions Overview](../../../docs/extensions/index.md)**
- **[Sample 15: OpenTelemetry](../../samples/WorkflowForge.Samples.BasicConsole/README.md)**

---

