# WorkflowForge.Extensions.Persistence.Recovery

Recovery orchestration extension for WorkflowForge to resume persisted workflows from the last checkpoint with configurable retry and backoff.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Persistence.Recovery.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Persistence.Recovery/)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=coverage)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)

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

## Important Notes

### Stable Keys

**Critical**: Use stable, deterministic keys for foundry and workflow:

```csharp
// Good: Stable keys
var foundryKey = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
var workflowKey = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");

// Bad: Random keys (won't find saved state)
var foundryKey = Guid.NewGuid();  // Different every time!
```

### Operation Order

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
5. **Retry on Failure**: Use RecoveryMiddlewareOptions for transient failures

## Error Handling

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

## Documentation

- **[Getting Started](../../../docs/getting-started/getting-started.md)**
- **[Configuration Guide](../../../docs/core/configuration.md#recovery-extension)**
- **[Extensions Overview](../../../docs/extensions/index.md)**
- **[Persistence Extension](../WorkflowForge.Extensions.Persistence/README.md)** - Required for state storage
- **[Sample 21: Recovery](../../samples/WorkflowForge.Samples.BasicConsole/README.md)**

---

