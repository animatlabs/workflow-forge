# WorkflowForge.Extensions.Observability.HealthChecks

Comprehensive health monitoring extension for WorkflowForge applications with built-in and custom health checks.

## Installation

```bash
dotnet add package WorkflowForge.Extensions.Observability.HealthChecks
```

## Quick Start

```csharp
using WorkflowForge.Extensions.Observability.HealthChecks;

// Create foundry and create a health check service
using var foundry = WorkflowForge.CreateFoundry("MyWorkflow");
var healthService = foundry.CreateHealthCheckService(TimeSpan.FromSeconds(30));

// Execute health checks
var results = await healthService.CheckHealthAsync();

Console.WriteLine($"Overall Status: {healthService.OverallStatus}");
foreach (var (name, result) in results)
{
    Console.WriteLine($"{name}: {result.Status} - {result.Description}");
}
```

## Key Features

- **Built-in Health Checks**: Memory, garbage collection, thread pool monitoring
- **Custom Health Checks**: Easy interface for application-specific checks  
- **Periodic Monitoring**: Optional background health check execution
- **Foundry Integration**: Seamless foundry health monitoring
- **Detailed Reporting**: Rich health check results with performance data

## Built-in Health Checks

### System Health Monitoring
```csharp
// Automatically included when using CreateHealthCheckService():
// - Memory Health Check (working set, managed memory)
// - Garbage Collector Health Check (GC performance)
// - Thread Pool Health Check (thread availability)

var overallStatus = await foundry.CheckFoundryHealthAsync(healthService);
```

## Custom Health Checks

```csharp
public class DatabaseHealthCheck : IHealthCheck
{
    public string Name => "Database";
    public string Description => "Checks database connectivity";

    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _connection.OpenAsync(cancellationToken);
            return HealthCheckResult.Healthy("Database connection successful");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    }
}

// Register custom health check
healthService.RegisterHealthCheck(new DatabaseHealthCheck(dbConnection));
```

## Periodic Monitoring

```csharp
// Enable automatic health monitoring every 30 seconds
var healthService = foundry.CreateHealthCheckService(TimeSpan.FromSeconds(30));

// Access latest results anytime
var lastResults = healthService.LastResults;
var currentStatus = healthService.OverallStatus;
```

## Examples & Documentation

- **[Complete Examples](../../samples/WorkflowForge.Samples.BasicConsole/README.md#16-health-checks)** - Interactive health monitoring samples
- **[Core Documentation](../../core/WorkflowForge/README.md)** - Core concepts
- **[Performance Extension](../WorkflowForge.Extensions.Observability.Performance/README.md)** - Performance monitoring
- **[Main README](../../../README.md)** - Framework overview

---

*Keep your workflows healthy* 