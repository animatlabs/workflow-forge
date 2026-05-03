---
title: Samples Catalog
description: 33 progressive samples covering basic workflows, compensation, parallel execution, and real-world patterns in WorkflowForge.
---

# WorkflowForge Samples Catalog

**Total Samples**: 33  
**Project**: `src/samples/WorkflowForge.Samples.BasicConsole`

> Samples use **`WorkflowOperationBase`** for custom steps unless noted.

## Table of Contents

- [Learning Path](#learning-path)
- [Category 1: Basic (Samples 1-4)](#category-1-basic-samples-1-4)
- [Category 2: Control Flow (Samples 5-8)](#category-2-control-flow-samples-5-8)
- [Category 3: Configuration & Middleware (Samples 9-12)](#category-3-configuration--middleware-samples-9-12)
- [Category 4: Extensions (Samples 13-18, 21-25)](#category-4-extensions-samples-13-18-21-25)
- [Category 5: Advanced (Samples 19-20)](#category-5-advanced-samples-19-20)
- [Category 6: Onboarding & Guidelines (Samples 26-33)](#category-6-onboarding--guidelines-samples-26-33)
- [Key Patterns Across All Samples](#key-patterns-across-all-samples)
- [Sample Execution Order (Recommended)](#sample-execution-order-recommended)
- [Sample Coverage Matrix](#sample-coverage-matrix)

---

## Learning Path

**Beginner** → Samples 1-4  
**Intermediate** → Samples 5-12  
**Advanced** → Samples 13-33

---

## Category 1: Basic (Samples 1-4)

### Samples 1–2: `HelloWorldSample.cs`, `DataPassingSample.cs`

- **1**: Minimal `CreateWorkflow`, one inline op, `CreateSmith`, `ForgeAsync`.
- **2**: `SetProperty` / `GetPropertyOrDefault` between steps.

```csharp
var workflow = WorkflowForge.CreateWorkflow("HelloWorld")
    .AddOperation("SayHello", (foundry, ct) => {
        foundry.Logger.LogInformation("Hello, World!");
        return Task.CompletedTask;
    })
    .Build();

using var smith = WorkflowForge.CreateSmith();
await smith.ForgeAsync(workflow);
```

```csharp
foundry.SetProperty("Input", inputData);

// Operation 1
foundry.SetProperty("ProcessedData", result);

// Operation 2
var data = foundry.GetPropertyOrDefault<DataType>("ProcessedData");
```

---

### Sample 3: MultipleOutcomesSample.cs

Reads flags like `IsApproved` from the foundry and branches.

```csharp
var approved = foundry.GetPropertyOrDefault<bool>("IsApproved");
if (approved) {
    // Happy path
} else {
    // Alternative path
}
```

---

### Sample 4: ClassBasedOperationsSample.cs

`WorkflowOperationBase` subclasses, output chaining, and shared properties. Use this when a step outgrows a lambda.

**Key Code Pattern**:
```csharp
public sealed class ValidateOrderOperation : WorkflowOperationBase
{
    public override string Name => "ValidateOrder";

    protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken ct)
    {
        var userName = foundry.Properties["user_name"]?.ToString();
        var orderTotal = (decimal)foundry.Properties["order_total"]!;
        
        foundry.SetProperty("order_valid", orderTotal > 0 && !string.IsNullOrEmpty(userName));
        return Task.FromResult(inputData);
    }
}

// Usage
foundry
    .WithOperation(new ValidateOrderOperation())
    .WithOperation(new CalculateShippingOperation())
    .WithOperation(new ProcessPaymentOperation());
```

Prefer `WorkflowOperationBase` once the step needs structure or tests.

---

## Category 2: Control Flow (Samples 5-8)

### Sample 5: ConditionalWorkflowSample.cs

`ConditionalWorkflowOperation`: predicate picks the true or false child op.

```csharp
var conditional = new ConditionalWorkflowOperation(
    condition: (input, foundry) => foundry.GetPropertyOrDefault<bool>("Condition"),
    trueOperation: new TrueOperation(),
    falseOperation: new FalseOperation(),
    name: "CheckCondition"
);
```

---

### Sample 6: ForEachLoopSample.cs

`ForEachWorkflowOperation`: shared input, split collection, or no input, with optional concurrency cap.

**Snippet**:
```csharp
// Shared input - all operations receive the same input data
var sharedInput = ForEachWorkflowOperation.CreateSharedInput(
    new[] { op1, op2, op3 },
    maxConcurrency: 4,
    name: "ProcessAllItems"
);

// Split input - input collection is distributed among operations
var splitInput = ForEachWorkflowOperation.CreateSplitInput(
    new[] { op1, op2, op3 },
    maxConcurrency: 2
);

// No input - operations receive null input
var noInput = ForEachWorkflowOperation.CreateNoInput(operations);
```

---

### Sample 7: ErrorHandlingSample.cs

Try/catch in `ForgeAsyncCore`, `RestoreAsync` undo, smith/foundry failure events.

**Snippet**:
```csharp
protected override async Task<object?> ForgeAsyncCore(...) {
    try {
        // Operation logic
    } catch (Exception ex) {
        // Handle error
        throw;
    }
}

public override async Task RestoreAsync(...) {
    // Compensate/rollback
}
```

---

### Sample 8: BuiltInOperationsSample.cs

One workflow touches `LoggingOperation`, `DelayOperation`, `ConditionalWorkflowOperation`, `ForEachWorkflowOperation`, `ActionWorkflowOperation`, and `DelegateWorkflowOperation`.

**Snippet**:
```csharp
.AddOperation(new LoggingOperation("Message", WorkflowForgeLogLevel.Information))
.AddOperation(new DelayOperation(TimeSpan.FromSeconds(1)))
.AddOperation(new ConditionalWorkflowOperation(...))
.AddOperation(ForEachWorkflowOperation.CreateSharedInput(operations))
```

---

## Category 3: Configuration & Middleware (Samples 9-12)

### Sample 9: OptionsPatternSample.cs

Bind `WorkflowForgeOptions` from configuration; read via `IOptions<WorkflowForgeOptions>`.

```csharp
services.Configure<WorkflowForgeOptions>(
    Configuration.GetSection(WorkflowForgeOptions.DefaultSectionName)
);

var config = serviceProvider.GetRequiredService<IOptions<WorkflowForgeOptions>>();
```

---

### Sample 10: ConfigurationProfilesSample.cs

Two presets: strict prod vs higher throughput (`ContinueOnError`).

```csharp
var productionOptions = new WorkflowForgeOptions
{
    Enabled = true,
    ContinueOnError = false,
    FailFastCompensation = false,
    ThrowOnCompensationError = true,
    EnableOutputChaining = true
};

var highThroughputOptions = new WorkflowForgeOptions
{
    Enabled = true,
    ContinueOnError = true,
    FailFastCompensation = false,
    ThrowOnCompensationError = false,
    EnableOutputChaining = true
};

var productionFoundry = WorkflowForge.CreateFoundry("Workflow", options: productionOptions);
var highPerfFoundry = WorkflowForge.CreateFoundry("Workflow", options: highThroughputOptions);
```

---

### Sample 11: WorkflowEventsSample.cs

Subscribe on the smith for workflow/compensation hooks; on the foundry for per-op hooks.

```csharp
smith.WorkflowStarted += (sender, args) => { ... };
smith.WorkflowCompleted += (sender, args) => { ... };
smith.WorkflowFailed += (sender, args) => { ... };

foundry.OperationStarted += (sender, args) => { ... };
foundry.OperationCompleted += (sender, args) => { ... };
foundry.OperationFailed += (sender, args) => { ... };

smith.CompensationTriggered += (sender, args) => { ... };
smith.CompensationCompleted += (sender, args) => { ... };
```

---

### Sample 12: MiddlewareSample.cs

Custom `IWorkflowOperationMiddleware`: ordered wrappers (timing, logging, checks).

**Snippet**:
```csharp
public class TimingMiddleware : IWorkflowOperationMiddleware
{
    public async Task<object?> ExecuteAsync(
        IWorkflowOperation operation,
        IWorkflowFoundry foundry,
        object? inputData,
        Func<CancellationToken, Task<object?>> next,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try {
            return await next(cancellationToken);
        } finally {
            sw.Stop();
            foundry.SetProperty($"{operation.Name}.ExecutionTime", sw.ElapsedMilliseconds);
        }
    }
}

foundry.AddMiddleware(new TimingMiddleware());
```

---

## Category 4: Extensions (Samples 13-18, 21-25)

### Sample 13: SerilogIntegrationSample.cs

`SerilogLoggerFactory` produces `IWorkflowForgeLogger` for `CreateSmith` or your foundry path.

```csharp
using WorkflowForge.Extensions.Logging.Serilog;

var logger = SerilogLoggerFactory.CreateLogger(new SerilogLoggerOptions
{
    MinimumLevel = "Information",
    EnableConsoleSink = true
});
var smith = WorkflowForge.CreateSmith(logger);
```

**Extension**: WorkflowForge.Extensions.Logging.Serilog  
**Bundling**: ILRepack internalizes Serilog.

---

### Sample 14: PollyResilienceSample.cs

Polly retry, breaker, timeout; `UsePollyComprehensive` vs single-policy calls.

```csharp
// Comprehensive policy with retry, circuit breaker, and timeout
foundry.UsePollyComprehensive(
    maxRetryAttempts: 3,
    circuitBreakerThreshold: 5,
    circuitBreakerDuration: TimeSpan.FromSeconds(30),
    timeoutDuration: TimeSpan.FromSeconds(10));

// Or use individual policies
foundry.UsePollyRetry(maxRetryAttempts: 3);
foundry.UsePollyCircuitBreaker(failureThreshold: 5, durationOfBreak: TimeSpan.FromSeconds(30));
foundry.UsePollyTimeout(TimeSpan.FromSeconds(10));
```

**Extension**: WorkflowForge.Extensions.Resilience.Polly  
**Bundling**: ILRepack internalizes Polly.

---

### Sample 15: OpenTelemetryObservabilitySample.cs

`EnableOpenTelemetry` on the foundry; exporters live in your host setup.

```csharp
using WorkflowForge.Extensions.Observability.OpenTelemetry;

var foundry = WorkflowForge.CreateFoundry("ProcessOrder");
foundry.EnableOpenTelemetry(new WorkflowForgeOpenTelemetryOptions
{
    ServiceName = "OrderService",
    ServiceVersion = "1.0.0"
});
```

**Extension**: WorkflowForge.Extensions.Observability.OpenTelemetry  
**Bundling**: ILRepack internalizes OpenTelemetry.

---

### Sample 16: HealthChecksSample.cs

`CreateHealthCheckService` / `CheckFoundryHealthAsync` for probe-style checks (wire ASP.NET in your app).

```csharp
var healthService = foundry.CreateHealthCheckService();
var overallStatus = await foundry.CheckFoundryHealthAsync(healthService);
```

**Extension**: WorkflowForge.Extensions.Observability.HealthChecks  
**Dependencies**: Uses Microsoft/System types alongside the extension; no ILRepack merge for those.

---

### Sample 17: PerformanceMonitoringSample.cs

`EnablePerformanceMonitoring` plus `GetPerformanceStatistics()` (duration and allocation counters).

```csharp
foundry.EnablePerformanceMonitoring();

var stats = foundry.GetPerformanceStatistics();
Console.WriteLine($"Total Duration: {stats.TotalDuration}ms");
Console.WriteLine($"Memory Allocated: {stats.TotalMemoryAllocated}KB");
```

**Extension**: WorkflowForge.Extensions.Observability.Performance (no extra third-party packages).

---

### Sample 18: PersistenceSample.cs

`UsePersistence` with a provider (sample uses `FilePersistenceProvider`); checkpoints follow your options.

```csharp
var persistenceProvider = new FilePersistenceProvider("./state");
foundry.UsePersistence(persistenceProvider);

// Workflow state automatically checkpointed
```

**Extension**: WorkflowForge.Extensions.Persistence (bring-your-own storage).

---

### Sample 21: RecoveryOnlySample.cs

`ForgeWithRecoveryAsync` with the same provider/keys you used when saving state.

```csharp
var provider = new MyPersistenceProvider();
using var foundry = WorkflowForge.CreateFoundry("RecoveredWorkflow");
foundry.UsePersistence(provider, options);

// Resume workflow from last checkpoint
await smith.ForgeWithRecoveryAsync(
    workflow, foundry, provider,
    foundryKey, workflowKey);
```

**Extension**: WorkflowForge.Extensions.Persistence.Recovery.

---

### Sample 22: ResilienceRecoverySample.cs

Polly retries transient failures; recovery replays after restart.

```csharp
// Add resilience middleware to foundry
foundry.UsePollyRetry(maxRetryAttempts: 3);

// Execute with recovery support
await smith.ForgeWithRecoveryAsync(
    workflow, foundry, persistenceProvider,
    foundryKey, workflowKey);

// Workflow benefits from both retry and recovery
```

**Extensions**: Resilience.Polly and Persistence.Recovery together.

---

### Sample 23: ValidationSample.cs

`UseValidation` runs DataAnnotations on a model pulled from the foundry (here `Order`).

```csharp
using System.ComponentModel.DataAnnotations;

public class Order
{
    [Required]
    public string CustomerId { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
}

foundry.UseValidation(
    f => f.GetPropertyOrDefault<Order>("Order"));
```

**Extension**: WorkflowForge.Extensions.Validation (DataAnnotations only).

---

### Sample 24: AuditSample.cs

`UseAudit` with `InMemoryAuditProvider` in the sample; plug your own `IAuditProvider` for storage.

```csharp
var auditProvider = new InMemoryAuditProvider();
foundry.UseAudit(
    auditProvider,
    initiatedBy: "user@example.com",
    includeMetadata: true
);

// All operations automatically audited
// Access audit entries
var entries = auditProvider.Entries;
```

**Extension**: WorkflowForge.Extensions.Audit (no extra third-party packages).

---

### Sample 25: ConfigurationSample.cs

Registers audit, validation, persistence, recovery, and Polly from `appsettings.json`, then calls each `Use*` only when options say enabled.

```csharp
// Setup DI with configuration
var services = new ServiceCollection();
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

// Register extension configurations
services.AddAuditConfiguration(configuration);
services.AddValidationConfiguration(configuration);
services.AddPersistenceConfiguration(configuration);
services.AddRecoveryConfiguration(configuration);
services.AddWorkflowForgePolly(configuration);

// Check if extension is enabled before use
var auditOptions = serviceProvider.GetRequiredService<IOptions<AuditOptions>>();
if (auditOptions.Value.Enabled)
{
    foundry.UseAudit(auditProvider);
}
```

Options toggles plus the usual registration helpers.

---

## Category 5: Advanced (Samples 19-20)

### Sample 19: ComprehensiveIntegrationSample.cs

Performance stats, Polly retry, validation, audit, persistence, and timing on one foundry.

```csharp
foundry.EnablePerformanceMonitoring();
foundry.UsePollyRetry(maxRetryAttempts: 3);
foundry.UseValidation<OrderDto>(f => f.GetPropertyOrDefault<OrderDto>("Order"));
foundry.UseAudit(auditProvider);
foundry.UsePersistence(persistenceProvider);
foundry.UseTiming();

// Complex workflow with all features enabled
```

**Extensions**: Performance, Polly, Validation, Audit, Persistence (sample wiring).

---

### Sample 20: OperationCreationPatternsSample.cs

Shows `WorkflowOperationBase`, inline async/sync ops, and `WorkflowOperationBase<TInput, TOutput>`.

```csharp
// Class-based (production-recommended) - extend WorkflowOperationBase
public class ProcessOrderOperation : WorkflowOperationBase
{
    protected override async Task<object?> ForgeAsyncCore(...) { ... }
}

// Inline async
.AddOperation("Process", async (foundry, ct) => { ... })

// Inline sync
.AddOperation("Process", (foundry) => { ... })

// Typed generic - extend WorkflowOperationBase<TInput, TOutput>
public class ProcessOperation : WorkflowOperationBase<Order, OrderResult>
{
    protected override async Task<OrderResult> ForgeAsyncCore(Order input, ...) { ... }
}
```

Classes when you need DI or tests; lambdas for glue.

---

## Category 6: Onboarding & Guidelines (Samples 26-33)

### Sample 26: DependencyInjectionSample.cs

`AddWorkflowForge` / `AddWorkflowSmith`, resolve `IWorkflowSmith`, resolve app services inside ops.

```csharp
var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddSingleton<IWorkflowForgeLogger>(_ => new ConsoleLogger("WF-DI"));

services.AddWorkflowForge(configuration);
services.AddWorkflowSmith();

services.AddSingleton<IOrderIdGenerator, OrderIdGenerator>();

using var provider = services.BuildServiceProvider();
var smith = provider.GetRequiredService<IWorkflowSmith>();

var workflow = WorkflowForge.CreateWorkflow("DiConfiguredWorkflow")
    .AddOperation(new GenerateOrderIdOperation())
    .Build();

await smith.ForgeAsync(workflow);
```

**Extension**: WorkflowForge.Extensions.DependencyInjection.

---

### Sample 27: WorkflowMiddlewareSample.cs

`IWorkflowMiddleware` wraps the whole workflow on the smith; operation middleware lives on the foundry.

```csharp
public sealed class WorkflowTimingMiddleware : IWorkflowMiddleware
{
    public async Task ExecuteAsync(
        IWorkflow workflow, 
        IWorkflowFoundry foundry, 
        Func<Task> next, 
        CancellationToken cancellationToken)
    {
        var start = DateTimeOffset.UtcNow;
        foundry.Logger.LogInformation("[WorkflowTiming] Starting {WorkflowName}", workflow.Name);
        
        await next().ConfigureAwait(false);
        
        var duration = DateTimeOffset.UtcNow - start;
        foundry.Logger.LogInformation("[WorkflowTiming] Completed in {DurationMs}ms", duration.TotalMilliseconds.ToString("F0"));
    }
}

var smith = WorkflowForge.CreateSmith(logger);
smith.AddWorkflowMiddleware(new WorkflowTimingMiddleware());
smith.AddWorkflowMiddleware(new WorkflowAuditMiddleware());

await smith.ForgeAsync(workflow);
```

---

### Sample 28: CancellationAndTimeoutSample.cs

`OperationTimeoutMiddleware` vs cancelling `ForgeAsync`; handles `TimeoutException` and `OperationCanceledException`.

```csharp
// Timeout via middleware
using var foundry = WorkflowForge.CreateFoundry("TimeoutDemo");
foundry.AddMiddleware(new OperationTimeoutMiddleware(TimeSpan.FromMilliseconds(100), foundry.Logger));
foundry.WithOperation(new SlowOperation("SlowOp", 300));

try
{
    await foundry.ForgeAsync();
}
catch (TimeoutException ex)
{
    Console.WriteLine($"Timeout triggered: {ex.Message}");
}

// Cancellation via token
using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
try
{
    await foundry.ForgeAsync(cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation cancelled.");
}
```

---

### Sample 29: ContinueOnErrorSample.cs

`ContinueOnError = true` keeps the list going, then one `AggregateException` at the end.

```csharp
var options = new WorkflowForgeOptions { ContinueOnError = true };
using var foundry = WorkflowForge.CreateFoundry("ContinueOnErrorDemo", options: options);

foundry
    .WithOperation(new SuccessOperation("First"))
    .WithOperation(new FailingOperation("FailurePoint"))
    .WithOperation(new SuccessOperation("Final"));

try
{
    await foundry.ForgeAsync();
}
catch (AggregateException ex)
{
    Console.WriteLine($"AggregateException with {ex.InnerExceptions.Count} error(s).");
}

// Final operation still executed despite earlier failure
Console.WriteLine($"Final operation ran: {foundry.GetPropertyOrDefault<bool>("final.ran", false)}");
```

---

### Sample 30: CompensationBehaviorSample.cs

`FailFastCompensation` and `ThrowOnCompensationError` change how restore failures propagate.

```csharp
var options = new WorkflowForgeOptions
{
    FailFastCompensation = true,
    ThrowOnCompensationError = true
};

using var foundry = WorkflowForge.CreateFoundry("CompensationDemo", options: options);

var workflow = WorkflowForge.CreateWorkflow("CompensationWorkflow")
    .AddOperation(new CompensatableOperation("StepA"))
    .AddOperation(new CompensatableOperation("StepB"))
    .AddOperation(new FailingOperation("FailurePoint"))
    .Build();

try
{
    await smith.ForgeAsync(workflow, foundry);
}
catch (AggregateException ex)
{
    Console.WriteLine($"Compensation errors: {ex.InnerExceptions.Count}");
}
```

---

### Sample 31: FoundryReuseSample.cs

One foundry from `smith.CreateFoundry()`, multiple `ForgeAsync` calls; properties accumulate.

```csharp
var smith = WorkflowForge.CreateSmith();
using var foundry = smith.CreateFoundry();

var workflowA = WorkflowForge.CreateWorkflow("ReuseA")
    .AddOperation(new RecordRunOperation("FirstWorkflow"))
    .Build();

var workflowB = WorkflowForge.CreateWorkflow("ReuseB")
    .AddOperation(new RecordRunOperation("SecondWorkflow"))
    .Build();

await smith.ForgeAsync(workflowA, foundry);
await smith.ForgeAsync(workflowB, foundry);

// Properties persist across workflow executions
var runs = foundry.GetPropertyOrDefault<List<string>>("runs") ?? new();
Console.WriteLine($"Runs: {string.Join(", ", runs)}"); // Output: FirstWorkflow, SecondWorkflow
```

---

### Sample 32: OutputChainingSample.cs

Each op's return becomes the next op's `inputData` unless `EnableOutputChaining` is false.

```csharp
using var foundry = WorkflowForge.CreateFoundry("OutputChainingDemo");

foundry
    .WithOperation(new SeedNumberOperation())      // Returns: 7
    .WithOperation(new MultiplyOperation(3))       // Receives: 7, Returns: 21
    .WithOperation(new FormatResultOperation());   // Receives: 21, Returns: "Result: 21"

await foundry.ForgeAsync();

// Each operation receives the previous operation's output as inputData (extend WorkflowOperationBase)
protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken ct)
{
    var value = inputData is int number ? number : 0;
    var result = value * _multiplier;
    Console.WriteLine($"Multiplied {value} by {_multiplier} = {result}");
    return Task.FromResult<object?>(result);
}
```

---

### Sample 33: ServiceProviderResolutionSample.cs

Pass one `IServiceProvider` to `CreateSmith`; resolve from `foundry.ServiceProvider` in `ForgeAsyncCore`.

```csharp
var services = new ServiceCollection();
services.AddSingleton<IWorkflowForgeLogger>(_ => new ConsoleLogger("WF-Services"));
services.AddSingleton<IPriceCalculator, PriceCalculator>();
using var provider = services.BuildServiceProvider();

var smith = WorkflowForge.CreateSmith(
    provider.GetRequiredService<IWorkflowForgeLogger>(), 
    provider);

var workflow = WorkflowForge.CreateWorkflow("ServiceProviderDemo")
    .AddOperation(new CalculateTotalOperation())
    .Build();

await smith.ForgeAsync(workflow);

// Inside the operation (extend WorkflowOperationBase)
protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken ct)
{
    var calculator = foundry.ServiceProvider?.GetRequiredService<IPriceCalculator>()
        ?? throw new InvalidOperationException("IPriceCalculator not registered.");
    
    var total = calculator.CalculateTotal(subtotal: 120m, taxRate: 0.08m);
    return Task.FromResult<object?>(total);
}
```

---

## Key Patterns Across All Samples

- Almost every sample uses `foundry.Properties` for shared state.
- Class-based `WorkflowOperationBase` types are the default recommendation once logic grows past a short lambda.
- Thirteen samples turn on an extension package (logging, resilience, observability, validation, audit, persistence, recovery, DI, and similar).
- Two samples walk compensation in detail.
- The runtime still calls `RestoreAsync` on failure in every run.
- Configuration-focused samples are 9, 10, and 25.
- Sample 11 covers lifecycle events in detail.
- Sample 12 shows custom operation middleware.
- Several extensions add their own middleware internally.

---

## Sample Execution Order (Recommended)

Suggested order for a first read:

| Phase | Samples (numbers) |
|-------|---------------------|
| First runs | 1, 2, 4, 3 |
| Branching and parallelism | 5, 6, 8, 7 |
| Options, profiles, events, op middleware | 9, 10, 11, 12 |
| DI, chaining, services | 26, 32, 33 |
| Observability and validation | 13, 17, 23, 24, 25 |
| Resilience and lifecycle edge cases | 14, 28, 29, 30, 31, 27 |
| Persistence stack | 18, 21, 22, 16, 15 |
| Patterns and “everything on” | 20, 19 |

Within a phase you can reorder. Sample 19 assumes you have skimmed the extensions it stacks.

---

## Sample Coverage Matrix

| Feature | Sample(s) |
|---------|-----------|
| Basic workflow creation | 1 |
| Data passing (dictionary) | 2, 3, ALL |
| Class-based operations | 4, 20, ALL |
| Operation patterns | 20 |
| Conditional branching | 3, 5, 8 |
| ForEach loops | 6, 8 |
| Error handling | 7, 29, 30 |
| Built-in operations | 8 |
| Configuration | 9, 10, 25 |
| Events | 11 |
| Middleware (operation) | 12 |
| Middleware (workflow) | 27 |
| Logging (Serilog) | 13 |
| Resilience (Polly) | 14, 22 |
| OpenTelemetry | 15 |
| Health checks | 16 |
| Performance monitoring | 17 |
| Persistence | 18, 22 |
| Recovery | 21, 22 |
| Validation | 23 |
| Audit | 24 |
| Configuration-driven extensions | 25 |
| Dependency injection | 26 |
| Cancellation + timeout | 28 |
| ContinueOnError | 29 |
| Compensation behaviors | 30 |
| Foundry reuse | 31 |
| Output chaining | 32 |
| Service provider resolution | 33 |
| Multi-extension stack | 19 |

---

## Related Documentation

- [Getting Started](getting-started.md) - Install and first workflow
- [Operations Guide](../core/operations.md) - Operation types and patterns
- [Extensions](../extensions/index.md) - Optional packages
- [Configuration](../core/configuration.md) - Options and appsettings
- [Events System](../core/events.md) - Lifecycle hooks
