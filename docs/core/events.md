# WorkflowForge Event System

Comprehensive guide to the WorkflowForge event system for workflow monitoring and observability.

---

## Table of Contents

- [Overview](#overview)
- [Event System Design](#event-system-design)
- [Event Interfaces](#event-interfaces)
- [Event Args](#event-args)
- [Subscribing to Events](#subscribing-to-events)
- [Event Patterns](#event-patterns)
- [Best Practices](#best-practices)

---

## Overview

WorkflowForge provides a comprehensive event system for monitoring workflow execution, tracking operations, and handling compensation scenarios. The event system follows the **Single Responsibility Principle (SRP)** with three focused interfaces.

### Why Events?

- **Observability**: Monitor workflow execution in real-time
- **Auditing**: Track all workflow activities for compliance
- **Error Handling**: React to failures and compensation
- **Metrics**: Collect performance data
- **Integration**: Connect to external monitoring systems

---

## Event System Design

### SRP-Compliant Architecture

WorkflowForge evolved from a single monolithic event interface to three focused interfaces, each with a single responsibility:

```
Old Design (Violates SRP):
IWorkflowEvents
├── WorkflowStarted
├── WorkflowCompleted
├── OperationStarted
├── OperationCompleted
├── CompensationTriggered
└── ... (all events mixed together)

New Design (SRP-Compliant):
├── IWorkflowLifecycleEvents    (Workflow-level events)
├── IOperationLifecycleEvents   (Operation-level events)
└── ICompensationLifecycleEvents (Compensation events)
```

### Interface Mapping

| Component | Implements | Events Exposed |
|-----------|------------|----------------|
| `IWorkflowSmith` | `IWorkflowLifecycleEvents`, `ICompensationLifecycleEvents` | Workflow and compensation events |
| `IWorkflowFoundry` | `IOperationLifecycleEvents` | Operation execution events |

**Design Rationale**:
- Smith manages workflows → fires workflow events
- Foundry hosts operations → fires operation events
- Compensation is workflow-level concern → smith fires compensation events

---

## Event Interfaces

### 1. IWorkflowLifecycleEvents

Workflow-level lifecycle events.

```csharp
public interface IWorkflowLifecycleEvents
{
    event EventHandler<WorkflowStartedEventArgs>? WorkflowStarted;
    event EventHandler<WorkflowCompletedEventArgs>? WorkflowCompleted;
    event EventHandler<WorkflowFailedEventArgs>? WorkflowFailed;
}
```

**Implemented By**: `IWorkflowSmith`

**When Events Fire**:
- `WorkflowStarted`: Before first operation executes
- `WorkflowCompleted`: After all operations complete successfully
- `WorkflowFailed`: When workflow execution fails

### 2. IOperationLifecycleEvents

Operation-level lifecycle events.

```csharp
public interface IOperationLifecycleEvents
{
    event EventHandler<OperationStartedEventArgs>? OperationStarted;
    event EventHandler<OperationCompletedEventArgs>? OperationCompleted;
    event EventHandler<OperationFailedEventArgs>? OperationFailed;
}
```

**Implemented By**: `IWorkflowFoundry`

**When Events Fire**:
- `OperationStarted`: Before operation executes
- `OperationCompleted`: After operation completes successfully
- `OperationFailed`: When operation execution fails

### 3. ICompensationLifecycleEvents

Compensation (rollback) lifecycle events.

```csharp
public interface ICompensationLifecycleEvents
{
    event EventHandler<CompensationTriggeredEventArgs>? CompensationTriggered;
    event EventHandler<CompensationCompletedEventArgs>? CompensationCompleted;
    event EventHandler<OperationRestoreStartedEventArgs>? OperationRestoreStarted;
    event EventHandler<OperationRestoreCompletedEventArgs>? OperationRestoreCompleted;
    event EventHandler<OperationRestoreFailedEventArgs>? OperationRestoreFailed;
}
```

**Implemented By**: `IWorkflowSmith`

**When Events Fire**:
- `CompensationTriggered`: When workflow failure triggers compensation
- `CompensationCompleted`: After all compensations complete
- `OperationRestoreStarted`: Before each operation's RestoreAsync executes
- `OperationRestoreCompleted`: After successful operation restoration
- `OperationRestoreFailed`: When operation restoration fails

---

## Event Args

All event arguments inherit from `BaseWorkflowForgeEventArgs`:

```csharp
public abstract class BaseWorkflowForgeEventArgs : EventArgs
{
    public Guid ExecutionId { get; }
    public string WorkflowName { get; }
    public DateTimeOffset Timestamp { get; }
}
```

### Workflow Event Args

**WorkflowStartedEventArgs**:
```csharp
public class WorkflowStartedEventArgs : BaseWorkflowForgeEventArgs
{
    public Guid WorkflowId { get; }
    public int OperationCount { get; }
}
```

**WorkflowCompletedEventArgs**:
```csharp
public class WorkflowCompletedEventArgs : BaseWorkflowForgeEventArgs
{
    public Guid WorkflowId { get; }
    public TimeSpan Duration { get; }
    public int OperationsExecuted { get; }
}
```

**WorkflowFailedEventArgs**:
```csharp
public class WorkflowFailedEventArgs : BaseWorkflowForgeEventArgs
{
    public Guid WorkflowId { get; }
    public Exception Exception { get; }
    public string? FailedOperationName { get; }
}
```

### Operation Event Args

**OperationStartedEventArgs**:
```csharp
public class OperationStartedEventArgs : BaseWorkflowForgeEventArgs
{
    public Guid OperationId { get; }
    public string OperationName { get; }
    public int OperationIndex { get; }
}
```

**OperationCompletedEventArgs**:
```csharp
public class OperationCompletedEventArgs : BaseWorkflowForgeEventArgs
{
    public Guid OperationId { get; }
    public string OperationName { get; }
    public TimeSpan Duration { get; }
    public int OperationIndex { get; }
}
```

**OperationFailedEventArgs**:
```csharp
public class OperationFailedEventArgs : BaseWorkflowForgeEventArgs
{
    public Guid OperationId { get; }
    public string OperationName { get; }
    public Exception Exception { get; }
    public int OperationIndex { get; }
}
```

### Compensation Event Args

**CompensationTriggeredEventArgs**:
```csharp
public class CompensationTriggeredEventArgs : BaseWorkflowForgeEventArgs
{
    public Guid WorkflowId { get; }
    public int OperationsToCompensate { get; }
    public Exception TriggeringException { get; }
}
```

**OperationRestoreStartedEventArgs**:
```csharp
public class OperationRestoreStartedEventArgs : BaseWorkflowForgeEventArgs
{
    public Guid OperationId { get; }
    public string OperationName { get; }
}
```

**OperationRestoreCompletedEventArgs**:
```csharp
public class OperationRestoreCompletedEventArgs : BaseWorkflowForgeEventArgs
{
    public Guid OperationId { get; }
    public string OperationName { get; }
    public TimeSpan Duration { get; }
}
```

---

## Subscribing to Events

### Basic Subscription

```csharp
using var smith = WorkflowForge.CreateSmith();

// Subscribe to workflow events
smith.WorkflowStarted += (sender, e) => {
    Console.WriteLine($"Workflow {e.WorkflowName} started at {e.Timestamp}");
};

smith.WorkflowCompleted += (sender, e) => {
    Console.WriteLine($"Workflow completed in {e.Duration.TotalMilliseconds}ms");
};

smith.WorkflowFailed += (sender, e) => {
    Console.WriteLine($"Workflow failed: {e.Exception.Message}");
};

// Execute workflow
await smith.ForgeAsync(workflow);
```

### Monitoring Operations

```csharp
using var foundry = WorkflowForge.CreateFoundry("MonitoredWorkflow");

// Subscribe to operation events
foundry.OperationStarted += (sender, e) => {
    Console.WriteLine($"Operation {e.OperationName} started (#{e.OperationIndex})");
};

foundry.OperationCompleted += (sender, e) => {
    Console.WriteLine($"Operation {e.OperationName} completed in {e.Duration.TotalMilliseconds}ms");
};

foundry.OperationFailed += (sender, e) => {
    Console.WriteLine($"Operation {e.OperationName} failed: {e.Exception.Message}");
};

// Execute workflow with foundry
using var smith = WorkflowForge.CreateSmith();
await smith.ForgeAsync(workflow, foundry);
```

### Monitoring Compensation

```csharp
using var smith = WorkflowForge.CreateSmith();

// Subscribe to compensation events
smith.CompensationTriggered += (sender, e) => {
    Console.WriteLine($"Compensation triggered: {e.OperationsToCompensate} operations to rollback");
    Console.WriteLine($"Cause: {e.TriggeringException.Message}");
};

smith.OperationRestoreStarted += (sender, e) => {
    Console.WriteLine($"Rolling back operation: {e.OperationName}");
};

smith.OperationRestoreCompleted += (sender, e) => {
    Console.WriteLine($"Operation {e.OperationName} rolled back in {e.Duration.TotalMilliseconds}ms");
};

smith.CompensationCompleted += (sender, e) => {
    Console.WriteLine("All compensation completed");
};

await smith.ForgeAsync(workflow);
```

---

## Event Patterns

### Pattern 1: Centralized Event Logger

```csharp
public class WorkflowEventLogger
{
    private readonly ILogger _logger;
    
    public void AttachToSmith(IWorkflowSmith smith)
    {
        smith.WorkflowStarted += OnWorkflowStarted;
        smith.WorkflowCompleted += OnWorkflowCompleted;
        smith.WorkflowFailed += OnWorkflowFailed;
        smith.CompensationTriggered += OnCompensationTriggered;
        smith.CompensationCompleted += OnCompensationCompleted;
    }
    
    public void AttachToFoundry(IWorkflowFoundry foundry)
    {
        foundry.OperationStarted += OnOperationStarted;
        foundry.OperationCompleted += OnOperationCompleted;
        foundry.OperationFailed += OnOperationFailed;
    }
    
    private void OnWorkflowStarted(object? sender, WorkflowStartedEventArgs e)
    {
        _logger.LogInformation(
            "Workflow started: {WorkflowName} ({ExecutionId}) with {OperationCount} operations",
            e.WorkflowName, e.ExecutionId, e.OperationCount);
    }
    
    // ... other event handlers
}

// Usage
var eventLogger = new WorkflowEventLogger(logger);
var smith = WorkflowForge.CreateSmith();
var foundry = WorkflowForge.CreateFoundry("MyWorkflow");

eventLogger.AttachToSmith(smith);
eventLogger.AttachToFoundry(foundry);

await smith.ForgeAsync(workflow, foundry);
```

### Pattern 2: Performance Metrics Collection

```csharp
public class WorkflowMetrics
{
    private readonly List<(string Name, TimeSpan Duration)> _operationMetrics = new();
    private DateTimeOffset _workflowStart;
    
    public void AttachToEvents(IWorkflowSmith smith, IWorkflowFoundry foundry)
    {
        smith.WorkflowStarted += (s, e) => _workflowStart = e.Timestamp;
        smith.WorkflowCompleted += (s, e) => LogWorkflowMetrics(e);
        
        foundry.OperationCompleted += (s, e) => {
            _operationMetrics.Add((e.OperationName, e.Duration));
        };
    }
    
    private void LogWorkflowMetrics(WorkflowCompletedEventArgs e)
    {
        var totalDuration = e.Timestamp - _workflowStart;
        var slowestOp = _operationMetrics.OrderByDescending(m => m.Duration).FirstOrDefault();
        
        Console.WriteLine($"Workflow Metrics:");
        Console.WriteLine($"  Total Duration: {totalDuration.TotalMilliseconds}ms");
        Console.WriteLine($"  Operations: {_operationMetrics.Count}");
        Console.WriteLine($"  Slowest: {slowestOp.Name} ({slowestOp.Duration.TotalMilliseconds}ms)");
    }
}
```

### Pattern 3: Audit Trail

```csharp
public class WorkflowAuditTrail
{
    private readonly IAuditRepository _repository;
    
    public void AttachToEvents(IWorkflowSmith smith, IWorkflowFoundry foundry)
    {
        smith.WorkflowStarted += async (s, e) => {
            await _repository.LogAsync(new AuditEntry {
                EventType = "WorkflowStarted",
                WorkflowName = e.WorkflowName,
                ExecutionId = e.ExecutionId,
                Timestamp = e.Timestamp
            });
        };
        
        foundry.OperationCompleted += async (s, e) => {
            await _repository.LogAsync(new AuditEntry {
                EventType = "OperationCompleted",
                OperationName = e.OperationName,
                Duration = e.Duration,
                Timestamp = e.Timestamp
            });
        };
        
        smith.WorkflowFailed += async (s, e) => {
            await _repository.LogAsync(new AuditEntry {
                EventType = "WorkflowFailed",
                Error = e.Exception.Message,
                FailedOperation = e.FailedOperationName,
                Timestamp = e.Timestamp
            });
        };
    }
}
```

### Pattern 4: Error Notification

```csharp
public class ErrorNotificationHandler
{
    private readonly INotificationService _notificationService;
    
    public void AttachToEvents(IWorkflowSmith smith, IWorkflowFoundry foundry)
    {
        smith.WorkflowFailed += async (s, e) => {
            await _notificationService.SendAsync(new Notification {
                Title = $"Workflow Failed: {e.WorkflowName}",
                Message = $"Error: {e.Exception.Message}",
                Severity = NotificationSeverity.Critical
            });
        };
        
        foundry.OperationFailed += async (s, e) => {
            await _notificationService.SendAsync(new Notification {
                Title = $"Operation Failed: {e.OperationName}",
                Message = $"Error: {e.Exception.Message}",
                Severity = NotificationSeverity.High
            });
        };
        
        smith.CompensationTriggered += async (s, e) => {
            await _notificationService.SendAsync(new Notification {
                Title = "Compensation Triggered",
                Message = $"Rolling back {e.OperationsToCompensate} operations",
                Severity = NotificationSeverity.High
            });
        };
    }
}
```

---

## Best Practices

### 1. Always Unsubscribe

```csharp
// Good: Using statement ensures disposal
using var smith = WorkflowForge.CreateSmith();
smith.WorkflowCompleted += OnCompleted;
await smith.ForgeAsync(workflow);
// smith is disposed, events unsubscribed

// Alternative: Manual cleanup
var smith = WorkflowForge.CreateSmith();
try
{
    smith.WorkflowCompleted += OnCompleted;
    await smith.ForgeAsync(workflow);
}
finally
{
    smith.WorkflowCompleted -= OnCompleted;
    smith.Dispose();
}
```

### 2. Handle Exceptions in Event Handlers

```csharp
smith.WorkflowCompleted += (sender, e) => {
    try
    {
        // Your event handling logic
        UpdateMetrics(e);
    }
    catch (Exception ex)
    {
        // Log but don't throw - don't break workflow execution
        _logger.LogError(ex, "Error in event handler");
    }
};
```

### 3. Use Async Event Handlers Carefully

```csharp
// Events are synchronous - don't await in handlers unless necessary
smith.WorkflowCompleted += (sender, e) => {
    // Don't do this - blocks workflow completion
    // await LongRunningOperationAsync();
    
    // Do this instead - fire and forget
    _ = Task.Run(async () => {
        try
        {
            await LongRunningOperationAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in async event handler");
        }
    });
};
```

### 4. Collect Contextual Information

```csharp
foundry.OperationCompleted += (sender, e) => {
    var context = new {
        ExecutionId = e.ExecutionId,
        WorkflowName = e.WorkflowName,
        OperationName = e.OperationName,
        Duration = e.Duration,
        Timestamp = e.Timestamp,
        // Access foundry properties for additional context
        OrderId = foundry.Properties.TryGetValue("OrderId", out var id) ? id : null
    };
    
    _metrics.Record(context);
};
```

### 5. Create Reusable Event Handlers

```csharp
public static class WorkflowEventHandlers
{
    public static EventHandler<WorkflowStartedEventArgs> CreateStartLogger(ILogger logger)
    {
        return (sender, e) => logger.LogInformation(
            "Workflow {WorkflowName} started with {OperationCount} operations",
            e.WorkflowName, e.OperationCount);
    }
    
    public static EventHandler<WorkflowFailedEventArgs> CreateErrorLogger(ILogger logger)
    {
        return (sender, e) => logger.LogError(
            e.Exception,
            "Workflow {WorkflowName} failed at operation {OperationName}",
            e.WorkflowName, e.FailedOperationName);
    }
}

// Usage
smith.WorkflowStarted += WorkflowEventHandlers.CreateStartLogger(logger);
smith.WorkflowFailed += WorkflowEventHandlers.CreateErrorLogger(logger);
```

### 6. Use Events for Integration

```csharp
// Integrate with Application Insights
smith.WorkflowCompleted += (s, e) => {
    _telemetryClient.TrackEvent("WorkflowCompleted", new Dictionary<string, string> {
        ["WorkflowName"] = e.WorkflowName,
        ["Duration"] = e.Duration.TotalMilliseconds.ToString(),
        ["OperationCount"] = e.OperationsExecuted.ToString()
    });
};

// Integrate with Prometheus
foundry.OperationCompleted += (s, e) => {
    _operationDurationHistogram
        .WithLabels(e.WorkflowName, e.OperationName)
        .Observe(e.Duration.TotalSeconds);
};
```

---

## Next Steps

- **[Architecture](../architecture/overview.md)** - Understanding event system design
- **[Operations](operations.md)** - Creating operations that fire events
- **[Samples Guide](../getting-started/samples-guide.md)** - See events in action (Sample 11: Workflow Events)
- **[Extensions](../extensions/index.md)** - Audit and Performance extensions use events

---

**← Back to [Documentation Home](../index.md)**
