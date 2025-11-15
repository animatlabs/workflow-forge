# WorkflowForge Configuration

<p align="center">
  <img src="../icon.png" alt="WorkflowForge" width="120" height="120">
</p>

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
4. **Property-Based Configuration**: Runtime configuration via foundry properties
5. **Extension Configuration**: Per-extension settings

### Configuration Philosophy

- **Explicit over Implicit**: Clear, compile-time configuration
- **Code over Config Files**: Type-safe programmatic configuration
- **Sensible Defaults**: Zero-config for simple scenarios
- **Extension Points**: Customization without modification

---

## Core Settings

WorkflowForge configuration is defined in `WorkflowForgeOptions` class and can be configured via `appsettings.json` or programmatically.

### WorkflowForgeOptions Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MaxConcurrentWorkflows` | int | 0 (unlimited) | Maximum concurrent workflows (0-10000, 0 = unlimited) |
| `OperationTimeout` | TimeSpan? | null | Per-operation timeout (null = no timeout) |
| `WorkflowTimeout` | TimeSpan? | null | Per-workflow timeout (null = no timeout) |

**Configuration Section**: `"WorkflowForge"` in `appsettings.json`

---

## Configuration Methods

### Method 1: appsettings.json (Recommended)

**Best for**: Production applications, ASP.NET Core, containerized deployments

#### Step 1: Create appsettings.json

```json
{
  "WorkflowForge": {
    "MaxConcurrentWorkflows": 10,
    "OperationTimeout": "00:00:30",
    "WorkflowTimeout": "00:05:00",
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
        Console.WriteLine($"OperationTimeout: {settings.OperationTimeout}");
        
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

## Core Settings (Legacy)

### IWorkflowSettings Interface

WorkflowForge core uses `IWorkflowSettings` for execution behavior:

```csharp
public interface IWorkflowSettings
{
    bool AutoRestore { get; }                    // Auto-compensation on failure
    bool ContinueOnRestorationFailure { get; }   // Continue rollback on errors
    int MaxConcurrentFlows { get; }              // Concurrent workflow limit
    int RestorationRetryAttempts { get; }        // Compensation retry count
    TimeSpan OperationTimeout { get; }           // Per-operation timeout
    TimeSpan FlowTimeout { get; }                // Workflow timeout
    bool EnableMetrics { get; }                  // Performance metrics
    bool EnableTracing { get; }                  // Distributed tracing
    string MinimumLogLevel { get; }              // Log level filter
}
```

**Current Implementation**: These settings are defined but not yet fully wired into `WorkflowSmith` execution. Version 2.0 focuses on the foundation; full settings integration is planned for 2.1.

### Default Behavior

Without explicit settings, WorkflowForge operates with:

- **No automatic compensation**: `RestoreAsync` must be called explicitly
- **No timeouts**: Operations run until completion or exception
- **No concurrency limits**: Limited only by system resources
- **Minimal logging**: Via injected `IWorkflowForgeLogger`

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
using Serilog;

// Configure Serilog logger
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/workflow-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Create foundry with Serilog
using var foundry = WorkflowForge.CreateFoundry("MyWorkflow");
foundry.UseSerilogLogger(Log.Logger);

// Or use extension method
var logger = foundry.CreateSerilogLogger();
```

**Dependency Isolation**: Uses Costura.Fody to embed Serilog. Your app can use any Serilog version without conflicts.

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

**Dependency Isolation**: Polly is embedded. Your app can use any Polly version without conflicts.

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

**Dependency Isolation**: OpenTelemetry is embedded. Your app can use any OpenTelemetry version without conflicts.

### Validation Extension

```csharp
using WorkflowForge.Extensions.Validation;
using FluentValidation;

// Define validator
public class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("Customer ID required");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be positive");
        RuleFor(x => x.Items).NotEmpty().WithMessage("Order must have items");
    }
}

// Configure validation
using var foundry = WorkflowForge.CreateFoundry("ValidatedWorkflow");
foundry.SetProperty("Order", order);

var validator = new OrderValidator();
foundry.AddMiddleware(new ValidationMiddleware<Order>(
    validator,
    f => f.GetPropertyOrDefault<Order>("Order"),
    throwOnFailure: true  // Throw ValidationException on failure
));

// Validation runs before every operation
await smith.ForgeAsync(workflow, foundry);
```

**Dependency Isolation**: FluentValidation is embedded. Your app can use any FluentValidation version without conflicts.

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

### Recovery Extension

```csharp
using WorkflowForge.Extensions.Persistence.Recovery;
using WorkflowForge.Extensions.Persistence.Abstractions;

// Configure recovery policy
var policy = new RecoveryPolicy
{
    MaxAttempts = 3,
    BaseDelay = TimeSpan.FromSeconds(1),
    UseExponentialBackoff = true
};

// Create recovery coordinator
var provider = new SQLitePersistenceProvider("workflows.db");
var coordinator = new RecoveryCoordinator(provider, policy);

// Resume from last checkpoint
await coordinator.ResumeAsync(
    foundryFactory: () => WorkflowForge.CreateFoundry("OrderService"),
    workflowFactory: BuildProcessOrderWorkflow,
    foundryKey: stableFoundryKey,
    workflowKey: stableWorkflowKey);

// Or use extension method for automatic recovery
await smith.ForgeWithRecoveryAsync(
    workflow,
    foundry,
    provider,
    foundryKey,
    workflowKey,
    policy);
```

**Recovery Features**:
- Resume from last checkpoint
- Exponential backoff for retry
- Skip already-completed operations
- Catalog-based batch recovery

**Zero Dependencies**: Pure WorkflowForge extension built on Persistence abstractions.

### Health Checks Extension

```csharp
using WorkflowForge.Extensions.Observability.HealthChecks;

// Configure health checks
var healthCheck = new WorkflowHealthCheck(
    timeProvider: new SystemTimeProvider(),
    unhealthyThresholdSeconds: 30);

// Register workflow execution
smith.WorkflowStarted += (s, e) => healthCheck.RecordWorkflowExecution();
smith.WorkflowCompleted += (s, e) => healthCheck.RecordWorkflowExecution();

// Check health
var healthStatus = await healthCheck.CheckHealthAsync(new HealthCheckContext());

if (healthStatus.Status == HealthStatus.Unhealthy)
{
    logger.LogWarning("WorkflowForge health check failed: {Description}", 
        healthStatus.Description);
}
```

**Zero Dependencies**: Pure WorkflowForge extension. Implements `IHealthCheck` from Microsoft.Extensions.Diagnostics.HealthChecks (interface only, no DLL dependency).

---

## Environment Strategies

### Development Environment

```csharp
public static class DevelopmentConfiguration
{
    public static IWorkflowFoundry CreateFoundry(string name, IServiceProvider serviceProvider)
    {
        var foundry = WorkflowForge.CreateFoundry(name, serviceProvider);
        
        // Detailed logging
        var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: 
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
        foundry.UseSerilogLogger(logger);
        
        // Performance monitoring
        foundry.AddMiddleware(new TimingMiddleware(logger));
        
        // Event logging
        var smith = WorkflowForge.CreateSmith();
        smith.WorkflowStarted += (s, e) => 
            Console.WriteLine($"Started: {e.WorkflowName}");
        smith.WorkflowCompleted += (s, e) => 
            Console.WriteLine($"Completed in {e.Duration.TotalMilliseconds}ms");
        
        return foundry;
    }
}
```

### Production Environment

```csharp
public static class ProductionConfiguration
{
    public static IWorkflowFoundry CreateFoundry(
        string name, 
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        var foundry = WorkflowForge.CreateFoundry(name, serviceProvider);
        
        // Production logging (file + structured)
        var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                "/var/log/workflowforge/workflow-.txt",
                rollingInterval: RollingInterval.Hour,
                retainedFileCountLimit: 168)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .CreateLogger();
        foundry.UseSerilogLogger(logger);
        
        // Resilience policies
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
        
        // Validation (if order processing)
        if (configuration.GetValue<bool>("EnableValidation"))
        {
            var validator = serviceProvider.GetRequiredService<IValidator<Order>>();
            foundry.AddMiddleware(new ValidationMiddleware<Order>(
                validator,
                f => f.GetPropertyOrDefault<Order>("Order")));
        }
        
        // Audit logging
        var auditProvider = serviceProvider.GetRequiredService<IAuditProvider>();
        var auditLogger = new AuditLogger(
            auditProvider,
            userId: "system",
            sessionId: Guid.NewGuid().ToString(),
            timeProvider: serviceProvider.GetRequiredService<ISystemTimeProvider>());
        
        var smith = WorkflowForge.CreateSmith();
        smith.WorkflowStarted += async (s, e) => 
            await auditLogger.LogWorkflowStartedAsync(e);
        smith.WorkflowCompleted += async (s, e) => 
            await auditLogger.LogWorkflowCompletedAsync(e);
        
        return foundry;
    }
}
```

### Production Environment

```csharp
public static class ProductionConfiguration
{
    public static IWorkflowFoundry CreateFoundry(
        string name,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        TracerProvider tracerProvider)
    {
        var foundry = WorkflowForge.CreateFoundry(name, serviceProvider);
        
        // Production logging (Splunk/ELK)
        var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Splunk(
                configuration["Splunk:Host"],
                configuration.GetValue<int>("Splunk:Port"),
                configuration["Splunk:Token"])
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithCorrelationId()
            .CreateLogger();
        foundry.UseSerilogLogger(logger);
        
        // Distributed tracing
        var tracer = tracerProvider.GetTracer("WorkflowForge");
        
        // Comprehensive resilience
        var resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions { MaxRetryAttempts = 5 })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions 
            { 
                FailureRatio = 0.5, 
                BreakDuration = TimeSpan.FromMinutes(1) 
            })
            .AddTimeout(TimeSpan.FromSeconds(30))
            .Build();
        
        // Full audit trail
        var auditProvider = serviceProvider.GetRequiredService<IAuditProvider>();
        var auditLogger = new AuditLogger(
            auditProvider,
            userId: configuration["Audit:SystemUserId"],
            sessionId: Guid.NewGuid().ToString(),
            timeProvider: serviceProvider.GetRequiredService<ISystemTimeProvider>());
        
        // Health checks
        var healthCheck = serviceProvider.GetRequiredService<WorkflowHealthCheck>();
        
        var smith = WorkflowForge.CreateSmith();
        smith.WorkflowStarted += async (s, e) =>
        {
            await auditLogger.LogWorkflowStartedAsync(e);
            healthCheck.RecordWorkflowExecution();
        };
        
        return foundry;
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
    public override async Task<object?> ForgeAsync(
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken = default)
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
    
    public override async Task<object?> ForgeAsync(
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.UtcNow; // Testable!
        // ...
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

## Next Steps

- **[Getting Started](getting-started.md)** - Initial setup and first workflow
- **[Architecture](architecture.md)** - Understanding the configuration model
- **[Operations](operations.md)** - Creating configurable operations
- **[Events](events.md)** - Event-based configuration
- **[Extensions](extensions.md)** - All 10 extensions with configuration examples
- **[Samples Guide](samples-guide.md)** - 24 practical configuration examples

---

[Back to Documentation Hub](README.md)
