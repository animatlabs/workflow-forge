# WorkflowForge Core

The foundational workflow orchestration framework for .NET. This core package provides the essential abstractions, operations, and foundry infrastructure for building robust workflows without any external dependencies.

## 🎯 Core Package Overview

WorkflowForge Core is the dependency-free foundation of the WorkflowForge ecosystem, providing:

- **🏭 Foundry & Smith Architecture**: Industrial-strength metaphor with `IWorkflowFoundry` for execution context and `IWorkflowSmith` for orchestration
- **⚙️ Flexible Operations**: Support for sync/async operations, lambda expressions, and typed operations  
- **🔄 Compensation Support**: Built-in saga pattern with automatic rollback capabilities
- **🧩 Middleware Pipeline**: Extensible middleware system for cross-cutting concerns
- **📊 Data Management**: Thread-safe shared data with `ConcurrentDictionary`
- **🏗️ Builder Pattern**: Fluent API for constructing workflows
- **🔧 Zero Dependencies**: Core framework has no external dependencies
- **📋 Multiple Execution Patterns**: Sequential, parallel, conditional, and for-each operations
- **🎯 Type Safety**: Strongly-typed operations with compile-time validation
- **⚡ High Performance**: Optimized for production workloads

## 📦 Installation

```bash
dotnet add package WorkflowForge
```

## 🚀 Quick Start

### 1. Create a Simple Workflow

```csharp
using WorkflowForge;

// Create a workflow using the forge
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("ProcessOrder")
    .AddOperation("ValidateOrder", async (order, foundry, ct) => 
    {
        foundry.Logger.LogInformation("Validating order {OrderId}", order.Id);
        if (order.Amount <= 0)
            throw new InvalidOperationException("Invalid order amount");
        return order;
    })
    .AddOperation("ProcessPayment", async (order, foundry, ct) =>
    {
        foundry.Logger.LogInformation("Processing payment for order {OrderId}", order.Id);
        var paymentResult = await ProcessPaymentAsync(order, ct);
        foundry.Properties["PaymentId"] = paymentResult.Id;
        return paymentResult;
    })
    .AddOperation("FulfillOrder", async (paymentResult, foundry, ct) =>
    {
        foundry.Logger.LogInformation("Fulfilling order for payment {PaymentId}", paymentResult.Id);
        return await FulfillOrderAsync(paymentResult, ct);
    })
    .Build();
```

### 2. Execute with Foundry and Smith

```csharp
// Create foundry and smith
using var foundry = WorkflowForge.CreateFoundry("ProcessOrder");
using var smith = WorkflowForge.CreateSmith();

// Execute the workflow
try
{
    var order = new Order { Id = "ORD-001", Amount = 99.99m };
    // Set initial data in foundry properties
    foundry.Properties["order"] = order;
    
    await smith.ForgeAsync(workflow, foundry);
    foundry.Logger.LogInformation("Workflow completed successfully!");
}
catch (Exception ex)
{
    foundry.Logger.LogError(ex, "Workflow execution failed");
}
```

## 🏗️ Core Architecture

### The WorkflowForge Metaphor

In the WorkflowForge metaphor:
- **The Forge** (`WorkflowForge` static class) - Main factory for creating workflows and components
- **Foundries** (`IWorkflowFoundry`) - Execution environments where operations are performed
- **Smiths** (`IWorkflowSmith`) - Skilled craftsmen who manage foundries and forge workflows
- **Operations** (`IWorkflowOperation`) - Individual tasks performed in the foundry
- **Workflows** (`IWorkflow`) - Complete workflow definitions with operations

### Core Abstractions

#### IWorkflowFoundry - Execution Environment
The foundry provides the execution context and shared resources:

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
The smith manages workflow execution and compensation:

```csharp
public interface IWorkflowSmith : IDisposable
{
    Task ForgeAsync(IWorkflow workflow, IWorkflowFoundry foundry, CancellationToken cancellationToken = default);
    Task ForgeAsync(IWorkflow workflow, ConcurrentDictionary<string, object?> data, CancellationToken cancellationToken = default);
    // Additional overloads for various scenarios
}
```

#### IWorkflowOperation - Individual Tasks
Operations are the building blocks of workflows:

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

## 🔧 Built-in Operations

### Delegate Operations
Execute lambda expressions or method references:

```csharp
// Simple synchronous operation
workflow.AddOperation("LogMessage", (input, foundry, ct) => 
{
    foundry.Logger.LogInformation("Processing: {Input}", input);
    return input;
});

// Asynchronous operation
workflow.AddOperation("ProcessAsync", async (input, foundry, ct) => 
{
    await Task.Delay(100, ct);
    return $"Processed: {input}";
});
```

### Action Operations
Execute actions without return values:

```csharp
var actionOp = new ActionWorkflowOperation("LogAction", (input, foundry, ct) =>
{
    foundry.Logger.LogInformation("Action executed with: {Input}", input);
});

workflow.AddOperation(actionOp);
```

### Conditional Operations
Execute operations based on conditions:

```csharp
var conditionalOp = ConditionalWorkflowOperation.Create(
    condition: foundry => foundry.Properties.ContainsKey("IsPremium"),
    trueOperation: new PremiumProcessingOperation(),
    falseOperation: new StandardProcessingOperation()
);

workflow.AddOperation(conditionalOp);
```

### ForEach Operations
Process collections in parallel or sequentially:

```csharp
var forEachOp = ForEachWorkflowOperation.Create<string>(
    items: new[] { "item1", "item2", "item3" },
    operation: new ProcessItemOperation(),
    parallelExecution: true
);

workflow.AddOperation(forEachOp);
```

### Utility Operations
Common utility operations:

```csharp
// Delay operation
workflow.AddOperation(new DelayOperation(TimeSpan.FromSeconds(5)));

// Logging operation
workflow.AddOperation(new LoggingOperation("Processing started", LogLevel.Information));
```

## 🔄 Compensation & Saga Pattern

WorkflowForge supports automatic compensation for saga patterns:

```csharp
public class PaymentOperation : IWorkflowOperation
{
    public string Name => "ProcessPayment";
    public bool SupportsRestore => true;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var order = (Order)inputData!;
        foundry.Logger.LogInformation("Processing payment for order {OrderId}", order.Id);
        
        var paymentResult = await ProcessPaymentAsync(order, cancellationToken);
        foundry.Properties["PaymentId"] = paymentResult.PaymentId;
        
        return paymentResult;
    }
    
    public async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        if (foundry.Properties.TryGetValue("PaymentId", out var paymentId))
        {
            foundry.Logger.LogWarning("Reversing payment {PaymentId}", paymentId);
            await RefundPaymentAsync((string)paymentId!, cancellationToken);
        }
    }
}
```

## 🔌 Middleware System

Create custom middleware for cross-cutting concerns:

```csharp
public class TimingMiddleware : IWorkflowMiddleware
{
    private readonly IWorkflowForgeLogger _logger;

    public TimingMiddleware(IWorkflowForgeLogger logger)
    {
        _logger = logger;
    }

    public async Task<object?> ExecuteAsync(IWorkflowOperation operation, object? inputData, IWorkflowFoundry foundry, Func<Task<object?>> next, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await next();
            stopwatch.Stop();
            _logger.LogInformation("Operation {OperationName} completed in {Duration}ms", 
                operation.Name, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Operation {OperationName} failed after {Duration}ms", 
                operation.Name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

## ⚙️ Configuration

### Foundry Configuration
Configure foundries for different environments:

```csharp
// Minimal configuration
var foundry = WorkflowForge.CreateFoundry("MyWorkflow", FoundryConfiguration.Minimal());

// Development configuration
var foundry = WorkflowForge.CreateFoundry("MyWorkflow", FoundryConfiguration.ForDevelopment());

// Production configuration  
var foundry = WorkflowForge.CreateFoundry("MyWorkflow", FoundryConfiguration.ForProduction());

// Custom configuration
var config = new FoundryConfiguration
{
    EnableAutoRestore = true,
    MaxConcurrentOperations = 8,
    DefaultTimeout = TimeSpan.FromMinutes(5)
};
var foundry = WorkflowForge.CreateFoundry("MyWorkflow", config);
```

### Workflow Settings
Configure workflows with metadata:

```csharp
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("ProcessOrder")
    .WithVersion("1.2.0")
    .WithDescription("Process customer orders with payment and fulfillment")
    .WithTimeout(TimeSpan.FromMinutes(10))
    .AddOperation(/* operations */)
    .Build();
```

## 🧪 Testing Support

WorkflowForge is designed for testability:

```csharp
[Test]
public async Task Should_Execute_Workflow_Operations()
{
    // Arrange
    var mockOperation = new Mock<IWorkflowOperation>();
    mockOperation.Setup(x => x.ForgeAsync(It.IsAny<object>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync("test-result");

    var workflow = WorkflowForge.CreateWorkflow("TestWorkflow")
        .AddOperation(mockOperation.Object)
        .Build();

    var foundry = WorkflowForge.CreateFoundry("TestWorkflow");
    var smith = WorkflowForge.CreateSmith();

    // Act
    foundry.Properties["test-input"] = "test-input"; // Set input data in foundry
    await smith.ForgeAsync(workflow, foundry);

    // Assert
    Assert.That(foundry.Properties["test-input"], Is.EqualTo("test-input"));
    mockOperation.Verify(x => x.ForgeAsync(It.IsAny<object>(), foundry, It.IsAny<CancellationToken>()), Times.Once);
}
```

## 📊 Performance Characteristics

WorkflowForge Core is optimized for performance:

- **Zero external dependencies** - No additional overhead
- **Minimal allocations** - Efficient memory usage in hot paths
- **Async-first design** - Non-blocking execution throughout
- **Thread-safe operations** - ConcurrentDictionary for shared state
- **Lightweight abstractions** - Minimal interface overhead

## 🔗 Extension Ecosystem

The core package integrates seamlessly with WorkflowForge extensions:

- **WorkflowForge.Extensions.Observability.Performance** - Performance monitoring and metrics
- **WorkflowForge.Extensions.Observability.HealthChecks** - System health monitoring
- **WorkflowForge.Extensions.Observability.OpenTelemetry** - Distributed tracing
- **WorkflowForge.Extensions.Resilience** - Basic retry and circuit breaker patterns
- **WorkflowForge.Extensions.Resilience.Polly** - Advanced resilience with Polly
- **WorkflowForge.Extensions.Logging.Serilog** - Structured logging with Serilog

## 📚 Additional Resources

- [Main Project Documentation](../../README.md)
- [Performance Benchmarks](../../benchmarks/WorkflowForge.Benchmarks/README.md)
- [Sample Projects](../../samples/README.md)
- [Extension Documentation](../)

## 📝 Professional Logging System

WorkflowForge Core includes a comprehensive, professional logging system designed for production environments with consistent property naming and professional messaging.

### Core Logging Components

#### Structured Property Names
Consistent property naming across all components using `PropertyNames` class:

```csharp
using WorkflowForge.Loggers;

// Core execution properties (consistent across all contexts)
PropertyNames.ExecutionId        // Unique execution identifier
PropertyNames.ExecutionName      // Human-readable execution name  
PropertyNames.ExecutionType      // Type of execution (Workflow, Operation, etc.)

// Workflow context properties
PropertyNames.FoundryExecutionId       // Foundry instance identifier
PropertyNames.TotalOperationCount      // Total operations in workflow
PropertyNames.ParentWorkflowExecutionId // For nested workflow tracking

// Error context properties
PropertyNames.ExceptionType      // Exception type for structured error tracking
PropertyNames.ErrorCode          // Error code for categorization
PropertyNames.ErrorCategory      // Error category (ArgumentError, etc.)

// Compensation context properties  
PropertyNames.CompensationOperationCount  // Operations to compensate
PropertyNames.CompensationSuccessCount    // Successful compensations
PropertyNames.CompensationFailureCount    // Failed compensations
```

#### Corporate Message Templates
Professional static messages via `WorkflowLogMessages` class:

```csharp
using WorkflowForge.Loggers;

// Workflow lifecycle messages
WorkflowLogMessages.WorkflowExecutionStarted
WorkflowLogMessages.WorkflowExecutionCompleted  
WorkflowLogMessages.WorkflowExecutionFailed
WorkflowLogMessages.WorkflowExecutionCancelled

// Operation lifecycle messages
WorkflowLogMessages.OperationExecutionStarted
WorkflowLogMessages.OperationExecutionCompleted
WorkflowLogMessages.OperationExecutionFailed

// Compensation messages
WorkflowLogMessages.CompensationProcessStarted
WorkflowLogMessages.CompensationActionCompleted
WorkflowLogMessages.CompensationActionFailed
```

#### Logging Context Helpers
Standardized scope creation with `LoggingContextHelper`:

```csharp
using WorkflowForge.Loggers;

// Create workflow execution scope with consistent properties
using var workflowScope = LoggingContextHelper.CreateWorkflowScope(logger, workflow, foundry);

// Create operation execution scope 
using var operationScope = LoggingContextHelper.CreateOperationScope(logger, operation, stepIndex);

// Create compensation scope
using var compensationScope = LoggingContextHelper.CreateCompensationScope(logger, operationCount);

// Create error properties
var errorProps = LoggingContextHelper.CreateErrorProperties(exception, "WorkflowExecution");
logger.LogError(errorProps, exception, WorkflowLogMessages.WorkflowExecutionFailed);
```

### Correlation ID Management

Correlation IDs are managed as **foundry data**, not logging properties, enabling tracking across sub-workflows:

```csharp
using WorkflowForge.Loggers;

// Set correlation ID for tracking across operations and sub-workflows
LoggingContextHelper.SetCorrelationId(foundry, "REQ-12345");

// Get correlation ID from foundry
var correlationId = LoggingContextHelper.GetCorrelationId(foundry);

// Set parent workflow for nested workflow tracking
LoggingContextHelper.SetParentWorkflowExecutionId(childFoundry, parentWorkflow.Id.ToString());

// Create child foundry that inherits correlation context
var childFoundry = WorkflowForge.CreateFoundry("ChildWorkflow");
LoggingContextHelper.SetCorrelationId(childFoundry, correlationId); // Propagate correlation
LoggingContextHelper.SetParentWorkflowExecutionId(childFoundry, workflow.Id.ToString());
```

### Scope-Based Property Inheritance

Properties automatically inherit through logging scopes:

```csharp
// Workflow scope provides base context
using var workflowScope = LoggingContextHelper.CreateWorkflowScope(logger, workflow, foundry);

// All logs within this scope inherit workflow properties
logger.LogInformation(WorkflowLogMessages.WorkflowExecutionStarted); 

for (int i = 0; i < workflow.Operations.Count; i++)
{
    var operation = workflow.Operations[i];
    
    // Operation scope inherits workflow context + adds operation context
    using var operationScope = LoggingContextHelper.CreateOperationScope(logger, operation, i + 1);
    
    // This log has both workflow AND operation context automatically
    logger.LogDebug(WorkflowLogMessages.OperationExecutionStarted);
    
    try
    {
        await operation.ForgeAsync(inputData, foundry, cancellationToken);
        logger.LogDebug(WorkflowLogMessages.OperationExecutionCompleted);
    }
    catch (Exception ex)
    {
        // Error properties complement inherited scope properties
        var errorProps = LoggingContextHelper.CreateErrorProperties(ex, "OperationExecution");
        logger.LogError(errorProps, ex, WorkflowLogMessages.OperationExecutionFailed);
        throw;
    }
}
```

### Built-in Logger Implementations

#### ConsoleLogger (Development)
Simple console output for development and testing:

```csharp
var logger = new ConsoleLogger("MyWorkflow");
var foundry = WorkflowForge.CreateFoundry("TestWorkflow", 
    FoundryConfiguration.ForDevelopment().WithLogger(logger));
```

#### NullLogger (Testing)
No-op logger for unit testing:

```csharp
var logger = NullLogger.Instance;
var foundry = WorkflowForge.CreateFoundry("TestWorkflow",
    FoundryConfiguration.ForTesting().WithLogger(logger));
```

### Extension-Based Advanced Logging

Core provides essential context - extensions handle specialized concerns:

**Performance Metrics** → `WorkflowForge.Extensions.Observability.Performance`
- Duration tracking, memory usage, throughput metrics
- Uses `PerformancePropertyNames.DurationMs`, etc.

**Distributed Tracing** → `WorkflowForge.Extensions.Observability.OpenTelemetry`  
- OpenTelemetry integration, span correlation, distributed context

**Structured Logging** → `WorkflowForge.Extensions.Logging.Serilog`
- Rich structured logging, property enrichment, production sinks

```csharp
// Core provides execution context, extensions add specialized capabilities
var foundryConfig = FoundryConfiguration.ForProduction()
    .UseSerilog()                    // Rich structured logging
    .EnablePerformanceMonitoring()   // Performance metrics
    .EnableOpenTelemetry();          // Distributed tracing

var foundry = WorkflowForge.CreateFoundry("ProductionWorkflow", foundryConfig);
```

### Log Level Guidelines

- **Trace**: Middleware entry/exit (detailed flow control)
- **Debug**: Operation lifecycle (business process steps)  
- **Information**: Workflow lifecycle (major business events)
- **Warning**: Compensation actions (recovery processes)
- **Error**: Execution failures (business/technical errors)
- **Critical**: System failures (infrastructure issues)

```csharp
// Middleware uses Trace for detailed flow
logger.LogTrace(WorkflowLogMessages.MiddlewareExecutionStarted);

// Operations use Debug for business steps
logger.LogDebug(WorkflowLogMessages.OperationExecutionStarted);

// Workflows use Information for major events
logger.LogInformation(WorkflowLogMessages.WorkflowExecutionStarted);

// Compensation uses Warning (recovery scenario)
logger.LogWarning(WorkflowLogMessages.CompensationProcessStarted);

// Failures use Error with structured context
var errorProps = LoggingContextHelper.CreateErrorProperties(ex, "BusinessProcess");
logger.LogError(errorProps, ex, WorkflowLogMessages.OperationExecutionFailed);
```

This professional logging system provides:
- **Consistent Property Naming**: No conflicts with external systems (e.g., avoids Azure's OperationId)
- **Corporate Messaging**: Professional templates without informal language
- **Correlation Tracking**: End-to-end request tracking through foundry data
- **Scope Inheritance**: Automatic context propagation without property repetition
- **Extension Separation**: Core context vs specialized metrics in extensions
- **Production Ready**: Appropriate log levels for production log control

---

**WorkflowForge Core** - *The foundation for reliable workflow orchestration* 