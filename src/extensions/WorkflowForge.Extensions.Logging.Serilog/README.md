# WorkflowForge.Extensions.Logging.Serilog

Structured logging extension for WorkflowForge with Serilog integration for rich, queryable logs.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Logging.Serilog.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Logging.Serilog/)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=coverage)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)

## Dependency Isolation

**This extension internalizes Serilog with ILRepack.** This means:

- Reduced dependency conflicts for Serilog
- Public APIs stay WorkflowForge/BCL only
- Microsoft/System assemblies remain external

## Installation

```bash
dotnet add package WorkflowForge.Extensions.Logging.Serilog
```

**Requires**: .NET Standard 2.0 or later

## Quick Start

```csharp
using WorkflowForge;
using WorkflowForge.Extensions.Logging.Serilog;

var logger = SerilogLoggerFactory.CreateLogger(new SerilogLoggerOptions
{
    MinimumLevel = "Information",
    EnableConsoleSink = true
});

using var foundry = WorkflowForge.CreateFoundry("MyWorkflow", logger);
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
var logger = SerilogLoggerFactory.CreateLogger(new SerilogLoggerOptions
{
    MinimumLevel = "Information",
    EnableConsoleSink = true
});

using var foundry = WorkflowForge.CreateFoundry("MyWorkflow", logger);
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
foundry.Logger.LogInformation(
    new Dictionary<string, string> { ["WorkflowId"] = workflowId },
    "Workflow started");
// Or use foundry.Logger.BeginScope(state, properties) for scoped context
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

## Sink Configuration

### Built-in Console Sink

The extension includes a built-in console sink configured via `SerilogLoggerOptions`:

```csharp
var logger = SerilogLoggerFactory.CreateLogger(new SerilogLoggerOptions
{
    EnableConsoleSink = true,
    MinimumLevel = "Debug"
});
```

### Advanced Sinks (Host Integration)

For advanced sinks (File, Seq, Elasticsearch, etc.), use the `CreateLogger(ILoggerFactory)` overload with your host application's Serilog configuration:

```csharp
// Configure Serilog in your host application (requires Serilog packages)
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("logs/workflow.log")
    .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();

var hostLoggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());

// Create WorkflowForge logger from host configuration
var logger = SerilogLoggerFactory.CreateLogger(hostLoggerFactory);
```

This approach gives you access to the full Serilog sink ecosystem while keeping the extension dependency-free.

## Documentation

- **[Getting Started](../../../docs/getting-started/getting-started.md)**
- **[Configuration Guide](../../../docs/core/configuration.md#serilog-extension)**
- **[Extensions Overview](../../../docs/extensions/index.md)**
- **[Sample 13: Serilog Integration](../../samples/WorkflowForge.Samples.BasicConsole/README.md)**

---

