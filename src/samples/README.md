# WorkflowForge Samples

<p align="center">
  <img src="https://raw.githubusercontent.com/animatlabs/workflow-forge/main/icon.png" alt="WorkflowForge" width="120" height="120">
</p>

**Interactive examples demonstrating WorkflowForge capabilities**

## Available Sample Projects

### 1. Basic Console Samples (33 Examples)

**Location**: `WorkflowForge.Samples.BasicConsole`

Interactive console application with 33 hands-on examples covering:

- **Basic Workflows (1-4)**: Hello World, Data Passing, Multiple Outcomes, Class-Based Operations
- **Control Flow (5-8)**: Conditionals, ForEach Loops, Error Handling, Built-in Operations
- **Configuration & Middleware (9-12)**: Options Pattern, Configuration Profiles, Events, Middleware
- **Extensions (13-18, 21-25)**: Serilog, Polly, OpenTelemetry, Health Checks, Performance, Persistence, Recovery, Validation, Audit, Configuration-Driven
- **Advanced (19-20)**: Comprehensive Integration, Operation Creation Patterns
- **Onboarding & Best Practices (26-33)**: DI, workflow middleware, cancellation/timeout, continue-on-error, compensation, foundry reuse, output chaining, service resolution

#### Quick Start

```bash
cd WorkflowForge.Samples.BasicConsole
dotnet run
```

The application provides an interactive menu to run individual samples or all samples sequentially.

#### Features Demonstrated

- Zero-dependency core workflow orchestration
- Dictionary-based data flow via `foundry.Properties`
- Saga pattern compensation/rollback
- Middleware pipeline (Russian Doll pattern)
- SRP-compliant event system (Workflow, Operation, Compensation)
- All 11 packages (10 extensions + Testing) with zero version conflicts
- Options pattern configuration from `appsettings.json`
- Production-grade patterns and best practices

## Learning Path

**Beginners**: Start with [Basic Console Samples 1-4](WorkflowForge.Samples.BasicConsole/README.md)  
**Intermediate**: Explore samples 5-12 for control flow and configuration  
**Advanced**: Study samples 13-33 for extensions and production patterns  

## Documentation

- **[Samples Guide](../../docs/getting-started/samples-guide.md)** - Detailed breakdown of all 33 samples
- **[Getting Started](../../docs/getting-started/getting-started.md)** - Step-by-step tutorial
- **[Operations Guide](../../docs/core/operations.md)** - All operation types
- **[Configuration](../../docs/core/configuration.md)** - Configuration options
- **[Extensions](../../docs/extensions/index.md)** - Available extensions

## Running Samples

### Prerequisites

- .NET 8.0 SDK or later
- Windows, Linux, or macOS

### Run All Samples

```bash
cd WorkflowForge.Samples.BasicConsole
dotnet run
# Select 'A' from menu to run all samples
```

### Run Specific Sample

```bash
dotnet run
# Enter sample number (1-33) from menu
```

## Contributing Samples

Want to contribute a sample? See [CONTRIBUTING.md](../../CONTRIBUTING.md) for guidelines.

Samples should:
- Demonstrate a specific feature or pattern
- Include clear console output
- Be self-contained and runnable
- Follow existing sample structure

---

