# WorkflowForge.Extensions.Observability.OpenTelemetry

<p align="center">
  <img src="https://raw.githubusercontent.com/animatlabs/workflow-forge/main/icon.png" alt="WorkflowForge" width="120" height="120">
</p>

Distributed tracing extension for WorkflowForge with OpenTelemetry integration for comprehensive observability.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Observability.OpenTelemetry.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Observability.OpenTelemetry/)

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
using WorkflowForge.Extensions.Observability.OpenTelemetry;
using OpenTelemetry;
using OpenTelemetry.Trace;

// Configure OpenTelemetry
var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource("WorkflowForge")
    .AddConsoleExporter()
    .AddJaegerExporter(options =>
    {
        options.AgentHost = "localhost";
        options.AgentPort = 6831;
    })
    .Build();

// Create foundry with tracing
using var foundry = WorkflowForge.CreateFoundry("TracedWorkflow");
var tracer = tracerProvider.GetTracer("WorkflowForge");

// Operations will create spans
await smith.ForgeAsync(workflow, foundry);
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
var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource("WorkflowForge")
    .AddConsoleExporter()
    .AddJaegerExporter(options =>
    {
        options.AgentHost = "localhost";
        options.AgentPort = 6831;
    })
    .Build();

using var foundry = WorkflowForge.CreateFoundry("TracedWorkflow");
var tracer = tracerProvider.GetTracer("WorkflowForge");

await smith.ForgeAsync(workflow, foundry);
```

See [Configuration Guide](../../../docs/core/configuration.md#opentelemetry-extension) for complete options.

## Exporters

### Jaeger

```csharp
.AddJaegerExporter(options =>
{
    options.AgentHost = "localhost";
    options.AgentPort = 6831;
})
```

### Zipkin

```csharp
.AddZipkinExporter(options =>
{
    options.Endpoint = new Uri("http://localhost:9411/api/v2/spans");
})
```

### OTLP (OpenTelemetry Protocol)

```csharp
.AddOtlpExporter(options =>
{
    options.Endpoint = new Uri("http://localhost:4317");
})
```

### Console (Development)

```csharp
.AddConsoleExporter()
```

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
using var span = tracer.StartActiveSpan("CustomOperation");
span.SetAttribute("order.id", orderId);
span.SetAttribute("customer.id", customerId);

try
{
    // ... operation logic ...
    span.SetStatus(Status.Ok);
}
catch (Exception ex)
{
    span.SetStatus(Status.Error);
    span.RecordException(ex);
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

**WorkflowForge.Extensions.Observability.OpenTelemetry** - *Build workflows with industrial strength*
