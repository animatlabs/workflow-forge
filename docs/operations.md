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
        var emailService = foundry.GetService<IEmailService>();
        var messageId = await emailService.SendAsync(emailRequest, cancellationToken);
        
        // Store message ID for potential rollback
        foundry.SetProperty("EmailMessageId", messageId);
        
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
        var messageId = foundry.GetProperty<string>("EmailMessageId");
        
        if (!string.IsNullOrEmpty(messageId))
        {
            foundry.Logger.LogWarning("Attempting to recall email {MessageId}", messageId);
            
            var emailService = foundry.GetService<IEmailService>();
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
        foundry.SetProperty("BatchId", batchId);
        foundry.SetProperty("ProcessedItems", new List<object>());

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
                foundry.SetProperty("ProcessedItems", processedItems);
                
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
        var batchId = foundry.GetProperty<Guid>("BatchId");
        var processedItems = foundry.GetProperty<List<object>>("ProcessedItems");

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
        foundry.SetProperty("ConditionResult", conditionResult);

        if (conditionResult)
        {
            foundry.SetProperty("ExecutedOperation", "True");
            return await _trueOperation.ForgeAsync(inputData, foundry, cancellationToken);
        }
        else if (_falseOperation != null)
        {
            foundry.SetProperty("ExecutedOperation", "False");
            return await _falseOperation.ForgeAsync(inputData, foundry, cancellationToken);
        }
        else
        {
            foundry.SetProperty("ExecutedOperation", "None");
            foundry.Logger.LogInformation("Condition was false and no false operation provided");
            return inputData;
        }
    }

    public async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var executedOperation = foundry.GetProperty<string>("ExecutedOperation");
        
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
        foundry.SetProperty("CompletedOperations", completedOperations.ToList());

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
        var completedOperations = foundry.GetProperty<List<(IWorkflowOperation Operation, object? Result)>>("CompletedOperations");

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

## Operation Testing

### Unit Testing Operations

```csharp
public class EmailNotificationOperationTests
{
    [Fact]
    public async Task Should_Send_Email_Successfully()
    {
        // Arrange
        var mockEmailService = new Mock<IEmailService>();
        var mockFoundry = new Mock<IWorkflowFoundry>();
        var mockLogger = new Mock<IWorkflowForgeLogger>();

        mockFoundry.Setup(x => x.Logger).Returns(mockLogger.Object);
        mockFoundry.Setup(x => x.GetService<IEmailService>()).Returns(mockEmailService.Object);
        mockEmailService.Setup(x => x.SendAsync(It.IsAny<EmailRequest>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync("MSG123");

        var operation = new EmailNotificationOperation();
        var emailRequest = new EmailRequest { Recipient = "test@example.com", Subject = "Test" };

        // Act
        var result = await operation.ForgeAsync(emailRequest, mockFoundry.Object, CancellationToken.None);

        // Assert
        var response = Assert.IsType<EmailResponse>(result);
        Assert.Equal("MSG123", response.MessageId);
        Assert.True(response.Success);

        mockEmailService.Verify(x => x.SendAsync(emailRequest, It.IsAny<CancellationToken>()), Times.Once);
        mockFoundry.Verify(x => x.SetProperty("EmailMessageId", "MSG123"), Times.Once);
    }

    [Fact]
    public async Task Should_Rollback_Email_When_Restored()
    {
        // Arrange
        var mockEmailService = new Mock<IEmailService>();
        var mockFoundry = new Mock<IWorkflowFoundry>();
        var mockLogger = new Mock<IWorkflowForgeLogger>();

        mockFoundry.Setup(x => x.Logger).Returns(mockLogger.Object);
        mockFoundry.Setup(x => x.GetService<IEmailService>()).Returns(mockEmailService.Object);
        mockFoundry.Setup(x => x.GetProperty<string>("EmailMessageId")).Returns("MSG123");

        var operation = new EmailNotificationOperation();

        // Act
        await operation.RestoreAsync(null, mockFoundry.Object, CancellationToken.None);

        // Assert
        mockEmailService.Verify(x => x.RecallAsync("MSG123", It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### Integration Testing

```csharp
public class OperationIntegrationTests
{
    [Fact]
    public async Task Should_Execute_Operation_In_Workflow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IEmailService, MockEmailService>();
        var serviceProvider = services.BuildServiceProvider();

        var foundry = WorkflowForge.CreateFoundry("TestWorkflow", serviceProvider);
        
        var workflow = WorkflowForge.CreateWorkflow()
            .WithName("EmailWorkflow")
            .AddOperation(new EmailNotificationOperation())
            .Build();

        var smith = WorkflowForge.CreateSmith();
        var emailRequest = new EmailRequest { Recipient = "test@example.com", Subject = "Integration Test" };

        // Act
        var result = await smith.ForgeAsync(workflow, emailRequest, foundry);

        // Assert
        var response = Assert.IsType<EmailResponse>(result);
        Assert.True(response.Success);
    }
}
```

## Operation Best Practices

### 1. Resource Management

```csharp
public class DatabaseOperation : IWorkflowOperation, IDisposable
{
    private readonly IDbConnection _connection;
    private bool _disposed = false;

    public DatabaseOperation(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken);
        }

        // Database operations
        using var command = _connection.CreateCommand();
        // ... implementation

        return result;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _connection?.Dispose();
            _disposed = true;
        }
    }
}
```

### 2. Error Handling

```csharp
public class RobustOperation : IWorkflowOperation
{
    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        try
        {
            // Operation logic
            return await ProcessAsync(inputData, foundry, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            foundry.Logger.LogWarning("Operation was cancelled");
            throw;
        }
        catch (ValidationException ex)
        {
            foundry.Logger.LogError(ex, "Validation failed for operation");
            throw new WorkflowOperationException("Input validation failed", ex);
        }
        catch (Exception ex)
        {
            foundry.Logger.LogError(ex, "Unexpected error in operation");
            throw new WorkflowOperationException("Operation failed unexpectedly", ex);
        }
    }
}
```

### 3. Performance Optimization

```csharp
public class OptimizedOperation : IWorkflowOperation
{
    private static readonly ObjectPool<StringBuilder> StringBuilderPool = 
        new DefaultObjectPool<StringBuilder>(new StringBuilderPooledObjectPolicy());

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        // Use object pooling for frequently allocated objects
        var sb = StringBuilderPool.Get();
        try
        {
            // Process using pooled object
            sb.Append(inputData?.ToString());
            // ... processing

            return sb.ToString();
        }
        finally
        {
            StringBuilderPool.Return(sb);
        }
    }
}
```

## Related Documentation

- **[Getting Started](getting-started.md)** - Basic operation usage
- **[Architecture](architecture.md)** - Operation architecture
- **[Middleware](middleware.md)** - Cross-cutting concerns
- **[Testing](testing.md)** - Testing strategies

---

**WorkflowForge Operations** - *Build powerful, reusable workflow components* 