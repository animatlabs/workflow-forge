# WorkflowForge.Extensions.Resilience.Polly

Advanced resilience extension for WorkflowForge using the battle-tested Polly library.

## Installation

```bash
dotnet add package WorkflowForge.Extensions.Resilience.Polly
```

## Quick Start

```csharp
using WorkflowForge.Extensions.Resilience.Polly;

// Use predefined configurations
var foundryConfig = FoundryConfiguration.ForProduction()
    .UsePollyProductionResilience();

var foundry = WorkflowForge.CreateFoundry("MyWorkflow", foundryConfig);

// Or configure manually
var foundryConfig = FoundryConfiguration.ForProduction()
    .UsePollyRetry(maxRetryAttempts: 5, baseDelay: TimeSpan.FromSeconds(1))
    .UsePollyCircuitBreaker(failureThreshold: 3, breakDuration: TimeSpan.FromMinutes(1))
    .UsePollyTimeout(TimeSpan.FromSeconds(30));
```

## Key Features

- **Advanced Retry Policies**: Exponential backoff, fixed interval, random interval
- **Circuit Breakers**: Fail-fast pattern with configurable thresholds
- **Timeouts**: Operation-level timeouts with cancellation
- **Rate Limiting**: Resource throttling with sliding windows
- **Policy Composition**: Combine multiple resilience strategies
- **Configuration Support**: Complete appsettings.json integration

## Environment Configurations

```csharp
// Development - lenient settings
foundryConfig.UsePollyDevelopmentResilience();

// Production - balanced settings
foundryConfig.UsePollyProductionResilience();

// Enterprise - comprehensive settings
foundryConfig.UsePollyEnterpriseResilience();
```

## Operation-Level Resilience

```csharp
// Wrap individual operations
var resilientOperation = myOperation
    .WithPollyRetry(maxRetries: 3)
    .WithPollyCircuitBreaker(failureThreshold: 5)
    .WithPollyTimeout(TimeSpan.FromSeconds(30));
```

## Configuration File Support

```json
{
  "WorkflowForge": {
    "Polly": {
      "Retry": {
        "MaxRetryAttempts": 3,
        "BaseDelay": "00:00:01"
      },
      "CircuitBreaker": {
        "FailureThreshold": 5,
        "DurationOfBreak": "00:00:30"
      }
    }
  }
}
```

## Examples & Documentation

- **[Complete Examples](../../samples/WorkflowForge.Samples.BasicConsole/README.md#14-polly-resilience)** - Interactive samples with Polly integration
- **[Base Resilience Extension](../WorkflowForge.Extensions.Resilience/README.md)** - Basic resilience patterns
- **[Core Documentation](../../core/WorkflowForge/README.md)** - Core concepts
- **[Main README](../../../README.md)** - Framework overview
- **[Polly Documentation](https://www.pollydocs.org/)** - Polly library docs

---

*Advanced resilience for mission-critical workflows* 