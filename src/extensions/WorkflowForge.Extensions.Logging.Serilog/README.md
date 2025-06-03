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

## Documentation & Examples

- **[Interactive Samples](../../samples/WorkflowForge.Samples.BasicConsole/#13-serilog-logging)** - Sample #13: Serilog integration
- **[Extensions Documentation](../../../docs/extensions.md)** - Complete extensions guide  
- **[Getting Started](../../../docs/getting-started.md)** - Framework tutorial
- **[Main Documentation](../../../docs/)** - Comprehensive guides

---

*Professional structured logging for workflows* 