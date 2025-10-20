# WorkflowForge.Extensions.Observability.OpenTelemetry

Advanced distributed tracing and observability extension for WorkflowForge using OpenTelemetry standards.

## Installation

```bash
dotnet add package WorkflowForge.Extensions.Observability.OpenTelemetry
```

## Zero Version Conflicts

**This extension uses Costura.Fody to embed OpenTelemetry dependencies.** This means:

- **NO DLL Hell** - Use ANY version of OpenTelemetry in your project  
- **NO Conflicts** - Your app's OpenTelemetry version won't clash with this extension  
- **Clean Deployment** - Professional-grade dependency isolation

**Example**: Your app uses OpenTelemetry 1.10.x, this extension uses 1.9.x - both coexist perfectly with zero conflicts!

**How it works**: OpenTelemetry libraries are embedded as compressed resources at build time and loaded automatically at runtime, completely isolated from your application's dependencies.

## Quick Start

```csharp
using WorkflowForge.Extensions.Observability.OpenTelemetry;

// Enable OpenTelemetry in foundry
var foundryConfig = FoundryConfiguration.ForProduction()
    .EnableOpenTelemetry("MyWorkflowService", "1.0.0");

var foundry = WorkflowForge.CreateFoundry("OrderProcessing", foundryConfig);

// Configure OpenTelemetry SDK in startup
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("MyWorkflowService") // Match your service name
        .AddJaegerExporter()
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddMeter("MyWorkflowService") // Match your service name
        .AddPrometheusExporter());
```

## Key Features

- **Distributed Tracing**: Full distributed tracing using OpenTelemetry Activity API
- **Metrics Collection**: Comprehensive metrics using System.Diagnostics.Metrics
- **Foundry Integration**: Deep integration with WorkflowForge foundries and operations
- **Standard Protocols**: OTLP, Jaeger, Zipkin, Prometheus compatibility
- **High Performance**: Built on .NET's native observability APIs

## Distributed Tracing in Operations

```csharp
public class OrderProcessingOperation : IWorkflowOperation
{
    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        // Start distributed tracing with context
        using var activity = foundry.StartActivity("ProcessOrder", ActivityKind.Server);
        
        var order = (Order)inputData!;
        
        // Add rich trace context
        activity?.SetTag("order.id", order.Id);
        activity?.SetTag("order.amount", order.Amount);
        activity?.SetTag("customer.id", order.CustomerId);
        
        try
        {
            var result = await ProcessOrderInternalAsync(order, cancellationToken);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
```

## Built-in Metrics

```csharp
// Automatically collected operation metrics:
// - workflowforge.operations.total (counter)
// - workflowforge.operations.duration (histogram)
// - workflowforge.operations.errors.total (counter)
// - workflowforge.operations.active (updowncounter)

// System metrics:
// - workflowforge.process.memory.usage (gauge)
// - workflowforge.process.gc.collections.total (counter)
// - workflowforge.foundries.active (gauge)
```

## Environment Configurations

```csharp
// Development - verbose tracing
var devConfig = FoundryConfiguration.ForDevelopment()
    .EnableOpenTelemetry("MyService", "1.0.0", options =>
    {
        options.SampleRate = 1.0; // Trace everything
        options.EnableDetailedLogging = true;
    });

// Production - optimized performance
var prodConfig = FoundryConfiguration.ForProduction()
    .EnableOpenTelemetry("MyService", "1.0.0", options =>
    {
        options.SampleRate = 0.1; // Sample 10%
        options.EnableSystemMetrics = false;
    });
```

## Examples & Documentation

- **[Complete Examples](../../samples/WorkflowForge.Samples.BasicConsole/README.md#15-opentelemetry)** - Interactive OpenTelemetry samples
- **[Core Documentation](../../core/WorkflowForge/README.md)** - Core concepts
- **[Performance Extension](../WorkflowForge.Extensions.Observability.Performance/README.md)** - Performance monitoring
- **[Main README](../../../README.md)** - Framework overview
- **[OpenTelemetry Documentation](https://opentelemetry.io/docs/)** - OpenTelemetry official docs

---

*Professional distributed observability for workflows* 