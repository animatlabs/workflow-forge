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

The embedded pipeline reads only these three settings. Bind them to `SerilogLoggerOptions` and pass
the result to `CreateLogger`:

```json
{
  "WorkflowForge": {
    "Extensions": {
      "Serilog": {
        "MinimumLevel": "Information",
        "EnableConsoleSink": true,
        "ConsoleOutputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
      }
    }
  }
}
```

A standard Serilog `WriteTo` section is **not** read by this package. For file, Seq or any other
sink, configure Serilog in your host and use `CreateLogger(ILoggerFactory)` - see
[Advanced sinks](#advanced-sinks-host-integration) below.

[Serilog extension options](https://animatlabs.com/workflow-forge/core/configuration/#serilog-extension)

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

- [Getting Started](https://animatlabs.com/workflow-forge/getting-started/getting-started/)
- [Configuration Guide](https://animatlabs.com/workflow-forge/core/configuration/#serilog-extension)
- [Extensions Overview](https://animatlabs.com/workflow-forge/extensions/)
- [Sample 13: Serilog](https://github.com/animatlabs/workflow-forge/blob/main/src/samples/WorkflowForge.Samples.BasicConsole/README.md)
