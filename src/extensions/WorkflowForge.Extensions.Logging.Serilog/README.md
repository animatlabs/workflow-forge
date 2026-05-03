# WorkflowForge.Extensions.Logging.Serilog

Hooks WorkflowForge to Serilog for structured logs; Serilog is merged in with ILRepack so your public surface stays small.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Logging.Serilog.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Logging.Serilog/)

## Install

```bash
dotnet add package WorkflowForge.Extensions.Logging.Serilog
```

Targets .NET Standard 2.0 or later.

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

## Key points

- Serilog is internalized to cut down version clashes; your code still talks to WorkflowForge and BCL types.
- Console sink is available from the package; host apps can add File, Seq, Elasticsearch, and others via `ILoggerFactory`.
- Log levels from Verbose through Fatal; optional workflow and operation context on events.

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

[Serilog extension options](../../../docs/core/configuration.md#serilog-extension)

## Structured logging examples

### With context

```csharp
foundry.Logger.LogInformation(
    "Processing order {OrderId} for customer {CustomerId}",
    orderId,
    customerId);
```

### With properties

```csharp
foundry.Logger.LogInformation(
    new Dictionary<string, string> { ["WorkflowId"] = workflowId },
    "Workflow started");
// Or use foundry.Logger.BeginScope(state, properties) for scoped context
```

### Performance metrics

```csharp
var sw = Stopwatch.StartNew();
// ... operation ...
sw.Stop();

foundry.Logger.LogInformation(
    "Operation {OperationName} completed in {Duration}ms",
    operation.Name,
    sw.Elapsed.TotalMilliseconds);
```

## Sink configuration

### Built-in console sink

The extension includes a built-in console sink configured via `SerilogLoggerOptions`:

```csharp
var logger = SerilogLoggerFactory.CreateLogger(new SerilogLoggerOptions
{
    EnableConsoleSink = true,
    MinimumLevel = "Debug"
});
```

### Advanced sinks (host integration)

For File, Seq, Elasticsearch, and similar sinks, use `CreateLogger(ILoggerFactory)` with your host Serilog setup:

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

That path uses every sink you configure in the host while this package stays light.

## Links

- [Getting Started](../../../docs/getting-started/getting-started.md)
- [Configuration Guide](../../../docs/core/configuration.md#serilog-extension)
- [Extensions Overview](../../../docs/extensions/index.md)
- [Sample 13: Serilog](../../samples/WorkflowForge.Samples.BasicConsole/README.md)
