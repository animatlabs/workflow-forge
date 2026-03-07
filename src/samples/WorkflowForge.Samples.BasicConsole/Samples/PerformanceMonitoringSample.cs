using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Extensions.Observability.Performance;
using WorkflowForge.Extensions.Observability.Performance.Abstractions;
using WorkflowForge.Operations;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates performance monitoring capabilities for tracking workflow metrics.
/// Shows how to enable performance monitoring and collect performance statistics.
/// </summary>
public class PerformanceMonitoringSample : ISample
{
    public string Name => "Performance Monitoring";
    public string Description => "Performance metrics tracking and monitoring for workflow optimization";

    public async Task RunAsync()
    {
        Console.WriteLine("Demonstrating WorkflowForge performance monitoring capabilities...");

        // Scenario 1: Basic performance monitoring
        await RunBasicPerformanceMonitoringDemo();

        // Scenario 2: Performance statistics collection
        await RunPerformanceStatisticsDemo();

        // Scenario 3: Performance-aware operations
        await RunPerformanceAwareOperationsDemo();
    }

    private static async Task RunBasicPerformanceMonitoringDemo()
    {
        Console.WriteLine("\n--- Basic Performance Monitoring Demo ---");

        using var foundry = WorkflowForge.CreateFoundry("PerformanceMonitoringDemo");

        // Enable performance monitoring
        var monitoringEnabled = foundry.EnablePerformanceMonitoring();
        Console.WriteLine($"   Performance monitoring enabled: {monitoringEnabled}");

        foundry
            .WithOperation(LoggingOperation.Info("Starting performance monitoring demonstration"))
            .WithOperation(new PerformanceTestOperation("FastOperation", TimeSpan.FromMilliseconds(100)))
            .WithOperation(new PerformanceTestOperation("MediumOperation", TimeSpan.FromMilliseconds(250)))
            .WithOperation(new PerformanceTestOperation("SlowOperation", TimeSpan.FromMilliseconds(400)))
            .WithOperation(LoggingOperation.Info("Performance monitoring demonstration completed"));

        await foundry.ForgeAsync();

        // Get performance statistics
        var stats = foundry.GetPerformanceStatistics();
        if (stats != null)
        {
            Console.WriteLine($"   Total operations executed: {stats.TotalOperations}");
            Console.WriteLine($"   Average execution time: {stats.AverageDuration.TotalMilliseconds:F2}ms");
            Console.WriteLine($"   Total memory allocated: {stats.TotalMemoryAllocated / (1024 * 1024):F2} MB");
        }
        else
        {
            Console.WriteLine("   Performance statistics not available");
        }
    }

    private static async Task RunPerformanceStatisticsDemo()
    {
        Console.WriteLine("\n--- Performance Statistics Demo ---");

        using var foundry = WorkflowForge.CreateFoundry("PerformanceStatisticsDemo");

        // Enable performance monitoring
        foundry.EnablePerformanceMonitoring();

        foundry.SetProperty("batch_size", 5);
        foundry.SetProperty("iteration", 0);

        foundry
            .WithOperation(LoggingOperation.Info("Starting performance statistics collection"))
            .WithOperation(new StatisticsAwareOperation("IterativeProcessor"))
            .WithOperation(new StatisticsAwareOperation("DataAggregator"))
            .WithOperation(new StatisticsAwareOperation("ReportGenerator"))
            .WithOperation(LoggingOperation.Info("Performance statistics collection completed"));

        await foundry.ForgeAsync();

        // Display detailed statistics
        var stats = foundry.GetPerformanceStatistics();
        if (stats != null)
        {
            Console.WriteLine($"   Total operations: {stats.TotalOperations}");
            Console.WriteLine($"   Average duration: {stats.AverageDuration.TotalMilliseconds:F2}ms");
            Console.WriteLine($"   Total memory allocated: {stats.TotalMemoryAllocated / (1024 * 1024):F2} MB");
        }
    }

    private static async Task RunPerformanceAwareOperationsDemo()
    {
        Console.WriteLine("\n--- Performance-Aware Operations Demo ---");

        using var foundry = WorkflowForge.CreateFoundry("PerformanceAwareDemo");

        // Enable performance monitoring
        foundry.EnablePerformanceMonitoring();

        foundry.SetProperty("performance_threshold_ms", 200);
        foundry.SetProperty("optimization_enabled", true);

        foundry
            .WithOperation(LoggingOperation.Info("Starting performance-aware operations demonstration"))
            .WithOperation(new PerformanceOptimizedOperation("DatabaseQuery", TimeSpan.FromMilliseconds(150)))
            .WithOperation(new PerformanceOptimizedOperation("DataTransformation", TimeSpan.FromMilliseconds(300)))
            .WithOperation(new PerformanceOptimizedOperation("ValidationCheck", TimeSpan.FromMilliseconds(75)))
            .WithOperation(new PerformanceOptimizedOperation("ResultSerialization", TimeSpan.FromMilliseconds(180)))
            .WithOperation(LoggingOperation.Info("Performance-aware operations demonstration completed"));

        await foundry.ForgeAsync();

        // Performance summary
        var stats = foundry.GetPerformanceStatistics();
        var optimizationCount = foundry.GetPropertyOrDefault<int>("optimizations_applied", 0);
        var thresholdExceeded = foundry.GetPropertyOrDefault<int>("threshold_exceeded", 0);

        Console.WriteLine($"   Operations optimized: {optimizationCount}");
        Console.WriteLine($"   Performance threshold exceeded: {thresholdExceeded} times");

        if (stats != null)
        {
            Console.WriteLine($"   Overall performance score: {CalculatePerformanceScore(stats):F1}/10.0");
        }
    }

    private static double CalculatePerformanceScore(IFoundryPerformanceStatistics stats)
    {
        // Simple performance scoring algorithm
        var baseScore = 10.0;

        // Deduct points for slow operations
        if (stats.AverageDuration.TotalMilliseconds > 200)
            baseScore -= 2.0;
        else if (stats.AverageDuration.TotalMilliseconds > 100)
            baseScore -= 1.0;

        // Deduct points for high memory usage (assuming baseline of 50MB)
        var memoryMB = stats.TotalMemoryAllocated / (1024 * 1024);
        if (memoryMB > 100)
            baseScore -= 2.0;
        else if (memoryMB > 75)
            baseScore -= 1.0;

        // Deduct points for low success rate
        if (stats.SuccessRate < 0.95)
            baseScore -= 3.0;
        else if (stats.SuccessRate < 0.99)
            baseScore -= 1.0;

        return Math.Max(0, baseScore);
    }
}

/// <summary>
/// Operation for testing performance monitoring
/// </summary>
public class PerformanceTestOperation : WorkflowOperationBase
{
    private readonly string _operationName;
    private readonly TimeSpan _executionTime;

    public PerformanceTestOperation(string operationName, TimeSpan executionTime)
    {
        _operationName = operationName;
        _executionTime = executionTime;
    }

    public override string Name => _operationName;

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        foundry.Logger.LogInformation("Starting performance test: {OperationName} (Expected: {ExecutionTime}ms)",
            _operationName, _executionTime.TotalMilliseconds);

        var startTime = DateTime.UtcNow;

        // Simulate work
        await Task.Delay(_executionTime, cancellationToken);

        var actualDuration = DateTime.UtcNow - startTime;

        var result = new
        {
            OperationName = _operationName,
            ExpectedDuration = _executionTime,
            ActualDuration = actualDuration,
            PerformanceVariance = Math.Abs((actualDuration - _executionTime).TotalMilliseconds),
            CompletedAt = DateTime.UtcNow
        };

        foundry.Logger.LogInformation("Performance test completed: {OperationName}, Actual: {ActualDuration}ms",
            _operationName, actualDuration.TotalMilliseconds);

        return result;
    }
}

/// <summary>
/// Operation that collects and reports on performance statistics
/// </summary>
public class StatisticsAwareOperation : WorkflowOperationBase
{
    private readonly string _operationName;

    public StatisticsAwareOperation(string operationName)
    {
        _operationName = operationName;
    }

    public override string Name => _operationName;

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        foundry.Logger.LogInformation("Starting statistics-aware operation: {OperationName}", _operationName);

        // Get current performance statistics before operation
        var statsBefore = foundry.GetPerformanceStatistics();

        // Simulate variable work based on operation type
        var workDuration = _operationName switch
        {
            "IterativeProcessor" => TimeSpan.FromMilliseconds(200),
            "DataAggregator" => TimeSpan.FromMilliseconds(150),
            "ReportGenerator" => TimeSpan.FromMilliseconds(300),
            _ => TimeSpan.FromMilliseconds(100)
        };

        await Task.Delay(workDuration, cancellationToken);

        // Get performance statistics after operation
        var statsAfter = foundry.GetPerformanceStatistics();

        var result = new
        {
            OperationName = _operationName,
            WorkDuration = workDuration,
            StatsBefore = statsBefore != null ? new
            {
                TotalOps = statsBefore.TotalOperations,
                AvgTime = statsBefore.AverageDuration.TotalMilliseconds
            } : null,
            StatsAfter = statsAfter != null ? new
            {
                TotalOps = statsAfter.TotalOperations,
                AvgTime = statsAfter.AverageDuration.TotalMilliseconds
            } : null,
            CompletedAt = DateTime.UtcNow
        };

        foundry.Logger.LogInformation("Statistics-aware operation completed: {OperationName}", _operationName);

        return result;
    }
}

/// <summary>
/// Operation that optimizes its behavior based on performance metrics
/// </summary>
public class PerformanceOptimizedOperation : WorkflowOperationBase
{
    private readonly string _operationName;
    private readonly TimeSpan _baseExecutionTime;

    public PerformanceOptimizedOperation(string operationName, TimeSpan baseExecutionTime)
    {
        _operationName = operationName;
        _baseExecutionTime = baseExecutionTime;
    }

    public override string Name => _operationName;

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        foundry.Logger.LogInformation("Starting performance-optimized operation: {OperationName}", _operationName);

        var thresholdMs = foundry.GetPropertyOrDefault<int>("performance_threshold_ms", 200);
        var optimizationEnabled = foundry.GetPropertyOrDefault<bool>("optimization_enabled", false);

        var executionTime = _baseExecutionTime;
        var optimizationApplied = false;

        // Apply optimization if enabled and operation is expected to exceed threshold
        if (optimizationEnabled && _baseExecutionTime.TotalMilliseconds > thresholdMs)
        {
            // Simulate optimization by reducing execution time by 25%
            executionTime = TimeSpan.FromMilliseconds(_baseExecutionTime.TotalMilliseconds * 0.75);
            optimizationApplied = true;

            var optimizationCount = foundry.GetPropertyOrDefault<int>("optimizations_applied", 0);
            foundry.SetProperty("optimizations_applied", optimizationCount + 1);

            foundry.Logger.LogInformation("Performance optimization applied to {OperationName}: {OriginalTime}ms -> {OptimizedTime}ms",
                _operationName, _baseExecutionTime.TotalMilliseconds, executionTime.TotalMilliseconds);
        }

        // Track threshold exceeded
        if (_baseExecutionTime.TotalMilliseconds > thresholdMs)
        {
            var thresholdCount = foundry.GetPropertyOrDefault<int>("threshold_exceeded", 0);
            foundry.SetProperty("threshold_exceeded", thresholdCount + 1);
        }

        var startTime = DateTime.UtcNow;
        await Task.Delay(executionTime, cancellationToken);
        var actualDuration = DateTime.UtcNow - startTime;

        var result = new
        {
            OperationName = _operationName,
            BaseExecutionTime = _baseExecutionTime,
            OptimizedExecutionTime = executionTime,
            ActualExecutionTime = actualDuration,
            OptimizationApplied = optimizationApplied,
            PerformanceGain = optimizationApplied ?
                (_baseExecutionTime.TotalMilliseconds - executionTime.TotalMilliseconds) : 0,
            CompletedAt = DateTime.UtcNow
        };

        foundry.Logger.LogInformation("Performance-optimized operation completed: {OperationName}, Actual: {ActualDuration}ms, Optimized: {OptimizationApplied}",
            _operationName, actualDuration.TotalMilliseconds, optimizationApplied);

        return result;
    }
}