# WorkflowForge.Extensions.Audit

![WorkflowForge](https://raw.githubusercontent.com/animatlabs/workflow-forge/main/icon.png)

Comprehensive audit logging extension for WorkflowForge with pluggable storage providers for compliance and observability.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Audit.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Audit/)

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
using WorkflowForge.Extensions.Audit;

// Implement audit provider
public class FileAuditProvider : IAuditProvider
{
    private readonly string _logPath;
    
    public FileAuditProvider(string logPath)
    {
        _logPath = logPath;
    }
    
    public async Task WriteAuditEntryAsync(
        AuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(entry);
        await File.AppendAllTextAsync(_logPath, json + Environment.NewLine, cancellationToken);
    }
    
    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

// Configure audit logging
var auditProvider = new FileAuditProvider("audit.log");
var auditLogger = new AuditLogger(
    auditProvider,
    userId: "user@example.com",
    sessionId: Guid.NewGuid().ToString(),
    timeProvider: new SystemTimeProvider());

// Subscribe to events
smith.WorkflowStarted += async (s, e) => 
    await auditLogger.LogWorkflowStartedAsync(e);
smith.WorkflowCompleted += async (s, e) => 
    await auditLogger.LogWorkflowCompletedAsync(e);
foundry.OperationCompleted += async (s, e) => 
    await auditLogger.LogOperationCompletedAsync(e);

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
    public string EventType { get; set; }           // WorkflowStarted, OperationCompleted, etc.
    public DateTimeOffset Timestamp { get; set; }
    public string UserId { get; set; }
    public string SessionId { get; set; }
    public string WorkflowName { get; set; }
    public string OperationName { get; set; }
    public TimeSpan? Duration { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
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
    
    public async Task WriteAuditEntryAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        await _connection.ExecuteAsync(
            "INSERT INTO AuditLog (EventType, Timestamp, UserId, ...) VALUES (@EventType, @Timestamp, @UserId, ...)",
            entry);
    }
}
```

### Cloud Storage Provider

```csharp
public class AzureAuditProvider : IAuditProvider
{
    private readonly BlobContainerClient _container;
    
    public async Task WriteAuditEntryAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        var blobName = $"{entry.Timestamp:yyyy-MM-dd}/{Guid.NewGuid()}.json";
        var blob = _container.GetBlobClient(blobName);
        await blob.UploadAsync(JsonSerializer.Serialize(entry), cancellationToken);
    }
}
```

## Documentation

- **[Getting Started](../../../docs/getting-started/getting-started.md)**
- **[Configuration Guide](../../../docs/core/configuration.md#audit-extension)**
- **[Extensions Overview](../../../docs/extensions/index.md)**
- **[Sample 24: Audit](../../samples/WorkflowForge.Samples.BasicConsole/README.md)**

---

