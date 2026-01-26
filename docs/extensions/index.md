# WorkflowForge Extension System

<p align="center">
  <img src="https://raw.githubusercontent.com/animatlabs/workflow-forge/main/icon.png" alt="WorkflowForge" width="120" height="120">
</p>

WorkflowForge follows an extension-first architecture where the core library provides minimal functionality, and rich features are delivered through a comprehensive extension ecosystem.

## Table of Contents

- [Dependency-Free Core and Zero Version Conflicts](#dependency-free-core-and-zero-version-conflicts)
- [Extension Architecture](#extension-architecture)
- [Available Extensions](#available-extensions)
  - [Logging Extensions](#logging-extensions)
  - [Resilience Extensions](#resilience-extensions)
  - [Observability Extensions](#observability-extensions)
  - [Persistence Extensions](#persistence-extensions)
  - [Validation Extension](#validation-extension)
  - [Audit Extension](#audit-extension)
- [Extension Configuration Patterns](#extension-configuration-patterns)
- [Creating Custom Extensions](#creating-custom-extensions)
- [Extension Best Practices](#extension-best-practices)

---

## Dependency-Free Core and Dependency Isolation

WorkflowForge core is zero-dependency. Extensions isolate third-party libraries where it makes sense, while keeping Microsoft/System dependencies external to avoid runtime conflicts.

- **Internalized with ILRepack**: Serilog, Polly, OpenTelemetry
- **Always external**: Microsoft/System assemblies (runtime unification)
- **Validation**: DataAnnotations (no third-party dependency)

### How It Works

Extensions that depend on non-BCL packages use ILRepack to internalize those libraries into the extension assembly. This keeps the public API clean (only WorkflowForge or BCL types) while avoiding version conflicts.

Microsoft/System assemblies are never embedded; those are resolved by the runtime using the application's dependency graph.

## Extension Architecture

### Core Principles

1. **Dependency-Free Core** - The core library has zero dependencies
2. **Isolated Extensions** - Third-party dependencies are internalized where appropriate
3. **Optional Extensions** - Add only what you need
4. **Composable Features** - Extensions work together seamlessly
5. **Configuration-Driven** - Extensions are configured, not hard-coded
6. **Production-Ready** - Professional features for production use

### Extension Categories

```
WorkflowForge Extensions
├── Logging
│   └── Serilog Integration
├── Resilience
│   ├── Basic Patterns
│   └── Polly Integration
├── Persistence (BYO storage)
│   └── Recovery Extensions
├── Validation
│   └── DataAnnotations Integration
├── Audit
│   └── Compliance & Operational Monitoring
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
using WorkflowForge.Extensions.Logging.Serilog;

var logger = SerilogLoggerFactory.CreateLogger(new SerilogLoggerOptions
{
    MinimumLevel = "Information",
    EnableConsoleSink = true
});

var options = new WorkflowForgeOptions
{
    ContinueOnError = false,
    FailFastCompensation = false,
    ThrowOnCompensationError = true
};
var foundry = WorkflowForge.CreateFoundry("ProcessOrder", logger, options: options);
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

// Custom policies
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

### Persistence Extensions

#### WorkflowForge.Extensions.Persistence

Core workflow state persistence abstraction with bring-your-own-storage pattern.

**Installation:**
```bash
dotnet add package WorkflowForge.Extensions.Persistence
```

**Features:**
- Abstract persistence layer
- State snapshot and restoration
- Pluggable storage providers
- Metadata management
- Operation state tracking

**Usage:**
```csharp
using WorkflowForge.Extensions.Persistence;

// Implement custom storage provider
public class MyStorageProvider : IWorkflowStateStore
{
    public async Task SaveStateAsync(WorkflowState state, CancellationToken ct)
    {
        // Save to your database, file system, etc.
    }
    
    public async Task<WorkflowState?> LoadStateAsync(string workflowId, CancellationToken ct)
    {
        // Load from your storage
    }
}

// Use in workflow
var stateStore = new MyStorageProvider();
var foundry = WorkflowForge.CreateFoundry("PersistentWorkflow");
foundry.AddStateStore(stateStore);
```

#### WorkflowForge.Extensions.Persistence.Recovery

Resume interrupted workflows from saved state.

**Installation:**
```bash
dotnet add package WorkflowForge.Extensions.Persistence.Recovery
```

**Features:**
- Automatic workflow resumption
- Skip completed operations
- State validation and integrity checks
- Recovery point management
- Failure recovery strategies

**Usage:**
```csharp
using WorkflowForge.Extensions.Persistence;
using WorkflowForge.Extensions.Persistence.Recovery;

var stateStore = new MyStorageProvider();
var recoveryService = new WorkflowRecoveryService(stateStore);

// Attempt to recover workflow
var state = await recoveryService.LoadWorkflowStateAsync("workflow-123");
if (state != null && state.CanRecover)
{
    var foundry = WorkflowForge.CreateFoundry("RecoveredWorkflow");
    foundry.AddStateStore(stateStore);
    foundry.EnableRecovery(state);
    
    // Resume from last checkpoint
    await smith.ForgeAsync(workflow, foundry);
}
```

**See Also:**
- Sample 18: Persistence (BYO Storage)
- Sample 21: Recovery Only
- Sample 22: Recovery + Resilience

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
foundry.SetProperty("Order", order);
await smith.ForgeAsync(workflow, foundry);

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

### Persistence (Bring Your Own Storage)

Enable resumable workflows without adding dependencies by providing your own storage implementation.
Package: `WorkflowForge.Extensions.Persistence`

1) Implement the provider interface and plug it in via middleware:

```csharp
using WorkflowForge.Extensions; // UsePersistence
using WorkflowForge.Extensions.Persistence.Abstractions; // IWorkflowPersistenceProvider
using WorkflowForge.Extensions.Persistence.Abstractions; // WorkflowExecutionSnapshot

public sealed class MyPersistenceProvider : IWorkflowPersistenceProvider
{
    public Task SaveAsync(WorkflowExecutionSnapshot snapshot, CancellationToken ct = default) => Task.CompletedTask;
    public Task<WorkflowExecutionSnapshot?> TryLoadAsync(Guid foundryExecutionId, Guid workflowId, CancellationToken ct = default) => Task.FromResult<WorkflowExecutionSnapshot?>(null);
    public Task DeleteAsync(Guid foundryExecutionId, Guid workflowId, CancellationToken ct = default) => Task.CompletedTask;
}

// Enable persistence
var provider = new MyPersistenceProvider();
using var foundry = WorkflowForge.CreateFoundry("OrderProcessing");
foundry.UsePersistence(provider);
```

This middleware checkpoints after each operation and attempts to resume by skipping already-completed operations. Snapshot data includes:
- `FoundryExecutionId`, `WorkflowId`, `WorkflowName`
- `NextOperationIndex` (next op to run)
- `Properties` captured from the foundry

Note: You control storage and serialization. The core remains zero-dependency.

Important:
- Use a shared provider (e.g., DB/file/queue-backed) for real resume across processes. The in-memory provider in samples is for demonstration only and does not persist across app restarts.
- Keep snapshots minimal. Only store the properties required to resume safely.

2) Cross-process resume (stable keys):

```csharp
using WorkflowForge.Extensions;
using WorkflowForge.Extensions.Persistence;

var options = new PersistenceOptions
{
  InstanceId = "order-service-west-1", // maps to a deterministic foundry key
  WorkflowKey = "ProcessOrder-v1"      // maps to a deterministic workflow key
};

using var foundry = WorkflowForge.CreateFoundry("OrderProcessing");
foundry.UsePersistence(provider, options);
```

Notes:
- With stable keys and a shared provider, a new process can resume from the last successful step.
- Properties saved in the snapshot are restored on resume before determining which step to execute next.

#### Recovery (Resume + Retry)

Package: `WorkflowForge.Extensions.Persistence.Recovery`

Adds simple recovery orchestration that first attempts to resume from a snapshot and then runs a fresh execution with configurable retries/backoff. Best used with stable `InstanceId`/`WorkflowKey` and a shared provider (DB, file share, cache) for cross-process/host resume.

```csharp
using WorkflowForge.Extensions.Persistence.Recovery;

var foundryKey = DeterministicGuid(options.InstanceId!);
var workflowKey = DeterministicGuid(options.WorkflowKey!);

await smith.ForgeWithRecoveryAsync(
    workflow,
    foundry,
    provider,
    foundryKey,
    workflowKey,
    new RecoveryPolicy { MaxAttempts = 5, BaseDelay = TimeSpan.FromMilliseconds(50), UseExponentialBackoff = true },
    cancellationToken);
```

Key points:
- Resume attempts restore foundry properties and skip completed steps.
- After resume, a fresh execution is attempted with retries (policy).
- If resume or execution ultimately fails, the last exception is surfaced to the caller.

##### Using Resilience With Recovery (Unified Experience)

You can combine base Resilience retry middleware with Recovery for a unified experience. The retry middleware handles transient failures during a single run; Recovery resumes from the last checkpoint across runs.

```csharp
using WorkflowForge.Extensions.Resilience;
using WorkflowForge.Extensions.Persistence;
using WorkflowForge.Extensions.Persistence.Recovery;

var provider = new FilePersistenceProvider(checkpointPath);
var options = new PersistenceOptions { InstanceId = "svc-west-1", WorkflowKey = "Order-v1" };
var foundryKey = DeterministicGuid(options.InstanceId!);
var workflowKey = DeterministicGuid(options.WorkflowKey!);

using var foundry = WorkflowForge.CreateFoundry("OrderProcessing");
foundry.UsePersistence(provider, options);
foundry.AddMiddleware(
    RetryMiddleware.WithExponentialBackoff(foundry.Logger,
        initialDelay: TimeSpan.FromMilliseconds(50),
        maxDelay: TimeSpan.FromMilliseconds(500),
        maxAttempts: 2));

await smith.ForgeWithRecoveryAsync(
    workflow,
    foundry,
    provider,
    foundryKey,
    workflowKey,
    new RecoveryPolicy { MaxAttempts = 3, BaseDelay = TimeSpan.FromMilliseconds(100), UseExponentialBackoff = true },
    cancellationToken);
```

See interactive sample: `Recovery + Resilience` (menu 22) in `src/samples/WorkflowForge.Samples.BasicConsole`.

Catalog-driven recovery (multiple workflows): implement `IRecoveryCatalog` to enumerate pending snapshots, then use `ResumeAllAsync`:

```csharp
public sealed class MyRecoveryCatalog : IRecoveryCatalog
{
    public Task<IReadOnlyList<WorkflowExecutionSnapshot>> ListPendingAsync(CancellationToken ct = default)
    {
        // Query your store for snapshots that need recovery
        return Task.FromResult<IReadOnlyList<WorkflowExecutionSnapshot>>(pendingList);
    }
}

var catalog = new MyRecoveryCatalog();
var coordinator = new RecoveryCoordinator(provider, new RecoveryPolicy { MaxAttempts = 3 });
int recovered = await coordinator.ResumeAllAsync(
    () => WorkflowForge.CreateFoundry("Service"),
    () => BuildWorkflow(),
    catalog,
    cancellationToken);
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
foundry.SetProperty("Order", order);
await smith.ForgeAsync(workflow, foundry);

// Add custom events
foundry.AddEvent("PaymentProcessed", new { 
    Amount = order.Amount, 
    PaymentMethod = order.PaymentMethod 
});
```

## Extension Configuration Patterns

### Environment-Specific Configuration

Prefer explicit configuration per environment using `appsettings.{Environment}.json` or programmatic options.
This keeps extension behavior predictable and avoids preset helper methods that hide configuration details.

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

### Validation Extension

#### WorkflowForge.Extensions.Validation

Input validation and business rule enforcement using DataAnnotations.

**Installation:**
```bash
dotnet add package WorkflowForge.Extensions.Validation
```

**Features:**
- DataAnnotations validation
- Automatic middleware-based validation
- Manual validation support
- Comprehensive error reporting
- Property-level error details
- Validation result caching

**Usage:**
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
}

// Automatic validation via middleware (DataAnnotations)
var foundry = WorkflowForge.CreateFoundry("OrderProcessing");
foundry.UseValidation(
    f => f.GetPropertyOrDefault<Order>("Order"));

// Manual validation
var order = new Order { CustomerId = "CUST-123", Amount = 99.99m };
var result = await foundry.ValidateAsync(order);
if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
    }
}
```

**Middleware Integration:**
```csharp
// Validation happens automatically before operation execution
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("ValidatedWorkflow")
    .AddOperation(new ProcessOrderOperation())
    .Build();

// Validation errors are stored in foundry properties
var status = foundry.GetPropertyOrDefault<string>("Validation.ProcessOrder.Status");
var errors = foundry.GetPropertyOrDefault<IReadOnlyList<ValidationError>>(
    "Validation.ProcessOrder.Errors");
```

**Configuration:**
```json
{
  "WorkflowForge": {
    "Validation": {
      "ThrowOnFailure": true,
      "CacheResults": false,
      "DetailedErrors": true
    }
  }
}
```

**See Also:**
- Sample: `src/samples/WorkflowForge.Samples.BasicConsole/Samples/ValidationSample.cs`
- Tests: `tests/WorkflowForge.Extensions.Validation.Tests/`
- README: `src/extensions/WorkflowForge.Extensions.Validation/README.md`

---

### Audit Extension

#### WorkflowForge.Extensions.Audit

Comprehensive audit logging for compliance and operational monitoring.

**Installation:**
```bash
dotnet add package WorkflowForge.Extensions.Audit
```

**Features:**
- Automatic operation auditing
- Pluggable storage providers (bring your own)
- In-memory provider for testing
- Immutable audit entries
- Performance timing capture
- Metadata enrichment
- User context tracking

**Usage:**
```csharp
using WorkflowForge.Extensions.Audit;

// Create audit provider (in-memory for demo)
var auditProvider = new InMemoryAuditProvider();

// Enable audit logging
var foundry = WorkflowForge.CreateFoundry("OrderProcessing");
foundry.EnableAudit(
    auditProvider,
    initiatedBy: "admin@company.com",
    includeMetadata: true);

// Workflow operations are automatically audited
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("ProcessOrder")
    .AddOperation(new ValidateOrderOperation())
    .AddOperation(new ProcessPaymentOperation())
    .Build();

using var smith = WorkflowForge.CreateSmith();
await smith.ForgeAsync(workflow, foundry);

// Query audit entries
foreach (var entry in auditProvider.Entries)
{
    Console.WriteLine($"[{entry.Timestamp:HH:mm:ss.fff}] {entry.EventType} - " +
                     $"{entry.OperationName}: {entry.Status} ({entry.DurationMs}ms)");
}

// Custom audit entries
await foundry.WriteCustomAuditAsync(
    auditProvider,
    "ManualApproval",
    AuditEventType.Custom,
    "Approved",
    initiatedBy: "manager@company.com");
```

**Custom Audit Provider:**
```csharp
public class DatabaseAuditProvider : IAuditProvider
{
    private readonly DbContext _context;

    public async Task WriteAuditEntryAsync(AuditEntry entry)
    {
        _context.AuditLog.Add(entry);
        await _context.SaveChangesAsync();
    }

    public async Task FlushAsync()
    {
        await _context.SaveChangesAsync();
    }
}

// Use custom provider
var auditProvider = new DatabaseAuditProvider(dbContext);
foundry.EnableAudit(auditProvider);
```

**Audit Entry Structure:**
```csharp
public class AuditEntry
{
    public Guid Id { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string WorkflowName { get; init; }
    public string OperationName { get; init; }
    public AuditEventType EventType { get; init; }
    public string Status { get; init; }
    public long? DurationMs { get; init; }
    public string InitiatedBy { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; }
}
```

**Event Types:**
```csharp
public enum AuditEventType
{
    OperationStarted,
    OperationCompleted,
    OperationFailed,
    Custom
}
```

**Configuration:**
```json
{
  "WorkflowForge": {
    "Audit": {
      "Enabled": true,
      "IncludeMetadata": true,
      "DefaultInitiatedBy": "system"
    }
  }
}
```

**See Also:**
- Sample: `src/samples/WorkflowForge.Samples.BasicConsole/Samples/AuditSample.cs`
- Tests: `tests/WorkflowForge.Extensions.Audit.Tests/`
- README: `src/extensions/WorkflowForge.Extensions.Audit/README.md`

---

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
        Func<CancellationToken, Task<object?>> next,
        CancellationToken cancellationToken)
    {
        // Pre-execution logic
        await _customService.PreProcessAsync(operation, inputData);

        try
        {
            var result = await next(cancellationToken);
            
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
            .AddOperation(new ActionWorkflowOperation(
                "TestOp",
                async (input, foundry, ct) =>
                {
                    var extension = foundry.ServiceProvider
                        ?.GetRequiredService<ICustomExtension>()
                        ?? throw new InvalidOperationException("Service provider is required.");
                    var value = foundry.GetPropertyOrDefault<string>("Input");
                    var result = await extension.ProcessAsync(value);
                    foundry.SetProperty("Result", result);
                }
            ))
            .Build();

        var smith = WorkflowForge.CreateSmith();

        // Act
        foundry.SetProperty("Input", "test");
        await smith.ForgeAsync(workflow, foundry);

        // Assert
        var result = foundry.GetPropertyOrDefault<string>("Result");
        Assert.NotNull(result);
    }
}
```

## Related Documentation

- **[Getting Started](../getting-started/getting-started.md)** - Basic extension usage
- **[Architecture](../architecture/overview.md)** - Extension architecture details
- **[Configuration](../core/configuration.md)** - Configuration patterns
- **[Operations Guide](../core/operations.md)** - Custom operation patterns
- **[Events System](../core/events.md)** - Event-driven integration

Refer to extension package READMEs for details:
- Core logging integration: `src/core/WorkflowForge/README.md`
- Serilog: `src/extensions/WorkflowForge.Extensions.Logging.Serilog/README.md`
- Resilience base: `src/extensions/WorkflowForge.Extensions.Resilience/README.md`
- Resilience Polly: `src/extensions/WorkflowForge.Extensions.Resilience.Polly/README.md`
- Validation: `src/extensions/WorkflowForge.Extensions.Validation/README.md`
- Audit: `src/extensions/WorkflowForge.Extensions.Audit/README.md`
- Performance monitoring: `src/extensions/WorkflowForge.Extensions.Observability.Performance/README.md`
- Health checks: `src/extensions/WorkflowForge.Extensions.Observability.HealthChecks/README.md`
- OpenTelemetry: `src/extensions/WorkflowForge.Extensions.Observability.OpenTelemetry/README.md`

---

**← Back to [Documentation Home](../index.md)**
