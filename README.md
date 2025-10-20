# WorkflowForge - A forge for workflows

**Zero-dependency workflow orchestration for .NET with microsecond-level performance**

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge?logo=nuget)](https://www.nuget.org/packages/WorkflowForge)
[![GitHub Repository](https://img.shields.io/badge/GitHub-animatlabs%2Fworkflow--forge-blue?logo=github)](https://github.com/animatlabs/workflow-forge)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-blue)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)

## What Makes WorkflowForge Different?

**True Zero Dependencies**: Core package with no external dependencies, small footprint  
**Measured Performance**: Microsecond-level per-operation execution (medians ~14–36 μs; typical range ~14–80 μs). See benchmarks.  
**Production Ready**: Built-in compensation (saga pattern), observability via extensions  
**Developer First**: Fluent API, clear metaphor, extensive samples

## Quick Start

```bash
dotnet add package WorkflowForge
```

```csharp
using WorkflowForge;

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

## Learn & Explore

### Start Here: Interactive Samples
**Best way to learn** → Run 24 progressive examples from basic to advanced:

```bash
cd src/samples/WorkflowForge.Samples.BasicConsole
dotnet run
```

Browse all examples: [src/samples/WorkflowForge.Samples.BasicConsole/](src/samples/WorkflowForge.Samples.BasicConsole/)

### Documentation Hub
**Complete guides and reference** → **[docs/](docs/)** folder contains:
- **[Getting Started Guide](docs/getting-started.md)** - Step-by-step tutorial
- **[Samples Guide](docs/samples-guide.md)** - All 24 samples explained with best practices
- **[Architecture Overview](docs/architecture.md)** - Core design principles & SRP event model
- **[Event System Guide](docs/events.md)** - Comprehensive event handling & monitoring
- **[API Reference](docs/api-reference.md)** - Complete API documentation
- **[Extensions Guide](docs/extensions.md)** - Available extensions

## Extensions Ecosystem

**ZERO VERSION CONFLICTS** → Extensions use Costura.Fody to embed dependencies as compressed resources. Your users can have **ANY version** of Serilog, Polly, FluentValidation, or OpenTelemetry - **NO conflicts possible!**

**Add capabilities without dependencies** → Optional extensions:

| Extension | Purpose | Package |
|-----------|---------|---------|
| **Serilog Logging** | Structured logging | `WorkflowForge.Extensions.Logging.Serilog` |
| **Resilience** | Core retry abstractions | `WorkflowForge.Extensions.Resilience` |
| **Polly Resilience** | Circuit breakers, retries via Polly | `WorkflowForge.Extensions.Resilience.Polly` |
| **Validation** | Input validation, FluentValidation bridge | `WorkflowForge.Extensions.Validation` |
| **Audit Logging** | Compliance & audit trails | `WorkflowForge.Extensions.Audit` |
| **Persistence** | Workflow state storage | `WorkflowForge.Extensions.Persistence` |
| **Persistence Recovery** | Resume interrupted workflows | `WorkflowForge.Extensions.Persistence.Recovery` |
| **Performance Monitoring** | Metrics & profiling | `WorkflowForge.Extensions.Observability.Performance` |
| **Health Checks** | Application health | `WorkflowForge.Extensions.Observability.HealthChecks` |
| **OpenTelemetry** | Distributed tracing | `WorkflowForge.Extensions.Observability.OpenTelemetry` |

Complete Extensions Documentation: [docs/extensions.md](docs/extensions.md)

## Performance Characteristics

| Metric | Performance | Context |
|--------|-------------|---------|
| **Operation Execution** | Microsecond-level (median ~14–36 μs) | Per-operation timing (see OperationPerformance benchmarks) |
| **Foundry Creation** | ~5–7 μs median (means ~13–16 μs) | Setup time (ConfigurationProfiles benchmarks) |
| **Parallel Throughput** | Improves with concurrent execution | See throughput benchmarks |
| **Memory Footprint** | ≈2.2 KB per foundry; ≈0.9–2.3 KB per operation | Allocations from benchmarks |

Detailed Benchmarks & Analysis: [src/benchmarks/WorkflowForge.Benchmarks/](src/benchmarks/WorkflowForge.Benchmarks/)

## Industrial Metaphor

WorkflowForge uses an **industrial metaphor** for intuitive understanding:
- **The Forge** - Main factory creating workflows and components
- **Foundries** - Execution environments where operations are performed
- **Smiths** - Orchestration engines managing workflow execution
- **Operations** - Individual tasks within workflows

## Quick Links

| Resource | Description |
|----------|-------------|
| **[Getting Started](docs/getting-started.md)** | Step-by-step tutorial |
| **[Interactive Samples](src/samples/WorkflowForge.Samples.BasicConsole/)** | 24 hands-on examples |
| **[Complete Documentation](docs/)** | Comprehensive guides |
| **[Extensions](docs/extensions.md)** | Available extensions |
| **[Benchmarks](src/benchmarks/WorkflowForge.Benchmarks/)** | Performance analysis |

## Building & Contributing

```bash
git clone https://github.com/animatlabs/workflow-forge.git
cd workflow-forge
dotnet restore && dotnet build && dotnet test
```

[Contributing Guidelines](CONTRIBUTING.md) | [License: MIT](LICENSE)

---

**WorkflowForge** - *Build workflows with industrial strength*