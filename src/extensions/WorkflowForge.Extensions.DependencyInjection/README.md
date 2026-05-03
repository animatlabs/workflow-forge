# WorkflowForge.Extensions.DependencyInjection

Registers WorkflowForge in `Microsoft.Extensions.DependencyInjection`, binds options from configuration, and fails startup when settings are invalid.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.DependencyInjection.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.DependencyInjection/)

## Install

```bash
dotnet add package WorkflowForge.Extensions.DependencyInjection
```

Targets .NET Standard 2.0 or later. WorkflowForge core has no DI dependency; add this package only when you want `IOptions<T>`, `appsettings.json`, and hosted app wiring.

## Quick Start

### ASP.NET Core

```csharp
// Program.cs or Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // Register logger (choose your implementation)
    services.AddSingleton<IWorkflowForgeLogger>(sp =>
        SerilogLoggerFactory.CreateLogger());
    
    // Register WorkflowForge with configuration binding
    services.AddWorkflowForge(Configuration);
    
    // Register WorkflowSmith
    services.AddWorkflowSmith();
}
```

### appsettings.json

```json
{
  "WorkflowForge": {
    "MaxConcurrentWorkflows": 2,
    "ContinueOnError": false,
    "FailFastCompensation": false,
    "ThrowOnCompensationError": false,
    "EnableOutputChaining": true,
    "Middleware": {
      "Timing": {
        "Enabled": true,
        "IncludeDetailedTimings": false
      },
      "Logging": {
        "Enabled": true,
        "MinimumLevel": "Information",
        "LogDataPayloads": false
      },
      "ErrorHandling": {
        "Enabled": true,
        "RethrowExceptions": true,
        "IncludeStackTraces": true
      }
    }
  }
}
```

### Usage in controllers or services

```csharp
public class WorkflowService
{
    private readonly IWorkflowSmith _smith;
    private readonly IOptions<TimingMiddlewareOptions> _timingOptions;
    
    public WorkflowService(
        IWorkflowSmith smith,
        IOptions<TimingMiddlewareOptions> timingOptions)
    {
        _smith = smith;
        _timingOptions = timingOptions;
    }
    
    public async Task ExecuteWorkflow()
    {
        var workflow = new MyWorkflow();
        await _smith.ForgeAsync(workflow);
    }
}
```

## Key points

- `AddWorkflowForge(IConfiguration)` binds and validates options at startup.
- `AddWorkflowSmith()` needs `IWorkflowForgeLogger` and a prior `AddWorkflowForge` call.
- Invalid `appsettings.json` values throw during startup instead of mid-run.

## Configuration

### Without appsettings.json

```csharp
services.AddWorkflowForge(
    core =>
    {
        core.MaxConcurrentWorkflows = 2;
        core.ContinueOnError = false;
        core.EnableOutputChaining = true;
    },
    timing => timing.Enabled = false,
    logging => logging.MinimumLevel = "Warning",
    errorHandling => errorHandling.IncludeStackTraces = false
);
```

### Startup validation

`Validate()` runs on options; `IValidateOptions<WorkflowForgeOptions>` is registered so bad config fails fast:

```csharp
// This will throw on startup if configuration is invalid
services.AddWorkflowForge(configuration);

// Invalid configuration example:
{
  "WorkflowForge": {
    "MaxConcurrentWorkflows": -5,  // ❌ Throws: Must be between 0 and 10000
    "Middleware": {
      "Logging": {
        "MinimumLevel": "Verbose"  // ❌ Throws: Must be Trace/Debug/Information/Warning/Error/Critical
      }
    }
  }
}
```

### Registration API

**`AddWorkflowForge(IConfiguration)`**  
Binds configuration from `appsettings.json` and validates.

```csharp
services.AddWorkflowForge(Configuration);
```

**`AddWorkflowForge` with delegates**  
Configure options in code.

```csharp
services.AddWorkflowForge(
    core => { /* ... */ },
    timing => { /* ... */ },
    logging => { /* ... */ },
    errorHandling => { /* ... */ }
);
```

**`AddWorkflowSmith()`**  
Registers `IWorkflowSmith` as a singleton.

Requirements: register `IWorkflowForgeLogger`, call `AddWorkflowForge()` first.

```csharp
services.AddSingleton<IWorkflowForgeLogger>(/* ... */);
services.AddWorkflowForge(Configuration);
services.AddWorkflowSmith(); // ✅ Now WorkflowSmith is available
```

### Configuration sections

| Section | Options class | Validated |
|---------|---------------|-----------|
| `WorkflowForge` | `WorkflowForgeOptions` | Yes |
| `WorkflowForge:Middleware:Timing` | `TimingMiddlewareOptions` | No |
| `WorkflowForge:Middleware:Logging` | `LoggingMiddlewareOptions` | Yes |
| `WorkflowForge:Middleware:ErrorHandling` | `ErrorHandlingMiddlewareOptions` | No |

[WorkflowForge configuration](../../../docs/core/configuration.md)

## Links

- [Getting Started](../../../docs/getting-started/getting-started.md)
- [Extensions Overview](../../../docs/extensions/index.md)
- **WorkflowForge**: core workflow engine (zero dependencies)
- **WorkflowForge.Extensions.Logging.Serilog**: Serilog integration
- **WorkflowForge.Extensions.Resilience.Polly**: Polly resilience patterns
- **WorkflowForge.Extensions.Observability.Performance**: performance metrics

## License

MIT License - Copyright © 2025-2026 AnimatLabs
