# WorkflowForge - Build workflows with industrial strength

High-performance, dependency-free workflow orchestration library for .NET. Execute thousands of workflows per second with microsecond-level operation latency and minimal memory footprint.

**Version**: 2.0.0  
**License**: MIT  
**Compatibility**: .NET Standard 2.0

---

## Performance at a Glance

**Internal Benchmarks** (.NET 8.0, Windows 11):
- **Operation Execution**: 9.8-37.8μs median latency
- **Workflow Throughput**: 50-200μs for 1-50 operations
- **Memory Footprint**: 2.65KB baseline, linear scaling
- **Concurrent Scaling**: Near-perfect (16x speedup for 16 workflows)

**Competitive Benchmarks** (vs. Workflow Core 3.17, Elsa 3.5.1):
- **13-378x faster** execution across 8 real-world scenarios
- **6-1,495x less** memory allocation
- **Microsecond-scale** execution vs. millisecond-scale competitors

[Full Performance Details](docs/performance.md) | [Competitive Analysis](docs/competitive-analysis.md)

---

## Key Features

### Core Engine

- **Dependency-Free Core**: Zero external dependencies for core library
- **Microsecond Execution**: Sub-100μs operation execution
- **Minimal Memory**: Linear memory scaling, no memory leaks
- **Thread-Safe**: Concurrent workflow execution via `ConcurrentDictionary`
- **Fluent API**: Clean, readable workflow definition
- **Saga Pattern**: Built-in compensation/rollback via `RestoreAsync()`
- **Middleware Pipeline**: Russian Doll pattern for cross-cutting concerns
- **Event System**: SRP-compliant lifecycle events for workflows, operations, and compensation

### Zero Version Conflicts

**All extensions use Costura.Fody** to embed third-party dependencies. This means:

- **NO DLL Hell**: Use ANY version of Serilog, Polly, FluentValidation, etc. in your app
- **NO Conflicts**: Extension versions won't clash with your application's dependency versions
- **Clean Deployment**: Professional-grade dependency isolation

**Example**: Your app uses FluentValidation 12.x, the Validation extension uses 11.9.0 - both coexist perfectly.

### Extension Ecosystem (10 Extensions)

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
- FluentValidation bridge for workflow validation

**Audit**:
- Comprehensive audit logging with pluggable providers (dependency-free)

[Extension Documentation](docs/extensions.md)

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

[Getting Started Guide](docs/getting-started.md)

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

[Architecture Documentation](docs/architecture.md)

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

[Competitive Comparison](docs/competitive-analysis.md)

---

## Examples

**24 comprehensive samples** covering all features:

- Basic workflows (Hello World, data passing, inline operations)
- Control flow (conditionals, loops, error handling)
- Configuration (options pattern, environment profiles)
- Middleware and events
- All 10 extensions (Serilog, Polly, OpenTelemetry, Validation, Audit, etc.)
- Advanced patterns (comprehensive integration, operation creation patterns)

[Sample Applications](src/samples/WorkflowForge.Samples.BasicConsole/README.md) | [Samples Guide](docs/samples-guide.md)

---

## Documentation

- [Getting Started](docs/getting-started.md) - Installation and first workflow
- [Architecture](docs/architecture.md) - Design patterns and core concepts
- [Operations](docs/operations.md) - Built-in and custom operations
- [Events](docs/events.md) - Lifecycle event system
- [Extensions](docs/extensions.md) - All 10 extensions with examples
- [Configuration](docs/configuration.md) - Environment-specific setup
- [API Reference](docs/api-reference.md) - Complete API documentation
- [Performance](docs/performance.md) - Benchmark results and optimization
- [Competitive Analysis](docs/competitive-analysis.md) - vs. Workflow Core and Elsa
- [Samples Guide](docs/samples-guide.md) - Learning path through 24 samples

[Full Documentation](docs/README.md)

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

[Internal Benchmarks](docs/performance.md#internal-benchmarks)

### Competitive Performance

**Sequential Workflow** (10 operations):
- WorkflowForge: 231μs median
- Workflow Core: 8,594μs median (37x slower)
- Elsa: 20,898μs median (90x slower)

**Concurrent Execution** (8 workflows):
- WorkflowForge: 305μs median
- Workflow Core: 45,532μs median (149x slower)
- Elsa: 104,863μs median (344x slower)

**Creation Overhead**:
- WorkflowForge: 6.7μs median
- Workflow Core: 871μs median (130x slower)
- Elsa: 2,568μs median (383x slower)

[Competitive Benchmarks](docs/competitive-analysis.md)

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
