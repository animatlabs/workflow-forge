# WorkflowForge.Extensions.Observability.HealthChecks

Health check integration extension for WorkflowForge compatible with Microsoft.Extensions.Diagnostics.HealthChecks.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Observability.HealthChecks.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Observability.HealthChecks/)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=coverage)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)

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
using WorkflowForge;
using WorkflowForge.Extensions.Observability.HealthChecks;

using var foundry = WorkflowForge.CreateFoundry("MonitoredWorkflow");

// Create the health check service from the foundry
var healthService = foundry.CreateHealthCheckService(
    checkInterval: TimeSpan.FromSeconds(30));

// Run all registered health checks
var results = await healthService.CheckHealthAsync();

Console.WriteLine($"Overall status: {healthService.OverallStatus}");
foreach (var (name, result) in results)
{
    Console.WriteLine($"  {name}: {result.Status} - {result.Description}");
}
```

## Key Features

- **Built-in Checks**: Memory, Garbage Collector, and ThreadPool health checks out of the box
- **Custom Health Checks**: Implement `IHealthCheck` to add your own checks
- **Foundry Integration**: Create service directly from a foundry instance
- **Configurable Interval**: Set check frequency for periodic monitoring
- **Zero Dependencies**: WorkflowForge's own `IHealthCheck` abstraction (not Microsoft's)

## Built-in Health Checks

The `HealthCheckService` automatically registers three built-in checks when `registerBuiltInHealthChecks` is true (the default):

| Check | Description |
|-------|-------------|
| `MemoryHealthCheck` | Monitors process memory usage |
| `GarbageCollectorHealthCheck` | Monitors GC pressure and collection counts |
| `ThreadPoolHealthCheck` | Monitors thread pool availability |

## Custom Health Check

```csharp
using WorkflowForge.Extensions.Observability.HealthChecks.Abstractions;

public class DatabaseHealthCheck : IHealthCheck
{
    public string Name => "Database";
    public string Description => "Checks database connectivity";

    public async Task<HealthCheckResult> CheckHealthAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check your database connection
            await CheckDatabaseAsync(cancellationToken);
            return HealthCheckResult.Healthy("Database is reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database unreachable", ex);
        }
    }
}

// Register custom checks
healthService.RegisterHealthCheck(new DatabaseHealthCheck());
```

## Health Status

| Status | Meaning |
|--------|---------|
| `Healthy` | All checks pass |
| `Degraded` | Some checks report degraded performance |
| `Unhealthy` | One or more checks report failure |

```csharp
var result = await healthService.CheckHealthAsync("Memory");
if (result?.Status == HealthStatus.Unhealthy)
{
    logger.LogWarning("Memory health check failed: {Description}", result.Description);
}
```

See [Configuration Guide](../../../docs/core/configuration.md#health-checks-extension) for complete options.

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

