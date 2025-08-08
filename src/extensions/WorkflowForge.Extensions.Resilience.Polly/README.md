# WorkflowForge.Extensions.Resilience.Polly

Advanced resilience extension for WorkflowForge using the battle-tested Polly library. Provides circuit breakers, retry policies, timeout management, and rate limiting.

## Installation

```bash
dotnet add package WorkflowForge.Extensions.Resilience.Polly
```

## Quick Start

```csharp
using WorkflowForge.Extensions.Resilience.Polly;

// Create foundry and apply Polly resilience (extension methods are on WorkflowFoundry)
var config = FoundryConfiguration.ForProduction();
var foundry = (WorkflowFoundry)WorkflowForge.CreateFoundry("MyWorkflow", config);

// Preset:
foundry.UsePollyProductionResilience();

// Or custom policies:
foundry
    .UsePollyRetry(maxRetryAttempts: 5, baseDelay: TimeSpan.FromSeconds(1))
    .UsePollyCircuitBreaker(failureThreshold: 3, durationOfBreak: TimeSpan.FromMinutes(1))
    .UsePollyTimeout(TimeSpan.FromSeconds(30));
```

## Key Features

- **Circuit Breakers**: Prevent cascading failures with configurable thresholds
- **Retry Policies**: Exponential backoff, jitter, and custom retry strategies
- **Timeout Management**: Operation-level and workflow-level timeouts
- **Rate Limiting**: Control execution rates and resource usage
- **Battle-Tested**: Built on the proven Polly resilience library

## Configuration

```csharp
// Configuration from strongly-typed settings
var settings = PollySettings.ForProduction();
foundry.UsePollyFromSettings(settings);
```

## Documentation & Examples

- **[Interactive Samples](../../samples/WorkflowForge.Samples.BasicConsole/#14-polly-resilience)** - Sample #14: Polly integration
- **[Extensions Documentation](../../../docs/extensions.md)** - Complete extensions guide
- **[Getting Started](../../../docs/getting-started.md)** - Framework tutorial
- **[Main Documentation](../../../docs/)** - Comprehensive guides

---

*Advanced resilience patterns for robust workflows* 