# WorkflowForge Architecture

<p align="center">
  <img src="../icon.png" alt="WorkflowForge" width="120" height="120">
</p>

Complete architectural overview of WorkflowForge's design principles, patterns, and implementation.

---

## Table of Contents

- [Design Philosophy](#design-philosophy)
- [Core Metaphor](#core-metaphor)
- [Architectural Principles](#architectural-principles)
- [Component Architecture](#component-architecture)
- [Data Flow Patterns](#data-flow-patterns)
- [Event System Design](#event-system-design)
- [Middleware Pipeline](#middleware-pipeline)
- [Compensation Pattern](#compensation-pattern)
- [Performance Optimizations](#performance-optimizations)
- [Extension Architecture](#extension-architecture)

---

## Design Philosophy

WorkflowForge is built on three core principles:

### 1. Zero Dependencies
The core package has **no external dependencies**. This ensures:
- No version conflicts
- Minimal deployment footprint (~50KB)
- Maximum compatibility across .NET versions
- Predictable behavior without third-party surprises

### 2. Performance First
Every design decision considers performance impact:
- Microsecond-level operation execution
- Minimal memory allocations
- Efficient use of `ConcurrentDictionary` for thread safety
- Struct-based event args where possible
- Object pooling for internal structures

### 3. Developer Experience
Clean, intuitive API with industrial metaphor:
- Fluent builder pattern for workflow construction
- Clear separation of concerns (Forge, Foundry, Smith, Operations)
- Type-safe operations when needed
- Dictionary-based context for flexibility
- Comprehensive event system for observability

---

## Core Metaphor

WorkflowForge uses an **industrial metalworking metaphor** that makes complex orchestration intuitive:

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

This metaphor provides intuitive understanding: *just as a smith uses tools in a foundry to shape raw materials into finished products, WorkflowForge uses operations in a foundry to transform data through a workflow*.

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

Open for extension, closed for modification:
- Custom operations via `WorkflowOperationBase`
- Middleware pipeline for cross-cutting concerns
- Extension packages for additional capabilities
- No modification of core required

---

## Component Architecture

### The Forge (Static Factory)

`WorkflowForge` is the main entry point providing factory methods:

```csharp
public static class WorkflowForge
{
    // Workflow creation
    public static IWorkflowBuilder CreateWorkflow()
    
    // Foundry creation
    public static IWorkflowFoundry CreateFoundry(string workflowName)
    public static IWorkflowFoundry CreateFoundry(IWorkflowSettings settings)
    
    // Smith creation  
    public static IWorkflowSmith CreateSmith()
    public static IWorkflowSmith CreateSmith(IWorkflowSettings settings)
}
```

**Design Rationale**: Centralized factory provides discoverability and consistency.

### IWorkflowFoundry (Execution Context)

The foundry provides the execution environment:

```csharp
public interface IWorkflowFoundry : IDisposable, IOperationLifecycleEvents
{
    Guid ExecutionId { get; }                              // Unique execution identifier
    IWorkflow? CurrentWorkflow { get; }                    // Current workflow
    ConcurrentDictionary<string, object?> Properties { get; } // Thread-safe data
    IWorkflowForgeLogger Logger { get; }                  // Logging
    IServiceProvider? ServiceProvider { get; }             // DI container
    
    void SetCurrentWorkflow(IWorkflow? workflow);
    void AddOperation(IWorkflowOperation operation);
}
```

**Key Design Decisions**:
- `ConcurrentDictionary` for thread-safe property access
- `IServiceProvider` for dependency injection integration
- Implements `IOperationLifecycleEvents` for operation monitoring
- Reusable across multiple workflow executions

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
}
```

**Execution Flow**:
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
    bool SupportsRestore { get; }
    
    Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken ct);
    Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken ct);
}
```

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

### Primary Pattern: Dictionary-Based Context

**When to Use**: Most workflows, especially those with dynamic or evolving data structures.

```csharp
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("OrderProcessing")
    .AddOperation("ValidateOrder", async (input, foundry, ct) => {
        // Store data in foundry.Properties
        foundry.Properties["OrderId"] = orderId;
        foundry.Properties["Customer"] = customer;
        foundry.Properties["TotalAmount"] = 100.50m;
        return input;
    })
    .AddOperation("ProcessPayment", async (input, foundry, ct) => {
        // Retrieve data from foundry.Properties
        var orderId = (string)foundry.Properties["OrderId"];
        var amount = (decimal)foundry.Properties["TotalAmount"];
        // Process payment...
        return input;
    })
    .Build();
```

**Advantages**:
- Flexible - add/remove properties as needed
- No type constraints between operations
- Easy to debug (inspect `Properties` dictionary)
- Natural for dynamic workflows

**Considerations**:
- Requires casting when retrieving values
- No compile-time type safety
- Property names must be consistent

### Secondary Pattern: Type-Safe Operations

**When to Use**: Operations with clear contracts that benefit from compile-time safety.

```csharp
public class ValidateOrderOperation : WorkflowOperationBase<Order, ValidationResult>
{
    public override string Name => "ValidateOrder";
    
    public override async Task<ValidationResult> ForgeAsync(
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

**Advantages**:
- Compile-time type safety
- Clear contracts between operations
- IntelliSense support
- Refactoring-friendly

**Considerations**:
- Less flexible than dictionary pattern
- Operations tightly coupled by types
- Requires more upfront design

**Best Practice**: Use dictionary pattern by default, type-safe operations for critical business logic with stable contracts.

---

## Event System Design

WorkflowForge implements a **Single Responsibility Principle (SRP)-compliant event system** with three focused interfaces.

### Design Evolution

**Problem**: Original design had one interface handling all events, violating SRP.

**Solution**: Split into three focused interfaces based on lifecycle concerns:

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
    event EventHandler<CompensationStartedEventArgs>? CompensationStarted;
    event EventHandler<CompensationCompletedEventArgs>? CompensationCompleted;
    event EventHandler<CompensationFailedEventArgs>? CompensationFailed;
}
```

### Implementation Mapping

- `IWorkflowSmith` implements `IWorkflowLifecycleEvents` + `ICompensationLifecycleEvents`
- `IWorkflowFoundry` implements `IOperationLifecycleEvents`

**Rationale**: Smith manages workflow and compensation, Foundry manages operations.

### Event Data

All event args inherit from `BaseWorkflowForgeEventArgs`:
```csharp
public abstract class BaseWorkflowForgeEventArgs : EventArgs
{
    public Guid ExecutionId { get; }
    public string WorkflowName { get; }
    public DateTimeOffset Timestamp { get; }
}
```

For complete event documentation, see [Event System Guide](events.md).

---

## Middleware Pipeline

WorkflowForge supports a **middleware pipeline** for cross-cutting concerns.

### Design Pattern: Russian Doll

Each middleware wraps the next in the chain:

```
Request → Middleware 1 → Middleware 2 → Operation → Middleware 2 → Middleware 1 → Response
```

### IWorkflowOperationMiddleware

```csharp
public interface IWorkflowOperationMiddleware
{
    int Order { get; }  // Execution order (lower executes first)
    
    Task<object?> InvokeAsync(
        IWorkflowOperation operation,
        object? inputData,
        IWorkflowFoundry foundry,
        Func<Task<object?>> next,
        CancellationToken cancellationToken);
}
```

### Example: Timing Middleware

```csharp
public class TimingMiddleware : IWorkflowOperationMiddleware
{
    public int Order => 100;
    
    public async Task<object?> InvokeAsync(
        IWorkflowOperation operation,
        object? inputData,
        IWorkflowFoundry foundry,
        Func<Task<object?>> next,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await next(); // Call next middleware or operation
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

### Middleware Order

Middleware executes based on `Order` property (ascending):
- Order 0-99: Pre-processing (logging, validation)
- Order 100-199: Core concerns (timing, caching)
- Order 200-299: Resilience (retry, circuit breaker)
- Order 300+: Post-processing (cleanup, auditing)

---

## Compensation Pattern (Saga)

WorkflowForge implements the **Saga pattern** for distributed transaction compensation.

### RestoreAsync Method

Every operation can implement compensation:

```csharp
public class CreateOrderOperation : WorkflowOperationBase
{
    public override bool SupportsRestore => true;
    
    public override async Task<object?> ForgeAsync(
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
5. Fires `CompensationStarted`, `CompensationCompleted` events

**Design Decision**: Compensation is opt-in via `SupportsRestore` property.

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

**Result**: 13-378x faster than competitors, 6-1,495x less memory.

---

## Extension Architecture

### Costura.Fody Integration

All extensions embed their dependencies using **Costura.Fody**:

```xml
<PackageReference Include="Fody" Version="6.9.3" PrivateAssets="All" />
<PackageReference Include="Costura.Fody" Version="6.0.0" PrivateAssets="All" />
```

**How it Works**:
1. Extension references third-party libraries (Serilog, Polly, etc.)
2. Costura.Fody embeds those DLLs as compressed resources
3. At runtime, assemblies are loaded from resources
4. User's application sees no third-party dependencies

**Benefit**: Zero version conflicts. Users can have ANY version of Serilog, Polly, FluentValidation, or OpenTelemetry without conflicts.

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

For complete extension documentation, see [Extensions Guide](extensions.md).

---

## Design Patterns Used

### Creational Patterns
- **Factory Pattern**: `WorkflowForge` static factory
- **Builder Pattern**: `IWorkflowBuilder` fluent API

### Structural Patterns
- **Facade Pattern**: `WorkflowForge` simplifies complex subsystems
- **Decorator Pattern**: Middleware pipeline

### Behavioral Patterns
- **Strategy Pattern**: Different operation types
- **Observer Pattern**: Event system
- **Chain of Responsibility**: Middleware pipeline
- **Saga Pattern**: Compensation flow

---

## Thread Safety

### Thread-Safe Components
- `ConcurrentDictionary` in foundry properties
- Immutable workflow definitions
- Event subscriptions (standard .NET events)

### Not Thread-Safe
- Foundry reuse across concurrent workflows (use separate foundries)
- Smith reuse across concurrent workflows (create per-workflow or use locking)

**Best Practice**: Create new foundry and smith instances for concurrent workflows.

---

## Next Steps

- **[API Reference](api-reference.md)** - Complete API documentation
- **[Operations Guide](operations.md)** - Creating custom operations
- **[Event System](events.md)** - Working with events
- **[Performance](performance.md)** - Optimization techniques
- **[Extensions](extensions.md)** - Available extensions

---

[Back to Documentation Hub](README.md)
