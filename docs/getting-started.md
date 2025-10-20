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
        if (foundry.TryGetProperty<string>("TransactionId", out var transactionId) && !string.IsNullOrEmpty(transactionId))
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
        foundry.SetProperty("order", order);

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
foundry.SetProperty("order", order); // Set order data in foundry
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
var config = FoundryConfiguration.ForProduction().UseSerilog(Log.Logger);
var foundry = WorkflowForge.CreateFoundry("OrderProcessing", config);
```

### Resilience with Polly

```csharp
// Install: dotnet add package WorkflowForge.Extensions.Resilience.Polly

using WorkflowForge.Extensions.Resilience.Polly;

// Apply Polly resilience to foundry (extension methods are on IWorkflowFoundry)
var foundry = WorkflowForge.CreateFoundry("OrderProcessing", FoundryConfiguration.ForProduction());
foundry.UsePollyProductionResilience(); // Retry, circuit breaker, timeout from settings
```

### Persistence + Recovery Quickstart

```bash
# Install persistence + recovery extensions
dotnet add package WorkflowForge.Extensions.Persistence
dotnet add package WorkflowForge.Extensions.Persistence.Recovery
```

```csharp
using WorkflowForge.Extensions; // UsePersistence
using WorkflowForge.Extensions.Persistence; // PersistenceOptions
using WorkflowForge.Extensions.Persistence.Abstractions; // IWorkflowPersistenceProvider
using WorkflowForge.Extensions.Persistence.Recovery; // ForgeWithRecoveryAsync

// Implement your provider (DB/file/etc.). See sample FilePersistenceProvider in samples.
public sealed class DemoInMemoryProvider : IWorkflowPersistenceProvider
{
    private static readonly ConcurrentDictionary<(Guid, Guid), WorkflowExecutionSnapshot> Store = new();
    public Task SaveAsync(WorkflowExecutionSnapshot snapshot, CancellationToken ct = default)
    {
        Store[(snapshot.FoundryExecutionId, snapshot.WorkflowId)] = snapshot;
        return Task.CompletedTask;
    }
    public Task<WorkflowExecutionSnapshot?> TryLoadAsync(Guid foundryExecutionId, Guid workflowId, CancellationToken ct = default)
    {
        Store.TryGetValue((foundryExecutionId, workflowId), out var s);
        return Task.FromResult<WorkflowExecutionSnapshot?>(s);
    }
    public Task DeleteAsync(Guid foundryExecutionId, Guid workflowId, CancellationToken ct = default)
    {
        Store.TryRemove((foundryExecutionId, workflowId), out _);
        return Task.CompletedTask;
    }
}

// Enable persistence with stable keys and run with recovery
var provider = new DemoInMemoryProvider();
var options = new PersistenceOptions
{
    InstanceId = "order-service-west-1",
    WorkflowKey = "ProcessOrder-v1"
};

using var foundry = WorkflowForge.CreateFoundry("OrderProcessing");
foundry.UsePersistence(provider, options);

// On startup, the smith can resume or start fresh using recovery:
using var smith = WorkflowForge.CreateSmith();
await smith.ForgeWithRecoveryAsync(workflow, foundry, CancellationToken.None);

// Note: For cross-process resume, use a shared provider (e.g., DB or file). See
// `src/samples/WorkflowForge.Samples.BasicConsole/Samples/FilePersistenceProvider.cs` for a file-based demo.
```

### Logging-only Quickstart (no external logger)

```csharp
// Use core logging middleware with the foundry's logger
using WorkflowForge.Extensions; // UseLogging

using var foundry = WorkflowForge.CreateFoundry("OrderProcessing");
foundry.UseLogging();

// Optionally provide your own IWorkflowForgeLogger implementation
// foundry.UseLogging(myLogger);
```

### Performance Monitoring

```csharp
// Install: dotnet add package WorkflowForge.Extensions.Observability.Performance

using WorkflowForge.Extensions.Observability.Performance;

// Enable performance monitoring
var foundry = WorkflowForge.CreateFoundry("OrderProcessing");
foundry.EnablePerformanceMonitoring();

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
        foundry.SetProperty("order", order); // Set order data in foundry
        await smith.ForgeAsync(workflow, foundry);

        // Assert
        var processedOrder = foundry.GetPropertyOrDefault<Order>("processedOrder");
        Assert.Equal("Paid", processedOrder?.Status);
        Assert.True(foundry.TryGetProperty<string>("TransactionId", out _));
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
        foundry.SetProperty("order", invalidOrder); // Set invalid order data in foundry
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
- Middleware development - See examples in samples and `architecture.md`
- Error handling & compensation - Covered throughout `operations.md` and samples

### 3. **Add Enterprise Features**
- **[Extensions Guide](extensions.md)** - Logging, resilience, observability
- **[Configuration](configuration.md)** - Environment-specific settings
- **[Performance Optimization](performance.md)** - High-performance patterns

### 4. **Run Sample Applications**
- **[Interactive Samples](../src/samples/WorkflowForge.Samples.BasicConsole/README.md)** - Comprehensive examples
- Quickstarts in samples (menu numbers): Persistence (18), Recovery Only (21), Recovery + Resilience (22)
- **[Performance Benchmarks](../src/benchmarks/README.md)** - See performance characteristics

### 5. **Advanced Deployment**
- Advanced patterns - To be documented in future versions
- Security considerations - Follow standard .NET security practices
- Monitoring & Observability - See `docs/extensions.md` and extension READMEs

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
        if (foundry.TryGetProperty<string>("PreviousResult", out var prev) && prev == "HighValue")
        {
            // High value processing
        }
        return input;
    })
    .Build();
```

### Parallel Processing
```csharp
// Use ForEachWorkflowOperation for parallel execution of collections
foundry.SetProperty("items", new[] { "Item1", "Item2", "Item3" });

var parallelOp = ForEachWorkflowOperation.Create(
    WorkflowOperations.CreateAsync("ProcessItem", async item =>
    {
        // Each item processed in parallel
        return await ProcessItemAsync(item);
    })
);

var workflow = WorkflowForge.CreateWorkflow()
    .WithName("ParallelProcessing")
    .AddOperation(parallelOp)
    .Build();

// Or use Task.WhenAll within operations for ad-hoc parallel work
var workflow2 = WorkflowForge.CreateWorkflow()
    .WithName("AdHocParallel")
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

## Data Flow Patterns

### Core Principle: Explicit Data Flow

All workflow data should live in `foundry.Properties` (thread-safe dictionary). Direct object passing between operations creates implicit dependencies and maintenance issues.

### Pattern: Shared Foundry Properties (Recommended)

```csharp
public class ValidateOrderOperation : IWorkflowOperation
{
    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken ct)
    {
        // Read from properties
        var orderId = foundry.GetPropertyOrDefault<int>("order_id");
        var total = foundry.GetPropertyOrDefault<decimal>("order_total");
        
        // Validate
        if (total <= 0)
            throw new InvalidOperationException("Order total must be positive");
        
        // Write to properties
        foundry.SetProperty("order_validated", true);
        foundry.SetProperty("validation_timestamp", DateTime.UtcNow);
        
        return "Validation complete";
    }
}

public class ProcessPaymentOperation : IWorkflowOperation
{
    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken ct)
    {
        // Read results from previous operation
        var validated = foundry.GetPropertyOrDefault<bool>("order_validated");
        if (!validated)
            throw new InvalidOperationException("Order not validated");
        
        var total = foundry.GetPropertyOrDefault<decimal>("order_total");
        
        // Process payment
        var paymentId = await ProcessPaymentAsync(total);
        
        // Write results
        foundry.SetProperty("payment_id", paymentId);
        foundry.SetProperty("payment_processed", true);
        
        return "Payment processed";
    }
}

// Usage
var foundry = smith.CreateFoundry();
foundry.SetProperty("order_id", 123);
foundry.SetProperty("order_total", 99.99m);

var workflow = WorkflowForge.CreateWorkflow()
    .WithName("OrderProcessing")
    .AddOperation(new ValidateOrderOperation())
    .AddOperation(new ProcessPaymentOperation())
    .Build();

await smith.ForgeAsync(workflow, foundry);

// Access final results
var paymentId = foundry.GetPropertyOrDefault<string>("payment_id");
```

### Best Practices

1. Always document properties your operation uses
2. Use `GetPropertyOrDefault<T>(key, defaultValue)` for safe access
3. Implement `RestoreAsync` to clean up properties if operation fails
4. Use consistent property naming conventions

---

## Troubleshooting

### Common Issues

**1. "Operation failed but no compensation occurred"**
- Ensure your operations implement `SupportsRestore = true`
- Check that `RestoreAsync` is properly implemented

**2. "Foundry properties not persisting between operations"**
- Use `foundry.SetProperty("key", value);` to store data
- Ensure the foundry instance is reused across operations

**3. "Performance is slower than expected"**
- Check if you're running in Debug mode (use Release for benchmarks)
- Consider adding performance monitoring extension
- Review operation implementations for blocking calls

### Getting Help

- **Documentation**: Explore the [full documentation](README.md)
- **Samples**: Run the [interactive samples](../src/samples/WorkflowForge.Samples.BasicConsole/README.md)
- **Issues**: Report bugs on GitHub Issues: https://github.com/animatlabs/workflow-forge/issues
- **Discussions**: Ask questions in GitHub Discussions: https://github.com/animatlabs/workflow-forge/discussions

---

**Welcome to WorkflowForge!** - *Start building robust workflows today* 