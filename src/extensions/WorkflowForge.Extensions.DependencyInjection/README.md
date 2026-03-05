# WorkflowForge.Extensions.DependencyInjection

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.DependencyInjection.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.DependencyInjection/)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=coverage)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![NuGet Downloads](https://img.shields.io/nuget/dt/WorkflowForge.Extensions.DependencyInjection.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.DependencyInjection/)

**Microsoft.Extensions.DependencyInjection integration for WorkflowForge**

This extension provides seamless integration with Microsoft's dependency injection container, enabling:
- IOptions<T> pattern support
- Automatic configuration validation on startup
- ASP.NET Core integration
- Configuration binding from appsettings.json

---

## 📦 Installation

```bash
dotnet add package WorkflowForge.Extensions.DependencyInjection
```

**Note**: This package is **optional**. WorkflowForge core has zero dependencies and can be used standalone.

---

## 🚀 Quick Start

### ASP.NET Core

```csharp
// Program.cs or Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // Register logger (choose your implementation)
    services.AddSingleton<IWorkflowForgeLogger>(sp =>
        SerilogExtensions.CreateWorkflowForgeLogger());
    
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

### Usage in Controllers/Services

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

---

## 🔧 Manual Configuration

If you don't use appsettings.json, you can configure options manually:

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

---

## ✅ Automatic Validation

This extension **validates configuration on startup** using the `Validate()` methods from options classes
and registers `IValidateOptions<WorkflowForgeOptions>` for startup-time checks:

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

**Validation errors are caught at startup, not at runtime!**

---

## 📚 API Reference

### AddWorkflowForge(IConfiguration)
Binds configuration from appsettings.json and validates.

```csharp
services.AddWorkflowForge(Configuration);
```

### AddWorkflowForge(Actions)
Manually configure options.

```csharp
services.AddWorkflowForge(
    core => { /* ... */ },
    timing => { /* ... */ },
    logging => { /* ... */ },
    errorHandling => { /* ... */ }
);
```

### AddWorkflowSmith()
Registers `IWorkflowSmith` as a singleton.

**Requirements**:
- `IWorkflowForgeLogger` must be registered
- `AddWorkflowForge()` must be called first

```csharp
services.AddSingleton<IWorkflowForgeLogger>(/* ... */);
services.AddWorkflowForge(Configuration);
services.AddWorkflowSmith(); // ✅ Now WorkflowSmith is available
```

---

## 🎯 Configuration Sections

| Section | Options Class | Validated |
|---------|---------------|-----------|
| `WorkflowForge` | `WorkflowForgeOptions` | ✅ Yes |
| `WorkflowForge:Middleware:Timing` | `TimingMiddlewareOptions` | ❌ No |
| `WorkflowForge:Middleware:Logging` | `LoggingMiddlewareOptions` | ✅ Yes |
| `WorkflowForge:Middleware:ErrorHandling` | `ErrorHandlingMiddlewareOptions` | ❌ No |

---

## 🔍 Why This Extension Exists

**WorkflowForge core has ZERO dependencies** to avoid version conflicts when using extensions.

This extension provides **optional** DI integration for users who want:
- IOptions<T> pattern
- Configuration validation on startup
- ASP.NET Core integration
- appsettings.json binding

**If you don't need these features, use WorkflowForge core directly!**

---

## 📖 Related Packages

- **WorkflowForge** - Core workflow engine (zero dependencies)
- **WorkflowForge.Extensions.Logging.Serilog** - Serilog integration
- **WorkflowForge.Extensions.Resilience.Polly** - Polly resilience patterns
- **WorkflowForge.Extensions.Observability.Performance** - Performance metrics

---

## 📄 License

MIT License - Copyright © 2025-2026 AnimatLabs







