# WorkflowForge Core

The foundational workflow orchestration framework for .NET with zero dependencies, built-in compensation, and sub-20 microsecond operation performance.

## Package Overview

WorkflowForge Core is the dependency-free foundation providing:

- **Foundry & Smith Architecture**: Industrial-strength metaphor with `IWorkflowFoundry` for execution context and `IWorkflowSmith` for orchestration
- **Flexible Operations**: Support for sync/async operations, lambda expressions, and typed operations  
- **Compensation Support**: Built-in saga pattern with automatic rollback capabilities
- **Middleware Pipeline**: Extensible middleware system for cross-cutting concerns
- **Data Management**: Thread-safe shared data with `ConcurrentDictionary`
- **Builder Pattern**: Fluent API for constructing workflows
- **Zero Dependencies**: Core framework has no external dependencies
- **High Performance**: Optimized for production workloads

## Installation

```bash
dotnet add package WorkflowForge
```

## Quick Start

```csharp
using WorkflowForge;

// Create a workflow using the forge
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("ProcessOrder")
    .AddOperation("ValidateOrder", async (order, foundry, ct) => 
    {
        foundry.Logger.LogInformation("Validating order {OrderId}", order.Id);
        return order;
    })
    .Build();

// Execute the workflow
using var foundry = WorkflowForge.CreateFoundry("ProcessOrder");
using var smith = WorkflowForge.CreateSmith();

await smith.ForgeAsync(workflow, foundry);
```

## Core Architecture

### The WorkflowForge Metaphor

- **The Forge** (`WorkflowForge` static class) - Main factory for creating workflows and components
- **Foundries** (`IWorkflowFoundry`) - Execution environments where operations are performed
- **Smiths** (`IWorkflowSmith`) - Skilled craftsmen who manage foundries and forge workflows
- **Operations** (`IWorkflowOperation`) - Individual tasks performed in the foundry
- **Workflows** (`IWorkflow`) - Complete workflow definitions with operations

### Core Abstractions

#### IWorkflowFoundry - Execution Environment
```csharp
public interface IWorkflowFoundry : IDisposable
{
    Guid ExecutionId { get; }
    IWorkflow? CurrentWorkflow { get; }
    ConcurrentDictionary<string, object?> Properties { get; }
    IWorkflowForgeLogger Logger { get; }
    IServiceProvider? ServiceProvider { get; }
    
    void SetCurrentWorkflow(IWorkflow? workflow);
    void AddOperation(IWorkflowOperation operation);
}
```

#### IWorkflowSmith - Orchestration Engine
```csharp
public interface IWorkflowSmith : IDisposable
{
    Task ForgeAsync(IWorkflow workflow, IWorkflowFoundry foundry, CancellationToken cancellationToken = default);
    Task ForgeAsync(IWorkflow workflow, ConcurrentDictionary<string, object?> data, CancellationToken cancellationToken = default);
}
```

#### IWorkflowOperation - Individual Tasks
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

## Documentation & Examples

- **[Interactive Samples](../../samples/WorkflowForge.Samples.BasicConsole/)** - 18 hands-on examples (recommended starting point)
- **[Getting Started Guide](../../../docs/getting-started.md)** - Step-by-step tutorial
- **[Architecture Documentation](../../../docs/architecture.md)** - Core design principles
- **[Extensions](../../../docs/extensions.md)** - Available extensions
- **[Complete Documentation](../../../docs/)** - Comprehensive guides and reference

## Built-in Operations

### Delegate Operations
```csharp
// Simple operation
workflow.AddOperation("LogMessage", (input, foundry, ct) => 
{
    foundry.Logger.LogInformation("Processing: {Input}", input);
    return input;
});

// Async operation
workflow.AddOperation("ProcessAsync", async (input, foundry, ct) => 
{
    await Task.Delay(100, ct);
    return $"Processed: {input}";
});
```

### Conditional Operations
```csharp
var conditionalOp = ConditionalWorkflowOperation.Create(
    condition: foundry => foundry.Properties.ContainsKey("IsPremium"),
    trueOperation: new PremiumProcessingOperation(),
    falseOperation: new StandardProcessingOperation()
);
```

### ForEach Operations
```csharp
var forEachOp = ForEachWorkflowOperation.Create<string>(
    items: new[] { "item1", "item2", "item3" },
    operation: new ProcessItemOperation(),
    parallelExecution: true
);
```

## Compensation (Saga Pattern)

Built-in support for automatic rollback:

```csharp
public class PaymentOperation : IWorkflowOperation
{
    public string Name => "ProcessPayment";
    public bool SupportsRestore => true;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var paymentResult = await ProcessPaymentAsync((Order)inputData!, cancellationToken);
        foundry.Properties["PaymentId"] = paymentResult.PaymentId;
        return paymentResult;
    }
    
    public async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        if (foundry.Properties.TryGetValue("PaymentId", out var paymentId))
        {
            await RefundPaymentAsync((string)paymentId!, cancellationToken);
        }
    }
}
```

## Testing

Built for testability with mockable interfaces:

```csharp
[Fact]
public async Task Should_Execute_Workflow_Successfully()
{
    // Arrange
    var mockOperation = new Mock<IWorkflowOperation>();
    mockOperation.Setup(x => x.ForgeAsync(It.IsAny<object>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync("result");

    var workflow = WorkflowForge.CreateWorkflow()
        .AddOperation(mockOperation.Object)
        .Build();

    // Act & Assert
    var result = await smith.ForgeAsync(workflow, foundry);
    Assert.Equal("result", result);
}
```

---

*Zero-dependency workflow orchestration for .NET* 