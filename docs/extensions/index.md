---
title: Extension System
description: 13 NuGet packages for logging, resilience, observability, persistence, validation, and testing with zero version conflicts.
---

# WorkflowForge Extension System

<a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=alert_status" alt="Quality Gate Status" /></a>
<a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=coverage" alt="Coverage" /></a>
<a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=reliability_rating" alt="Reliability Rating" /></a>
<a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=security_rating" alt="Security Rating" /></a>
<a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=sqale_rating" alt="Maintainability Rating" /></a>

The core stays small. Optional packages add logging, resilience, observability, persistence, and more, without dependency version fights.

## Table of Contents

- [Dependency-Free Core and Dependency Isolation](#dependency-free-core-and-dependency-isolation)
- [Extension Architecture](#extension-architecture)
- [Available Packages](#available-packages)
  - [Testing Package](#testing-package)
  - [DependencyInjection Extension](#dependencyinjection-extension)
  - [Logging Extensions](#logging-extensions)
  - [Resilience Extensions](#resilience-extensions)
  - [Observability Extensions](#observability-extensions)
  - [Persistence Extensions](#persistence-extensions)
  - [Validation Extension](#validation-extension)
  - [Audit Extension](#audit-extension)
- [Extension Configuration Patterns](#extension-configuration-patterns)
- [Creating Custom Extensions](#creating-custom-extensions)
- [Extension Guidelines](#extension-guidelines)
- [Extension Testing](#extension-testing)

---

## Dependency-Free Core and Dependency Isolation

Core has no package dependencies. Extensions bundle third-party libs where that helps. Microsoft and System assemblies stay external so the runtime can unify versions.

- **Internalized with ILRepack**: Serilog, Polly, OpenTelemetry
- **Always external**: Microsoft/System assemblies (runtime unification)
- **Validation**: DataAnnotations (no third-party dependency)

### How it works

Non-BCL dependencies are often merged into the extension assembly with ILRepack. The surface area you code against stays WorkflowForge plus BCL types.

Microsoft and System assemblies are not embedded; they follow the app’s normal reference graph.

## Extension Architecture

### Core Principles

1. **Dependency-free core**: zero NuGet dependencies on the main package.
2. **Isolated extensions**: ILRepack hides many third-party implementations inside the extension assembly.
3. **Optional packages**: take only what you use.
4. **Composable**: Serilog, Polly, OpenTelemetry, and others can coexist without type clashes.
5. **Explicit config**: options and `appsettings` drive behavior; surprises are rare on purpose.
6. **Operational extras**: logging, health, persistence, and audit map to how teams actually run services.

### Extension Categories

```
WorkflowForge Packages
├── Testing
│   └── FakeWorkflowFoundry (unit testing)
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

## Available Packages

### Testing Package

#### WorkflowForge.Testing

Fakes and helpers for testing operations and workflows in isolation.

**Add with NuGet:**
```bash
dotnet add package WorkflowForge.Testing
```

**Includes:**
- `FakeWorkflowFoundry` - Lightweight fake for unit testing operations in isolation
- Execution tracking - Assert which operations executed
- Property management - Test data flow between operations
- Event verification - Subscribe to operation events in tests
- Reset capability - Reuse foundry between tests

**Quick Start:**
```csharp
using WorkflowForge.Testing;
using Xunit;

public class MyOperationTests
{
    [Fact]
    public async Task Operation_Should_SetProperty()
    {
        // Arrange
        var foundry = new FakeWorkflowFoundry();
        var operation = new MyCustomOperation();
        
        // Act
        await operation.ForgeAsync("input", foundry, CancellationToken.None);
        
        // Assert
        Assert.True(foundry.Properties.ContainsKey("expectedKey"));
    }
    
    [Fact]
    public async Task Workflow_Should_ExecuteAllOperations()
    {
        // Arrange
        var foundry = new FakeWorkflowFoundry();
        foundry.AddOperation(new OpA());
        foundry.AddOperation(new OpB());
        
        // Act
        await foundry.ForgeAsync();
        
        // Assert
        Assert.Equal(2, foundry.ExecutedOperations.Count);
    }
}
```

**API Reference:**
| Property/Method | Description |
|-----------------|-------------|
| `ExecutionId` | Unique ID (auto-generated, settable) |
| `Properties` | Thread-safe property storage |
| `Operations` | Added operations |
| `ExecutedOperations` | Operations executed during ForgeAsync |
| `ForgeAsync()` | Execute all operations sequentially |
| `Reset()` | Clear all state for test reuse |
| `TrackExecution(op)` | Manually track operation as executed |

---

### DependencyInjection Extension

#### WorkflowForge.Extensions.DependencyInjection

Registers WorkflowForge with `Microsoft.Extensions.DependencyInjection` (ASP.NET Core and generic hosts).

**Package reference:**
```bash
dotnet add package WorkflowForge.Extensions.DependencyInjection
```

**Includes:**
- `IServiceCollection` registration helpers
- `IOptions` binding and validation hooks
- Works with the stock ASP.NET Core container
- Scoped and singleton lifetimes as you configure them

**Usage:**
```csharp
using WorkflowForge.Extensions.DependencyInjection;

// In Program.cs or Startup.cs
services.AddWorkflowForge(options =>
{
    options.ContinueOnError = false;
    options.FailFastCompensation = false;
    options.ThrowOnCompensationError = true;
});

// Inject IWorkflowSmith in your services
public class OrderService
{
    private readonly IWorkflowSmith _smith;

    public OrderService(IWorkflowSmith smith)
    {
        _smith = smith;
    }

    public async Task ProcessOrderAsync(Order order)
    {
        var foundry = WorkflowForge.CreateFoundry("ProcessOrder");
        foundry.SetProperty("Order", order);

        var workflow = WorkflowForge.CreateWorkflow()
            .WithName("OrderWorkflow")
            .AddOperation(new ValidateOrderOperation())
            .AddOperation(new ProcessPaymentOperation())
            .Build();

        await _smith.ForgeAsync(workflow, foundry);
    }
}
```

**Configuration via appsettings.json:**
```json
{
  "WorkflowForge": {
    "ContinueOnError": false,
    "FailFastCompensation": false,
    "ThrowOnCompensationError": true
  }
}
```

```csharp
// Bind from configuration
services.AddWorkflowForge(configuration.GetSection("WorkflowForge"));
```

---

### Logging Extensions

#### WorkflowForge.Extensions.Logging.Serilog

Serilog-backed `IWorkflowForgeLogger` and sinks you configure yourself.

**Installation:**
```bash
dotnet add package WorkflowForge.Extensions.Logging.Serilog
```

**Includes:**
- Structured events, scopes, and enrichment
- Correlation-friendly property bags
- Any Serilog sink the host loads

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

Retry, breaker, timeout, and throttling without pulling in Polly.

**Installation:**
```bash
dotnet add package WorkflowForge.Extensions.Resilience
```

**Includes:**
- Retry middleware
- Circuit breaker
- Timeouts
- Simple rate limiting

**Usage:**
```csharp
// See package README for current API; base resilience is covered by Polly extension.
```

#### WorkflowForge.Extensions.Resilience.Polly

Polly policies wired to the foundry.

**Installation:**
```bash
dotnet add package WorkflowForge.Extensions.Resilience.Polly
```

**Includes:**
- Retry with backoff and jitter
- Circuit breaker thresholds
- Rate limits and bulkheads
- Timeouts
- Chained or combined policies
- Bind settings per environment from config

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
        "MaxRetryAttempts": 3,
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

Snapshots and middleware only; you supply storage behind `IWorkflowPersistenceProvider`.

**Installation:**
```bash
dotnet add package WorkflowForge.Extensions.Persistence
```

**Includes:**
- Snapshot model and save hooks
- Restore path that skips completed steps
- Your database, file store, or blob implementation

**Usage:**
```csharp
using WorkflowForge.Extensions.Persistence;
using WorkflowForge.Extensions.Persistence.Abstractions;

// Implement custom storage provider
public class MyStorageProvider : IWorkflowPersistenceProvider
{
    public Task SaveAsync(WorkflowExecutionSnapshot snapshot, CancellationToken ct = default)
    {
        // Save to your database, file system, etc.
        return Task.CompletedTask;
    }

    public Task<WorkflowExecutionSnapshot?> TryLoadAsync(
        Guid foundryExecutionId, Guid workflowId, CancellationToken ct = default)
    {
        // Load from your storage
        return Task.FromResult<WorkflowExecutionSnapshot?>(null);
    }

    public Task DeleteAsync(Guid foundryExecutionId, Guid workflowId, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

// Use in workflow
var provider = new MyStorageProvider();
var foundry = WorkflowForge.CreateFoundry("PersistentWorkflow");
foundry.UsePersistence(provider);
```

#### WorkflowForge.Extensions.Persistence.Recovery

Try resume from a snapshot, then run a fresh attempt with retry options.

**Installation:**
```bash
dotnet add package WorkflowForge.Extensions.Persistence.Recovery
```

**Includes:**
- Resume merges snapshot state before the next step
- Skips operations already marked complete
- Configurable retry/backoff after a failed resume or run

**Usage:**
```csharp
using WorkflowForge.Extensions.Persistence;
using WorkflowForge.Extensions.Persistence.Recovery;

var provider = new MyStorageProvider();

// Resume workflow from last checkpoint
using var foundry = WorkflowForge.CreateFoundry("RecoveredWorkflow");
foundry.UsePersistence(provider, options);

var smith = WorkflowForge.CreateSmith();
await smith.ForgeWithRecoveryAsync(
    workflow,
    foundry,
    provider,
    foundryKey,
    workflowKey,
    new RecoveryMiddlewareOptions { MaxRetryAttempts = 3, UseExponentialBackoff = true });
```

**See Also:**
- Sample 18: Persistence (BYO Storage)
- Sample 21: Recovery Only
- Sample 22: Recovery + Resilience

### Observability Extensions

#### WorkflowForge.Extensions.Observability.Performance

Per-operation timings and aggregates on the foundry.

**Installation:**
```bash
dotnet add package WorkflowForge.Extensions.Observability.Performance
```

**Includes:**
- Durations and throughput counters
- Success vs failure counts
- `GetPerformanceStatistics()` after a run

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
    Console.WriteLine($"{opStats.OperationName}: {opStats.AverageExecutionTime.TotalMilliseconds:F2}ms average");
}
```

### Persistence (Bring Your Own Storage)

For package install, `IWorkflowPersistenceProvider`, and the sample `MyStorageProvider` flow, see [Persistence Extensions](#persistence-extensions) above. This subsection describes checkpoint behavior and stable keys.

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

Persistence middleware writes a snapshot after each operation and, on resume, skips steps already marked complete. Snapshot data includes:
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
    new RecoveryMiddlewareOptions { MaxRetryAttempts = 5, BaseDelay = TimeSpan.FromMilliseconds(50), UseExponentialBackoff = true },
    cancellationToken);
```

Key points:
- Resume attempts restore foundry properties and skip completed steps.
- After resume, a fresh execution is attempted with retries (policy).
- If resume or execution ultimately fails, the last exception is surfaced to the caller.

##### Using resilience with recovery

You can combine base Resilience retry middleware with Recovery: retries cover transient failures inside one run, while Recovery picks up from the last checkpoint across runs.

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
    new RecoveryMiddlewareOptions { MaxRetryAttempts = 3, BaseDelay = TimeSpan.FromMilliseconds(100), UseExponentialBackoff = true },
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
var coordinator = new RecoveryCoordinator(provider, new RecoveryMiddlewareOptions { MaxRetryAttempts = 3 });
int recovered = await coordinator.ResumeAllAsync(
    () => WorkflowForge.CreateFoundry("Service"),
    () => BuildWorkflow(),
    catalog,
    cancellationToken);
```

#### WorkflowForge.Extensions.Observability.HealthChecks

Host-style checks (memory, thread pool) plus your own `IHealthCheck` types.

**Installation:**
```bash
dotnet add package WorkflowForge.Extensions.Observability.HealthChecks
```

**Includes:**
- Built-in probes for GC, memory, thread pool
- Register custom checks
- One aggregated status per round

**Usage:**
```csharp
using WorkflowForge.Extensions.Observability.HealthChecks;

var foundry = WorkflowForge.CreateFoundry("ProcessOrder");
var healthService = foundry.CreateHealthCheckService();
var results = await healthService.CheckHealthAsync();

Console.WriteLine($"Overall Status: {healthService.OverallStatus}");
foreach (var (name, result) in results)
{
    Console.WriteLine($"{name}: {result.Status} - {result.Description}");
}

// Custom health checks
healthService.RegisterHealthCheck(new DatabaseHealthCheck()); // implements IHealthCheck
results = await healthService.CheckHealthAsync();
```

#### WorkflowForge.Extensions.Observability.OpenTelemetry

Activities and tags for OTel exporters you already configure.

**Installation:**
```bash
dotnet add package WorkflowForge.Extensions.Observability.OpenTelemetry
```

**Includes:**
- `Activity` creation around operations
- Tags and events you add from workflow code
- Works with whatever exporter the app registers

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
using var activity = foundry.StartActivity("PaymentProcessed");
activity?.SetTag("amount", order.Amount.ToString());
```

## Extension Configuration Patterns

### Environment-Specific Configuration

Use `appsettings.{Environment}.json` or code per environment. Names and toggles should be obvious in diffs, not buried in defaults.

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
services.Configure<WorkflowForgeOptions>(foundryConfig);
var foundry = WorkflowForge.CreateFoundry("ProcessOrder");
```

### Validation Extension

#### WorkflowForge.Extensions.Validation

DataAnnotations before each operation; optional manual `ValidateAsync`.

**Installation:**
```bash
dotnet add package WorkflowForge.Extensions.Validation
```

**Includes:**
- Middleware runs validators from attributes
- Manual validation API when you need it
- Errors land in foundry properties for inspection

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

Append-only audit entries through `IAuditProvider` (your store).

**Installation:**
```bash
dotnet add package WorkflowForge.Extensions.Audit
```

**Includes:**
- Hooks for operation start / complete / fail
- `InMemoryAuditProvider` for tests
- You choose retention, PII rules, and storage

**Usage:**
```csharp
using WorkflowForge.Extensions.Audit;

// Create audit provider (in-memory for demo)
var auditProvider = new InMemoryAuditProvider();

// Enable audit logging
var foundry = WorkflowForge.CreateFoundry("OrderProcessing");
foundry.UseAudit(
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
foundry.UseAudit(auditProvider);
```

**Audit Entry Structure:**
```csharp
public class AuditEntry
{
    public Guid AuditId { get; init; }
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

## Extension Guidelines

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

---

## Related Documentation

- [Getting Started](../getting-started/getting-started.md)
- [Architecture](../architecture/overview.md)
- [Configuration](../core/configuration.md)
- [Operations Guide](../core/operations.md)
- [Events System](../core/events.md)
