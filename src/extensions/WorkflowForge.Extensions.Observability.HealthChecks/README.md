# WorkflowForge.Extensions.Observability.HealthChecks

<p align="center">
  <img src="https://raw.githubusercontent.com/animatlabs/workflow-forge/main/icon.png" alt="WorkflowForge" width="120" height="120">
</p>

Health check integration extension for WorkflowForge compatible with Microsoft.Extensions.Diagnostics.HealthChecks.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Observability.HealthChecks.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Observability.HealthChecks/)

## Zero Dependencies - Zero Conflicts

**This extension has ZERO external dependencies.** This means:

- NO DLL Hell - No third-party dependencies to conflict with
- NO Version Conflicts - Works with any versions of your application dependencies
- Clean Deployment - Pure WorkflowForge extension

**Interface-only**: Implements `IHealthCheck` interface without requiring the full Microsoft package.

## Installation

```bash
dotnet add package WorkflowForge.Extensions.Observability.HealthChecks
```

**Requires**: .NET Standard 2.0 or later

## Quick Start

```csharp
using WorkflowForge.Extensions.Observability.HealthChecks;

// Configure health checks
var healthCheck = new WorkflowHealthCheck(
    timeProvider: new SystemTimeProvider(),
    unhealthyThresholdSeconds: 30);

// Register workflow execution
smith.WorkflowStarted += (s, e) => healthCheck.RecordWorkflowExecution();
smith.WorkflowCompleted += (s, e) => healthCheck.RecordWorkflowExecution();

// Check health
var healthStatus = await healthCheck.CheckHealthAsync(new HealthCheckContext());

if (healthStatus.Status == HealthStatus.Unhealthy)
{
    logger.LogWarning("WorkflowForge health check failed: {Description}", 
        healthStatus.Description);
}
```

## Key Features

- **ASP.NET Core Integration**: Compatible with health check middleware
- **Workflow Monitoring**: Track workflow execution health
- **Threshold-Based**: Configurable unhealthy thresholds
- **Time Provider Integration**: `ISystemTimeProvider` for testability
- **No Dependencies**: Interface-only implementation

## ASP.NET Core Integration

```csharp
// Startup.cs or Program.cs
services.AddSingleton<ISystemTimeProvider, SystemTimeProvider>();
services.AddSingleton<WorkflowHealthCheck>();

services.AddHealthChecks()
    .AddCheck<WorkflowHealthCheck>("workflow_health");

app.MapHealthChecks("/health");
```

## Configuration

```csharp
var healthCheck = new WorkflowHealthCheck(
    timeProvider: serviceProvider.GetRequiredService<ISystemTimeProvider>(),
    unhealthyThresholdSeconds: 30);

// Subscribe to events
smith.WorkflowStarted += (s, e) => healthCheck.RecordWorkflowExecution();
smith.WorkflowCompleted += (s, e) => healthCheck.RecordWorkflowExecution();
```

See [Configuration Guide](../../../docs/core/configuration.md#health-checks-extension) for complete options.

## Health Status Logic

- **Healthy**: Workflow executed within threshold (< 30 seconds by default)
- **Unhealthy**: No workflow execution within threshold
- **Degraded**: Not currently used

## Custom Health Check

```csharp
public class CustomWorkflowHealthCheck : IHealthCheck
{
    private readonly IWorkflowSmith _smith;
    private DateTime _lastExecution;
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var timeSinceLastExecution = DateTime.UtcNow - _lastExecution;
        
        if (timeSinceLastExecution > TimeSpan.FromMinutes(5))
        {
            return HealthCheckResult.Unhealthy(
                $"No workflow execution in {timeSinceLastExecution.TotalMinutes:F1} minutes");
        }
        
        return HealthCheckResult.Healthy("Workflows executing normally");
    }
}
```

## Monitoring Dashboard

Health checks can be monitored via:
- ASP.NET Core `/health` endpoint
- Application Insights
- Prometheus metrics
- Custom monitoring solutions

## Documentation

- **[Getting Started](../../../docs/getting-started/getting-started.md)**
- **[Configuration Guide](../../../docs/core/configuration.md#health-checks-extension)**
- **[Extensions Overview](../../../docs/extensions/index.md)**
- **[Sample 16: Health Checks](../../samples/WorkflowForge.Samples.BasicConsole/README.md)**

---

**WorkflowForge.Extensions.Observability.HealthChecks** - *Build workflows with industrial strength*
