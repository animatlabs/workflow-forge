# WorkflowForge.Extensions.Persistence

Save and resume workflows through `IWorkflowPersistenceProvider`: you choose SQL, Cosmos, Redis, files, or anything else. The package ships abstractions and middleware only, no built-in database driver.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Persistence.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Persistence/)

## Install

```bash
dotnet add package WorkflowForge.Extensions.Persistence
```

Targets .NET Standard 2.0 or later.

## Quick Start

### 1. Implement the provider interface

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

### 2. Enable persistence on the foundry

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

### 3. Enable with stable keys (for cross-process resume)

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

## Key points

- No default provider on purpose: stay storage-neutral (you implement `IWorkflowPersistenceProvider`). Beyond core, depends only on `Microsoft.Extensions.*` for options/DI.
- Checkpoints after each successful operation; resume skips completed steps via `NextOperationIndex`.
- Optional stable keys tie executions to logical instances across restarts.
- Middleware is written to be safe under concurrent workflows.

## How it works

The `PersistenceMiddleware` wraps each operation in the pipeline:

1. **Before each operation**: Calls `TryLoadAsync` to check for a saved snapshot
2. **If snapshot exists and operation already completed**: Restores properties, skips the operation
3. **If not resumed**: Calls the next middleware/operation normally
4. **After success**: Builds a `WorkflowExecutionSnapshot` with `NextOperationIndex` incremented and calls `SaveAsync`
5. **When all operations complete**: Calls `DeleteAsync` to clean up

## Snapshot structure

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
- `Properties`: Arbitrary key-value state from `foundry.Properties`; store only what you need for resumption

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

### Via code

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

### Via dependency injection

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Extensions.Persistence;

services.AddPersistenceConfiguration(configuration);
var options = serviceProvider.GetRequiredService<IOptions<PersistenceOptions>>().Value;
```

[Persistence configuration](../../../docs/core/configuration.md#persistence-extension)

## Provider interface

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

## Links

- [Getting Started](../../../docs/getting-started/getting-started.md)
- [Configuration Guide](../../../docs/core/configuration.md#persistence-extension)
- [Extensions Overview](../../../docs/extensions/index.md)
- [Recovery Extension](../WorkflowForge.Extensions.Persistence.Recovery/README.md)
- [Sample 18: Persistence](../../samples/WorkflowForge.Samples.BasicConsole/README.md)
