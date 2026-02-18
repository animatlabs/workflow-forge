---
title: Configuration Guide
description: Complete configuration reference for WorkflowForge workflows including error handling, compensation, and environment-specific settings.
---

# WorkflowForge Configuration

Complete configuration guide for WorkflowForge workflows across all environments.

---

## Table of Contents

- [Overview](#overview)
- [Core Settings](#core-settings)
- [Configuration Methods](#configuration-methods)
  - [Method 1: appsettings.json (Recommended)](#method-1-appsettingsjson-recommended)
  - [Method 2: Programmatic Configuration](#method-2-programmatic-configuration)
- [Configuration Patterns](#configuration-patterns)
- [Extension Configuration](#extension-configuration)
- [Environment Strategies](#environment-strategies)
- [Best Practices](#best-practices)

---

## Overview

WorkflowForge provides flexible configuration through multiple mechanisms:

1. **appsettings.json Configuration**: Options pattern with strongly-typed classes (recommended for production)
2. **Programmatic Configuration**: Direct API calls for dynamic scenarios
3. **Service Provider Integration**: DI-based configuration
4. **Extension Configuration**: Per-extension settings

### Configuration Philosophy

- **Explicit over Implicit**: Clear, compile-time configuration
- **Code over Config Files**: Type-safe programmatic configuration
- **Sensible Defaults**: Zero-config for simple scenarios
- **Extension Points**: Customization without modification

---

## Core Settings

WorkflowForge configuration is defined in `WorkflowForgeOptions` and can be configured via `appsettings.json` or programmatically.
`WorkflowForgeOptions` inherits from `WorkflowForgeOptionsBase`, which provides `Enabled`, `SectionName`, `Validate()`, and `Clone()` for a consistent options pattern.

### WorkflowForgeOptions Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | bool | true | Enables or disables the core WorkflowForge feature set |
| `SectionName` | string | "WorkflowForge" | Configuration section name used for binding |
| `MaxConcurrentWorkflows` | int | 0 (unlimited) | Maximum concurrent workflows (0-10000, 0 = unlimited) |
| `ContinueOnError` | bool | false | Continue execution and throw AggregateException at end |
| `FailFastCompensation` | bool | false | Stop compensation on first restore failure |
| `ThrowOnCompensationError` | bool | false | Throw AggregateException when compensation fails |
| `EnableOutputChaining` | bool | true | Pass operation output as next operation input |

**Configuration Section**: `"WorkflowForge"` in `appsettings.json`

---

## Configuration Methods

### Method 1: appsettings.json (Recommended)

**Best for**: Production applications, ASP.NET Core, containerized deployments

#### Step 1: Create appsettings.json

```json
{
  "WorkflowForge": {
    "Enabled": true,
    "MaxConcurrentWorkflows": 10,
    "ContinueOnError": false,
    "FailFastCompensation": false,
    "ThrowOnCompensationError": false,
    "EnableOutputChaining": true,
    "Extensions": {
      "Polly": {
        "Enabled": true,
        "EnableDetailedLogging": true,
        "Retry": {
          "IsEnabled": true,
          "MaxRetryAttempts": 3,
          "BaseDelay": "00:00:01",
          "BackoffType": "Exponential",
          "UseJitter": true
        },
        "CircuitBreaker": {
          "IsEnabled": false,
          "FailureThreshold": 5,
          "BreakDuration": "00:00:30"
        },
        "Timeout": {
          "IsEnabled": true,
          "DefaultTimeout": "00:00:30"
        }
      },
      "Validation": {
        "Enabled": true,
        "ThrowOnValidationError": true,
        "LogValidationErrors": true
      },
      "Audit": {
        "Enabled": true,
        "DetailLevel": "Standard",
        "LogDataPayloads": false
      },
      "Persistence": {
        "Enabled": false,
        "PersistOnOperationComplete": true,
        "PersistOnWorkflowComplete": true
      }
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "WorkflowForge": "Information"
    }
  }
}
```

#### Step 2: Register Configuration in Startup

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WorkflowForge.Options;
using WorkflowForge.Extensions.Resilience.Polly;
using WorkflowForge.Extensions.Resilience.Polly.Options;
using WorkflowForge.Extensions.Validation;
using WorkflowForge.Extensions.Audit;
using WorkflowForge.Extensions.Persistence;

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// Register configuration with Options pattern
var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);

// Core WorkflowForge settings
services.Configure<WorkflowForgeOptions>(
    configuration.GetSection(WorkflowForgeOptions.DefaultSectionName));

// Extension settings using extension methods
services.AddWorkflowForgePolly(configuration);
services.AddValidationConfiguration(configuration);
services.AddAuditConfiguration(configuration);
services.AddPersistenceConfiguration(configuration);

var serviceProvider = services.BuildServiceProvider();
```

#### Step 3: Use Configuration in Application

```csharp
using Microsoft.Extensions.Options;
using WorkflowForge.Options;
using WorkflowForge.Extensions.Resilience.Polly.Options;

public class OrderWorkflowService
{
    private readonly IOptions<WorkflowForgeOptions> _workflowOptions;
    private readonly IOptions<PollyMiddlewareOptions> _pollyOptions;
    
    public OrderWorkflowService(
        IOptions<WorkflowForgeOptions> workflowOptions,
        IOptions<PollyMiddlewareOptions> pollyOptions)
    {
        _workflowOptions = workflowOptions;
        _pollyOptions = pollyOptions;
    }
    
    public async Task ProcessOrderAsync(Order order)
    {
        // Configuration is automatically loaded from appsettings.json
        var settings = _workflowOptions.Value;
        Console.WriteLine($"MaxConcurrentWorkflows: {settings.MaxConcurrentWorkflows}");
        Console.WriteLine($"ContinueOnError: {settings.ContinueOnError}");
        
        // Create foundry and apply configuration
        using var foundry = WorkflowForge.CreateFoundry($"Order-{order.Id}");
        
        // Apply Polly configuration if enabled
        if (_pollyOptions.Value.Enabled)
        {
            foundry.UsePollyFromSettings(_pollyOptions.Value);
        }
        
        foundry.SetProperty("Order", order);
        
        var workflow = BuildOrderWorkflow();
        using var smith = WorkflowForge.CreateSmith();
        await smith.ForgeAsync(workflow, foundry);
    }
}
```

#### Benefits of appsettings.json Configuration

- **Environment-specific**: Different settings for dev/test/prod without code changes
- **Container-friendly**: Override via environment variables in Docker/Kubernetes
- **Strongly-typed**: Compile-time type safety with configuration classes
- **Validation**: Built-in validation with `IOptions<T>.Value`
- **Reloadable**: Hot-reload configuration without restarting application
- **Centralized**: All settings in one place

### Method 2: Programmatic Configuration

**Best for**: Dynamic scenarios, testing, simple console applications

---

## Core Settings

WorkflowForge core uses `WorkflowForgeOptions` for execution behavior:

```csharp
public sealed class WorkflowForgeOptions : WorkflowForgeOptionsBase
{
    public bool Enabled { get; set; } = true;
    public string SectionName { get; }
    public int MaxConcurrentWorkflows { get; set; } = 0;
    public bool ContinueOnError { get; set; } = false;
    public bool FailFastCompensation { get; set; } = false;
    public bool ThrowOnCompensationError { get; set; } = false;
    public bool EnableOutputChaining { get; set; } = true;
    public override IList<string> Validate();
    public override object Clone();
}
```

### Default Behavior

Without explicit settings, WorkflowForge operates with:

- **Automatic compensation**: On failure, `WorkflowSmith` triggers `RestoreAsync` on all completed operations (no-op base class default handles non-restorable operations)
- **Stop-on-first-error**: Execution stops at the first failed operation
- **Best-effort compensation**: Compensation continues even if a restore fails
- **No concurrency limits**: Limited only by system resources
- **Minimal logging**: Via injected `IWorkflowForgeLogger`

### Behavior Switches

- `ContinueOnError = true`: Continue execution, then throw `AggregateException`
- `FailFastCompensation = true`: Stop compensation on the first restore failure
- `ThrowOnCompensationError = true`: Surface restore failures via `AggregateException`

### When to Use Each Switch

- **ContinueOnError**: Use for batch workflows where partial success is acceptable and you want a full failure report at the end.
- **FailFastCompensation**: Use when a single failed rollback should halt further compensation to avoid compounding damage.
- **ThrowOnCompensationError**: Use when compensation failures must be visible to callers for alerting and remediation.

---

## Configuration Patterns

### Pattern 1: Basic Configuration

```csharp
using WorkflowForge;

// Simple workflow with defaults
var workflow = WorkflowForge.CreateWorkflow("SimpleWorkflow")
    .AddOperation(new CalculateTotal())
    .AddOperation(new SendNotification())
    .Build();

using var smith = WorkflowForge.CreateSmith();
await smith.ForgeAsync(workflow);
```

### Pattern 2: Foundry with Properties

```csharp
// Configure via foundry properties
using var foundry = WorkflowForge.CreateFoundry("ConfiguredWorkflow");

// Set execution context
foundry.SetProperty("OrderId", orderId);
foundry.SetProperty("CustomerId", customerId);
foundry.SetProperty("Environment", "Production");

// Execute workflow
using var smith = WorkflowForge.CreateSmith();
await smith.ForgeAsync(workflow, foundry);

// Read results
var total = foundry.GetPropertyOrDefault<decimal>("Total");
```

### Pattern 3: Service Provider Integration

```csharp
// ASP.NET Core / DI integration
public class OrderWorkflowService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderWorkflowService> _logger;
    
    public OrderWorkflowService(
        IServiceProvider serviceProvider,
        ILogger<OrderWorkflowService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    public async Task ProcessOrderAsync(Order order)
    {
        // Create foundry with service provider
        using var foundry = WorkflowForge.CreateFoundry(
            $"Order-{order.Id}",
            _serviceProvider);
        
        foundry.SetProperty("Order", order);
        
        var workflow = BuildOrderWorkflow();
        
        using var smith = WorkflowForge.CreateSmith();
        await smith.ForgeAsync(workflow, foundry);
    }
    
    private IWorkflow BuildOrderWorkflow()
    {
        return WorkflowForge.CreateWorkflow("OrderProcessing")
            .AddOperation(new ValidateOrderOperation())
            .AddOperation(new ChargePaymentOperation())
            .AddOperation(new ReserveInventoryOperation())
            .AddOperation(new CreateShipmentOperation())
            .Build();
    }
}
```

### Pattern 4: Event-Based Configuration

```csharp
// Subscribe to events for monitoring
using var smith = WorkflowForge.CreateSmith();
using var foundry = WorkflowForge.CreateFoundry("MonitoredWorkflow");

// Configure workflow events
smith.WorkflowStarted += (s, e) =>
    _logger.LogInformation("Workflow started: {Name}", e.WorkflowName);

smith.WorkflowCompleted += (s, e) =>
    _logger.LogInformation("Workflow completed in {Duration}ms", 
        e.Duration.TotalMilliseconds);

smith.WorkflowFailed += (s, e) =>
    _logger.LogError(e.Exception, "Workflow failed: {Name}", e.WorkflowName);

// Configure operation events
foundry.OperationStarted += (s, e) =>
    _logger.LogDebug("Operation started: {Name}", e.OperationName);

foundry.OperationCompleted += (s, e) =>
    _metrics.RecordOperationDuration(e.OperationName, e.Duration);

await smith.ForgeAsync(workflow, foundry);
```

### Pattern 5: Middleware Configuration

```csharp
// Configure operation middleware
using var foundry = WorkflowForge.CreateFoundry("MiddlewareExample");

// Add timing middleware (from Performance extension)
foundry.AddMiddleware(new TimingMiddleware(logger));

// Add error handling middleware
foundry.AddMiddleware(new ErrorHandlingMiddleware(logger));

// Add validation middleware (from Validation extension)
foundry.AddMiddleware(new ValidationMiddleware<Order>(
    validator, 
    f => f.GetPropertyOrDefault<Order>("Order")));

await smith.ForgeAsync(workflow, foundry);
```

---

## Extension Configuration

### Serilog Extension

```csharp
using WorkflowForge.Extensions.Logging.Serilog;

// Create a WorkflowForge logger configured via options
var logger = SerilogLoggerFactory.CreateLogger(new SerilogLoggerOptions
{
    MinimumLevel = "Information",
    EnableConsoleSink = true
});

using var foundry = WorkflowForge.CreateFoundry("MyWorkflow");
```

**Dependency Isolation**: Serilog is internalized with ILRepack; Microsoft/System assemblies remain external.

### Resilience Extension

```csharp
using WorkflowForge.Extensions.Resilience;
using WorkflowForge.Extensions.Resilience.Strategies;

// Wrap operations with retry logic
var resilientOperation = RetryWorkflowOperation.WithExponentialBackoff(
    operation: myOperation,
    baseDelay: TimeSpan.FromMilliseconds(100),
    maxDelay: TimeSpan.FromSeconds(30),
    maxAttempts: 3);

// Or use specific strategies
var strategy = new ExponentialBackoffStrategy(
    maxAttempts: 5,
    baseDelay: TimeSpan.FromSeconds(1),
    maxDelay: TimeSpan.FromSeconds(60),
    logger: logger);

var resilientOp = new RetryWorkflowOperation(myOperation, strategy);

// Add to workflow
var workflow = WorkflowForge.CreateWorkflow("ResilientProcess")
    .AddOperation("ProcessWithRetry", resilientOp)
    .Build();
```

**Strategies Available**:
- `ExponentialBackoffStrategy` - Best for external services
- `FixedIntervalStrategy` - Best for databases
- `RandomIntervalStrategy` - Prevents thundering herd

**Zero Dependencies**: Pure WorkflowForge extension with no external dependencies.  
**Configuration**: Programmatic only (via code). For `appsettings.json` support, use WorkflowForge.Extensions.Resilience.Polly.

### Polly Extension

```csharp
using WorkflowForge.Extensions.Resilience.Polly;
using Polly;

// Configure retry policy
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
        onRetry: (exception, timeSpan, retryCount, context) =>
        {
            logger.LogWarning("Retry {RetryCount} after {Delay}ms", 
                retryCount, timeSpan.TotalMilliseconds);
        });

// Wrap operation with policy
var operation = new PollyWrappedOperation<HttpResponseMessage>(
    new CallExternalApiOperation(),
    retryPolicy);

var workflow = WorkflowForge.CreateWorkflow("ResilientWorkflow")
    .AddOperation(operation)
    .Build();
```

**Dependency Isolation**: Polly is internalized with ILRepack; Microsoft/System assemblies remain external.

### OpenTelemetry Extension

```csharp
using WorkflowForge.Extensions.Observability.OpenTelemetry;
using OpenTelemetry;
using OpenTelemetry.Trace;

// Configure OpenTelemetry
var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource("WorkflowForge")
    .AddConsoleExporter()
    .AddJaegerExporter(options =>
    {
        options.AgentHost = "localhost";
        options.AgentPort = 6831;
    })
    .Build();

// Create foundry with tracing
using var foundry = WorkflowForge.CreateFoundry("TracedWorkflow");
var tracer = tracerProvider.GetTracer("WorkflowForge");

// Operations will create spans
await smith.ForgeAsync(workflow, foundry);
```

**Dependency Isolation**: OpenTelemetry is internalized with ILRepack; Microsoft/System assemblies remain external.

### Validation Extension

```csharp
using System.ComponentModel.DataAnnotations;
using WorkflowForge.Extensions.Validation;

// Define model with DataAnnotations
public class Order
{
    [Required]
    public string CustomerId { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [MinLength(1)]
    public List<OrderItem> Items { get; set; } = new();
}

// Configure validation
using var foundry = WorkflowForge.CreateFoundry("ValidatedWorkflow");
foundry.SetProperty("Order", order);

foundry.UseValidation(
    f => f.GetPropertyOrDefault<Order>("Order"),
    new ValidationMiddlewareOptions { ThrowOnValidationError = true });

// Validation runs before every operation
await smith.ForgeAsync(workflow, foundry);
```

**Dependency Isolation**: Validation uses DataAnnotations and does not add third-party dependencies.

### Audit Extension

```csharp
using WorkflowForge.Extensions.Audit;

// Implement audit provider
public class FileAuditProvider : IAuditProvider
{
    private readonly string _logPath;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    
    public FileAuditProvider(string logPath)
    {
        _logPath = logPath;
    }
    
    public async Task WriteAuditEntryAsync(
        AuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            var json = JsonSerializer.Serialize(entry);
            await File.AppendAllTextAsync(_logPath, json + Environment.NewLine, cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }
    
    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

// Configure audit logging
var auditProvider = new FileAuditProvider("audit.log");
var auditLogger = new AuditLogger(
    auditProvider,
    userId: "user@example.com",
    sessionId: Guid.NewGuid().ToString(),
    timeProvider: new SystemTimeProvider());

// Subscribe to events
smith.WorkflowStarted += async (s, e) => 
    await auditLogger.LogWorkflowStartedAsync(e);
smith.WorkflowCompleted += async (s, e) => 
    await auditLogger.LogWorkflowCompletedAsync(e);
foundry.OperationCompleted += async (s, e) => 
    await auditLogger.LogOperationCompletedAsync(e);

await smith.ForgeAsync(workflow, foundry);
```

**Zero Dependencies**: Pure WorkflowForge extension. Implement `IAuditProvider` for your storage.

### Persistence Extensions

```csharp
using WorkflowForge.Extensions.Persistence.InMemory;
using WorkflowForge.Extensions.Persistence.SQLite;

// In-memory persistence (development/testing)
var inMemoryProvider = new InMemoryPersistenceProvider();
await inMemoryProvider.SaveWorkflowStateAsync(executionId, state);
var loadedState = await inMemoryProvider.LoadWorkflowStateAsync(executionId);

// SQLite persistence (production)
var sqliteProvider = new SQLitePersistenceProvider("workflows.db");
await sqliteProvider.SaveWorkflowStateAsync(executionId, state);
var loadedState = await sqliteProvider.LoadWorkflowStateAsync(executionId);

// Use in workflow
foundry.SetProperty("PersistenceProvider", sqliteProvider);

// Save checkpoints during execution
var checkpointOp = new DelegateWorkflowOperation(async (foundry, ct) =>
{
    var provider = foundry.GetPropertyOrDefault<IPersistenceProvider>("PersistenceProvider");
    var state = new WorkflowState { /* ... */ };
    await provider.SaveWorkflowStateAsync(foundry.ExecutionId, state, ct);
});
```

**Zero Dependencies (InMemory)**: Pure WorkflowForge extension.  
**Dependency Isolation (SQLite)**: Microsoft.Data.Sqlite embedded.

---

## Environment Strategies

### Development Environment

```csharp
public static class DevelopmentConfiguration
{
    public static IWorkflowFoundry CreateFoundry(string name)
    {
        var logger = SerilogLoggerFactory.CreateLogger(new SerilogLoggerOptions
        {
            MinimumLevel = "Debug",
            EnableConsoleSink = true
        });

        var foundry = WorkflowForge.CreateFoundry(name, logger);
        foundry.AddMiddleware(new TimingMiddleware(foundry.Logger));

        return foundry;
    }
}
```

### Production Environment

```csharp
public static class ProductionConfiguration
{
    public static IWorkflowFoundry CreateFoundry(string name)
    {
        var logger = SerilogLoggerFactory.CreateLogger(new SerilogLoggerOptions
        {
            MinimumLevel = "Information",
            EnableConsoleSink = true
        });

        return WorkflowForge.CreateFoundry(name, logger);
    }
}
```

---

## Best Practices

### 1. Use Service Provider for Dependencies

```csharp
// Good: DI-based
public class OrderOperation : WorkflowOperationBase
{
    protected override async Task<object?> ForgeAsyncCore(
        object? inputData,
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken)
    {
        // Get service from foundry's service provider
        var orderService = foundry.ServiceProvider
            .GetRequiredService<IOrderService>();
        
        var order = foundry.GetPropertyOrDefault<Order>("Order");
        await orderService.ProcessAsync(order, cancellationToken);
        
        return null;
    }
}

// Bad: Static dependencies
public class OrderOperation : WorkflowOperationBase
{
    private static readonly HttpClient _httpClient = new(); // Don't do this
    
    // ...
}
```

### 2. Configure Middleware in Order

```csharp
// Correct order: Validation → Timing → Error Handling → Business Logic
foundry.AddMiddleware(new ValidationMiddleware<Order>(...));  // First
foundry.AddMiddleware(new TimingMiddleware(...));             // Second
foundry.AddMiddleware(new ErrorHandlingMiddleware(...));      // Third
// Operations execute last
```

### 3. Use Properties for Context, Not Input Data

```csharp
// Good: Context in properties
foundry.SetProperty("OrderId", orderId);
foundry.SetProperty("CustomerId", customerId);
foundry.SetProperty("CorrelationId", correlationId);

var workflow = WorkflowForge.CreateWorkflow("OrderWorkflow")
    .AddOperation(new FetchOrderOperation())    // Reads OrderId from properties
    .AddOperation(new ProcessPaymentOperation()) // Reads order data from properties
    .Build();

// Bad: Passing data as input (only use for generic operations)
var data = new { OrderId = orderId, CustomerId = customerId };
await operation.ForgeAsync(data, foundry, cancellationToken); // Avoid unless IWorkflowOperation<TInput, TOutput>
```

### 4. Dispose Resources Properly

```csharp
// Good: Using statement
using var foundry = WorkflowForge.CreateFoundry("MyWorkflow");
using var smith = WorkflowForge.CreateSmith();
await smith.ForgeAsync(workflow, foundry);
// Automatic disposal

// Also good: Try-finally
var foundry = WorkflowForge.CreateFoundry("MyWorkflow");
try
{
    await smith.ForgeAsync(workflow, foundry);
}
finally
{
    foundry.Dispose();
}
```

### 5. Inject ISystemTimeProvider for Testability

```csharp
// Good: Inject time provider
public class TimeSensitiveOperation : WorkflowOperationBase
{
    private readonly ISystemTimeProvider _timeProvider;
    
    public TimeSensitiveOperation(ISystemTimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }
    
    protected override async Task<object?> ForgeAsyncCore(
        object? inputData,
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.UtcNow; // Testable!
        // ...
        return inputData;
    }
}

// Bad: Direct DateTime usage
var now = DateTime.UtcNow; // Can't mock in tests
```

### 6. Use Events for Observability, Not Control Flow

```csharp
// Good: Events for monitoring
smith.WorkflowCompleted += (s, e) => _metrics.RecordDuration(e.Duration);

// Bad: Events for control flow
smith.WorkflowCompleted += (s, e) => 
{
    // Don't do complex business logic in event handlers
    StartAnotherWorkflow(); // Use operations instead
};
```

### 7. Version Your Workflows

```csharp
// Good: Include version in workflow name
var workflow = WorkflowForge.CreateWorkflow("OrderProcessing_v2")
    .AddOperation(new ValidateOrderV2())
    .AddOperation(new ProcessPaymentV2())
    .Build();

// Track version in properties
foundry.SetProperty("WorkflowVersion", "2.0");
```

### 8. Test with Mock Implementations

```csharp
// Create testable time provider
public class MockTimeProvider : ISystemTimeProvider
{
    private DateTimeOffset _currentTime;
    
    public DateTimeOffset UtcNow => _currentTime;
    
    public void SetTime(DateTimeOffset time) => _currentTime = time;
    public void Advance(TimeSpan duration) => _currentTime += duration;
}

// Use in tests
var mockTime = new MockTimeProvider();
mockTime.SetTime(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));

var operation = new TimeSensitiveOperation(mockTime);
// Test with controlled time
```

---

## Related Documentation

- [Getting Started](../getting-started/getting-started.md) - Initial setup and first workflow
- [Architecture](../architecture/overview.md) - Understanding the configuration model
- [Operations](operations.md) - Creating configurable operations
- [Events](events.md) - Event-based configuration
- [Extensions](../extensions/index.md) - All 13 packages with configuration examples
- [Samples Guide](../getting-started/samples-guide.md) - Practical examples including configuration
