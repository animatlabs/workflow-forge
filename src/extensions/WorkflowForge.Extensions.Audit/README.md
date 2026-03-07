# WorkflowForge.Extensions.Audit

Comprehensive audit logging extension for WorkflowForge with pluggable storage providers for compliance and observability.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Audit.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Audit/)
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

**Architecture**: Implement `IAuditProvider` for your storage (file, database, cloud).

## Installation

```bash
dotnet add package WorkflowForge.Extensions.Audit
```

**Requires**: .NET Standard 2.0 or later

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

## Key Features

- **Pluggable Storage**: Implement `IAuditProvider` for any storage
- **Comprehensive Logging**: Workflow and operation lifecycle events
- **Structured Data**: Rich audit entries with metadata
- **Time Provider Integration**: `ISystemTimeProvider` for testability
- **User Context**: Track user and session information
- **Compliance Ready**: Immutable audit trail for regulatory requirements

## Audit Entry Structure

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

### Via Code

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

### Via Dependency Injection

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Extensions.Audit;

services.AddAuditConfiguration(configuration);
var options = serviceProvider.GetRequiredService<IOptions<AuditMiddlewareOptions>>().Value;
```

See [Configuration Guide](../../../docs/core/configuration.md#audit-extension) for complete options.

## Storage Provider Examples

### Database Provider

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

### Cloud Storage Provider

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

## Documentation

- **[Getting Started](../../../docs/getting-started/getting-started.md)**
- **[Configuration Guide](../../../docs/core/configuration.md#audit-extension)**
- **[Extensions Overview](../../../docs/extensions/index.md)**
- **[Sample 24: Audit](../../samples/WorkflowForge.Samples.BasicConsole/README.md)**

---

