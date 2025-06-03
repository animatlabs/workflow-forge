# WorkflowForge.Extensions.Observability.HealthChecks

Comprehensive health monitoring extension for WorkflowForge applications. Provides essential system health checks and monitoring capabilities with seamless foundry integration.

## 🎯 Extension Overview

The HealthChecks extension provides comprehensive health monitoring for WorkflowForge applications, including:

- **🔍 Built-in Health Checks**: Memory, garbage collection, and thread pool monitoring
- **🔧 Custom Health Checks**: Easy interface for creating application-specific checks  
- **⏱️ Periodic Monitoring**: Optional background health check execution
- **🎯 Simple Integration**: Minimal configuration required
- **🏭 Foundry Integration**: Works seamlessly with WorkflowForge foundries
- **📊 Detailed Reporting**: Rich health check results with performance data

## 📦 Installation

```bash
dotnet add package WorkflowForge.Extensions.Observability.HealthChecks
```

## 🚀 Quick Start

### 1. Enable Health Checks for Foundry

```csharp
using WorkflowForge;
using WorkflowForge.Extensions.Observability.HealthChecks;

// Enable health checks for a foundry
var foundry = WorkflowForge.CreateFoundry("MyWorkflow");
foundry.EnableHealthChecks();

// Or create with configuration
var foundryConfig = FoundryConfiguration.ForProduction()
    .EnableHealthChecks();

var foundry = WorkflowForge.CreateFoundryWithData("MyWorkflow", initialData, foundryConfig);
```

### 2. Execute Health Checks

```csharp
// Get health check service
var healthService = foundry.GetHealthCheckService();

// Check all health checks
var results = await healthService.CheckHealthAsync();

Console.WriteLine($"Overall Status: {results.Status}");
Console.WriteLine($"Total Checks: {results.Results.Count}");

// Check specific health checks
foreach (var (name, result) in results.Results)
{
    Console.WriteLine($"{name}: {result.Status} - {result.Description}");
}
```

### 3. Foundry Health Integration

```csharp
// Quick foundry health check
var overallStatus = await foundry.CheckFoundryHealthAsync();

// Check with custom service
var healthService = foundry.GetHealthCheckService();
var status = await foundry.CheckFoundryHealthAsync(healthService);
```

## 🔍 Built-in Health Checks

The extension automatically includes comprehensive system health checks:

### Memory Health Check
Monitors system and managed memory usage:

```csharp
// Health criteria:
// ✅ Healthy: < 500MB working set
// ⚠️ Degraded: 500MB - 1GB working set  
// ❌ Unhealthy: > 1GB working set

var memoryCheck = new MemoryHealthCheck();
var result = await memoryCheck.CheckHealthAsync();

// Result includes:
// - working_set_bytes: Total working set memory
// - managed_memory_bytes: .NET managed memory
// - working_set_mb: Working set in MB
// - managed_memory_mb: Managed memory in MB
```

### Garbage Collector Health Check
Monitors GC performance and pressure:

```csharp
// Health criteria:
// ✅ Healthy: < 5% Gen2 collections ratio
// ⚠️ Degraded: 5-10% Gen2 collections ratio
// ❌ Unhealthy: > 10% Gen2 collections ratio

var gcCheck = new GarbageCollectorHealthCheck();
var result = await gcCheck.CheckHealthAsync();

// Result includes:
// - gen0_collections: Generation 0 collection count
// - gen1_collections: Generation 1 collection count
// - gen2_collections: Generation 2 collection count
// - total_memory_bytes: Total allocated memory
```

### Thread Pool Health Check
Monitors thread pool availability and usage:

```csharp
// Health criteria:
// ✅ Healthy: < 75% worker threads busy
// ⚠️ Degraded: 75-90% worker threads busy
// ❌ Unhealthy: > 90% worker threads busy

var threadPoolCheck = new ThreadPoolHealthCheck();
var result = await threadPoolCheck.CheckHealthAsync();

// Result includes:
// - available_worker_threads: Available worker threads
// - max_worker_threads: Maximum worker threads
// - busy_worker_threads: Currently busy threads
// - worker_thread_usage_percent: Usage percentage
```

## 🔧 Custom Health Checks

Create application-specific health checks by implementing `IHealthCheck`:

```csharp
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDbConnection _connection;

    public string Name => "Database";
    public string Description => "Checks database connectivity and performance";

    public DatabaseHealthCheck(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Test database connectivity
            await _connection.OpenAsync(cancellationToken);
            
            // Test with simple query
            var result = await _connection.QuerySingleAsync<int>("SELECT 1", cancellationToken);
            
            stopwatch.Stop();
            
            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                return HealthCheckResult.Degraded(
                    $"Database responding slowly: {stopwatch.ElapsedMilliseconds}ms",
                    new Dictionary<string, object>
                    {
                        ["response_time_ms"] = stopwatch.ElapsedMilliseconds,
                        ["connection_state"] = _connection.State.ToString()
                    }
                );
            }
            
            return HealthCheckResult.Healthy(
                $"Database connection successful: {stopwatch.ElapsedMilliseconds}ms",
                new Dictionary<string, object>
                {
                    ["response_time_ms"] = stopwatch.ElapsedMilliseconds,
                    ["connection_state"] = _connection.State.ToString()
                }
            );
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Database connection failed",
                ex,
                new Dictionary<string, object>
                {
                    ["connection_state"] = _connection.State.ToString(),
                    ["error_type"] = ex.GetType().Name
                }
            );
        }
        finally
        {
            if (_connection.State == ConnectionState.Open)
                await _connection.CloseAsync();
        }
    }
}

// Register custom health check
var healthService = foundry.GetHealthCheckService();
healthService.RegisterHealthCheck(new DatabaseHealthCheck(dbConnection));
```

## 📊 Health Check Results

Each health check provides detailed diagnostic information:

```csharp
var results = await healthService.CheckHealthAsync();

Console.WriteLine($"Overall Status: {results.Status}");
Console.WriteLine($"Total Duration: {results.TotalDuration.TotalMilliseconds}ms");
Console.WriteLine($"Checked At: {results.CheckedAt}");

foreach (var (name, result) in results.Results)
{
    Console.WriteLine($"\n=== {name} Health Check ===");
    Console.WriteLine($"Status: {result.Status}");
    Console.WriteLine($"Description: {result.Description}");
    Console.WriteLine($"Duration: {result.Duration.TotalMilliseconds}ms");
    
    if (result.Exception != null)
    {
        Console.WriteLine($"Error: {result.Exception.Message}");
    }
    
    if (result.Data?.Any() == true)
    {
        Console.WriteLine("Additional Data:");
        foreach (var (key, value) in result.Data)
        {
            Console.WriteLine($"  {key}: {value}");
        }
    }
}
```

### Example Health Check Output

```
Overall Status: Healthy
Total Duration: 15ms
Checked At: 2024-06-03T10:30:45.123Z

=== Memory Health Check ===
Status: Healthy
Description: Memory usage normal: 245.3MB working set
Duration: 2ms
Additional Data:
  working_set_bytes: 257294336
  managed_memory_bytes: 89438720
  working_set_mb: 245.3
  managed_memory_mb: 85.3

=== GarbageCollector Health Check ===
Status: Healthy
Description: GC performance normal: 5 Gen2 collections
Duration: 1ms
Additional Data:
  gen0_collections: 127
  gen1_collections: 23
  gen2_collections: 5
  total_memory_bytes: 89438720
  gen2_collection_ratio: 0.039

=== ThreadPool Health Check ===
Status: Healthy
Description: Thread pool healthy: 12.5% worker threads busy
Duration: 0ms
Additional Data:
  available_worker_threads: 2043
  max_worker_threads: 2047
  busy_worker_threads: 4
  worker_thread_usage_percent: 12.5
```

## ⏱️ Periodic Health Monitoring

Enable automatic background health monitoring:

```csharp
// Create health service with periodic checks every 30 seconds
var healthService = foundry.CreateHealthCheckService(TimeSpan.FromSeconds(30));

// Access the latest results anytime
var lastResults = healthService.LastResults;
var currentStatus = healthService.OverallStatus;

// Stop periodic monitoring when done
healthService.StopPeriodicChecks();
```

## 🏭 Workflow Integration

Monitor system health during workflow execution:

```csharp
public class HealthAwareOperation : IWorkflowOperation
{
    public string Name => "HealthAwareProcessing";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        // Check system health before critical operations
        var healthService = foundry.GetHealthCheckService();
        var healthResults = await healthService.CheckHealthAsync(cancellationToken);
        
        if (healthResults.Status == HealthStatus.Unhealthy)
        {
            foundry.Logger.LogError("System health check failed - aborting operation. Status: {Status}", 
                healthResults.Status);
            throw new InvalidOperationException("System health check failed - operation aborted");
        }
        
        if (healthResults.Status == HealthStatus.Degraded)
        {
            foundry.Logger.LogWarning("System health degraded - proceeding with caution. Status: {Status}",
                healthResults.Status);
        }

        // Log health status
        foundry.Logger.LogInformation("System health check passed. Status: {Status}, Duration: {Duration}ms",
            healthResults.Status, healthResults.TotalDuration.TotalMilliseconds);

        // Proceed with operation
        return await ProcessDataAsync(inputData, cancellationToken);
    }
}
```

## 🎯 Configuration Options

### Health Check Service Configuration

```csharp
// Create health service with custom configuration
var options = new HealthCheckServiceOptions
{
    PeriodicCheckInterval = TimeSpan.FromMinutes(2),
    EnableDetailedLogging = true,
    IncludeExceptionDetails = false,
    CheckTimeout = TimeSpan.FromSeconds(10)
};

var healthService = foundry.CreateHealthCheckService(options);
```

### Foundry Configuration Integration

```csharp
// Configure health checks in foundry configuration
var foundryConfig = FoundryConfiguration.ForProduction()
    .EnableHealthChecks(options => 
    {
        options.PeriodicCheckInterval = TimeSpan.FromMinutes(1);
        options.EnableDetailedLogging = true;
        options.RegisterCustomChecks = services =>
        {
            services.Add(new DatabaseHealthCheck(connectionString));
            services.Add(new ExternalServiceHealthCheck("PaymentAPI", apiEndpoint));
        };
    });
```

## 🎯 Best Practices

### 1. Health Check Frequency
```csharp
// ✅ Good: Reasonable intervals
var healthService = foundry.CreateHealthCheckService(TimeSpan.FromMinutes(1));

// ❌ Avoid: Too frequent checks
var healthService = foundry.CreateHealthCheckService(TimeSpan.FromSeconds(5));
```

### 2. Custom Health Check Design
```csharp
// ✅ Good: Include performance data
return HealthCheckResult.Healthy("Service responding", new Dictionary<string, object>
{
    ["response_time_ms"] = responseTime,
    ["endpoint"] = endpoint,
    ["status_code"] = 200
});

// ❌ Avoid: Vague results
return HealthCheckResult.Healthy("OK");
```

### 3. Exception Handling
```csharp
// ✅ Good: Catch specific exceptions
try
{
    await CheckServiceAsync();
    return HealthCheckResult.Healthy("Service available");
}
catch (TimeoutException ex)
{
    return HealthCheckResult.Degraded("Service timeout", ex);
}
catch (Exception ex)
{
    return HealthCheckResult.Unhealthy("Service unavailable", ex);
}
```

## 🔗 Integration with Other Extensions

### With Performance Monitoring
```csharp
var foundryConfig = FoundryConfiguration.ForProduction()
    .EnableHealthChecks()
    .EnablePerformanceMonitoring();

// Monitor both health and performance
var healthResults = await foundry.CheckFoundryHealthAsync();
var perfStats = foundry.GetPerformanceStatistics();
```

### With OpenTelemetry
```csharp
var foundryConfig = FoundryConfiguration.ForProduction()
    .EnableHealthChecks()
    .EnableOpenTelemetry("MyService", "1.0.0");

// Health checks will be traced in OpenTelemetry
using var activity = foundry.StartActivity("HealthCheck");
var results = await foundry.CheckFoundryHealthAsync();
```

## 📚 Additional Resources

- [Core Framework Documentation](../WorkflowForge/README.md)
- [Performance Monitoring Extension](../WorkflowForge.Extensions.Observability.Performance/README.md)
- [OpenTelemetry Extension](../WorkflowForge.Extensions.Observability.OpenTelemetry/README.md)
- [Main Project Documentation](../../README.md)

---

**WorkflowForge.Extensions.Observability.HealthChecks** - *Keep your workflows healthy* 