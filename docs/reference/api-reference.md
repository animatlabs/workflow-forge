# WorkflowForge API Reference

<p align="center">
  <img src="../../icon.png" alt="WorkflowForge" width="120" height="120">
</p>

Complete API reference for WorkflowForge core types and abstractions.

---

## Table of Contents

- [Core Factory](#core-factory)
- [Workflow Interfaces](#workflow-interfaces)
- [Operation Interfaces](#operation-interfaces)
- [Foundry Interfaces](#foundry-interfaces)
- [Smith Interfaces](#smith-interfaces)
- [Event Interfaces](#event-interfaces)
- [Extension Interfaces](#extension-interfaces)

---

## Core Factory

### WorkflowForge

The main entry point for creating workflows and components.

```csharp
public static class WorkflowForge
```

#### Methods

**CreateWorkflow**
```csharp
public static IWorkflowBuilder CreateWorkflow(string name = "Workflow")
```
Creates a new workflow builder for fluent workflow construction.

**Parameters**:
- `name`: Optional workflow name (default: "Workflow")

**Returns**: `IWorkflowBuilder` for fluent configuration

**Example**:
```csharp
var workflow = WorkflowForge.CreateWorkflow("OrderProcessing")
    .AddOperation(new ValidateOperation())
    .Build();
```

---

**CreateFoundry**
```csharp
public static IWorkflowFoundry CreateFoundry(
    string name,
    IServiceProvider? serviceProvider = null,
    WorkflowForgeOptions? options = null)
```
Creates a new foundry (execution context) for workflow operations.

**Parameters**:
- `name`: Foundry name for identification
- `serviceProvider`: Optional DI container for operation dependencies
- `options`: Optional execution options for the foundry

**Returns**: `IWorkflowFoundry` execution context

**Example**:
```csharp
using var foundry = WorkflowForge.CreateFoundry("MyFoundry", serviceProvider, options);
```

---

**CreateSmith**
```csharp
public static IWorkflowSmith CreateSmith()
```
Creates a new smith (workflow executor).

**Returns**: `IWorkflowSmith` workflow executor

**Example**:
```csharp
using var smith = WorkflowForge.CreateSmith();
await smith.ForgeAsync(workflow, foundry);
```

---

## Workflow Interfaces

### IWorkflow

Represents an immutable workflow definition.

```csharp
public interface IWorkflow
{
    Guid Id { get; }
    string Name { get; }
    IReadOnlyList<IWorkflowOperation> Operations { get; }
}
```

**Properties**:
- `Id`: Unique identifier for the workflow
- `Name`: Human-readable workflow name
- `Operations`: Ordered collection of operations to execute

---

### IWorkflowBuilder

Fluent builder for constructing workflows.

```csharp
public interface IWorkflowBuilder
{
    IWorkflowBuilder AddOperation(IWorkflowOperation operation);
    IWorkflowBuilder AddOperation(string name, Func<object?, IWorkflowFoundry, CancellationToken, Task<object?>> operation);
    IWorkflow Build();
}
```

**Methods**:

**AddOperation (class-based)**
```csharp
IWorkflowBuilder AddOperation(IWorkflowOperation operation)
```
Adds a class-based operation to the workflow.

**Parameters**:
- `operation`: Operation instance implementing `IWorkflowOperation`

**Returns**: Builder for chaining

---

**AddOperation (inline)**
```csharp
IWorkflowBuilder AddOperation(
    string name,
    Func<object?, IWorkflowFoundry, CancellationToken, Task<object?>> operation)
```
Adds an inline operation to the workflow.

**Parameters**:
- `name`: Operation name
- `operation`: Async delegate for operation logic

**Returns**: Builder for chaining

---

**Build**
```csharp
IWorkflow Build()
```
Constructs the immutable workflow from builder configuration.

**Returns**: Immutable `IWorkflow` instance

---

## Operation Interfaces

### IWorkflowOperation

Base interface for all workflow operations.

```csharp
public interface IWorkflowOperation : IDisposable
{
    Guid Id { get; }
    string Name { get; }
    bool SupportsRestore { get; }
    
    Task<object?> ForgeAsync(
        object? inputData,
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken = default);
    
    Task RestoreAsync(
        object? outputData,
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken = default);
}
```

**Properties**:
- `Id`: Unique identifier for the operation instance
- `Name`: Human-readable operation name
- `SupportsRestore`: Whether operation implements compensation logic

**Methods**:

**ForgeAsync**
```csharp
Task<object?> ForgeAsync(
    object? inputData,
    IWorkflowFoundry foundry,
    CancellationToken cancellationToken = default)
```
Executes the operation's main logic.

**Parameters**:
- `inputData`: Optional input data (prefer foundry properties)
- `foundry`: Execution context providing services, logging, and properties
- `cancellationToken`: Cancellation token

**Returns**: Operation result (optional)

**Best Practice**: Read from and write to `foundry.Properties` using `SetProperty`/`GetPropertyOrDefault` helpers instead of relying on `inputData`/return values.

---

**RestoreAsync**
```csharp
Task RestoreAsync(
    object? outputData,
    IWorkflowFoundry foundry,
    CancellationToken cancellationToken = default)
```
Executes compensation logic to undo operation effects.

**Parameters**:
- `outputData`: Output from original `ForgeAsync` call
- `foundry`: Execution context
- `cancellationToken`: Cancellation token

**Usage**: Called automatically by WorkflowSmith if workflow fails and `SupportsRestore` is true.

---

### WorkflowOperationBase

Abstract base class implementing common operation patterns.

```csharp
public abstract class WorkflowOperationBase : IWorkflowOperation
{
    public Guid Id { get; }
    public abstract string Name { get; }
    public virtual bool SupportsRestore => false;
    
    public abstract Task<object?> ForgeAsync(
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken = default);
    
    public virtual Task RestoreAsync(
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
    
    public virtual void Dispose() { }
}
```

**Key Differences from IWorkflowOperation**:
- `ForgeAsync` doesn't have `inputData` parameter (encourages foundry properties)
- `RestoreAsync` doesn't have `outputData` parameter (encourages foundry properties)
- Base class handles IWorkflowOperation bridge

**Recommended Base Class**: Use `WorkflowOperationBase` for most operations.

---

### IWorkflowOperation<TInput, TOutput>

Generic interface for type-safe operations (advanced use case).

```csharp
public interface IWorkflowOperation<TInput, TOutput> : IWorkflowOperation
{
    Task<TOutput> ForgeAsync(
        TInput input,
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken = default);
}
```

**Use Case**: When you need strongly-typed input/output contracts.

**Note**: Most operations should use `WorkflowOperationBase` with foundry properties instead.

---

## Foundry Interfaces

### IWorkflowFoundry

Execution context providing services, state, and logging to operations.

```csharp
public interface IWorkflowFoundry : IOperationLifecycleEvents, IDisposable
{
    Guid ExecutionId { get; }
    string Name { get; }
    IServiceProvider ServiceProvider { get; }
    IWorkflowForgeLogger Logger { get; }
    ConcurrentDictionary<string, object?> Properties { get; }
    
    void SetProperty(string key, object? value);
    T? GetPropertyOrDefault<T>(string key, T? defaultValue = default);
    bool TryGetProperty<T>(string key, out T? value);
    
    void AddMiddleware(IWorkflowOperationMiddleware middleware);
}
```

**Properties**:
- `ExecutionId`: Unique identifier for this foundry execution
- `Name`: Foundry name
- `ServiceProvider`: DI container for resolving operation dependencies
- `Logger`: Logging abstraction
- `Properties`: Thread-safe dictionary for workflow data

**Methods**:

**SetProperty**
```csharp
void SetProperty(string key, object? value)
```
Stores a value in foundry properties (thread-safe).

**Parameters**:
- `key`: Property key
- `value`: Property value

**Example**:
```csharp
foundry.SetProperty("OrderId", 12345);
foundry.SetProperty("Customer", new Customer { /*...*/ });
```

---

**GetPropertyOrDefault**
```csharp
T? GetPropertyOrDefault<T>(string key, T? defaultValue = default)
```
Retrieves a property value with type safety and default fallback.

**Parameters**:
- `key`: Property key
- `defaultValue`: Value to return if key not found

**Returns**: Property value or default

**Example**:
```csharp
var orderId = foundry.GetPropertyOrDefault<int>("OrderId");
var total = foundry.GetPropertyOrDefault<decimal>("Total", 0m);
```

---

**TryGetProperty**
```csharp
bool TryGetProperty<T>(string key, out T? value)
```
Attempts to retrieve a property value.

**Parameters**:
- `key`: Property key
- `value`: Out parameter for the retrieved value

**Returns**: True if property exists and is of type T

**Example**:
```csharp
if (foundry.TryGetProperty<string>("TransactionId", out var txnId))
{
    // Use txnId
}
```

---

**AddMiddleware**
```csharp
void AddMiddleware(IWorkflowOperationMiddleware middleware)
```
Adds middleware to the operation execution pipeline.

**Parameters**:
- `middleware`: Middleware implementation

**Example**:
```csharp
foundry.AddMiddleware(new TimingMiddleware(logger));
foundry.AddMiddleware(new ValidationMiddleware<Order>(validator, ...));
```

---

## Smith Interfaces

### IWorkflowSmith

Workflow executor responsible for orchestrating operation execution.

```csharp
public interface IWorkflowSmith : 
    IWorkflowLifecycleEvents, 
    ICompensationLifecycleEvents, 
    IDisposable
{
    Task ForgeAsync(
        IWorkflow workflow,
        IWorkflowFoundry? foundry = null,
        CancellationToken cancellationToken = default);
}
```

**Methods**:

**ForgeAsync**
```csharp
Task ForgeAsync(
    IWorkflow workflow,
    IWorkflowFoundry? foundry = null,
    CancellationToken cancellationToken = default)
```
Executes a workflow with the provided foundry.

**Parameters**:
- `workflow`: Workflow to execute
- `foundry`: Optional execution context (created if null)
- `cancellationToken`: Cancellation token

**Behavior**:
1. Fires `WorkflowStarted` event
2. Executes operations in sequence
3. Fires `WorkflowCompleted` on success
4. Fires `WorkflowFailed` and triggers compensation on failure
5. If `ContinueOnError` is enabled, throws `AggregateException` after execution

**Example**:
```csharp
using var smith = WorkflowForge.CreateSmith();
using var foundry = WorkflowForge.CreateFoundry("MyWorkflow");

await smith.ForgeAsync(workflow, foundry);
```

---

## Event Interfaces

### IWorkflowLifecycleEvents

Workflow-level lifecycle events.

```csharp
public interface IWorkflowLifecycleEvents
{
    event EventHandler<WorkflowStartedEventArgs>? WorkflowStarted;
    event EventHandler<WorkflowCompletedEventArgs>? WorkflowCompleted;
    event EventHandler<WorkflowFailedEventArgs>? WorkflowFailed;
}
```

**Implemented By**: `IWorkflowSmith`

**Events**:
- `WorkflowStarted`: Fired before first operation executes
- `WorkflowCompleted`: Fired after all operations complete successfully
- `WorkflowFailed`: Fired when workflow execution fails

**Example**:
```csharp
smith.WorkflowStarted += (s, e) => 
    Console.WriteLine($"Started: {e.WorkflowName}");
smith.WorkflowCompleted += (s, e) => 
    Console.WriteLine($"Completed in {e.Duration.TotalMilliseconds}ms");
```

---

### IOperationLifecycleEvents

Operation-level lifecycle events.

```csharp
public interface IOperationLifecycleEvents
{
    event EventHandler<OperationStartedEventArgs>? OperationStarted;
    event EventHandler<OperationCompletedEventArgs>? OperationCompleted;
    event EventHandler<OperationFailedEventArgs>? OperationFailed;
}
```

**Implemented By**: `IWorkflowFoundry`

**Events**:
- `OperationStarted`: Fired before each operation executes
- `OperationCompleted`: Fired after operation completes successfully
- `OperationFailed`: Fired when operation execution fails

**Example**:
```csharp
foundry.OperationStarted += (s, e) => 
    Console.WriteLine($"Operation {e.OperationName} started");
foundry.OperationCompleted += (s, e) => 
    Console.WriteLine($"Operation {e.OperationName} completed");
```

---

### ICompensationLifecycleEvents

Compensation (saga pattern) lifecycle events.

```csharp
public interface ICompensationLifecycleEvents
{
    event EventHandler<CompensationTriggeredEventArgs>? CompensationTriggered;
    event EventHandler<CompensationCompletedEventArgs>? CompensationCompleted;
    event EventHandler<OperationRestoreStartedEventArgs>? OperationRestoreStarted;
    event EventHandler<OperationRestoreCompletedEventArgs>? OperationRestoreCompleted;
    event EventHandler<OperationRestoreFailedEventArgs>? OperationRestoreFailed;
}
```

**Implemented By**: `IWorkflowSmith`

**Events**:
- `CompensationTriggered`: Fired when workflow failure triggers compensation
- `CompensationCompleted`: Fired after all compensations complete
- `OperationRestoreStarted`: Fired before each operation's `RestoreAsync`
- `OperationRestoreCompleted`: Fired after successful restoration
- `OperationRestoreFailed`: Fired when restoration fails

**Example**:
```csharp
smith.CompensationTriggered += (s, e) => 
    Console.WriteLine($"Compensating {e.OperationsToCompensate} operations");
smith.OperationRestoreStarted += (s, e) => 
    Console.WriteLine($"Rolling back {e.OperationName}");
```

---

## Core Options

```csharp
public sealed class WorkflowForgeOptions
{
    public int MaxConcurrentWorkflows { get; set; } = 0;
    public bool ContinueOnError { get; set; } = false;
    public bool FailFastCompensation { get; set; } = false;
    public bool ThrowOnCompensationError { get; set; } = false;
}
```

---

## Extension Interfaces

### IWorkflowOperationMiddleware

Middleware for operation execution pipeline.

```csharp
public interface IWorkflowOperationMiddleware
{
    Task<object?> ExecuteAsync(
        IWorkflowOperation operation,
        IWorkflowFoundry foundry,
        object? inputData,
        Func<CancellationToken, Task<object?>> next,
        CancellationToken cancellationToken);
}
```

**Methods**:

**ExecuteAsync**
```csharp
Task<object?> ExecuteAsync(
    IWorkflowOperation operation,
    IWorkflowFoundry foundry,
    object? inputData,
    Func<CancellationToken, Task<object?>> next,
    CancellationToken cancellationToken)
```
Executes middleware logic in the operation pipeline.

**Parameters**:
- `operation`: Operation being executed
- `foundry`: Execution context
- `next`: Delegate to call next middleware/operation
- `cancellationToken`: Cancellation token

**Returns**: Operation result

**Example**:
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
        try
        {
            return await next(cancellationToken);
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation("Operation {Op} took {Ms}ms", 
                operation.Name, sw.ElapsedMilliseconds);
        }
    }
}
```

---

### IWorkflowForgeLogger

Logging abstraction for framework and operations.

```csharp
public interface IWorkflowForgeLogger
{
    void LogTrace(string message, params object[] args);
    void LogDebug(string message, params object[] args);
    void LogInformation(string message, params object[] args);
    void LogWarning(string message, params object[] args);
    void LogError(Exception? exception, string message, params object[] args);
    void LogCritical(Exception? exception, string message, params object[] args);
}
```

**Methods**: Standard logging levels (Trace, Debug, Information, Warning, Error, Critical)

**Implementations**:
- `ConsoleLogger`: Default implementation (console output)
- `NullLogger`: No-op logger
- `SerilogAdapter`: Bridges to Serilog (Extension)

---

### ISystemTimeProvider

Time abstraction for testability.

```csharp
public interface ISystemTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
```

**Purpose**: Enables time mocking in tests

**Implementations**:
- `SystemTimeProvider`: Returns `DateTimeOffset.UtcNow`
- Mock implementations for testing

**Example**:
```csharp
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
        var now = _timeProvider.UtcNow;
        // Time-dependent logic
    }
}
```

---

## Built-In Operations

### DelayOperation

Introduces a delay in workflow execution.

```csharp
public class DelayOperation : WorkflowOperationBase
{
    public DelayOperation(TimeSpan delay);
}
```

**Example**:
```csharp
var workflow = WorkflowForge.CreateWorkflow("DelayedWorkflow")
    .AddOperation(new DelayOperation(TimeSpan.FromSeconds(2)))
    .Build();
```

---

### LoggingOperation

Logs a message during workflow execution.

```csharp
public class LoggingOperation : WorkflowOperationBase
{
    public LoggingOperation(IWorkflowForgeLogger logger, string message);
}
```

**Example**:
```csharp
var workflow = WorkflowForge.CreateWorkflow("LoggingWorkflow")
    .AddOperation(new LoggingOperation(logger, "Starting workflow"))
    .Build();
```

---

### ConditionalWorkflowOperation

Executes different operations based on a predicate.

```csharp
public class ConditionalWorkflowOperation : WorkflowOperationBase
{
    public ConditionalWorkflowOperation(
        Func<IWorkflowFoundry, bool> predicate,
        IWorkflowOperation trueOperation,
        IWorkflowOperation falseOperation);
}
```

**Example**:
```csharp
var conditionalOp = new ConditionalWorkflowOperation(
    predicate: f => f.GetPropertyOrDefault<decimal>("Amount") > 100,
    trueOperation: new HighValueOperation(),
    falseOperation: new StandardOperation());
```

---

### ForEachWorkflowOperation

Processes collections in parallel or sequential mode.

```csharp
public static class ForEachWorkflowOperation
{
    public static IWorkflowOperation Create(
        IWorkflowOperation operation,
        bool parallel = true);
}
```

**Example**:
```csharp
foundry.SetProperty("items", new[] { "A", "B", "C" });

var forEachOp = ForEachWorkflowOperation.Create(
    new ProcessItemOperation(),
    parallel: true);
```

---

## Next Steps

- **[Architecture](../architecture/overview.md)** - Understanding the design
- **[Operations](../core/operations.md)** - Creating operations
- **[Events](../core/events.md)** - Event system details
- **[Configuration](../core/configuration.md)** - Configuring workflows
- **[Extensions](../extensions/index.md)** - Extension ecosystem

---

**← Back to [Documentation Home](../index.md)**
