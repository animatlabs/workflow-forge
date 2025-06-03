# WorkflowForge.Extensions.Resilience.Polly

Advanced resilience extension for WorkflowForge using the battle-tested Polly library. This extension provides advanced circuit breakers, retry policies, timeout management, and rate limiting for robust workflow execution.

## 🎯 Extension Overview

The Polly extension brings advanced resilience patterns to WorkflowForge applications, including:

- **🔄 Advanced Retry Policies**: Exponential backoff with configurable delays, jitter, and max attempts
- **⚡ Circuit Breakers**: Fail-fast pattern with configurable failure thresholds and break durations
- **⏱️ Timeouts**: Operation-level timeouts with cooperative cancellation
- **🚦 Rate Limiting**: Resource throttling using sliding window approach
- **🔗 Policy Composition**: Ability to combine multiple resilience strategies in pipelines
- **⚙️ Configuration Support**: Complete configuration system with validation
- **💉 Dependency Injection**: Full DI support with service collection extensions
- **🌍 Flexible Configurations**: Pre-configured setups for different deployment scenarios
- **📊 Comprehensive Logging**: Full integration with WorkflowForge's structured logging system

## 📦 Installation

```bash
dotnet add package WorkflowForge.Extensions.Resilience.Polly
```

## 🚀 Quick Start

### 1. Basic Foundry Configuration

```csharp
using WorkflowForge;
using WorkflowForge.Extensions.Resilience.Polly;

// Create foundry with Polly resilience
var foundryConfig = FoundryConfiguration.ForProduction()
    .UsePollyProductionResilience();

var foundry = WorkflowForge.CreateFoundry("MyWorkflow", foundryConfig);

// Execute workflows with automatic resilience
var smith = WorkflowForge.CreateSmith();
await smith.ForgeAsync(workflow, foundry);
```

### 2. Configuration Profiles

```csharp
// Lenient settings for testing and iteration
var foundryConfig = FoundryConfiguration.ForDevelopment()
    .UsePollyDevelopmentResilience();

// Balanced settings for general use
var foundryConfig = FoundryConfiguration.ForProduction()
    .UsePollyProductionResilience();

// Comprehensive settings for maximum resilience
var foundryConfig = FoundryConfiguration.ForEnterprise()
    .UsePollyEnterpriseResilience();
```

### 3. Custom Resilience Configuration

```csharp
// Custom Polly configuration
var foundryConfig = FoundryConfiguration.ForProduction()
    .UsePollyRetry(maxRetryAttempts: 5, baseDelay: TimeSpan.FromSeconds(1))
    .UsePollyCircuitBreaker(failureThreshold: 3, breakDuration: TimeSpan.FromMinutes(1))
    .UsePollyTimeout(TimeSpan.FromSeconds(30))
    .UsePollyRateLimit(permitLimit: 10, window: TimeSpan.FromSeconds(1));

var foundry = WorkflowForge.CreateFoundry("MyWorkflow", foundryConfig);
```

## ⚙️ Configuration File Support

Add comprehensive Polly configuration to your `appsettings.json`:

```json
{
  "WorkflowForge": {
    "Polly": {
      "IsEnabled": true,
      "EnableComprehensivePolicies": true,
      "EnableDetailedLogging": true,
      "Retry": {
        "IsEnabled": true,
        "MaxRetryAttempts": 3,
        "BaseDelay": "00:00:01",
        "MaxDelay": "00:00:30",
        "UseJitter": true,
        "BackoffType": "Exponential"
      },
      "CircuitBreaker": {
        "IsEnabled": true,
        "FailureThreshold": 5,
        "DurationOfBreak": "00:00:30",
        "MinimumThroughput": 5,
        "SamplingDuration": "00:01:00"
      },
      "Timeout": {
        "IsEnabled": true,
        "TimeoutDuration": "00:00:30"
      },
      "RateLimiter": {
        "IsEnabled": false,
        "PermitLimit": 10,
        "Window": "00:00:01",
        "QueueLimit": 5
      }
    }
  }
}
```

Then configure using dependency injection:

```csharp
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Extensions.Resilience.Polly;

// Register Polly services with configuration
services.AddWorkflowForgePolly(configuration, "WorkflowForge:Polly");

// Or with direct options
services.AddWorkflowForgePolly(options =>
{
    options.Retry.MaxRetryAttempts = 5;
    options.CircuitBreaker.IsEnabled = true;
    options.CircuitBreaker.FailureThreshold = 3;
    options.EnableDetailedLogging = true;
});

// Use in your application
var foundry = serviceProvider.GetRequiredService<IWorkflowFoundry>();
```

## 🌍 Environment-Specific Configurations

### Development Configuration
For development environments with lenient policies:

```csharp
// Foundry configuration
foundryConfig.UsePollyDevelopmentResilience();

// Or via DI
services.AddWorkflowForgePollyForDevelopment();
```

**Development Features:**
- ✅ 2 retry attempts with 200ms base delay
- ✅ 2-minute operation timeouts  
- ❌ Circuit breaker disabled
- ✅ Detailed logging enabled
- ❌ Rate limiting disabled

### Production Configuration
For production environments with balanced resilience:

```csharp
// Foundry configuration
foundryConfig.UsePollyProductionResilience();

// Or via DI
services.AddWorkflowForgePollyForProduction();
```

**Production Features:**
- ✅ 5 retry attempts with 100ms base delay
- ✅ 10-second operation timeouts
- ✅ Circuit breaker with 3 failure threshold
- ✅ Rate limiting (10 permits/second)
- ⚠️ Minimal logging for performance

### Enterprise Configuration
For enterprise environments with comprehensive policies:

```csharp
// Foundry configuration
foundryConfig.UsePollyEnterpriseResilience();

// Or via DI
services.AddWorkflowForgePollyForEnterprise();
```

**Enterprise Features:**
- ✅ 3 retry attempts with 500ms base delay
- ✅ 30-second operation timeouts
- ✅ Circuit breaker with 5 failure threshold  
- ✅ Rate limiting (20 permits/second)
- ✅ Comprehensive logging and metrics

## 🔧 Operation-Level Resilience

### Wrapping Individual Operations

```csharp
using WorkflowForge.Extensions.Resilience.Polly;

// Wrap existing operations with Polly resilience
var paymentOperation = new ProcessPaymentOperation();

// Add retry policy
var resilientPayment = paymentOperation.WithPollyRetry(
    maxRetryAttempts: 5,
    baseDelay: TimeSpan.FromMilliseconds(500));

// Add circuit breaker
var protectedPayment = paymentOperation.WithPollyCircuitBreaker(
    failureThreshold: 3,
    breakDuration: TimeSpan.FromMinutes(1));

// Combine multiple policies
var enterprisePayment = paymentOperation
    .WithPollyRetry(maxRetries: 3)
    .WithPollyCircuitBreaker(failureThreshold: 5)
    .WithPollyTimeout(TimeSpan.FromSeconds(30));

// Use in workflow
var workflow = WorkflowForge.CreateWorkflow("ProcessOrder")
    .AddOperation(enterprisePayment)
    .Build();
```

### Creating Polly-Enabled Operations

```csharp
// Create operations with built-in Polly policies
public class ResilientApiOperation : IWorkflowOperation
{
    private readonly HttpClient _httpClient;
    private readonly ResiliencePipeline _pipeline;

    public ResilientApiOperation(HttpClient httpClient)
    {
        _httpClient = httpClient;
        
        // Build custom resilience pipeline
        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 5
            })
            .AddTimeout(TimeSpan.FromSeconds(10))
            .Build();
    }

    public string Name => "ResilientApiCall";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        return await _pipeline.ExecuteAsync(async (ct) =>
        {
            foundry.Logger.LogInformation("Executing API call with resilience");
            var response = await _httpClient.GetAsync("https://api.example.com/data", ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(ct);
        }, cancellationToken);
    }
}
```

## 🔄 Retry Policies

### Exponential Backoff with Jitter

```csharp
foundryConfig.UsePollyRetry(options =>
{
    options.MaxRetryAttempts = 5;
    options.BaseDelay = TimeSpan.FromMilliseconds(500);
    options.MaxDelay = TimeSpan.FromSeconds(30);
    options.BackoffType = DelayBackoffType.Exponential;
    options.UseJitter = true; // Adds randomization to prevent thundering herd
});
```

### Conditional Retry Logic

```csharp
// Retry only on specific exceptions
var retryOperation = baseOperation.WithPollyRetry(
    shouldRetry: ex => ex is HttpRequestException || ex is TaskCanceledException,
    maxRetryAttempts: 3,
    baseDelay: TimeSpan.FromSeconds(1)
);
```

## ⚡ Circuit Breaker Patterns

### Basic Circuit Breaker

```csharp
foundryConfig.UsePollyCircuitBreaker(options =>
{
    options.FailureThreshold = 5;          // Open after 5 failures
    options.DurationOfBreak = TimeSpan.FromMinutes(1); // Stay open for 1 minute
    options.MinimumThroughput = 10;        // Minimum calls before evaluation
    options.SamplingDuration = TimeSpan.FromMinutes(2); // Evaluation window
});
```

### Advanced Circuit Breaker

```csharp
// Circuit breaker with custom failure ratio
foundryConfig.UsePollyCircuitBreaker(options =>
{
    options.FailureRatio = 0.6;            // Open when 60% of calls fail
    options.MinimumThroughput = 20;        // Need at least 20 calls
    options.SamplingDuration = TimeSpan.FromMinutes(5);
    options.DurationOfBreak = TimeSpan.FromMinutes(2);
    
    // Custom state change handlers
    options.OnOpened = (context) => 
    {
        context.Logger.LogWarning("Circuit breaker opened for {OperationName}", context.OperationName);
    };
    
    options.OnClosed = (context) =>
    {
        context.Logger.LogInformation("Circuit breaker closed for {OperationName}", context.OperationName);
    };
});
```

## ⏱️ Timeout Management

### Operation Timeouts

```csharp
// Simple timeout
foundryConfig.UsePollyTimeout(TimeSpan.FromSeconds(30));

// Optimistic timeout (cancellation token based)
foundryConfig.UsePollyTimeout(TimeSpan.FromSeconds(30), TimeoutStrategy.Optimistic);

// Pessimistic timeout (thread-based)
foundryConfig.UsePollyTimeout(TimeSpan.FromSeconds(30), TimeoutStrategy.Pessimistic);
```

## 🚦 Rate Limiting

### Sliding Window Rate Limiting

```csharp
foundryConfig.UsePollyRateLimit(options =>
{
    options.PermitLimit = 100;             // 100 permits
    options.Window = TimeSpan.FromMinutes(1); // Per minute
    options.ReplenishmentPeriod = TimeSpan.FromSeconds(10); // Replenish every 10s
    options.QueueLimit = 50;               // Queue up to 50 requests
    options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
});
```

## 📊 Advanced Scenarios

### Policy Composition

```csharp
// Combine multiple resilience strategies
var comprehensiveFoundryConfig = FoundryConfiguration.ForProduction()
    .UsePollyRetry(maxRetryAttempts: 3, baseDelay: TimeSpan.FromSeconds(1))
    .UsePollyCircuitBreaker(failureThreshold: 5, breakDuration: TimeSpan.FromMinutes(1))
    .UsePollyTimeout(TimeSpan.FromSeconds(30))
    .UsePollyRateLimit(permitLimit: 10, window: TimeSpan.FromSeconds(1))
    .UsePollyBulkhead(maxParallelization: 5, maxQueuingActions: 10);

var foundry = WorkflowForge.CreateFoundry("ComprehensiveWorkflow", comprehensiveFoundryConfig);
```

### Workflow-Level Resilience

```csharp
using WorkflowForge;
using WorkflowForge.Extensions.Resilience.Polly;

// Create workflow with resilient operations
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("ResilientOrder")
    .AddOperation(new ProcessPaymentOperation()
        .WithPollyRetry(maxRetries: 3, baseDelay: TimeSpan.FromSeconds(1)))
    .AddOperation(new SendEmailOperation()
        .WithPollyCircuitBreaker(exceptionsAllowedBeforeBreaking: 3))
    .Build();

// Configure foundry with Polly
var foundry = WorkflowForge.CreateFoundry("ResilientOrder")
    .UsePollyResilience();

// Execute with automatic resilience
using var smith = WorkflowForge.CreateSmith();
await smith.ForgeAsync(workflow, foundry);
```

## 📈 Monitoring and Metrics

### Resilience Events Logging

```csharp
foundryConfig.UsePollyWithEvents(events =>
{
    events.OnRetry = (context, retryCount, delay) =>
    {
        context.Logger.LogWarning("Retry attempt {RetryCount} for {OperationName} after {Delay}ms delay",
            retryCount, context.OperationName, delay.TotalMilliseconds);
    };
    
    events.OnCircuitBreakerOpened = (context, breakDuration) =>
    {
        context.Logger.LogError("Circuit breaker opened for {OperationName}, break duration: {BreakDuration}",
            context.OperationName, breakDuration);
    };
    
    events.OnTimeout = (context, timeout) =>
    {
        context.Logger.LogError("Operation {OperationName} timed out after {Timeout}ms",
            context.OperationName, timeout.TotalMilliseconds);
    };
});
```

### Integration with Performance Monitoring

```csharp
// Combine with performance monitoring
var foundryConfig = FoundryConfiguration.ForProduction()
    .UsePollyProductionResilience()
    .EnablePerformanceMonitoring();

var foundry = WorkflowForge.CreateFoundry("MonitoredWorkflow", foundryConfig);

// Execute and analyze
await smith.ForgeAsync(workflow, foundry);

var perfStats = foundry.GetPerformanceStatistics();
Console.WriteLine($"Success Rate: {perfStats.SuccessRate:P2}");
Console.WriteLine($"Average Duration: {perfStats.AverageDuration}ms");
Console.WriteLine($"Retry Rate: {perfStats.GetRetryRate():P2}");
```

## 🎯 Best Practices

### 1. Choose Appropriate Timeouts
```csharp
// ✅ Good: Different timeouts for different operations
paymentOp.WithPollyTimeout(TimeSpan.FromSeconds(30));    // Payment: 30s
emailOp.WithPollyTimeout(TimeSpan.FromSeconds(10));      // Email: 10s  
databaseOp.WithPollyTimeout(TimeSpan.FromSeconds(5));    // Database: 5s

// ❌ Avoid: One-size-fits-all timeouts
```

### 2. Circuit Breaker Thresholds
```csharp
// ✅ Good: Based on actual failure patterns
foundryConfig.UsePollyCircuitBreaker(
    failureThreshold: 5,        // Open after 5 consecutive failures
    breakDuration: TimeSpan.FromMinutes(1), // Brief recovery period
    minThroughput: 10           // Need data to make decisions
);

// ❌ Avoid: Too sensitive thresholds
foundryConfig.UsePollyCircuitBreaker(failureThreshold: 1); // Too aggressive
```

### 3. Retry Strategy Selection
```csharp
// ✅ Good: Exponential backoff with jitter for external services
externalApiOp.WithPollyRetry(
    maxRetryAttempts: 3,
    baseDelay: TimeSpan.FromSeconds(1),
    backoffType: DelayBackoffType.Exponential,
    useJitter: true
);

// ✅ Good: Fixed delay for database operations
databaseOp.WithPollyRetry(
    maxRetryAttempts: 5,
    fixedDelay: TimeSpan.FromMilliseconds(100)
);
```

## 🔗 Integration with Other Extensions

### With Logging (Serilog)
```csharp
var foundryConfig = FoundryConfiguration.ForProduction()
    .UseSerilog()
    .UsePollyProductionResilience();

// Polly events will be logged with structured data
```

### With OpenTelemetry
```csharp
var foundryConfig = FoundryConfiguration.ForProduction()
    .UsePollyProductionResilience()
    .EnableOpenTelemetry("MyService", "1.0.0");

// Polly operations will be traced in distributed tracing
```

### With Health Checks
```csharp
var foundryConfig = FoundryConfiguration.ForProduction()
    .UsePollyProductionResilience()
    .EnableHealthChecks();

// Health checks will respect circuit breaker states
```

## 📚 Additional Resources

- [Core Framework Documentation](../WorkflowForge/README.md)
- [Base Resilience Extension](../WorkflowForge.Extensions.Resilience/README.md)
- [Performance Monitoring Extension](../WorkflowForge.Extensions.Observability.Performance/README.md)
- [Main Project Documentation](../../README.md)
- [Polly Documentation](https://www.pollydocs.org/)

---

**WorkflowForge.Extensions.Resilience.Polly** - *Advanced resilience for mission-critical workflows* 