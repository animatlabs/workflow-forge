# WorkflowForge.Extensions.Logging.Serilog

Professional structured logging extension for WorkflowForge using Serilog.

## Installation

```bash
dotnet add package WorkflowForge.Extensions.Logging.Serilog
```

## Quick Start

```csharp
using WorkflowForge.Extensions.Logging.Serilog;

// Enable Serilog logging for foundry
var foundryConfig = FoundryConfiguration.ForProduction()
    .UseSerilog();

var foundry = WorkflowForge.CreateFoundry("MyWorkflow", foundryConfig);

// Or use specific logger instance
var foundryConfig = FoundryConfiguration.ForDevelopment()
    .UseSerilog(myLoggerInstance);
```

## Key Features

- **Zero Configuration**: Works with existing Serilog setup
- **Structured Logging**: Rich structured logs with workflow context
- **Property Enrichment**: Automatic workflow metadata enrichment
- **Scope Support**: Correlated logging across operations
- **Performance Optimized**: Minimal overhead

## Environment Configurations

```csharp
// Development - verbose logging
foundryConfig.UseSerilog(logger => logger
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.Debug());

// Production - structured file logging  
foundryConfig.UseSerilog(logger => logger
    .MinimumLevel.Information()
    .WriteTo.File("logs/workflows-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq("http://seq-server:5341"));
```

## Examples & Documentation

- **[Complete Examples](../../samples/WorkflowForge.Samples.BasicConsole/README.md#13-serilog-logging)** - Interactive samples with Serilog integration
- **[Core Documentation](../../core/WorkflowForge/README.md#professional-logging-system)** - Professional logging system details
- **[Main README](../../../README.md)** - Framework overview

---

*Professional structured logging for workflows* 