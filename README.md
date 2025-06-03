# WorkflowForge - A forge for workflows

A modern, robust, and extensible workflow orchestration framework for .NET with zero dependencies, built-in compensation, and sub-20 microsecond operation performance.

[![GitHub Repository](https://img.shields.io/badge/GitHub-animatlabs%2Fworkflow--forge-blue?logo=github)](https://github.com/animatlabs/workflow-forge)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-blue)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)

## 🎯 Why Choose WorkflowForge?

**Zero Dependencies, Maximum Performance**
- **Core package has ZERO external dependencies** - Just add one package
- **~15x faster concurrency scaling** - 16 concurrent workflows vs sequential execution
- **Sub-20 microsecond operations** - Custom operations execute in 4-56 μs
- **Built-in compensation** (automatic rollback/saga pattern)
- **Comprehensive observability** (metrics, tracing, health checks)

## Quick Start

### Installation
```bash
dotnet add package WorkflowForge
```

### Your First Workflow
```csharp
using WorkflowForge;

// Create and execute a workflow
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("ProcessOrder")
    .AddOperation("ValidateOrder", async (order, foundry, ct) => 
    {
        foundry.Logger.LogInformation("Validating order {OrderId}", order.Id);
        return await ValidateOrderAsync(order, ct);
    })
    .AddOperation("ProcessPayment", async (order, foundry, ct) => 
    {
        foundry.Logger.LogInformation("Processing payment for order {OrderId}", order.Id);
        return await ProcessPaymentAsync(order, ct);
    })
    .Build();

using var foundry = WorkflowForge.CreateFoundry("ProcessOrder");
using var smith = WorkflowForge.CreateSmith();

await smith.ForgeAsync(workflow, foundry);
```

## Key Features

- **Clean Architecture** - Modern .NET practices with intuitive industrial metaphor
- **Zero Dependencies** - Core library has no external dependencies  
- **Automatic Compensation** - Built-in rollback capabilities for failed workflows
- **Middleware Pipeline** - Extensible system for cross-cutting concerns
- **High Performance** - Sub-microsecond operations with minimal allocations
- **Rich Observability** - Comprehensive monitoring, metrics, and health checks
- **Test-First Design** - Mockable interfaces and comprehensive test coverage

## Extensions

Enhance WorkflowForge with optional extensions:

```bash
# Logging with Serilog
dotnet add package WorkflowForge.Extensions.Logging.Serilog

# Advanced resilience with Polly  
dotnet add package WorkflowForge.Extensions.Resilience.Polly

# Performance monitoring
dotnet add package WorkflowForge.Extensions.Observability.Performance

# Health checks
dotnet add package WorkflowForge.Extensions.Observability.HealthChecks

# Distributed tracing
dotnet add package WorkflowForge.Extensions.Observability.OpenTelemetry
```

### Advanced Configuration
```csharp
// Full-featured setup with extensions
var foundryConfig = FoundryConfiguration.Default()
    .UseSerilog()
    .UsePollyResilience()
    .EnablePerformanceMonitoring()
    .EnableHealthChecks()
    .EnableOpenTelemetry("OrderService", "1.0.0");

using var foundry = WorkflowForge.CreateFoundry("ProcessOrder", foundryConfig);
```

## Compensation (Rollback)

Automatic rollback when workflows fail:

```csharp
public class PaymentOperation : IWorkflowOperation
{
    public string Name => "ProcessPayment";
    public bool SupportsRestore => true;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var paymentResult = await ProcessPaymentAsync((Order)inputData!, cancellationToken);
        foundry.Properties["PaymentId"] = paymentResult.PaymentId;
        return paymentResult;
    }
    
    public async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        if (foundry.Properties.TryGetValue("PaymentId", out var paymentId))
        {
            await RefundPaymentAsync((string)paymentId!, cancellationToken);
        }
    }
}
```

## Performance Benchmarks

WorkflowForge delivers exceptional performance:

| Metric | Performance |
|--------|-------------|
| **Operation Execution** | 4-56 μs per operation |
| **Foundry Creation** | 5-15 μs setup time |
| **Concurrency Scaling** | ~15x improvement with 16 concurrent workflows |
| **Memory Allocation** | <1KB per operation, <2KB per foundry |

[View detailed benchmarks →](src/benchmarks/WorkflowForge.Benchmarks/README.md)

## Testing

Built for testability with mockable interfaces:

```csharp
[Fact]
public async Task Should_Execute_Workflow_Successfully()
{
    // Arrange
    var mockOperation = new Mock<IWorkflowOperation>();
    mockOperation.Setup(x => x.ForgeAsync(It.IsAny<object>(), It.IsAny<IWorkflowFoundry>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync("result");

    var workflow = WorkflowForge.CreateWorkflow()
        .AddOperation(mockOperation.Object)
        .Build();

    // Act & Assert
    var result = await smith.ForgeAsync(workflow, foundry);
    Assert.Equal("result", result);
}
```

## Documentation & Examples

- **[Getting Started Guide](docs/getting-started.md)** - Comprehensive tutorial
- **[Architecture Overview](docs/architecture.md)** - Core design principles
- **[Extension System](docs/extensions.md)** - Using and creating extensions
- **[Sample Applications](src/samples/)** - Interactive examples
- **[API Reference](docs/api-reference.md)** - Complete API documentation

## Building

```bash
git clone https://github.com/animatlabs/workflow-forge.git
cd WorkflowForge
dotnet restore
dotnet build
dotnet test
```

## Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**WorkflowForge** - *Forge robust workflows with confidence*