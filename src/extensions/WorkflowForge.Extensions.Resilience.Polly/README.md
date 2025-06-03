# WorkflowForge.Extensions.Resilience.Polly

Advanced resilience extension for WorkflowForge using the battle-tested Polly library. Provides circuit breakers, retry policies, timeout management, and rate limiting.

## Installation

```bash
dotnet add package WorkflowForge.Extensions.Resilience.Polly
```

## Quick Start

```csharp
using WorkflowForge.Extensions.Resilience.Polly;

// Enable Polly resilience
var foundryConfig = FoundryConfiguration.ForProduction()
    .UsePollyResilience();

var foundry = WorkflowForge.CreateFoundry("MyWorkflow", foundryConfig);
```

## Key Features

- **Circuit Breakers**: Prevent cascading failures with configurable thresholds
- **Retry Policies**: Exponential backoff, jitter, and custom retry strategies
- **Timeout Management**: Operation-level and workflow-level timeouts
- **Rate Limiting**: Control execution rates and resource usage
- **Battle-Tested**: Built on the proven Polly resilience library

## Configuration

```csharp
// Custom resilience policies
var pollyConfig = new PollyResilienceConfiguration
{
    RetryAttempts = 3,
    CircuitBreakerFailureThreshold = 5,
    CircuitBreakerSamplingDuration = TimeSpan.FromMinutes(1),
    TimeoutDuration = TimeSpan.FromSeconds(30)
};

foundryConfig.UsePollyResilience(pollyConfig);
```

## Documentation & Examples

- **[Interactive Samples](../../samples/WorkflowForge.Samples.BasicConsole/#14-polly-resilience)** - Sample #14: Polly integration
- **[Extensions Documentation](../../../docs/extensions.md)** - Complete extensions guide
- **[Getting Started](../../../docs/getting-started.md)** - Framework tutorial
- **[Main Documentation](../../../docs/)** - Comprehensive guides

---

*Advanced resilience patterns for robust workflows* 