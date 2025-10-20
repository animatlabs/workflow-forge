# WorkflowForge.Extensions.Audit

Comprehensive audit logging extension for WorkflowForge providing detailed tracking of workflow execution, data changes, and compliance reporting.

## Installation

```bash
dotnet add package WorkflowForge.Extensions.Audit
```

## Zero Dependencies - Zero Conflicts

**This extension has ZERO external dependencies.** This means:

- **NO DLL Hell** - No third-party dependencies to conflict with  
- **NO Conflicts** - Works with any versions of your application dependencies  
- **Clean Deployment** - Pure WorkflowForge extension with no baggage

**Pluggable architecture**: Bring your own storage (database, file, cloud) via the `IAuditProvider` interface.

## Quick Start

### 1. Implement an Audit Provider

```csharp
using WorkflowForge.Extensions.Audit;

// Use built-in in-memory provider for testing
var auditProvider = new InMemoryAuditProvider();

// OR implement your own for production
public class DatabaseAuditProvider : IAuditProvider
{
    private readonly DbContext _dbContext;
    
    public DatabaseAuditProvider(DbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task WriteAuditEntryAsync(
        AuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.AuditLog.AddAsync(entry, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
    
    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
```

### 2. Enable Audit Logging

```csharp
using WorkflowForge;
using WorkflowForge.Extensions.Audit;

var foundry = WorkflowForge.CreateFoundry("OrderProcessing");
foundry.SetProperty("Workflow.Name", "ProcessOrder");

// Enable audit middleware
foundry.EnableAudit(
    auditProvider,
    initiatedBy: "user@example.com",
    includeMetadata: true);

// Execute workflow - all operations are automatically audited
var workflow = WorkflowForge.CreateWorkflow("ProcessOrder")
    .AddOperation(new ValidateOrderOperation())
    .AddOperation(new ChargePaymentOperation())
    .AddOperation(new FulfillOrderOperation())
    .Build();

using var smith = WorkflowForge.CreateSmith();
await smith.ForgeAsync(workflow, foundry);

// Access audit entries (if using InMemoryAuditProvider)
var memoryProvider = (InMemoryAuditProvider)auditProvider;
foreach (var entry in memoryProvider.Entries)
{
    Console.WriteLine($"{entry.Timestamp}: {entry.OperationName} - {entry.Status}");
}
```

## Key Features

- **Automatic Auditing**: Captures all workflow and operation events
- **Bring Your Own Storage**: Implement `IAuditProvider` for any storage system
- **Rich Audit Data**: Timestamps, durations, statuses, errors, metadata
- **Time Provider Integration**: Uses `ISystemTimeProvider` for testable timestamps
- **Compliance Ready**: Immutable audit entries with unique identifiers
- **Performance Optimized**: Non-blocking async operations
- **Failure Resilient**: Audit failures don't break workflow execution

## Audit Entry Structure

Each audit entry contains:

```csharp
public sealed class AuditEntry
{
    public Guid AuditId { get; }                    // Unique entry identifier
    public DateTimeOffset Timestamp { get; }         // When the event occurred
    public Guid ExecutionId { get; }                 // Workflow execution ID
    public string WorkflowName { get; }              // Workflow name
    public string OperationName { get; }             // Operation name
    public AuditEventType EventType { get; }         // Event type
    public string? InitiatedBy { get; }              // User/system identifier
    public IReadOnlyDictionary<string, object?> Metadata { get; } // Optional metadata
    public string Status { get; }                    // Operation status
    public string? ErrorMessage { get; }             // Error details (if failed)
    public long? DurationMs { get; }                 // Duration in milliseconds
}
```

## Audit Event Types

```csharp
public enum AuditEventType
{
    WorkflowStarted = 1,
    WorkflowCompleted = 2,
    WorkflowFailed = 3,
    OperationStarted = 4,
    OperationCompleted = 5,
    OperationFailed = 6,
    DataModified = 7,
    ValidationPerformed = 8,
    CompensationTriggered = 9,
    Custom = 100
}
```

## Usage Patterns

### Basic Auditing

```csharp
var auditProvider = new InMemoryAuditProvider();
foundry.EnableAudit(auditProvider);
```

### Auditing with User Context

```csharp
var userId = "admin@company.com";
foundry.EnableAudit(auditProvider, initiatedBy: userId);
```

### Auditing with Metadata

```csharp
// Include all foundry properties in audit metadata
foundry.EnableAudit(auditProvider, includeMetadata: true);
```

### Custom Audit Entries

```csharp
// Write custom audit events within operations
public class ApproveOrderOperation : IWorkflowOperation
{
    private readonly IAuditProvider _auditProvider;
    
    public async Task<object?> ForgeAsync(
        IWorkflowFoundry foundry,
        object? inputData,
        CancellationToken cancellationToken)
    {
        // Perform approval logic
        var approved = true;
        
        // Write custom audit entry
        await foundry.WriteCustomAuditAsync(
            _auditProvider,
            "OrderApproval",
            AuditEventType.Custom,
            approved ? "Approved" : "Rejected",
            initiatedBy: "ApprovalSystem");
        
        return null;
    }
}
```

## Audit Provider Implementations

### In-Memory Provider (Testing/Development)

```csharp
var provider = new InMemoryAuditProvider();
foundry.EnableAudit(provider);

// Query entries
var allEntries = provider.Entries;
var failedOperations = provider.Entries
    .Where(e => e.EventType == AuditEventType.OperationFailed)
    .ToList();
```

### Database Provider (Production)

```csharp
public class SqlServerAuditProvider : IAuditProvider
{
    private readonly string _connectionString;
    
    public SqlServerAuditProvider(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task WriteAuditEntryAsync(
        AuditEntry entry,
        CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        var sql = @"
            INSERT INTO AuditLog 
            (AuditId, Timestamp, ExecutionId, WorkflowName, OperationName, 
             EventType, Status, InitiatedBy, ErrorMessage, DurationMs, Metadata)
            VALUES 
            (@AuditId, @Timestamp, @ExecutionId, @WorkflowName, @OperationName,
             @EventType, @Status, @InitiatedBy, @ErrorMessage, @DurationMs, @Metadata)";
        
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@AuditId", entry.AuditId);
        command.Parameters.AddWithValue("@Timestamp", entry.Timestamp);
        command.Parameters.AddWithValue("@ExecutionId", entry.ExecutionId);
        command.Parameters.AddWithValue("@WorkflowName", entry.WorkflowName);
        command.Parameters.AddWithValue("@OperationName", entry.OperationName);
        command.Parameters.AddWithValue("@EventType", (int)entry.EventType);
        command.Parameters.AddWithValue("@Status", entry.Status);
        command.Parameters.AddWithValue("@InitiatedBy", (object?)entry.InitiatedBy ?? DBNull.Value);
        command.Parameters.AddWithValue("@ErrorMessage", (object?)entry.ErrorMessage ?? DBNull.Value);
        command.Parameters.AddWithValue("@DurationMs", (object?)entry.DurationMs ?? DBNull.Value);
        command.Parameters.AddWithValue("@Metadata", JsonSerializer.Serialize(entry.Metadata));
        
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
    
    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
```

### File-Based Provider

```csharp
public class FileAuditProvider : IAuditProvider
{
    private readonly string _logDirectory;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    
    public FileAuditProvider(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(logDirectory);
    }
    
    public async Task WriteAuditEntryAsync(
        AuditEntry entry,
        CancellationToken cancellationToken)
    {
        var fileName = $"audit-{entry.Timestamp:yyyy-MM-dd}.log";
        var filePath = Path.Combine(_logDirectory, fileName);
        
        var logLine = $"{entry.Timestamp:O}|{entry.ExecutionId}|{entry.WorkflowName}|" +
                     $"{entry.OperationName}|{entry.EventType}|{entry.Status}|" +
                     $"{entry.DurationMs}|{entry.InitiatedBy}|{entry.ErrorMessage}\n";
        
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(filePath, logLine, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
```

## Advanced Scenarios

### Buffered Audit Provider

```csharp
public class BufferedAuditProvider : IAuditProvider, IDisposable
{
    private readonly IAuditProvider _innerProvider;
    private readonly ConcurrentQueue<AuditEntry> _buffer = new();
    private readonly Timer _flushTimer;
    private readonly int _bufferSize;
    
    public BufferedAuditProvider(
        IAuditProvider innerProvider,
        int bufferSize = 100,
        TimeSpan? flushInterval = null)
    {
        _innerProvider = innerProvider;
        _bufferSize = bufferSize;
        _flushTimer = new Timer(_ => FlushAsync().GetAwaiter().GetResult(),
            null, flushInterval ?? TimeSpan.FromSeconds(10), flushInterval ?? TimeSpan.FromSeconds(10));
    }
    
    public Task WriteAuditEntryAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        _buffer.Enqueue(entry);
        
        if (_buffer.Count >= _bufferSize)
        {
            return FlushAsync(cancellationToken);
        }
        
        return Task.CompletedTask;
    }
    
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        while (_buffer.TryDequeue(out var entry))
        {
            await _innerProvider.WriteAuditEntryAsync(entry, cancellationToken);
        }
        
        await _innerProvider.FlushAsync(cancellationToken);
    }
    
    public void Dispose()
    {
        _flushTimer?.Dispose();
        FlushAsync().GetAwaiter().GetResult();
    }
}
```

### Filtered Audit Provider

```csharp
public class FilteredAuditProvider : IAuditProvider
{
    private readonly IAuditProvider _innerProvider;
    private readonly Func<AuditEntry, bool> _filter;
    
    public FilteredAuditProvider(
        IAuditProvider innerProvider,
        Func<AuditEntry, bool> filter)
    {
        _innerProvider = innerProvider;
        _filter = filter;
    }
    
    public Task WriteAuditEntryAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        if (_filter(entry))
        {
            return _innerProvider.WriteAuditEntryAsync(entry, cancellationToken);
        }
        
        return Task.CompletedTask;
    }
    
    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        return _innerProvider.FlushAsync(cancellationToken);
    }
}

// Usage: Only audit failures
var filteredProvider = new FilteredAuditProvider(
    new DatabaseAuditProvider(connectionString),
    entry => entry.Status == "Failed" || entry.EventType == AuditEventType.OperationFailed);
```

## Compliance and Security

### Immutability

Audit entries are immutable by design - all properties are read-only.

### Unique Identifiers

Each audit entry has a unique `AuditId` for tracking and correlation.

### Timestamp Consistency

Uses `ISystemTimeProvider` for consistent, testable timestamps.

### Error Resilience

Audit failures are logged but don't interrupt workflow execution.

## Best Practices

1. **Choose the Right Provider**: In-memory for testing, database/file for production
2. **Enable Early**: Add audit middleware at the start of the pipeline
3. **Include Context**: Use `initiatedBy` to track who executed the workflow
4. **Selective Metadata**: Only include metadata when necessary (performance)
5. **Implement Retention**: Archive or delete old audit entries based on compliance requirements
6. **Monitor Audit System**: Ensure audit provider is functioning correctly
7. **Test Audit Logic**: Verify audit entries are written correctly
8. **Secure Audit Data**: Protect audit logs from tampering

## Performance Considerations

- Audit writes are async and non-blocking
- Audit failures don't impact workflow execution
- Use buffering for high-throughput scenarios
- Consider metadata overhead (serialize only when needed)
- Typical overhead: ~1-3ms per operation

## Integration with Other Extensions

### With Persistence Extension

```csharp
foundry.UsePersistence(persistenceProvider);  // State management
foundry.EnableAudit(auditProvider);           // Audit trail
```

### With Validation Extension

```csharp
foundry.AddValidation(validator, dataExtractor); // Validates first
foundry.EnableAudit(auditProvider);             // Audits validation results
```

### With Performance Monitoring

```csharp
foundry.EnablePerformanceMonitoring();  // Performance metrics
foundry.EnableAudit(auditProvider);     // Audit with timing data
```

## License

MIT License - See LICENSE file for details

