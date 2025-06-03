# Getting Started with WorkflowForge

Welcome to WorkflowForge! This guide will walk you through installing, configuring, and creating your first workflow in just a few minutes.

## Prerequisites

Before you begin, ensure you have:

- **.NET 8.0 SDK** or later ([Download](https://dotnet.microsoft.com/download))
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

```csharp
// Operations/ValidateOrderOperation.cs
using WorkflowForge;

public class ValidateOrderOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "ValidateOrder";
    public bool SupportsRestore => false; // Validation doesn't need rollback

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var order = (Order)inputData!;
        
        foundry.Logger.LogInformation("Validating order {OrderId} for customer {CustomerId}", 
            order.Id, order.CustomerId);

        // Simulate validation logic
        await Task.Delay(100, cancellationToken);

        if (string.IsNullOrEmpty(order.CustomerId))
            throw new InvalidOperationException("Customer ID is required");

        if (order.Amount <= 0)
            throw new InvalidOperationException("Order amount must be positive");

        if (!order.Items.Any())
            throw new InvalidOperationException("Order must contain at least one item");

        foundry.Logger.LogInformation("Order {OrderId} validation completed successfully", order.Id);
        
        // Update order status
        order.Status = "Validated";
        return order;
    }

    public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        // No restoration needed for validation
        return Task.CompletedTask;
    }
}
```

```csharp
// Operations/ProcessPaymentOperation.cs
public class ProcessPaymentOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "ProcessPayment";
    public bool SupportsRestore => true; // Payment processing supports refunds

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var order = (Order)inputData!;
        
        foundry.Logger.LogInformation("Processing payment for order {OrderId}, amount: {Amount:C}", 
            order.Id, order.Amount);

        // Simulate payment processing
        await Task.Delay(500, cancellationToken);

        var paymentResult = new PaymentResult
        {
            Success = true,
            TransactionId = $"TXN_{Guid.NewGuid():N}",
            Message = "Payment processed successfully"
        };

        // Store transaction ID for potential refund
        foundry.SetProperty("TransactionId", paymentResult.TransactionId);
        
        foundry.Logger.LogInformation("Payment processed successfully for order {OrderId}, transaction: {TransactionId}", 
            order.Id, paymentResult.TransactionId);

        order.Status = "Paid";
        return order;
    }

    public async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var transactionId = foundry.GetProperty<string>("TransactionId");
        
        if (!string.IsNullOrEmpty(transactionId))
        {
            foundry.Logger.LogWarning("Refunding payment for transaction {TransactionId}", transactionId);
            
            // Simulate refund process
            await Task.Delay(200, cancellationToken);
            
            foundry.Logger.LogInformation("Payment refunded successfully for transaction {TransactionId}", transactionId);
        }
    }
}
```

### Step 3: Build and Execute the Workflow

```csharp
// Program.cs
using WorkflowForge;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("WorkflowForge Getting Started Example");
        Console.WriteLine("=====================================");

        // Create a sample order
        var order = new Order
        {
            Id = "ORD-001",
            CustomerId = "CUST-123",
            Amount = 99.99m,
            Items = new List<string> { "Product A", "Product B" }
        };

        // Build the workflow
        var workflow = WorkflowForge.CreateWorkflow()
            .WithName("OrderProcessing")
            .AddOperation(new ValidateOrderOperation())
            .AddOperation(new ProcessPaymentOperation())
            .AddOperation("FulfillOrder", async (input, foundry, ct) =>
            {
                var processedOrder = (Order)input!;
                foundry.Logger.LogInformation("Fulfilling order {OrderId}", processedOrder.Id);
                
                // Simulate fulfillment
                await Task.Delay(300, ct);
                
                processedOrder.Status = "Fulfilled";
                foundry.Logger.LogInformation("Order {OrderId} fulfilled successfully", processedOrder.Id);
                
                return processedOrder;
            })
            .Build();

        // Execute the workflow
        using var foundry = WorkflowForge.CreateFoundry("ProcessOrder");
        using var smith = WorkflowForge.CreateSmith();

        // Set initial data in foundry
        foundry.Properties["order"] = order;

        await smith.ForgeAsync(workflow, foundry);
    }
}
```

### Step 4: Run Your First Workflow

```bash
dotnet run
```

**Expected Output:**
```
WorkflowForge Getting Started Example
=====================================
Processing order: ORD-001
Initial status: Pending

[INFO] Validating order ORD-001 for customer CUST-123
[INFO] Order ORD-001 validation completed successfully
[INFO] Processing payment for order ORD-001, amount: $99.99
[INFO] Payment processed successfully for order ORD-001, transaction: TXN_1234567890abcdef
[INFO] Fulfilling order ORD-001
[INFO] Order ORD-001 fulfilled successfully

Workflow completed successfully!
Final status: Fulfilled
Transaction stored: TXN_1234567890abcdef
```

## Understanding the Flow

### 1. Workflow Creation
```csharp
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("OrderProcessing")           // Give the workflow a name
    .AddOperation(new ValidateOrderOperation())  // Add custom operation
    .AddOperation(new ProcessPaymentOperation()) // Add another operation
    .AddOperation("FulfillOrder", async ...)    // Add inline operation
    .Build();                              // Build immutable workflow
```

### 2. Foundry Setup
```csharp
var foundry = WorkflowForge.CreateFoundry("OrderProcessing");
```
The foundry provides:
- **Logging**: Built-in logging abstraction
- **Properties**: Shared state across operations
- **Services**: Dependency injection support

### 3. Execution
```csharp
var smith = WorkflowForge.CreateSmith();
foundry.Properties["order"] = order; // Set order data in foundry
await smith.ForgeAsync(workflow, foundry);
```
The smith:
- Executes operations in sequence
- Passes data between operations
- Handles errors and compensation automatically

### 4. Error Handling & Compensation
If any operation fails, WorkflowForge automatically:
1. Stops execution
2. Calls `RestoreAsync` on completed operations (in reverse order)
3. Propagates the exception

## Adding Extensions

Enhance your workflow with powerful extensions:

### Structured Logging with Serilog

```csharp
// Install: dotnet add package WorkflowForge.Extensions.Logging.Serilog

using Serilog;
using WorkflowForge.Extensions.Logging.Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/workflow-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Create foundry with Serilog
var foundry = WorkflowForge.CreateFoundry("OrderProcessing")
    .UseSerilog(Log.Logger);
```

### Resilience with Polly

```csharp
// Install: dotnet add package WorkflowForge.Extensions.Resilience.Polly

using WorkflowForge.Extensions.Resilience.Polly;

// Create resilient foundry
var foundry = WorkflowForge.CreateFoundry("OrderProcessing")
    .UsePollyResilience(); // Adds retry, circuit breaker, timeout
```

### Performance Monitoring

```csharp
// Install: dotnet add package WorkflowForge.Extensions.Observability.Performance

using WorkflowForge.Extensions.Observability.Performance;

// Enable performance monitoring
var foundry = WorkflowForge.CreateFoundry("OrderProcessing")
    .EnablePerformanceMonitoring();

// After execution, get statistics
var stats = foundry.GetPerformanceStatistics();
Console.WriteLine($"Total operations: {stats.TotalOperations}");
Console.WriteLine($"Success rate: {stats.SuccessRate:P2}");
Console.WriteLine($"Average duration: {stats.AverageDuration.TotalMilliseconds:F2}ms");
```

## Testing Your Workflow

WorkflowForge is designed for testability:

```csharp
// Tests/OrderWorkflowTests.cs
using Xunit;
using Moq;
using WorkflowForge;

public class OrderWorkflowTests
{
    [Fact]
    public async Task Should_Process_Valid_Order_Successfully()
    {
        // Arrange
        var order = new Order
        {
            Id = "TEST-001",
            CustomerId = "TEST-CUSTOMER",
            Amount = 50.00m,
            Items = new List<string> { "Test Item" }
        };

        var workflow = WorkflowForge.CreateWorkflow()
            .WithName("TestOrderProcessing")
            .AddOperation(new ValidateOrderOperation())
            .AddOperation(new ProcessPaymentOperation())
            .Build();

        var foundry = WorkflowForge.CreateFoundry("Test");
        var smith = WorkflowForge.CreateSmith();

        // Act
        foundry.Properties["order"] = order; // Set order data in foundry
        await smith.ForgeAsync(workflow, foundry);

        // Assert
        var processedOrder = foundry.Properties["processedOrder"] as Order;
        Assert.Equal("Paid", processedOrder?.Status);
        Assert.NotNull(foundry.Properties["TransactionId"]);
    }

    [Fact]
    public async Task Should_Compensate_On_Failure()
    {
        // Arrange
        var invalidOrder = new Order { Id = "INVALID" }; // Missing required fields

        var workflow = WorkflowForge.CreateWorkflow()
            .WithName("TestFailure")
            .AddOperation(new ValidateOrderOperation())
            .Build();

        var foundry = WorkflowForge.CreateFoundry("Test");
        var smith = WorkflowForge.CreateSmith();

        // Act & Assert
        foundry.Properties["order"] = invalidOrder; // Set invalid order data in foundry
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => smith.ForgeAsync(workflow, foundry));
    }
}
```

## Next Steps

Congratulations! You've created your first WorkflowForge workflow. Here's what to explore next:

### 1. **Learn Core Concepts**
- **[Architecture Overview](architecture.md)** - Understand the design principles
- **[Workflow Concepts](concepts.md)** - Deep dive into workflows and operations

### 2. **Explore Advanced Features**
- **[Building Operations](operations.md)** - Create sophisticated custom operations
- **[Middleware Development](middleware.md)** - Add cross-cutting concerns
- **[Error Handling & Compensation](error-handling.md)** - Robust error management

### 3. **Add Enterprise Features**
- **[Extensions Guide](extensions.md)** - Logging, resilience, observability
- **[Configuration](configuration.md)** - Environment-specific settings
- **[Performance Optimization](performance.md)** - High-performance patterns

### 4. **Run Sample Applications**
- **[Interactive Samples](../src/samples/WorkflowForge.Samples.BasicConsole/README.md)** - Comprehensive examples
- **[Performance Benchmarks](../src/benchmarks/README.md)** - See performance characteristics

### 5. **Advanced Deployment**
- **[Advanced Patterns](advanced-patterns.md)** - Advanced implementation patterns
- **[Security Considerations](security.md)** - Security best practices
- **[Monitoring & Observability](extensions/observability.md)** - Advanced monitoring

## Common Patterns

### Configuration-Based Workflows
```csharp
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("ConfigurableWorkflow")
    .AddOperation("Step1", async (input, foundry, ct) =>
    {
        var config = foundry.GetService<IConfiguration>();
        var setting = config["MyApp:ProcessingMode"];
        // Use configuration in operation
        return ProcessWithMode(input, setting);
    })
    .Build();
```

### Conditional Workflows
```csharp
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("ConditionalProcessing")
    .AddOperation("CheckCondition", async (input, foundry, ct) =>
    {
        var order = (Order)input!;
        return order.Amount > 100 ? "HighValue" : "StandardValue";
    })
    .AddOperation("ProcessHighValue", async (input, foundry, ct) =>
    {
        if (foundry.GetProperty<string>("PreviousResult") == "HighValue")
        {
            // High value processing
        }
        return input;
    })
    .Build();
```

### Parallel Processing
```csharp
// Note: Built-in parallel operations coming in future releases
// For now, use Task.WhenAll within operations for parallel work
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("ParallelProcessing")
    .AddOperation("ParallelTasks", async (input, foundry, ct) =>
    {
        var tasks = new[]
        {
            ProcessTaskA(input, foundry, ct),
            ProcessTaskB(input, foundry, ct),
            ProcessTaskC(input, foundry, ct)
        };
        
        var results = await Task.WhenAll(tasks);
        return AggregateResults(results);
    })
    .Build();
```

## Troubleshooting

### Common Issues

**1. "Operation failed but no compensation occurred"**
- Ensure your operations implement `SupportsRestore = true`
- Check that `RestoreAsync` is properly implemented

**2. "Foundry properties not persisting between operations"**
- Use `foundry.SetProperty()` to store data
- Ensure the foundry instance is reused across operations

**3. "Performance is slower than expected"**
- Check if you're running in Debug mode (use Release for benchmarks)
- Consider adding performance monitoring extension
- Review operation implementations for blocking calls

### Getting Help

- **Documentation**: Explore the [full documentation](README.md)
- **Samples**: Run the [interactive samples](../src/samples/WorkflowForge.Samples.BasicConsole/README.md)
- **Issues**: Report bugs on [GitHub Issues](../issues)
- **Discussions**: Ask questions in [GitHub Discussions](../discussions)

---

**Welcome to WorkflowForge!** - *Start building robust workflows today* 