# WorkflowForge Documentation

Comprehensive documentation for WorkflowForge, the modern workflow orchestration framework for .NET.

[![GitHub Repository](https://img.shields.io/badge/GitHub-animatlabs%2Fworkflow--forge-blue?logo=github)](https://github.com/animatlabs/workflow-forge)
[![Documentation](https://img.shields.io/badge/Docs-Latest-green?logo=gitbook)](https://github.com/animatlabs/workflow-forge/tree/main/docs)

## 🎯 Quick Start Learning Path

### **🚀 Start Here - Interactive Samples (Recommended)**
**[Complete Sample Collection](../src/samples/WorkflowForge.Samples.BasicConsole/README.md)** - 18 hands-on examples covering everything from basics to advanced patterns:

```bash
cd src/samples/WorkflowForge.Samples.BasicConsole
dotnet run
```

**Learning Path:**
- **Beginner (1-4)**: Hello World, Data Passing, Conditions, Inline Operations
- **Intermediate (5-12)**: Control Flow, Error Handling, Configuration, Middleware
- **Advanced (13-18)**: Extensions, Observability, Resilience, Comprehensive Integration

### **📖 Core Documentation**
- **[Getting Started Guide](getting-started.md)** - Step-by-step introduction
- **[Core Framework](../src/core/WorkflowForge/README.md)** - Architecture and core concepts
- **[Extensions Overview](../src/extensions/)** - Available extensions

### **🔧 Reference Documentation**
- **[API Reference](api-reference.md)** - Complete API documentation
- **[Configuration Reference](configuration-reference.md)** - All configuration options
- **[Performance Benchmarks](../src/benchmarks/README.md)** - Verified performance claims

## 🎯 Why WorkflowForge?

### **Zero Dependencies, Maximum Power**
- **Minimal deployment footprint** (~50KB core)
- **No version conflicts** with your existing dependencies
- **Maximum compatibility** across .NET versions
- **Lightweight containers** and edge deployments

### **Performance That Scales**
- **15x better concurrency scaling** - 16 concurrent workflows vs sequential execution
- **Microsecond-level operations** - Custom operations execute in 4-56 μs range
- **Memory efficient** with <2KB per operation
- **Concurrent execution** with excellent parallel performance

### **Feature-Rich Architecture**
- **Built-in compensation** (saga pattern) for automatic rollback
- **Middleware pipeline** similar to ASP.NET Core
- **Comprehensive observability** with metrics, tracing, and health checks
- **Advanced resilience** with circuit breakers and retries

## Documentation Structure

### Getting Started
- **[Getting Started Guide](getting-started.md)** - Step-by-step introduction
- **[Installation & Setup](installation.md)** - Installation and initial configuration
- **[Your First Workflow](first-workflow.md)** - Creating and executing workflows

### Core Concepts
- **[Architecture Overview](architecture.md)** - Core design principles
- **[Workflow Concepts](concepts.md)** - Understanding workflows, operations, foundries
- **[Configuration](configuration.md)** - Configuration management

### Implementation Guides
- **[Building Operations](operations.md)** - Creating custom workflow operations
- **[Middleware Development](middleware.md)** - Building and using middleware
- **[Error Handling & Compensation](error-handling.md)** - Robust error handling
- **[Testing Workflows](testing.md)** - Unit and integration testing

### Extensions
- **[Extension System](extensions.md)** - Overview of the extension ecosystem
- **[Logging Extensions](extensions/logging.md)** - Structured logging with Serilog
- **[Resilience Extensions](extensions/resilience.md)** - Retry policies and circuit breakers
- **[Observability Extensions](extensions/observability.md)** - Monitoring and tracing

### Advanced Topics
- **[Performance Optimization](performance.md)** - Best practices for high-performance workflows
- **[Scalability & Concurrency](scalability.md)** - Designing workflows for scale
- **[Security Considerations](security.md)** - Security best practices

### Reference
- **[API Reference](api-reference.md)** - Complete API documentation
- **[Configuration Reference](configuration-reference.md)** - All configuration options
- **[Migration Guide](migration.md)** - Upgrading between versions
- **[Troubleshooting](troubleshooting.md)** - Common issues and solutions

## Quick Navigation

| Topic | Description | Audience |
|-------|-------------|----------|
| [Interactive Samples](../src/samples/WorkflowForge.Samples.BasicConsole/README.md) | **Recommended starting point** | All users |
| [Getting Started](getting-started.md) | Basic introduction and first steps | New users |
| [Architecture](architecture.md) | Core design and abstractions | Developers |
| [Extensions](extensions.md) | Using and creating extensions | All users |
| [Performance](performance.md) | Optimization techniques | Advanced users |

## 🏆 Competitive Advantages

### **Developer Experience**
- **Fluent API** with IntelliSense support
- **Industrial metaphor** (Foundries, Smiths) for intuitive understanding
- **Comprehensive examples** with real-world scenarios
- **Test-first design** with mockable interfaces

### **Performance & Efficiency**
- **Zero-dependency core** for minimal footprint
- **Memory-optimized** with object pooling
- **Async-first** design throughout
- **Benchmark-proven** performance characteristics

### **Core Features**
- **Automatic compensation** for robust error handling
- **Rich observability** with metrics and distributed tracing
- **Advanced resilience** patterns
- **Configuration management** for different environments

## Documentation Conventions

### Code Examples
All code examples are tested and maintained. They follow these conventions:
- **Complete examples** that can be run as-is
- **Clear naming** without emojis or casual language
- **Robust patterns** suitable for any deployment
- **Clear comments** explaining key concepts

### Versioning
Documentation is versioned alongside the codebase:
- **Current version**: Matches the latest release
- **Version compatibility** is noted where applicable
- **Migration guides** are provided for breaking changes

## Contributing to Documentation

We welcome contributions to improve the documentation:

1. **Identify gaps** - What's missing or unclear?
2. **Propose improvements** - Submit issues or pull requests
3. **Follow conventions** - Maintain professional tone and structure
4. **Test examples** - Ensure all code examples work correctly

## Support

- **Issues**: Report documentation issues on [GitHub Issues](https://github.com/animatlabs/workflow-forge/issues)
- **Discussions**: Ask questions in [GitHub Discussions](https://github.com/animatlabs/workflow-forge/discussions)
- **Repository**: [github.com/animatlabs/workflow-forge](https://github.com/animatlabs/workflow-forge)

---

**WorkflowForge Documentation** - *Comprehensive guides for workflow orchestration mastery* 