# WorkflowForge.Extensions.Observability.Performance

Performance monitoring extension for WorkflowForge with operation timing and metrics collection.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Observability.Performance.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Observability.Performance/)
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

**Lightweight architecture**: Built entirely on WorkflowForge core with no external libraries.

## Installation

```bash
dotnet add package WorkflowForge.Extensions.Observability.Performance
```

**Requires**: .NET Standard 2.0 or later

## Quick Start

The recommended approach is to use custom timing middleware for performance monitoring:

```csharp
using WorkflowForge;
using WorkflowForge.Extensions.Observability.Performance;

using var foundry = WorkflowForge.CreateFoundry("PerformanceMonitored");

// Add timing middleware to track operation performance (see Custom Timing Middleware section below)
foundry.AddMiddleware(new DetailedTimingMiddleware(foundry.Logger, TimeSpan.FromMilliseconds(100)));

var smith = WorkflowForge.CreateSmith();
var workflow = WorkflowForge.CreateWorkflow("PerformanceMonitored")
    .AddOperation(new ActionWorkflowOperation("Step1", async (input, foundry, ct) => { /* ... */ }))
    .Build();

await smith.ForgeAsync(workflow, foundry);
```

> **Note:** The `EnablePerformanceMonitoring()` and `GetPerformanceStatistics()` extension methods require a foundry that implements `IPerformanceMonitoredFoundry`. Standard foundries created via `WorkflowForge.CreateFoundry()` use the middleware pattern shown above instead.

## Key Features

- **Operation Timing**: Precise timing for each operation
- **Per-Operation Statistics**: Drill into individual operation metrics
- **Memory Tracking**: Track memory allocation per operation
- **Success/Failure Rates**: Monitor operation reliability
- **Enable/Disable at Runtime**: Toggle monitoring without restarting
- **Zero Dependencies**: Pure WorkflowForge extension

## Configuration

**This extension provides middleware components and interfaces for performance monitoring.** The primary approach is to add timing middleware to your foundry:

```csharp
using var foundry = WorkflowForge.CreateFoundry("PerformanceMonitored");
foundry.AddMiddleware(new DetailedTimingMiddleware(foundry.Logger, TimeSpan.FromMilliseconds(500)));
```

The `IFoundryPerformanceStatistics` and `IOperationStatistics` interfaces define the contract for foundries that provide built-in performance statistics.

## Advanced Usage

### Custom Timing Middleware

```csharp
public class DetailedTimingMiddleware : IWorkflowOperationMiddleware
{
    private readonly IWorkflowForgeLogger _logger;
    private readonly TimeSpan _slowThreshold;
    
    public DetailedTimingMiddleware(IWorkflowForgeLogger logger, TimeSpan slowThreshold)
    {
        _logger = logger;
        _slowThreshold = slowThreshold;
    }
    
    public async Task<object?> ExecuteAsync(
        IWorkflowOperation operation,
        IWorkflowFoundry foundry,
        object? inputData,
        Func<CancellationToken, Task<object?>> next,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        
        try
        {
            var result = await next(cancellationToken).ConfigureAwait(false);
            sw.Stop();
            
            if (sw.Elapsed > _slowThreshold)
            {
                _logger.LogWarning(
                    "SLOW: Operation {Name} took {Duration}ms (threshold: {Threshold}ms)",
                    operation.Name,
                    sw.Elapsed.TotalMilliseconds,
                    _slowThreshold.TotalMilliseconds);
            }
            else
            {
                _logger.LogInformation(
                    "Operation {Name} completed in {Duration}ms",
                    operation.Name,
                    sw.Elapsed.TotalMilliseconds);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "Operation {Name} failed after {Duration}ms",
                operation.Name,
                sw.Elapsed.TotalMilliseconds);
            throw;
        }
    }
}
```

### Memory Tracking

```csharp
public class MemoryTrackingMiddleware : IWorkflowOperationMiddleware
{
    public async Task<object?> ExecuteAsync(
        IWorkflowOperation operation,
        IWorkflowFoundry foundry,
        object? inputData,
        Func<CancellationToken, Task<object?>> next,
        CancellationToken cancellationToken = default)
    {
        var gen0Before = GC.CollectionCount(0);
        var memoryBefore = GC.GetTotalMemory(false);
        
        var result = await next(cancellationToken).ConfigureAwait(false);
        
        var gen0After = GC.CollectionCount(0);
        var memoryAfter = GC.GetTotalMemory(false);
        
        foundry.Logger.LogInformation(
            "Operation {Name}: Memory delta {MemoryDelta} bytes, Gen0 collections: {Gen0Collections}",
            operation.Name,
            memoryAfter - memoryBefore,
            gen0After - gen0Before);
        
        return result;
    }
}
```

## Available Statistics

### Foundry-Level (`IFoundryPerformanceStatistics`)

- **TotalOperations / SuccessfulOperations / FailedOperations**: Operation counts
- **SuccessRate**: Percentage of successful operations
- **AverageDuration / MinimumDuration / MaximumDuration**: Timing statistics
- **TotalMemoryAllocated / AverageMemoryPerOperation**: Memory metrics
- **OperationsPerSecond**: Throughput
- **StartTime / EndTime / TotalDuration**: Workflow timing

### Per-Operation (`IOperationStatistics`)

```csharp
var stats = foundry.GetPerformanceStatistics();
foreach (var opStats in stats.GetAllOperationStatistics())
{
    Console.WriteLine($"{opStats.OperationName}: avg {opStats.AverageExecutionTime.TotalMilliseconds:F2}ms");
}
```

## Documentation

- **[Getting Started](../../../docs/getting-started/getting-started.md)**
- **[Configuration Guide](../../../docs/core/configuration.md#performance-extension)**
- **[Extensions Overview](../../../docs/extensions/index.md)**
- **[Sample 17: Performance Monitoring](../../samples/WorkflowForge.Samples.BasicConsole/)**

---

