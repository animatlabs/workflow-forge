---
title: Architecture Overview
description: Design principles, patterns, and implementation details behind WorkflowForge workflow orchestration.
---

# WorkflowForge Architecture

<a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=alert_status" alt="Quality Gate Status" /></a>
<a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=coverage" alt="Coverage" /></a>
<a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=reliability_rating" alt="Reliability Rating" /></a>
<a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=security_rating" alt="Security Rating" /></a>
<a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=sqale_rating" alt="Maintainability Rating" /></a>

WorkflowForge keeps a small core, fast paths, and names that stay memorable in large solutions.

---

## Table of Contents

- [Design Philosophy](#design-philosophy)
- [Core Metaphor](#core-metaphor)
- [Architectural Principles](#architectural-principles)
- [Component Architecture](#component-architecture)
- [Data Flow Patterns](#data-flow-patterns)
- [Event System Design](#event-system-design)
- [Middleware Pipeline](#middleware-pipeline)
- [Compensation Pattern (Saga)](#compensation-pattern-saga)
- [Performance Optimizations](#performance-optimizations)
- [Extension Architecture](#extension-architecture)

---

## Design Philosophy

Three ideas drive the core:

### 1. Zero Dependencies

- **No external packages** on the core assembly.
- Fewer version clashes.
- Small binary (~50KB).
- Runs anywhere .NET Standard 2.0 runs.
- No surprise transitive stacks.

### 2. Performance First

- Prefer low overhead in hot paths.
- Microsecond-scale op execution in benchmarks.
- Tight allocations.
- `ConcurrentDictionary` for shared bag state.
- Struct event args where it pays.
- Pooling for hot internal objects.

### 3. Developer Experience

- Forge / foundry / smith map cleanly to orchestration.
- Fluent builders.
- Optional typed operations.
- Dictionary bag when schemas move.
- Events for telemetry and tests.

---

## Core Metaphor

WorkflowForge uses an **industrial metalworking metaphor** so big graphs stay legible:

```
The Forge (Factory)
    ↓
Creates Workflows & Components
    ↓
Workflows execute in Foundries (Workshops)
    ↓
Managed by Smiths (Craftsmen)
    ↓
Using Operations (Tools)
    ↓
To transform Data (Raw Materials → Finished Products)
```

### Component Mapping

| Component | Metaphor | Role |
|-----------|----------|------|
| **WorkflowForge** | The Forge | Static factory for creating workflows and components |
| **IWorkflowFoundry** | Foundry/Workshop | Execution environment with context, logging, services |
| **IWorkflowSmith** | Smith/Craftsman | Orchestration engine managing workflow execution |
| **IWorkflowOperation** | Tools/Processes | Individual tasks that transform data |
| **IWorkflow** | Blueprint | Complete workflow definition with operations |
| **Properties** | Raw Materials | Data flowing through the workflow |

*A smith shapes stock in a workshop; WorkflowForge runs operations in a foundry to move data through a blueprint.*

---

## Architectural Principles

### Single Responsibility Principle (SRP)

Every component has one clear purpose:

**Before (Anti-pattern)**:
```csharp
// One interface doing everything - violates SRP
public interface IWorkflowEvents
{
    event WorkflowStarted;
    event OperationStarted;
    event CompensationStarted;
    // ... all events mixed together
}
```

**After (WorkflowForge Design)**:
```csharp
// Three focused interfaces
public interface IWorkflowLifecycleEvents { /* workflow events */ }
public interface IOperationLifecycleEvents { /* operation events */ }
public interface ICompensationLifecycleEvents { /* compensation events */ }
```

### Dependency Inversion Principle

Core depends on abstractions, not implementations:
```csharp
// Abstractions define contracts
public interface IWorkflowFoundry { }
public interface IWorkflowOperation { }

// Implementations fulfill contracts
internal sealed class WorkflowFoundry : IWorkflowFoundry { }
public sealed class DelegateWorkflowOperation : IWorkflowOperation { }
```

### Open/Closed Principle

- Add behavior without forking core types.
- Subclass `WorkflowOperationBase`.
- Register middleware.
- Add extension packages; the core assembly stays stable.

---

## Component Architecture

### The Forge (Static Factory)

`WorkflowForge` is the main entry point; it exposes factory methods for workflows, foundries, and smiths:

```csharp
public static class WorkflowForge
{
    // Workflow creation
    public static WorkflowBuilder CreateWorkflow(string? workflowName = null, IServiceProvider? serviceProvider = null)
    
    // Foundry creation
    public static IWorkflowFoundry CreateFoundry(
        string workflowName,
        IWorkflowForgeLogger? logger = null,
        IDictionary<string, object?>? initialProperties = null,
        WorkflowForgeOptions? options = null)
    
    // Smith creation  
    public static IWorkflowSmith CreateSmith(
        IWorkflowForgeLogger? logger = null,
        IServiceProvider? serviceProvider = null,
        WorkflowForgeOptions? options = null)
}
```

**Why one static type**: Creation APIs stay in one place; names stay consistent across hosts.

### IWorkflowFoundry (Execution Context)

The foundry holds runtime context for a workflow run:

```csharp
public interface IWorkflowFoundry :
    IWorkflowExecutionContext,
    IWorkflowMiddlewarePipeline,
    IOperationLifecycleEvents,
    IDisposable
{
    Task ForgeAsync(CancellationToken ct = default);
    void ReplaceOperations(IEnumerable<IWorkflowOperation> operations);
    bool IsFrozen { get; }
}
```

**Design choices**:
- `ConcurrentDictionary` for thread-safe property access
- `IServiceProvider` for dependency injection integration
- Implements `IOperationLifecycleEvents` for operation monitoring
- Reusable across multiple workflow executions with explicit `ReplaceOperations`
- Pipeline freezes during `ForgeAsync` to prevent mutation mid-execution

### IWorkflowSmith (Orchestration Engine)

The smith manages workflow execution:

```csharp
public interface IWorkflowSmith : IDisposable, IWorkflowLifecycleEvents, ICompensationLifecycleEvents
{
    // Simple pattern: smith manages foundry
    Task ForgeAsync(IWorkflow workflow, CancellationToken ct = default);
    
    // Dictionary pattern: smith creates foundry with data
    Task ForgeAsync(IWorkflow workflow, ConcurrentDictionary<string, object?> data, CancellationToken ct = default);
    
    // Advanced pattern: reusable foundry
    Task ForgeAsync(IWorkflow workflow, IWorkflowFoundry foundry, CancellationToken ct = default);

    // Foundry helpers
    IWorkflowFoundry CreateFoundry(IWorkflowForgeLogger? logger = null, IServiceProvider? serviceProvider = null);
    IWorkflowFoundry CreateFoundryFor(IWorkflow workflow, IWorkflowForgeLogger? logger = null, IServiceProvider? serviceProvider = null);
    IWorkflowFoundry CreateFoundryWithData(ConcurrentDictionary<string, object?> data, IWorkflowForgeLogger? logger = null, IServiceProvider? serviceProvider = null);

    // Workflow-level middleware
    void AddWorkflowMiddleware(IWorkflowMiddleware middleware);
}
```

**Flow**:
1. Validate workflow and foundry
2. Fire `WorkflowStarted` event
3. For each operation:
   - Fire `OperationStarted` event
   - Execute operation via middleware pipeline
   - Fire `OperationCompleted` event
4. Fire `WorkflowCompleted` event
5. On error: Compensation flow (if supported)

### IWorkflowOperation (Executable Tasks)

Operations are the building blocks:

```csharp
public interface IWorkflowOperation : IDisposable
{
    Guid Id { get; }
    string Name { get; }
    
    Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken ct);
    Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken ct);
}
```

**Undo**: Override `RestoreAsync` when an op joins compensation. Base default is no-op; safe to skip.

**Type-Safe Variant**:
```csharp
public interface IWorkflowOperation<TInput, TOutput> : IWorkflowOperation
{
    Task<TOutput> ForgeAsync(TInput input, IWorkflowFoundry foundry, CancellationToken ct);
    Task RestoreAsync(TOutput output, IWorkflowFoundry foundry, CancellationToken ct);
}
```

**Built-in Operations**:
- `DelegateWorkflowOperation` - Lambda-based operations
- `ActionWorkflowOperation` - Side-effect operations (no return value)
- `ConditionalWorkflowOperation` - If-then-else logic
- `ForEachWorkflowOperation` - Collection processing
- `DelayOperation` - Async delays
- `LoggingOperation` - Structured logging

---

## Data Flow Patterns

WorkflowForge supports two data flow patterns, each with specific use cases.

### Primary: dictionary context

**Use when**: Most workflows, especially shifting shapes.

```csharp
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("OrderProcessing")
    .AddOperation("ValidateOrder", async (foundry, ct) => {
        // Store data in foundry properties (typed helpers)
        foundry.SetProperty("OrderId", orderId);
        foundry.SetProperty("Customer", customer);
        foundry.SetProperty("TotalAmount", 100.50m);
    })
    .AddOperation("ProcessPayment", async (foundry, ct) => {
        // Retrieve data from foundry properties
        var orderId = foundry.GetPropertyOrDefault<string>("OrderId");
        var amount = foundry.GetPropertyOrDefault<decimal>("TotalAmount");
        // Process payment...
    })
    .Build();
```

**Pros**:

- Keys can appear or disappear as the workflow evolves.
- Loose coupling between ops.
- Inspect `Properties` at runtime.
- Fits dynamic payloads.

**Cons**:

- Casts at read time.
- Keys are not compile-time checked.
- Typos show up when you run.

### Secondary: type-safe ops

**Use when**: Stable contracts and compile-time checks help.

```csharp
public class ValidateOrderOperation : WorkflowOperationBase<Order, ValidationResult>
{
    public override string Name => "ValidateOrder";
    
    protected override async Task<ValidationResult> ForgeAsyncCore(
        Order input, 
        IWorkflowFoundry foundry, 
        CancellationToken cancellationToken)
    {
        // Type-safe input and output
        var result = new ValidationResult
        {
            IsValid = input.Amount > 0 && input.Customer != null,
            Message = "Order validated"
        };
        
        return result;
    }
}

var workflow = WorkflowForge.CreateWorkflow()
    .WithName("TypeSafeWorkflow")
    .AddOperation(new ValidateOrderOperation())
    .Build();
```

**Compared to the bag**:

- Compile-time checks; obvious contracts.
- Safer refactors.

**Costs**:

- Tighter coupling; more upfront modeling.

**Practical default**: Start with the dictionary pattern; add typed ops when contracts settle.

---

## Event System Design

WorkflowForge splits lifecycle notifications across **three interfaces** so workflow, operation, and compensation hooks are not one megatype.

### How it evolved

The first cut put every hook on `IWorkflowEvents`. Now they group by lifecycle:

```csharp
// Workflow lifecycle
public interface IWorkflowLifecycleEvents
{
    event EventHandler<WorkflowStartedEventArgs>? WorkflowStarted;
    event EventHandler<WorkflowCompletedEventArgs>? WorkflowCompleted;
    event EventHandler<WorkflowFailedEventArgs>? WorkflowFailed;
}

// Operation lifecycle  
public interface IOperationLifecycleEvents
{
    event EventHandler<OperationStartedEventArgs>? OperationStarted;
    event EventHandler<OperationCompletedEventArgs>? OperationCompleted;
    event EventHandler<OperationFailedEventArgs>? OperationFailed;
}

// Compensation lifecycle
public interface ICompensationLifecycleEvents
{
    event EventHandler<CompensationTriggeredEventArgs>? CompensationTriggered;
    event EventHandler<CompensationCompletedEventArgs>? CompensationCompleted;
    event EventHandler<OperationRestoreStartedEventArgs>? OperationRestoreStarted;
    event EventHandler<OperationRestoreCompletedEventArgs>? OperationRestoreCompleted;
    event EventHandler<OperationRestoreFailedEventArgs>? OperationRestoreFailed;
}
```

### Implementation Mapping

- `IWorkflowSmith` implements `IWorkflowLifecycleEvents` + `ICompensationLifecycleEvents`
- `IWorkflowFoundry` implements `IOperationLifecycleEvents`

**Why**: The smith owns the run (workflow + compensation). The foundry owns each operation invocation.

### Event Data

All event args inherit from `BaseWorkflowForgeEventArgs`:
```csharp
public abstract class BaseWorkflowForgeEventArgs : EventArgs
{
    public IWorkflowFoundry Foundry { get; }
    public DateTimeOffset Timestamp { get; }
}
```

Access `ExecutionId` and workflow name via `e.Foundry.ExecutionId` and `e.Foundry.CurrentWorkflow?.Name`.

For event wiring details, see [Event System Guide](../core/events.md).

---

## Middleware Pipeline

A **middleware pipeline** wraps each operation so logging, resilience, and validation stay out of the op body.

### Russian doll layout

Each middleware wraps the next:

```
Request → Middleware 1 → Middleware 2 → Operation → Middleware 2 → Middleware 1 → Response
```

### IWorkflowOperationMiddleware

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

### Example: Timing Middleware

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
            return await next(cancellationToken); // Call next middleware or operation
        }
        finally
        {
            sw.Stop();
            foundry.Logger.LogInformation(
                "Operation {Name} took {Ms}ms", 
                operation.Name, 
                sw.ElapsedMilliseconds);
        }
    }
}
```

### Middleware order

Registration order defines nesting (see [Middleware Pipeline](middleware-pipeline.md) for the exact wrap rules):

- First added runs as the outer layer on the way in.
- Last added hugs the operation.

---

## Compensation Pattern (Saga)

WorkflowForge implements the **Saga pattern** for distributed transaction compensation.

### RestoreAsync Method

Every operation can implement compensation:

```csharp
public class CreateOrderOperation : WorkflowOperationBase
{
    protected override async Task<object?> ForgeAsyncCore(
        object? inputData, 
        IWorkflowFoundry foundry, 
        CancellationToken ct)
    {
        var orderId = await _orderService.CreateOrderAsync();
        foundry.Properties["CreatedOrderId"] = orderId;
        return orderId;
    }
    
    public override async Task RestoreAsync(
        object? outputData, 
        IWorkflowFoundry foundry, 
        CancellationToken ct)
    {
        var orderId = (string)foundry.Properties["CreatedOrderId"];
        await _orderService.DeleteOrderAsync(orderId);
        foundry.Logger.LogInformation("Compensated: Deleted order {OrderId}", orderId);
    }
}
```

### Compensation Flow

1. Workflow executes operations sequentially
2. Operation fails
3. WorkflowSmith triggers compensation
4. Executes `RestoreAsync` in **reverse order** on completed operations
5. Fires `CompensationTriggered`, `CompensationCompleted` events

**Design choice**: Compensation visits every completed op. Implement `RestoreAsync` where rollback is real; base no-op steps are skipped.

---

## Performance Optimizations

### 1. Minimal Allocations

- Use `ConcurrentDictionary` (no unnecessary copying)
- Struct-based event args where possible
- Object pooling for frequently created objects
- Efficient builder pattern without intermediate collections

### 2. Async Throughout

All operations are async-first:
- No blocking calls
- Proper `ConfigureAwait(false)` where appropriate
- Efficient task chaining

### 3. Thread Safety

- `ConcurrentDictionary` for foundry properties
- No locks in hot paths
- Immutable workflow definitions after build

### 4. Zero Unnecessary Abstractions

- Minimal interface layers
- Direct execution paths
- No reflection in hot paths

**Benchmarks (12 scenarios, .NET 10 / 8 / FX 4.8)**: about **13x–511x** faster than the libraries we compared against, and **6x–575x** less memory, depending on scenario and runtime.

---

## Extension Architecture

### Dependency Isolation with ILRepack

- Extensions that bundle third-party libraries (Serilog, Polly, OpenTelemetry) run those bits through **ILRepack**.
- Third-party bits ship inside the extension assembly; public surfaces stay on WorkflowForge or BCL types.
- Microsoft/System assemblies stay external and resolve normally at runtime.
- That cuts version clashes without inlining framework binaries.

### Extension Pattern

All extensions follow a consistent pattern:

```csharp
// Extension provides middleware or services
public class SerilogWorkflowMiddleware : IWorkflowOperationMiddleware
{
    // Implementation uses embedded Serilog
}

// Extension methods for easy integration
public static class SerilogExtensions
{
    public static IWorkflowFoundry WithSerilog(this IWorkflowFoundry foundry)
    {
        // Setup Serilog logging
        return foundry;
    }
}
```

For extension setup, see [Extensions Guide](../extensions/index.md).

---

## Design Patterns Used

- Creation flows use the static **factory** on `WorkflowForge` and the fluent **builder** on `WorkflowBuilder`.
- `WorkflowForge` is a **facade** over the internal subsystems.
- Middleware is a **decorator** / **chain-of-responsibility** stack.
- Distinct operation implementations follow **strategy**.
- Lifecycle hooks surface through **observer**-style events.
- Compensation follows **saga** semantics.

---

## Thread Safety

Foundry properties rely on `ConcurrentDictionary`; workflow graphs are immutable after build; events behave like standard .NET multicast delegates. **Do not** share one foundry or smith across concurrent workflow runs unless you add your own synchronization: spin up separate instances per parallel unit of work.

---

## Related Documentation

- [API Reference](../reference/api-reference.md) - Type and member reference
- [Operations Guide](../core/operations.md) - Creating custom operations
- [Event System](../core/events.md) - Working with events
- [Performance](../performance/performance.md) - Optimization techniques
- [Extensions](../extensions/index.md) - Available extensions
