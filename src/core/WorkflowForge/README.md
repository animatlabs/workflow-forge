# WorkflowForge Core

<p align="center">
  <img src="https://raw.githubusercontent.com/animatlabs/workflow-forge/main/icon.png" alt="WorkflowForge" width="120" height="120">
</p>

**Zero-dependency workflow orchestration framework for .NET**

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.svg)](https://www.nuget.org/packages/WorkflowForge/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/animatlabs/workflow-forge/blob/main/LICENSE)

## Overview

WorkflowForge Core is the foundational workflow orchestration library providing industrial-strength workflow capabilities with **zero external dependencies**. Built on the forge/foundry/smith metaphor, it delivers microsecond-level performance, built-in compensation (Saga pattern), and a flexible middleware pipeline.

### Key Features

- **Zero Dependencies**: Absolutely no external NuGet packages required
- **Microsecond Performance**: Sub-20μs operation execution in high-performance scenarios
- **Saga Pattern**: Built-in compensation/rollback support via `RestoreAsync`
- **Middleware Pipeline**: Extensible Russian Doll pattern for cross-cutting concerns
- **Dictionary-Based Data Flow**: Thread-safe `ConcurrentDictionary` for shared context
- **Type-Safe Operations**: Optional `IWorkflowOperation<TInput, TOutput>` for explicit data contracts
- **Configurable Output Chaining**: Pass operation output to next input via `WorkflowForgeOptions.EnableOutputChaining`
- **Event System**: SRP-compliant lifecycle events (Workflow, Operation, Compensation)
- **Builder Pattern**: Fluent API for workflow construction
- **.NET Standard 2.0**: Compatible with .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+

## Installation

```bash
dotnet add package WorkflowForge
```

**Requirements**: .NET Standard 2.0 or later

## Quick Start

### 1. Create Your First Workflow

```csharp
using WorkflowForge;
using WorkflowForge.Extensions;

// Create workflow
var workflow = WorkflowForge.CreateWorkflow("OrderProcessing")
    .AddOperation("ValidateOrder", new ValidateOrderOperation())
    .AddOperation("ChargePayment", new ChargePaymentOperation())
    .AddOperation("ReserveInventory", new ReserveInventoryOperation())
    .AddOperation("CreateShipment", new CreateShipmentOperation())
    .Build();

// Create execution environment
using var foundry = WorkflowForge.CreateFoundry("Order-12345");
foundry.SetProperty("OrderId", "12345");
foundry.SetProperty("CustomerId", "CUST-001");

// Execute workflow
using var smith = WorkflowForge.CreateSmith();
await smith.ForgeAsync(workflow, foundry);

// Read results
var shipmentId = foundry.GetPropertyOrDefault<string>("ShipmentId");
```

### 2. Inline Operations (Quick Prototyping)

```csharp
var workflow = WorkflowForge.CreateWorkflow("QuickDemo")
    .AddOperation("Step1", async (foundry, ct) =>
    {
        foundry.Logger.LogInformation("Executing Step 1");
        foundry.SetProperty("Result", "Success");
        await Task.CompletedTask;
    })
    .AddOperation("Step2", async (foundry, ct) =>
    {
        var result = foundry.GetPropertyOrDefault<string>("Result");
        foundry.Logger.LogInformation("Step 2 received: {Result}", result);
        await Task.CompletedTask;
    })
    .Build();

using var smith = WorkflowForge.CreateSmith();
using var foundry = WorkflowForge.CreateFoundry("QuickDemo");
await smith.ForgeAsync(workflow, foundry);
```

## Architecture

### The Industrial Metaphor

WorkflowForge uses an industrial manufacturing metaphor:

- **Forge** (`WorkflowForge` static class): Main factory for creating workflows and components
- **Foundry** (`IWorkflowFoundry`): Execution environment with shared data/context
- **Smith** (`IWorkflowSmith`): Orchestrator that executes workflows through foundries
- **Operation** (`IWorkflowOperation`): Individual executable task

**Data Flow**: All workflow data lives in `foundry.Properties` (ConcurrentDictionary) by default. Use type-safe operations (`IWorkflowOperation<TInput, TOutput>`) only when explicit contracts are needed.

### Core Abstractions

#### IWorkflowFoundry - Execution Context

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

#### IWorkflowSmith - Orchestration Engine

```csharp
public interface IWorkflowSmith : IDisposable,
    IWorkflowLifecycleEvents,
    ICompensationLifecycleEvents
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

#### IWorkflowOperation - Executable Task

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

## Built-in Operations

### 1. DelegateWorkflowOperation

```csharp
var workflow = WorkflowForge.CreateWorkflow("DelegateExample")
    .AddOperation("Process", async (foundry, ct) => 
    {
        var input = foundry.GetPropertyOrDefault<string>("Input");
        foundry.Logger.LogInformation("Processing: {Input}", input);
        await Task.Delay(100, ct);
        foundry.SetProperty("Output", $"Processed: {input}");
    })
    .Build();
```

### 2. ActionWorkflowOperation

```csharp
var workflow = WorkflowForge.CreateWorkflow("ActionExample")
    .AddOperation("LogStep", async (foundry, ct) =>
    {
        foundry.Logger.LogInformation("Executing step");
        foundry.SetProperty("Timestamp", DateTime.UtcNow);
        await Task.CompletedTask;
    })
    .Build();
```

### 3. ConditionalWorkflowOperation

```csharp
var conditionalOp = ConditionalWorkflowOperation.Create(
    condition: foundry => foundry.GetPropertyOrDefault<bool>("IsPremium"),
    trueOperation: new PremiumProcessingOperation(),
    falseOperation: new StandardProcessingOperation()
);

workflow.AddOperation("ProcessByTier", conditionalOp);
```

### 4. ForEachWorkflowOperation

```csharp
// Execute multiple operations concurrently (shared input to all)
var forEachOp = ForEachWorkflowOperation.CreateSharedInput(
    new[] { new ProcessItemOperation(), new ValidateOperation() },
    maxConcurrency: 4,
    name: "ProcessItems"
);

// Or split input collection among operations
var splitOp = ForEachWorkflowOperation.CreateSplitInput(
    itemOperations,
    maxConcurrency: 2
);
```

### 5. DelayOperation

```csharp
var delayOp = new DelayOperation(TimeSpan.FromSeconds(5));
workflow.AddOperation("Wait", delayOp);
```

### 6. LoggingOperation

```csharp
var logOp = new LoggingOperation("Order processing completed", logger);
workflow.AddOperation("LogCompletion", logOp);
```

## Custom Operations

### Method 1: Inherit WorkflowOperationBase

```csharp
public class CalculateTotalOperation : WorkflowOperationBase
{
    public override string Name => "CalculateTotal";
    public override bool SupportsRestore => false;

    protected override async Task<object?> ForgeAsyncCore(
        object? inputData,
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken = default)
    {
        var items = foundry.GetPropertyOrDefault<List<OrderItem>>("Items");
        var total = items.Sum(x => x.Price * x.Quantity);
        
        foundry.SetProperty("Total", total);
        foundry.Logger.LogInformation("Calculated total: {Total}", total);
        
        return total;
    }
}
```

### Method 2: Type-Safe Operations (Optional)

```csharp
public class ValidateOrderOperation : WorkflowOperationBase<Order, ValidationResult>
{
    public override string Name => "ValidateOrder";

    protected override async Task<ValidationResult> ForgeAsyncCore(
        Order input,
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken = default)
    {
        if (input == null || input.Items.Count == 0)
        {
            return new ValidationResult { IsValid = false, Errors = ["No items in order"] };
        }
        
        return new ValidationResult { IsValid = true };
    }
}
```

## Compensation (Saga Pattern)

Implement `RestoreAsync` for rollback capabilities:

```csharp
public class ChargePaymentOperation : WorkflowOperationBase
{
    public override string Name => "ChargePayment";
    public override bool SupportsRestore => true;

    protected override async Task<object?> ForgeAsyncCore(
        object? inputData,
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken)
    {
        var orderId = foundry.GetPropertyOrDefault<string>("OrderId");
        var amount = foundry.GetPropertyOrDefault<decimal>("Total");
        
        var paymentId = await _paymentService.ChargeAsync(orderId, amount, cancellationToken);
        
        foundry.SetProperty("PaymentId", paymentId);
        foundry.Logger.LogInformation("Payment charged: {PaymentId}", paymentId);
        
        return paymentId;
    }
    
    public override async Task RestoreAsync(
        object? outputData,
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken)
    {
        var paymentId = foundry.GetPropertyOrDefault<string>("PaymentId");
        
        if (!string.IsNullOrEmpty(paymentId))
        {
            await _paymentService.RefundAsync(paymentId, cancellationToken);
            foundry.Logger.LogInformation("Payment refunded: {PaymentId}", paymentId);
        }
    }
}
```

## Middleware

Add cross-cutting concerns using the middleware pipeline:

```csharp
public class TimingMiddleware : IWorkflowOperationMiddleware
{
    private readonly IWorkflowForgeLogger _logger;
    
    public TimingMiddleware(IWorkflowForgeLogger logger)
    {
        _logger = logger;
    }
    
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
            return await next(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation(
                "Operation {Name} completed in {Duration}ms",
                operation.Name,
                sw.Elapsed.TotalMilliseconds);
        }
    }
}

// Add to foundry
foundry.AddMiddleware(new TimingMiddleware(logger));
```

## Event System

Subscribe to lifecycle events:

```csharp
// Workflow-level events (from Smith)
smith.WorkflowStarted += (s, e) => 
    Console.WriteLine($"Started: {e.WorkflowName}");
smith.WorkflowCompleted += (s, e) => 
    Console.WriteLine($"Completed in {e.Duration.TotalMilliseconds}ms");
smith.WorkflowFailed += (s, e) => 
    Console.WriteLine($"Failed: {e.Exception.Message}");

// Operation-level events (from Foundry)
foundry.OperationStarted += (s, e) => 
    Console.WriteLine($"Op started: {e.OperationName}");
foundry.OperationCompleted += (s, e) => 
    Console.WriteLine($"Op completed: {e.OperationName}");
foundry.OperationFailed += (s, e) => 
    Console.WriteLine($"Op failed: {e.OperationName}");

// Compensation events (from Foundry)
foundry.CompensationTriggered += (s, e) => 
    Console.WriteLine("Rollback triggered");
foundry.OperationRestoreStarted += (s, e) => 
    Console.WriteLine($"Restoring: {e.OperationName}");
```

## Configuration

`WorkflowForgeOptions` inherits from `WorkflowForgeOptionsBase`, providing `Enabled`, `SectionName`, `Validate()`, and `Clone()` for consistent options behavior.

### Programmatic Configuration

```csharp
var options = new WorkflowForgeOptions
{
    Enabled = true,
    MaxConcurrentWorkflows = 10,
    ContinueOnError = false,
    FailFastCompensation = false,
    ThrowOnCompensationError = true,
    EnableOutputChaining = true
};

var foundry = WorkflowForge.CreateFoundry("MyWorkflow", options: options);
```

### Options Pattern (appsettings.json)

```json
{
  "WorkflowForge": {
    "Enabled": true,
    "MaxConcurrentWorkflows": 10,
    "ContinueOnError": false,
    "FailFastCompensation": false,
    "ThrowOnCompensationError": true,
    "EnableOutputChaining": true
  }
}
```

```csharp
services.AddWorkflowForge(configuration);
var smith = services.BuildServiceProvider().GetRequiredService<IWorkflowSmith>();
```

## Performance

WorkflowForge Core is optimized for production workloads (12 scenarios benchmarked, 50 iterations):

- **Execution Speed**: 11-540x faster than competitors
- **State Machine**: Up to 540x faster (highest advantage)
- **Memory**: 9-573x less allocation than competitors
- **Concurrency**: Near-perfect linear scaling (16x speedup for 16 workflows)

| Scenario | WorkflowForge | Workflow Core | Elsa | Advantage |
|----------|---------------|---------------|------|-----------|
| Sequential (10 ops) | 247μs | 6,531μs | 17,617μs | 26-71x |
| State Machine (25) | 68μs | 20,624μs | 36,695μs | 303-540x |
| Concurrent (8 workers) | 356μs | 38,833μs | 94,018μs | 109-264x |

See [Performance Documentation](../../../docs/performance/performance.md) for all 12 scenarios.

## Testing

All interfaces are mockable for comprehensive testing:

```csharp
using Moq;
using Xunit;

public class WorkflowTests
{
    [Fact]
    public async Task Should_Execute_Operations_In_Order()
    {
        // Arrange
        var executionOrder = new List<string>();
        
        var workflow = WorkflowForge.CreateWorkflow("Test")
            .WithOperation("Step1", async (foundry) => executionOrder.Add("Step1"))
            .WithOperation("Step2", async (foundry) => executionOrder.Add("Step2"))
            .WithOperation("Step3", async (foundry) => executionOrder.Add("Step3"))
            .Build();
        
        using var foundry = WorkflowForge.CreateFoundry("Test");
        using var smith = WorkflowForge.CreateSmith();
        
        // Act
        await smith.ForgeAsync(workflow, foundry);
        
        // Assert
        Assert.Equal(new[] { "Step1", "Step2", "Step3" }, executionOrder);
    }
}
```

## Documentation

- **[Getting Started Guide](../../../docs/getting-started/getting-started.md)** - Step-by-step tutorial
- **[Architecture](../../../docs/architecture/overview.md)** - Design principles and patterns
- **[Operations Guide](../../../docs/core/operations.md)** - All operation types and patterns
- **[Events System](../../../docs/core/events.md)** - Lifecycle events and monitoring
- **[Configuration](../../../docs/core/configuration.md)** - All configuration options
- **[Extensions](../../../docs/extensions/index.md)** - Available extensions
- **[Samples](../../samples/WorkflowForge.Samples.BasicConsole/README.md)** - 33 hands-on examples
- **[API Reference](../../../docs/reference/api-reference.md)** - Complete API documentation

## Extensions

While Core has zero dependencies, extend functionality with official packages:

- **WorkflowForge.Testing** - Unit testing utilities (`FakeWorkflowFoundry`)
- **WorkflowForge.Extensions.Logging.Serilog** - Structured logging
- **WorkflowForge.Extensions.Resilience** - Retry strategies (zero dependencies)
- **WorkflowForge.Extensions.Resilience.Polly** - Advanced resilience with Polly
- **WorkflowForge.Extensions.Validation** - DataAnnotations-based validation
- **WorkflowForge.Extensions.Audit** - Comprehensive audit logging
- **WorkflowForge.Extensions.Persistence** - Workflow state persistence
- **WorkflowForge.Extensions.Persistence.Recovery** - Recovery coordinator
- **WorkflowForge.Extensions.Observability.Performance** - Performance monitoring
- **WorkflowForge.Extensions.Observability.HealthChecks** - Health check integration
- **WorkflowForge.Extensions.Observability.OpenTelemetry** - Distributed tracing

**Dependency isolation**: Extensions internalize third-party libraries with ILRepack where appropriate, while keeping Microsoft/System assemblies external.

## License

MIT License - see [LICENSE](../../../LICENSE) for details.

---

**WorkflowForge Core** - *Build workflows with industrial strength*
