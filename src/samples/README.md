# WorkflowForge Samples

<p align="center">
  <img src="../../icon.png" alt="WorkflowForge" width="120" height="120">
</p>

**Interactive examples demonstrating WorkflowForge capabilities**

## Available Sample Projects

### 1. Basic Console Samples (24 Examples)

**Location**: `WorkflowForge.Samples.BasicConsole/`

Interactive console application with 24 hands-on examples covering:

- **Basic Workflows (1-4)**: Hello World, Data Passing, Multiple Outcomes, Inline Operations
- **Control Flow (5-8)**: Conditionals, ForEach Loops, Error Handling, Built-in Operations
- **Configuration & Middleware (9-12)**: Options Pattern, Configuration Profiles, Events, Middleware
- **Extensions (13-18, 21-24)**: Serilog, Polly, OpenTelemetry, Health Checks, Performance, Persistence, Recovery, Validation, Audit
- **Advanced (19-20)**: Comprehensive Integration, Operation Creation Patterns

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
- All 10 extensions with zero version conflicts (Costura.Fody)
- Options pattern configuration from `appsettings.json`
- Production-grade patterns and best practices

## Learning Path

**Beginners**: Start with [Basic Console Samples 1-4](WorkflowForge.Samples.BasicConsole/)  
**Intermediate**: Explore samples 5-12 for control flow and configuration  
**Advanced**: Study samples 13-24 for extensions and production patterns  

## Documentation

- **[Samples Guide](../../docs/samples-guide.md)** - Detailed breakdown of all 24 samples
- **[Getting Started](../../docs/getting-started.md)** - Step-by-step tutorial
- **[Operations Guide](../../docs/operations.md)** - All operation types
- **[Configuration](../../docs/configuration.md)** - Configuration options
- **[Extensions](../../docs/extensions.md)** - Available extensions

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
# Enter sample number (1-24) from menu
```

## Contributing Samples

Want to contribute a sample? See [CONTRIBUTING.md](../../CONTRIBUTING.md) for guidelines.

Samples should:
- Demonstrate a specific feature or pattern
- Include clear console output
- Be self-contained and runnable
- Follow existing sample structure

---

**WorkflowForge Samples** - *Build workflows with industrial strength*
