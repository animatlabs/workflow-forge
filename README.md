# WorkflowForge

[![Build and Test](https://github.com/animatlabs/workflow-forge/actions/workflows/build-test.yml/badge.svg?branch=main)](https://github.com/animatlabs/workflow-forge/actions/workflows/build-test.yml)
[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.svg)](https://www.nuget.org/packages/WorkflowForge/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/WorkflowForge.svg)](https://www.nuget.org/packages/WorkflowForge/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.0-blueviolet.svg)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=coverage)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge)
[![Ko-fi](https://img.shields.io/badge/Ko--fi-support-ff5e5b?logo=ko-fi)](https://ko-fi.com/animat089)

Workflow orchestration for .NET. The core package has no dependencies. Benchmarks show high workflow throughput, microsecond-scale operation latency, and modest steady-state allocation.

**Version**: 2.1.2  
**License**: MIT  
**Compatibility**: .NET Standard 2.0

Against Workflow Core and Elsa, published runs hit **13-511×** faster execution and **6-575×** lower allocation in covered scenarios. [Internal metrics](docs/performance/performance.md) and [competitive methodology](docs/performance/competitive-analysis.md) live in the docs.

---

## Key Features

### Core engine

- **Zero external dependencies** in the core library
- Typical operations finish in **sub-100μs**
- Memory grows roughly linearly with workflow size
- Shared-state workflows use **`ConcurrentDictionary`**
- **Fluent API**: `AddOperations()`, `AddParallelOperations()`
- **Sagas**: override `RestoreAsync`; the default skips operations that do not restore
- **`OnBeforeExecuteAsync` / `OnAfterExecuteAsync`** for setup and teardown (no separate middleware requirement for that)
- **Russian Doll** operation middleware pipeline
- **Lifecycle events** for workflows, operations, and compensation (SRP-friendly splits)
- **`FakeWorkflowFoundry`** for unit tests focused on single operations

### Dependency boundaries

Extensions pull in NuGet packages only where needed. Validation uses **DataAnnotations** from the BCL.

### Packages (1 core + 11 extensions + Testing)

**Logging**: Serilog (structured)

**Resilience**: base abstractions in-extension; Polly for retry, circuit breaker, timeout

**Observability**: ASP.NET Core health checks; OpenTelemetry; performance monitoring extension

**Persistence**: abstractions; recovery and resume

**Validation**: DataAnnotations on workflows

**Audit**: logging with pluggable providers

[Extensions](docs/extensions/index.md)

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

### Data passing between operations

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

[Getting started](docs/getting-started/getting-started.md)

---

## Architecture

**Patterns**: Factory (`CreateWorkflow`, `CreateSmith`, `CreateFoundry`), fluent **Builder**, **Saga** via `RestoreAsync`, **Middleware** (Russian Doll), **event-driven** lifecycle, **DI** via `IServiceProvider` on foundry/builder.

**Core types**: `IWorkflow`, `IWorkflowOperation`, `IWorkflowFoundry`, `IWorkflowSmith`, `IWorkflowOperationMiddleware`

Details: [Architecture](docs/architecture/overview.md)

---

## Use cases

**Good fits**: throughput-heavy pipelines, latency-sensitive orchestration, light coordination between services.

| Scenario | Notes |
|----------|--------|
| High throughput | Thousands of workflows per second in benchmark setups |
| Real-time orchestration | Sub-millisecond targets where operation count stays bounded |
| Microservices | Stateless coordination without a heavy workflow host |
| API orchestration | Multiple outbound calls with shared context |
| Business rules | Fast, testable logic with clear operation boundaries |
| ETL-style flows | Streaming or batched transforms when allocation stays controlled |
| Event processing | Low-latency handlers that map cleanly to operations |
| Request/response APIs | Per-request workflows with middleware and hooks |

[Competitive comparison](docs/performance/competitive-analysis.md)

---

## Examples

**33 samples** across basics, control flow, configuration, middleware, events, all packages, and integration-style demos.

[Basic console sample](src/samples/WorkflowForge.Samples.BasicConsole/README.md) · [Samples guide](docs/getting-started/samples-guide.md)

---

## Documentation

| Topic | Link |
|--------|------|
| Install and first workflow | [Getting started](docs/getting-started/getting-started.md) |
| Design and components | [Architecture](docs/architecture/overview.md) |
| Operations | [Operations](docs/core/operations.md) |
| Lifecycle events | [Events](docs/core/events.md) |
| Extensions (11 + Testing) | [Extensions](docs/extensions/index.md) |
| Configuration | [Configuration](docs/core/configuration.md) |
| API reference | [API Reference](docs/reference/api-reference.md) |
| Benchmarks and tuning | [Performance](docs/performance/performance.md) |
| vs. Workflow Core and Elsa | [Competitive analysis](docs/performance/competitive-analysis.md) |
| Sample index | [Samples guide](docs/getting-started/samples-guide.md) |

[Index](docs/index.md)

---

## Benchmarks

**State machine** (25 transitions):

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 65μs | 29,537μs (455x) | 33,062μs (511x) |
| .NET 8.0 | 71μs | 21,683μs (305x) | 34,426μs (485x) |
| .NET FX 4.8 | 61μs | 18,486μs (303x) | N/A |

**Sequential workflow** (10 operations):

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 422μs | 13,828μs (33x) | 18,676μs (44x) |
| .NET 8.0 | 377μs | 9,879μs (26x) | 19,168μs (51x) |
| .NET FX 4.8 | 122μs | 6,743μs (55x) | N/A |

On **.NET 10.0**, state machine vs. Elsa reaches **511×**. For internal latency, throughput, and allocation, see [Performance](docs/performance/performance.md). The full competitive set is in [Competitive analysis](docs/performance/competitive-analysis.md).

---

## Contributing

PRs welcome. See [CONTRIBUTING.md](CONTRIBUTING.md). Performance work, built-in operations, extensions, docs, tests, and fixes all help.

---

## Support

[Issues](https://github.com/animatlabs/workflow-forge/issues) for bugs and features. [Discussions](https://github.com/animatlabs/workflow-forge/discussions) for questions. If the project saves you time, [Ko-fi](https://ko-fi.com/animat089) is appreciated.

---

## License

MIT. See [LICENSE](LICENSE).

---

## Acknowledgments

Thanks to everyone who filed issues, tried the samples, and suggested improvements.

---
