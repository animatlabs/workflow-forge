# WorkflowForge.Extensions.Persistence.Recovery

<p align="center">
  <img src="https://raw.githubusercontent.com/animatlabs/workflow-forge/main/icon.png" alt="WorkflowForge" width="120" height="120">
</p>

Recovery orchestration extension for WorkflowForge to resume persisted workflows from the last checkpoint with configurable retry and backoff.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Persistence.Recovery.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Persistence.Recovery/)

## Zero Dependencies - Zero Conflicts

**This extension has ZERO external dependencies.** This means:

- NO DLL Hell - No third-party dependencies to conflict with
- NO Version Conflicts - Works with any versions of your application dependencies
- Clean Deployment - Pure WorkflowForge extension

**Lightweight architecture**: Built entirely on WorkflowForge core and Persistence abstractions.

## Installation

```bash
dotnet add package WorkflowForge.Extensions.Persistence.Recovery
```

**Requires**: .NET Standard 2.0 or later

## Quick Start

```csharp
using WorkflowForge;
using WorkflowForge.Extensions.Persistence.Abstractions;
using WorkflowForge.Extensions.Persistence.Recovery;

// Your provider must be shared (DB/cache) used also by the runtime persistence middleware
IWorkflowPersistenceProvider provider = new SQLitePersistenceProvider("workflows.db");

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

## Key Features

- **Resume from Checkpoint**: Continue workflows from last saved state
- **Exponential Backoff**: Configurable retry with backoff
- **Skip Completed**: Automatically skip already-completed operations
- **Catalog-Based**: Batch resume for multiple workflows
- **Minimal Retry**: Resilient recovery on resume failures
- **Zero Dependencies**: Pure WorkflowForge extension

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

### Via Code

```csharp
using WorkflowForge.Extensions.Persistence.Recovery.Options;

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

### Via Dependency Injection

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Extensions.Persistence.Recovery;

services.AddRecoveryConfiguration(configuration);
var options = serviceProvider.GetRequiredService<IOptions<RecoveryMiddlewareOptions>>().Value;
```

See [Configuration Guide](../../../docs/core/configuration.md#recovery-extension) for complete options.

## Usage Patterns

### Single Workflow Recovery

```csharp
// Resume from last checkpoint
await coordinator.ResumeAsync(
    foundryFactory: () => WorkflowForge.CreateFoundry("OrderService"),
    workflowFactory: BuildProcessOrderWorkflow,
    foundryKey: stableFoundryKey,
    workflowKey: stableWorkflowKey);
```

### Batch Recovery

```csharp
public class MyCatalog : IRecoveryCatalog
{
    private readonly IWorkflowPersistenceProvider _provider;
    
    public async Task<IReadOnlyList<WorkflowExecutionSnapshot>> ListPendingAsync(
        CancellationToken cancellationToken = default)
    {
        // Query your storage for pending workflows
        return await _database.QueryAsync("SELECT * FROM Workflows WHERE Status = 'Pending'");
    }
}

var catalog = new MyCatalog(provider);
int resumedCount = await coordinator.ResumeAllAsync(
    foundryFactory: () => WorkflowForge.CreateFoundry("BatchRecovery"),
    workflowFactory: BuildWorkflow,
    catalog: catalog);
```

## Important Notes

### Stable Keys

**Critical**: Use stable, deterministic keys for foundry and workflow:

```csharp
// Good: Stable keys
var foundryKey = Guid.Parse("FIXED-GUID-FOR-ORDER-SERVICE");
var workflowKey = Guid.Parse("FIXED-GUID-FOR-WORKFLOW-DEF");

// Bad: Random keys (won't find saved state)
var foundryKey = Guid.NewGuid();  // Different every time!
```

### Operation Order

Keep workflow operation order stable across versions:

```csharp
// Version 1
workflow
    .AddOperation("ValidateOrder")
    .AddOperation("ChargePayment")
    .Build();

// Version 2 - OK: Append new operations
workflow
    .AddOperation("ValidateOrder")
    .AddOperation("ChargePayment")
    .AddOperation("SendNotification")  // New operation
    .Build();

// Version 2 - BAD: Reorder existing operations
workflow
    .AddOperation("ChargePayment")      // Order changed!
    .AddOperation("ValidateOrder")      // Recovery will break
    .Build();
```

### State Restoration

Ensure necessary state is in `foundry.Properties`:

```csharp
// Good: Store state in properties
foundry.SetProperty("OrderId", orderId);
foundry.SetProperty("CustomerId", customerId);
foundry.SetProperty("PaymentId", paymentId);

// Operations can access this state after recovery
var orderId = foundry.GetPropertyOrDefault<string>("OrderId");
```

## Recovery Flow

1. **Load Snapshot**: Retrieve saved workflow state from provider
2. **Restore Properties**: Populate foundry with saved properties
3. **Skip Completed**: Start from `NextOperationIndex`
4. **Resume Execution**: Continue workflow from checkpoint
5. **Retry on Failure**: Use recovery policy for transient failures

## Error Handling

```csharp
try
{
    await coordinator.ResumeAsync(...);
}
catch (RecoveryException ex)
{
    logger.LogError(ex, "Failed to resume workflow after {Attempts} attempts", ex.Attempts);
    // Handle permanent failure
}
```

## Documentation

- **[Getting Started](../../../docs/getting-started/getting-started.md)**
- **[Configuration Guide](../../../docs/core/configuration.md#recovery-extension)**
- **[Extensions Overview](../../../docs/extensions/index.md)**
- **[Persistence Extension](../WorkflowForge.Extensions.Persistence/README.md)** - Required for state storage
- **[Sample 21: Recovery](../../samples/WorkflowForge.Samples.BasicConsole/README.md)**

---

