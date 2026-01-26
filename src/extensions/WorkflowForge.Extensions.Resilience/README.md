# WorkflowForge.Extensions.Resilience

<p align="center">
  <img src="https://raw.githubusercontent.com/animatlabs/workflow-forge/main/icon.png" alt="WorkflowForge" width="120" height="120">
</p>

Base resilience patterns extension for WorkflowForge with fundamental retry logic and resilience strategies.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Resilience.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Resilience/)

## Zero Dependencies - Zero Conflicts

**This extension has ZERO external dependencies.** This means:

- NO DLL Hell - No third-party dependencies to conflict with
- NO Version Conflicts - Works with any versions of your application dependencies
- Clean Deployment - Pure WorkflowForge extension with no baggage

**Lightweight architecture**: Built entirely on WorkflowForge core with no external libraries.

## Installation

```bash
dotnet add package WorkflowForge.Extensions.Resilience
```

**Requires**: .NET Standard 2.0 or later

## Quick Start

```csharp
using WorkflowForge.Extensions.Resilience;
using WorkflowForge.Extensions.Resilience.Strategies;

// Wrap operations with retry logic
var resilientOperation = RetryWorkflowOperation.WithExponentialBackoff(
    operation: myOperation,
    baseDelay: TimeSpan.FromMilliseconds(100),
    maxDelay: TimeSpan.FromSeconds(30),
    maxAttempts: 3);

// Use in workflow
var workflow = WorkflowForge.CreateWorkflow("ProcessOrder")
    .AddOperation("ProcessPayment", resilientOperation)
    .Build();

await smith.ForgeAsync(workflow, foundry);
```

## Key Features

- **Retry Strategies**: Exponential backoff, fixed interval, random interval
- **Standardized Patterns**: Unified `IWorkflowResilienceStrategy` interface
- **Flexible Configuration**: Rich configuration through `RetryPolicySettings`
- **Operation Wrappers**: Easy-to-use resilient operation wrappers
- **Foundation for Advanced**: Base for Polly extension
- **Zero Dependencies**: Pure WorkflowForge implementation

## Retry Strategies

### 1. Exponential Backoff (Best for External Services)

```csharp
var strategy = new ExponentialBackoffStrategy(
    maxAttempts: 5,
    baseDelay: TimeSpan.FromSeconds(1),
    maxDelay: TimeSpan.FromSeconds(60),
    logger: logger);

var resilientOp = new RetryWorkflowOperation(myOperation, strategy);
```

**Use when**: Calling external APIs, databases, or services that may be temporarily unavailable.

### 2. Fixed Interval (Best for Databases)

```csharp
var retryOp = RetryWorkflowOperation.WithFixedInterval(
    operation: myOperation,
    interval: TimeSpan.FromSeconds(1),
    maxAttempts: 5);
```

**Use when**: Database queries, file operations, or scenarios where consistent retry timing is needed.

### 3. Random Interval (Prevents Thundering Herd)

```csharp
var retryOp = RetryWorkflowOperation.WithRandomInterval(
    operation: myOperation,
    minDelay: TimeSpan.FromMilliseconds(100),
    maxDelay: TimeSpan.FromSeconds(5),
    maxAttempts: 3);
```

**Use when**: Multiple concurrent workflows might retry simultaneously.

## Advanced Configuration

### Custom Retry Strategy

```csharp
public class CustomRetryStrategy : ResilienceStrategyBase
{
    public CustomRetryStrategy(IWorkflowForgeLogger logger) 
        : base("CustomRetry", logger) { }

    public override async Task<bool> ShouldRetryAsync(
        int attemptNumber, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        // Custom retry logic
        return attemptNumber < 5 && exception is TransientException;
    }

    public override TimeSpan GetRetryDelay(int attemptNumber, Exception exception)
    {
        // Custom delay calculation
        return TimeSpan.FromSeconds(Math.Pow(2, attemptNumber));
    }

    public override async Task ExecuteAsync(
        Func<Task> operation, 
        CancellationToken cancellationToken)
    {
        // Custom execution logic with retry
    }
}
```

### Retry Policy Settings

```csharp
var settings = new RetryPolicySettings
{
    MaxAttempts = 3,
    BaseDelay = TimeSpan.FromSeconds(1),
    MaxDelay = TimeSpan.FromSeconds(30),
    UseExponentialBackoff = true,
    UseJitter = true
};
```

## Configuration

**This extension uses programmatic configuration only.** There is no `appsettings.json` support. For file-based configuration, use **WorkflowForge.Extensions.Resilience.Polly**.

### Usage

```csharp
using WorkflowForge.Extensions.Resilience;
using WorkflowForge.Extensions.Resilience.Strategies;

// Wrap operations with retry logic
var resilientOperation = RetryWorkflowOperation.WithExponentialBackoff(
    operation: myOperation,
    baseDelay: TimeSpan.FromMilliseconds(100),
    maxDelay: TimeSpan.FromSeconds(30),
    maxAttempts: 3);

// Or use specific strategies
var strategy = new ExponentialBackoffStrategy(
    maxAttempts: 5,
    baseDelay: TimeSpan.FromSeconds(1),
    maxDelay: TimeSpan.FromSeconds(60),
    logger: logger);

var resilientOp = new RetryWorkflowOperation(myOperation, strategy);

// Add to workflow
var workflow = WorkflowForge.CreateWorkflow("ResilientProcess")
    .AddOperation("ProcessWithRetry", resilientOp)
    .Build();
```

**Strategies Available**:
- `ExponentialBackoffStrategy` - Best for external services
- `FixedIntervalStrategy` - Best for databases
- `RandomIntervalStrategy` - Prevents thundering herd

See [Configuration Guide](../../../docs/core/configuration.md#resilience-extension) for complete options.

## Interfaces

### IWorkflowResilienceStrategy

```csharp
public interface IWorkflowResilienceStrategy
{
    string Name { get; }
    Task<bool> ShouldRetryAsync(int attemptNumber, Exception exception, CancellationToken cancellationToken);
    TimeSpan GetRetryDelay(int attemptNumber, Exception exception);
    Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken);
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken);
}
```

## When to Use vs Polly Extension

**Use Resilience (this extension) when**:
- You want zero external dependencies
- Basic retry patterns are sufficient
- You need lightweight resilience

**Use Resilience.Polly extension when**:
- You need advanced patterns (circuit breaker, bulkhead, rate limiting)
- You want to leverage Polly's ecosystem
- You need complex policy combinations

Resilience.Polly internalizes Polly with ILRepack; this extension has no third-party dependencies.

## Documentation

- **[Getting Started](../../../docs/getting-started/getting-started.md)** - Basic tutorial
- **[Configuration Guide](../../../docs/core/configuration.md#resilience-extension)** - Full configuration options
- **[Extensions Overview](../../../docs/extensions/index.md)** - All extensions
- **[Samples](../../samples/WorkflowForge.Samples.BasicConsole/)** - Sample 14: Polly Resilience (demonstrates patterns)

## Sample Usage

See [Sample 14: PollyResilienceSample](../../samples/WorkflowForge.Samples.BasicConsole/Samples/PollyResilienceSample.cs) for complete examples of resilience patterns.

## License

MIT License - see [LICENSE](../../../LICENSE) for details.

---

