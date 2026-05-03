---
title: Operations Guide
description: Built-in operation types, custom `WorkflowOperationBase` subclasses, data flow, compensation, and `restoreAction` patterns.
---

# WorkflowForge Operations Guide

Built-in helpers, custom `WorkflowOperationBase` types, and how data plus rollback move through a graph.

---

> Prefer **`WorkflowOperationBase`** over raw **`IWorkflowOperation`**: ids, default `RestoreAsync`/`Dispose`, hooks, and **`ForgeAsyncCore`** are already wired.

---

## Table of Contents

- [Overview](#overview)
- [Built-in Operations](#built-in-operations)
- [Creating Custom Operations](#creating-custom-operations)
- [Operation Patterns](#operation-patterns)
- [Data Flow Between Operations](#data-flow-between-operations)
- [Compensation and Rollback](#compensation-and-rollback)
- [Inline Compensation with restoreAction](#inline-compensation-with-restoreaction)
- [Guidelines](#guidelines)

---

## Overview

One operation is one step: transform, side effect, or branch.

### IWorkflowOperation Interface

> Production code should inherit **`WorkflowOperationBase`**. The interface is the contract reference.

```csharp
public interface IWorkflowOperation : IDisposable
{
    Guid Id { get; }
    string Name { get; }
    
    Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken);
    Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken);
}
```

### Key concepts

- **ForgeAsync**: Forward work
- **RestoreAsync**: Undo path; override when needed (base is no-op; engine skips safely)
- **Foundry**: Context, logging, services

---

## Built-in Operations

Seven built-in operation types:

### 1. DelegateWorkflowOperation

Lambdas for quick glue.

```csharp
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("ProcessOrder")
    .AddOperation(new DelegateWorkflowOperation(
        "ValidateOrder",
        async (input, foundry, ct) => {
            var order = (Order)input;
            foundry.Logger.LogInformation("Validating order {OrderId}", order.Id);
            
            if (order.Amount <= 0)
                throw new InvalidOperationException("Invalid order amount");
                
            return order;
        }
    ))
    .Build();
```

**When**: Spikes, small transforms, throwaway helpers.

- Minimal ceremony.

### 2. ActionWorkflowOperation

Fire-and-forget body; no return value.

```csharp
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("Notifications")
    .AddOperation(new ActionWorkflowOperation(
        "SendEmail",
        async (input, foundry, ct) => {
            var email = foundry.Properties["CustomerEmail"] as string;
            await _emailService.SendAsync(email, "Order Confirmed");
            foundry.Logger.LogInformation("Email sent to {Email}", email);
        }
    ))
    .Build();
```

**When**: Logging, email, cleanup.

- Input passes through unchanged.

### 3. ConditionalWorkflowOperation

Predicate picks one of two child ops.

```csharp
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("OrderProcessing")
    .AddOperation(new ConditionalWorkflowOperation(
        name: "CheckOrderValue",
        condition: (input, foundry, ct) => {
            var amount = (decimal)foundry.Properties["OrderAmount"];
            return Task.FromResult(amount > 1000);
        },
        trueOperation: new DelegateWorkflowOperation(
            "HighValueProcessing",
            async (input, foundry, ct) => {
                foundry.Logger.LogInformation("High-value order processing");
                foundry.Properties["RequiresApproval"] = true;
                return input;
            }
        ),
        falseOperation: new DelegateWorkflowOperation(
            "StandardProcessing",
            async (input, foundry, ct) => {
                foundry.Logger.LogInformation("Standard order processing");
                return input;
            }
        )
    ))
    .Build();
```

**When**: Branching on runtime state.

- True/false arms are normal `IWorkflowOperation` instances.

### 4. ForEachWorkflowOperation

Runs child ops concurrently with a shared input, split collection, or null input.

```csharp
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("ProcessOrderItems")
    .AddOperation(ForEachWorkflowOperation.CreateSharedInput(
        new IWorkflowOperation[] {
            new ValidateInventoryOperation(),
            new ReserveInventoryOperation(),
            new NotifyWarehouseOperation()
        },
        maxConcurrency: 2,  // Throttle to 2 concurrent operations
        name: "ProcessItems"
    ))
    .Build();

// Or split input collection among operations
var splitWorkflow = WorkflowForge.CreateWorkflow()
    .WithName("DistributedProcessing")
    .AddOperation(ForEachWorkflowOperation.CreateSplitInput(
        itemOperations,
        maxConcurrency: 4
    ))
    .Build();
```

**Factories**:
- `CreateSharedInput` — same input everywhere
- `CreateSplitInput` — partition a collection
- `CreateNoInput` — `null` input per child

**When**: Fan-out or batch work with a concurrency cap.

### 4b. AddParallelOperations (builder helper)

Fluent shortcut over `ForEachWorkflowOperation.CreateSharedInput`.

```csharp
// Simple parallel execution (all operations get the same input)
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("ParallelValidation")
    .AddParallelOperations(
        new ValidateInventoryOperation(),
        new CheckFraudOperation(),
        new VerifyCustomerOperation()
    )
    .AddOperation(new ProcessOrderOperation())
    .Build();

// With concurrency control, timeout, and naming
var controlledWorkflow = WorkflowForge.CreateWorkflow()
    .WithName("ControlledParallel")
    .AddParallelOperations(
        operations: new[] { op1, op2, op3, op4 },
        maxConcurrency: 2,                      // Max 2 concurrent
        timeout: TimeSpan.FromSeconds(30),      // 30s timeout
        name: "ParallelValidations"             // Named group
    )
    .Build();
```

**Method Signatures**:
```csharp
// Simple params overload
WorkflowBuilder AddParallelOperations(params IWorkflowOperation[] operations)

// Full control overload
WorkflowBuilder AddParallelOperations(
    IEnumerable<IWorkflowOperation> operations,
    int? maxConcurrency = null,
    TimeSpan? timeout = null,
    string? name = null)
```

**When**: Parallel segments straight on `WorkflowBuilder`.

- Wraps `ForEachWorkflowOperation.CreateSharedInput` with optional concurrency, timeout, name.

### 5. DelayOperation

`Task.Delay` as an op.

```csharp
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("PollingWorkflow")
    .AddOperation(new DelegateWorkflowOperation(
        "CheckStatus",
        async (input, foundry, ct) => {
            var status = await _service.GetStatusAsync();
            foundry.Properties["Status"] = status;
            return status;
        }
    ))
    .AddOperation(new DelayOperation(TimeSpan.FromSeconds(5), "WaitBeforeRetry"))
    .AddOperation(new DelegateWorkflowOperation(
        "RetryCheck",
        async (input, foundry, ct) => {
            // Retry logic
            return input;
        }
    ))
    .Build();
```

**When**: Polling gaps, rate limits, fixed waits.

- Honors `CancellationToken`.

### 6. LoggingOperation

Emits a log at a point in the graph.

```csharp
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("AuditedWorkflow")
    .AddOperation(new LoggingOperation(
        "Workflow started for order processing",
        WorkflowForgeLogLevel.Information,
        "LogStart" // optional name
    ))
    .AddOperation(new DelegateWorkflowOperation(
        "ProcessOrder",
        async (input, foundry, ct) => {
            // Processing logic
            return input;
        }
    ))
    .AddOperation(new LoggingOperation("Workflow completed successfully"))
    .Build();

// Alternative: Use static factory methods for cleaner code
var workflow2 = WorkflowForge.CreateWorkflow()
    .WithName("AuditedWorkflow")
    .AddOperation(LoggingOperation.Info("Starting order processing"))
    .AddOperation(new DelegateWorkflowOperation("ProcessOrder", async (input, foundry, ct) => input))
    .AddOperation(LoggingOperation.Info("Workflow completed"))
    .Build();
```

**Constructor**:
```csharp
LoggingOperation(string message, WorkflowForgeLogLevel logLevel = Information, string? name = null)
```

**Static Factory Methods**:
- `LoggingOperation.Trace(message)`
- `LoggingOperation.Debug(message)`
- `LoggingOperation.Info(message)`
- `LoggingOperation.Warning(message)`
- `LoggingOperation.Error(message)`
- `LoggingOperation.Critical(message)`

**When**: Checkpoints, breadcrumbs.

- Pick level via `WorkflowForgeLogLevel` or helpers like `LoggingOperation.Info`.

### 7. Custom Operations (WorkflowOperationBase)

Class-based steps with DI and tests.

```csharp
public class ValidateOrderOperation : WorkflowOperationBase<Order, ValidationResult>
{
    private readonly IOrderValidator _validator;
    
    public ValidateOrderOperation(IOrderValidator validator)
    {
        _validator = validator;
    }
    
    public override string Name => "ValidateOrder";
    
    protected override async Task<ValidationResult> ForgeAsyncCore(
        Order input, 
        IWorkflowFoundry foundry, 
        CancellationToken cancellationToken)
    {
        foundry.Logger.LogInformation("Validating order {OrderId}", input.Id);
        
        var result = await _validator.ValidateAsync(input, cancellationToken);
        
        foundry.Properties["ValidationResult"] = result;
        
        return result;
    }
}

// Usage
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("TypeSafeWorkflow")
    .AddOperation(new ValidateOrderOperation(orderValidator))
    .Build();
```

**When**: Real domain rules, injected services, unit tests.

- Typed `ForgeAsyncCore`, narrow surface.

---

## Creating Custom Operations

### Method 1: Inherit from WorkflowOperationBase

Implement **`ForgeAsyncCore`** for untyped ops:

```csharp
public class CustomOperation : WorkflowOperationBase
{
    public override string Name => "CustomOperation";
    
    protected override async Task<object?> ForgeAsyncCore(
        object? inputData, 
        IWorkflowFoundry foundry, 
        CancellationToken cancellationToken)
    {
        // Your logic here
        foundry.Logger.LogInformation("Executing custom operation");
        
        // Access foundry properties
        foundry.Properties["Result"] = "Success";
        
        // Return result
        return inputData;
    }
    
    public override Task RestoreAsync(
        object? outputData, 
        IWorkflowFoundry foundry, 
        CancellationToken cancellationToken)
    {
        // Compensation logic
        foundry.Logger.LogInformation("Rolling back custom operation");
        foundry.Properties.TryRemove("Result", out _);
        return Task.CompletedTask;
    }
}
```

### Method 1b: Lifecycle hooks

`OnBeforeExecuteAsync` / `OnAfterExecuteAsync` for cross-cutting setup without bloating `ForgeAsyncCore`.

```csharp
public class AuditedOperation : WorkflowOperationBase
{
    public override string Name => "AuditedOperation";
    
    protected override Task OnBeforeExecuteAsync(
        object? inputData, 
        IWorkflowFoundry foundry, 
        CancellationToken ct)
    {
        foundry.Logger.LogInformation("Starting {Operation}", Name);
        foundry.Properties["StartTime"] = DateTime.UtcNow;
        return Task.CompletedTask;
    }
    
    protected override async Task<object?> ForgeAsyncCore(
        object? inputData, 
        IWorkflowFoundry foundry, 
        CancellationToken ct)
    {
        // Pure business logic
        return await ProcessDataAsync(inputData, ct);
    }
    
    protected override Task OnAfterExecuteAsync(
        object? inputData, 
        object? outputData, 
        IWorkflowFoundry foundry, 
        CancellationToken ct)
    {
        var duration = DateTime.UtcNow - (DateTime)foundry.Properties["StartTime"]!;
        foundry.Logger.LogInformation("Completed {Operation} in {Duration}ms", Name, duration.TotalMilliseconds);
        return Task.CompletedTask;
    }
}
```

### Method 2: `WorkflowOperationBase<TInput, TOutput>`

Typed input/output:

```csharp
public class ProcessOrderOperation : WorkflowOperationBase<Order, ProcessResult>
{
    private readonly IOrderService _orderService;
    
    public ProcessOrderOperation(IOrderService orderService)
    {
        _orderService = orderService;
    }
    
    public override string Name => "ProcessOrder";
    
    protected override async Task<ProcessResult> ForgeAsyncCore(
        Order input, 
        IWorkflowFoundry foundry, 
        CancellationToken cancellationToken)
    {
        var result = await _orderService.ProcessAsync(input, cancellationToken);
        
        // Store for restoration
        foundry.Properties["ProcessedOrderId"] = result.OrderId;
        
        return result;
    }
    
    public override Task RestoreAsync(
        ProcessResult output, 
        IWorkflowFoundry foundry, 
        CancellationToken cancellationToken)
    {
        var orderId = (string)foundry.Properties["ProcessedOrderId"];
        return _orderService.CancelAsync(orderId, cancellationToken);
    }
}
```

---

## Operation Patterns

### Pattern 1: Chain of Transformations

```csharp
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("DataPipeline")
    .AddOperation(new DelegateWorkflowOperation(
        "LoadData",
        async (input, foundry, ct) => {
            var data = await _repository.LoadAsync();
            foundry.Properties["RawData"] = data;
            return data;
        }
    ))
    .AddOperation(new DelegateWorkflowOperation(
        "TransformData",
        async (input, foundry, ct) => {
            var raw = foundry.Properties["RawData"] as RawData;
            var transformed = Transform(raw);
            foundry.Properties["TransformedData"] = transformed;
            return transformed;
        }
    ))
    .AddOperation(new DelegateWorkflowOperation(
        "SaveData",
        async (input, foundry, ct) => {
            var data = foundry.Properties["TransformedData"] as TransformedData;
            await _repository.SaveAsync(data);
            return data;
        }
    ))
    .Build();
```

### Pattern 2: Aggregation

```csharp
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("Aggregation")
    .AddOperation(new DelegateWorkflowOperation(
        "FetchUserData",
        async (input, foundry, ct) => {
            var user = await _userService.GetAsync(userId);
            foundry.Properties["User"] = user;
            return input;
        }
    ))
    .AddOperation(new DelegateWorkflowOperation(
        "FetchOrderData",
        async (input, foundry, ct) => {
            var orders = await _orderService.GetForUserAsync(userId);
            foundry.Properties["Orders"] = orders;
            return input;
        }
    ))
    .AddOperation(new DelegateWorkflowOperation(
        "AggregateResults",
        async (input, foundry, ct) => {
            var user = foundry.Properties["User"] as User;
            var orders = foundry.Properties["Orders"] as List<Order>;
            
            var result = new AggregatedData {
                User = user,
                Orders = orders,
                TotalSpent = orders.Sum(o => o.Amount)
            };
            
            return result;
        }
    ))
    .Build();
```

### Pattern 3: Conditional Routing

```csharp
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("ConditionalRouting")
    .AddOperation(new DelegateWorkflowOperation(
        "ClassifyRequest",
        async (input, foundry, ct) => {
            var request = input as Request;
            foundry.Properties["RequestType"] = request.Type;
            return input;
        }
    ))
    .AddOperation(new ConditionalWorkflowOperation(
        "RouteByType",
        (input, foundry, ct) => {
            var type = (RequestType)foundry.Properties["RequestType"];
            return Task.FromResult(type == RequestType.Premium);
        },
        trueOperation: new DelegateWorkflowOperation(
            "PremiumProcessing",
            async (input, foundry, ct) => { /* Premium logic */ return input; }
        ),
        falseOperation: new DelegateWorkflowOperation(
            "StandardProcessing",
            async (input, foundry, ct) => { /* Standard logic */ return input; }
        )
    ))
    .Build();
```

### Pattern 4: Fork-Join

```csharp
// Create multiple processing operations
var itemOperations = items.Select(item => 
    new DelegateWorkflowOperation(
        $"Process_{item.Id}",
        async (input, foundry, ct) => {
            var result = await ProcessAsync(item);
            foundry.Properties[$"Result_{item.Id}"] = result;
            return result;
        }
    )).Cast<IWorkflowOperation>().ToArray();

var workflow = WorkflowForge.CreateWorkflow()
    .WithName("ParallelProcessing")
    .AddOperation(ForEachWorkflowOperation.CreateNoInput(
        itemOperations,
        maxConcurrency: Environment.ProcessorCount,
        name: "ProcessInParallel"
    ))
    .AddOperation(new DelegateWorkflowOperation(
        "JoinResults",
        async (input, foundry, ct) => {
            var results = items.Select(item => 
                foundry.GetPropertyOrDefault<Result>($"Result_{item.Id}")).ToList();
            var aggregated = Aggregate(results);
            return aggregated;
        }
    ))
    .Build();
```

---

## Data Flow Between Operations

### Primary: dictionary (default bag)

```csharp
// Operation 1: Store data
foundry.Properties["CustomerId"] = customerId;
foundry.Properties["OrderDate"] = DateTime.UtcNow;
foundry.Properties["Items"] = orderItems;

// Operation 2: Retrieve data
var customerId = (string)foundry.Properties["CustomerId"];
var orderDate = (DateTime)foundry.Properties["OrderDate"];
var items = foundry.Properties["Items"] as List<OrderItem>;
```

**Pros**:
- Keys evolve without type churn
- Loose coupling
- Easy to dump while debugging

**Tip**: Stable key names; primitives or serializable objects; constants beat magic strings.

### Secondary: typed calls

Chaining `ForgeAsync` manually when you want compile-time flow:

```csharp
// This pattern chains operations with type safety
var result1 = await operation1.ForgeAsync(input, foundry, ct);   // Returns Order
var result2 = await operation2.ForgeAsync(result1, foundry, ct); // Takes Order, returns ValidationResult
```

**Pros**: compile-time flow, IntelliSense, obvious contracts. **Use when** shapes stay stable.

### Output Chaining Behavior

Default: each op's output feeds the next op's `inputData`. Turn it off so every op always sees `null`:

```csharp
var options = new WorkflowForgeOptions
{
    EnableOutputChaining = false
};
var foundry = WorkflowForge.CreateFoundry("NoChaining", options: options);
```

---

## Compensation and Rollback

### Implementing Compensation

Override **`RestoreAsync`** for undo. Base default is no-op; skipped safely when unchanged.

```csharp
public class CreateOrderOperation : WorkflowOperationBase
{
    protected override async Task<object?> ForgeAsyncCore(
        object? inputData, 
        IWorkflowFoundry foundry, 
        CancellationToken cancellationToken)
    {
        var orderId = await _orderService.CreateAsync();
        
        // Store for potential rollback
        foundry.Properties["CreatedOrderId"] = orderId;
        foundry.Logger.LogInformation("Order {OrderId} created", orderId);
        
        return orderId;
    }
    
    public override async Task RestoreAsync(
        object? outputData, 
        IWorkflowFoundry foundry, 
        CancellationToken cancellationToken)
    {
        var orderId = (string)foundry.Properties["CreatedOrderId"];
        await _orderService.DeleteAsync(orderId);
        foundry.Logger.LogInformation("Order {OrderId} rolled back", orderId);
    }
}
```

### Compensation Flow

1. Workflow executes operations sequentially
2. Operation fails
3. WorkflowSmith triggers compensation
4. Executes `RestoreAsync` in **reverse order** on completed operations
5. Operations that override `RestoreAsync` run their compensation logic; operations that use the base class default (no-op) are safely skipped

### Execution and Compensation Modes

- **Default**: stop on first error, best-effort compensation.
- **ContinueOnError**: run all operations and throw `AggregateException` at the end.
- **FailFastCompensation**: stop compensation on first restore failure.
- **ThrowOnCompensationError**: surface compensation failures as `AggregateException`.

---

## Inline Compensation with restoreAction

Inline `restoreAction` / `restoreFunc` on builder or foundry APIs when you do not want a separate class.

### Builder API

```csharp
var workflow = WorkflowForge.CreateWorkflow("OrderProcessing")
    .AddOperation("ProcessPayment", async (foundry, ct) =>
    {
        // Process payment logic
        foundry.Properties["payment_id"] = "PAY-123";
    },
    restoreAction: async (foundry, ct) =>
    {
        // Compensation: refund the payment
        var paymentId = foundry.Properties["payment_id"];
        // Issue refund...
    })
    .AddOperation("UpdateInventory", (foundry) =>
    {
        // Update inventory
    },
    restoreAction: (foundry) =>
    {
        // Compensation: restore inventory
    })
    .Build();
```

### Foundry API

```csharp
using var foundry = WorkflowForge.CreateFoundry("PaymentFoundry");
foundry.WithOperation("ChargeCard",
    async (f) => { /* charge */ },
    restoreAction: async (f) => { /* refund */ });
```

### Notes

- Omit `restoreAction` to keep the base no-op.
- Compensation still walks completed ops in reverse.
- Share state through foundry properties between forward and restore paths.

---

## Guidelines

### 1. Keep Operations Focused

Each operation should do one thing well:

```csharp
// Good: Focused operations
.AddOperation("ValidateOrder", ValidateAsync)
.AddOperation("ReserveInventory", ReserveAsync)
.AddOperation("ProcessPayment", ProcessPaymentAsync)

// Bad: God operation
.AddOperation("ProcessEverything", async (foundry, ct) => {
    // Validation, inventory, payment all mixed together
})
```

### 2. Use Foundry Properties for Shared State

```csharp
// Good: Store in foundry (typed helpers)
foundry.SetProperty("OrderId", orderId);
foundry.SetProperty("ProcessedAt", DateTime.UtcNow);

// Bad: Hidden state
private static string _orderId; // Don't do this
```

### 3. Log Important Events

```csharp
protected override async Task<object?> ForgeAsyncCore(...)
{
    foundry.Logger.LogInformation("Processing order {OrderId}", orderId);
    
    try
    {
        var result = await ProcessAsync(orderId);
        foundry.Logger.LogInformation("Order {OrderId} processed successfully", orderId);
        return result;
    }
    catch (Exception ex)
    {
        foundry.Logger.LogError(ex, "Failed to process order {OrderId}", orderId);
        throw;
    }
}
```

### 4. Implement Compensation for Critical Operations

```csharp
// Operations that modify state should override RestoreAsync for compensation
// For: Create, Update, Delete — override RestoreAsync with rollback logic
// For: Read, Query, Log — no override needed; base class no-op is used
```

### 5. Use Type-Safe Operations for Complex Business Logic

```csharp
// Good: Testable, maintainable
public class ComplexBusinessLogic : WorkflowOperationBase<Input, Output>
{
    // Can be unit tested
    // Dependencies injected
    // Clear contracts
}

// Okay: Simple delegate operation
.AddOperation(new DelegateWorkflowOperation(
    "SimpleTransform",
    async (input, foundry, ct) => Transform(input)
))
```

### 6. Handle Cancellation

```csharp
protected override async Task<object?> ForgeAsyncCore(
    object? inputData, 
    IWorkflowFoundry foundry, 
    CancellationToken cancellationToken)
{
    // Pass cancellation token to async operations
    var result = await _service.ProcessAsync(data, cancellationToken);
    
    // Check cancellation periodically in loops
    foreach (var item in items)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Process item
    }
    
    return result;
}
```

### 7. Dispose Resources Properly

```csharp
public class ResourceOperation : WorkflowOperationBase
{
    private readonly IDisposable _resource;
    
    public override void Dispose()
    {
        _resource?.Dispose();
        base.Dispose();
    }
}
```

---

## Related Documentation

- [Architecture](../architecture/overview.md) - Metaphor, components, and layering
- [Event System](../core/events.md) - Monitoring operation execution
- [Samples Guide](../getting-started/samples-guide.md) - See operations in action
- [API Reference](../reference/api-reference.md) - Member-level reference
