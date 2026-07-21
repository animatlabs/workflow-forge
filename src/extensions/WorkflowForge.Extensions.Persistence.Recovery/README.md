# WorkflowForge.Extensions.Persistence.Recovery

After a failure or restart, pick up persisted checkpoints again: `RecoveryCoordinator` loads state, restores foundry properties, and retries with configurable backoff.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Persistence.Recovery.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Persistence.Recovery/)

## Install

```bash
dotnet add package WorkflowForge.Extensions.Persistence.Recovery
```

Targets .NET Standard 2.0 or later. You need a shared `IWorkflowPersistenceProvider` with the main persistence middleware.

## Quick Start

```csharp
using WorkflowForge;
using WorkflowForge.Extensions.Persistence.Abstractions;
using WorkflowForge.Extensions.Persistence.Recovery;
using WorkflowForge.Extensions.Persistence.Recovery.Options;

// Your IWorkflowPersistenceProvider implementation (shared with runtime persistence middleware)
IWorkflowPersistenceProvider provider = /* your provider */;

var coordinator = new RecoveryCoordinator(provider, new RecoveryMiddlewareOptions
{
    MaxRetryAttempts = 3,
    BaseDelay = TimeSpan.FromSeconds(1),
    UseExponentialBackoff = true
});

await coordinator.ResumeAsync(
    foundryFactory: () => WorkflowForge.CreateFoundry("OrderService"),
    workflowFactory: BuildProcessOrderWorkflow,
    foundryKey: stableFoundryKey,
    workflowKey: stableWorkflowKey);
```

## Key points

- Replays from the last saved `NextOperationIndex` and skips finished operations.
- Retries resume with exponential or fixed backoff; options match what you use at runtime.
- `IRecoveryCatalog` supports batch resume over many pending executions.
- Depends on WorkflowForge core and the persistence contracts only.

## Configuration

### Via appsettings.json

```json
{
  "WorkflowForge": {
    "Extensions": {
      "Recovery": {
        "Enabled": true,
        "MaxRetryAttempts": 3,
        "BaseDelay": "00:00:01",
        "UseExponentialBackoff": true,
        "AttemptResume": true,
        "LogRecoveryAttempts": true
      }
    }
  }
}
```

### Via code

```csharp
using WorkflowForge;
using WorkflowForge.Extensions.Persistence.Recovery.Options;

var smith = WorkflowForge.CreateSmith();
var options = new RecoveryMiddlewareOptions
{
    Enabled = true,
    MaxRetryAttempts = 3,
    BaseDelay = TimeSpan.FromSeconds(1),
    UseExponentialBackoff = true,
    AttemptResume = true,
    LogRecoveryAttempts = true
};

await smith.ForgeWithRecoveryAsync(
    workflow,
    foundry,
    provider,
    foundryKey,
    workflowKey,
    options);
```

### Via dependency injection

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Extensions.Persistence.Recovery;

services.AddRecoveryConfiguration(configuration);
var options = serviceProvider.GetRequiredService<IOptions<RecoveryMiddlewareOptions>>().Value;
```

[Recovery configuration](../../../docs/core/configuration.md)

## Usage patterns

### Single workflow recovery

```csharp
// Resume from last checkpoint
await coordinator.ResumeAsync(
    foundryFactory: () => WorkflowForge.CreateFoundry("OrderService"),
    workflowFactory: BuildProcessOrderWorkflow,
    foundryKey: stableFoundryKey,
    workflowKey: stableWorkflowKey);
```

### Batch recovery

```csharp
using WorkflowForge.Extensions.Persistence.Abstractions;

public class MyCatalog : IRecoveryCatalog
{
    private readonly IWorkflowPersistenceProvider _provider;

    public MyCatalog(IWorkflowPersistenceProvider provider)
    {
        _provider = provider;
    }

    public async Task<IReadOnlyList<WorkflowExecutionSnapshot>> ListPendingAsync(
        CancellationToken cancellationToken = default)
    {
        // Query your storage for pending workflows and return snapshots with FoundryExecutionId and WorkflowId
        return Array.Empty<WorkflowExecutionSnapshot>();
    }
}

var catalog = new MyCatalog(provider);
int resumedCount = await coordinator.ResumeAllAsync(
    foundryFactory: () => WorkflowForge.CreateFoundry("BatchRecovery"),
    workflowFactory: BuildWorkflow,
    catalog: catalog);
```

## Stable keys and invariants

### Stable keys

**Critical**: Use stable, deterministic keys for foundry and workflow:

```csharp
// Good: Stable keys
var foundryKey = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
var workflowKey = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");

// Bad: Random keys (won't find saved state)
var foundryKey = Guid.NewGuid();  // Different every time!
```

### Operation order

Keep workflow operation order stable across versions:

```text
// Pseudocode illustrating operation ordering rules:

// Version 1
workflow: [ValidateOrder] → [ChargePayment]

// Version 2 - OK: Append new operations at the end
workflow: [ValidateOrder] → [ChargePayment] → [SendNotification]

// Version 2 - BAD: Reorder existing operations
workflow: [ChargePayment] → [ValidateOrder]  // Recovery will break!
```

### State restoration

Ensure necessary state is in `foundry.Properties`:

```csharp
// Good: Store state in properties
foundry.SetProperty("OrderId", orderId);
foundry.SetProperty("CustomerId", customerId);
foundry.SetProperty("PaymentId", paymentId);

// Operations can access this state after recovery
var orderId = foundry.GetPropertyOrDefault<string>("OrderId");
```

## Recovery flow

1. **Load Snapshot**: read state from the provider
2. **Restore Properties**: copy saved keys into the foundry
3. **Skip Completed**: begin at `NextOperationIndex`
4. **Resume Execution**: run remaining operations
5. **Retry on Failure**: `RecoveryMiddlewareOptions` controls backoff and attempts

## Error handling

```csharp
try
{
    await coordinator.ResumeAsync(...);
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to resume workflow after exhausting retries");
    // Handle permanent failure
}
```

## Links

- [Getting Started](../../../docs/getting-started/getting-started.md)
- [Configuration Guide](../../../docs/core/configuration.md)
- [Extensions Overview](../../../docs/extensions/index.md)
- [Persistence Extension](../WorkflowForge.Extensions.Persistence/README.md)
- [Sample 21: Recovery](../../samples/WorkflowForge.Samples.BasicConsole/README.md)
