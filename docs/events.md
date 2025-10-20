# WorkflowForge Event System

## Overview

WorkflowForge provides a comprehensive event system based on the Single Responsibility Principle, allowing you to monitor and react to workflow execution at multiple levels.

## Event Architecture

### Three-Tier Event Model

WorkflowForge segregates events into three focused interfaces:

1. **IWorkflowLifecycleEvents** - Workflow-level events
2. **IOperationLifecycleEvents** - Operation-level events  
3. **ICompensationLifecycleEvents** - Compensation/rollback events

This architecture ensures consumers only subscribe to events relevant to their concerns.

### Event Sources

| Interface | Available On | Events | Use Case |
|-----------|--------------|--------|----------|
| IWorkflowLifecycleEvents | IWorkflowSmith | 3 | Monitor entire workflow execution |
| IOperationLifecycleEvents | IWorkflowFoundry | 3 | Track individual operation execution |
| ICompensationLifecycleEvents | IWorkflowSmith | 5 | Observe rollback/compensation behavior |

## Workflow Lifecycle Events

Subscribe via `IWorkflowSmith`:

```csharp
using var smith = WorkflowForge.CreateSmith();

smith.WorkflowStarted += (sender, e) =>
{
    Console.WriteLine($"Workflow started: {e.Foundry?.CurrentWorkflow?.Name}");
};

smith.WorkflowCompleted += (sender, e) =>
{
    Console.WriteLine($"Workflow completed in {e.Duration.TotalMilliseconds}ms");
    Console.WriteLine($"Final properties: {e.FinalProperties?.Count}");
};

smith.WorkflowFailed += (sender, e) =>
{
    Console.WriteLine($"Workflow failed: {e.Exception.Message}");
};
```

## Operation Lifecycle Events

Subscribe via `IWorkflowFoundry`:

```csharp
using var foundry = smith.CreateFoundry();

foundry.OperationStarted += (sender, e) =>
{
    Console.WriteLine($"Operation started: {e.Operation.Name}");
};

foundry.OperationCompleted += (sender, e) =>
{
    Console.WriteLine($"Operation completed: {e.Operation.Name} in {e.Duration.TotalMilliseconds}ms");
};

foundry.OperationFailed += (sender, e) =>
{
    Console.WriteLine($"Operation failed: {e.Operation.Name} - {e.Exception.Message}");
};
```

## Compensation Events

Subscribe via `IWorkflowSmith` for saga pattern monitoring:

```csharp
smith.CompensationTriggered += (sender, e) =>
{
    Console.WriteLine($"Compensation triggered: {e.Reason}");
    Console.WriteLine($"Failed operation: {e.FailedOperationName}");
};

smith.OperationRestoreStarted += (sender, e) =>
{
    Console.WriteLine($"Restoring: {e.Operation.Name}");
};

smith.OperationRestoreCompleted += (sender, e) =>
{
    Console.WriteLine($"Restored: {e.Operation.Name} in {e.Duration.TotalMilliseconds}ms");
};

smith.OperationRestoreFailed += (sender, e) =>
{
    Console.WriteLine($"Restore failed: {e.Operation.Name} - {e.Exception.Message}");
};

smith.CompensationCompleted += (sender, e) =>
{
    Console.WriteLine($"Compensation completed: {e.SuccessfulRestores} succeeded, {e.FailedRestores} failed");
};
```

## Complete Example

```csharp
using WorkflowForge;
using WorkflowForge.Abstractions;

public class WorkflowMonitor
{
    public async Task MonitorWorkflowAsync()
    {
        using var smith = WorkflowForge.CreateSmith();
        using var foundry = smith.CreateFoundry();

        // Subscribe to all event levels
        SubscribeToWorkflowEvents(smith);
        SubscribeToOperationEvents(foundry);
        SubscribeToCompensationEvents(smith);

        var workflow = WorkflowForge.CreateWorkflow()
            .WithName("MonitoredWorkflow")
            .AddOperation(new Step1Operation())
            .AddOperation(new Step2Operation())
            .AddOperation(new Step3Operation())
            .Build();

        try
        {
            await smith.ForgeAsync(workflow, foundry);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Workflow execution failed: {ex.Message}");
        }
    }

    private void SubscribeToWorkflowEvents(IWorkflowSmith smith)
    {
        smith.WorkflowStarted += (s, e) => 
            Log("Workflow", "Started", e.Foundry?.CurrentWorkflow?.Name ?? "Unknown");
        
        smith.WorkflowCompleted += (s, e) => 
            Log("Workflow", "Completed", $"Duration: {e.Duration.TotalMilliseconds}ms");
        
        smith.WorkflowFailed += (s, e) => 
            Log("Workflow", "Failed", e.Exception.Message);
    }

    private void SubscribeToOperationEvents(IWorkflowFoundry foundry)
    {
        foundry.OperationStarted += (s, e) => 
            Log("Operation", "Started", e.Operation.Name);
        
        foundry.OperationCompleted += (s, e) => 
            Log("Operation", "Completed", $"{e.Operation.Name} ({e.Duration.TotalMilliseconds}ms)");
        
        foundry.OperationFailed += (s, e) => 
            Log("Operation", "Failed", $"{e.Operation.Name}: {e.Exception.Message}");
    }

    private void SubscribeToCompensationEvents(IWorkflowSmith smith)
    {
        smith.CompensationTriggered += (s, e) => 
            Log("Compensation", "Triggered", $"Reason: {e.Reason}, Failed: {e.FailedOperationName}");
        
        smith.OperationRestoreStarted += (s, e) => 
            Log("Restore", "Started", e.Operation.Name);
        
        smith.OperationRestoreCompleted += (s, e) => 
            Log("Restore", "Completed", $"{e.Operation.Name} ({e.Duration.TotalMilliseconds}ms)");
        
        smith.OperationRestoreFailed += (s, e) => 
            Log("Restore", "Failed", $"{e.Operation.Name}: {e.Exception.Message}");
        
        smith.CompensationCompleted += (s, e) => 
            Log("Compensation", "Completed", $"Success: {e.SuccessfulRestores}, Failed: {e.FailedRestores}");
    }

    private void Log(string category, string action, string details)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{category}] {action}: {details}");
    }
}
```

## Event Argument Details

### WorkflowStartedEventArgs
- `IWorkflowFoundry Foundry` - The foundry executing the workflow
- `DateTimeOffset StartedAt` - When execution began

### WorkflowCompletedEventArgs
- `IWorkflowFoundry Foundry` - The foundry that executed the workflow
- `DateTimeOffset CompletedAt` - When execution completed
- `Dictionary<string, object?> FinalProperties` - Final workflow properties
- `TimeSpan Duration` - Total execution time

### WorkflowFailedEventArgs
- `IWorkflowFoundry Foundry` - The foundry that was executing
- `DateTimeOffset FailedAt` - When failure occurred
- `Exception Exception` - The exception that caused failure
- `string FailedOperationName` - Name of operation that failed
- `TimeSpan Duration` - Time until failure

### OperationStartedEventArgs
- `IWorkflowOperation Operation` - The operation starting
- `IWorkflowFoundry Foundry` - The execution foundry
- `object? InputData` - Input data (if any)

### OperationCompletedEventArgs
- `IWorkflowOperation Operation` - The completed operation
- `IWorkflowFoundry Foundry` - The execution foundry
- `object? InputData` - Input data (if any)
- `object? Result` - Operation result
- `TimeSpan Duration` - Execution time

### OperationFailedEventArgs
- `IWorkflowOperation Operation` - The failed operation
- `IWorkflowFoundry Foundry` - The execution foundry
- `object? InputData` - Input data (if any)
- `Exception Exception` - The exception
- `TimeSpan Duration` - Time until failure

### CompensationTriggeredEventArgs
- `IWorkflowFoundry Foundry` - The execution foundry
- `DateTimeOffset TriggeredAt` - When compensation started
- `string Reason` - Why compensation was triggered
- `string FailedOperationName` - Operation that failed
- `Exception? Exception` - Original exception (if any)

### CompensationCompletedEventArgs
- `IWorkflowFoundry Foundry` - The execution foundry
- `DateTimeOffset CompletedAt` - When compensation finished
- `int SuccessfulRestores` - Number of successful rollbacks
- `int FailedRestores` - Number of failed rollbacks
- `TimeSpan Duration` - Total compensation time

### OperationRestoreStartedEventArgs
- `IWorkflowOperation Operation` - Operation being restored
- `IWorkflowFoundry Foundry` - The execution foundry

### OperationRestoreCompletedEventArgs
- `IWorkflowOperation Operation` - Restored operation
- `IWorkflowFoundry Foundry` - The execution foundry
- `TimeSpan Duration` - Restore time

### OperationRestoreFailedEventArgs
- `IWorkflowOperation Operation` - Operation that failed to restore
- `IWorkflowFoundry Foundry` - The execution foundry
- `Exception Exception` - Restore exception
- `TimeSpan Duration` - Time until failure

## Best Practices

### 1. Event Handler Thread Safety

Event handlers may be called from workflow execution threads. Ensure thread-safe operations:

```csharp
private readonly ConcurrentQueue<string> _log = new();

smith.OperationCompleted += (s, e) =>
{
    _log.Enqueue($"Completed: {e.Operation.Name}");
};
```

### 2. Avoid Blocking Operations

Keep event handlers fast. For expensive operations, queue work:

```csharp
smith.WorkflowCompleted += async (s, e) =>
{
    await Task.Run(async () =>
    {
        await ExpensiveLoggingOperationAsync(e);
    });
};
```

### 3. Error Handling in Event Handlers

Always handle exceptions in event handlers to prevent disrupting workflow execution:

```csharp
smith.OperationFailed += (s, e) =>
{
    try
    {
        NotifyAdministrators(e.Exception);
    }
    catch (Exception ex)
    {
        // Log but don't throw
        Console.WriteLine($"Failed to notify: {ex.Message}");
    }
};
```

### 4. Unsubscribe When Done

Always unsubscribe to prevent memory leaks:

```csharp
void Handler(object? sender, WorkflowCompletedEventArgs e) { }

smith.WorkflowCompleted += Handler;
try
{
    await smith.ForgeAsync(workflow);
}
finally
{
    smith.WorkflowCompleted -= Handler;
}
```

## Use Cases

### Audit Logging
```csharp
var auditLog = new List<AuditEntry>();

smith.WorkflowStarted += (s, e) => 
    auditLog.Add(new AuditEntry("WorkflowStarted", e.StartedAt));

foundry.OperationCompleted += (s, e) => 
    auditLog.Add(new AuditEntry($"OperationCompleted: {e.Operation.Name}", DateTime.Now));
```

### Progress Monitoring
```csharp
var totalOperations = workflow.Operations.Count;
var completedOperations = 0;

foundry.OperationCompleted += (s, e) =>
{
    completedOperations++;
    var progress = (double)completedOperations / totalOperations * 100;
    Console.WriteLine($"Progress: {progress:F1}%");
};
```

### Performance Tracking
```csharp
var performanceMetrics = new ConcurrentDictionary<string, TimeSpan>();

foundry.OperationCompleted += (s, e) =>
{
    performanceMetrics[e.Operation.Name] = e.Duration;
};

smith.WorkflowCompleted += (s, e) =>
{
    var slowestOperation = performanceMetrics.OrderByDescending(x => x.Value).FirstOrDefault();
    Console.WriteLine($"Slowest: {slowestOperation.Key} ({slowestOperation.Value.TotalMilliseconds}ms)");
};
```

### Circuit Breaker
```csharp
var consecutiveFailures = 0;
var circuitBreakerThreshold = 3;

foundry.OperationFailed += (s, e) =>
{
    consecutiveFailures++;
    if (consecutiveFailures >= circuitBreakerThreshold)
    {
        Console.WriteLine("Circuit breaker triggered!");
        // Stop workflow execution
    }
};

foundry.OperationCompleted += (s, e) =>
{
    consecutiveFailures = 0; // Reset on success
};
```

## See Also

- [Architecture Overview](architecture.md)
- [Building Operations](operations.md)
- [Error Handling](error-handling.md)
- [Compensation Pattern](compensation.md)

