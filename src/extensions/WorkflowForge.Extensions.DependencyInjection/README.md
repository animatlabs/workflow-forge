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

- `AddWorkflowForge(IConfiguration)` binds options and registers validation with `ValidateOnStart()`.
- `AddWorkflowSmith()` needs `IWorkflowForgeLogger` and a prior `AddWorkflowForge` call.
- On a .NET generic host, invalid `appsettings.json` values fail the host at startup rather than
  mid-request. Outside a host, validation runs on first `IOptions<T>.Value` resolution.

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

Options are registered with `.Validate(...)` plus `.ValidateOnStart()`, and
`IValidateOptions<WorkflowForgeOptions>` is registered as well. On a generic host,
`IHost.StartAsync` throws before the application serves traffic:

```csharp
// host.RunAsync() throws at startup if configuration is invalid
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

[WorkflowForge configuration](https://animatlabs.com/workflow-forge/core/configuration/)

## Links

- [Getting Started](https://animatlabs.com/workflow-forge/getting-started/getting-started/)
- [Extensions Overview](https://animatlabs.com/workflow-forge/extensions/)
- **WorkflowForge**: core workflow engine (zero dependencies)
- **WorkflowForge.Extensions.Logging.Serilog**: Serilog integration
- **WorkflowForge.Extensions.Resilience.Polly**: Polly resilience patterns
- **WorkflowForge.Extensions.Observability.Performance**: performance metrics

## License

MIT License - Copyright © 2025-2026 AnimatLabs
