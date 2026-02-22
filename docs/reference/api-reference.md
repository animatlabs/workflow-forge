---
title: API Reference
description: Complete API reference for WorkflowForge core types, interfaces, builders, and extension points.
---

# WorkflowForge API Reference

Complete API reference for WorkflowForge core types and abstractions.

---

## Table of Contents

- [Core Factory](#core-factory)
- [Workflow Interfaces](#workflow-interfaces)
- [Operation Interfaces](#operation-interfaces)
- [Foundry Interfaces](#foundry-interfaces)
- [Smith Interfaces](#smith-interfaces)
- [Event Interfaces](#event-interfaces)
- [Testing Support](#testing-support)
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
public static WorkflowBuilder CreateWorkflow(string? workflowName = null, IServiceProvider? serviceProvider = null)
```
Creates a new workflow builder for fluent workflow construction.

**Parameters**:
- `workflowName`: Optional workflow name (null lets you call `WithName` later)
- `serviceProvider`: Optional DI container for operation resolution

**Returns**: `WorkflowBuilder` for fluent configuration

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
    string workflowName,
    IWorkflowForgeLogger? logger = null,
    IDictionary<string, object?>? initialProperties = null,
    WorkflowForgeOptions? options = null)
```
Creates a new foundry (execution context) for workflow operations.

**Parameters**:
- `workflowName`: Foundry name for identification
- `logger`: Optional logger for the foundry
- `initialProperties`: Optional initial properties for the foundry
- `options`: Optional execution options for the foundry

**Returns**: `IWorkflowFoundry` execution context

**Example**:
```csharp
using var foundry = WorkflowForge.CreateFoundry("MyFoundry", logger, initialProperties, options);
```

---

**CreateSmith**
```csharp
public static IWorkflowSmith CreateSmith(
    IWorkflowForgeLogger? logger = null,
    IServiceProvider? serviceProvider = null,
    WorkflowForgeOptions? options = null)
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
    string? Description { get; }
    string Version { get; }
    IReadOnlyList<IWorkflowOperation> Operations { get; }
}
```

**Properties**:
- `Id`: Unique identifier for the workflow
- `Name`: Human-readable workflow name
- `Description`: Optional description for documentation
- `Version`: Workflow version string
- `Operations`: Ordered collection of operations to execute

---

### WorkflowBuilder

Fluent builder for constructing workflows.

```csharp
public sealed class WorkflowBuilder
{
    WorkflowBuilder WithName(string name);
    WorkflowBuilder WithDescription(string? description);
    WorkflowBuilder WithVersion(string version);
    WorkflowBuilder AddOperation(IWorkflowOperation operation);
    WorkflowBuilder AddOperation<T>() where T : class, IWorkflowOperation;
    WorkflowBuilder AddOperation(string name, Func<IWorkflowFoundry, CancellationToken, Task> action, Func<IWorkflowFoundry, CancellationToken, Task>? restoreAction = null);
    WorkflowBuilder AddOperation(string name, Action<IWorkflowFoundry> action, Action<IWorkflowFoundry>? restoreAction = null);
    WorkflowBuilder AddOperations(params IWorkflowOperation[] operations);
    WorkflowBuilder AddOperations(IEnumerable<IWorkflowOperation> operations);
    WorkflowBuilder AddParallelOperations(params IWorkflowOperation[] operations);
    WorkflowBuilder AddParallelOperations(IEnumerable<IWorkflowOperation> operations, int? maxConcurrency = null, TimeSpan? timeout = null, string? name = null);
    IWorkflow Build();
}
```

**Notes**:
- `AddOperation<T>()` resolves the operation from the provided `IServiceProvider`.
- Inline operations are wrapped into `ActionWorkflowOperation` instances.
- `AddOperations()` adds multiple operations to the sequential execution chain.
- `AddParallelOperations()` wraps operations in a `ForEachWorkflowOperation` for parallel execution with shared input.

---

## Operation Interfaces

### IWorkflowOperation

Base interface for all workflow operations.

> **Recommended**: Use `WorkflowOperationBase` as the base class for custom operations instead of implementing this interface directly. The base class provides automatic ID generation, default RestoreAsync/Dispose implementations, lifecycle hooks, and the ForgeAsyncCore pattern.

```csharp
public interface IWorkflowOperation : IDisposable
{
    Guid Id { get; }
    string Name { get; }
    
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

**Usage**: Called automatically by WorkflowSmith if workflow fails. Operations that override `RestoreAsync` run their compensation logic; operations that use the base class default (no-op) are safely skipped.

---

### WorkflowOperationBase

Abstract base class implementing common operation patterns with lifecycle hooks.

```csharp
public abstract class WorkflowOperationBase : IWorkflowOperation
{
    public virtual Guid Id { get; } = Guid.NewGuid();
    public abstract string Name { get; }
    
    // Lifecycle hooks (virtual, override to customize)
    protected virtual Task OnBeforeExecuteAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken ct)
        => Task.CompletedTask;
    
    protected virtual Task OnAfterExecuteAsync(object? inputData, object? outputData, IWorkflowFoundry foundry, CancellationToken ct)
        => Task.CompletedTask;
    
    // Core logic (abstract, implement in derived classes)
    protected abstract Task<object?> ForgeAsyncCore(
        object? inputData,
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken);
    
    // Public entry point (orchestrates hooks + core logic)
    public Task<object?> ForgeAsync(
        object? inputData,
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken = default);
    
    public virtual Task RestoreAsync(
        object? outputData,
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken = default);
    
    public virtual void Dispose() { }
}
```

**Features**:
- Auto-generates unique `Id` for each operation instance
- `RestoreAsync` provides a no-op default — override it to implement compensation. Operations that don't override are safely skipped during compensation.
- Override `Dispose()` if your operation holds unmanaged resources

**Lifecycle Hooks** (new in v2.0):
- `OnBeforeExecuteAsync`: Called before the operation executes (setup, validation, logging)
- `OnAfterExecuteAsync`: Called after the operation completes successfully (cleanup, metrics)
- Implement `ForgeAsyncCore` instead of `ForgeAsync` for your operation logic

**Example with Hooks**:
```csharp
public class AuditedOperation : WorkflowOperationBase
{
    public override string Name => "AuditedOperation";
    
    protected override Task OnBeforeExecuteAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken ct)
    {
        foundry.Logger.LogInformation("Starting operation with input: {Input}", inputData);
        return Task.CompletedTask;
    }
    
    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken ct)
    {
        // Your operation logic here
        return await ProcessAsync(inputData, ct);
    }
    
    protected override Task OnAfterExecuteAsync(object? inputData, object? outputData, IWorkflowFoundry foundry, CancellationToken ct)
    {
        foundry.Logger.LogInformation("Completed operation with result: {Output}", outputData);
        return Task.CompletedTask;
    }
}
```

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

    Task RestoreAsync(
        TOutput output,
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
public interface IWorkflowFoundry :
    IWorkflowExecutionContext,
    IWorkflowMiddlewarePipeline,
    IOperationLifecycleEvents,
    IDisposable
{
    Task ForgeAsync(CancellationToken cancellationToken = default);
    void ReplaceOperations(IEnumerable<IWorkflowOperation> operations);
    bool IsFrozen { get; }
}
```

**Properties**:
- `ExecutionId`: Unique identifier for this foundry execution
- `CurrentWorkflow`: Workflow being executed (or null)
- `ServiceProvider`: DI container for resolving operation dependencies
- `Logger`: Logging abstraction
- `Options`: Execution options for the foundry
- `Properties`: Thread-safe dictionary for workflow data
- `IsFrozen`: True while `ForgeAsync` is executing (pipeline is immutable)

**Notes**:
- Property helpers like `SetProperty`, `GetPropertyOrDefault`, and `TryGetProperty` are extension methods.
- Operations and middleware cannot be added/removed while `ForgeAsync` is running.

**Foundry WithOperation Extensions** (from `FoundryPropertyExtensions`):

```csharp
IWorkflowFoundry WithOperation(this IWorkflowFoundry foundry, string name, Func<IWorkflowFoundry, Task> action, Func<IWorkflowFoundry, Task>? restoreAction = null);
IWorkflowFoundry WithOperation(this IWorkflowFoundry foundry, string name, Action<IWorkflowFoundry> action, Action<IWorkflowFoundry>? restoreAction = null);
```

Adds inline operations to the foundry. The optional `restoreAction` provides compensation logic when the workflow fails.

---

### IWorkflowMiddlewarePipeline

Pipeline builder for operations and middleware.

```csharp
public interface IWorkflowMiddlewarePipeline
{
    void AddOperation(IWorkflowOperation operation);
    void AddMiddleware(IWorkflowOperationMiddleware middleware);
    void AddMiddlewares(IEnumerable<IWorkflowOperationMiddleware> middlewares);
}
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
    Task ForgeAsync(IWorkflow workflow, CancellationToken cancellationToken = default);
    Task ForgeAsync(IWorkflow workflow, ConcurrentDictionary<string, object?> data, CancellationToken cancellationToken = default);
    Task ForgeAsync(IWorkflow workflow, IWorkflowFoundry foundry, CancellationToken cancellationToken = default);
    IWorkflowFoundry CreateFoundry(IWorkflowForgeLogger? logger = null, IServiceProvider? serviceProvider = null);
    IWorkflowFoundry CreateFoundryFor(IWorkflow workflow, IWorkflowForgeLogger? logger = null, IServiceProvider? serviceProvider = null);
    IWorkflowFoundry CreateFoundryWithData(ConcurrentDictionary<string, object?> data, IWorkflowForgeLogger? logger = null, IServiceProvider? serviceProvider = null);
    void AddWorkflowMiddleware(IWorkflowMiddleware middleware);
}
```

**Methods**:
- `ForgeAsync(...)`: Executes a workflow using one of the three patterns (smith-managed, data dictionary, or reusable foundry).
- `CreateFoundry(...)`: Creates a reusable foundry with optional logger/service provider.
- `CreateFoundryFor(...)`: Creates a foundry pre-associated with a workflow.
- `CreateFoundryWithData(...)`: Creates a foundry with an initial data dictionary.
- `AddWorkflowMiddleware(...)`: Adds workflow-level middleware around the entire execution.

**Example**:
```csharp
using var smith = WorkflowForge.CreateSmith();
using var foundry = smith.CreateFoundryFor(workflow);

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

**Notes**:
- `WorkflowForgeOptions` inherits from `WorkflowForgeOptionsBase` for consistent `Enabled`, `SectionName`, `Validate()`, and `Clone()` behavior.

---

## Testing Support

### FakeWorkflowFoundry

A lightweight fake implementation of `IWorkflowFoundry` for unit testing operations.

**Package**: `WorkflowForge.Testing`

```csharp
public class FakeWorkflowFoundry : IWorkflowFoundry
{
    // Configurable properties
    public Guid ExecutionId { get; set; }
    public ConcurrentDictionary<string, object?> Properties { get; }
    public IWorkflowForgeLogger Logger { get; set; }
    public WorkflowForgeOptions Options { get; set; }
    public IServiceProvider? ServiceProvider { get; set; }
    
    // Test assertions
    public IReadOnlyList<IWorkflowOperation> Operations { get; }
    public IReadOnlyList<IWorkflowOperationMiddleware> Middlewares { get; }
    public IReadOnlyList<IWorkflowOperation> ExecutedOperations { get; }
    
    // Test helpers
    public void Reset();
    public void TrackExecution(IWorkflowOperation operation);
}
```

**Features**:
- Implements full `IWorkflowFoundry` interface
- Tracks executed operations for assertions
- Configurable logger, options, and service provider
- `Reset()` method for reusing between tests

**Example Usage**:
```csharp
[Fact]
public async Task MyOperation_Should_SetProperty()
{
    // Arrange
    var foundry = new FakeWorkflowFoundry();
    var operation = new MyCustomOperation();
    
    // Act
    await operation.ForgeAsync("input", foundry, CancellationToken.None);
    
    // Assert
    Assert.True(foundry.Properties.ContainsKey("myKey"));
    Assert.Equal("expectedValue", foundry.Properties["myKey"]);
}

[Fact]
public async Task Workflow_Should_ExecuteAllOperations()
{
    // Arrange
    var foundry = new FakeWorkflowFoundry();
    foundry.AddOperation(new StepOneOperation());
    foundry.AddOperation(new StepTwoOperation());
    
    // Act
    await foundry.ForgeAsync();
    
    // Assert
    Assert.Equal(2, foundry.ExecutedOperations.Count);
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
    
    protected override async Task<object?> ForgeAsyncCore(
        object? inputData,
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.UtcNow;
        // Time-dependent logic
        return inputData;
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
    public LoggingOperation(string message, WorkflowForgeLogLevel logLevel = WorkflowForgeLogLevel.Information, string? name = null);
}
```

**Example**:
```csharp
var workflow = WorkflowForge.CreateWorkflow("LoggingWorkflow")
    .AddOperation(new LoggingOperation("Starting workflow"))
    .Build();
```

---

### ConditionalWorkflowOperation

Executes different operations based on a condition.

```csharp
public class ConditionalWorkflowOperation : WorkflowOperationBase
{
    // Async condition with input data
    public ConditionalWorkflowOperation(
        Func<object?, IWorkflowFoundry, CancellationToken, Task<bool>> condition,
        IWorkflowOperation trueOperation,
        IWorkflowOperation? falseOperation = null,
        string? name = null,
        Guid? id = null);

    // Sync condition with input data
    public ConditionalWorkflowOperation(
        Func<object?, IWorkflowFoundry, bool> condition,
        IWorkflowOperation trueOperation,
        IWorkflowOperation? falseOperation = null,
        string? name = null,
        Guid? id = null);

    // Simple condition (foundry-only) via factory
    public static ConditionalWorkflowOperation Create(
        Func<IWorkflowFoundry, bool> condition,
        IWorkflowOperation trueOperation,
        IWorkflowOperation? falseOperation = null,
        string? name = null);
}
```

**Example**:
```csharp
var conditionalOp = new ConditionalWorkflowOperation(
    condition: (input, f) => f.GetPropertyOrDefault<decimal>("Amount") > 100,
    trueOperation: new HighValueOperation(),
    falseOperation: new StandardOperation());
```

---

### ForEachWorkflowOperation

Executes multiple operations concurrently with configurable data distribution.

```csharp
public sealed class ForEachWorkflowOperation : WorkflowOperationBase
{
    // Factory methods
    public static ForEachWorkflowOperation CreateSharedInput(
        IEnumerable<IWorkflowOperation> operations,
        TimeSpan? timeout = null,
        int? maxConcurrency = null,
        string? name = null);
    
    public static ForEachWorkflowOperation CreateSplitInput(
        IEnumerable<IWorkflowOperation> operations,
        TimeSpan? timeout = null,
        int? maxConcurrency = null,
        string? name = null);
    
    public static ForEachWorkflowOperation CreateNoInput(
        IEnumerable<IWorkflowOperation> operations,
        TimeSpan? timeout = null,
        int? maxConcurrency = null,
        string? name = null);
}
```

**Data Strategies**:
- `CreateSharedInput`: All operations receive the same input data
- `CreateSplitInput`: Input collection is split and distributed among operations
- `CreateNoInput`: Operations receive null input

**Example**:
```csharp
// Execute multiple operations in parallel with shared input
var forEachOp = ForEachWorkflowOperation.CreateSharedInput(
    new IWorkflowOperation[] { new ProcessA(), new ProcessB(), new ProcessC() },
    maxConcurrency: 4,
    name: "ProcessAll");

// Throttled execution with 2 concurrent operations max
var throttledOp = ForEachWorkflowOperation.CreateSplitInput(
    operations,
    maxConcurrency: 2);
```

---

## Related Documentation

- [Architecture](../architecture/overview.md) - Understanding the design
- [Operations](../core/operations.md) - Creating operations
- [Events](../core/events.md) - Event system details
- [Configuration](../core/configuration.md) - Configuring workflows
- [Extensions](../extensions/index.md) - Extension ecosystem
