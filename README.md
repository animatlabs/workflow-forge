# WorkflowForge - A forge for workflows

**Zero-dependency workflow orchestration for .NET with sub-microsecond performance**

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge?logo=nuget)](https://www.nuget.org/packages/WorkflowForge)
[![GitHub Repository](https://img.shields.io/badge/GitHub-animatlabs%2Fworkflow--forge-blue?logo=github)](https://github.com/animatlabs/workflow-forge)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-blue)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)

## What Makes WorkflowForge Different?

**True Zero Dependencies** → Core package: **0 external dependencies**, **~50KB footprint**  
**Industrial Performance** → **4-56 μs operation execution**, **15x concurrency scaling**  
**Production Ready** → **Built-in compensation** (saga pattern), **comprehensive observability**  
**Developer First** → **Fluent API**, **industrial metaphor**, **extensive samples**

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

### 🎯 Start Here: Interactive Samples
**Best way to learn** → Run 18 progressive examples from basic to advanced:

```bash
cd src/samples/WorkflowForge.Samples.BasicConsole
dotnet run
```

**➜ [Browse All Examples](src/samples/WorkflowForge.Samples.BasicConsole/)**

### 📚 Documentation Hub
**Complete guides and reference** → **[docs/](docs/)** folder contains:
- **[Getting Started Guide](docs/getting-started.md)** - Step-by-step tutorial
- **[Architecture Overview](docs/architecture.md)** - Core design principles  
- **[API Reference](docs/api-reference.md)** - Complete API documentation
- **[Extensions Guide](docs/extensions.md)** - Available extensions

## Extensions Ecosystem

**Add capabilities without dependencies** → Optional extensions available:

| Extension | Purpose | Package |
|-----------|---------|---------|
| **Serilog Logging** | Structured logging | `WorkflowForge.Extensions.Logging.Serilog` |
| **Polly Resilience** | Circuit breakers, retries | `WorkflowForge.Extensions.Resilience.Polly` |
| **Performance Monitoring** | Metrics & profiling | `WorkflowForge.Extensions.Observability.Performance` |
| **Health Checks** | Application health | `WorkflowForge.Extensions.Observability.HealthChecks` |
| **OpenTelemetry** | Distributed tracing | `WorkflowForge.Extensions.Observability.OpenTelemetry` |

**➜ [Complete Extensions Documentation](docs/extensions.md)**

## Performance Characteristics

| Metric | Performance | Context |
|--------|-------------|---------|
| **Operation Execution** | 4-56 μs | Per operation overhead |
| **Foundry Creation** | 5-15 μs | Setup time |
| **Concurrency Scaling** | 15x improvement | 16 concurrent vs sequential |
| **Memory Footprint** | <2KB per foundry | Runtime allocation |

**➜ [Detailed Benchmarks & Analysis](src/benchmarks/WorkflowForge.Benchmarks/)**

## Industrial Metaphor

WorkflowForge uses an **industrial metaphor** for intuitive understanding:
- **🏭 The Forge** - Main factory creating workflows and components
- **⚒️ Foundries** - Execution environments where operations are performed
- **👨‍🔧 Smiths** - Orchestration engines managing workflow execution
- **⚙️ Operations** - Individual tasks within workflows

## Quick Links

| Resource | Description |
|----------|-------------|
| **[📖 Getting Started](docs/getting-started.md)** | Step-by-step tutorial |
| **[🎯 Interactive Samples](src/samples/WorkflowForge.Samples.BasicConsole/)** | 18 hands-on examples |
| **[📚 Complete Documentation](docs/)** | Comprehensive guides |
| **[🔧 Extensions](docs/extensions.md)** | Available extensions |
| **[⚡ Benchmarks](src/benchmarks/WorkflowForge.Benchmarks/)** | Performance analysis |

## Building & Contributing

```bash
git clone https://github.com/animatlabs/workflow-forge.git
cd workflow-forge
dotnet restore && dotnet build && dotnet test
```

**➜ [Contributing Guidelines](CONTRIBUTING.md)** | **➜ [License: MIT](LICENSE)**

---

**WorkflowForge** - *Build workflows with industrial strength* 🏭