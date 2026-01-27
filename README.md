# WorkflowForge

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.svg)](https://www.nuget.org/packages/WorkflowForge/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/WorkflowForge.svg)](https://www.nuget.org/packages/WorkflowForge/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.0-blueviolet.svg)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)

High-performance, dependency-free workflow orchestration library for .NET. Execute thousands of workflows per second with microsecond-level operation latency and minimal memory footprint.

**Version**: 2.0.0  
**License**: MIT  
**Compatibility**: .NET Standard 2.0

---

## Performance at a Glance

**Internal Benchmarks** (.NET 8.0.23, Windows 11, 50 iterations):
- **Operation Execution**: 12-45μs median latency
- **Workflow Throughput**: 39-178μs for typical operations
- **Memory Footprint**: 3.2-121KB across scenarios
- **Concurrent Scaling**: Near-perfect (16x speedup for 16 workflows)

**Competitive Benchmarks** (12 scenarios vs. Workflow Core, Elsa):
- **11-540x faster** execution (State Machine: up to 540x)
- **9-573x less** memory allocation
- **Microsecond-scale** execution vs. millisecond-scale competitors

[Full Performance Details](docs/performance/performance.md) | [Competitive Analysis](docs/performance/competitive-analysis.md)

---

## Key Features

### Core Engine

- **Dependency-Free Core**: Zero external dependencies for core library
- **Microsecond Execution**: Sub-100μs operation execution
- **Minimal Memory**: Linear memory scaling, no memory leaks
- **Thread-Safe**: Concurrent workflow execution via `ConcurrentDictionary`
- **Fluent API**: Clean, readable workflow definition with `AddOperations()` and `AddParallelOperations()`
- **Saga Pattern**: Built-in compensation/rollback via `RestoreAsync()`
- **Lifecycle Hooks**: `OnBeforeExecuteAsync`/`OnAfterExecuteAsync` for setup/teardown without middleware
- **Middleware Pipeline**: Russian Doll pattern for cross-cutting concerns
- **Event System**: SRP-compliant lifecycle events for workflows, operations, and compensation
- **Testing Support**: `FakeWorkflowFoundry` for unit testing operations in isolation

### Dependency Boundaries

Extensions declare explicit NuGet dependencies where needed. The validation extension uses **DataAnnotations** from the BCL, so no third‑party validation library is required.

### Package Ecosystem (13 Packages: 1 Core + 11 Extensions + Testing)

**Logging**:
- Serilog integration for structured logging

**Resilience**:
- Base resilience abstractions (dependency-free)
- Polly integration for retry, circuit breaker, timeout policies

**Observability**:
- Health checks (ASP.NET Core integration)
- OpenTelemetry (distributed tracing and metrics)
- Performance monitoring (dependency-free)

**Persistence**:
- State persistence abstractions (dependency-free)
- Recovery and resume capabilities (dependency-free)

**Validation**:
- DataAnnotations-based workflow validation

**Audit**:
- Comprehensive audit logging with pluggable providers (dependency-free)

[Extension Documentation](docs/extensions/index.md)

---

## Quick Start

### Installation

```bash
dotnet add package WorkflowForge
```

### Hello World

```csharp
using WorkflowForge;

// Build workflow
var workflow = WorkflowForge.CreateWorkflow("HelloWorld")
    .AddOperation("SayHello", async (foundry, ct) => {
        foundry.Logger.LogInformation("Hello, WorkflowForge!");
        await Task.CompletedTask;
    })
    .Build();

// Execute workflow
using var smith = WorkflowForge.CreateSmith();
await smith.ForgeAsync(workflow);
```

### Data Passing Between Operations

```csharp
var workflow = WorkflowForge.CreateWorkflow("ProcessOrder")
    .AddOperation("LoadOrder", async (foundry, ct) => {
        var order = await LoadOrderFromDb();
        foundry.SetProperty("Order", order);
    })
    .AddOperation("ValidateOrder", async (foundry, ct) => {
        var order = foundry.GetPropertyOrDefault<Order>("Order");
        if (order == null) throw new InvalidOperationException("Order not found");
        // Validation logic
    })
    .AddOperation("ProcessPayment", async (foundry, ct) => {
        var order = foundry.GetPropertyOrDefault<Order>("Order");
        var result = await ProcessPayment(order);
        foundry.SetProperty("PaymentResult", result);
    })
    .Build();

using var smith = WorkflowForge.CreateSmith();
await smith.ForgeAsync(workflow);
```

[Getting Started Guide](docs/getting-started/getting-started.md)

---

## Architecture

WorkflowForge follows **production-grade design patterns**:

- **Factory Pattern**: `WorkflowForge.CreateWorkflow()`, `CreateSmith()`, `CreateFoundry()`
- **Builder Pattern**: Fluent API for workflow construction
- **Saga Pattern**: Compensation via `RestoreAsync()`
- **Middleware Pattern**: Russian Doll pipeline for operations
- **Event-Driven**: Lifecycle events for monitoring and integration
- **Dependency Injection**: Full support for `IServiceProvider`

**Core Abstractions**:
- `IWorkflow`: Workflow definition
- `IWorkflowOperation`: Executable operation
- `IWorkflowFoundry`: Execution context (properties, logging, services)
- `IWorkflowSmith`: Orchestration engine
- `IWorkflowOperationMiddleware`: Middleware abstraction

[Architecture Documentation](docs/architecture/overview.md)

---

## Use Cases

WorkflowForge excels at:

1. **High-Throughput Processing**: Thousands of workflows per second
2. **Real-Time Orchestration**: Sub-millisecond execution requirements
3. **Microservices**: Lightweight, stateless orchestration
4. **API Orchestration**: Coordinate multiple service calls efficiently
5. **Business Rules Execution**: Fast, testable business logic
6. **ETL Pipelines**: High-performance data transformation
7. **Event Processing**: Low-latency event handling
8. **Request/Response Workflows**: API request processing

[Competitive Comparison](docs/performance/competitive-analysis.md)

---

## Examples

**33 comprehensive samples** covering all features:

- Basic workflows (Hello World, data passing, inline operations)
- Control flow (conditionals, loops, error handling)
- Configuration (options pattern, environment profiles)
- Middleware and events
- All 13 packages (Serilog, Polly, OpenTelemetry, Validation, Audit, Testing, etc.)
- Advanced patterns (comprehensive integration, operation creation patterns)

[Sample Applications](src/samples/WorkflowForge.Samples.BasicConsole/README.md) | [Samples Guide](docs/getting-started/samples-guide.md)

---

## Documentation

- [Getting Started](docs/getting-started/getting-started.md) - Installation and first workflow
- [Architecture](docs/architecture/overview.md) - Design patterns and core concepts
- [Operations](docs/core/operations.md) - Built-in and custom operations
- [Events](docs/core/events.md) - Lifecycle event system
- [Extensions](docs/extensions/index.md) - All 13 packages (11 extensions + Testing) with examples
- [Configuration](docs/core/configuration.md) - Environment-specific setup
- [API Reference](docs/reference/api-reference.md) - Complete API documentation
- [Performance](docs/performance/performance.md) - Benchmark results and optimization
- [Competitive Analysis](docs/performance/competitive-analysis.md) - vs. Workflow Core and Elsa
- [Samples Guide](docs/getting-started/samples-guide.md) - Learning path through 33 samples

[Full Documentation](docs/index.md)

---

## Benchmarks

### Internal Performance

**Operation Performance**:
- Custom: 26.1μs median
- Delegate: 37.8μs median
- Logging: 9.8μs median

**Workflow Throughput** (10 operations):
- Sequential custom: 96.9μs median
- High-performance config: 635.2μs mean

**Concurrency** (8 workflows, 5 ops each):
- Sequential: 631.75ms
- Concurrent: 78.88ms (8x speedup)

**Memory Allocation** (100 iterations):
- Minimal workflow: 2.65KB
- No Gen2 collections

[Internal Benchmarks](docs/performance/performance.md#internal-performance-benchmarks)

### Competitive Performance

**Sequential Workflow** (10 operations):
- WorkflowForge: 247μs median
- Workflow Core: 6,531μs median (26x slower)
- Elsa: 17,617μs median (71x slower)

**State Machine** (25 transitions):
- WorkflowForge: 68μs median
- Workflow Core: 20,624μs median (303x slower)
- Elsa: 36,695μs median (540x slower)

**Creation Overhead**:
- WorkflowForge: 13μs median
- Workflow Core: 814μs median (63x slower)
- Elsa: 2,107μs median (162x slower)

[Competitive Benchmarks](docs/performance/competitive-analysis.md)

---

## Contributing

We welcome contributions! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

**Key Areas**:
- Performance optimizations
- New built-in operations
- Extension development
- Documentation improvements
- Bug fixes and testing

---

## Support

- **GitHub Issues**: Bug reports and feature requests
- **Discussions**: Questions and community support
- **Documentation**: Comprehensive guides and API reference

---

## License

MIT License - see [LICENSE](LICENSE) for details.

---

## Acknowledgments

Built with passion for performance and developer experience. Special thanks to the .NET community for inspiration and feedback.

---
