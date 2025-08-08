# WorkflowForge Samples

Comprehensive samples and tutorials demonstrating the full capabilities of WorkflowForge and its extension ecosystem. All samples are organized within the `WorkflowForge.Samples.BasicConsole` project for consistency and ease of use.

## Sample Collection Overview

This collection provides hands-on examples showcasing WorkflowForge's capabilities, from basic workflow patterns to advanced scenarios with full extension integration.

### Key Features Demonstrated
- **Core Workflow Patterns**: Sequential, parallel, conditional, and loop-based workflows
- **Extension Integrations**: Complete ecosystem integration with observability, resilience, and logging
- **Configuration Management**: Flexible configurations and best practices  
- **Real-world Scenarios**: Robust examples and patterns
- **Testing Patterns**: Unit testing and integration testing examples
- **Best Practices**: Industry-standard patterns and anti-patterns to avoid

## Sample Structure

### Core Console Application
- **[WorkflowForge.Samples.BasicConsole](WorkflowForge.Samples.BasicConsole/)** - Main console application containing all sample categories

### Available Sample Categories

#### Basic Workflow Patterns
- **hello-world** - Simple sequential workflow with greeting operations
- **data-passing** - Data flow between workflow operations with type safety
- **multiple-outcomes** - Conditional branching and decision making patterns
- **inline-operations** - Quick operations without separate class definitions

#### Control Flow Patterns  
- **conditional** - Advanced if/then logic using ConditionalWorkflowOperation
- **foreach-loop** - Parallel and sequential collection processing with ForEachWorkflowOperation
- **error-handling** - Comprehensive exception handling, retry logic, and circuit breaker patterns

#### Configuration Examples
- **config-profiles** - Flexible configuration profiles and best practices

#### Extension Integration Showcase
- **serilog-integration** - Structured logging with rich context and correlation tracking
- **polly-resilience** - Advanced resilience patterns with circuit breakers, retries, and timeouts
- **opentelemetry-observability** - Distributed tracing, custom metrics, and observability pipelines
- **health-checks** - System health monitoring and custom health check implementations
- **performance-monitoring** - Performance metrics, memory monitoring, and benchmarking
- **middleware** - Custom middleware creation and execution pipeline patterns
- **workflow-events** - Event handling, notifications, and event-driven architectures
- **builtin-operations** - Comprehensive demonstrations of all built-in operations
- **comprehensive-integration** - Enterprise e-commerce scenario combining all extensions

## Getting Started

### Running All Samples

```bash
cd src/samples/WorkflowForge.Samples.BasicConsole
dotnet run
```

### Running Specific Samples

Use the interactive menu to select samples. Command-line arguments are not implemented at this time.

### Interactive Sample Explorer

```bash
dotnet run
```
The console will provide:
- Menu-driven sample selection
- Detailed sample descriptions
- Step-by-step execution with explanations
- Performance metrics and timing information

## Detailed Sample Descriptions

### Basic Workflow Patterns

#### hello-world
**Purpose**: Introduction to WorkflowForge fundamentals
```csharp
// Demonstrates:
// - Basic workflow creation with WorkflowBuilder
// - Sequential operation execution
// - Foundry and Smith usage patterns
// - Simple data passing between operations
```

#### data-passing  
**Purpose**: Understanding data flow and type safety
```csharp
// Demonstrates:
// - Strongly-typed data flow between operations
// - Data transformation patterns
// - Shared state management via foundry properties
// - Input validation and output typing
```

#### multiple-outcomes
**Purpose**: Conditional workflow execution
```csharp
// Demonstrates:
// - Conditional branching with ConditionalWorkflowOperation
// - Dynamic workflow path selection
// - Result aggregation from multiple branches
// - Error handling across different execution paths
```

#### inline-operations
**Purpose**: Rapid prototyping and simple operations
```csharp
// Demonstrates:
// - Lambda-based operation definitions
// - Quick workflow assembly without separate classes
// - Async operation patterns
// - Performance characteristics of inline vs class-based operations
```

### Control Flow Patterns

#### conditional
**Purpose**: Advanced conditional logic and decision trees
```csharp
// Demonstrates:
// - Complex conditional operations with multiple criteria
// - Nested conditional logic
// - Performance optimization for decision trees
// - Error handling in conditional branches
```

#### foreach-loop
**Purpose**: Collection processing patterns
```csharp
// Demonstrates:
// - Parallel vs sequential collection processing
// - Custom aggregation patterns
// - Error handling in collection operations
// - Memory management for large collections
// - Performance comparison: parallel vs sequential execution
```

#### error-handling
**Purpose**: Comprehensive resilience and error management
```csharp
// Demonstrates:
// - Exception handling strategies
// - Retry patterns with exponential backoff
// - Circuit breaker implementation
// - Saga pattern for compensation
// - Error aggregation and reporting
// - Custom exception types and handling
```

### Configuration Examples

#### config-profiles
**Purpose**: Flexible configuration profiles and best practices
```csharp
// Demonstrates:
// - Development configuration (verbose logging, detailed metrics)
// - Production configuration (optimized performance, minimal logging)
// - Enterprise configuration (comprehensive monitoring, compliance)
// - High-performance configuration (maximum throughput optimization)
// - Custom configuration creation and validation
```

### Extension Integration Showcase

#### serilog-integration
**Purpose**: Professional structured logging
```csharp
// Demonstrates:
// - Structured logging with rich context
// - Correlation ID tracking across operations
// - Performance logging and metrics
// - Error logging with stack traces and context
// - Log aggregation and searching patterns
// - Environment-specific logging configurations
```

#### polly-resilience
**Purpose**: Advanced resilience patterns with Polly
```csharp
// Demonstrates:
// - Circuit breaker patterns with failure thresholds
// - Exponential backoff retry strategies
// - Timeout management and cancellation
// - Rate limiting and throttling
// - Policy combination and chaining
// - Real-world failure simulation and recovery
```

#### opentelemetry-observability
**Purpose**: Distributed tracing and metrics collection
```csharp
// Demonstrates:
// - Distributed tracing with correlation across services
// - Custom metrics creation and collection
// - Performance monitoring and alerting
// - Integration with observability backends (Jaeger, Prometheus)
// - Business metrics and KPI tracking
// - Trace sampling strategies for production
```

#### health-checks
**Purpose**: System health monitoring and diagnostics
```csharp
// Demonstrates:
// - Built-in health checks (memory, GC, thread pool)
// - Custom health check implementation
// - Health check aggregation and reporting
// - Integration with monitoring systems
// - Performance impact assessment
// - Health-based auto-scaling patterns
```

#### performance-monitoring
**Purpose**: Performance analysis and optimization
```csharp
// Demonstrates:
// - Real-time performance metrics collection
// - Memory allocation tracking and optimization
// - Throughput analysis and bottleneck identification
// - Performance baseline establishment
// - Load testing integration
// - Performance regression detection
```

#### comprehensive-integration
**Purpose**: Real-world enterprise e-commerce scenario
```csharp
// Demonstrates:
// - Complete order processing workflow
// - Integration of all WorkflowForge extensions
// - Production-ready error handling and resilience
// - Comprehensive logging and monitoring
// - Performance optimization techniques
// - Scalability patterns and considerations
```

## Adding New Samples

### Step-by-Step Guide

1. **Create Sample Class**: Implement the `ISample` interface
```csharp
public class MyCustomSample : ISample
{
    public string Name => "my-custom-sample";
    public string Category => "custom";
    public string Description => "Demonstrates custom workflow patterns";
    public TimeSpan EstimatedDuration => TimeSpan.FromMinutes(2);
    
    public async Task RunAsync()
    {
        Console.WriteLine("Starting My Custom Sample...");
        
        // Your sample implementation here
        var foundry = WorkflowForge.CreateFoundry("CustomSample");
        
        // ... implementation details ...
        
        Console.WriteLine("Custom sample completed successfully.");
    }
}
```

2. **Register Sample**: Add to the samples collection in `Program.cs`
```csharp
private static readonly Dictionary<string, ISample> Samples = new()
{
    // ... existing samples ...
    ["my-custom-sample"] = new MyCustomSample(),
};
```

3. **Update Documentation**: Add description to this README
4. **Add Unit Tests**: Create corresponding test cases
5. **Performance Benchmarks**: Include performance considerations

### Sample Development Guidelines

- **Clear Console Output**: Use concise, consistent formatting for readability
- **Comprehensive Comments**: Explain key concepts and patterns
- **Error Handling**: Demonstrate proper exception management
- **Performance Notes**: Include timing and memory usage information
- **Real-world Relevance**: Base samples on actual use cases
- **Progressive Complexity**: Build from simple to advanced concepts

## Extension Integration Examples

### Complete Extension Stack
```csharp
// Example from comprehensive-integration sample
var foundryConfig = FoundryConfiguration.ForEnterprise()
    .UseSerilog(logger => logger
        .MinimumLevel.Information()
        .WriteTo.Console()
        .WriteTo.File("logs/ecommerce-.txt"))
    .UsePollyEnterpriseResilience()
    .EnableOpenTelemetry("ECommerceService", "1.0.0")
    .EnableHealthChecks()
    .EnablePerformanceMonitoring();

var foundry = WorkflowForge.CreateFoundry("ECommerceWorkflow", foundryConfig);
```

### Extension-Specific Patterns

#### Logging Extensions
- Structured logging with business context
- Correlation tracking across distributed systems
- Performance logging and analysis
- Error aggregation and alerting

#### Resilience Extensions  
- Multi-layer retry strategies (fast fail → exponential backoff)
- Circuit breaker patterns with health check integration
- Timeout management with graceful degradation
- Rate limiting for external service protection

#### Observability Extensions
- End-to-end distributed tracing
- Business metrics and KPI tracking
- Real-time performance monitoring
- Custom dashboard creation and alerting

## Learning Path Recommendations

### Beginner Path (1-2 hours)
1. **hello-world** - Understand basic concepts (15 min)
2. **data-passing** - Learn data flow patterns (20 min)
3. **conditional** - Explore branching logic (25 min)
4. **config-profiles** - Understand configuration management (30 min)

### Intermediate Path (2-3 hours)
1. **error-handling** - Master resilience patterns (45 min)
2. **foreach-loop** - Collection processing optimization (30 min)
3. **serilog-integration** - Structured logging implementation (45 min)
4. **performance-monitoring** - Performance analysis techniques (30 min)

### Advanced Path (3-4 hours)
1. **polly-resilience** - Enterprise resilience patterns (60 min)
2. **opentelemetry-observability** - Distributed observability (60 min)
3. **health-checks** - System monitoring and diagnostics (45 min)
4. **comprehensive-integration** - Real-world enterprise scenario (90 min)

### Expert Path (4+ hours)
1. **middleware** - Custom middleware development (60 min)
2. **workflow-events** - Event-driven architecture patterns (60 min)
3. **builtin-operations** - Advanced operation patterns (45 min)
4. **Custom sample creation** - Build your own samples (120+ min)

## Sample Features Matrix

| Sample | Core | Logging | Resilience | Observability | Health | Performance |
|--------|------|---------|------------|---------------|--------|-------------|
| hello-world | Yes | No | No | No | No | No |
| data-passing | Yes | No | No | No | No | No |
| conditional | Yes | No | No | No | No | No |
| error-handling | Yes | Yes | Yes | No | No | No |
| serilog-integration | Yes | Yes | No | No | No | No |
| polly-resilience | Yes | Yes | Yes | No | No | No |
| opentelemetry-observability | Yes | Yes | No | Yes | No | No |
| health-checks | Yes | Yes | No | No | Yes | No |
| performance-monitoring | Yes | Yes | No | No | No | Yes |
| comprehensive-integration | Yes | Yes | Yes | Yes | Yes | Yes |

## Prerequisites

### Required Software
- **.NET 8.0 SDK** or later
- **Visual Studio 2022** or **VS Code** with C# extension
- **Git** for cloning the repository

### Recommended Tools
- **Docker Desktop** (for observability backend samples)
- **Postman** or **curl** (for API integration samples)
- **Performance profilers** (dotMemory, PerfView) for advanced performance analysis

### Knowledge Prerequisites
- **C# Programming**: Intermediate level with async/await patterns
- **Dependency Injection**: Basic understanding of DI containers
- **Enterprise Patterns**: Familiarity with logging, monitoring, and resilience concepts

## Documentation Links

### Core Framework
- [Core Framework Documentation](../core/WorkflowForge/README.md)

### Extensions  
#### Logging Extensions
- [Serilog Logging Extension](../extensions/WorkflowForge.Extensions.Logging.Serilog/README.md)
#### Resilience Extensions  
- [Polly Resilience Extension](../extensions/WorkflowForge.Extensions.Resilience.Polly/README.md)
#### Observability Extensions
- [OpenTelemetry Extension](../extensions/WorkflowForge.Extensions.Observability.OpenTelemetry/README.md)
- [Health Checks Extension](../extensions/WorkflowForge.Extensions.Observability.HealthChecks/README.md)
- [Performance Monitoring Extension](../extensions/WorkflowForge.Extensions.Observability.Performance/README.md)

### Supporting Resources
- [Performance Benchmarks](../benchmarks/WorkflowForge.Benchmarks/README.md)

## Contributing

### Adding New Samples
1. **Follow the ISample interface pattern** for consistency
2. **Include comprehensive console output** showing execution flow
3. **Add detailed comments** explaining key concepts and decisions
4. **Update the samples registry** in `Program.cs`
5. **Update this README** with sample descriptions and learning path updates
6. **Include performance benchmarks** where applicable
7. **Add unit tests** for complex sample logic

### Sample Quality Standards
- **Clear Objectives**: Each sample should have a specific learning goal
- **Progressive Complexity**: Build concepts incrementally
- **Real-world Relevance**: Base examples on actual use cases
- **Performance Awareness**: Include timing and resource usage information
- **Error Handling**: Demonstrate proper exception management
- **Documentation**: Comprehensive inline comments and README updates

---

**WorkflowForge Samples** - *Learn by example with comprehensive hands-on tutorials* 