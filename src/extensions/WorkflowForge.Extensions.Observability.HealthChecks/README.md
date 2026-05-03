# WorkflowForge.Extensions.Observability.HealthChecks

Expose memory, GC, thread-pool, and custom probes with the same `IHealthCheck` shape you use elsewhere, without taking a dependency on Microsoft's health-check package graph.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Observability.HealthChecks.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Observability.HealthChecks/)

## Install

```bash
dotnet add package WorkflowForge.Extensions.Observability.HealthChecks
```

Targets .NET Standard 2.0 or later.

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

## Key points

- Ships WorkflowForge's `IHealthCheck` abstraction, not the full Microsoft health-check stack.
- Registers memory, GC, and thread-pool checks by default (`registerBuiltInHealthChecks: true`).
- `CreateHealthCheckService` lives on the foundry; interval is yours to choose.
- You can add any custom check that implements `IHealthCheck`.

## Built-in health checks

The `HealthCheckService` registers three built-in checks when `registerBuiltInHealthChecks` is true (the default):

| Check | Description |
|-------|-------------|
| `MemoryHealthCheck` | Monitors process memory usage |
| `GarbageCollectorHealthCheck` | Monitors GC pressure and collection counts |
| `ThreadPoolHealthCheck` | Monitors thread pool availability |

## Custom health check

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

## Health status

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

[Health checks configuration](../../../docs/core/configuration.md#health-checks-extension)

You can surface results over ASP.NET Core `/health`, Application Insights, Prometheus, or your own dashboards.

## Links

- [Getting Started](../../../docs/getting-started/getting-started.md)
- [Configuration Guide](../../../docs/core/configuration.md#health-checks-extension)
- [Extensions Overview](../../../docs/extensions/index.md)
- [Sample 16: Health Checks](../../samples/WorkflowForge.Samples.BasicConsole/README.md)
