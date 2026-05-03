# WorkflowForge.Extensions.Resilience.Polly

Apply Polly retry, circuit breaker, timeout, and rate limiting to WorkflowForge operations. Polly is ILRepacked so your app sees fewer version conflicts.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Resilience.Polly.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Resilience.Polly/)

## Install

```bash
dotnet add package WorkflowForge.Extensions.Resilience.Polly
```

Targets .NET Standard 2.0 or later.

## Quick Start

```csharp
using WorkflowForge;
using WorkflowForge.Extensions.Resilience.Polly;

// Option 1: Foundry-level middleware (applies to all operations)
using var foundry = WorkflowForge.CreateFoundry("ResilientWorkflow");
foundry.UsePollyRetry(maxRetryAttempts: 3, baseDelay: TimeSpan.FromSeconds(1));

// Option 2: Wrap individual operations
var resilientOp = PollyRetryOperation.WithRetryPolicy(
    new ActionWorkflowOperation("CallApi", async (input, foundry, ct) => { /* call external API */ }),
    maxRetryAttempts: 3,
    baseDelay: TimeSpan.FromSeconds(1));

var workflow = WorkflowForge.CreateWorkflow("ResilientWorkflow")
    .AddOperation(resilientOp)
    .Build();

// Option 3: Retry + circuit breaker + timeout together
foundry.UsePollyComprehensive(
    maxRetryAttempts: 3,
    circuitBreakerThreshold: 5,
    timeoutDuration: TimeSpan.FromSeconds(30));
```

## Key points

- Foundry-wide middleware or per-operation wrappers use the same policy types.
- `PollyMiddlewareOptions` binds to `appsettings.json` and DI.
- Exponential backoff, jitter, circuit breaker sampling, timeouts, and rate limits are all optional slices you can enable independently.

## Configuration

### Via appsettings.json

```json
{
  "WorkflowForge": {
    "Extensions": {
      "Polly": {
        "Enabled": true,
        "EnableComprehensivePolicies": false,
        "EnableDetailedLogging": true,
        "DefaultTags": {
          "environment": "production",
          "application": "my-app"
        },
        "Retry": {
          "IsEnabled": true,
          "MaxRetryAttempts": 3,
          "BaseDelay": "00:00:01",
          "BackoffType": "Exponential",
          "UseJitter": true
        },
        "CircuitBreaker": {
          "IsEnabled": true,
          "FailureThreshold": 5,
          "BreakDuration": "00:00:30",
          "SamplingDuration": "00:00:30",
          "MinimumThroughput": 10
        },
        "Timeout": {
          "IsEnabled": true,
          "DefaultTimeout": "00:00:30",
          "UseOptimisticTimeout": true
        },
        "RateLimiter": {
          "IsEnabled": false,
          "PermitLimit": 100,
          "Window": "00:01:00",
          "QueueLimit": 0
        }
      }
    }
  }
}
```

### Via code

```csharp
using WorkflowForge.Extensions.Resilience.Polly.Options;

var options = new PollyMiddlewareOptions
{
    Enabled = true,
    Retry = { IsEnabled = true, MaxRetryAttempts = 3, BaseDelay = TimeSpan.FromSeconds(1) },
    CircuitBreaker = { IsEnabled = true, FailureThreshold = 5 },
    Timeout = { IsEnabled = true, DefaultTimeout = TimeSpan.FromSeconds(30) }
};

foundry.UsePollyFromSettings(options);
```

### Via dependency injection

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Extensions.Resilience.Polly;

services.AddWorkflowForgePolly(configuration, PollyMiddlewareOptions.DefaultSectionName);
var options = serviceProvider.GetRequiredService<PollyMiddlewareOptions>();
```

[Polly extension options](../../../docs/core/configuration.md#polly-extension)

## Usage examples

### Retry with exponential backoff

```csharp
// Foundry-level: applies retry to all operations
foundry.UsePollyRetry(maxRetryAttempts: 3, baseDelay: TimeSpan.FromSeconds(1));
```

### Combined policies (retry + circuit breaker + timeout)

```csharp
foundry.UsePollyComprehensive(
    maxRetryAttempts: 3,
    circuitBreakerThreshold: 5,
    timeoutDuration: TimeSpan.FromSeconds(30));
```

### Code-configured options

```csharp
var options = new PollyMiddlewareOptions
{
    Enabled = true,
    Retry = { IsEnabled = true, MaxRetryAttempts = 5, BaseDelay = TimeSpan.FromSeconds(2) },
    CircuitBreaker = { IsEnabled = true, FailureThreshold = 10 },
    Timeout = { IsEnabled = true, DefaultTimeout = TimeSpan.FromSeconds(60) }
};

foundry.UsePollyFromSettings(options);
```

## Links

- [Getting Started](../../../docs/getting-started/getting-started.md)
- [Configuration Guide](../../../docs/core/configuration.md#polly-extension)
- [Extensions Overview](../../../docs/extensions/index.md)
- [Sample 14: Polly Resilience](../../samples/WorkflowForge.Samples.BasicConsole/README.md)
