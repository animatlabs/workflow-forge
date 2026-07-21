# WorkflowForge.Extensions.Observability.Performance

Measure per-operation timing and memory from middleware, and read aggregate stats when the foundry exposes `IFoundryPerformanceStatistics`.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Observability.Performance.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Observability.Performance/)

## Install

```bash
dotnet add package WorkflowForge.Extensions.Observability.Performance
```

Targets .NET Standard 2.0. Beyond WorkflowForge core, it depends on `System.Diagnostics.DiagnosticSource` and `System.ComponentModel.Annotations` (both provided by the runtime on modern .NET).

## Quick Start

Call `EnablePerformanceMonitoring()` before running the workflow, then read the aggregated
statistics afterward with `GetPerformanceStatistics()`:

```csharp
using WorkflowForge;
using WorkflowForge.Extensions.Observability.Performance;

using var foundry = WorkflowForge.CreateFoundry("PerformanceMonitored");
foundry.EnablePerformanceMonitoring();

using var smith = WorkflowForge.CreateSmith();
var workflow = WorkflowForge.CreateWorkflow("PerformanceMonitored")
    .AddOperation(new ActionWorkflowOperation("Step1", async (input, foundry, ct) => { /* ... */ }))
    .Build();

// Pass the SAME foundry you enabled monitoring on so the stats survive the run.
await smith.ForgeAsync(workflow, foundry);

var stats = foundry.GetPerformanceStatistics();
Console.WriteLine($"Operations: {stats!.TotalOperations}, success rate: {stats.SuccessRate:P0}");
foreach (var op in stats.GetAllOperationStatistics())
    Console.WriteLine($"{op.OperationName}: avg {op.AverageExecutionTime.TotalMilliseconds:F2} ms");
```

`EnablePerformanceMonitoring()` registers a `PerformanceStatisticsMiddleware` and stores a
`FoundryPerformanceStatistics` on the foundry; the middleware records timing, success/failure, and
approximate memory per operation. For custom needs you can add your own middleware instead (see
[Custom Timing Middleware](#custom-timing-middleware)).

## Key points

- Middleware can log durations, flag slow calls, and track allocations per operation.
- `IFoundryPerformanceStatistics` and `IOperationStatistics` describe the contract when a foundry exposes built-in counters.
- Toggle or tune behavior in code; there is no separate JSON schema in this package.

## Configuration

- Ships middleware types and `IFoundryPerformanceStatistics` / `IOperationStatistics` for foundries that expose statistics.
- Typical pattern: timing middleware on the foundry:

```csharp
using var foundry = WorkflowForge.CreateFoundry("PerformanceMonitored");
foundry.AddMiddleware(new DetailedTimingMiddleware(foundry.Logger, TimeSpan.FromMilliseconds(500)));
```

- `IFoundryPerformanceStatistics` and `IOperationStatistics` define the contract for built-in performance statistics on a foundry.

[Performance extension](../../../docs/core/configuration.md)

## Advanced usage

### Custom timing middleware

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

### Memory tracking

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

## Available statistics

### Foundry-level (`IFoundryPerformanceStatistics`)

- **TotalOperations / SuccessfulOperations / FailedOperations**: Operation counts
- **SuccessRate**: Percentage of successful operations
- **AverageDuration / MinimumDuration / MaximumDuration**: Timing statistics
- **TotalMemoryAllocated / AverageMemoryPerOperation**: Memory metrics
- **OperationsPerSecond**: Throughput
- **StartTime / EndTime / TotalDuration**: Workflow timing

### Per-operation (`IOperationStatistics`)

```csharp
var stats = foundry.GetPerformanceStatistics();
foreach (var opStats in stats.GetAllOperationStatistics())
{
    Console.WriteLine($"{opStats.OperationName}: avg {opStats.AverageExecutionTime.TotalMilliseconds:F2}ms");
}
```

## Links

- [Getting Started](../../../docs/getting-started/getting-started.md)
- [Configuration Guide](../../../docs/core/configuration.md)
- [Extensions Overview](../../../docs/extensions/index.md)
- [Sample 17: Performance Monitoring](../../samples/WorkflowForge.Samples.BasicConsole/)
