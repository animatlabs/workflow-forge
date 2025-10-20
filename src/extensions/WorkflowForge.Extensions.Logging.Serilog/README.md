# WorkflowForge.Extensions.Logging.Serilog

Professional structured logging extension for WorkflowForge using Serilog.

## Installation

```bash
dotnet add package WorkflowForge.Extensions.Logging.Serilog
```

## Zero Version Conflicts

**This extension uses Costura.Fody to embed Serilog dependencies.** This means:

- **NO DLL Hell** - Use ANY version of Serilog in your project  
- **NO Conflicts** - Your app's Serilog version won't clash with this extension  
- **Clean Deployment** - Professional-grade dependency isolation

**Example**: Your app uses Serilog 4.x, this extension uses 3.x - both coexist perfectly with zero conflicts!

**How it works**: Serilog is embedded as a compressed resource at build time and loaded automatically at runtime, completely isolated from your application's dependencies.

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