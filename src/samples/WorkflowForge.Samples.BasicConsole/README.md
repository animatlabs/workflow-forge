# WorkflowForge Basic Console Samples

Interactive examples demonstrating the core features and capabilities of WorkflowForge.

## Getting Started

```bash
cd src/samples/WorkflowForge.Samples.BasicConsole
dotnet run
```

## Available Samples

### Basic Workflows (1-4)
- **Hello World** - Simple workflow demonstration
- **Data Passing** - Pass data between operations  
- **Multiple Outcomes** - Workflows with different results
- **Inline Operations** - Quick operations with lambdas

### Control Flow (5-8)
- **Conditional Workflows** - If/else logic in workflows
- **ForEach Loops** - Process collections
- **Error Handling** - Handle exceptions gracefully
- **Built-in Operations** - Use logging, delays, etc.

### Configuration & Middleware (9-12)
- **Options Pattern** - Configuration management
- **Configuration Profiles** - Environment-specific settings
- **Workflow Events** - Listen to workflow lifecycle
- **Middleware Usage** - Add cross-cutting concerns

### Extensions (13-17)
- **Serilog Logging** - Structured logging
- **Polly Resilience** - Retry policies and circuit breakers
- **OpenTelemetry** - Distributed tracing
- **Health Checks** - System monitoring
- **Performance Monitoring** - Metrics and statistics

### Advanced (18)
- **Comprehensive Demo** - Full-featured example with all extensions

## Interactive Menu

The console application provides an easy-to-use menu system:

- **1-18**: Run specific samples
- **A**: Run ALL samples
- **B**: Run Basic samples only (1-4)
- **Q**: Quit

Each sample includes:
- Clear explanations of what's being demonstrated
- Real-time console output showing execution flow
- Performance metrics and timing information
- Error handling demonstrations

## Configuration

Samples use `appsettings.json` for environment-specific configuration including logging levels, resilience policies, and extension settings.

## Learning Path

**Beginners**: Start with samples 1-4 (Basic Workflows)  
**Intermediate**: Try samples 5-12 (Control Flow & Configuration)  
**Advanced**: Explore samples 13-18 (Extensions & Advanced)

## Features Demonstrated

- Core workflow engine and execution patterns
- Data flow and operation results handling
- Error handling and compensation strategies
- Configuration management and environment settings
- Extension integration (logging, resilience, observability)
- Performance monitoring and optimization
- Real-world usage patterns and best practices

---

*Learn by example, build with confidence* 