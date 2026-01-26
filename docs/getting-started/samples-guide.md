# WorkflowForge Samples Catalog

<p align="center">
  <img src="https://raw.githubusercontent.com/animatlabs/workflow-forge/main/icon.png" alt="WorkflowForge" width="120" height="120">
</p>

**Total Samples**: 33  
**Project**: `src/samples/WorkflowForge.Samples.BasicConsole`

## Table of Contents

- [Learning Path](#learning-path)
- [Category 1: Basic (Samples 1-4)](#category-1-basic-samples-1-4)
- [Category 2: Control Flow (Samples 5-8)](#category-2-control-flow-samples-5-8)
- [Category 3: Configuration & Middleware (Samples 9-12)](#category-3-configuration--middleware-samples-9-12)
- [Category 4: Extensions (Samples 13-18, 21-25)](#category-4-extensions-samples-13-18-21-25)
- [Category 5: Advanced (Samples 19-20)](#category-5-advanced-samples-19-20)
- [Category 6: Onboarding & Best Practices (Samples 26-33)](#category-6-onboarding--best-practices-samples-26-33)
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

### Sample 4: ClassBasedOperationsSample.cs
**Purpose**: Demonstrate class-based operations as the preferred pattern  
**Demonstrates**:
- Class-based operations implementing `IWorkflowOperation`
- Output chaining between operations
- Property-based state management
- Production-ready operation structure

**Key Code Pattern**:
```csharp
public sealed class ValidateOrderOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "ValidateOrder";
    public bool SupportsRestore => false;

    public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken ct)
    {
        var userName = foundry.Properties["user_name"]?.ToString();
        var orderTotal = (decimal)foundry.Properties["order_total"]!;
        
        foundry.SetProperty("order_valid", orderTotal > 0 && !string.IsNullOrEmpty(userName));
        return Task.FromResult(inputData);
    }

    public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken ct)
        => Task.CompletedTask;
    
    public void Dispose() { }
}

// Usage
foundry
    .WithOperation(new ValidateOrderOperation())
    .WithOperation(new CalculateShippingOperation())
    .WithOperation(new ProcessPaymentOperation());
```

**Data Flow**: Dictionary-based via `foundry.Properties`  
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

## Category 4: Extensions (Samples 13-18, 21-25)

### Sample 13: SerilogIntegrationSample.cs
**Purpose**: Structured logging with Serilog  
**Demonstrates**:
- Serilog extension usage
- Context enrichment
- Structured log output
- Multiple sinks (Console, File)

**Key Code Pattern**:
```csharp
using WorkflowForge.Extensions.Logging.Serilog;

var logger = SerilogLoggerFactory.CreateLogger(new SerilogLoggerOptions
{
    MinimumLevel = "Information",
    EnableConsoleSink = true
});
var smith = WorkflowForge.CreateSmith(logger);
```

**Data Flow**: Dictionary-based  
**Complexity**: Intermediate  
**Extension**: WorkflowForge.Extensions.Logging.Serilog  
**Dependency Isolation**: ILRepack internalized (Serilog only)

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
**Dependency Isolation**: ILRepack internalized (Polly only)

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
**Dependency Isolation**: ILRepack internalized (OpenTelemetry only)

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
**Dependency Isolation**: External dependencies (Microsoft/System runtime unification)

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
**Purpose**: DataAnnotations validation  
**Demonstrates**:
- Validation extension usage
- DataAnnotations validation
- Middleware-based validation
- Manual validation
- Custom validators

**Key Code Pattern**:
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

**Data Flow**: Dictionary-based  
**Complexity**: Intermediate  
**Extension**: WorkflowForge.Extensions.Validation  
**Dependency Isolation**: No third-party dependencies

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

### Sample 25: ConfigurationSample.cs
**Purpose**: Configuration-driven workflow setup using appsettings.json  
**Demonstrates**:
- Loading configuration from appsettings.json
- Enabling/disabling extensions via configuration
- Using IOptions pattern for type-safe configuration
- Configuration validation on startup

**Key Code Pattern**:
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
    foundry.EnableAudit(auditProvider);
}
```

**Data Flow**: Dictionary-based  
**Complexity**: Advanced  
**Extensions**: All (configuration-driven)  
**Pattern**: Options pattern + feature toggles

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

## Category 6: Onboarding & Best Practices (Samples 26-33)

### Sample 26: DependencyInjectionSample.cs
**Purpose**: Configure WorkflowForge with dependency injection  
**Demonstrates**:
- Configuring WorkflowForge via DI container
- Registering `IWorkflowSmith` in services
- Resolving workflows from DI
- Using `AddWorkflowForge()` and `AddWorkflowSmith()`

**Key Code Pattern**:
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

**Data Flow**: Dictionary-based  
**Complexity**: Intermediate  
**Extension**: WorkflowForge.Extensions.DependencyInjection  
**Pattern**: Dependency Injection + Options pattern

---

### Sample 27: WorkflowMiddlewareSample.cs
**Purpose**: Workflow-level middleware vs operation middleware  
**Demonstrates**:
- `IWorkflowMiddleware` interface
- Adding workflow middleware to smith
- Timing and audit at workflow level
- Difference between workflow and operation middleware

**Key Code Pattern**:
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
        foundry.Logger.LogInformation($"[WorkflowTiming] Starting {workflow.Name}");
        
        await next().ConfigureAwait(false);
        
        var duration = DateTimeOffset.UtcNow - start;
        foundry.Logger.LogInformation($"[WorkflowTiming] Completed in {duration.TotalMilliseconds:F0}ms");
    }
}

var smith = WorkflowForge.CreateSmith(logger);
smith.AddWorkflowMiddleware(new WorkflowTimingMiddleware());
smith.AddWorkflowMiddleware(new WorkflowAuditMiddleware());

await smith.ForgeAsync(workflow);
```

**Data Flow**: Dictionary-based  
**Complexity**: Intermediate  
**Pattern**: Workflow-level middleware pipeline

---

### Sample 28: CancellationAndTimeoutSample.cs
**Purpose**: Cancellation tokens and operation timeouts  
**Demonstrates**:
- Using `CancellationToken` in operations
- `OperationTimeoutMiddleware` usage
- Handling `TimeoutException` and `OperationCanceledException`
- Graceful shutdown patterns

**Key Code Pattern**:
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

**Data Flow**: Dictionary-based  
**Complexity**: Intermediate  
**Pattern**: Timeout + cancellation handling

---

### Sample 29: ContinueOnErrorSample.cs
**Purpose**: ContinueOnError behavior and aggregate exception handling  
**Demonstrates**:
- `WorkflowForgeOptions.ContinueOnError = true`
- Workflow continues after operation failure
- `AggregateException` at end of workflow
- Use case for batch processing

**Key Code Pattern**:
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
Console.WriteLine($"Final operation ran: {foundry.GetPropertyOrDefault("final.ran", false)}");
```

**Data Flow**: Dictionary-based  
**Complexity**: Intermediate  
**Pattern**: Error aggregation

---

### Sample 30: CompensationBehaviorSample.cs
**Purpose**: Compensation behaviors and error handling strategies  
**Demonstrates**:
- `FailFastCompensation` option
- `ThrowOnCompensationError` option
- Compensation success vs failure scenarios
- `RestoreAsync()` implementation

**Key Code Pattern**:
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

**Data Flow**: Dictionary-based  
**Complexity**: Intermediate  
**Pattern**: Saga compensation

---

### Sample 31: FoundryReuseSample.cs
**Purpose**: Reusing a foundry across multiple workflows  
**Demonstrates**:
- Creating foundry via `smith.CreateFoundry()`
- Running multiple workflows with same foundry
- Property persistence across executions
- Foundry lifecycle management

**Key Code Pattern**:
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

**Data Flow**: Dictionary-based  
**Complexity**: Intermediate  
**Pattern**: Foundry reuse

---

### Sample 32: OutputChainingSample.cs
**Purpose**: Operation output becoming the next operation's input  
**Demonstrates**:
- Return values from `ForgeAsync()` flow to next operation
- `EnableOutputChaining` option behavior
- Type-safe data flow between operations
- Chained transformations

**Key Code Pattern**:
```csharp
using var foundry = WorkflowForge.CreateFoundry("OutputChainingDemo");

foundry
    .WithOperation(new SeedNumberOperation())      // Returns: 7
    .WithOperation(new MultiplyOperation(3))       // Receives: 7, Returns: 21
    .WithOperation(new FormatResultOperation());   // Receives: 21, Returns: "Result: 21"

await foundry.ForgeAsync();

// Each operation receives the previous operation's output as inputData
public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken ct)
{
    var value = inputData is int number ? number : 0;
    var result = value * _multiplier;
    Console.WriteLine($"Multiplied {value} by {_multiplier} = {result}");
    return Task.FromResult<object?>(result);
}
```

**Data Flow**: Output chaining + Dictionary-based  
**Complexity**: Intermediate  
**Pattern**: Pipeline transformation

---

### Sample 33: ServiceProviderResolutionSample.cs
**Purpose**: Resolving services inside operations via foundry.ServiceProvider  
**Demonstrates**:
- Accessing `foundry.ServiceProvider` in operations
- Resolving registered services at runtime
- DI-aware operation design
- Service abstraction patterns

**Key Code Pattern**:
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

// Inside the operation
public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken ct)
{
    var calculator = foundry.ServiceProvider?.GetRequiredService<IPriceCalculator>()
        ?? throw new InvalidOperationException("IPriceCalculator not registered.");
    
    var total = calculator.CalculateTotal(subtotal: 120m, taxRate: 0.08m);
    return Task.FromResult<object?>(total);
}
```

**Data Flow**: Dictionary-based  
**Complexity**: Intermediate  
**Pattern**: Service Locator via DI container

---

## Key Patterns Across All Samples

1. **Data Flow**: Dictionary-based via `foundry.Properties` (33/33 samples)
2. **Type Safety**: Generic `IWorkflowOperation<TInput, TOutput>` mentioned but rarely used
3. **Production Focus**: Class-based operations recommended over inline
4. **Extension Usage**: 13/33 samples demonstrate extensions
5. **Error Handling**: Explicit compensation in 2 samples, implicit in all via framework
6. **Configuration**: 3 samples dedicated to configuration patterns
7. **Events**: 1 sample dedicated to event system, events used in others
8. **Middleware**: 1 sample dedicated, middleware used in many extensions

---

## Sample Execution Order (Recommended)

**For New Users** (Learning Path):
1. HelloWorldSample (1)
2. DataPassingSample (2)
3. ClassBasedOperationsSample (4)
4. MultipleOutcomesSample (3)
5. ConditionalWorkflowSample (5)
6. ForEachLoopSample (6)
7. BuiltInOperationsSample (8)
8. ErrorHandlingSample (7)
9. OptionsPatternSample (9)
10. ConfigurationProfilesSample (10)
11. WorkflowEventsSample (11)
12. MiddlewareSample (12)
13. DependencyInjectionSample (26)
14. OutputChainingSample (32)
15. ServiceProviderResolutionSample (33)
16. SerilogIntegrationSample (13)
17. PerformanceMonitoringSample (17)
18. ValidationSample (23)
19. AuditSample (24)
20. ConfigurationSample (25)
21. PollyResilienceSample (14)
22. CancellationAndTimeoutSample (28)
23. ContinueOnErrorSample (29)
24. CompensationBehaviorSample (30)
25. FoundryReuseSample (31)
26. WorkflowMiddlewareSample (27)
27. PersistenceSample (18)
28. RecoveryOnlySample (21)
29. ResilienceRecoverySample (22)
30. HealthChecksSample (16)
31. OpenTelemetryObservabilitySample (15)
32. OperationCreationPatternsSample (20)
33. ComprehensiveIntegrationSample (19)

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
| Comprehensive integration | 19 |

---

## Related Documentation

- **[Getting Started](getting-started.md)** - Learn the basics before exploring samples
- **[Operations Guide](../core/operations.md)** - Detailed operation documentation
- **[Extensions](../extensions/index.md)** - Extension usage in samples
- **[Configuration](../core/configuration.md)** - Configuration patterns in samples
- **[Events System](../core/events.md)** - Event handling in samples

**← Back to [Documentation Home](../index.md)**
