# WorkflowForge Operations Guide

<p align="center">
  <img src="../../icon.png" alt="WorkflowForge" width="120" height="120">
</p>

Complete guide to creating and using operations in WorkflowForge.

---

## Table of Contents

- [Overview](#overview)
- [Built-in Operations](#built-in-operations)
- [Creating Custom Operations](#creating-custom-operations)
- [Operation Patterns](#operation-patterns)
- [Data Flow Between Operations](#data-flow-between-operations)
- [Compensation and Rollback](#compensation-and-rollback)
- [Best Practices](#best-practices)

---

## Overview

Operations are the fundamental building blocks of WorkflowForge workflows. Each operation represents a discrete task that transforms data, performs side effects, or makes decisions.

### IWorkflowOperation Interface

```csharp
public interface IWorkflowOperation : IDisposable
{
    Guid Id { get; }
    string Name { get; }
    bool SupportsRestore { get; }
    
    Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken);
    Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken);
}
```

### Key Concepts

- **ForgeAsync**: Main execution method
- **RestoreAsync**: Compensation/rollback logic (optional)
- **SupportsRestore**: Indicates if operation can be rolled back
- **Foundry**: Provides execution context, logging, and services

---

## Built-in Operations

WorkflowForge provides 7 built-in operation types:

### 1. DelegateWorkflowOperation

Lambda-based operations for quick, inline logic.

```csharp
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("ProcessOrder")
    .AddOperation("ValidateOrder", async (input, foundry, ct) => {
        var order = (Order)input;
        foundry.Logger.LogInformation("Validating order {OrderId}", order.Id);
        
        if (order.Amount <= 0)
            throw new InvalidOperationException("Invalid order amount");
            
        return order;
    })
    .Build();
```

**When to Use**: Simple operations, prototyping, one-off logic

**Features**:
- Inline lambda syntax
- Quick to write
- Good for simple transformations

### 2. ActionWorkflowOperation

Side-effect operations that don't return values.

```csharp
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("Notifications")
    .AddOperation("SendEmail", async (input, foundry, ct) => {
        var email = foundry.Properties["CustomerEmail"] as string;
        await _emailService.SendAsync(email, "Order Confirmed");
        foundry.Logger.LogInformation("Email sent to {Email}", email);
    })
    .Build();
```

**When to Use**: Logging, notifications, audit trails, cleanup

**Features**:
- No return value (returns input unchanged)
- Focus on side effects
- Clean separation of concerns

### 3. ConditionalWorkflowOperation

If-then-else decision logic.

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

**When to Use**: Branching logic, routing, decision points

**Features**:
- Clean if-then-else semantics
- Nested operations
- Condition evaluation with foundry access

### 4. ForEachWorkflowOperation

Process collections sequentially or in parallel.

```csharp
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("ProcessOrderItems")
    .AddOperation(new ForEachWorkflowOperation<OrderItem>(
        name: "ProcessItems",
        itemsSource: (input, foundry, ct) => {
            var items = foundry.Properties["OrderItems"] as IEnumerable<OrderItem>;
            return Task.FromResult(items);
        },
        itemOperation: new DelegateWorkflowOperation<OrderItem, OrderItem>(
            "ProcessSingleItem",
            async (item, foundry, ct) => {
                foundry.Logger.LogInformation("Processing item {ItemId}", item.Id);
                await _inventoryService.ReserveAsync(item.ProductId, item.Quantity);
                item.Reserved = true;
                return item;
            }
        ),
        parallel: false  // Set to true for parallel execution
    ))
    .Build();
```

**When to Use**: Collection processing, batch operations, aggregations

**Features**:
- Sequential or parallel execution
- Individual item processing
- Results aggregation
- Progress tracking

### 5. DelayOperation

Introduce async delays into workflows.

```csharp
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("PollingWorkflow")
    .AddOperation("CheckStatus", async (input, foundry, ct) => {
        var status = await _service.GetStatusAsync();
        foundry.Properties["Status"] = status;
        return status;
    })
    .AddOperation(new DelayOperation("WaitBeforeRetry", TimeSpan.FromSeconds(5)))
    .AddOperation("RetryCheck", async (input, foundry, ct) => {
        // Retry logic
        return input;
    })
    .Build();
```

**When to Use**: Polling, rate limiting, scheduled delays

**Features**:
- Configurable delay duration
- Async/await compatible
- Cancellation token support

### 6. LoggingOperation

Structured logging at specific workflow points.

```csharp
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("AuditedWorkflow")
    .AddOperation(new LoggingOperation(
        "LogStart",
        WorkflowForgeLogLevel.Information,
        "Workflow started for order {OrderId}",
        foundry => new object[] { foundry.Properties["OrderId"] }
    ))
    .AddOperation("ProcessOrder", async (input, foundry, ct) => {
        // Processing logic
        return input;
    })
    .AddOperation(new LoggingOperation(
        "LogCompletion",
        WorkflowForgeLogLevel.Information,
        "Workflow completed successfully"
    ))
    .Build();
```

**When to Use**: Audit points, debugging, progress tracking

**Features**:
- Structured logging
- Log level control
- Property access for dynamic messages

### 7. Custom Operations (WorkflowOperationBase)

For complex business logic, create custom operation classes.

```csharp
public class ValidateOrderOperation : WorkflowOperationBase<Order, ValidationResult>
{
    private readonly IOrderValidator _validator;
    
    public ValidateOrderOperation(IOrderValidator validator)
    {
        _validator = validator;
    }
    
    public override string Name => "ValidateOrder";
    public override bool SupportsRestore => false;
    
    public override async Task<ValidationResult> ForgeAsync(
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

**When to Use**: Complex business logic, testable operations, reusable components

**Features**:
- Type safety
- Dependency injection
- Unit testable
- Clean separation of concerns

---

## Creating Custom Operations

### Method 1: Inherit from WorkflowOperationBase

For untyped operations:

```csharp
public class CustomOperation : WorkflowOperationBase
{
    public override string Name => "CustomOperation";
    public override bool SupportsRestore => true;
    
    public override async Task<object?> ForgeAsync(
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
    
    public override async Task RestoreAsync(
        object? outputData, 
        IWorkflowFoundry foundry, 
        CancellationToken cancellationToken)
    {
        // Compensation logic
        foundry.Logger.LogInformation("Rolling back custom operation");
        foundry.Properties.TryRemove("Result", out _);
    }
}
```

### Method 2: Inherit from WorkflowOperationBase<TInput, TOutput>

For typed operations:

```csharp
public class ProcessOrderOperation : WorkflowOperationBase<Order, ProcessResult>
{
    private readonly IOrderService _orderService;
    
    public ProcessOrderOperation(IOrderService orderService)
    {
        _orderService = orderService;
    }
    
    public override string Name => "ProcessOrder";
    public override bool SupportsRestore => true;
    
    public override async Task<ProcessResult> ForgeAsync(
        Order input, 
        IWorkflowFoundry foundry, 
        CancellationToken cancellationToken)
    {
        var result = await _orderService.ProcessAsync(input, cancellationToken);
        
        // Store for restoration
        foundry.Properties["ProcessedOrderId"] = result.OrderId;
        
        return result;
    }
    
    public override async Task RestoreAsync(
        ProcessResult output, 
        IWorkflowFoundry foundry, 
        CancellationToken cancellationToken)
    {
        var orderId = (string)foundry.Properties["ProcessedOrderId"];
        await _orderService.CancelAsync(orderId, cancellationToken);
    }
}
```

---

## Operation Patterns

### Pattern 1: Chain of Transformations

Each operation transforms data and passes it to the next.

```csharp
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("DataPipeline")
    .AddOperation("LoadData", async (input, foundry, ct) => {
        var data = await _repository.LoadAsync();
        foundry.Properties["RawData"] = data;
        return data;
    })
    .AddOperation("TransformData", async (input, foundry, ct) => {
        var raw = foundry.Properties["RawData"] as RawData;
        var transformed = Transform(raw);
        foundry.Properties["TransformedData"] = transformed;
        return transformed;
    })
    .AddOperation("SaveData", async (input, foundry, ct) => {
        var data = foundry.Properties["TransformedData"] as TransformedData;
        await _repository.SaveAsync(data);
        return data;
    })
    .Build();
```

### Pattern 2: Aggregation

Collect results from multiple operations.

```csharp
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("Aggregation")
    .AddOperation("FetchUserData", async (input, foundry, ct) => {
        var user = await _userService.GetAsync(userId);
        foundry.Properties["User"] = user;
        return input;
    })
    .AddOperation("FetchOrderData", async (input, foundry, ct) => {
        var orders = await _orderService.GetForUserAsync(userId);
        foundry.Properties["Orders"] = orders;
        return input;
    })
    .AddOperation("AggregateResults", async (input, foundry, ct) => {
        var user = foundry.Properties["User"] as User;
        var orders = foundry.Properties["Orders"] as List<Order>;
        
        var result = new AggregatedData {
            User = user,
            Orders = orders,
            TotalSpent = orders.Sum(o => o.Amount)
        };
        
        return result;
    })
    .Build();
```

### Pattern 3: Conditional Routing

Route workflow based on runtime conditions.

```csharp
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("ConditionalRouting")
    .AddOperation("ClassifyRequest", async (input, foundry, ct) => {
        var request = input as Request;
        foundry.Properties["RequestType"] = request.Type;
        return input;
    })
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

Process items in parallel then join results.

```csharp
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("ParallelProcessing")
    .AddOperation(new ForEachWorkflowOperation<Item>(
        "ProcessInParallel",
        (input, foundry, ct) => Task.FromResult(items),
        new DelegateWorkflowOperation<Item, Result>(
            "ProcessItem",
            async (item, foundry, ct) => await ProcessAsync(item)
        ),
        parallel: true  // Parallel execution
    ))
    .AddOperation("JoinResults", async (input, foundry, ct) => {
        var results = foundry.Properties["ProcessInParallel_Results"] as List<Result>;
        var aggregated = Aggregate(results);
        return aggregated;
    })
    .Build();
```

---

## Data Flow Between Operations

### Primary: Dictionary-Based (Recommended)

Store all workflow data in `foundry.Properties`:

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

**Advantages**:
- Flexible - add/remove properties dynamically
- No type constraints
- Easy debugging

**Best Practices**:
- Use consistent key names
- Store primitive types or serializable objects
- Consider using constants for key names

### Secondary: Type-Safe Input/Output

Pass data directly between typed operations:

```csharp
// This pattern chains operations with type safety
var result1 = await operation1.ForgeAsync(input, foundry, ct);   // Returns Order
var result2 = await operation2.ForgeAsync(result1, foundry, ct); // Takes Order, returns ValidationResult
```

**Advantages**:
- Compile-time type safety
- IntelliSense support
- Clear contracts

**Best Practices**:
- Use for operations with stable contracts
- Document expected input/output types
- Consider immutable types for safety

---

## Compensation and Rollback

### Implementing Compensation

```csharp
public class CreateOrderOperation : WorkflowOperationBase
{
    public override bool SupportsRestore => true;
    
    public override async Task<object?> ForgeAsync(
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
5. Only operations with `SupportsRestore = true` are compensated

### Execution and Compensation Modes

- **Default**: stop on first error, best-effort compensation.
- **ContinueOnError**: run all operations and throw `AggregateException` at the end.
- **FailFastCompensation**: stop compensation on first restore failure.
- **ThrowOnCompensationError**: surface compensation failures as `AggregateException`.

---

## Best Practices

### 1. Keep Operations Focused

Each operation should do one thing well:

```csharp
// Good: Focused operations
.AddOperation("ValidateOrder", ValidateAsync)
.AddOperation("ReserveInventory", ReserveAsync)
.AddOperation("ProcessPayment", ProcessPaymentAsync)

// Bad: God operation
.AddOperation("ProcessEverything", async (input, foundry, ct) => {
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
public override async Task<object?> ForgeAsync(...)
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
// Operations that modify state should support restoration
public override bool SupportsRestore => true;  // For: Create, Update, Delete
public override bool SupportsRestore => false; // For: Read, Query, Log
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

// Okay: Simple inline logic
.AddOperation("SimpleTransform", async (input, foundry, ct) => Transform(input))
```

### 6. Handle Cancellation

```csharp
public override async Task<object?> ForgeAsync(
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

## Next Steps

- **[Architecture](../architecture/overview.md)** - Understanding WorkflowForge design
- **[Event System](events.md)** - Monitoring operation execution
- **[Samples Guide](../getting-started/samples-guide.md)** - See operations in action
- **[API Reference](../reference/api-reference.md)** - Complete API documentation

---

**← Back to [Documentation Home](../index.md)**
