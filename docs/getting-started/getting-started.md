# Getting Started with WorkflowForge

<p align="center">
  <img src="https://raw.githubusercontent.com/animatlabs/workflow-forge/main/icon.png" alt="WorkflowForge" width="120" height="120">
</p>

Welcome to WorkflowForge! This guide will walk you through installing, configuring, and creating your first workflow in just a few minutes.

## Table of Contents

- [What's New in 2.0.0](#whats-new-in-200)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Your First Workflow](#your-first-workflow)
- [Core Concepts Explained](#core-concepts-explained)
- [Next Steps](#next-steps)
- [Troubleshooting](#troubleshooting)

---

## What's New in 2.0.0

**WorkflowForge 2.0.0** introduces major improvements:

### Dependency Isolation
Extensions internalize third-party dependencies with **ILRepack** where appropriate, while keeping Microsoft/System assemblies external for runtime unification.

### New Extensions
- **Validation**: DataAnnotations-based validation for comprehensive input rules
- **Audit**: Production-ready audit logging with pluggable providers

### Breaking Changes
- **Event System**: Refactored from single `IWorkflowEvents` to three focused interfaces (`IWorkflowLifecycleEvents`, `IOperationLifecycleEvents`, `ICompensationLifecycleEvents`) for SRP compliance. See [Events Guide](../core/events.md) for migration.
- **ISystemTimeProvider**: Now injected via DI instead of static instance

### Enhancements
- Comprehensive test suite (>90% coverage)
- Improved documentation and samples
- Better testability with DI throughout

## Prerequisites

Before you begin, ensure you have:

- **.NET SDK**: .NET 6.0+ (WorkflowForge targets .NET Standard 2.0)
- **IDE**: Visual Studio 2022, VS Code, or JetBrains Rider
- **Basic C# knowledge**: Understanding of async/await patterns

## Installation

### Step 1: Create a New Project

```bash
# Create a new console application
dotnet new console -n MyWorkflowApp
cd MyWorkflowApp
```

### Step 2: Install WorkflowForge

```bash
# Install the core package
dotnet add package WorkflowForge

# Optional: Install extensions for enhanced capabilities
dotnet add package WorkflowForge.Extensions.Logging.Serilog
dotnet add package WorkflowForge.Extensions.Resilience.Polly
dotnet add package WorkflowForge.Extensions.Validation
dotnet add package WorkflowForge.Extensions.Audit
```

### Step 3: Verify Installation

```bash
# Build to ensure everything is installed correctly
dotnet build
```

## Your First Workflow

Let's create a simple order processing workflow to demonstrate core concepts.

### Step 1: Define the Domain Model

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

### Step 2: Create Custom Operations

**Best Practice**: Use class-based operations for production scenarios (better performance, testability, and maintainability).

```csharp
// Operations/ValidateOrderOperation.cs
using WorkflowForge;
using WorkflowForge.Operations;

public class ValidateOrderOperation : WorkflowOperationBase
{
    public override string Name => "ValidateOrder";
    public override bool SupportsRestore => false; // Validation doesn't need rollback

    public override async Task<object?> ForgeAsync(
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

        foundry.Logger.LogInformation($"Validating order {order.Id}");

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
    public override bool SupportsRestore => true; // Payment can be refunded

    public override async Task<object?> ForgeAsync(
        object? inputData,
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken)
    {
        var order = foundry.GetPropertyOrDefault<Order>("Order");
        if (order == null)
        {
            throw new InvalidOperationException("Order not found");
        }

        foundry.Logger.LogInformation($"Processing payment for order {order.Id}");

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
        
        foundry.Logger.LogInformation($"Payment processed: {paymentResult.TransactionId}");
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
            foundry.Logger.LogWarning($"Refunding payment {paymentResult.TransactionId}");
            
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
    public override bool SupportsRestore => false;

    public override async Task<object?> ForgeAsync(
        object? inputData,
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken)
    {
        var order = foundry.GetPropertyOrDefault<Order>("Order");
        if (order == null)
        {
            throw new InvalidOperationException("Order not found");
        }

        foundry.Logger.LogInformation($"Fulfilling order {order.Id}");

        // Simulate fulfillment
        await Task.Delay(50, cancellationToken);

        order.Status = "Fulfilled";
        
        foundry.Logger.LogInformation("Order fulfilled successfully");
        return order;
    }
}
```

### Step 3: Build the Workflow

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
    .WithVersion("2.0.0")
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

### Step 4: Run the Application

```bash
dotnet run
```

**Expected Output**:
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

### The Metaphor

WorkflowForge uses an industrial manufacturing metaphor where workflows are "forged" through operations.

**Core Components**:
- **Forge**: Main factory for creating workflows
- **Foundry**: Execution environment with shared data
- **Smith**: Orchestrator that executes workflows
- **Operation**: Individual executable task

For detailed explanation of the metaphor and architecture, see [Architecture Guide](../architecture/overview.md#core-metaphor).

### Data Flow Pattern

**PRIMARY**: Dictionary-based via `foundry.Properties`

```csharp
// Store data
foundry.SetProperty("Key", value);

// Retrieve data
var value = foundry.GetPropertyOrDefault<T>("Key");

// Retrieve with default
var value = foundry.GetPropertyOrDefault<T>("Key", defaultValue);
```

**SECONDARY**: Type-safe via `IWorkflowOperation<TInput, TOutput>` (for explicit input/output contracts)

```csharp
public class MyOperation : IWorkflowOperation<Order, OrderResult>
{
    public async Task<OrderResult> ForgeAsync(
        Order input,
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken)
    {
        // Typed input and output
        return new OrderResult { Success = true };
    }
}
```

**Recommendation**: Use dictionary-based data flow for most scenarios. Type-safe operations are useful when you need explicit compile-time contracts.

### Compensation (Saga Pattern)

WorkflowForge supports automatic rollback on failure:

```csharp
public class MyOperation : WorkflowOperationBase
{
    public override bool SupportsRestore => true;

    public override async Task<object?> ForgeAsync(...) 
    {
        // Forward logic
    }

    public override async Task RestoreAsync(...) 
    {
        // Compensation/rollback logic
    }
}
```

Configure execution behavior via options:
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

### Explore Samples

WorkflowForge includes 33 comprehensive samples covering all features:

```bash
# Clone the repository
git clone https://github.com/animatlabs/workflow-forge.git
cd workflow-forge

# Run the samples
cd src/samples/WorkflowForge.Samples.BasicConsole
dotnet run
```

[Samples Guide](samples-guide.md)

### Add Extensions

Enhance your workflows with extensions:

**Structured Logging (Serilog)**:
```csharp
using WorkflowForge.Extensions.Logging.Serilog;

var logger = SerilogLoggerFactory.CreateLogger(new SerilogLoggerOptions
{
    MinimumLevel = "Information",
    EnableConsoleSink = true
});
var smith = WorkflowForge.CreateSmith(logger);
```

**Resilience (Polly)**:
```csharp
using WorkflowForge.Extensions.Resilience.Polly;

// Comprehensive policy with retry, circuit breaker, and timeout
foundry.UsePollyComprehensive(
    maxRetryAttempts: 3,
    circuitBreakerThreshold: 5,
    circuitBreakerDuration: TimeSpan.FromSeconds(30));

// Or use individual policies
foundry.UsePollyRetry(maxRetryAttempts: 3);
foundry.UsePollyCircuitBreaker(failureThreshold: 5, durationOfBreak: TimeSpan.FromSeconds(30));
```

**Validation (DataAnnotations)**:
```csharp
using WorkflowForge.Extensions.Validation;

foundry.UseValidation(
    f => f.GetPropertyOrDefault<Order>("Order"));
```

[Extensions Guide](../extensions/index.md)

### Learn Advanced Patterns

- **[Architecture Overview](../architecture/overview.md)** - Design patterns and principles
- **[Operations Guide](../core/operations.md)** - Built-in and custom operations
- **[Event System](../core/events.md)** - Lifecycle events and monitoring
- **[Configuration](../core/configuration.md)** - Environment-specific settings
- **[API Reference](../reference/api-reference.md)** - Complete API documentation

## Troubleshooting

### Common Issues

**Issue**: "Workflow name is required" exception
**Solution**: Always set a workflow name via `.WithName()` or `CreateWorkflow(name)`.

**Issue**: Operations not executing
**Solution**: Verify you called `.Build()` on the workflow builder and `await smith.ForgeAsync()`.

**Issue**: Data not passing between operations
**Solution**: Use `foundry.SetProperty()` to store and `foundry.GetPropertyOrDefault()` to retrieve data.

**Issue**: Compensation not running
**Solution**: Set `SupportsRestore = true` on your operation. Compensation runs automatically when a workflow fails and operations with `SupportsRestore = true` have been executed.

### Getting Help

- **GitHub Issues**: https://github.com/animatlabs/workflow-forge/issues
- **Documentation**: https://github.com/animatlabs/workflow-forge/tree/main/docs
- **Samples**: https://github.com/animatlabs/workflow-forge/tree/main/src/samples

## Summary

You've learned:

- How to install WorkflowForge
- How to create custom operations (class-based, recommended)
- How to build and execute workflows
- Core concepts (Forge, Workflow, Operation, Foundry, Smith)
- Data flow patterns (dictionary-based preferred)
- Compensation/rollback (Saga pattern)

**Next**: Explore the [33 samples](samples-guide.md) to see WorkflowForge in action, or dive into [architecture](../architecture/overview.md) to understand the design principles.

---

## Related Documentation

- **[Architecture Overview](../architecture/overview.md)** - Design patterns and core concepts
- **[Operations Guide](../core/operations.md)** - All operation types and patterns
- **[Events System](../core/events.md)** - Monitoring and observability
- **[Extensions](../extensions/index.md)** - Available extensions
- **[Configuration](../core/configuration.md)** - Environment-specific setup
- **[Samples Guide](samples-guide.md)** - All 33 samples with learning path
- **[API Reference](../reference/api-reference.md)** - Complete API documentation

**← Back to [Documentation Home](../index.md)**
