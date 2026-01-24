# WorkflowForge Samples Catalog

<p align="center">
  <img src="../../icon.png" alt="WorkflowForge" width="120" height="120">
</p>

**Total Samples**: 24  
**Project**: `src/samples/WorkflowForge.Samples.BasicConsole`

## Table of Contents

- [Learning Path](#learning-path)
- [Category 1: Basic (Samples 1-4)](#category-1-basic-samples-1-4)
- [Category 2: Control Flow (Samples 5-8)](#category-2-control-flow-samples-5-8)
- [Category 3: Configuration & Middleware (Samples 9-12)](#category-3-configuration--middleware-samples-9-12)
- [Category 4: Extensions (Samples 13-18, 21-24)](#category-4-extensions-samples-13-18-21-24)
- [Category 5: Advanced (Samples 19-20)](#category-5-advanced-samples-19-20)
- [Key Patterns Across All Samples](#key-patterns-across-all-samples)
- [Sample Execution Order (Recommended)](#sample-execution-order-recommended)
- [Sample Coverage Matrix](#sample-coverage-matrix)

---

## Learning Path

**Beginner** → Samples 1-4  
**Intermediate** → Samples 5-12  
**Advanced** → Samples 13-24

---

## Category 1: Basic (Samples 1-4)

### Sample 1: HelloWorldSample.cs
**Purpose**: Simplest possible workflow  
**Demonstrates**:
- Creating a workflow with `WorkflowForge.CreateWorkflow()`
- Adding inline operations
- Executing with `WorkflowSmith`
- Basic logging

**Key Code Pattern**:
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

**Data Flow**: None  
**Complexity**: Trivial

---

### Sample 2: DataPassingSample.cs
**Purpose**: Dictionary-based data flow between operations  
**Demonstrates**:
- Using `foundry.SetProperty` / `GetPropertyOrDefault` (typed helpers)
- Passing data between operations
- Reading and writing properties

**Key Code Pattern**:
```csharp
foundry.SetProperty("Input", inputData);

// Operation 1
foundry.SetProperty("ProcessedData", result);

// Operation 2
var data = foundry.GetPropertyOrDefault<DataType>("ProcessedData");
```

**Data Flow**: Dictionary-based (PRIMARY pattern)  
**Complexity**: Simple

---

### Sample 3: MultipleOutcomesSample.cs
**Purpose**: Operations with different outcomes  
**Demonstrates**:
- Conditional logic within operations
- Different execution paths
- Property-based branching

**Key Code Pattern**:
```csharp
var approved = foundry.GetPropertyOrDefault<bool>("IsApproved");
if (approved) {
    // Happy path
} else {
    // Alternative path
}
```

**Data Flow**: Dictionary-based  
**Complexity**: Simple

---

### Sample 4: InlineOperationsSample.cs
**Purpose**: Compare inline vs. class-based operations  
**Demonstrates**:
- Inline async operations (`AddOperation(name, Func<...>)`)
- Inline sync operations (`AddOperation(name, Action<...>)`)
- Class-based operations (recommended for production)
- When to use each pattern

**Key Code Pattern**:
```csharp
// Inline async
.AddOperation("InlineAsync", async (foundry, ct) => { ... })

// Inline sync
.AddOperation("InlineSync", (foundry) => { ... })

// Class-based (production-recommended)
.AddOperation(new CustomOperation())
```

**Data Flow**: Dictionary-based  
**Complexity**: Simple  
**Recommendation**: Use class-based operations for production scenarios

---

## Category 2: Control Flow (Samples 5-8)

### Sample 5: ConditionalWorkflowSample.cs
**Purpose**: Conditional branching in workflows  
**Demonstrates**:
- `ConditionalWorkflowOperation` usage
- Predicate-based branching
- True/false operation execution

**Key Code Pattern**:
```csharp
var conditional = new ConditionalWorkflowOperation(
    name: "CheckCondition",
    predicate: foundry => foundry.GetPropertyOrDefault<bool>("Condition"),
    onTrue: new TrueOperation(),
    onFalse: new FalseOperation()
);
```

**Data Flow**: Dictionary-based  
**Complexity**: Intermediate

---

### Sample 6: ForEachLoopSample.cs
**Purpose**: Collection iteration  
**Demonstrates**:
- `ForEachWorkflowOperation` usage
- Sequential vs. parallel execution
- Shared vs. independent data strategies
- Result aggregation

**Key Code Pattern**:
```csharp
// Sequential
var sequential = ForEachWorkflowOperation.CreateSequential(
    items,
    itemOperation
);

// Parallel
var parallel = ForEachWorkflowOperation.CreateParallel(
    items,
    itemOperation,
    maxDegreeOfParallelism: 4
);

// Shared input (all operations get same input)
var shared = ForEachWorkflowOperation.CreateSharedInput(operations);
```

**Data Flow**: Dictionary-based + collection iteration  
**Complexity**: Intermediate

---

### Sample 7: ErrorHandlingSample.cs
**Purpose**: Exception handling and compensation  
**Demonstrates**:
- Try-catch in operations
- `RestoreAsync()` compensation
- Saga pattern
- Error event handling

**Key Code Pattern**:
```csharp
public override async Task<object?> ForgeAsync(...) {
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

**Data Flow**: Dictionary-based  
**Complexity**: Intermediate  
**Pattern**: Saga (compensation)

---

### Sample 8: BuiltInOperationsSample.cs
**Purpose**: Showcase all built-in operations  
**Demonstrates**:
- `LoggingOperation`
- `DelayOperation`
- `ConditionalWorkflowOperation`
- `ForEachWorkflowOperation`
- `ActionWorkflowOperation`
- `DelegateWorkflowOperation`

**Key Code Pattern**:
```csharp
.AddOperation(new LoggingOperation("Message", WorkflowForgeLogLevel.Information))
.AddOperation(new DelayOperation(TimeSpan.FromSeconds(1)))
.AddOperation(new ConditionalWorkflowOperation(...))
.AddOperation(ForEachWorkflowOperation.CreateSequential(...))
```

**Data Flow**: Dictionary-based  
**Complexity**: Intermediate

---

## Category 3: Configuration & Middleware (Samples 9-12)

### Sample 9: OptionsPatternSample.cs
**Purpose**: Configuration management  
**Demonstrates**:
- `WorkflowForgeOptions` configuration
- Options pattern
- Configuration binding
- appsettings.json integration

**Key Code Pattern**:
```csharp
services.Configure<WorkflowForgeOptions>(
    Configuration.GetSection(WorkflowForgeOptions.DefaultSectionName)
);

var config = serviceProvider.GetRequiredService<IOptions<WorkflowForgeOptions>>();
```

**Data Flow**: Dictionary-based  
**Complexity**: Intermediate  
**Pattern**: Options pattern

---

### Sample 10: ConfigurationProfilesSample.cs
**Purpose**: Environment-specific configuration  
**Demonstrates**:
- `WorkflowForgeOptions` presets
- Production vs. high-throughput tradeoffs

**Key Code Pattern**:
```csharp
var productionOptions = new WorkflowForgeOptions
{
    ContinueOnError = false,
    FailFastCompensation = false,
    ThrowOnCompensationError = true
};

var highThroughputOptions = new WorkflowForgeOptions
{
    ContinueOnError = true,
    FailFastCompensation = false,
    ThrowOnCompensationError = false
};

var productionFoundry = WorkflowForge.CreateFoundry("Workflow", options: productionOptions);
var highPerfFoundry = WorkflowForge.CreateFoundry("Workflow", options: highThroughputOptions);
```

**Data Flow**: Dictionary-based  
**Complexity**: Intermediate  
**Use Case**: Environment-specific deployments

---

### Sample 11: WorkflowEventsSample.cs
**Purpose**: Event system usage  
**Demonstrates**:
- `IWorkflowLifecycleEvents` (workflow events)
- `IOperationLifecycleEvents` (operation events)
- `ICompensationLifecycleEvents` (compensation events)
- Event subscription and unsubscription
- Strongly-typed event args

**Key Code Pattern**:
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

**Data Flow**: Dictionary-based  
**Complexity**: Intermediate  
**Pattern**: Event-driven architecture

---

### Sample 12: MiddlewareSample.cs
**Purpose**: Custom middleware creation  
**Demonstrates**:
- `IWorkflowOperationMiddleware` interface
- Russian Doll execution pattern
- Middleware ordering
- Cross-cutting concerns (timing, logging, validation)

**Key Code Pattern**:
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

**Data Flow**: Dictionary-based  
**Complexity**: Intermediate  
**Pattern**: Middleware/Pipeline

---

## Category 4: Extensions (Samples 13-18, 21-24)

### Sample 13: SerilogIntegrationSample.cs
**Purpose**: Structured logging with Serilog  
**Demonstrates**:
- Serilog extension usage
- Context enrichment
- Structured log output
- Multiple sinks (Console, File)

**Key Code Pattern**:
```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/workflow.log")
    .CreateLogger();

var serilogLogger = new SerilogWorkflowForgeLogger(Log.Logger);
var smith = WorkflowForge.CreateSmith(serilogLogger);
```

**Data Flow**: Dictionary-based  
**Complexity**: Intermediate  
**Extension**: WorkflowForge.Extensions.Logging.Serilog  
**Dependency Isolation**: Costura.Fody embedded

---

### Sample 14: PollyResilienceSample.cs
**Purpose**: Resilience patterns (retry, circuit breaker)  
**Demonstrates**:
- Polly extension usage
- Retry policies
- Circuit breaker policies
- Timeout policies
- Fallback handlers

**Key Code Pattern**:
```csharp
foundry.UsePolly(policy => policy
    .RetryAsync(3)
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30))
    .TimeoutAsync(TimeSpan.FromSeconds(10))
    .FallbackAsync(fallbackAction)
);
```

**Data Flow**: Dictionary-based  
**Complexity**: Intermediate  
**Extension**: WorkflowForge.Extensions.Resilience.Polly  
**Dependency Isolation**: Costura.Fody embedded

---

### Sample 15: OpenTelemetryObservabilitySample.cs
**Purpose**: Distributed tracing and metrics  
**Demonstrates**:
- OpenTelemetry extension usage
- Span creation and enrichment
- Distributed tracing
- Metrics collection

**Key Code Pattern**:
```csharp
services.AddOpenTelemetryTracing(builder => builder
    .AddWorkflowForgeInstrumentation()
    .AddJaegerExporter()
);

foundry.EnableOpenTelemetry();
```

**Data Flow**: Dictionary-based  
**Complexity**: Advanced  
**Extension**: WorkflowForge.Extensions.Observability.OpenTelemetry  
**Dependency Isolation**: Costura.Fody embedded

---

### Sample 16: HealthChecksSample.cs
**Purpose**: Health monitoring  
**Demonstrates**:
- Health checks extension usage
- Custom health checks
- ASP.NET Core integration
- Health check endpoints

**Key Code Pattern**:
```csharp
services.AddHealthChecks()
    .AddWorkflowForgeHealthCheck();

app.MapHealthChecks("/health");
```

**Data Flow**: Dictionary-based  
**Complexity**: Intermediate  
**Extension**: WorkflowForge.Extensions.Observability.HealthChecks  
**Dependency Isolation**: Costura.Fody embedded

---

### Sample 17: PerformanceMonitoringSample.cs
**Purpose**: Performance metrics collection  
**Demonstrates**:
- Performance monitoring extension
- Operation timing
- Memory allocation tracking
- Throughput metrics

**Key Code Pattern**:
```csharp
foundry.EnablePerformanceMonitoring();

var metrics = foundry.GetPerformanceMetrics();
Console.WriteLine($"Total Duration: {metrics.TotalDuration}ms");
Console.WriteLine($"Memory Allocated: {metrics.MemoryAllocated}KB");
```

**Data Flow**: Dictionary-based  
**Complexity**: Intermediate  
**Extension**: WorkflowForge.Extensions.Observability.Performance  
**Dependency Isolation**: None (pure WorkflowForge)

---

### Sample 18: PersistenceSample.cs
**Purpose**: Workflow state persistence  
**Demonstrates**:
- Persistence extension usage
- State checkpointing
- Custom persistence providers
- State snapshots

**Key Code Pattern**:
```csharp
var persistenceProvider = new FilePersistenceProvider("./state");
foundry.UsePersistence(persistenceProvider);

// Workflow state automatically checkpointed
```

**Data Flow**: Dictionary-based  
**Complexity**: Advanced  
**Extension**: WorkflowForge.Extensions.Persistence  
**Dependency Isolation**: None (pure WorkflowForge)

---

### Sample 21: RecoveryOnlySample.cs
**Purpose**: Workflow recovery and resume  
**Demonstrates**:
- Recovery extension usage
- Resume from checkpoint
- Replay failed operations
- State reconstruction

**Key Code Pattern**:
```csharp
var recoveryProvider = new FileRecoveryProvider("./recovery");
foundry.UseRecovery(recoveryProvider);

// Resume workflow from checkpoint
await smith.ResumeAsync(workflowId);
```

**Data Flow**: Dictionary-based  
**Complexity**: Advanced  
**Extension**: WorkflowForge.Extensions.Persistence.Recovery  
**Dependency Isolation**: None (pure WorkflowForge)

---

### Sample 22: ResilienceRecoverySample.cs
**Purpose**: Combined resilience + recovery  
**Demonstrates**:
- Using multiple extensions together
- Resilience policies with recovery
- Complex failure scenarios
- Multi-layer error handling

**Key Code Pattern**:
```csharp
foundry.UsePolly(policy => policy.RetryAsync(3));
foundry.UseRecovery(recoveryProvider);

// Workflow benefits from both retry and recovery
```

**Data Flow**: Dictionary-based  
**Complexity**: Advanced  
**Extensions**: Resilience.Polly + Persistence.Recovery  
**Pattern**: Defense in depth

---

### Sample 23: ValidationSample.cs
**Purpose**: FluentValidation integration  
**Demonstrates**:
- Validation extension usage
- FluentValidation bridge
- Middleware-based validation
- Manual validation
- Custom validators

**Key Code Pattern**:
```csharp
public class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

foundry.AddValidation(
    new OrderValidator(),
    f => f.GetPropertyOrDefault<Order>("Order"),
    throwOnFailure: true
);
```

**Data Flow**: Dictionary-based  
**Complexity**: Intermediate  
**Extension**: WorkflowForge.Extensions.Validation  
**Dependency Isolation**: Costura.Fody embedded

---

### Sample 24: AuditSample.cs
**Purpose**: Comprehensive audit logging  
**Demonstrates**:
- Audit extension usage
- Pluggable audit providers
- In-memory provider
- Custom audit entries
- Compliance logging

**Key Code Pattern**:
```csharp
var auditProvider = new InMemoryAuditProvider();
foundry.EnableAudit(
    auditProvider,
    initiatedBy: "user@example.com",
    includeMetadata: true
);

// All operations automatically audited
// Access audit entries
var entries = auditProvider.Entries;
```

**Data Flow**: Dictionary-based  
**Complexity**: Intermediate  
**Extension**: WorkflowForge.Extensions.Audit  
**Dependency Isolation**: None (pure WorkflowForge)

---

## Category 5: Advanced (Samples 19-20)

### Sample 19: ComprehensiveIntegrationSample.cs
**Purpose**: All features combined  
**Demonstrates**:
- Multiple extensions together
- Complex workflow scenarios
- Real-world patterns
- Production best practices

**Key Code Pattern**:
```csharp
foundry.EnablePerformanceMonitoring();
foundry.UsePolly(policy => policy.RetryAsync(3));
foundry.AddValidation(validator, extractor);
foundry.EnableAudit(auditProvider);
foundry.UsePersistence(persistenceProvider);
foundry.AddMiddleware(new TimingMiddleware());

// Complex workflow with all features enabled
```

**Data Flow**: Dictionary-based  
**Complexity**: Advanced  
**Extensions**: All combined  
**Use Case**: Production-grade workflows

---

### Sample 20: OperationCreationPatternsSample.cs
**Purpose**: Different ways to create operations  
**Demonstrates**:
- Class-based operations (recommended)
- Inline async operations
- Inline sync operations
- Delegate operations
- Action operations
- Generic typed operations
- When to use each pattern

**Key Code Pattern**:
```csharp
// Class-based (production-recommended)
public class ProcessOrderOperation : WorkflowOperationBase
{
    public override async Task<object?> ForgeAsync(...) { ... }
}

// Inline async
.AddOperation("Process", async (foundry, ct) => { ... })

// Inline sync
.AddOperation("Process", (foundry) => { ... })

// Typed generic
public class ProcessOperation : IWorkflowOperation<Order, OrderResult>
{
    public async Task<OrderResult> ForgeAsync(Order input, ...) { ... }
}
```

**Data Flow**: Dictionary-based + type-safe  
**Complexity**: Advanced  
**Recommendation**: Class-based operations for production scenarios

---

## Key Patterns Across All Samples

1. **Data Flow**: Dictionary-based via `foundry.Properties` (24/24 samples)
2. **Type Safety**: Generic `IWorkflowOperation<TInput, TOutput>` mentioned but rarely used
3. **Production Focus**: Class-based operations recommended over inline
4. **Extension Usage**: 10/24 samples demonstrate extensions
5. **Error Handling**: Explicit compensation in 1 sample, implicit in all via framework
6. **Configuration**: 2 samples dedicated to configuration patterns
7. **Events**: 1 sample dedicated to event system, events used in others
8. **Middleware**: 1 sample dedicated, middleware used in many extensions

---

## Sample Execution Order (Recommended)

**For New Users** (Learning Path):
1. HelloWorldSample (1)
2. DataPassingSample (2)
3. InlineOperationsSample (4)
4. MultipleOutcomesSample (3)
5. ConditionalWorkflowSample (5)
6. ForEachLoopSample (6)
7. BuiltInOperationsSample (8)
8. ErrorHandlingSample (7)
9. OptionsPatternSample (9)
10. ConfigurationProfilesSample (10)
11. WorkflowEventsSample (11)
12. MiddlewareSample (12)
13. SerilogIntegrationSample (13)
14. PerformanceMonitoringSample (17)
15. ValidationSample (23)
16. AuditSample (24)
17. PollyResilienceSample (14)
18. PersistenceSample (18)
19. RecoveryOnlySample (21)
20. ResilienceRecoverySample (22)
21. HealthChecksSample (16)
22. OpenTelemetryObservabilitySample (15)
23. OperationCreationPatternsSample (20)
24. ComprehensiveIntegrationSample (19)

---

## Sample Coverage Matrix

| Feature | Sample(s) |
|---------|-----------|
| Basic workflow creation | 1 |
| Data passing (dictionary) | 2, 3, ALL |
| Inline operations | 4, 20 |
| Class-based operations | 4, 20, ALL |
| Conditional branching | 3, 5, 8 |
| ForEach loops | 6, 8 |
| Error handling | 7 |
| Built-in operations | 8 |
| Configuration | 9, 10 |
| Events | 11 |
| Middleware | 12 |
| Logging (Serilog) | 13 |
| Resilience (Polly) | 14, 22 |
| OpenTelemetry | 15 |
| Health checks | 16 |
| Performance monitoring | 17 |
| Persistence | 18, 22 |
| Recovery | 21, 22 |
| Validation | 23 |
| Audit | 24 |
| Comprehensive integration | 19 |
| Operation patterns | 20 |

---

## Related Documentation

- **[Getting Started](getting-started.md)** - Learn the basics before exploring samples
- **[Operations Guide](../core/operations.md)** - Detailed operation documentation
- **[Extensions](../extensions/index.md)** - Extension usage in samples
- **[Configuration](../core/configuration.md)** - Configuration patterns in samples
- **[Events System](../core/events.md)** - Event handling in samples

**← Back to [Documentation Home](../index.md)**
