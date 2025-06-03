# API Reference

Complete API reference for WorkflowForge workflow orchestration framework.

## Core Interfaces

### IWorkflowForge

Main factory interface for creating workflows and components.

```csharp
public static class WorkflowForge
{
    // Workflow creation
    public static IWorkflowBuilder CreateWorkflow();
    public static IWorkflowBuilder CreateWorkflow(string name);
    
    // Foundry creation  
    public static IWorkflowFoundry CreateFoundry(string foundryName);
    public static IWorkflowFoundry CreateFoundry(string foundryName, FoundryConfiguration configuration);
    
    // Smith creation
    public static IWorkflowSmith CreateSmith();
    public static IWorkflowSmith CreateSmith(IServiceProvider? serviceProvider);
}
```

### IWorkflowFoundry

Execution environment interface providing context and shared resources.

```csharp
public interface IWorkflowFoundry : IDisposable
{
    // Core properties
    Guid ExecutionId { get; }
    string FoundryName { get; }
    IWorkflow? CurrentWorkflow { get; }
    
    // Shared data and services
    ConcurrentDictionary<string, object?> Properties { get; }
    IWorkflowForgeLogger Logger { get; }
    IServiceProvider? ServiceProvider { get; }
    
    // Workflow management
    void SetCurrentWorkflow(IWorkflow? workflow);
    void AddOperation(IWorkflowOperation operation);
}
```

### IWorkflowSmith

Orchestration engine interface for executing workflows.

```csharp
public interface IWorkflowSmith : IDisposable
{
    // Primary execution methods
    Task ForgeAsync(IWorkflow workflow, IWorkflowFoundry foundry, CancellationToken cancellationToken = default);
    Task ForgeAsync(IWorkflow workflow, ConcurrentDictionary<string, object?> data, CancellationToken cancellationToken = default);
    
    // Utility overloads
    Task<T?> ForgeAsync<T>(IWorkflow workflow, IWorkflowFoundry foundry, CancellationToken cancellationToken = default);
    Task<T?> ForgeAsync<T>(IWorkflow workflow, ConcurrentDictionary<string, object?> data, CancellationToken cancellationToken = default);
}
```

### IWorkflow

Workflow definition interface containing operations and metadata.

```csharp
public interface IWorkflow : IDisposable
{
    // Identity and metadata
    Guid Id { get; }
    string Name { get; }
    string? Version { get; }
    string? Description { get; }
    
    // Operations and configuration
    IReadOnlyList<IWorkflowOperation> Operations { get; }
    TimeSpan? Timeout { get; }
    
    // Execution tracking
    DateTime CreatedAt { get; }
    WorkflowStatus Status { get; }
}
```

### IWorkflowOperation

Individual task interface for workflow operations.

```csharp
public interface IWorkflowOperation : IDisposable
{
    // Identity
    Guid Id { get; }
    string Name { get; }
    
    // Capabilities
    bool SupportsRestore { get; }
    
    // Execution methods
    Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken);
    Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken);
}
```

## Workflow Builder

### IWorkflowBuilder

Fluent builder interface for constructing workflows.

```csharp
public interface IWorkflowBuilder
{
    // Metadata configuration
    IWorkflowBuilder WithName(string name);
    IWorkflowBuilder WithVersion(string version);
    IWorkflowBuilder WithDescription(string description);
    IWorkflowBuilder WithTimeout(TimeSpan timeout);
    
    // Operation addition
    IWorkflowBuilder AddOperation(IWorkflowOperation operation);
    IWorkflowBuilder AddOperation(string name, Func<object?, IWorkflowFoundry, CancellationToken, Task<object?>> func);
    IWorkflowBuilder AddOperation(string name, Func<object?, IWorkflowFoundry, CancellationToken, object?> func);
    
    // Conditional operations
    IWorkflowBuilder AddConditionalOperation(string name, Func<IWorkflowFoundry, bool> condition, IWorkflowOperation trueOperation, IWorkflowOperation falseOperation);
    
    // ForEach operations
    IWorkflowBuilder AddForEachOperation<T>(string name, IEnumerable<T> items, IWorkflowOperation itemOperation, bool parallelExecution = false);
    
    // Build workflow
    IWorkflow Build();
}
```

## Built-in Operations

### DelegateWorkflowOperation

Execute delegate functions as operations.

```csharp
public class DelegateWorkflowOperation : IWorkflowOperation
{
    public DelegateWorkflowOperation(string name, Func<object?, IWorkflowFoundry, CancellationToken, Task<object?>> asyncFunc);
    public DelegateWorkflowOperation(string name, Func<object?, IWorkflowFoundry, CancellationToken, object?> func);
}
```

### ConditionalWorkflowOperation

Execute operations based on conditions.

```csharp
public class ConditionalWorkflowOperation : IWorkflowOperation
{
    public static ConditionalWorkflowOperation Create(
        Func<IWorkflowFoundry, bool> condition,
        IWorkflowOperation trueOperation,
        IWorkflowOperation falseOperation);
}
```

### ForEachWorkflowOperation

Process collections in parallel or sequentially.

```csharp
public class ForEachWorkflowOperation<T> : IWorkflowOperation
{
    public static ForEachWorkflowOperation<T> Create(
        IEnumerable<T> items,
        IWorkflowOperation operation,
        bool parallelExecution = false);
}
```

### ActionWorkflowOperation

Execute actions without return values.

```csharp
public class ActionWorkflowOperation : IWorkflowOperation
{
    public ActionWorkflowOperation(string name, Action<object?, IWorkflowFoundry, CancellationToken> action);
    public ActionWorkflowOperation(string name, Func<object?, IWorkflowFoundry, CancellationToken, Task> asyncAction);
}
```

## Configuration

### FoundryConfiguration

Configuration class for foundry behavior.

```csharp
public class FoundryConfiguration
{
    // Factory methods
    public static FoundryConfiguration Minimal();
    public static FoundryConfiguration ForDevelopment();
    public static FoundryConfiguration ForProduction();
    public static FoundryConfiguration ForTesting();
    
    // Properties
    public bool EnableAutoRestore { get; set; }
    public int MaxConcurrentOperations { get; set; }
    public TimeSpan DefaultTimeout { get; set; }
    public IWorkflowForgeLogger? Logger { get; set; }
    public IServiceProvider? ServiceProvider { get; set; }
    
    // Fluent configuration
    public FoundryConfiguration WithLogger(IWorkflowForgeLogger logger);
    public FoundryConfiguration WithServiceProvider(IServiceProvider serviceProvider);
    public FoundryConfiguration WithMaxConcurrentOperations(int maxConcurrentOperations);
    public FoundryConfiguration WithDefaultTimeout(TimeSpan timeout);
}
```

## Logging

### IWorkflowForgeLogger

Logging interface for structured workflow logging.

```csharp
public interface IWorkflowForgeLogger
{
    // Basic logging methods
    void LogTrace(string message, params object[] args);
    void LogDebug(string message, params object[] args);
    void LogInformation(string message, params object[] args);
    void LogWarning(string message, params object[] args);
    void LogError(string message, params object[] args);
    void LogCritical(string message, params object[] args);
    
    // Structured logging with properties
    void LogTrace(IDictionary<string, string> properties, string message, params object[] args);
    void LogDebug(IDictionary<string, string> properties, string message, params object[] args);
    void LogInformation(IDictionary<string, string> properties, string message, params object[] args);
    void LogWarning(IDictionary<string, string> properties, string message, params object[] args);
    void LogError(IDictionary<string, string> properties, string message, params object[] args);
    void LogCritical(IDictionary<string, string> properties, string message, params object[] args);
    
    // Exception logging
    void LogWarning(IDictionary<string, string> properties, Exception exception, string message, params object[] args);
    void LogError(IDictionary<string, string> properties, Exception exception, string message, params object[] args);
    void LogCritical(IDictionary<string, string> properties, Exception exception, string message, params object[] args);
    
    // Scope management
    IDisposable BeginScope<TState>(TState state);
}
```

### Built-in Loggers

```csharp
// Console logger for development
public class ConsoleLogger : IWorkflowForgeLogger

// Null logger for testing
public class NullLogger : IWorkflowForgeLogger
{
    public static readonly IWorkflowForgeLogger Instance;
}
```

## Middleware

### IWorkflowMiddleware

Middleware interface for cross-cutting concerns.

```csharp
public interface IWorkflowMiddleware
{
    Task<object?> ExecuteAsync(
        IWorkflowOperation operation,
        object? inputData,
        IWorkflowFoundry foundry,
        Func<Task<object?>> next,
        CancellationToken cancellationToken);
}
```

## Enumerations

### WorkflowStatus

```csharp
public enum WorkflowStatus
{
    Created,
    Running,
    Completed,
    Failed,
    Cancelled,
    Compensating,
    Compensated
}
```

### LogLevel

```csharp
public enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Critical = 5,
    None = 6
}
```

## Exceptions

### WorkflowForgeException

Base exception for WorkflowForge-specific errors.

```csharp
public class WorkflowForgeException : Exception
{
    public WorkflowForgeException();
    public WorkflowForgeException(string message);
    public WorkflowForgeException(string message, Exception innerException);
}
```

### OperationExecutionException

Exception thrown during operation execution.

```csharp
public class OperationExecutionException : WorkflowForgeException
{
    public IWorkflowOperation Operation { get; }
    public object? InputData { get; }
}
```

### CompensationException

Exception thrown during compensation/restore operations.

```csharp
public class CompensationException : WorkflowForgeException
{
    public IReadOnlyList<IWorkflowOperation> FailedOperations { get; }
}
```

## Extension Points

### Extension Configuration

Methods for configuring extensions on FoundryConfiguration.

```csharp
// Serilog extension
public static FoundryConfiguration UseSerilog(this FoundryConfiguration config);
public static FoundryConfiguration UseSerilog(this FoundryConfiguration config, ILogger logger);

// Polly resilience extension
public static FoundryConfiguration UsePollyResilience(this FoundryConfiguration config);
public static FoundryConfiguration UsePollyResilience(this FoundryConfiguration config, PollyResilienceConfiguration pollyConfig);

// Performance monitoring extension
public static FoundryConfiguration EnablePerformanceMonitoring(this FoundryConfiguration config);

// Health checks extension
public static FoundryConfiguration EnableHealthChecks(this FoundryConfiguration config);

// OpenTelemetry extension
public static FoundryConfiguration EnableOpenTelemetry(this FoundryConfiguration config, string serviceName, string serviceVersion);
```

---

*Complete API reference for WorkflowForge* 