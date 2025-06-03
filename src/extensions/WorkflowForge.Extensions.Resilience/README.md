# WorkflowForge.Extensions.Resilience

Base resilience patterns extension for WorkflowForge operations. This extension provides fundamental retry logic and resilience strategies for building fault-tolerant workflows.

## 🎯 Extension Overview

The Base Resilience extension brings core resilience patterns to WorkflowForge applications, including:

- **🔄 Retry Strategies**: Exponential backoff, fixed interval, and random interval retry patterns
- **🎯 Standardized Patterns**: Unified `IWorkflowResilienceStrategy` pattern for consistency
- **⚙️ Flexible Configuration**: Rich configuration options through `RetryPolicySettings`
- **🔧 Operation Wrappers**: Easy-to-use operation wrappers with built-in resilience
- **🧪 Testability**: Easily testable retry scenarios and strategies
- **📈 Extensibility**: Simple framework for adding new resilience strategies
- **🏭 Foundry Integration**: Deep integration with WorkflowForge foundries
- **⚡ Performance Optimized**: Efficient retry patterns with minimal overhead

## 📦 Installation

```bash
dotnet add package WorkflowForge.Extensions.Resilience
```

## 🚀 Quick Start

### 1. Basic Retry with Exponential Backoff

```csharp
using WorkflowForge;
using WorkflowForge.Extensions.Resilience;

// Create a resilient operation with exponential backoff
var paymentOperation = new ProcessPaymentOperation();
var resilientPayment = RetryWorkflowOperation.WithExponentialBackoff(
    operation: paymentOperation,
    baseDelay: TimeSpan.FromMilliseconds(100),
    maxDelay: TimeSpan.FromSeconds(30),
    maxAttempts: 3);

// Use in workflow
var workflow = WorkflowForge.CreateWorkflow("ProcessOrder")
    .AddOperation("ValidateOrder", new ValidateOrderOperation())
    .AddOperation("ProcessPayment", resilientPayment) // Resilient operation
    .AddOperation("FulfillOrder", new FulfillOrderOperation())
    .Build();

var foundry = WorkflowForge.CreateFoundry("OrderProcessing");
var smith = WorkflowForge.CreateSmith();

foundry.Properties["orderData"] = orderData; // Set order data in foundry
await smith.ForgeAsync(workflow, foundry);
```

### 2. Fixed Interval Retry

```csharp
// Retry with fixed delays - good for database operations
var databaseOperation = new UpdateInventoryOperation();
var resilientDatabase = RetryWorkflowOperation.WithFixedInterval(
    operation: databaseOperation,
    interval: TimeSpan.FromSeconds(1),
    maxAttempts: 5);
```

### 3. Random Interval Retry

```csharp
// Retry with random delays - helps avoid thundering herd
var externalApiOperation = new CallExternalApiOperation();
var resilientApi = RetryWorkflowOperation.WithRandomInterval(
    operation: externalApiOperation,
    minDelay: TimeSpan.FromMilliseconds(100),
    maxDelay: TimeSpan.FromSeconds(5),
    maxAttempts: 3);
```

## 🔧 Retry Strategies

### Exponential Backoff Strategy

Best for external service calls to avoid overwhelming failing services:

```csharp
var retryOperation = RetryWorkflowOperation.WithExponentialBackoff(
    operation: myOperation,
    baseDelay: TimeSpan.FromMilliseconds(100),    // Start with 100ms
    maxDelay: TimeSpan.FromSeconds(30),           // Cap at 30 seconds
    maxAttempts: 5,                               // Try up to 5 times
    backoffMultiplier: 2.0,                       // Double delay each time
    useJitter: true);                             // Add randomization
```

### Fixed Interval Strategy

Best for predictable systems like databases:

```csharp
var retryOperation = RetryWorkflowOperation.WithFixedInterval(
    operation: myOperation,
    interval: TimeSpan.FromSeconds(2),            // Wait 2 seconds between attempts
    maxAttempts: 3);                              // Try up to 3 times
```

### Random Interval Strategy

Best for high-volume scenarios to prevent synchronized retries:

```csharp
var retryOperation = RetryWorkflowOperation.WithRandomInterval(
    operation: myOperation,
    minDelay: TimeSpan.FromMilliseconds(500),     // Minimum wait time
    maxDelay: TimeSpan.FromSeconds(10),           // Maximum wait time
    maxAttempts: 4);                              // Try up to 4 times
```

## ⚙️ Configuration-Based Retry

### Using RetryPolicySettings

```csharp
var retrySettings = new RetryPolicySettings
{
    StrategyType = RetryStrategyType.ExponentialBackoff,
    MaxAttempts = 3,
    BaseDelay = TimeSpan.FromMilliseconds(100),
    MaxDelay = TimeSpan.FromSeconds(30),
    BackoffMultiplier = 2.0,
    UseJitter = true,
    RetryableExceptions = new[]
    {
        typeof(HttpRequestException),
        typeof(TimeoutException),
        typeof(SocketException)
    }
};

var retryOperation = RetryWorkflowOperation.FromSettings(
    operation: myOperation,
    settings: retrySettings);
```

### Configuration from appsettings.json

```json
{
  "WorkflowForge": {
    "Resilience": {
      "DefaultRetryPolicy": {
        "StrategyType": "ExponentialBackoff",
        "MaxAttempts": 3,
        "BaseDelay": "00:00:00.100",
        "MaxDelay": "00:00:30",
        "BackoffMultiplier": 2.0,
        "UseJitter": true
      },
      "Policies": {
        "DatabaseOperations": {
          "StrategyType": "FixedInterval",
          "MaxAttempts": 5,
          "BaseDelay": "00:00:01"
        },
        "ExternalApi": {
          "StrategyType": "RandomInterval",
          "MaxAttempts": 3,
          "BaseDelay": "00:00:00.500",
          "MaxDelay": "00:00:05"
        }
      }
    }
  }
}
```

```csharp
// Load from configuration
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var retrySettings = configuration
    .GetSection("WorkflowForge:Resilience:Policies:ExternalApi")
    .Get<RetryPolicySettings>();

var retryOperation = RetryWorkflowOperation.FromSettings(operation, retrySettings);
```

## 🎯 Exception Filtering

### Custom Exception Filtering

```csharp
// Only retry on specific exceptions
var retryOperation = RetryWorkflowOperation.WithExponentialBackoff(
    operation: myOperation,
    retryPredicate: ex => ex is TimeoutException || 
                         ex is HttpRequestException ||
                         (ex is SocketException socketEx && socketEx.SocketErrorCode == SocketError.ConnectionRefused));
```

### Transient Error Optimization

```csharp
// Optimized for common transient errors
var retryOperation = RetryWorkflowOperation.ForTransientErrors(
    operation: myOperation,
    maxAttempts: 5);

// Equivalent to:
var retryOperation = RetryWorkflowOperation.WithExponentialBackoff(
    operation: myOperation,
    retryPredicate: ex => IsTransientError(ex),
    maxAttempts: 5);
```

### Business Logic Exception Handling

```csharp
// Custom business logic for retry decisions
var retryOperation = RetryWorkflowOperation.WithExponentialBackoff(
    operation: myOperation,
    retryPredicate: ex =>
    {
        // Don't retry on validation errors
        if (ex is ValidationException) return false;
        
        // Don't retry on authentication errors
        if (ex is UnauthorizedAccessException) return false;
        
        // Don't retry on business rule violations
        if (ex is BusinessRuleException) return false;
        
        // Retry on infrastructure errors
        return ex is HttpRequestException ||
               ex is TimeoutException ||
               ex is TaskCanceledException;
    });
```

## 🔗 Integration with Foundries

### Foundry-Level Resilience Configuration

```csharp
// Configure resilience at the foundry level
var foundryConfig = FoundryConfiguration.ForProduction()
    .UseResilienceDefaults()
    .ConfigureResilience(options =>
    {
        options.DefaultRetryStrategy = RetryStrategyType.ExponentialBackoff;
        options.DefaultMaxAttempts = 3;
        options.DefaultBaseDelay = TimeSpan.FromMilliseconds(100);
    });

var foundry = WorkflowForge.CreateFoundry("ResilientWorkflow", foundryConfig);
```

### Operation-Level Resilience Override

```csharp
public class ResilientDatabaseOperation : IWorkflowOperation
{
    private readonly IWorkflowOperation _innerOperation;
    private readonly RetryWorkflowOperation _retryWrapper;

    public ResilientDatabaseOperation(IWorkflowOperation innerOperation)
    {
        _innerOperation = innerOperation;
        _retryWrapper = RetryWorkflowOperation.WithFixedInterval(
            operation: innerOperation,
            interval: TimeSpan.FromSeconds(1),
            maxAttempts: 5);
    }

    public string Name => $"Resilient{_innerOperation.Name}";
    public bool SupportsRestore => _innerOperation.SupportsRestore;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        foundry.Logger.LogInformation("Executing operation {OperationName} with resilience", Name);
        return await _retryWrapper.ForgeAsync(inputData, foundry, cancellationToken);
    }

    public async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        if (SupportsRestore)
        {
            await _retryWrapper.RestoreAsync(outputData, foundry, cancellationToken);
        }
    }
}
```

## 📊 Advanced Retry Scenarios

### Multi-Stage Retry with Different Strategies

```csharp
public class MultiStageRetryOperation : IWorkflowOperation
{
    private readonly IWorkflowOperation _operation;

    public string Name => "MultiStageRetry";
    public bool SupportsRestore => _operation.SupportsRestore;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        // Stage 1: Fast retries for quick recovery
        try
        {
            var fastRetry = RetryWorkflowOperation.WithFixedInterval(
                operation: _operation,
                interval: TimeSpan.FromMilliseconds(100),
                maxAttempts: 3);
            
            return await fastRetry.ForgeAsync(inputData, foundry, cancellationToken);
        }
        catch (Exception ex) when (IsRetryableException(ex))
        {
            foundry.Logger.LogWarning("Fast retry failed, switching to exponential backoff");
        }

        // Stage 2: Exponential backoff for persistent issues
        var slowRetry = RetryWorkflowOperation.WithExponentialBackoff(
            operation: _operation,
            baseDelay: TimeSpan.FromSeconds(1),
            maxDelay: TimeSpan.FromMinutes(1),
            maxAttempts: 3);

        return await slowRetry.ForgeAsync(inputData, foundry, cancellationToken);
    }
}
```

### Conditional Retry Based on Context

```csharp
public class ContextAwareRetryOperation : IWorkflowOperation
{
    private readonly IWorkflowOperation _operation;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        // Get retry configuration from foundry context
        var retryConfig = foundry.Properties.GetValueOrDefault("RetryConfig") as RetryPolicySettings;
        var isHighPriority = foundry.Properties.GetValueOrDefault("IsHighPriority", false);

        RetryWorkflowOperation retryOperation;

        if (isHighPriority)
        {
            // High priority: more aggressive retries
            retryOperation = RetryWorkflowOperation.WithExponentialBackoff(
                operation: _operation,
                baseDelay: TimeSpan.FromMilliseconds(50),
                maxAttempts: 5);
        }
        else if (retryConfig != null)
        {
            // Use configured policy
            retryOperation = RetryWorkflowOperation.FromSettings(_operation, retryConfig);
        }
        else
        {
            // Default conservative retry
            retryOperation = RetryWorkflowOperation.WithFixedInterval(
                operation: _operation,
                interval: TimeSpan.FromSeconds(2),
                maxAttempts: 2);
        }

        return await retryOperation.ForgeAsync(inputData, foundry, cancellationToken);
    }
}
```

## 🧪 Testing Resilience Patterns

### Unit Testing Retry Logic

```csharp
[Test]
public async Task Should_Retry_On_Transient_Failures()
{
    // Arrange
    var mockOperation = new Mock<IWorkflowOperation>();
    var attempts = 0;
    
    mockOperation.Setup(x => x.ForgeAsync(It.IsAny<object>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()))
             .Returns(() =>
             {
                 attempts++;
                 if (attempts < 3)
                     throw new HttpRequestException("Transient error");
                 return Task.FromResult<object?>("success");
             });

    var retryOperation = RetryWorkflowOperation.WithFixedInterval(
        operation: mockOperation.Object,
        interval: TimeSpan.FromMilliseconds(10), // Fast for testing
        maxAttempts: 3);

    var foundry = WorkflowForge.CreateFoundry("Test");

    // Act
    var result = await retryOperation.ForgeAsync("input", foundry);

    // Assert
    Assert.That(result, Is.EqualTo("success"));
    Assert.That(attempts, Is.EqualTo(3));
    mockOperation.Verify(x => x.ForgeAsync("input", foundry, It.IsAny<CancellationToken>()), Times.Exactly(3));
}
```

### Integration Testing with Real Dependencies

```csharp
[Test]
public async Task Should_Handle_Real_Service_Failures()
{
    // Arrange
    var httpClient = new HttpClient();
    var apiOperation = new CallExternalApiOperation(httpClient, "http://unreliable-service/api");
    
    var retryOperation = RetryWorkflowOperation.WithExponentialBackoff(
        operation: apiOperation,
        baseDelay: TimeSpan.FromMilliseconds(100),
        maxAttempts: 3);

    var foundry = WorkflowForge.CreateFoundry("IntegrationTest");

    // Act & Assert
    // This would test against a real service or test double
    var result = await retryOperation.ForgeAsync(requestData, foundry);
    Assert.That(result, Is.Not.Null);
}
```

## 🎯 Best Practices

### 1. Choose Appropriate Retry Strategies
```csharp
// ✅ Good: Exponential backoff for external services
var apiRetry = RetryWorkflowOperation.WithExponentialBackoff(
    operation: apiOperation,
    baseDelay: TimeSpan.FromMilliseconds(100),
    maxDelay: TimeSpan.FromSeconds(30),
    maxAttempts: 3);

// ✅ Good: Fixed interval for databases
var dbRetry = RetryWorkflowOperation.WithFixedInterval(
    operation: dbOperation,
    interval: TimeSpan.FromSeconds(1),
    maxAttempts: 5);

// ❌ Avoid: Fixed short intervals for external services
var badRetry = RetryWorkflowOperation.WithFixedInterval(
    operation: apiOperation,
    interval: TimeSpan.FromMilliseconds(100), // Too aggressive
    maxAttempts: 10); // Too many attempts
```

### 2. Proper Exception Filtering
```csharp
// ✅ Good: Specific exception handling
var retryOperation = RetryWorkflowOperation.WithExponentialBackoff(
    operation: operation,
    retryPredicate: ex => ex is HttpRequestException || ex is TimeoutException);

// ❌ Avoid: Catching all exceptions
var badRetry = RetryWorkflowOperation.WithExponentialBackoff(
    operation: operation,
    retryPredicate: ex => true); // Will retry on all exceptions, including non-recoverable ones
```

### 3. Appropriate Retry Limits
```csharp
// ✅ Good: Reasonable limits based on context
var criticalOperation = RetryWorkflowOperation.WithExponentialBackoff(
    operation: operation,
    maxAttempts: 5); // More attempts for critical operations

var nonCriticalOperation = RetryWorkflowOperation.WithFixedInterval(
    operation: operation,
    maxAttempts: 2); // Fewer attempts for non-critical operations
```

## 🔗 Integration with Advanced Resilience

For advanced resilience patterns like circuit breakers and timeouts, consider using:

- **[WorkflowForge.Extensions.Resilience.Polly](../WorkflowForge.Extensions.Resilience.Polly/README.md)** - Advanced resilience with Polly integration

```csharp
// Upgrade to Polly for advanced patterns
var advancedResilience = operation
    .WithPollyRetry(maxRetries: 3)
    .WithPollyCircuitBreaker(failureThreshold: 5)
    .WithPollyTimeout(TimeSpan.FromSeconds(30));
```

## 📚 Additional Resources

- [Core Framework Documentation](../WorkflowForge/README.md)
- [Advanced Polly Resilience Extension](../WorkflowForge.Extensions.Resilience.Polly/README.md)
- [Performance Monitoring Extension](../WorkflowForge.Extensions.Observability.Performance/README.md)
- [Main Project Documentation](../../README.md)

---

**WorkflowForge.Extensions.Resilience** - *Foundation resilience patterns for reliable workflows* 