# WorkflowForge.Extensions.Resilience

Wrap operations with retry decorators and small strategy classes (exponential, fixed, jittered). No third-party policy library (Polly-free) — just WorkflowForge core plus small BCL polyfills (`System.Threading.Tasks.Extensions`, `System.ComponentModel.Annotations`).

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Resilience.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Resilience/)

## Install

```bash
dotnet add package WorkflowForge.Extensions.Resilience
```

Targets .NET Standard 2.0 or later.

## Quick Start

```csharp
using WorkflowForge;
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
    .AddOperation(resilientOperation)
    .Build();

await smith.ForgeAsync(workflow, foundry);
```

## Key points

- `IWorkflowResilienceStrategy` is the single extension point for delay and retry decisions.
- Ships exponential backoff, fixed interval, and random (jitter) helpers.
- `RetryPolicySettings` centralizes attempt counts and delay bounds.
- For circuit breaker, bulkheads, or Polly pipelines, see **WorkflowForge.Extensions.Resilience.Polly**.

## Retry strategies

### 1. Exponential backoff (external services)

```csharp
var strategy = new ExponentialBackoffStrategy(
    baseDelay: TimeSpan.FromSeconds(1),
    maxDelay: TimeSpan.FromSeconds(60),
    maxAttempts: 5,
    logger: logger);

var resilientOp = new RetryWorkflowOperation(myOperation, strategy);
```

**Use when**: External APIs, databases, or partners that drop connections briefly.

### 2. Fixed interval (predictable spacing)

```csharp
var retryOp = RetryWorkflowOperation.WithFixedInterval(
    operation: myOperation,
    interval: TimeSpan.FromSeconds(1),
    maxAttempts: 5);
```

**Use when**: You want the same delay between every attempt (files, simple DB retries).

### 3. Random interval (spread retries)

```csharp
var retryOp = RetryWorkflowOperation.WithRandomInterval(
    operation: myOperation,
    minDelay: TimeSpan.FromMilliseconds(100),
    maxDelay: TimeSpan.FromSeconds(5),
    maxAttempts: 3);
```

**Use when**: Many workflows might retry at once; jitter spreads the load.

## Advanced configuration

### Custom retry strategy

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

### Retry policy settings

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

**Programmatic only.** There is no `appsettings.json` support in this package. For file-based policy wiring, use **WorkflowForge.Extensions.Resilience.Polly**.

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
    baseDelay: TimeSpan.FromSeconds(1),
    maxDelay: TimeSpan.FromSeconds(60),
    maxAttempts: 5,
    logger: logger);

var resilientOp = new RetryWorkflowOperation(myOperation, strategy);

// Add to workflow
var workflow = WorkflowForge.CreateWorkflow("ResilientProcess")
    .AddOperation(resilientOp)
    .Build();
```

**Strategies Available**:
- `ExponentialBackoffStrategy` - Best for external services
- `FixedIntervalStrategy` - Best for databases
- `RandomIntervalStrategy` - Prevents thundering herd

[Resilience configuration](../../../docs/core/configuration.md#resilience-extension)

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

## When to use this package vs Polly

**This package** fits when you want to avoid a third-party policy library and simple retry timing is enough.

**WorkflowForge.Extensions.Resilience.Polly** fits when you need circuit breakers, bulkheads, rate limits, or stacked policies. Polly is ILRepacked there; this package pulls in no third-party policy library (only small BCL polyfills).

## Links

- [Getting Started](../../../docs/getting-started/getting-started.md)
- [Configuration Guide](../../../docs/core/configuration.md#resilience-extension)
- [Extensions Overview](../../../docs/extensions/index.md)
- [Samples](../../samples/WorkflowForge.Samples.BasicConsole/) (see Sample 14: Polly Resilience for related patterns)
- [Sample source (PollyResilienceSample)](../../samples/WorkflowForge.Samples.BasicConsole/Samples/PollyResilienceSample.cs)

## License

MIT License - see [LICENSE](../../../LICENSE) for details.
