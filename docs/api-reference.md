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
}
```

### IWorkflowFoundry

Execution environment interface providing context and shared resources.

```csharp
public interface IWorkflowFoundry : IDisposable
{
    // Core properties
    Guid ExecutionId { get; }
    IWorkflow? CurrentWorkflow { get; }

    // Shared data and services
    ConcurrentDictionary<string, object?> Properties { get; }
    IWorkflowForgeLogger Logger { get; }
    IServiceProvider? ServiceProvider { get; }

    // Workflow management
    void SetCurrentWorkflow(IWorkflow? workflow);
    void AddOperation(IWorkflowOperation operation);
    void AddMiddleware(IWorkflowOperationMiddleware middleware);
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
    
    // Utility overloads (typed result)
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

### WorkflowBuilder

Fluent builder for constructing workflows.

```csharp
public sealed class WorkflowBuilder
{
    // Metadata configuration
    public WorkflowBuilder WithName(string name);
    public WorkflowBuilder WithVersion(string version);
    public WorkflowBuilder WithDescription(string? description);

    // Operation addition
    public WorkflowBuilder AddOperation(IWorkflowOperation operation);
    public WorkflowBuilder AddOperation<T>() where T : class, IWorkflowOperation;
    public WorkflowBuilder AddOperation(string name, Func<IWorkflowFoundry, CancellationToken, Task> action);
    public WorkflowBuilder AddOperation(string name, Action<IWorkflowFoundry> action);

    // Build workflow
    public IWorkflow Build();

    // Helpers
    public static IWorkflow Sequential(params IWorkflowOperation[] operations);
    public static IWorkflow Parallel(params IWorkflowOperation[] operations);
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
public sealed class ConditionalWorkflowOperation : IWorkflowOperation
{
    public static ConditionalWorkflowOperation Create(
        Func<IWorkflowFoundry, bool> condition,
        IWorkflowOperation trueOperation,
        IWorkflowOperation? falseOperation = null,
        string? name = null);

    public static ConditionalWorkflowOperation CreateDataAware(
        Func<object?, IWorkflowFoundry, CancellationToken, Task<bool>> condition,
        IWorkflowOperation trueOperation,
        IWorkflowOperation? falseOperation = null,
        string? name = null);

    public static ConditionalWorkflowOperation CreateTyped<T>(
        Func<T, IWorkflowFoundry, CancellationToken, Task<bool>> condition,
        IWorkflowOperation trueOperation,
        IWorkflowOperation? falseOperation = null,
        string? name = null);
}
```

### ForEachWorkflowOperation

Execute a set of operations with shared, split, or no input, with optional throttling.

```csharp
public sealed class ForEachWorkflowOperation : IWorkflowOperation
{
    public static ForEachWorkflowOperation CreateSharedInput(
        IEnumerable<IWorkflowOperation> operations,
        TimeSpan? timeout = null,
        int? maxConcurrency = null,
        string? name = null);

    public static ForEachWorkflowOperation CreateSplitInput(
        IEnumerable<IWorkflowOperation> operations,
        TimeSpan? timeout = null,
        int? maxConcurrency = null,
        string? name = null);

    public static ForEachWorkflowOperation CreateNoInput(
        IEnumerable<IWorkflowOperation> operations,
        TimeSpan? timeout = null,
        int? maxConcurrency = null,
        string? name = null);

    public static ForEachWorkflowOperation Create(params IWorkflowOperation[] operations);
    public static ForEachWorkflowOperation CreateWithThrottling(
        IEnumerable<IWorkflowOperation> operations,
        int maxConcurrency,
        TimeSpan? timeout = null);
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
public sealed class FoundryConfiguration
{
    // Presets
    public static FoundryConfiguration Default();
    public static FoundryConfiguration Minimal();
    public static FoundryConfiguration HighPerformance();
    public static FoundryConfiguration ForHighPerformance();
    public static FoundryConfiguration Development();
    public static FoundryConfiguration ForDevelopment();
    public static FoundryConfiguration ForProduction();

    // Properties
    public TimeSpan DefaultTimeout { get; set; }
    public IWorkflowForgeLogger? Logger { get; set; }
    public IServiceProvider? ServiceProvider { get; set; }
    public int MaxRetryAttempts { get; set; }
    public bool EnableParallelExecution { get; set; }
    public int MaxDegreeOfParallelism { get; set; }
    public bool EnableDetailedTiming { get; set; }
    public bool AutoDisposeOperations { get; set; }
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

### IWorkflowOperationMiddleware

Middleware interface for cross-cutting concerns in operation execution.

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

Methods for configuring extensions.

```csharp
// Serilog logging (configuration-based)
public static FoundryConfiguration UseSerilog(this FoundryConfiguration config);
public static FoundryConfiguration UseSerilog(this FoundryConfiguration config, ILogger logger);

// Resilience (Polly) on foundry
public static IWorkflowFoundry UsePollyRetry(this IWorkflowFoundry foundry, int maxRetryAttempts = 3, TimeSpan? baseDelay = null, TimeSpan? maxDelay = null);
public static IWorkflowFoundry UsePollyCircuitBreaker(this IWorkflowFoundry foundry, int failureThreshold = 5, TimeSpan? durationOfBreak = null);
public static IWorkflowFoundry UsePollyTimeout(this IWorkflowFoundry foundry, TimeSpan timeout);
public static IWorkflowFoundry UsePollyComprehensive(this IWorkflowFoundry foundry, int maxRetryAttempts = 3, TimeSpan? baseDelay = null, int circuitBreakerThreshold = 5, TimeSpan? circuitBreakerDuration = null, TimeSpan? timeoutDuration = null);
public static IWorkflowFoundry UsePollyFromSettings(this IWorkflowFoundry foundry, PollySettings settings);
public static IWorkflowFoundry UsePollyEnterpriseResilience(this IWorkflowFoundry foundry);
public static IWorkflowFoundry UsePollyDevelopmentResilience(this IWorkflowFoundry foundry);
public static IWorkflowFoundry UsePollyProductionResilience(this IWorkflowFoundry foundry);
public static IWorkflowFoundry UsePollyMinimalResilience(this IWorkflowFoundry foundry);

// Performance monitoring on foundry
public static bool EnablePerformanceMonitoring(this IWorkflowFoundry foundry);
public static IFoundryPerformanceStatistics? GetPerformanceStatistics(this IWorkflowFoundry foundry);

// Health checks on foundry
public static HealthCheckService CreateHealthCheckService(this IWorkflowFoundry foundry, TimeSpan? checkInterval = null);
public static Task<HealthStatus> CheckFoundryHealthAsync(this IWorkflowFoundry foundry, HealthCheckService service, CancellationToken cancellationToken = default);

// OpenTelemetry on foundry
public static bool EnableOpenTelemetry(this IWorkflowFoundry foundry, WorkflowForgeOpenTelemetryOptions? options = null);
public static bool DisableOpenTelemetry(this IWorkflowFoundry foundry);

// Persistence (BYO storage) on foundry
public static IWorkflowFoundry UsePersistence(this IWorkflowFoundry foundry, IWorkflowPersistenceProvider provider);
public static IWorkflowFoundry UsePersistence(this IWorkflowFoundry foundry, IWorkflowPersistenceProvider provider, PersistenceOptions options);

// Recovery (resume + retry)
public static Task ForgeWithRecoveryAsync(this IWorkflowSmith smith,
    IWorkflow workflow,
    IWorkflowFoundry foundry,
    IWorkflowPersistenceProvider provider,
    Guid foundryKey,
    Guid workflowKey,
    RecoveryPolicy? policy = null,
    CancellationToken cancellationToken = default);

// Recovery coordinator and catalog (multi-resume)
public interface IRecoveryCatalog
{
    Task<IReadOnlyList<WorkflowExecutionSnapshot>> ListPendingAsync(CancellationToken cancellationToken = default);
}

public interface IRecoveryCoordinator
{
    Task ResumeAsync(
        Func<IWorkflowFoundry> foundryFactory,
        Func<IWorkflow> workflowFactory,
        Guid foundryKey,
        Guid workflowKey,
        CancellationToken cancellationToken = default);

    Task<int> ResumeAllAsync(
        Func<IWorkflowFoundry> foundryFactory,
        Func<IWorkflow> workflowFactory,
        IRecoveryCatalog catalog,
        CancellationToken cancellationToken = default);
}
```

## WorkflowForge.Extensions.Validation (NEW in 2.0.0)

### IWorkflowValidator<T>

```csharp
public interface IWorkflowValidator<T>
{
    Task<ValidationResult> ValidateAsync(T data, CancellationToken cancellationToken = default);
}
```

### Extension Methods

```csharp
public static IWorkflowFoundry AddValidation<T>(
    this IWorkflowFoundry foundry,
    IValidator<T> validator,
    Func<IWorkflowFoundry, T> dataExtractor,
    bool throwOnFailure = true);

public static Task<ValidationResult> ValidateAsync<T>(
    this IWorkflowFoundry foundry,
    IValidator<T> validator,
    T data,
    CancellationToken cancellationToken = default);
```

## WorkflowForge.Extensions.Audit (NEW in 2.0.0)

### IAuditProvider

```csharp
public interface IAuditProvider
{
    Task WriteAuditEntryAsync(AuditEntry entry, CancellationToken cancellationToken = default);
    Task FlushAsync(CancellationToken cancellationToken = default);
}
```

### AuditEntry

```csharp
public class AuditEntry
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; }
    public string WorkflowName { get; set; }
    public string? OperationName { get; set; }
    public string UserId { get; set; }
    public string SessionId { get; set; }
    public Dictionary<string, object?>? Properties { get; set; }
    public string? ExceptionMessage { get; set; }
    public TimeSpan? Duration { get; set; }
}
```

### Extension Methods

```csharp
public static IWorkflowFoundry EnableAuditLogging(
    this IWorkflowFoundry foundry,
    IAuditProvider auditProvider,
    string userId,
    string sessionId);
```

## Event Interfaces (Refactored in 2.0.0)

**Breaking Change from 1.x**: Single `IWorkflowEvents` split into three focused interfaces.

### IWorkflowLifecycleEvents (IWorkflowSmith)

```csharp
event EventHandler<WorkflowStartedEventArgs> WorkflowStarted;
event EventHandler<WorkflowCompletedEventArgs> WorkflowCompleted;
event EventHandler<WorkflowFailedEventArgs> WorkflowFailed;
```

### IOperationLifecycleEvents (IWorkflowFoundry)

```csharp
event EventHandler<OperationStartedEventArgs> OperationStarted;
event EventHandler<OperationCompletedEventArgs> OperationCompleted;
event EventHandler<OperationFailedEventArgs> OperationFailed;
```

### ICompensationLifecycleEvents (IWorkflowSmith)

```csharp
event EventHandler<CompensationTriggeredEventArgs> CompensationTriggered;
event EventHandler<OperationRestoreStartedEventArgs> OperationRestoreStarted;
event EventHandler<OperationRestoreCompletedEventArgs> OperationRestoreCompleted;
event EventHandler<OperationRestoreFailed EventArgs> OperationRestoreFailed;
event EventHandler<CompensationCompletedEventArgs> CompensationCompleted;
```

---

*Complete API reference for WorkflowForge* 