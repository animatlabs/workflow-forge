# WorkflowForge.Extensions.Persistence

Add resumable workflows to WorkflowForge via a pluggable provider without introducing external dependencies. You implement storage; we provide the middleware and hooks.

## Installation

```bash
dotnet add package WorkflowForge.Extensions.Persistence
```

## Zero Dependencies - Zero Conflicts

**This extension has ZERO external dependencies.** This means:

- **NO DLL Hell** - No third-party dependencies to conflict with  
- **NO Conflicts** - Works with any versions of your application dependencies  
- **Clean Deployment** - Pure WorkflowForge extension with no baggage

**Pluggable architecture**: Bring your own storage (database, file, cloud) via the `IWorkflowPersistenceProvider` interface.

## Quick Start

1) Implement the provider interface

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Persistence;

public sealed class MyPersistenceProvider : IWorkflowPersistenceProvider
{
    public Task SaveAsync(WorkflowExecutionSnapshot snapshot, CancellationToken ct = default)
    {
        // Serialize and save to your storage
        return Task.CompletedTask;
    }

    public Task<WorkflowExecutionSnapshot?> TryLoadAsync(Guid foundryExecutionId, Guid workflowId, CancellationToken ct = default)
    {
        // Load and deserialize snapshot if present
        return Task.FromResult<WorkflowExecutionSnapshot?>(null);
    }

    public Task DeleteAsync(Guid foundryExecutionId, Guid workflowId, CancellationToken ct = default)
    {
        // Remove snapshot when workflow completes
        return Task.CompletedTask;
    }
}
```

2) Enable persistence

```csharp
using WorkflowForge;
using WorkflowForge.Extensions; // UsePersistence extension

var foundry = WorkflowForge.CreateFoundry("OrderWorkflow");
foundry.UsePersistence(new MyPersistenceProvider());

// For cross-process resume, pass stable keys via options and use a shared provider (e.g., DB/cache)
foundry.UsePersistence(new MyPersistenceProvider(), new PersistenceOptions
{
    InstanceId = "order-service-west-1",
    WorkflowKey = "ProcessOrder-v1"
});
```

How it works

- After each operation completes, a snapshot is saved with the next operation index and foundry properties.
- On resume, properties are restored once and already-completed operations are skipped.
- When the last operation completes, the snapshot is deleted.

Notes

- The extension is zero-dependency; you bring the storage and serialization.
- Snapshots store only what you put in foundry `Properties`. Keep payloads minimal.
- To resume across processes or after restarts, use a provider backed by shared storage and provide stable keys via `PersistenceOptions`.

