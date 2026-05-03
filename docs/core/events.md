---
title: Event System
description: Subscribe to workflow, operation, and compensation lifecycle events for logging, metrics, and audits.
---

# WorkflowForge Event System

Smith and foundry events for runs, ops, and compensation.

---

## Table of Contents

- [Overview](#overview)
- [Event System Design](#event-system-design)
- [Event Interfaces](#event-interfaces)
- [Event Args](#event-args)
- [Subscribing to Events](#subscribing-to-events)
- [Event Patterns](#event-patterns)
- [Guidelines](#guidelines)

---

## Overview

Workflow-, operation-, and compensation-level hooks live on different surfaces so subscribers stay small.

### Why events?

- **Observability**: See runs as they happen
- **Auditing**: Trails for compliance
- **Errors**: Inspect failures and rollbacks
- **Metrics**: Durations and counts from args
- **Integrations**: Forward to APM, queues, dashboards

---

## Event System Design

### Three interfaces

`IWorkflowEvents` mixed every hook together. WorkflowForge 2.x split that into three contracts:

```
Old Design:
IWorkflowEvents
├── WorkflowStarted
├── WorkflowCompleted
├── OperationStarted
├── OperationCompleted
├── CompensationTriggered
└── ... (all events mixed together)

New Design:
├── IWorkflowLifecycleEvents    (Workflow-level events)
├── IOperationLifecycleEvents   (Operation-level events)
└── ICompensationLifecycleEvents (Compensation events)
```

### Who raises what

| Component | Implements | Events Exposed |
|-----------|------------|----------------|
| `IWorkflowSmith` | `IWorkflowLifecycleEvents`, `ICompensationLifecycleEvents` | Workflow and compensation events |
| `IWorkflowFoundry` | `IOperationLifecycleEvents` | Operation execution events |

**Split reasoning**:
- `IWorkflowSmith` runs the graph: workflow + compensation events.
- `IWorkflowFoundry` runs each operation: operation events.
- Compensation stays at workflow scope on the smith.

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

- `WorkflowStarted` runs before the first operation.
- `WorkflowCompleted` after a successful finish.
- `WorkflowFailed` when execution fails.

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

| Event | When |
|-------|------|
| `OperationStarted` | Immediately before the operation body runs |
| `OperationCompleted` | After the operation returns without throwing |
| `OperationFailed` | After the operation throws |

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

**Compensation and restore events**

- **`CompensationTriggered`**: Failure started compensation.
- **`CompensationCompleted`**: All restore attempts finished; args include success and failure counts.
- **`OperationRestoreStarted`**: Before each completed operation's `RestoreAsync` (every completed op is attempted; non-restorable ops use the base no-op).
- **`OperationRestoreCompleted`**: That operation's restore succeeded.
- **`OperationRestoreFailed`**: That operation's restore threw.

---

## Event Args

All event arguments inherit from `BaseWorkflowForgeEventArgs`:

```csharp
public abstract class BaseWorkflowForgeEventArgs : EventArgs
{
    public IWorkflowFoundry Foundry { get; }
    public DateTimeOffset Timestamp { get; }
}
```

### Workflow Event Args

**WorkflowStartedEventArgs**:
```csharp
public class WorkflowStartedEventArgs : BaseWorkflowForgeEventArgs
{
    public DateTimeOffset StartedAt { get; }
}
```

**WorkflowCompletedEventArgs**:
```csharp
public class WorkflowCompletedEventArgs : BaseWorkflowForgeEventArgs
{
    public DateTimeOffset CompletedAt { get; }
    public IReadOnlyDictionary<string, object?> FinalProperties { get; }
    public TimeSpan Duration { get; }
}
```

**WorkflowFailedEventArgs**:
```csharp
public class WorkflowFailedEventArgs : BaseWorkflowForgeEventArgs
{
    public DateTimeOffset FailedAt { get; }
    public Exception? Exception { get; }
    public string FailedOperationName { get; }
    public TimeSpan Duration { get; }
}
```

### Operation Event Args

**OperationStartedEventArgs**:
```csharp
public class OperationStartedEventArgs : BaseWorkflowForgeEventArgs
{
    public IWorkflowOperation Operation { get; }
    public object? InputData { get; }
}
```

**OperationCompletedEventArgs**:
```csharp
public class OperationCompletedEventArgs : BaseWorkflowForgeEventArgs
{
    public IWorkflowOperation Operation { get; }
    public object? InputData { get; }
    public object? OutputData { get; }
    public TimeSpan Duration { get; }
}
```

**OperationFailedEventArgs**:
```csharp
public class OperationFailedEventArgs : BaseWorkflowForgeEventArgs
{
    public IWorkflowOperation Operation { get; }
    public object? InputData { get; }
    public Exception? Exception { get; }
    public TimeSpan Duration { get; }
}
```

### Compensation Event Args

**CompensationTriggeredEventArgs**:
```csharp
public class CompensationTriggeredEventArgs : BaseWorkflowForgeEventArgs
{
    public DateTimeOffset TriggeredAt { get; }
    public string Reason { get; }
    public string FailedOperationName { get; }
    public Exception? Exception { get; }
}
```

**OperationRestoreStartedEventArgs**:
```csharp
public class OperationRestoreStartedEventArgs : BaseWorkflowForgeEventArgs
{
    public IWorkflowOperation Operation { get; }
    public DateTimeOffset StartedAt { get; }
}
```

**OperationRestoreCompletedEventArgs**:
```csharp
public class OperationRestoreCompletedEventArgs : BaseWorkflowForgeEventArgs
{
    public IWorkflowOperation Operation { get; }
    public DateTimeOffset CompletedAt { get; }
    public TimeSpan Duration { get; }
}
```

### CompensationCompletedEventArgs

Raised when all compensation (restore) operations have completed.

| Property | Type | Description |
|----------|------|-------------|
| Foundry | IWorkflowFoundry | The workflow foundry instance |
| Timestamp | DateTimeOffset | When the event occurred |
| CompletedAt | DateTimeOffset | When compensation completed |
| SuccessCount | int | Number of successful restore operations |
| FailureCount | int | Number of failed restore operations |
| Duration | TimeSpan | Total compensation duration |

### OperationRestoreFailedEventArgs

Raised when an individual operation's restore (compensation) fails.

| Property | Type | Description |
|----------|------|-------------|
| Foundry | IWorkflowFoundry | The workflow foundry instance |
| Timestamp | DateTimeOffset | When the event occurred |
| Operation | IWorkflowOperation | The operation that failed to restore |
| FailedAt | DateTimeOffset | When the restore failed |
| Exception | Exception | The exception that caused the failure |
| Duration | TimeSpan | How long the restore attempt took |

---

## Subscribing to Events

### Basic Subscription

```csharp
using var smith = WorkflowForge.CreateSmith();

// Subscribe to workflow events
smith.WorkflowStarted += (sender, e) => {
    Console.WriteLine($"Workflow {e.Foundry.CurrentWorkflow?.Name} started at {e.Timestamp}");
};

smith.WorkflowCompleted += (sender, e) => {
    Console.WriteLine($"Workflow completed in {e.Duration.TotalMilliseconds}ms");
};

smith.WorkflowFailed += (sender, e) => {
    Console.WriteLine($"Workflow failed: {e.Exception?.Message}");
};

// Execute workflow
await smith.ForgeAsync(workflow);
```

### Monitoring Operations

```csharp
using var foundry = WorkflowForge.CreateFoundry("MonitoredWorkflow");

// Subscribe to operation events
foundry.OperationStarted += (sender, e) => {
    Console.WriteLine($"Operation {e.Operation.Name} started");
};

foundry.OperationCompleted += (sender, e) => {
    Console.WriteLine($"Operation {e.Operation.Name} completed in {e.Duration.TotalMilliseconds}ms");
};

foundry.OperationFailed += (sender, e) => {
    Console.WriteLine($"Operation {e.Operation.Name} failed: {e.Exception?.Message}");
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
    Console.WriteLine($"Compensation triggered: {e.Reason} (failed: {e.FailedOperationName})");
    Console.WriteLine($"Cause: {e.Exception?.Message}");
};

smith.OperationRestoreStarted += (sender, e) => {
    Console.WriteLine($"Rolling back operation: {e.Operation.Name}");
};

smith.OperationRestoreCompleted += (sender, e) => {
    Console.WriteLine($"Operation {e.Operation.Name} rolled back in {e.Duration.TotalMilliseconds}ms");
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
            e.Foundry.CurrentWorkflow?.Name, e.Foundry.ExecutionId, e.Foundry.CurrentWorkflow?.Operations.Count ?? 0);
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
            _operationMetrics.Add((e.Operation.Name, e.Duration));
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
                WorkflowName = e.Foundry.CurrentWorkflow?.Name,
                ExecutionId = e.Foundry.ExecutionId,
                Timestamp = e.Timestamp
            });
        };
        
        foundry.OperationCompleted += async (s, e) => {
            await _repository.LogAsync(new AuditEntry {
                EventType = "OperationCompleted",
                OperationName = e.Operation.Name,
                Duration = e.Duration,
                Timestamp = e.Timestamp
            });
        };
        
        smith.WorkflowFailed += async (s, e) => {
            await _repository.LogAsync(new AuditEntry {
                EventType = "WorkflowFailed",
                Error = e.Exception?.Message,
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
                Title = $"Workflow Failed: {e.Foundry.CurrentWorkflow?.Name}",
                Message = $"Error: {e.Exception?.Message}",
                Severity = NotificationSeverity.Critical
            });
        };
        
        foundry.OperationFailed += async (s, e) => {
            await _notificationService.SendAsync(new Notification {
                Title = $"Operation Failed: {e.Operation.Name}",
                Message = $"Error: {e.Exception?.Message}",
                Severity = NotificationSeverity.High
            });
        };
        
        smith.CompensationTriggered += async (s, e) => {
            await _notificationService.SendAsync(new Notification {
                Title = "Compensation Triggered",
                Message = $"Rolling back: {e.Reason} (failed: {e.FailedOperationName})",
                Severity = NotificationSeverity.High
            });
        };
    }
}
```

---

## Guidelines

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
        ExecutionId = e.Foundry.ExecutionId,
        WorkflowName = e.Foundry.CurrentWorkflow?.Name,
        OperationName = e.Operation.Name,
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
            e.Foundry.CurrentWorkflow?.Name, e.Foundry.CurrentWorkflow?.Operations.Count ?? 0);
    }
    
    public static EventHandler<WorkflowFailedEventArgs> CreateErrorLogger(ILogger logger)
    {
        return (sender, e) => logger.LogError(
            e.Exception,
            "Workflow {WorkflowName} failed at operation {OperationName}",
            e.Foundry.CurrentWorkflow?.Name, e.FailedOperationName);
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
        ["WorkflowName"] = e.Foundry.CurrentWorkflow?.Name ?? "",
        ["Duration"] = e.Duration.TotalMilliseconds.ToString(),
        ["PropertyCount"] = e.FinalProperties.Count.ToString()
    });
};

// Integrate with Prometheus
foundry.OperationCompleted += (s, e) => {
    _operationDurationHistogram
        .WithLabels(e.Foundry.CurrentWorkflow?.Name ?? "", e.Operation.Name)
        .Observe(e.Duration.TotalSeconds);
};
```

---

## Related Documentation

- [Architecture](../architecture/overview.md) - How smith, foundry, and workflows fit together
- [Operations](operations.md) - Creating operations that fire events
- [Samples Guide](../getting-started/samples-guide.md) - See events in action (Sample 11: Workflow Events)
- [Extensions](../extensions/index.md) - Audit and Performance extensions use events
