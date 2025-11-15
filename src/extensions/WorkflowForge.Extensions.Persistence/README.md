# WorkflowForge.Extensions.Persistence

<p align="center">
  <img src="../../../icon.png" alt="WorkflowForge" width="120" height="120">
</p>

Workflow state persistence extension for WorkflowForge with in-memory and SQLite providers for checkpointing and recovery.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Persistence.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Persistence/)

## Zero Dependencies (InMemory) / Zero Conflicts (SQLite)

**InMemory Provider**: ZERO external dependencies - Pure WorkflowForge  
**SQLite Provider**: Uses Costura.Fody to embed Microsoft.Data.Sqlite

- NO DLL Hell - Embedded dependencies don't conflict
- NO Version Conflicts - Your app can use any SQLite version
- Clean Deployment - Professional dependency isolation

## Installation

```bash
dotnet add package WorkflowForge.Extensions.Persistence
```

**Requires**: .NET Standard 2.0 or later

## Quick Start

### In-Memory (Development/Testing)

```csharp
using WorkflowForge.Extensions.Persistence.InMemory;

var provider = new InMemoryPersistenceProvider();

// Save workflow state
var state = new WorkflowState
{
    ExecutionId = foundry.ExecutionId,
    WorkflowName = workflow.Name,
    Properties = foundry.Properties.ToDictionary(x => x.Key, x => x.Value),
    CompletedOperations = new List<string> { "Op1", "Op2" }
};

await provider.SaveWorkflowStateAsync(foundry.ExecutionId, state);

// Load workflow state
var loadedState = await provider.LoadWorkflowStateAsync(foundry.ExecutionId);
```

### SQLite (Production)

```csharp
using WorkflowForge.Extensions.Persistence.SQLite;

var provider = new SQLitePersistenceProvider("workflows.db");

// Same API as InMemory
await provider.SaveWorkflowStateAsync(foundry.ExecutionId, state);
var loadedState = await provider.LoadWorkflowStateAsync(foundry.ExecutionId);
```

## Key Features

- **Two Providers**: InMemory (testing) and SQLite (production)
- **Checkpoint Support**: Save workflow state at any point
- **Resume Workflows**: Continue from last checkpoint
- **Pluggable Architecture**: `IWorkflowPersistenceProvider` interface
- **Thread-Safe**: Concurrent workflow support
- **Transactional**: SQLite provider uses transactions

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

See [Configuration Guide](../../../docs/configuration.md#persistence-extensions) for complete options.

## Workflow State Structure

```csharp
public class WorkflowState
{
    public Guid ExecutionId { get; set; }
    public string WorkflowName { get; set; }
    public Dictionary<string, object> Properties { get; set; }
    public List<string> CompletedOperations { get; set; }
    public int NextOperationIndex { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
```

## Custom Persistence Provider

```csharp
public class CosmosDbPersistenceProvider : IWorkflowPersistenceProvider
{
    private readonly CosmosClient _client;
    private readonly Container _container;
    
    public async Task SaveWorkflowStateAsync(
        Guid executionId,
        WorkflowState state,
        CancellationToken cancellationToken = default)
    {
        await _container.UpsertItemAsync(state, new PartitionKey(executionId.ToString()), cancellationToken: cancellationToken);
    }
    
    public async Task<WorkflowState?> LoadWorkflowStateAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<WorkflowState>(
                executionId.ToString(),
                new PartitionKey(executionId.ToString()),
                cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
```

## Usage Patterns

### Save Checkpoint at Each Operation

```csharp
foundry.OperationCompleted += async (s, e) =>
{
    var state = CreateStateFromFoundry(foundry);
    await provider.SaveWorkflowStateAsync(foundry.ExecutionId, state);
};
```

### Resume from Checkpoint

```csharp
var state = await provider.LoadWorkflowStateAsync(executionId);
if (state != null)
{
    // Restore foundry state
    foreach (var prop in state.Properties)
    {
        foundry.SetProperty(prop.Key, prop.Value);
    }
    
    // Skip completed operations
    foundry.SetProperty("NextOperationIndex", state.NextOperationIndex);
}
```

## Documentation

- **[Getting Started](../../../docs/getting-started.md)**
- **[Configuration Guide](../../../docs/configuration.md#persistence-extensions)**
- **[Extensions Overview](../../../docs/extensions.md)**
- **[Sample 18: Persistence](../../samples/WorkflowForge.Samples.BasicConsole/README.md)**
- **[Recovery Extension](../WorkflowForge.Extensions.Persistence.Recovery/README.md)** - Recovery orchestration on top of persistence

---

**WorkflowForge.Extensions.Persistence** - *Build workflows with industrial strength*
