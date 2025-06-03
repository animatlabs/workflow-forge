# WorkflowForge.Extensions.Observability.Performance

Comprehensive performance monitoring extension for WorkflowForge that provides detailed metrics and analytics for workflow operations and workflow execution with professional performance analytics.

## 🎯 Extension Overview

The Performance extension brings comprehensive performance monitoring to WorkflowForge applications, including:

- **📊 Foundry Performance Statistics**: Comprehensive metrics for workflow execution including success rates, timing, memory usage, and throughput
- **⚙️ Operation-level Metrics**: Detailed statistics for individual operations within workflows
- **🔧 Extension-based Design**: Performance monitoring is opt-in and doesn't affect core framework performance
- **💾 Memory Efficient**: Uses property-based storage to avoid interface pollution
- **📈 Real-time Monitoring**: Live performance data collection and reporting
- **🎯 Detailed Analytics**: Success rates, timing distributions, memory allocations, and error patterns
- **🔗 Integration Ready**: Seamless integration with other WorkflowForge extensions
- **🏭 Foundry Integration**: Deep integration with WorkflowForge foundries and operations

## 📦 Installation

```bash
dotnet add package WorkflowForge.Extensions.Observability.Performance
```

## 🚀 Quick Start

### 1. Enable Performance Monitoring

```csharp
using WorkflowForge;
using WorkflowForge.Extensions.Observability.Performance;

// Option 1: Enable via foundry configuration
var foundryConfig = FoundryConfiguration.ForProduction()
    .EnablePerformanceMonitoring();

var foundry = WorkflowForge.CreateFoundry("MyWorkflow", foundryConfig);

// Option 2: Enable on existing foundry
var foundry = WorkflowForge.CreateFoundry("MyWorkflow");
if (foundry.EnablePerformanceMonitoring())
{
    // Performance monitoring is now active
}
```

### 2. Execute Workflows with Monitoring

```csharp
var workflow = WorkflowForge.CreateWorkflow("OrderProcessing")
    .AddOperation("ValidateOrder", async (order, foundry, ct) => {
        // Your operation logic
        return await ValidateOrderAsync(order, ct);
    })
    .AddOperation("ProcessPayment", async (order, foundry, ct) => {
        // Your operation logic  
        return await ProcessPaymentAsync(order, ct);
    })
    .AddOperation("FulfillOrder", async (payment, foundry, ct) => {
        // Your operation logic
        return await FulfillOrderAsync(payment, ct);
    })
    .Build();

var smith = WorkflowForge.CreateSmith();

// Execute multiple workflow instances for meaningful statistics
for (int i = 0; i < 100; i++)
{
    var order = GenerateTestOrder(i);
    foundry.Properties["order"] = order; // Set order data in foundry
    await smith.ForgeAsync(workflow, foundry);
}
```

### 3. Access Performance Statistics

```csharp
// Get comprehensive performance statistics
var stats = foundry.GetPerformanceStatistics();
if (stats != null)
{
    Console.WriteLine($"Total Operations: {stats.TotalOperations}");
    Console.WriteLine($"Success Rate: {stats.SuccessRate:P2}");
    Console.WriteLine($"Average Duration: {stats.AverageDuration}ms");
    Console.WriteLine($"Operations/sec: {stats.OperationsPerSecond:F2}");
    Console.WriteLine($"Memory Allocated: {stats.TotalMemoryAllocated / 1024 / 1024:F2} MB");
    
    // Get operation-specific statistics
    foreach (var opStats in stats.GetAllOperationStatistics())
    {
        Console.WriteLine($"Operation '{opStats.OperationName}': {opStats.SuccessRate:P2} success rate, {opStats.AverageExecutionTime}ms avg time");
    }
}
```

## 📊 Comprehensive Performance Metrics

### Foundry-Level Metrics

```csharp
var stats = foundry.GetPerformanceStatistics();

// Execution Statistics
Console.WriteLine($"Total Executions: {stats.TotalOperations}");
Console.WriteLine($"Successful: {stats.SuccessfulOperations}");
Console.WriteLine($"Failed: {stats.FailedOperations}");
Console.WriteLine($"Success Rate: {stats.SuccessRate:P2}");

// Timing Statistics  
Console.WriteLine($"Average Duration: {stats.AverageDuration}ms");
Console.WriteLine($"Min Duration: {stats.MinDuration}ms");
Console.WriteLine($"Max Duration: {stats.MaxDuration}ms");
Console.WriteLine($"95th Percentile: {stats.GetPercentile(95)}ms");
Console.WriteLine($"99th Percentile: {stats.GetPercentile(99)}ms");

// Throughput Metrics
Console.WriteLine($"Operations per Second: {stats.OperationsPerSecond:F2}");
Console.WriteLine($"Peak Throughput: {stats.PeakOperationsPerSecond:F2}");

// Memory Statistics
Console.WriteLine($"Total Memory Allocated: {stats.TotalMemoryAllocated / 1024 / 1024:F2} MB");
Console.WriteLine($"Average Memory per Operation: {stats.AverageMemoryPerOperation / 1024:F2} KB");
Console.WriteLine($"Peak Memory Usage: {stats.PeakMemoryUsage / 1024 / 1024:F2} MB");

// Error Analysis
Console.WriteLine($"Error Rate: {stats.ErrorRate:P2}");
var errorBreakdown = stats.GetErrorBreakdown();
foreach (var (errorType, count) in errorBreakdown)
{
    Console.WriteLine($"  {errorType}: {count} occurrences");
}
```

### Operation-Level Metrics

```csharp
var stats = foundry.GetPerformanceStatistics();

foreach (var opStats in stats.GetAllOperationStatistics())
{
    Console.WriteLine($"\n=== Operation: {opStats.OperationName} ===");
    Console.WriteLine($"Executions: {opStats.ExecutionCount}");
    Console.WriteLine($"Success Rate: {opStats.SuccessRate:P2}");
    Console.WriteLine($"Average Time: {opStats.AverageExecutionTime}ms");
    Console.WriteLine($"Min Time: {opStats.MinExecutionTime}ms");
    Console.WriteLine($"Max Time: {opStats.MaxExecutionTime}ms");
    Console.WriteLine($"Memory per Execution: {opStats.AverageMemoryUsage / 1024:F2} KB");
    
    // Operation-specific percentiles
    Console.WriteLine($"50th Percentile: {opStats.GetPercentile(50)}ms");
    Console.WriteLine($"95th Percentile: {opStats.GetPercentile(95)}ms");
    Console.WriteLine($"99th Percentile: {opStats.GetPercentile(99)}ms");
    
    // Error analysis for this operation
    if (opStats.ErrorCount > 0)
    {
        Console.WriteLine($"Errors: {opStats.ErrorCount} ({opStats.ErrorRate:P2})");
        var errors = opStats.GetErrorBreakdown();
        foreach (var (errorType, count) in errors)
        {
            Console.WriteLine($"  {errorType}: {count}");
        }
    }
}
```

## 🔧 Advanced Configuration

### Performance Monitoring Configuration

```csharp
// Create foundry with performance monitoring configuration
var performanceConfig = new PerformanceMonitoringConfiguration
{
    EnableDetailedOperationStats = true,
    EnableMemoryTracking = true,
    EnableThroughputTracking = true,
    SampleRate = 1.0, // Monitor 100% of operations
    RetentionPeriod = TimeSpan.FromHours(24),
    EnablePercentileCalculation = true,
    MaxOperationHistory = 10000
};

var foundryConfig = FoundryConfiguration.ForProduction()
    .EnablePerformanceMonitoring(performanceConfig);

var foundry = WorkflowForge.CreateFoundry("AdvancedMonitoring", foundryConfig);
```

### Environment-Specific Monitoring

```csharp
// Development environment - detailed monitoring
var devFoundryConfig = FoundryConfiguration.ForDevelopment()
    .EnablePerformanceMonitoring(options =>
    {
        options.EnableDetailedOperationStats = true;
        options.EnableMemoryTracking = true;
        options.SampleRate = 1.0; // Monitor everything in dev
        options.EnableVerboseLogging = true;
    });

// Production environment - optimized monitoring
var prodFoundryConfig = FoundryConfiguration.ForProduction()
    .EnablePerformanceMonitoring(options =>
    {
        options.EnableDetailedOperationStats = true;
        options.EnableMemoryTracking = false; // Reduce overhead
        options.SampleRate = 0.1; // Sample 10% for performance
        options.EnableVerboseLogging = false;
    });

// Enterprise environment - comprehensive monitoring
var enterpriseFoundryConfig = FoundryConfiguration.ForEnterprise()
    .EnablePerformanceMonitoring(options =>
    {
        options.EnableDetailedOperationStats = true;
        options.EnableMemoryTracking = true;
        options.EnableThroughputTracking = true;
        options.EnablePercentileCalculation = true;
        options.SampleRate = 1.0;
        options.RetentionPeriod = TimeSpan.FromDays(7);
    });
```

## 📈 Real-time Performance Monitoring

### Continuous Performance Tracking

```csharp
public class PerformanceMonitoringService
{
    private readonly IWorkflowFoundry _foundry;
    private readonly Timer _monitoringTimer;

    public PerformanceMonitoringService(IWorkflowFoundry foundry)
    {
        _foundry = foundry;
        
        // Monitor performance every 30 seconds
        _monitoringTimer = new Timer(MonitorPerformance, null, 
            TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }

    private void MonitorPerformance(object? state)
    {
        var stats = _foundry.GetPerformanceStatistics();
        if (stats == null) return;

        // Check for performance degradation
        if (stats.SuccessRate < 0.95) // Below 95% success rate
        {
            Alert($"High failure rate detected: {stats.SuccessRate:P2}");
        }

        if (stats.AverageDuration > 5000) // Slower than 5 seconds
        {
            Alert($"Performance degradation detected: {stats.AverageDuration}ms average");
        }

        if (stats.OperationsPerSecond < 10) // Low throughput
        {
            Alert($"Low throughput detected: {stats.OperationsPerSecond:F2} ops/sec");
        }

        // Log current performance
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Success: {stats.SuccessRate:P2}, " +
                         $"Avg: {stats.AverageDuration}ms, " +
                         $"Throughput: {stats.OperationsPerSecond:F2} ops/sec");
    }

    private void Alert(string message)
    {
        Console.WriteLine($"⚠️ ALERT: {message}");
        // Send to monitoring system, Slack, etc.
    }
}
```

### Performance Benchmarking

```csharp
public class WorkflowPerformanceBenchmark
{
    public async Task<BenchmarkResult> BenchmarkWorkflowAsync(
        IWorkflow workflow, 
        object inputData, 
        int iterations = 1000)
    {
        var foundry = WorkflowForge.CreateFoundry("Benchmark");
        foundry.EnablePerformanceMonitoring();
        
        var smith = WorkflowForge.CreateSmith();
        var stopwatch = Stopwatch.StartNew();

        // Warm up
        for (int i = 0; i < 10; i++)
        {
            await smith.ForgeAsync(workflow, foundry);
        }

        // Reset stats after warmup
        foundry.ResetPerformanceStatistics();
        
        // Benchmark
        var benchmarkStart = DateTime.UtcNow;
        for (int i = 0; i < iterations; i++)
        {
            await smith.ForgeAsync(workflow, foundry);
        }
        stopwatch.Stop();

        var stats = foundry.GetPerformanceStatistics()!;
        
        return new BenchmarkResult
        {
            TotalIterations = iterations,
            TotalDuration = stopwatch.Elapsed,
            SuccessRate = stats.SuccessRate,
            AverageOperationTime = stats.AverageDuration,
            ThroughputPerSecond = stats.OperationsPerSecond,
            MemoryPerOperation = stats.AverageMemoryPerOperation,
            OperationBreakdown = stats.GetAllOperationStatistics().ToList()
        };
    }
}
```

## 🔗 Integration with Other Extensions

### With Logging (Serilog)

```csharp
var foundryConfig = FoundryConfiguration.ForProduction()
    .UseSerilog()
    .EnablePerformanceMonitoring();

var foundry = WorkflowForge.CreateFoundry("LoggedWorkflow", foundryConfig);

// Performance stats will be automatically logged
var stats = foundry.GetPerformanceStatistics();
foundry.Logger.LogInformation("Workflow performance: {SuccessRate:P2} success rate, {AvgDuration}ms average, {Throughput:F2} ops/sec", 
    stats.SuccessRate, stats.AverageDuration, stats.OperationsPerSecond);
```

### With OpenTelemetry

```csharp
var foundryConfig = FoundryConfiguration.ForProduction()
    .EnablePerformanceMonitoring()
    .EnableOpenTelemetry("OrderService", "1.0.0");

var foundry = WorkflowForge.CreateFoundry("TracedWorkflow", foundryConfig);

// Performance metrics will be exported to OpenTelemetry
using var activity = foundry.StartActivity("WorkflowExecution");
var stats = foundry.GetPerformanceStatistics();
activity.SetTag("workflow.success_rate", stats.SuccessRate.ToString("P2"));
activity.SetTag("workflow.avg_duration", stats.AverageDuration.ToString());
activity.SetTag("workflow.throughput", stats.OperationsPerSecond.ToString("F2"));
```

### With Health Checks

```csharp
var foundryConfig = FoundryConfiguration.ForProduction()
    .EnablePerformanceMonitoring()
    .EnableHealthChecks();

var foundry = WorkflowForge.CreateFoundry("HealthyWorkflow", foundryConfig);

// Create performance-based health check
public class PerformanceHealthCheck : IHealthCheck
{
    private readonly IWorkflowFoundry _foundry;

    public PerformanceHealthCheck(IWorkflowFoundry foundry)
    {
        _foundry = foundry;
    }

    public string Name => "WorkflowPerformance";
    public string Description => "Monitors workflow performance metrics";

    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stats = _foundry.GetPerformanceStatistics();
        if (stats == null)
        {
            return HealthCheckResult.Healthy("Performance monitoring not enabled");
        }

        var data = new Dictionary<string, object>
        {
            ["success_rate"] = stats.SuccessRate,
            ["avg_duration_ms"] = stats.AverageDuration,
            ["operations_per_sec"] = stats.OperationsPerSecond,
            ["total_operations"] = stats.TotalOperations
        };

        if (stats.SuccessRate < 0.95)
        {
            return HealthCheckResult.Unhealthy($"Low success rate: {stats.SuccessRate:P2}", null, data);
        }

        if (stats.AverageDuration > 5000)
        {
            return HealthCheckResult.Degraded($"High average duration: {stats.AverageDuration}ms", data);
        }

        return HealthCheckResult.Healthy($"Performance healthy: {stats.SuccessRate:P2} success rate", data);
    }
}
```

## ⚙️ Advanced Foundry Implementation

For custom foundry implementations that support performance monitoring:

```csharp
using WorkflowForge.Extensions.Observability.Performance;

public class PerformanceAwareFoundry : IWorkflowFoundry, IPerformanceMonitoredFoundry
{
    private readonly ConcurrentDictionary<string, object?> _properties = new();
    private IFoundryPerformanceStatistics? _performanceStats;
    private bool _performanceEnabled;

    public bool IsPerformanceMonitoringEnabled => _performanceEnabled;

    public IFoundryPerformanceStatistics? GetPerformanceStatistics()
    {
        return _performanceEnabled ? _performanceStats : null;
    }

    public bool EnablePerformanceMonitoring()
    {
        if (!_performanceEnabled)
        {
            _performanceStats = new FoundryPerformanceStatistics();
            _performanceEnabled = true;
            Logger.LogInformation("Performance monitoring enabled for foundry {FoundryId}", ExecutionId);
        }
        return true;
    }

    public bool DisablePerformanceMonitoring()
    {
        if (_performanceEnabled)
        {
            _performanceEnabled = false;
            _performanceStats = null;
            Logger.LogInformation("Performance monitoring disabled for foundry {FoundryId}", ExecutionId);
        }
        return true;
    }

    public void ResetPerformanceStatistics()
    {
        if (_performanceEnabled && _performanceStats != null)
        {
            _performanceStats.Reset();
            Logger.LogInformation("Performance statistics reset for foundry {FoundryId}", ExecutionId);
        }
    }

    // IWorkflowFoundry implementation
    public Guid ExecutionId { get; } = Guid.NewGuid();
    public IWorkflow? CurrentWorkflow { get; private set; }
    public ConcurrentDictionary<string, object?> Properties => _properties;
    public IWorkflowForgeLogger Logger { get; } = new DefaultWorkflowForgeLogger();
    public IServiceProvider? ServiceProvider { get; }

    public void SetCurrentWorkflow(IWorkflow? workflow) => CurrentWorkflow = workflow;
    public void AddOperation(IWorkflowOperation operation) { /* Implementation */ }
    public void Dispose() => DisablePerformanceMonitoring();
}
```

## 🎯 Best Practices

### 1. Sampling for Production
```csharp
// ✅ Good: Use sampling in high-throughput production environments
var prodConfig = FoundryConfiguration.ForProduction()
    .EnablePerformanceMonitoring(options =>
    {
        options.SampleRate = 0.1; // Monitor 10% of operations
        options.EnableMemoryTracking = false; // Reduce overhead
    });

// ❌ Avoid: Full monitoring in high-volume production
var badConfig = FoundryConfiguration.ForProduction()
    .EnablePerformanceMonitoring(options =>
    {
        options.SampleRate = 1.0; // 100% can impact performance
        options.EnableMemoryTracking = true; // Additional overhead
    });
```

### 2. Appropriate Retention Periods
```csharp
// ✅ Good: Reasonable retention based on environment
var devConfig = FoundryConfiguration.ForDevelopment()
    .EnablePerformanceMonitoring(options =>
    {
        options.RetentionPeriod = TimeSpan.FromHours(2); // Short for dev
    });

var prodConfig = FoundryConfiguration.ForProduction()
    .EnablePerformanceMonitoring(options =>
    {
        options.RetentionPeriod = TimeSpan.FromDays(1); // Longer for prod analysis
    });
```

### 3. Performance-Based Alerting
```csharp
// ✅ Good: Set appropriate thresholds based on your SLAs
private void MonitorPerformance(IFoundryPerformanceStatistics stats)
{
    if (stats.SuccessRate < 0.99) // 99% SLA
    {
        AlertCritical($"SLA breach: {stats.SuccessRate:P2} success rate");
    }
    
    if (stats.GetPercentile(95) > 2000) // 95th percentile under 2s
    {
        AlertWarning($"Latency SLA breach: {stats.GetPercentile(95)}ms p95");
    }
}
```

## 📚 Additional Resources

- [Core Framework Documentation](../WorkflowForge/README.md)
- [Health Checks Extension](../WorkflowForge.Extensions.Observability.HealthChecks/README.md)
- [OpenTelemetry Extension](../WorkflowForge.Extensions.Observability.OpenTelemetry/README.md)
- [Serilog Logging Extension](../WorkflowForge.Extensions.Logging.Serilog/README.md)
- [Main Project Documentation](../../README.md)
- [Performance Benchmarks](../../benchmarks/WorkflowForge.Benchmarks/README.md)

---

**WorkflowForge.Extensions.Observability.Performance** - *Comprehensive performance monitoring for workflows*
