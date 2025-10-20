# WorkflowForge Samples Guide

## Running the Samples

All samples are located in `src/samples/WorkflowForge.Samples.BasicConsole/`.

```bash
cd src/samples/WorkflowForge.Samples.BasicConsole
dotnet run
```

Select a sample from the interactive menu to run it.

## Available Samples

### 1. Hello World (`HelloWorldSample.cs`)
**Difficulty**: Beginner  
**Concepts**: Basic workflow creation, simple operations, foundry execution

The simplest possible workflow with inline operations.

```csharp
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("HelloWorld")
    .AddOperation(LoggingOperation.Info("Hello, WorkflowForge!"))
    .Build();

using var foundry = WorkflowForge.CreateFoundry("HelloWorld");
await foundry.ForgeAsync();
```

### 2. Inline Operations (`InlineOperationsSample.cs`)
**Difficulty**: Beginner  
**Concepts**: ActionWorkflowOperation, inline lambdas, quick prototyping

Create operations on-the-fly without defining classes.

```csharp
.AddOperation(ActionWorkflowOperation.Create("ProcessData", async (foundry, ct) =>
{
    var data = foundry.GetPropertyOrDefault<string>("input");
    foundry.SetProperty("result", data.ToUpper());
}))
```

### 3. Operation Creation Patterns (`OperationCreationPatternsSample.cs`)
**Difficulty**: Beginner  
**Concepts**: Different ways to create operations, built-in helpers

Shows multiple patterns for creating operations:
- Class-based operations
- Inline operations
- Built-in operations (LoggingOperation, DelayOperation)
- Factory patterns

### 4. Data Passing (`DataPassingSample.cs`)
**Difficulty**: Intermediate  
**Concepts**: `foundry.Properties`, type-safe data access, data flow

**CRITICAL**: WorkflowForge uses dictionary-based data passing via `foundry.Properties`:

```csharp
// Store data
foundry.SetProperty("user_id", 12345);
foundry.SetProperty("email", "user@example.com");

// Retrieve data
var userId = foundry.GetPropertyOrDefault<int>("user_id");
var email = foundry.GetPropertyOrDefault<string>("email");
```

### 5. Conditional Workflows (`ConditionalWorkflowSample.cs`)
**Difficulty**: Intermediate  
**Concepts**: ConditionalWorkflowOperation, branching logic, decision trees

Execute different operations based on runtime conditions:

```csharp
.AddOperation(new ConditionalWorkflowOperation(
    name: "CheckEnvironment",
    condition: foundry => foundry.GetPropertyOrDefault<string>("env") == "production",
    trueOperation: new ProductionDeployOperation(),
    falseOperation: new StagingDeployOperation()
))
```

### 6. ForEach Loops (`ForEachLoopSample.cs`)
**Difficulty**: Intermediate  
**Concepts**: ForEachWorkflowOperation, parallel/sequential processing, collection handling

Process collections with parallel or sequential execution:

```csharp
.AddOperation(new ForEachWorkflowOperation<int>(
    name: "ProcessItems",
    items: Enumerable.Range(1, 100).ToList(),
    operation: new ProcessItemOperation(),
    maxDegreeOfParallelism: 10 // Process 10 items concurrently
))
```

### 7. Error Handling (`ErrorHandlingSample.cs`)
**Difficulty**: Intermediate  
**Concepts**: Exception handling, workflow cancellation, error recovery

Best practices for handling errors in workflows.

### 8. Workflow Events (`WorkflowEventsSample.cs`)
**Difficulty**: Intermediate  
**Concepts**: **SRP event interfaces**, event subscriptions, lifecycle monitoring

**NEW**: Uses properly segregated event interfaces:

```csharp
using var smith = WorkflowForge.CreateSmith();
using var foundry = smith.CreateFoundry();

// Workflow-level events (via smith)
smith.WorkflowStarted += (s, e) => Console.WriteLine("Workflow started");
smith.WorkflowCompleted += (s, e) => Console.WriteLine($"Completed in {e.Duration}");
smith.WorkflowFailed += (s, e) => Console.WriteLine($"Failed: {e.Exception.Message}");

// Operation-level events (via foundry)
foundry.OperationStarted += (s, e) => Console.WriteLine($"Operation: {e.Operation.Name}");
foundry.OperationCompleted += (s, e) => Console.WriteLine($"Done: {e.Operation.Name}");
foundry.OperationFailed += (s, e) => Console.WriteLine($"Failed: {e.Operation.Name}");
```

### 9. Middleware (`MiddlewareSample.cs`)
**Difficulty**: Advanced  
**Concepts**: IWorkflowOperationMiddleware, cross-cutting concerns, Russian Doll pattern

Add cross-cutting concerns like timing, logging, validation:

```csharp
foundry.AddMiddleware(new TimingMiddleware());
foundry.AddMiddleware(new ErrorHandlingMiddleware());
foundry.AddMiddleware(new RetryMiddleware());
```

**Order matters**: Middleware wraps in reverse order (Russian Doll pattern).

### 10. Multiple Outcomes (`MultipleOutcomesSample.cs`)
**Difficulty**: Advanced  
**Concepts**: Branching workflows, complex decision trees, outcome handling

Handle multiple execution paths based on results.

### 11. Configuration Profiles (`ConfigurationProfilesSample.cs`)
**Difficulty**: Intermediate  
**Concepts**: FoundryConfiguration, environment-specific settings

Configure foundries for different environments:

```csharp
var config = new FoundryConfiguration
{
    MaxOperationRetries = 3,
    OperationTimeout = TimeSpan.FromSeconds(30)
};

var foundry = WorkflowForge.CreateFoundry("Production", config);
```

### 12. Options Pattern (`OptionsPatternSample.cs`)
**Difficulty**: Intermediate  
**Concepts**: IOptions, configuration management, dependency injection

Use .NET Options pattern for configuration.

## Extension Samples

### 13. Serilog Integration (`SerilogIntegrationSample.cs`)
**Extension**: WorkflowForge.Extensions.Logging.Serilog  
**Concepts**: Structured logging, log enrichment, contextual logging

```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var logger = new SerilogWorkflowForgeLogger(Log.Logger);
var smith = WorkflowForge.CreateSmith(logger);
```

### 14. Polly Resilience (`PollyResilienceSample.cs`)
**Extension**: WorkflowForge.Extensions.Resilience.Polly  
**Concepts**: Retry policies, circuit breakers, timeouts

```csharp
foundry.AddMiddleware(new PollyRetryMiddleware(
    retryCount: 3,
    sleepDuration: TimeSpan.FromSeconds(1)
));

foundry.AddMiddleware(new PollyCircuitBreakerMiddleware(
    exceptionsAllowedBeforeBreaking: 5,
    durationOfBreak: TimeSpan.FromMinutes(1)
));
```

### 15. Persistence (`PersistenceSample.cs`)
**Extension**: WorkflowForge.Extensions.Persistence  
**Concepts**: Workflow state persistence, recovery, durability

Save workflow state for recovery:

```csharp
var persistence = new FilePersistenceProvider("./workflows");
foundry.AddMiddleware(new PersistenceMiddleware(persistence));
```

### 16. Recovery (`RecoveryOnlySample.cs`, `ResilienceRecoverySample.cs`)
**Extension**: WorkflowForge.Extensions.Persistence  
**Concepts**: Workflow recovery, state restoration, failure recovery

Recover failed workflows from persisted state.

### 17. Health Checks (`HealthChecksSample.cs`)
**Extension**: WorkflowForge.Extensions.Observability.HealthChecks  
**Concepts**: Application health monitoring, readiness checks

Integrate with ASP.NET Core health checks.

### 18. Performance Monitoring (`PerformanceMonitoringSample.cs`)
**Extension**: WorkflowForge.Extensions.Observability.Performance  
**Concepts**: Performance metrics, profiling, benchmarking

Track workflow performance metrics.

### 19. OpenTelemetry (`OpenTelemetryObservabilitySample.cs`)
**Extension**: WorkflowForge.Extensions.Observability.OpenTelemetry  
**Concepts**: Distributed tracing, spans, telemetry

Integrate with OpenTelemetry for distributed tracing.

### 20. Comprehensive Integration (`ComprehensiveIntegrationSample.cs`)
**Difficulty**: Advanced  
**Concepts**: All features combined, production-ready patterns

A complete example combining multiple extensions and patterns for production use.

### 21. Built-In Operations (`BuiltInOperationsSample.cs`)
**Difficulty**: Beginner  
**Concepts**: Core operations, delays, logging

Demonstrates the built-in operations available in WorkflowForge core.

### 22. Resilience Recovery (`ResilienceRecoverySample.cs`)
**Difficulty**: Advanced  
**Concepts**: Retry logic, persistence, state recovery

Combines resilience patterns with workflow recovery for fault-tolerant execution.

### 23. Validation (`ValidationSample.cs`)
**Difficulty**: Intermediate  
**Concepts**: Input validation, business rules, FluentValidation integration

Demonstrates comprehensive validation capabilities:
- **Automatic Validation**: Middleware-based validation that happens before operation execution
- **Manual Validation**: Explicit validation calls with detailed error handling
- **FluentValidation Integration**: Leverage FluentValidation's powerful rule builder
- **Error Handling**: Graceful handling of validation failures

```csharp
// Define validator
public class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required");
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be positive");
    }
}

// Automatic validation via middleware
foundry.AddValidation(
    new OrderValidator(),
    f => f.GetPropertyOrDefault<Order>("Order"),
    throwOnFailure: true);
```

**Key Scenarios**:
1. Basic manual validation with error reporting
2. Automatic middleware-based validation
3. Multiple validation stages (basic + business rules)
4. Validation with error handling (no exceptions)

### 24. Audit Logging (`AuditSample.cs`)
**Difficulty**: Intermediate  
**Concepts**: Audit trails, compliance logging, operational monitoring

Demonstrates comprehensive audit logging for compliance and monitoring:
- **Automatic Auditing**: Middleware captures all operation executions
- **Pluggable Storage**: Bring your own audit provider (database, file, external service)
- **In-Memory Provider**: Built-in provider for testing and development
- **Performance Tracking**: Automatic duration measurement for operations
- **User Context**: Track who initiated each workflow execution
- **Metadata Enrichment**: Capture additional context from workflow properties

```csharp
// Create audit provider
var auditProvider = new InMemoryAuditProvider();

// Enable audit logging with user context
foundry.EnableAudit(
    auditProvider,
    initiatedBy: "admin@company.com",
    includeMetadata: true);

// Query audit entries
foreach (var entry in auditProvider.Entries)
{
    Console.WriteLine($"[{entry.Timestamp:HH:mm:ss.fff}] " +
                     $"{entry.EventType} - {entry.OperationName}: " +
                     $"{entry.Status} ({entry.DurationMs}ms)");
}
```

**Key Scenarios**:
1. Basic audit logging with in-memory provider
2. Audit with user context tracking
3. Audit with metadata enrichment
4. Custom audit entries for manual events

## Sample Categories

### By Difficulty
- **Beginner**: HelloWorld, InlineOperations, OperationCreationPatterns, BuiltInOperations
- **Intermediate**: DataPassing, Conditional, ForEach, ErrorHandling, Events, Configuration, Options, Validation, Audit
- **Advanced**: Middleware, MultipleOutcomes, ResilienceRecovery, ComprehensiveIntegration

### By Concept
- **Core Concepts**: HelloWorld, DataPassing, ErrorHandling, BuiltInOperations
- **Control Flow**: Conditional, ForEach, MultipleOutcomes
- **Observability**: Events, Serilog, OpenTelemetry, PerformanceMonitoring, Audit
- **Resilience**: ErrorHandling, PollyResilience, Recovery, ResilienceRecovery
- **Persistence**: Persistence, Recovery, ResilienceRecovery
- **Validation & Compliance**: Validation, Audit
- **Patterns**: OperationCreationPatterns, Middleware, Options, Configuration

### By Extension
- **No Extensions**: HelloWorld, InlineOperations, DataPassing, Conditional, ForEach, ErrorHandling, Events, Middleware, BuiltInOperations
- **Logging**: Serilog
- **Resilience**: PollyResilience, Recovery, ResilienceRecovery
- **Validation**: Validation
- **Audit**: Audit
- **Persistence**: Persistence, Recovery, ResilienceRecovery
- **Observability**: HealthChecks, PerformanceMonitoring, OpenTelemetry

## Best Practices Demonstrated

### 1. Dictionary-Based Data Flow
All samples use `foundry.Properties` for data passing:
```csharp
foundry.SetProperty("key", value);
var value = foundry.GetPropertyOrDefault<T>("key");
```

### 2. SRP Event Pattern
Event samples use segregated interfaces:
- `IWorkflowSmith` for workflow-level events
- `IWorkflowFoundry` for operation-level events

### 3. Class-Based Operations (Enterprise)
Production samples use class-based operations:
```csharp
public class ProcessOrderOperation : IWorkflowOperation
{
    public string Name => "ProcessOrder";
    public bool SupportsRestore => false;
    
    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken ct)
    {
        var orderId = foundry.GetPropertyOrDefault<int>("order_id");
        // Process order
        foundry.SetProperty("processed", true);
        return null;
    }
    
    public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken ct)
        => throw new NotSupportedException();
    
    public void Dispose() { }
}
```

### 4. Middleware Ordering
Middleware is added in order of outer-to-inner wrapping:
```csharp
foundry.AddMiddleware(timingMiddleware);        // Outermost
foundry.AddMiddleware(errorHandlingMiddleware); // Middle
foundry.AddMiddleware(retryMiddleware);         // Innermost (closest to operation)
```

### 5. Resource Management
All samples properly dispose resources:
```csharp
using var smith = WorkflowForge.CreateSmith();
using var foundry = smith.CreateFoundry();
// Resources automatically disposed
```

## Next Steps

1. **Run the samples** to see WorkflowForge in action
2. **Read the source code** of samples relevant to your use case
3. **Modify samples** to experiment with different configurations
4. **Build your own workflows** using the patterns you learned

## Additional Resources

- [Getting Started Guide](getting-started.md)
- [Architecture Overview](architecture.md)
- [Event System Guide](events.md)
- [API Reference](api-reference.md)
- [Extensions Guide](extensions.md)

