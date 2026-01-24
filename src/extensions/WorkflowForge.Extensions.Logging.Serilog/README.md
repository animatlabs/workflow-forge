# WorkflowForge.Extensions.Logging.Serilog

<p align="center">
  <img src="../../../icon.png" alt="WorkflowForge" width="120" height="120">
</p>

Structured logging extension for WorkflowForge with Serilog integration for rich, queryable logs.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Logging.Serilog.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Logging.Serilog/)

## Zero Version Conflicts

**This extension uses Costura.Fody to embed Serilog.** This means:

- NO DLL Hell - No conflicts with your application's Serilog version
- NO Version Conflicts - Works with ANY version of Serilog in your app
- Clean Deployment - Professional dependency isolation

**How it works**: Serilog is embedded as compressed resources at build time and loaded at runtime, completely isolated from your application.

## Installation

```bash
dotnet add package WorkflowForge.Extensions.Logging.Serilog
```

**Requires**: .NET Standard 2.0 or later

## Quick Start

```csharp
using WorkflowForge.Extensions.Logging.Serilog;
using Serilog;

// Configure Serilog logger
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/workflow-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Create foundry with Serilog
using var foundry = WorkflowForge.CreateFoundry("MyWorkflow");
foundry.UseSerilogLogger(Log.Logger);

// Or use extension method
var logger = foundry.CreateSerilogLogger();
```

## Key Features

- **Structured Logging**: Rich, queryable log data
- **Multiple Sinks**: Console, File, Elasticsearch, Seq, etc.
- **Contextual Properties**: Automatic workflow/operation context
- **Log Levels**: Fine-grained control (Verbose, Debug, Information, Warning, Error, Fatal)
- **Performance**: Minimal overhead with async logging
- **Full Serilog Ecosystem**: Access all Serilog sinks and enrichers

## Configuration

### Programmatic

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/workflow-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

using var foundry = WorkflowForge.CreateFoundry("MyWorkflow");
foundry.UseSerilogLogger(Log.Logger);
```

### From appsettings.json

```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/workflow-.txt",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

See [Configuration Guide](../../../docs/core/configuration.md#serilog-extension) for complete options.

## Structured Logging Examples

### With Context

```csharp
foundry.Logger.LogInformation(
    "Processing order {OrderId} for customer {CustomerId}",
    orderId,
    customerId);
```

### With Properties

```csharp
using (LogContext.PushProperty("WorkflowId", workflow.Id))
{
    foundry.Logger.LogInformation("Workflow started");
    // All logs in this scope include WorkflowId
}
```

### Performance Metrics

```csharp
var sw = Stopwatch.StartNew();
// ... operation ...
sw.Stop();

foundry.Logger.LogInformation(
    "Operation {OperationName} completed in {Duration}ms",
    operation.Name,
    sw.Elapsed.TotalMilliseconds);
```

## Sinks

### Console Sink

```csharp
.WriteTo.Console(outputTemplate: 
    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
```

### File Sink

```csharp
.WriteTo.File(
    "logs/workflow-.txt",
    rollingInterval: RollingInterval.Day,
    retainedFileCountLimit: 7)
```

### Seq Sink

```csharp
.WriteTo.Seq("http://localhost:5341")
```

### Elasticsearch Sink

```csharp
.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200")))
```

## Documentation

- **[Getting Started](../../../docs/getting-started/getting-started.md)**
- **[Configuration Guide](../../../docs/core/configuration.md#serilog-extension)**
- **[Extensions Overview](../../../docs/extensions/index.md)**
- **[Sample 13: Serilog Integration](../../samples/WorkflowForge.Samples.BasicConsole/README.md)**

---

**WorkflowForge.Extensions.Logging.Serilog** - *Build workflows with industrial strength*
