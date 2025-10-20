# Building Workflow Operations

This guide covers creating custom workflow operations in WorkflowForge, from simple inline operations to sophisticated robust components.

## Operation Fundamentals

### The IWorkflowOperation Interface

All operations implement the `IWorkflowOperation` interface:

```csharp
public interface IWorkflowOperation
{
    Guid Id { get; }
    string Name { get; }
    bool SupportsRestore { get; }
    
    Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken);
    Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken);
}
```

### Key Properties

- **Id**: Unique identifier for tracking and logging
- **Name**: Human-readable name for debugging and monitoring
- **SupportsRestore**: Whether the operation can be rolled back (compensation)

### Key Methods

- **ForgeAsync**: Execute the operation with input data
- **RestoreAsync**: Rollback the operation (compensation logic)

## Operation Types

### 1. Inline Operations

Quick operations defined using lambdas:

```csharp
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("SimpleProcessing")
    .AddOperation("ProcessData", async (input, foundry, ct) =>
    {
        var data = (string)input!;
        foundry.Logger.LogInformation("Processing: {Data}", data);
        
        // Simple processing logic
        await Task.Delay(100, ct);
        
        return data.ToUpperInvariant();
    })
    .Build();
```

### 2. Class-Based Operations

Reusable operations with full lifecycle support:

```csharp
public class EmailNotificationOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "SendEmailNotification";
    public bool SupportsRestore => true;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var emailRequest = (EmailRequest)inputData!;
        
        foundry.Logger.LogInformation("Sending email to {Recipient}", emailRequest.Recipient);
        
        // Send email logic
         var emailService = (IEmailService)foundry.ServiceProvider!.GetService(typeof(IEmailService))!;
        var messageId = await emailService.SendAsync(emailRequest, cancellationToken);
        
        // Store message ID for potential rollback
         foundry.Properties["EmailMessageId"] = messageId;
        
        foundry.Logger.LogInformation("Email sent successfully, MessageId: {MessageId}", messageId);
        
        return new EmailResponse
        {
            MessageId = messageId,
            SentAt = DateTime.UtcNow,
            Success = true
        };
    }

    public async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        if (foundry.Properties.TryGetValue("EmailMessageId", out var msg) && msg is string messageId && !string.IsNullOrEmpty(messageId))
        {
            foundry.Logger.LogWarning("Attempting to recall email {MessageId}", messageId);
            
            var emailService = (IEmailService)foundry.ServiceProvider!.GetService(typeof(IEmailService))!;
            await emailService.RecallAsync(messageId, cancellationToken);
            
            foundry.Logger.LogInformation("Email recall completed for {MessageId}", messageId);
        }
    }
}
```

### 3. Configurable Operations

Operations that accept configuration:

```csharp
public class HttpRequestOperation : IWorkflowOperation
{
    private readonly HttpRequestSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;

    public HttpRequestOperation(HttpRequestSettings settings, IHttpClientFactory httpClientFactory)
    {
        _settings = settings;
        _httpClientFactory = httpClientFactory;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public string Name => $"HttpRequest_{_settings.Method}_{_settings.Endpoint}";
    public bool SupportsRestore => false; // HTTP requests typically can't be undone

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        foundry.Logger.LogInformation("Making HTTP {Method} request to {Endpoint}", 
            _settings.Method, _settings.Endpoint);

        var httpClient = _httpClientFactory.CreateClient(_settings.ClientName);
        
        // Set timeout
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_settings.Timeout);

        HttpResponseMessage response;
        
        switch (_settings.Method.ToUpperInvariant())
        {
            case "GET":
                response = await httpClient.GetAsync(_settings.Endpoint, timeoutCts.Token);
                break;
            case "POST":
                var content = new StringContent(JsonSerializer.Serialize(inputData), Encoding.UTF8, "application/json");
                response = await httpClient.PostAsync(_settings.Endpoint, content, timeoutCts.Token);
                break;
            default:
                throw new NotSupportedException($"HTTP method {_settings.Method} is not supported");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            foundry.Logger.LogError("HTTP request failed: {StatusCode} {ReasonPhrase}", 
                response.StatusCode, response.ReasonPhrase);
            throw new HttpRequestException($"HTTP {response.StatusCode}: {response.ReasonPhrase}");
        }

        foundry.Logger.LogInformation("HTTP request completed successfully: {StatusCode}", response.StatusCode);
        
        return new HttpResponse
        {
            StatusCode = response.StatusCode,
            Content = responseContent,
            Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value))
        };
    }

    public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        // HTTP requests typically cannot be rolled back
        foundry.Logger.LogWarning("HTTP request operation does not support rollback");
        return Task.CompletedTask;
    }
}

public class HttpRequestSettings
{
    public string Method { get; set; } = "GET";
    public string Endpoint { get; set; } = string.Empty;
    public string ClientName { get; set; } = "default";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
```

## Advanced Operation Patterns

### 1. Stateful Operations

Operations that maintain state across workflow executions:

```csharp
public class BatchProcessingOperation : IWorkflowOperation
{
    private readonly IBatchProcessor _processor;
    private readonly BatchSettings _settings;

    public BatchProcessingOperation(IBatchProcessor processor, BatchSettings settings)
    {
        _processor = processor;
        _settings = settings;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "BatchProcessing";
    public bool SupportsRestore => true;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var items = (IEnumerable<object>)inputData!;
        var batchId = Guid.NewGuid();
        
        foundry.Logger.LogInformation("Starting batch processing for {ItemCount} items, BatchId: {BatchId}", 
            items.Count(), batchId);

        // Store batch ID for tracking
        foundry.Properties["BatchId"] = batchId;
        foundry.Properties["ProcessedItems"] = new List<object>();

        var processedItems = new List<object>();
        var batch = new List<object>();

        foreach (var item in items)
        {
            batch.Add(item);

            if (batch.Count >= _settings.BatchSize)
            {
                var batchResult = await ProcessBatch(batch, foundry, cancellationToken);
                processedItems.AddRange(batchResult);
                
                // Update progress
                foundry.Properties["ProcessedItems"] = processedItems;
                
                batch.Clear();
                
                // Respect cancellation
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        // Process remaining items
        if (batch.Any())
        {
            var batchResult = await ProcessBatch(batch, foundry, cancellationToken);
            processedItems.AddRange(batchResult);
        }

        foundry.Logger.LogInformation("Batch processing completed, BatchId: {BatchId}, ProcessedCount: {ProcessedCount}", 
            batchId, processedItems.Count);

        return new BatchResult
        {
            BatchId = batchId,
            ProcessedItems = processedItems,
            TotalProcessed = processedItems.Count
        };
    }

    public async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var batchId = foundry.GetPropertyOrDefault<Guid>("BatchId");
        var processedItems = foundry.GetPropertyOrDefault<List<object>>("ProcessedItems");

        if (processedItems?.Any() == true)
        {
            foundry.Logger.LogWarning("Rolling back batch processing, BatchId: {BatchId}, ItemCount: {ItemCount}", 
                batchId, processedItems.Count);

            await _processor.RollbackBatchAsync(batchId, processedItems, cancellationToken);
            
            foundry.Logger.LogInformation("Batch rollback completed, BatchId: {BatchId}", batchId);
        }
    }

    private async Task<IEnumerable<object>> ProcessBatch(List<object> batch, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        foundry.Logger.LogDebug("Processing batch of {BatchSize} items", batch.Count);
        
        var result = await _processor.ProcessBatchAsync(batch, cancellationToken);
        
        foundry.Logger.LogDebug("Batch processing completed, ResultCount: {ResultCount}", result.Count());
        
        return result;
    }
}
```

### 2. Conditional Operations

Operations that execute conditionally:

```csharp
public class ConditionalWorkflowOperation : IWorkflowOperation
{
    private readonly Func<object?, IWorkflowFoundry, bool> _condition;
    private readonly IWorkflowOperation _trueOperation;
    private readonly IWorkflowOperation? _falseOperation;

    public ConditionalWorkflowOperation(
        Func<object?, IWorkflowFoundry, bool> condition,
        IWorkflowOperation trueOperation,
        IWorkflowOperation? falseOperation = null)
    {
        _condition = condition;
        _trueOperation = trueOperation;
        _falseOperation = falseOperation;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "ConditionalOperation";
    public bool SupportsRestore => true;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var conditionResult = _condition(inputData, foundry);
        
        foundry.Logger.LogInformation("Condition evaluated to: {ConditionResult}", conditionResult);
        foundry.Properties["ConditionResult"] = conditionResult;

        if (conditionResult)
        {
            foundry.Properties["ExecutedOperation"] = "True";
            return await _trueOperation.ForgeAsync(inputData, foundry, cancellationToken);
        }
        else if (_falseOperation != null)
        {
            foundry.Properties["ExecutedOperation"] = "False";
            return await _falseOperation.ForgeAsync(inputData, foundry, cancellationToken);
        }
        else
        {
            foundry.Properties["ExecutedOperation"] = "None";
            foundry.Logger.LogInformation("Condition was false and no false operation provided");
            return inputData;
        }
    }

    public async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var executedOperation = foundry.GetPropertyOrDefault<string>("ExecutedOperation");
        
        switch (executedOperation)
        {
            case "True" when _trueOperation.SupportsRestore:
                await _trueOperation.RestoreAsync(outputData, foundry, cancellationToken);
                break;
            case "False" when _falseOperation?.SupportsRestore == true:
                await _falseOperation.RestoreAsync(outputData, foundry, cancellationToken);
                break;
            default:
                foundry.Logger.LogInformation("No restoration needed for conditional operation");
                break;
        }
    }
}
```

### 3. Parallel Operations

Operations that execute multiple tasks in parallel:

```csharp
public class ParallelWorkflowOperation : IWorkflowOperation
{
    private readonly IEnumerable<IWorkflowOperation> _operations;
    private readonly ParallelExecutionSettings _settings;

    public ParallelWorkflowOperation(IEnumerable<IWorkflowOperation> operations, ParallelExecutionSettings settings)
    {
        _operations = operations;
        _settings = settings;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "ParallelExecution";
    public bool SupportsRestore => true;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        foundry.Logger.LogInformation("Starting parallel execution of {OperationCount} operations", _operations.Count());

        var semaphore = new SemaphoreSlim(_settings.MaxConcurrency, _settings.MaxConcurrency);
        var completedOperations = new ConcurrentBag<(IWorkflowOperation Operation, object? Result)>();
        var exceptions = new ConcurrentBag<Exception>();

        var tasks = _operations.Select(async operation =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                foundry.Logger.LogDebug("Starting operation: {OperationName}", operation.Name);
                
                var result = await operation.ForgeAsync(inputData, foundry, cancellationToken);
                completedOperations.Add((operation, result));
                
                foundry.Logger.LogDebug("Completed operation: {OperationName}", operation.Name);
            }
            catch (Exception ex)
            {
                foundry.Logger.LogError(ex, "Operation failed: {OperationName}", operation.Name);
                exceptions.Add(ex);
                
                if (_settings.FailFast)
                {
                    throw;
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        try
        {
            await Task.WhenAll(tasks);
        }
        catch when (!_settings.FailFast)
        {
            // Continue if not fail-fast
        }

        // Store completed operations for potential rollback
        foundry.Properties["CompletedOperations"] = completedOperations.ToList();

        if (exceptions.Any() && _settings.FailFast)
        {
            throw new AggregateException("Parallel operation failed", exceptions);
        }

        foundry.Logger.LogInformation("Parallel execution completed. Success: {SuccessCount}, Failed: {FailedCount}", 
            completedOperations.Count, exceptions.Count);

        return new ParallelExecutionResult
        {
            Results = completedOperations.ToDictionary(x => x.Operation.Name, x => x.Result),
            SuccessCount = completedOperations.Count,
            FailureCount = exceptions.Count,
            Exceptions = exceptions.ToList()
        };
    }

    public async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var completedOperations = foundry.GetPropertyOrDefault<List<(IWorkflowOperation Operation, object? Result)>>("CompletedOperations");

        if (completedOperations?.Any() == true)
        {
            foundry.Logger.LogWarning("Rolling back {OperationCount} completed operations", completedOperations.Count);

            // Rollback in reverse order
            var rollbackTasks = completedOperations
                .Where(x => x.Operation.SupportsRestore)
                .Reverse()
                .Select(async x =>
                {
                    try
                    {
                        await x.Operation.RestoreAsync(x.Result, foundry, cancellationToken);
                        foundry.Logger.LogDebug("Rolled back operation: {OperationName}", x.Operation.Name);
                    }
                    catch (Exception ex)
                    {
                        foundry.Logger.LogError(ex, "Failed to rollback operation: {OperationName}", x.Operation.Name);
                    }
                });

            await Task.WhenAll(rollbackTasks);
            foundry.Logger.LogInformation("Parallel operation rollback completed");
        }
    }
}

public class ParallelExecutionSettings
{
    public int MaxConcurrency { get; set; } = Environment.ProcessorCount;
    public bool FailFast { get; set; } = true;
}
```

## Advanced Patterns

### Error Handling Strategies

**Fail Fast (Default)**
```csharp
public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken ct)
{
    try
    {
        var result = await DoWorkAsync();
        return result;
    }
    catch (Exception ex)
    {
        foundry.Logger.LogError(ex, "Operation failed");
        throw; // Stop workflow, trigger compensation
    }
}
```

**Collect Errors and Continue**
```csharp
public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken ct)
{
    var items = foundry.GetPropertyOrDefault<List<Item>>("items");
    var errors = new List<string>();
    var validItems = new List<Item>();
    
    foreach (var item in items)
    {
        try
        {
            if (await ValidateItemAsync(item))
                validItems.Add(item);
        }
        catch (Exception ex)
        {
            errors.Add($"Item {item.Id}: {ex.Message}");
        }
    }
    
    foundry.SetProperty("valid_items", validItems);
    foundry.SetProperty("validation_errors", errors);
    
    return validItems.Count > 0 ? "Partial success" : "All items failed";
}
```

### Compensation Design Patterns

**State-Based Compensation**
```csharp
public class UpdateStateOperation : IWorkflowOperation
{
    public bool SupportsRestore => true;
    
    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken ct)
    {
        var previousState = await GetCurrentStateAsync();
        foundry.SetProperty("previous_state", previousState);
        
        await ModifyStateAsync();
        foundry.SetProperty("state_modified", true);
        
        return "State updated";
    }
    
    public async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken ct)
    {
        if (foundry.GetPropertyOrDefault<bool>("state_modified"))
        {
            var previousState = foundry.GetPropertyOrDefault<State>("previous_state");
            if (previousState != null)
                await RestoreStateAsync(previousState);
        }
    }
}
```

**Idempotent Compensation**
```csharp
public async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken ct)
{
    var alreadyCompensated = foundry.GetPropertyOrDefault<bool>("compensated");
    if (!alreadyCompensated)
    {
        await UndoChangesAsync();
        foundry.SetProperty("compensated", true);
    }
}
```

### Performance Optimization

**Lazy Loading**
```csharp
private static readonly MemoryCache _cache = new();

public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken ct)
{
    var key = foundry.GetPropertyOrDefault<string>("lookup_key");
    
    if (!_cache.TryGetValue(key, out var cached))
    {
        cached = await FetchExpensiveDataAsync(key);
        _cache.Set(key, cached, TimeSpan.FromMinutes(5));
        foundry.SetProperty("cache_hit", false);
    }
    else
    {
        foundry.SetProperty("cache_hit", true);
    }
    
    foundry.SetProperty("lookup_result", cached);
    return "Data loaded";
}
```

**Batch Processing**
```csharp
public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken ct)
{
    var items = foundry.GetPropertyOrDefault<List<Item>>("items");
    var batchSize = 100;
    var results = new List<Result>();
    
    for (int i = 0; i < items.Count; i += batchSize)
    {
        var batch = items.Skip(i).Take(batchSize).ToList();
        var batchResults = await ProcessBatchAsync(batch);
        results.AddRange(batchResults);
    }
    
    foundry.SetProperty("batch_results", results);
    return $"Processed {results.Count} items in batches";
}
```

### Type Safety

**Strongly Typed Generic Operations**
```csharp
public class TransformOrderOperation : IWorkflowOperation<Order, OrderDto>
{
    public string Name => "TransformOrder";
    
    public async Task<OrderDto> ForgeAsync(
        Order order,
        IWorkflowFoundry foundry,
        CancellationToken ct)
    {
        var dto = new OrderDto
        {
            Id = order.Id,
            Total = order.Items.Sum(i => i.Price),
            ItemCount = order.Items.Count
        };
        
        foundry.SetProperty("order_dto", dto);
        return dto;
    }
}
```

## Validation Operations (NEW in 2.0.0)

WorkflowForge 2.0.0 introduces the **Validation extension** for comprehensive validation using FluentValidation.

### Using Validation Middleware

The recommended pattern is to use validation middleware:

```csharp
using WorkflowForge.Extensions.Validation;

// Define validator
public class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).Must(c => c == "USD" || c == "EUR");
    }
}

// Add validation to foundry
var foundry = WorkflowForge.CreateFoundry("OrderProcessing");
foundry.SetProperty("Order", new Order { /* ... */ });

var validator = new OrderValidator();
foundry.AddValidation(
    validator,
    f => f.GetPropertyOrDefault<Order>("Order"),
    throwOnFailure: true);

// Validation runs automatically before each operation
var workflow = WorkflowForge.CreateWorkflow("ProcessOrder")
    .AddOperation(new ProcessOrderOperation())
    .AddOperation(new ChargePaymentOperation())
    .Build();

using var smith = WorkflowForge.CreateSmith();
await smith.ForgeAsync(workflow, foundry);
```

### Manual Validation in Operations

For operation-specific validation:

```csharp
public class ProcessOrderOperation : IWorkflowOperation
{
    public async Task<object?> ForgeAsync(
        IWorkflowFoundry foundry,
        object? inputData,
        CancellationToken cancellationToken)
    {
        var order = foundry.GetPropertyOrDefault<Order>("Order");
        var validator = new OrderValidator();
        
        // Manual validation
        var result = await foundry.ValidateAsync(validator, order, cancellationToken);
        
        if (!result.IsValid)
        {
            throw new WorkflowValidationException(
                "Order validation failed",
                result.Errors);
        }
        
        // Continue processing...
        return null;
    }
    
    public Task<object?> RestoreAsync(
        IWorkflowFoundry foundry,
        object? inputData,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<object?>(null);
    }
    
    public void Dispose() { }
}
```

### Custom Validators

Implement `IWorkflowValidator<T>` for custom validation logic:

```csharp
public class BusinessRuleValidator : IWorkflowValidator<Order>
{
    public async Task<ValidationResult> ValidateAsync(
        Order data,
        CancellationToken cancellationToken)
    {
        if (data.Amount > 10000 && !data.RequiresApproval)
        {
            return ValidationResult.Failure(
                new ValidationError("RequiresApproval", 
                    "Orders over $10,000 require approval"));
        }
        
        return ValidationResult.Success;
    }
}
```

See the **[Validation Extension README](../src/extensions/WorkflowForge.Extensions.Validation/README.md)** for complete documentation.

## Audit Operations (NEW in 2.0.0)

WorkflowForge 2.0.0 introduces the **Audit extension** for compliance and operational monitoring.

### Using Audit Middleware

```csharp
using WorkflowForge.Extensions.Audit;

// Implement audit provider
public class DatabaseAuditProvider : IAuditProvider
{
    private readonly DbContext _dbContext;
    
    public async Task WriteAuditEntryAsync(
        AuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.AuditLog.AddAsync(entry, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
    
    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}

// Enable audit logging
var auditProvider = new DatabaseAuditProvider(dbContext);
var foundry = WorkflowForge.CreateFoundry("OrderProcessing");

foundry.EnableAuditLogging(
    auditProvider,
    userId: "user@example.com",
    sessionId: Guid.NewGuid().ToString());

// Audit entries automatically recorded for all operations
var workflow = WorkflowForge.CreateWorkflow("ProcessOrder")
    .AddOperation(new ValidateOrderOperation())
    .AddOperation(new ChargePaymentOperation())
    .Build();

using var smith = WorkflowForge.CreateSmith();
await smith.ForgeAsync(workflow, foundry);

// Audit log contains:
// - Workflow start/completion/failure
// - Each operation start/completion/failure
// - User ID, session ID, timestamps
// - Property snapshots (before/after)
```

See the **[Audit Extension README](../src/extensions/WorkflowForge.Extensions.Audit/README.md)** for complete documentation.

## Related Documentation

- **[Getting Started](getting-started.md)** - Basic operation usage
- **[Architecture](architecture.md)** - Operation architecture
- **[Extensions Guide](extensions.md)** - All available extensions
- **[Validation Extension](../src/extensions/WorkflowForge.Extensions.Validation/README.md)** - Detailed validation patterns
- **[Audit Extension](../src/extensions/WorkflowForge.Extensions.Audit/README.md)** - Detailed audit patterns

---

**WorkflowForge Operations** - *Build powerful, reusable workflow components* 