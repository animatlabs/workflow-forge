# WorkflowForge.Extensions.Persistence

Persistence extension for WorkflowForge enabling resumable workflows via a pluggable provider (bring your own storage). Zero-dependency core integration.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Persistence.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Persistence/)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=coverage)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)

## Bring Your Own Storage

This extension defines the **abstractions and middleware** for workflow persistence. You implement `IWorkflowPersistenceProvider` to plug in your chosen storage (SQL Server, Cosmos DB, Redis, file system, etc.).

There are **no built-in providers** -- this is by design to keep the extension dependency-free and storage-agnostic.

## Installation

```bash
dotnet add package WorkflowForge.Extensions.Persistence
```

**Requires**: .NET Standard 2.0 or later

## Quick Start

### 1. Implement the Provider Interface

```csharp
using WorkflowForge.Extensions.Persistence.Abstractions;

public class SqlPersistenceProvider : IWorkflowPersistenceProvider
{
    private readonly string _connectionString;

    public SqlPersistenceProvider(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task SaveAsync(
        WorkflowExecutionSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        // Serialize and store the snapshot in your database
    }

    public async Task<WorkflowExecutionSnapshot?> TryLoadAsync(
        Guid foundryExecutionId,
        Guid workflowId,
        CancellationToken cancellationToken = default)
    {
        // Load the snapshot from your database; return null if not found
    }

    public async Task DeleteAsync(
        Guid foundryExecutionId,
        Guid workflowId,
        CancellationToken cancellationToken = default)
    {
        // Remove the snapshot from your database
    }
}
```

### 2. Enable Persistence on the Foundry

```csharp
using WorkflowForge;
using WorkflowForge.Extensions.Persistence;

IWorkflowPersistenceProvider provider = new SqlPersistenceProvider("Server=...");

var smith = WorkflowForge.CreateSmith();
var workflow = WorkflowForge.CreateWorkflow("MyWorkflow")
    .AddOperation(/* your operations */)
    .Build();

using var foundry = WorkflowForge.CreateFoundry("OrderService");
foundry.UsePersistence(provider);

await smith.ForgeAsync(workflow, foundry);
```

### 3. Enable with Stable Keys (for Cross-Process Resume)

```csharp
using WorkflowForge.Extensions.Persistence;

var options = new PersistenceOptions
{
    InstanceId = "order-service-instance",
    WorkflowKey = "process-order-v1"
};

foundry.UsePersistence(provider, options);
```

When `InstanceId` and `WorkflowKey` are set, deterministic GUIDs are derived from those strings so the same workflow can be found across process restarts.

## Key Features

- **Pluggable Storage**: `IWorkflowPersistenceProvider` interface -- bring any storage backend
- **Checkpoint & Resume**: Automatically checkpoints after each operation and skips completed steps on resume
- **Index-Based Tracking**: O(1) operation tracking with no dictionary lookups
- **Stable Keys**: Deterministic IDs for cross-process recovery
- **Thread-Safe**: Safe for concurrent workflow execution
- **Zero Dependencies**: No external packages beyond WorkflowForge core

## How It Works

The `PersistenceMiddleware` wraps each operation in the pipeline:

1. **Before each operation**: Calls `TryLoadAsync` to check for a saved snapshot
2. **If snapshot exists and operation already completed**: Restores properties, skips the operation
3. **If not resumed**: Calls the next middleware/operation normally
4. **After success**: Builds a `WorkflowExecutionSnapshot` with `NextOperationIndex` incremented and calls `SaveAsync`
5. **When all operations complete**: Calls `DeleteAsync` to clean up

## Snapshot Structure

```csharp
public sealed class WorkflowExecutionSnapshot
{
    public Guid FoundryExecutionId { get; set; }
    public Guid WorkflowId { get; set; }
    public string WorkflowName { get; set; }
    public int NextOperationIndex { get; set; }
    public Dictionary<string, object?> Properties { get; set; }
}
```

- `NextOperationIndex`: -1 means not started; N means operation at index N is next
- `Properties`: Arbitrary key-value state from `foundry.Properties` -- store only what you need for resumption

## Configuration

### Via appsettings.json

```json
{
  "WorkflowForge": {
    "Extensions": {
      "Persistence": {
        "Enabled": true,
        "PersistOnOperationComplete": true,
        "PersistOnWorkflowComplete": true,
        "PersistOnFailure": true,
        "MaxVersions": 10,
        "InstanceId": "my-instance-id",
        "WorkflowKey": "my-workflow-key"
      }
    }
  }
}
```

### Via Code

```csharp
using WorkflowForge.Extensions.Persistence;

var options = new PersistenceOptions
{
    Enabled = true,
    PersistOnOperationComplete = true,
    PersistOnWorkflowComplete = true,
    PersistOnFailure = true,
    MaxVersions = 10,
    InstanceId = "my-instance-id",
    WorkflowKey = "my-workflow-key"
};

foundry.UsePersistence(provider, options);
```

### Via Dependency Injection

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Extensions.Persistence;

services.AddPersistenceConfiguration(configuration);
var options = serviceProvider.GetRequiredService<IOptions<PersistenceOptions>>().Value;
```

See [Configuration Guide](../../../docs/core/configuration.md#persistence-extensions) for complete options.

## Provider Interface

```csharp
public interface IWorkflowPersistenceProvider
{
    Task SaveAsync(WorkflowExecutionSnapshot snapshot, CancellationToken cancellationToken = default);

    Task<WorkflowExecutionSnapshot?> TryLoadAsync(Guid foundryExecutionId, Guid workflowId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid foundryExecutionId, Guid workflowId,
        CancellationToken cancellationToken = default);
}
```

## Documentation

- **[Getting Started](../../../docs/getting-started/getting-started.md)**
- **[Configuration Guide](../../../docs/core/configuration.md#persistence-extensions)**
- **[Extensions Overview](../../../docs/extensions/index.md)**
- **[Recovery Extension](../WorkflowForge.Extensions.Persistence.Recovery/README.md)** - Recovery orchestration on top of persistence
- **[Sample 18: Persistence](../../samples/WorkflowForge.Samples.BasicConsole/README.md)**

---
