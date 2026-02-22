# WorkflowForge.Extensions.Resilience.Polly

![WorkflowForge](https://raw.githubusercontent.com/animatlabs/workflow-forge/main/icon.png)

Advanced resilience extension for WorkflowForge with Polly integration for retry, circuit breaker, timeout, and rate limiting policies.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Resilience.Polly.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Resilience.Polly/)

## Dependency Isolation

**This extension internalizes Polly with ILRepack.** This means:

- Reduced dependency conflicts for Polly
- Public APIs stay WorkflowForge/BCL only
- Microsoft/System assemblies remain external

## Installation

```bash
dotnet add package WorkflowForge.Extensions.Resilience.Polly
```

**Requires**: .NET Standard 2.0 or later

## Quick Start

```csharp
using WorkflowForge;
using WorkflowForge.Extensions.Resilience.Polly;

// Option 1: Foundry-level middleware (applies to all operations)
using var foundry = WorkflowForge.CreateFoundry("ResilientWorkflow");
foundry.UsePollyRetry(maxRetryAttempts: 3, baseDelay: TimeSpan.FromSeconds(1));

// Option 2: Wrap individual operations
var resilientOp = PollyRetryOperation.WithRetryPolicy(
    new CallExternalApiOperation(),
    maxRetryAttempts: 3,
    baseDelay: TimeSpan.FromSeconds(1));

var workflow = WorkflowForge.CreateWorkflow("ResilientWorkflow")
    .AddOperation(resilientOp)
    .Build();

// Option 3: Comprehensive policies (retry + circuit breaker + timeout)
foundry.UsePollyComprehensive(
    maxRetryAttempts: 3,
    circuitBreakerThreshold: 5,
    timeoutDuration: TimeSpan.FromSeconds(30));
```

## Key Features

- **Retry Policies**: Exponential backoff, fixed intervals, jitter
- **Circuit Breaker**: Prevent cascading failures
- **Timeout**: Operation-level timeouts
- **Rate Limiting**: Control operation throughput
- **Policy Composition**: Combine multiple policies
- **Full Polly Integration**: Access entire Polly ecosystem

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

### Via Code

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

### Via Dependency Injection

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Extensions.Resilience.Polly;

services.AddWorkflowForgePolly(configuration, PollyMiddlewareOptions.DefaultSectionName);
var options = serviceProvider.GetRequiredService<PollyMiddlewareOptions>();
```

### Preset Configurations

```csharp
// Development settings (minimal retry)
var devOptions = PollyMiddlewareOptions.ForDevelopment();

// Production settings (retry + circuit breaker)
var prodOptions = PollyMiddlewareOptions.ForProduction();

// Enterprise settings (all policies enabled)
var enterpriseOptions = PollyMiddlewareOptions.ForEnterprise();

// Minimal settings (basic retry only)
var minimalOptions = PollyMiddlewareOptions.Minimal();
```

See [Configuration Guide](../../../docs/core/configuration.md#polly-extension) for complete options.

## Policy Examples

### Retry with Exponential Backoff

```csharp
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(3, 
        attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
```

### Circuit Breaker

```csharp
var circuitBreakerPolicy = Policy
    .Handle<HttpRequestException>()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
```

### Timeout

```csharp
var timeoutPolicy = Policy
    .TimeoutAsync(TimeSpan.FromSeconds(30));
```

### Combined Policies

```csharp
var combinedPolicy = Policy.WrapAsync(
    retryPolicy,
    circuitBreakerPolicy,
    timeoutPolicy);
```

## Documentation

- **[Getting Started](../../../docs/getting-started/getting-started.md)**
- **[Configuration Guide](../../../docs/core/configuration.md#polly-extension)**
- **[Extensions Overview](../../../docs/extensions/index.md)**
- **[Sample 14: Polly Resilience](../../samples/WorkflowForge.Samples.BasicConsole/README.md)**

---

