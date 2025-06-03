# WorkflowForge Basic Console Samples

Interactive examples demonstrating the core features and capabilities of WorkflowForge. This console application provides a simple menu-driven interface to explore and run various workflow patterns and extensions.

## Getting Started

Simply run the application and choose from the interactive menu:

```bash
# Navigate to the samples directory
cd src/samples/WorkflowForge.Samples.BasicConsole

# Run the interactive samples
dotnet run
```

## Available Samples

### Basic Workflows (1-4)
| Sample | Description | Key Learning Points |
|--------|-------------|-------------------|
| **1. Hello World** | Simple workflow demonstration | Basic workflow creation, execution flow |
| **2. Data Passing** | Pass data between operations | Data flow, operation results, workflow data |
| **3. Multiple Outcomes** | Workflows with different results | Conditional outcomes, result handling |
| **4. Inline Operations** | Quick operations with lambdas | Inline operations, lambda expressions |

### Control Flow (5-8)
| Sample | Description | Key Learning Points |
|--------|-------------|-------------------|
| **5. Conditional Workflows** | If/else logic in workflows | Conditional branching, decision points |
| **6. ForEach Loops** | Process collections | Collection processing, loops, iteration |
| **7. Error Handling** | Handle exceptions gracefully | Exception handling, retry logic, error recovery |
| **8. Built-in Operations** | Use logging, delays, etc. | Built-in operations, logging, timing |

### Configuration & Middleware (9-12)
| Sample | Description | Key Learning Points |
|--------|-------------|-------------------|
| **9. Options Pattern** | Configuration management | .NET Options pattern, settings injection |
| **10. Configuration Profiles** | Environment-specific settings | Environment configuration, profiles |
| **11. Workflow Events** | Listen to workflow lifecycle | Event handling, lifecycle hooks, monitoring |
| **12. Middleware Usage** | Add cross-cutting concerns | Middleware pipeline, cross-cutting concerns |

### Extensions (13-17)
| Sample | Description | Key Learning Points |
|--------|-------------|-------------------|
| **13. Serilog Logging** | Structured logging | Serilog integration, structured logging |
| **14. Polly Resilience** | Retry policies and circuit breakers | Resilience patterns, fault tolerance |
| **15. OpenTelemetry** | Distributed tracing | Observability, tracing, monitoring |
| **16. Health Checks** | System monitoring | Health monitoring, diagnostics |
| **17. Performance Monitoring** | Metrics and statistics | Performance metrics, statistics |

### Advanced (18)
| Sample | Description | Key Learning Points |
|--------|-------------|-------------------|
| **18. Comprehensive Demo** | Full-featured example | Real-world integration, all features combined |

## Interactive Menu Features

- **Easy Navigation**: Simple numbered menu (1-18)
- **Quick Options**: 
  - `A` - Run ALL samples
  - `B` - Run Basic samples only (1-4)
  - `Q` - Quit
- **Visual Feedback**: Clear status indicators ([SUCCESS], [ERROR], [RUNNING])
- **Error Handling**: Graceful error recovery and reporting
- **Return to Menu**: Easy navigation back to the main menu

## Configuration

The samples use `appsettings.json` for configuration with support for:
- Environment-specific settings
- Structured logging configuration
- Extension-specific settings (Polly, Performance, etc.)

## Learning Path

**For Beginners:**
1. Start with Basic Workflows (1-4)
2. Try Control Flow samples (5-8)
3. Explore Configuration & Middleware (9-12)

**For Advanced Users:**
1. Jump to Extensions (13-17)
2. Try the Comprehensive Demo (18)
3. Examine the source code for implementation details

## Project Structure

```
WorkflowForge.Samples.BasicConsole/
├── Program.cs                 # Main application with interactive menu
├── Samples/                   # All sample implementations
│   ├── HelloWorldSample.cs    # Sample 1: Basic workflow
│   ├── DataPassingSample.cs   # Sample 2: Data flow
│   └── ...                    # Other samples
├── appsettings.json          # Configuration settings
└── README.md                 # This file
```

## Features Demonstrated

- **Core Workflow Engine**: Basic workflow creation and execution
- **Data Flow**: Passing data between operations and handling results
- **Control Flow**: Conditional logic, loops, and branching
- **Error Handling**: Exception management and recovery strategies
- **Configuration**: Options pattern, environment-specific settings
- **Middleware**: Cross-cutting concerns and pipeline customization
- **Extensions**: Logging, resilience, observability, and monitoring
- **Core Features**: Professional logging, performance monitoring, health checks

## Contributing

To add a new sample:
1. Create a new class implementing `ISample` in the `Samples/` folder
2. Add it to the `Samples` dictionary in `Program.cs`
3. Update this README with the sample description
4. Test using the interactive menu

## Tips

- **Start Simple**: Begin with sample 1 (Hello World) to understand the basics
- **Read the Output**: Each sample provides detailed console output explaining what's happening
- **Experiment**: Try modifying the samples to see how changes affect behavior
- **Check Configuration**: Look at `appsettings.json` to understand configuration options

## Available Samples

### Core Samples

| Sample | Description | Key Features |
|--------|-------------|--------------|
| **Built-in Operations** | Demonstrates core operations like logging, delays, and data processing | `LoggingOperation`, `DelayOperation`, custom operations |
| **Workflow Events** | Shows workflow lifecycle events and event handling | Event subscriptions, workflow state tracking, error handling |
| **Middleware Usage** | Comprehensive middleware examples and patterns | Built-in middleware, custom middleware, pipeline composition |
| **Performance Monitoring** | Performance metrics, statistics, and optimization | Performance tracking, statistics collection, optimization strategies |
| **Health Checks** | System health monitoring and diagnostics | Health check services, system monitoring, diagnostic information |

## Running the Samples

### Run All Samples
```bash
dotnet run --project src/samples/WorkflowForge.Samples.BasicConsole
```

### Run Specific Sample
```bash
# Run middleware sample
dotnet run --project src/samples/WorkflowForge.Samples.BasicConsole -- middleware

# Run performance monitoring sample
dotnet run --project src/samples/WorkflowForge.Samples.BasicConsole -- performance

# Run health checks sample
dotnet run --project src/samples/WorkflowForge.Samples.BasicConsole -- health
```

### Interactive Mode
```bash
# List all available samples
dotnet run --project src/samples/WorkflowForge.Samples.BasicConsole -- list

# Get help
dotnet run --project src/samples/WorkflowForge.Samples.BasicConsole -- help
```

## Sample Details

### 1. Built-in Operations Sample

**What it demonstrates:**
- Core WorkflowForge operations
- Custom operation development
- Data flow between operations
- Property management

**Key Code Examples:**
```csharp
// Basic logging operation
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("BasicWorkflow")
    .AddOperation(LoggingOperation.Info("Starting workflow"))
    .AddOperation(DelayOperation.FromMilliseconds(500))
    .AddOperation(new DataProcessingOperation("ProcessCustomerData"))
    .Build();
```

### 2. Workflow Events Sample

**What it demonstrates:**
- Workflow lifecycle events
- Event handler registration
- Error handling and recovery
- Workflow state tracking

**Key Code Examples:**
```csharp
// Event handler registration
smith.WorkflowStarted += OnWorkflowStarted;
smith.WorkflowCompleted += OnWorkflowCompleted;
smith.WorkflowFailed += OnWorkflowFailed;

// Event handling with context
private static void OnWorkflowStarted(object? sender, WorkflowStartedEventArgs e)
{
    Console.WriteLine($"Workflow started: {e.Foundry?.CurrentWorkflow?.Name}");
}
```

### 3. Middleware Usage Sample

**What it demonstrates:**
- Built-in middleware components
- Custom middleware development
- Middleware pipeline composition
- Cross-cutting concerns implementation

**Key Code Examples:**
```csharp
// Built-in middleware with workflow
var workflow = WorkflowForge.CreateWorkflow()
    .WithName("MiddlewareWorkflow")
    .AddOperation(new BusinessOperation("ProcessData"))
    .Build();

var foundry = WorkflowForge.CreateFoundry("MiddlewareWorkflow")
    .WithMiddleware(new TimingMiddleware())
    .WithMiddleware(new LoggingMiddleware(foundry.Logger));
```

### 4. Performance Monitoring Sample

**What it demonstrates:**
- Performance metrics collection
- Statistics analysis
- Performance optimization strategies
- Resource usage monitoring

**Key Code Examples:**
```csharp
// Performance statistics
var stats = foundry.GetPerformanceStatistics();
Console.WriteLine($"Total operations: {stats.TotalOperations}");
Console.WriteLine($"Average duration: {stats.AverageDuration.TotalMilliseconds:F2}ms");

// Performance-aware operations
public class PerformanceOptimizedOperation : IWorkflowOperation
{
    // Implementation with performance considerations
}
```

### 5. Health Checks Sample

**What it demonstrates:**
- System health monitoring
- Health check services
- Diagnostic information collection
- Health status reporting

**Key Code Examples:**
```csharp
// Health check service
var healthService = foundry.GetHealthCheckService();
var healthStatus = await healthService.CheckHealthAsync();

Console.WriteLine($"System Health: {healthStatus.Status}");
Console.WriteLine($"Memory Usage: {healthStatus.MemoryUsageMB:F2} MB");
```

## Architecture Patterns Demonstrated

### 1. **Clean Architecture**
- Separation of concerns
- Dependency inversion
- Interface-based design

### 2. **Middleware Pattern**
- Cross-cutting concerns
- Pipeline composition
- Aspect-oriented programming

### 3. **Event-Driven Architecture**
- Workflow lifecycle events
- Loose coupling
- Observer pattern

### 4. **Professional Logging**
- Professional logging
- Correlation IDs
- Hierarchical scopes
- Professional messaging

### 5. **Performance Monitoring**
- Metrics collection
- Resource tracking
- Performance optimization

## Customization Examples

### Custom Operations
```csharp
public class CustomBusinessOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "CustomBusiness";
    public bool SupportsRestore => true;

    public async Task<object?> ForgeAsync(object? inputData, 
        IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        // Custom business logic
        return processedData;
    }

    public async Task RestoreAsync(object? outputData, 
        IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        // Compensation logic
    }
}
```

### Custom Middleware
```csharp
public class AuditMiddleware : IWorkflowOperationMiddleware
{
    public async Task<object?> ExecuteAsync(IWorkflowOperation operation,
        IWorkflowFoundry foundry, object? inputData,
        Func<Task<object?>> next, CancellationToken cancellationToken)
    {
        // Pre-execution audit
        var result = await next();
        // Post-execution audit
        return result;
    }
}
```

## Learning Path

1. **Start with Built-in Operations** - Learn the basics
2. **Explore Workflow Events** - Understand lifecycle management
3. **Implement Middleware** - Add cross-cutting concerns
4. **Monitor Performance** - Optimize and measure
5. **Add Health Checks** - Ensure system reliability

## Best Practices Demonstrated

- **Professional logging** with correlation IDs and structured messages
- **Proper error handling** with compensation patterns
- **Performance monitoring** with metrics and optimization
- **Clean separation** of concerns with middleware
- **Resource management** with proper disposal patterns
- **Thread-safe operations** with concurrent collections
- **Testable design** with dependency injection

## Related Documentation

- [Main README](../../../README.md) - Framework overview
- [Core Documentation](../../core/WorkflowForge/README.md) - Core concepts
- [Extension Documentation](../../extensions/WorkflowForge.Extensions.*/README.md) - Extension guides
- [Benchmark Results](../../benchmarks/WorkflowForge.Benchmarks/README.md) - Performance data

---

**WorkflowForge Samples** - *Learn by example, build with confidence* 