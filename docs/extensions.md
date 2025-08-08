# WorkflowForge Extension System

WorkflowForge follows an extension-first architecture where the core library provides minimal functionality, and rich features are delivered through a comprehensive extension ecosystem.

## Extension Architecture

### Core Principles

1. **Dependency-Free Core** - The core library has zero dependencies
2. **Optional Extensions** - Add only what you need
3. **Composable Features** - Extensions work together seamlessly
4. **Configuration-Driven** - Extensions are configured, not hard-coded
5. **Enterprise-Ready** - Professional features for production use

### Extension Categories

```
WorkflowForge Extensions
├── Logging
│   └── Serilog Integration
├── Resilience
│   ├── Basic Patterns
│   └── Polly Integration
└── Observability
    ├── Performance Monitoring
    ├── Health Checks
    └── OpenTelemetry Integration
```

## Available Extensions

### Logging Extensions

#### WorkflowForge.Extensions.Logging.Serilog

Professional structured logging with Serilog integration.

**Installation:**
```bash
dotnet add package WorkflowForge.Extensions.Logging.Serilog
```

**Features:**
- Structured logging with rich context
- Correlation ID tracking
- Property enrichment
- Scope management
- Multiple output targets (Console, File, Database, etc.)

**Usage:**
```csharp
using Serilog;
using WorkflowForge.Extensions.Logging.Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/workflow-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.WithProperty("Application", "WorkflowApp")
    .CreateLogger();

// Bind via configuration and create foundry
var config = FoundryConfiguration.ForProduction().UseSerilog(Log.Logger);
var foundry = WorkflowForge.CreateFoundry("ProcessOrder", config);
```

**Configuration:**
```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      { 
        "Name": "File", 
        "Args": { 
          "path": "logs/workflow-.txt",
          "rollingInterval": "Day" 
        } 
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName"]
  }
}
```

### Resilience Extensions

#### WorkflowForge.Extensions.Resilience

Basic resilience patterns and retry middleware.

**Installation:**
```bash
dotnet add package WorkflowForge.Extensions.Resilience
```

**Features:**
- Retry middleware with configurable policies
- Circuit breaker patterns
- Timeout management
- Basic rate limiting

**Usage:**
```csharp
// See package README for current API; base resilience is covered by Polly extension.
```

#### WorkflowForge.Extensions.Resilience.Polly

Advanced resilience patterns using the Polly library.

**Installation:**
```bash
dotnet add package WorkflowForge.Extensions.Resilience.Polly
```

**Features:**
- Comprehensive retry strategies (exponential backoff, jitter)
- Circuit breakers with failure thresholds
- Rate limiting and throttling
- Timeout policies
- Policy combination and chaining
- Environment-specific configurations

**Usage:**
```csharp
using WorkflowForge;
using WorkflowForge.Extensions.Resilience.Polly;

var foundry = WorkflowForge.CreateFoundry("ProcessOrder");

// Development-friendly preset
foundry.UsePollyDevelopmentResilience();

// Or custom policies
foundry
    .UsePollyRetry(maxRetryAttempts: 5, baseDelay: TimeSpan.FromSeconds(1))
    .UsePollyCircuitBreaker(failureThreshold: 3, durationOfBreak: TimeSpan.FromMinutes(1))
    .UsePollyTimeout(TimeSpan.FromSeconds(30));
```

**Configuration:**
```json
{
  "WorkflowForge": {
    "Polly": {
      "Retry": {
        "MaxAttempts": 3,
        "BaseDelay": "00:00:01",
        "UseExponentialBackoff": true,
        "UseJitter": true
      },
      "CircuitBreaker": {
        "FailureThreshold": 5,
        "BreakDuration": "00:01:00",
        "MinimumThroughput": 10
      },
      "RateLimit": {
        "PermitLimit": 100,
        "Window": "00:00:01"
      }
    }
  }
}
```

### Observability Extensions

#### WorkflowForge.Extensions.Observability.Performance

Comprehensive performance monitoring and metrics collection.

**Installation:**
```bash
dotnet add package WorkflowForge.Extensions.Observability.Performance
```

**Features:**
- Operation timing and throughput metrics
- Memory allocation tracking
- Success/failure rate monitoring
- Performance baseline establishment
- Real-time statistics collection

**Usage:**
```csharp
using WorkflowForge.Extensions.Observability.Performance;

var foundry = WorkflowForge.CreateFoundry("ProcessOrder");
foundry.EnablePerformanceMonitoring();

// Execute workflow
await smith.ForgeAsync(workflow, order, foundry);

// Analyze performance
var stats = foundry.GetPerformanceStatistics();
Console.WriteLine($"Total operations: {stats.TotalOperations}");
Console.WriteLine($"Success rate: {stats.SuccessRate:P2}");
Console.WriteLine($"Average duration: {stats.AverageDuration.TotalMilliseconds:F2}ms");
Console.WriteLine($"Operations/sec: {stats.OperationsPerSecond:F2}");

// Per-operation statistics
foreach (var opStats in stats.GetAllOperationStatistics())
{
    Console.WriteLine($"{opStats.OperationName}: {opStats.AverageDuration.TotalMilliseconds:F2}ms average");
}
```

#### WorkflowForge.Extensions.Observability.HealthChecks

System health monitoring and diagnostics.

**Installation:**
```bash
dotnet add package WorkflowForge.Extensions.Observability.HealthChecks
```

**Features:**
- Built-in health checks (memory, GC, thread pool)
- Custom health check support
- Health status aggregation
- Integration with monitoring systems
- Performance impact assessment

**Usage:**
```csharp
using WorkflowForge.Extensions.Observability.HealthChecks;

var foundry = WorkflowForge.CreateFoundry("ProcessOrder");
var healthService = foundry.CreateHealthCheckService();
var result = await healthService.CheckHealthAsync();

Console.WriteLine($"Overall Status: {result.Status}");
Console.WriteLine($"Memory Usage: {result.Results["Memory"].Description}");
Console.WriteLine($"GC Health: {result.Results["GarbageCollector"].Description}");
Console.WriteLine($"Thread Pool: {result.Results["ThreadPool"].Description}");

// Custom health checks
foundry.AddHealthCheck("Database", async () =>
{
    var isHealthy = await CheckDatabaseConnectionAsync();
    return isHealthy ? HealthCheckResult.Healthy("Database connection OK") 
                     : HealthCheckResult.Unhealthy("Database connection failed");
});
```

#### WorkflowForge.Extensions.Observability.OpenTelemetry

Distributed tracing and telemetry using OpenTelemetry.

**Installation:**
```bash
dotnet add package WorkflowForge.Extensions.Observability.OpenTelemetry
```

**Features:**
- Distributed tracing with span creation
- Activity source integration
- Custom metrics and events
- Integration with observability backends (Jaeger, Zipkin, etc.)
- Correlation context propagation

**Usage:**
```csharp
using WorkflowForge.Extensions.Observability.OpenTelemetry;

var foundry = WorkflowForge.CreateFoundry("ProcessOrder");
foundry.EnableOpenTelemetry(new WorkflowForgeOpenTelemetryOptions
{
    ServiceName = "OrderService",
    ServiceVersion = "1.0.0"
});

// Create custom spans
using var activity = foundry.StartActivity("ProcessOrder")
    .SetTag("order.id", order.Id)
    .SetTag("customer.id", order.CustomerId);

// Execute with tracing
var result = await smith.ForgeAsync(workflow, order, foundry);

// Add custom events
foundry.AddEvent("PaymentProcessed", new { 
    Amount = order.Amount, 
    PaymentMethod = order.PaymentMethod 
});
```

## Extension Configuration Patterns

### Environment-Specific Configuration

```csharp
public static class FoundryExtensions
{
    public static IWorkflowFoundry ForDevelopment(this IWorkflowFoundry foundry)
    {
        return foundry
            .UseSerilog(CreateDevelopmentLogger())
            .UsePollyDevelopmentResilience()
            .EnablePerformanceMonitoring()
            .EnableHealthChecks();
    }

    public static IWorkflowFoundry ForOptimized(this IWorkflowFoundry foundry)
    {
        return foundry
            .UseSerilog(CreateOptimizedLogger())
            .UsePollyResilience()
            .EnableOpenTelemetry("OptimizedService", "1.0.0");
    }

    public static IWorkflowFoundry ForAdvanced(this IWorkflowFoundry foundry)
    {
        return foundry
            .UseSerilog(CreateAdvancedLogger())
            .UsePollyAdvancedResilience()
            .EnablePerformanceMonitoring()
            .EnableHealthChecks()
            .EnableOpenTelemetry("AdvancedService", "1.0.0");
    }
}
```

### Configuration-Driven Setup

```csharp
// appsettings.json
{
  "WorkflowForge": {
    "Extensions": {
      "Logging": {
        "Provider": "Serilog",
        "Configuration": { /* Serilog config */ }
      },
      "Resilience": {
        "Provider": "Polly",
        "Configuration": { /* Polly config */ }
      },
      "Observability": {
        "Performance": { "Enabled": true },
        "HealthChecks": { "Enabled": true },
        "OpenTelemetry": { 
          "Enabled": true,
          "ServiceName": "MyService",
          "ServiceVersion": "1.0.0"
        }
      }
    }
  }
}

// Configuration loading
var foundryConfig = configuration.GetSection("WorkflowForge");
var foundry = WorkflowForge.CreateFoundry("ProcessOrder")
    .ConfigureFromSection(foundryConfig);
```

## Creating Custom Extensions

### Extension Development Pattern

```csharp
// 1. Define extension interface
public interface ICustomExtension
{
    Task<string> ProcessAsync(string input);
}

// 2. Create extension implementation
public class CustomExtensionImplementation : ICustomExtension
{
    private readonly CustomExtensionSettings _settings;

    public CustomExtensionImplementation(IOptions<CustomExtensionSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<string> ProcessAsync(string input)
    {
        // Extension logic
        return await ProcessWithCustomLogicAsync(input);
    }
}

// 3. Create foundry extension method
public static class CustomExtensions
{
    public static IWorkflowFoundry UseCustomExtension(
        this IWorkflowFoundry foundry, 
        Action<CustomExtensionSettings>? configureOptions = null)
    {
        // Register services
        var services = foundry.ServiceProvider ?? new ServiceCollection().BuildServiceProvider();
        services.AddSingleton<ICustomExtension, CustomExtensionImplementation>();
        
        // Configure options
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        // Return configured foundry
        return foundry.WithServiceProvider(services);
    }
}

// 4. Usage
var foundry = WorkflowForge.CreateFoundry("ProcessOrder")
    .UseCustomExtension(options =>
    {
        options.CustomSetting = "value";
        options.Timeout = TimeSpan.FromSeconds(30);
    });
```

### Middleware-Based Extensions

```csharp
public class CustomMiddleware : IWorkflowOperationMiddleware
{
    private readonly ICustomService _customService;

    public CustomMiddleware(ICustomService customService)
    {
        _customService = customService;
    }

    public async Task<object?> ExecuteAsync(
        IWorkflowOperation operation,
        IWorkflowFoundry foundry,
        object? inputData,
        Func<Task<object?>> next,
        CancellationToken cancellationToken)
    {
        // Pre-execution logic
        await _customService.PreProcessAsync(operation, inputData);

        try
        {
            var result = await next();
            
            // Post-execution logic
            await _customService.PostProcessAsync(operation, result);
            
            return result;
        }
        catch (Exception ex)
        {
            // Error handling
            await _customService.HandleErrorAsync(operation, ex);
            throw;
        }
    }
}

// Extension method
public static class CustomMiddlewareExtensions
{
    public static IWorkflowFoundry UseCustomMiddleware(this IWorkflowFoundry foundry)
    {
        return foundry.UseMiddleware<CustomMiddleware>();
    }
}
```

## Extension Best Practices

### 1. Configuration Management

```csharp
// Use strongly-typed configuration
public class ExtensionSettings
{
    public const string SectionName = "WorkflowForge:CustomExtension";
    
    public bool Enabled { get; set; } = true;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public string ConnectionString { get; set; } = string.Empty;
}

// Register with DI container
services.Configure<ExtensionSettings>(
    configuration.GetSection(ExtensionSettings.SectionName));
```

### 2. Logging Integration

```csharp
public class CustomExtension
{
    private readonly IWorkflowForgeLogger _logger;

    public CustomExtension(IWorkflowForgeLogger logger)
    {
        _logger = logger;
    }

    public async Task ProcessAsync()
    {
        _logger.LogInformation("Starting custom processing");
        
        try
        {
            // Processing logic
            _logger.LogInformation("Custom processing completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Custom processing failed");
            throw;
        }
    }
}
```

### 3. Resource Management

```csharp
public class CustomExtension : IDisposable
{
    private readonly IDisposableResource _resource;
    private bool _disposed = false;

    public CustomExtension()
    {
        _resource = CreateResource();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _resource?.Dispose();
            _disposed = true;
        }
    }
}
```

### 4. Performance Considerations

```csharp
public class PerformantExtension
{
    private static readonly ObjectPool<StringBuilder> StringBuilderPool = 
        new DefaultObjectPool<StringBuilder>(new StringBuilderPooledObjectPolicy());

    public async Task<string> ProcessAsync(string input)
    {
        var sb = StringBuilderPool.Get();
        try
        {
            // Use pooled StringBuilder
            sb.Append(input);
            // ... processing
            return sb.ToString();
        }
        finally
        {
            StringBuilderPool.Return(sb);
        }
    }
}
```

## Extension Testing

### Unit Testing Extensions

```csharp
public class CustomExtensionTests
{
    [Fact]
    public async Task Should_Process_Input_Successfully()
    {
        // Arrange
        var mockLogger = new Mock<IWorkflowForgeLogger>();
        var settings = Options.Create(new CustomExtensionSettings());
        var extension = new CustomExtensionImplementation(settings);

        // Act
        var result = await extension.ProcessAsync("test input");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("processed", result);
    }
}
```

### Integration Testing

```csharp
public class ExtensionIntegrationTests
{
    [Fact]
    public async Task Should_Integrate_With_Foundry_Successfully()
    {
        // Arrange
        var foundry = WorkflowForge.CreateFoundry("Test")
            .UseCustomExtension();

        var workflow = WorkflowForge.CreateWorkflow()
            .WithName("TestWorkflow")
            .AddOperation("TestOp", async (input, foundry, ct) =>
            {
                var extension = foundry.GetService<ICustomExtension>();
                return await extension.ProcessAsync((string)input!);
            })
            .Build();

        var smith = WorkflowForge.CreateSmith();

        // Act
        var result = await smith.ForgeAsync(workflow, "test", foundry);

        // Assert
        Assert.NotNull(result);
    }
}
```

## Related Documentation

- **[Getting Started](getting-started.md)** - Basic extension usage
- **[Architecture](architecture.md)** - Extension architecture details
- **[Configuration](configuration.md)** - Configuration patterns
Refer to extension package READMEs for details:
- Core logging integration: `src/core/WorkflowForge/README.md`
- Serilog: `src/extensions/WorkflowForge.Extensions.Logging.Serilog/README.md`
- Resilience base: `src/extensions/WorkflowForge.Extensions.Resilience/README.md`
- Resilience Polly: `src/extensions/WorkflowForge.Extensions.Resilience.Polly/README.md`
- Performance monitoring: `src/extensions/WorkflowForge.Extensions.Observability.Performance/README.md`
- Health checks: `src/extensions/WorkflowForge.Extensions.Observability.HealthChecks/README.md`
- OpenTelemetry: `src/extensions/WorkflowForge.Extensions.Observability.OpenTelemetry/README.md`

---

**WorkflowForge Extensions** - *Enhance your workflows with professional capabilities* 