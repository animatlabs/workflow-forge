# WorkflowForge Basic Console Samples

<p align="center">
  <img src="../../../icon.png" alt="WorkflowForge" width="120" height="120">
</p>

Interactive examples demonstrating the core features and capabilities of WorkflowForge 2.0.

## Getting Started

```bash
cd src/samples/WorkflowForge.Samples.BasicConsole
dotnet run
```

## Available Samples (24 Total)

### Basic Workflows (1-4)
- **1. Hello World** - Simple workflow demonstration
- **2. Data Passing** - Pass data between operations via foundry properties
- **3. Multiple Outcomes** - Workflows with different results
- **4. Inline Operations** - Quick operations with lambdas

### Control Flow (5-8)
- **5. Conditional Workflows** - If/else logic using ConditionalWorkflowOperation
- **6. ForEach Loops** - Process collections in parallel or sequential
- **7. Error Handling** - Exception handling and automatic compensation
- **8. Built-in Operations** - Use logging, delays, and more

### Configuration & Middleware (9-12)
- **9. Options Pattern** - ASP.NET Core IOptions<T> integration
- **10. Configuration Profiles** - Environment-specific settings (Dev/Prod)
- **11. Workflow Events** - SRP event system (Lifecycle, Operation, Compensation)
- **12. Middleware** - Cross-cutting concerns with operation middleware

### Extensions (13-18, 21-24)
- **13. Serilog Logging** - Structured logging with Serilog (zero conflicts)
- **14. Polly Resilience** - Retry, circuit breaker, timeout policies (zero conflicts)
- **15. OpenTelemetry** - Distributed tracing with Jaeger (zero conflicts)
- **16. Health Checks** - ASP.NET Core health monitoring integration
- **17. Performance Monitoring** - Operation timing and metrics
- **18. Persistence** - Workflow state checkpointing (BYO storage provider)
- **21. Recovery Only** - Retry workflows without persistence
- **22. Resilience + Recovery** - Combined Polly resilience and recovery
- **23. Validation** - FluentValidation integration (zero conflicts)
- **24. Audit** - Compliance audit logging with pluggable providers

### Advanced (19-20)
- **19. Comprehensive Integration** - Full production workflow with all extensions
- **20. Operation Creation Patterns** - All operation creation methods

## Interactive Menu

The console application provides an easy-to-use menu system:

- **1-24**: Run specific samples by number
- **A**: Run ALL samples sequentially
- **B**: Run Basic samples only (1-4)
- **Q**: Quit the application

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
**Advanced Users**: Explore samples 13-18, 21-24 (Extensions)  
**Production**: Study samples 19-20 (Comprehensive Integration & Patterns)

## Features Demonstrated

- **Core Engine**: Workflow creation, operation execution, data flow via foundry properties
- **Control Flow**: Conditionals, loops (ForEach), error handling with compensation
- **Configuration**: IOptions<T> pattern, environment profiles, middleware
- **Events**: SRP-compliant event system (Workflow, Operation, Compensation)
- **Extensions**: All 10 extensions with zero dependency conflicts (Costura.Fody)
- **Resilience**: Retry, circuit breaker, timeout policies with Polly
- **Observability**: Logging, tracing, health checks, performance monitoring
- **Persistence**: Workflow state checkpointing and recovery
- **Validation**: FluentValidation integration for input validation
- **Audit**: Compliance audit logging with pluggable storage
- **Best Practices**: Production-grade patterns and proven architectures

## Documentation

For detailed explanations of all samples, see [docs/samples-guide.md](../../../docs/samples-guide.md)

---

**WorkflowForge Samples** - *Build workflows with industrial strength* 