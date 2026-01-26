# WorkflowForge.Extensions.Observability.Performance

<p align="center">
  <img src="https://raw.githubusercontent.com/animatlabs/workflow-forge/main/icon.png" alt="WorkflowForge" width="120" height="120">
</p>

Performance monitoring extension for WorkflowForge with operation timing and metrics collection.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Observability.Performance.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Observability.Performance/)

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

```csharp
using WorkflowForge.Extensions.Observability.Performance;

// Add timing middleware
using var foundry = WorkflowForge.CreateFoundry("PerformanceMonitored");
foundry.AddMiddleware(new TimingMiddleware(logger));

// Timing data is logged per operation
await smith.ForgeAsync(workflow, foundry);

// Output:
// [INF] Operation 'CalculateTotal' started
// [INF] Operation 'CalculateTotal' completed in 23.4ms
```

## Key Features

- **Operation Timing**: Precise timing for each operation
- **Middleware-Based**: Non-intrusive timing collection
- **Automatic Logging**: Integrates with foundry logger
- **Memory Tracking**: Optional memory allocation tracking
- **Configurable Thresholds**: Alert on slow operations
- **Zero Overhead**: Minimal performance impact

## Configuration

**This extension requires NO configuration.** Simply add the middleware to your foundry:

```csharp
using var foundry = WorkflowForge.CreateFoundry("PerformanceMonitored");
foundry.AddMiddleware(new TimingMiddleware(logger));
```

The extension works out-of-the-box with sensible defaults. See [Configuration Guide](../../../docs/core/configuration.md#performance-extension) for more information.

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
        Func<Task<object?>> next,
        IWorkflowOperation operation,
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        
        try
        {
            var result = await next();
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
        Func<Task<object?>> next,
        IWorkflowOperation operation,
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken)
    {
        var gen0Before = GC.CollectionCount(0);
        var memoryBefore = GC.GetTotalMemory(false);
        
        var result = await next();
        
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

## Performance Metrics

The extension tracks:
- **Operation Duration**: Precise timing for each operation
- **Workflow Duration**: Total workflow execution time
- **Slow Operations**: Operations exceeding threshold
- **Memory Allocation**: Optional memory tracking
- **GC Collections**: Garbage collection metrics

## Documentation

- **[Getting Started](../../../docs/getting-started/getting-started.md)**
- **[Configuration Guide](../../../docs/core/configuration.md#performance-extension)**
- **[Extensions Overview](../../../docs/extensions/index.md)**
- **[Sample 17: Performance Monitoring](../../samples/WorkflowForge.Samples.BasicConsole/)**

---

**WorkflowForge.Extensions.Observability.Performance** - *Build workflows with industrial strength*
