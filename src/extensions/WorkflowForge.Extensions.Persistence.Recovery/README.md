# WorkflowForge.Extensions.Persistence.Recovery

Adds orchestration helpers to resume persisted workflows from the last checkpoint, optionally with retry/backoff.

## Installation

```bash
dotnet add package WorkflowForge.Extensions.Persistence.Recovery
```

## Zero Dependencies - Zero Conflicts

**This extension has ZERO external dependencies.** This means:

- **NO DLL Hell** - No third-party dependencies to conflict with  
- **NO Conflicts** - Works with any versions of your application dependencies  
- **Clean Deployment** - Pure WorkflowForge extension with no baggage

**Lightweight architecture**: Built entirely on WorkflowForge core and Persistence abstractions with no external libraries.

## Quick Start

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

