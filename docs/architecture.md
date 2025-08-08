# WorkflowForge Architecture

This document provides a comprehensive overview of WorkflowForge's architecture, design principles, and core abstractions.

## Design Principles

### 1. Clean Architecture
WorkflowForge follows clean architecture principles with clear separation of concerns:

- **Domain Layer**: Core business logic (workflows, operations)
- **Application Layer**: Orchestration and coordination (smiths, foundries)
- **Infrastructure Layer**: Cross-cutting concerns (logging, resilience)
- **Presentation Layer**: APIs and extensions

### 2. Dependency-Free Core
The core WorkflowForge library has zero external dependencies, ensuring:
- **Lightweight footprint** - Minimal impact on your application
- **Version compatibility** - No dependency conflicts
- **Flexibility** - Use with any logging, DI, or observability framework

### 3. Extension-First Design
Core functionality is minimal, with rich features provided through extensions:
- **Modular architecture** - Add only what you need
- **Pluggable components** - Replace or customize any part
- **Future-proof** - New capabilities without breaking changes

## Core Abstractions

### IWorkflow
Represents a complete workflow definition containing a sequence of operations.

```csharp
public interface IWorkflow
{
    Guid Id { get; }
    string Name { get; }
    IReadOnlyList<IWorkflowOperation> Operations { get; }
    IDictionary<string, object> Properties { get; }
}
```

**Key Characteristics:**
- **Immutable** - Cannot be modified after creation
- **Composable** - Built using the fluent builder pattern
- **Identifiable** - Each workflow has a unique ID
- **Metadata-rich** - Properties for workflow-level configuration

### IWorkflowOperation
Individual executable units within a workflow.

```csharp
public interface IWorkflowOperation
{
    Guid Id { get; }
    string Name { get; }
    bool SupportsRestore { get; }
    
    Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken);
    Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken);
}
```

**Key Characteristics:**
- **Async-first** - All operations are asynchronous
- **Compensatable** - Support rollback through `RestoreAsync`
- **Contextual** - Access to foundry for logging, data, and services
- **Cancellable** - Proper cancellation token support

### IWorkflowFoundry
The execution environment that provides context, data, and services to operations.

```csharp
public interface IWorkflowFoundry : IDisposable
{
    string Name { get; }
    IWorkflowForgeLogger Logger { get; }
    IDictionary<string, object> Properties { get; }
    IServiceProvider? ServiceProvider { get; }
    
    T GetService<T>() where T : class;
   ConcurrentDictionary<string, object?> Properties { get; }
}
```

**Key Characteristics:**
- **Resource management** - Implements IDisposable
- **Service location** - Access to dependency injection container
- **Shared state** - Properties accessible across operations
- **Logging integration** - Built-in logging abstraction

### IWorkflowSmith
The skilled craftsman responsible for executing workflows with proper error handling and compensation.

```csharp
public interface IWorkflowSmith
{
    Task ForgeAsync(IWorkflow workflow, IWorkflowFoundry foundry, CancellationToken cancellationToken = default);
    Task ForgeAsync(IWorkflow workflow, ConcurrentDictionary<string, object?> data, CancellationToken cancellationToken = default);
    
    event EventHandler<WorkflowStartedEventArgs>? WorkflowStarted;
    event EventHandler<WorkflowCompletedEventArgs>? WorkflowCompleted;
    event EventHandler<WorkflowFailedEventArgs>? WorkflowFailed;
}
```

**Key Characteristics:**
- **Orchestration** - Manages workflow execution flow
- **Error handling** - Automatic compensation on failures
- **Event-driven** - Lifecycle events for monitoring
- **Stateless** - Can be reused across multiple executions

## Architecture Layers

### 1. Core Layer
**Location**: `src/core/WorkflowForge/`

Contains the fundamental abstractions and implementations:

```
WorkflowForge/
├── Abstractions/          # Core interfaces
│   ├── IWorkflow.cs
│   ├── IWorkflowOperation.cs
│   ├── IWorkflowFoundry.cs
│   └── IWorkflowSmith.cs
├── Builders/              # Fluent builders
│   └── WorkflowBuilder.cs
├── Core/                  # Core implementations
│   ├── Workflow.cs
│   ├── WorkflowFoundry.cs
│   └── WorkflowSmith.cs
└── Operations/            # Built-in operations
    ├── InlineOperation.cs
    └── ConditionalOperation.cs
```

### 2. Extension Layer
**Location**: `src/extensions/`

Provides specialized functionality through extensions:

```
extensions/
├── WorkflowForge.Extensions.Logging.Serilog/
├── WorkflowForge.Extensions.Resilience/
├── WorkflowForge.Extensions.Resilience.Polly/
├── WorkflowForge.Extensions.Observability.Performance/
├── WorkflowForge.Extensions.Observability.HealthChecks/
└── WorkflowForge.Extensions.Observability.OpenTelemetry/
```

### 3. Application Layer
**Location**: `src/samples/` and user applications

Contains sample applications, tutorials, and user implementations.

## Execution Flow

### 1. Workflow Creation
```csharp
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("OrderProcessing")
    .AddOperation(new ValidateOrderOperation())
    .AddOperation(new ProcessPaymentOperation())
    .AddOperation(new FulfillOrderOperation())
    .Build();
```

### 2. Foundry Setup
```csharp
var foundry = WorkflowForge.CreateFoundry("OrderProcessing")
    .WithProperty("OrderId", orderId)
    .WithLogger(logger)
    .WithServiceProvider(serviceProvider);
```

### 3. Execution
```csharp
using var smith = WorkflowForge.CreateSmith();
using var foundry = WorkflowForge.CreateFoundry("OrderProcessing");

// Set initial data in foundry
foundry.SetProperty("order", order);

await smith.ForgeAsync(workflow, foundry);
```

### 4. Compensation (on failure)
If any operation fails, the smith automatically calls `RestoreAsync` on all previously executed operations in reverse order.

## Middleware Pipeline

WorkflowForge supports middleware for cross-cutting concerns:

```csharp
public interface IWorkflowOperationMiddleware
{
    Task<object?> ExecuteAsync(
        IWorkflowOperation operation,
        IWorkflowFoundry foundry,
        object? inputData,
        Func<Task<object?>> next,
        CancellationToken cancellationToken);
}
```

### Middleware Chain
Middleware executes in the order registered:

```
Request → Middleware1 → Middleware2 → Operation → Middleware2 → Middleware1 → Response
```

### Built-in Middleware
- **LoggingMiddleware** - Request/response logging
- **TimingMiddleware** - Performance measurement
- **ErrorHandlingMiddleware** - Exception handling
- **RetryMiddleware** - Automatic retry logic

## Extension Points

### 1. Custom Operations
Implement `IWorkflowOperation` for domain-specific logic:

```csharp
public class CustomBusinessOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "CustomBusiness";
    public bool SupportsRestore => true;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        // Implementation
        return result;
    }

    public async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        // Compensation logic
    }
}
```

### 2. Custom Middleware
Implement `IWorkflowOperationMiddleware` for cross-cutting concerns:

```csharp
public class SecurityMiddleware : IWorkflowOperationMiddleware
{
    public async Task<object?> ExecuteAsync(
        IWorkflowOperation operation,
        IWorkflowFoundry foundry,
        object? inputData,
        Func<Task<object?>> next,
        CancellationToken cancellationToken)
    {
        // Pre-execution security checks
        ValidateSecurity(inputData, foundry);
        
        var result = await next();
        
        // Post-execution audit
        AuditExecution(operation, result);
        
        return result;
    }
}
```

### 3. Custom Foundries
Extend `WorkflowFoundry` for specialized execution environments:

```csharp
public class EnterpiseWorkflowFoundry : WorkflowFoundry
{
    public EnterpiseWorkflowFoundry(string name, IServiceProvider serviceProvider) 
        : base(name, serviceProvider)
    {
        // Enterprise-specific initialization
    }
    
    // Additional enterprise features
}
```

## Configuration Architecture

### Environment-Specific Configuration
```csharp
public class FoundryConfiguration
{
    public static FoundryConfiguration ForDevelopment() => new()
    {
        LogLevel = LogLevel.Debug,
        EnablePerformanceMonitoring = true,
        RetryAttempts = 1
    };
    
    public static FoundryConfiguration ForProduction() => new()
    {
        LogLevel = LogLevel.Information,
        EnablePerformanceMonitoring = false,
        RetryAttempts = 3
    };
}
```

### Extension Configuration
Extensions use the Options pattern for configuration:

```csharp
services.Configure<PollySettings>(configuration.GetSection("Polly"));
services.Configure<SerilogSettings>(configuration.GetSection("Serilog"));
```

## Performance Architecture

### 1. Async-First Design
All APIs are async to avoid thread pool starvation:
- Operations are `Task<object?>` based
- Middleware uses async delegates
- No sync-over-async anti-patterns

### 2. Memory Management
- **Object pooling** for frequently allocated objects
- **Minimal allocations** in hot paths
- **Proper disposal** of resources

### 3. Concurrency Support
- **Thread-safe foundries** for concurrent access
- **Cancellation token** propagation throughout
- **Concurrent workflow execution** support

## Security Architecture

### 1. Input Validation
- Operations receive typed input data
- Foundry properties are validated
- Middleware can implement additional validation

### 2. Audit Trail
- All operations are logged with context
- Workflow execution is tracked
- Security events can be captured via middleware

### 3. Isolation
- Foundries provide execution isolation
- Operations cannot directly access other operations' data
- Service provider scoping supports request isolation

## Testing Architecture

### 1. Interface-Based Design
All core components implement interfaces for easy mocking:

```csharp
var mockOperation = new Mock<IWorkflowOperation>();
var mockFoundry = new Mock<IWorkflowFoundry>();
var mockSmith = new Mock<IWorkflowSmith>();
```

### 2. Dependency Injection
Foundries can be configured with test dependencies:

```csharp
var testFoundry = WorkflowForge.CreateFoundry("Test")
    .WithServiceProvider(testServiceProvider);
```

### 3. Test Utilities
The framework provides utilities for testing:
- In-memory foundries
- Mock operations
- Test middleware

## Related Documentation

- **[Getting Started](getting-started.md)** - Basic usage patterns
- **[Operations Guide](operations.md)** - Building custom operations
 - Middleware: see extension and pipeline examples in `docs/extensions.md` and samples
 - Performance: see `src/benchmarks/WorkflowForge.Benchmarks/README.md`
 - Testing: see testing examples in samples and unit tests

---

**WorkflowForge Architecture** - *Understanding the foundation of workflow orchestration* 