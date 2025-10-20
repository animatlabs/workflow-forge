# WorkflowForge.Extensions.Resilience

Base resilience patterns extension for WorkflowForge with fundamental retry logic and resilience strategies.

## Installation

```bash
dotnet add package WorkflowForge.Extensions.Resilience
```

## Zero Version Conflicts

**This extension uses Costura.Fody to embed System.Threading.Tasks.Extensions.** This means:

- **NO DLL Hell** - No conflicts with your application's System libraries  
- **NO Conflicts** - Works seamlessly with any .NET version  
- **Clean Deployment** - Professional-grade dependency isolation

**How it works**: Dependencies are embedded as compressed resources at build time and loaded automatically at runtime, completely isolated from your application.

## Quick Start

```csharp
using WorkflowForge.Extensions.Resilience;

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
```

## Key Features

- **Retry Strategies**: Exponential backoff, fixed interval, random interval
- **Standardized Patterns**: Unified `IWorkflowResilienceStrategy` interface
- **Flexible Configuration**: Rich configuration through `RetryPolicySettings`
- **Operation Wrappers**: Easy-to-use resilient operation wrappers
- **Foundation for Advanced**: Base for Polly extension

## Retry Strategies

```csharp
// Exponential backoff - best for external services
var retryOp = RetryWorkflowOperation.WithExponentialBackoff(
    operation: myOperation,
    baseDelay: TimeSpan.FromMilliseconds(100),
    maxAttempts: 3);

// Fixed interval - best for databases  
var retryOp = RetryWorkflowOperation.WithFixedInterval(
    operation: myOperation,
    interval: TimeSpan.FromSeconds(1),
    maxAttempts: 5);

// Random interval - prevents thundering herd
var retryOp = RetryWorkflowOperation.WithRandomInterval(
    operation: myOperation,
    minDelay: TimeSpan.FromMilliseconds(500),
    maxDelay: TimeSpan.FromSeconds(10),
    maxAttempts: 4);
```

## Configuration Support

```csharp
// From configuration
var retrySettings = new RetryPolicySettings
{
    StrategyType = RetryStrategyType.ExponentialBackoff,
    MaxAttempts = 3,
    BaseDelay = TimeSpan.FromMilliseconds(100)
};

var retryOperation = RetryWorkflowOperation.FromSettings(myOperation, retrySettings);
```

## Examples & Documentation

- **[Complete Examples](../../samples/WorkflowForge.Samples.BasicConsole/README.md#7-error-handling)** - Interactive resilience samples
- **[Advanced Polly Extension](../WorkflowForge.Extensions.Resilience.Polly/README.md)** - Advanced resilience patterns
- **[Core Documentation](../../core/WorkflowForge/README.md)** - Core concepts
- **[Main README](../../../README.md)** - Framework overview

---

*Foundation resilience patterns for reliable workflows* 