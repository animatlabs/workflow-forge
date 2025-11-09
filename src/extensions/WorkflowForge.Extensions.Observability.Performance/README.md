# WorkflowForge.Extensions.Observability.Performance

Comprehensive performance monitoring extension for WorkflowForge with detailed metrics and analytics.

## Installation

```bash
dotnet add package WorkflowForge.Extensions.Observability.Performance
```

## Zero Version Conflicts

**This extension uses Costura.Fody to embed System.Diagnostics.DiagnosticSource.** This means:

- **NO DLL Hell** - No conflicts with your application's System libraries  
- **NO Conflicts** - Works seamlessly with any .NET version  
- **Clean Deployment** - Professional-grade dependency isolation

**How it works**: Dependencies are embedded as compressed resources at build time and loaded automatically at runtime, completely isolated from your application.

## Quick Start

```csharp
using WorkflowForge.Extensions.Observability.Performance;

// Create foundry and enable monitoring
using var foundry = WorkflowForge.CreateFoundry("MyWorkflow");
foundry.EnablePerformanceMonitoring();

// Execute workflows (multiple times for meaningful statistics)
using var smith = WorkflowForge.CreateSmith();
await smith.ForgeAsync(workflow, foundry);

// Access performance statistics
var stats = foundry.GetPerformanceStatistics();
Console.WriteLine($"Success Rate: {stats?.SuccessRate:P2}");
Console.WriteLine($"Average Duration: {stats?.AverageDuration}ms");
Console.WriteLine($"Operations/sec: {stats?.OperationsPerSecond:F2}");
```

## Key Features

- **Foundry Performance Statistics**: Comprehensive workflow execution metrics
- **Operation-level Metrics**: Detailed statistics for individual operations
- **Real-time Monitoring**: Live performance data collection and reporting
- **Memory Efficient**: Property-based storage without interface pollution
- **Detailed Analytics**: Success rates, timing, memory usage, error patterns

## Performance Metrics

### Foundry-Level Metrics
```csharp
var stats = foundry.GetPerformanceStatistics();

// Execution Statistics
Console.WriteLine($"Total Operations: {stats.TotalOperations}");
Console.WriteLine($"Success Rate: {stats.SuccessRate:P2}");
Console.WriteLine($"Operations per Second: {stats.OperationsPerSecond:F2}");

// Timing Statistics
Console.WriteLine($"Average Duration: {stats.AverageDuration}ms");
Console.WriteLine($"95th Percentile: {stats.GetPercentile(95)}ms");

// Memory Statistics
Console.WriteLine($"Memory per Operation: {stats.AverageMemoryPerOperation / 1024:F2} KB");
```

### Operation-Level Metrics
```csharp
foreach (var opStats in stats.GetAllOperationStatistics())
{
    Console.WriteLine($"Operation '{opStats.OperationName}': {opStats.SuccessRate:P2} success, {opStats.AverageExecutionTime}ms avg");
}
```

## Environment Configurations

```csharp
// Development - detailed monitoring (no options object; enable/disable only)
using var devFoundry = WorkflowForge.CreateFoundry("Dev");
devFoundry.EnablePerformanceMonitoring();

// Production - enable selectively from config or toggle at runtime
using var prodFoundry = WorkflowForge.CreateFoundry("Prod");
prodFoundry.EnablePerformanceMonitoring();
```

## Examples & Documentation

- **[Complete Examples](../../samples/WorkflowForge.Samples.BasicConsole/README.md#17-performance-monitoring)** - Interactive performance monitoring samples
- **[Core Documentation](../../core/WorkflowForge/README.md)** - Core concepts
- **[Benchmark Results](../../benchmarks/WorkflowForge.Benchmarks/README.md)** - Performance benchmarks
- **[Main README](../../../README.md)** - Framework overview

---

*Comprehensive performance monitoring for workflows*
