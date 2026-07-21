---
title: Getting Started with WorkflowForge
description: Install, configure, and create your first high-performance .NET workflow in minutes with WorkflowForge.
---

# Getting Started with WorkflowForge

<a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=alert_status" alt="Quality Gate Status" /></a>
<a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=coverage" alt="Coverage" /></a>
<a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=reliability_rating" alt="Reliability Rating" /></a>
<a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=security_rating" alt="Security Rating" /></a>
<a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=sqale_rating" alt="Maintainability Rating" /></a>

Install the package, model a short pipeline, run it with a smith and foundry.

## Table of Contents

- [What's New in 2.1.2](#whats-new-in-212)
- [What's New in 2.1.1](#whats-new-in-211)
- [What's New in 2.0.0](#whats-new-in-200)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Your First Workflow](#your-first-workflow)
- [Core Concepts Explained](#core-concepts-explained)
- [Next Steps](#next-steps)
- [Troubleshooting](#troubleshooting)

---

## What's New in 2.1.2

**2.1.2** is a bug-fix release: it fixes the Polly extension's missing `Microsoft.Bcl.TimeProvider` dependency (and the same class of issue in the OpenTelemetry/Serilog extensions), makes `EnablePerformanceMonitoring()` / `GetPerformanceStatistics()` work end-to-end, fixes a pooled-foundry state leak and audit workflow-name resolution, and centralizes package versioning. Full notes: [CHANGELOG.md](https://github.com/animatlabs/workflow-forge/blob/main/CHANGELOG.md).

---

## What's New in 2.1.1

**2.1.1** multi-targets **.NET 10**, **.NET 8**, and **.NET Framework 4.8**, removes `SupportsRestore` in favor of always calling `RestoreAsync` (base no-op when unused), adds inline `restoreAction` / `restoreFunc`, seals event/exception types, hardens threading and disposal, and adds `GetOperationOutput` / `FoundryPropertyKeys`. Full notes: [CHANGELOG.md](https://github.com/animatlabs/workflow-forge/blob/main/CHANGELOG.md).

---

## What's New in 2.0.0

**2.0.0** isolated extension dependencies with **ILRepack**, shipped **Validation** and **Audit** packages, split lifecycle events into three interfaces (see [Events Guide](../core/events.md)), and moved **`ISystemTimeProvider`** behind DI. Full notes: [CHANGELOG.md](https://github.com/animatlabs/workflow-forge/blob/main/CHANGELOG.md).

## Prerequisites

You need:

- **.NET SDK**: .NET 8.0+ or .NET Framework 4.8 (WorkflowForge targets .NET Standard 2.0)
- **IDE**: Visual Studio 2022, VS Code, or JetBrains Rider
- **Basic C#**: Comfortable using `async`/`await` in console or web apps

## Installation

### Step 1: Create a project

```bash
# Create a new console application
dotnet new console -n MyWorkflowApp
cd MyWorkflowApp
```

### Step 2: Add packages

```bash
dotnet add package WorkflowForge

# Optional
dotnet add package WorkflowForge.Extensions.Logging.Serilog
dotnet add package WorkflowForge.Extensions.Resilience.Polly
dotnet add package WorkflowForge.Extensions.Validation
dotnet add package WorkflowForge.Extensions.Audit
```

### Step 3: Build

```bash
dotnet build
```

## Your First Workflow

Validate, pay, fulfill.

### Step 1: Models

```csharp
// Models/Order.cs
public class Order
{
    public string Id { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";
    public List<string> Items { get; set; } = new();
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
```

### Step 2: Operations

> Extend **`WorkflowOperationBase`**, not **`IWorkflowOperation`**, unless you have a reason. Use classes when the step will grow or needs tests.

```csharp
// Operations/ValidateOrderOperation.cs
using WorkflowForge;
using WorkflowForge.Operations;

public class ValidateOrderOperation : WorkflowOperationBase
{
    public override string Name => "ValidateOrder";

    protected override async Task<object?> ForgeAsyncCore(
        object? inputData,
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken)
    {
        // Get order from foundry properties (recommended pattern)
        var order = foundry.GetPropertyOrDefault<Order>("Order");
        
        if (order == null)
        {
            throw new InvalidOperationException("Order not found in foundry properties");
        }

        foundry.Logger.LogInformation("Validating order {OrderId}", order.Id);

        // Validation logic
        if (string.IsNullOrWhiteSpace(order.CustomerId))
        {
            throw new ArgumentException("Customer ID is required");
        }

        if (order.Amount <= 0)
        {
            throw new ArgumentException("Order amount must be greater than 0");
        }

        if (order.Items.Count == 0)
        {
            throw new ArgumentException("Order must contain at least one item");
        }

        foundry.Logger.LogInformation("Order validation successful");
        await Task.CompletedTask;
        return null;
    }
}
```

```csharp
// Operations/ProcessPaymentOperation.cs
using WorkflowForge;
using WorkflowForge.Operations;

public class ProcessPaymentOperation : WorkflowOperationBase
{
    public override string Name => "ProcessPayment";

    protected override async Task<object?> ForgeAsyncCore(
        object? inputData,
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken)
    {
        var order = foundry.GetPropertyOrDefault<Order>("Order");
        if (order == null)
        {
            throw new InvalidOperationException("Order not found");
        }

        foundry.Logger.LogInformation("Processing payment for order {OrderId}", order.Id);

        // Simulate payment processing
        await Task.Delay(100, cancellationToken); // Simulate API call

        var paymentResult = new PaymentResult
        {
            Success = true,
            TransactionId = Guid.NewGuid().ToString("N"),
            Message = "Payment processed successfully"
        };

        // Store result in foundry properties
        foundry.SetProperty("PaymentResult", paymentResult);
        
        foundry.Logger.LogInformation("Payment processed: {TransactionId}", paymentResult.TransactionId);
        return paymentResult;
    }

    public override async Task RestoreAsync(
        object? outputData,
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken)
    {
        // Compensation logic (rollback)
        var paymentResult = foundry.GetPropertyOrDefault<PaymentResult>("PaymentResult");
        
        if (paymentResult != null && paymentResult.Success)
        {
            foundry.Logger.LogWarning("Refunding payment {TransactionId}", paymentResult.TransactionId);
            
            // Simulate refund API call
            await Task.Delay(50, cancellationToken);
            
            foundry.Logger.LogInformation("Payment refunded successfully");
        }
    }
}
```

```csharp
// Operations/FulfillOrderOperation.cs
using WorkflowForge;
using WorkflowForge.Operations;

public class FulfillOrderOperation : WorkflowOperationBase
{
    public override string Name => "FulfillOrder";

    protected override async Task<object?> ForgeAsyncCore(
        object? inputData,
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken)
    {
        var order = foundry.GetPropertyOrDefault<Order>("Order");
        if (order == null)
        {
            throw new InvalidOperationException("Order not found");
        }

        foundry.Logger.LogInformation("Fulfilling order {OrderId}", order.Id);

        // Simulate fulfillment
        await Task.Delay(50, cancellationToken);

        order.Status = "Fulfilled";
        
        foundry.Logger.LogInformation("Order fulfilled successfully");
        return order;
    }
}
```

### Step 3: Wire and run

```csharp
// Program.cs
using WorkflowForge;
using WorkflowForge.Loggers;

// Create an order
var order = new Order
{
    Id = Guid.NewGuid().ToString("N"),
    CustomerId = "CUST-123",
    Amount = 99.99m,
    Items = new List<string> { "Product A", "Product B" }
};

Console.WriteLine($"Processing order {order.Id}...\n");

// Build the workflow
var workflow = WorkflowForge.CreateWorkflow("ProcessOrder")
    .WithDescription("Complete order processing workflow")
    .WithVersion("2.1.1")
    .AddOperation(new ValidateOrderOperation())
    .AddOperation(new ProcessPaymentOperation())
    .AddOperation(new FulfillOrderOperation())
    .Build();

// Create a foundry with the order data
var foundry = WorkflowForge.CreateFoundry(
    "ProcessOrder",
    initialProperties: new Dictionary<string, object?> { ["Order"] = order }
);

// Create a smith (orchestrator) with console logger
using var smith = WorkflowForge.CreateSmith(new ConsoleLogger());

try
{
    // Execute the workflow
    await smith.ForgeAsync(workflow, foundry);
    
    Console.WriteLine($"\nOrder processed successfully!");
    Console.WriteLine($"Final status: {order.Status}");
    
    var paymentResult = foundry.GetPropertyOrDefault<PaymentResult>("PaymentResult");
    if (paymentResult != null)
    {
        Console.WriteLine($"Transaction ID: {paymentResult.TransactionId}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"\nWorkflow failed: {ex.Message}");
}
```

### Step 4: Run

```bash
dotnet run
```

Sample console output:
```
Processing order abc123...

[INFO] Validating order abc123
[INFO] Order validation successful
[INFO] Processing payment for order abc123
[INFO] Payment processed: def456
[INFO] Fulfilling order abc123
[INFO] Order fulfilled successfully

Order processed successfully!
Final status: Fulfilled
Transaction ID: def456
```

## Core Concepts Explained

### The metaphor

Terms map to orchestration: forge builds graphs; foundry holds state; smith runs steps; each **operation** is one tool.

**Pieces**:
- **Forge**: Main factory for creating workflows
- **Foundry**: Execution environment with shared data
- **Smith**: Orchestrator that executes workflows
- **Operation**: Individual executable task

More detail: [Architecture Guide](../architecture/overview.md#core-metaphor).

### Data flow

**Primary**: `foundry.Properties`

```csharp
// Store data
foundry.SetProperty("Key", value);

// Retrieve data
var value = foundry.GetPropertyOrDefault<T>("Key");

// Retrieve with default
var value = foundry.GetPropertyOrDefault<T>("Key", defaultValue);
```

**Optional**: `WorkflowOperationBase<TInput, TOutput>` for fixed contracts

```csharp
public class MyOperation : WorkflowOperationBase<Order, OrderResult>
{
    public override string Name => "MyOperation";

    protected override async Task<OrderResult> ForgeAsyncCore(
        Order input,
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken)
    {
        // Typed input and output
        return new OrderResult { Success = true };
    }
}
```

Prefer the bag for most flows. Typed ops help when the contract is stable.

### Compensation

Implement **`RestoreAsync`** where undo matters; the base no-op is skipped safely on failure. Inline ops can pass **`restoreAction`**:

```csharp
var workflow = WorkflowForge.CreateWorkflow("OrderWorkflow")
    .AddOperation("ProcessPayment", async (foundry, ct) =>
    {
        foundry.Properties["payment_id"] = "PAY-123";
        // Process payment...
    },
    restoreAction: async (foundry, ct) =>
    {
        var paymentId = foundry.Properties["payment_id"];
        // Refund payment...
    })
    .Build();
```

Class-based steps: override `RestoreAsync`.

```csharp
public class MyOperation : WorkflowOperationBase
{
    protected override async Task<object?> ForgeAsyncCore(...) 
    {
        // Forward logic
    }

    public override async Task RestoreAsync(...) 
    {
        // Compensation/rollback logic
    }
}
```

Behavior flags:
```csharp
var options = new WorkflowForgeOptions
{
    Enabled = true,
    ContinueOnError = false,
    FailFastCompensation = false,
    ThrowOnCompensationError = false,
    EnableOutputChaining = true
};

var foundry = WorkflowForge.CreateFoundry("MyWorkflow", options: options);
```

## Next Steps

### Samples

Repo has 33 samples:

```bash
# Clone the repository
git clone https://github.com/animatlabs/workflow-forge.git
cd workflow-forge

# Run the samples
cd src/samples/WorkflowForge.Samples.BasicConsole
dotnet run
```

[Samples Guide](samples-guide.md)

### Parallel steps

`AddParallelOperations`:

```csharp
var workflow = WorkflowForge.CreateWorkflow("ParallelProcessing")
    .AddParallelOperations(
        new ValidateInventoryOperation(),
        new CheckFraudOperation(),
        new VerifyCustomerOperation()
    )
    .AddOperation(new ProcessOrderOperation())
    .Build();

// With concurrency control
var workflow2 = WorkflowForge.CreateWorkflow("ControlledParallel")
    .AddParallelOperations(
        operations,
        maxConcurrency: 4,
        timeout: TimeSpan.FromSeconds(30),
        name: "ParallelValidation"
    )
    .Build();
```

### Tests

`WorkflowForge.Testing` + `FakeWorkflowFoundry`:

```csharp
// Install testing package
dotnet add package WorkflowForge.Testing
```

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
        var operation = new ValidateOrderOperation();
        foundry.Properties["Order"] = new Order { Id = "123", Amount = 99.99m };
        
        // Act
        await operation.ForgeAsync(null, foundry, CancellationToken.None);
        
        // Assert - no exception means validation passed
    }
}
```

### Extensions

**Serilog**:
```csharp
using WorkflowForge.Extensions.Logging.Serilog;

var logger = SerilogLoggerFactory.CreateLogger(new SerilogLoggerOptions
{
    MinimumLevel = "Information",
    EnableConsoleSink = true
});
var smith = WorkflowForge.CreateSmith(logger);
```

**Polly**:
```csharp
using WorkflowForge.Extensions.Resilience.Polly;

// Retry, breaker, timeout together
foundry.UsePollyComprehensive(
    maxRetryAttempts: 3,
    circuitBreakerThreshold: 5,
    circuitBreakerDuration: TimeSpan.FromSeconds(30));

// Or one policy at a time
foundry.UsePollyRetry(maxRetryAttempts: 3);
foundry.UsePollyCircuitBreaker(failureThreshold: 5, durationOfBreak: TimeSpan.FromSeconds(30));
```

**Validation**:
```csharp
using WorkflowForge.Extensions.Validation;

foundry.UseValidation(
    f => f.GetPropertyOrDefault<Order>("Order"));
```

[Extensions Guide](../extensions/index.md)

### Read next

- **[Architecture Overview](../architecture/overview.md)** - Layout and patterns
- **[Operations Guide](../core/operations.md)** - Built-in and custom operations
- **[Event System](../core/events.md)** - Lifecycle events and monitoring
- **[Configuration](../core/configuration.md)** - Environment-specific settings
- **[API Reference](../reference/api-reference.md)** - Member-level reference

## Troubleshooting

### Common issues

**"Workflow name is required"**  
Use `.WithName()` or `CreateWorkflow(name)`.

**Nothing runs**  
Call `.Build()` and `await smith.ForgeAsync(...)`.

**Data missing between steps**  
Write with `foundry.SetProperty`, read with `foundry.GetPropertyOrDefault`.

**Compensation never does anything**  
Override `RestoreAsync` where rollback is real; on failure the smith walks completed ops in reverse and skips base no-ops.

### Getting Help

- **GitHub Issues**: https://github.com/animatlabs/workflow-forge/issues
- **Documentation**: https://github.com/animatlabs/workflow-forge/tree/main/docs
- **Samples**: https://github.com/animatlabs/workflow-forge/tree/main/src/samples

## Summary

You added packages, three `WorkflowOperationBase` steps, a smith, a foundry, and saw properties and `RestoreAsync`.

**Next**: [Samples catalog](samples-guide.md), project `WorkflowForge.Samples.BasicConsole`.

---

## Related Documentation

- [Architecture Overview](../architecture/overview.md) - Metaphor and layout
- [Operations Guide](../core/operations.md) - Built-in ops and patterns
- [Events System](../core/events.md) - Lifecycle hooks
- [Extensions](../extensions/index.md) - Optional packages
- [Configuration](../core/configuration.md) - Options and appsettings
- [API Reference](../reference/api-reference.md) - Member listings
