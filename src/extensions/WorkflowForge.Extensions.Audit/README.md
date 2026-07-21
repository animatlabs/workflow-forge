# WorkflowForge.Extensions.Audit

Record who did what, when, and whether it finished: audit logging with your own storage behind `IAuditProvider`.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Audit.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Audit/)

## Install

```bash
dotnet add package WorkflowForge.Extensions.Audit
```

Targets .NET Standard 2.0 or later.

## Quick Start

```csharp
using WorkflowForge;
using WorkflowForge.Extensions.Audit;
using WorkflowForge.Extensions.Audit.Options;

// Create the audit provider
var auditProvider = new InMemoryAuditProvider();

// Create foundry, workflow, and smith
using var foundry = WorkflowForge.CreateFoundry("OrderProcessing");
var smith = WorkflowForge.CreateSmith();
var workflow = WorkflowForge.CreateWorkflow("OrderProcessing")
    .AddOperation(new ActionWorkflowOperation("ValidateOrder", async (input, foundry, ct) => { /* ... */ }))
    .Build();

// Enable audit on the foundry
foundry.UseAudit(auditProvider, new AuditMiddlewareOptions
{
    Enabled = true,
    DetailLevel = AuditDetailLevel.Standard,
    IncludeTimestamps = true,
    IncludeUserContext = true
}, initiatedBy: "user@example.com");

await smith.ForgeAsync(workflow, foundry);
```

## Key points

- Depends on WorkflowForge core plus `Microsoft.Extensions.*` (options/DI integration); no storage library — you supply storage by implementing `IAuditProvider`.
- Covers workflow and operation lifecycle events in one stream.
- Optional initiator/session-style context and timestamps; detail level is configurable.
- `ISystemTimeProvider` helps keep tests deterministic.
- Entries carry metadata dictionaries; append-only style storage fits many compliance setups.

## Audit entry shape

```csharp
public class AuditEntry
{
    public Guid AuditId { get; }                    // Unique identifier for this entry
    public DateTimeOffset Timestamp { get; }
    public Guid ExecutionId { get; }                // Workflow execution ID
    public string WorkflowName { get; }
    public string OperationName { get; }
    public AuditEventType EventType { get; }       // WorkflowStarted, OperationCompleted, etc.
    public string? InitiatedBy { get; }             // User or system that initiated the operation
    public IReadOnlyDictionary<string, object?> Metadata { get; }
    public string Status { get; }                  // Started, Completed, Failed, etc.
    public string? ErrorMessage { get; }
    public long? DurationMs { get; }               // Duration in milliseconds
}
```

## Configuration

### Via appsettings.json

```json
{
  "WorkflowForge": {
    "Extensions": {
      "Audit": {
        "Enabled": true,
        "DetailLevel": "Standard",
        "LogDataPayloads": false,
        "IncludeTimestamps": true,
        "IncludeUserContext": true
      }
    }
  }
}
```

### Via code

```csharp
using WorkflowForge.Extensions.Audit.Options;

var options = new AuditMiddlewareOptions
{
    Enabled = true,
    DetailLevel = AuditDetailLevel.Standard,
    LogDataPayloads = false,
    IncludeTimestamps = true,
    IncludeUserContext = true
};

foundry.UseAudit(auditProvider, options);
```

### Via dependency injection

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Extensions.Audit;

services.AddAuditConfiguration(configuration);
var options = serviceProvider.GetRequiredService<IOptions<AuditMiddlewareOptions>>().Value;
```

[Configuration: Audit](../../../docs/core/configuration.md#audit-extension)

## Storage provider examples

### Database provider

```csharp
public class DatabaseAuditProvider : IAuditProvider
{
    private readonly IDbConnection _connection;

    public async Task WriteAuditEntryAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        await _connection.ExecuteAsync(
            "INSERT INTO AuditLog (AuditId, Timestamp, ExecutionId, WorkflowName, OperationName, EventType, InitiatedBy, Status, ErrorMessage, DurationMs) VALUES (@AuditId, @Timestamp, @ExecutionId, @WorkflowName, @OperationName, @EventType, @InitiatedBy, @Status, @ErrorMessage, @DurationMs)",
            entry,
            cancellationToken);
    }

    public Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
```

### Cloud storage provider

```csharp
public class AzureAuditProvider : IAuditProvider
{
    private readonly BlobContainerClient _container;

    public async Task WriteAuditEntryAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        var blobName = $"{entry.Timestamp:yyyy-MM-dd}/{entry.AuditId}.json";
        var blob = _container.GetBlobClient(blobName);
        await blob.UploadAsync(BinaryData.FromString(JsonSerializer.Serialize(entry)), overwrite: true, cancellationToken);
    }

    public Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
```

## Links

- [Getting Started](../../../docs/getting-started/getting-started.md)
- [Configuration Guide](../../../docs/core/configuration.md#audit-extension)
- [Extensions Overview](../../../docs/extensions/index.md)
- [Sample 24: Audit](../../samples/WorkflowForge.Samples.BasicConsole/README.md)
