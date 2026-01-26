WorkflowForge Persistence Recovery Extension

Adds orchestration helpers to resume persisted workflows from the last checkpoint, optionally with retry/backoff.

Install

```bash
dotnet add package WorkflowForge.Extensions.Persistence.Recovery
```

Quick start

```csharp
using WorkflowForge;
using WorkflowForge.Extensions.Persistence.Abstractions;
using WorkflowForge.Extensions.Persistence.Recovery;

// Your provider must be shared (DB/cache) used also by the runtime persistence middleware
IWorkflowPersistenceProvider provider = new MyProvider(...);

var coordinator = new RecoveryCoordinator(provider, new RecoveryPolicy
{
    MaxAttempts = 3,
    BaseDelay = TimeSpan.FromSeconds(1),
    UseExponentialBackoff = true
});

await coordinator.ResumeAsync(
    foundryFactory: () => WorkflowForge.CreateFoundry("OrderService"),
    workflowFactory: BuildProcessOrderWorkflow,
    foundryKey: stableFoundryKey,
    workflowKey: stableWorkflowKey);
```

Notes

- Combine with `PersistenceOptions` (core persistence) to ensure stable keys across processes.
- Keep workflow operation order stable; recovery uses operation indices.
- Ensure necessary state is placed in `foundry.Properties` so downstream steps can rehydrate when prior steps are skipped.

