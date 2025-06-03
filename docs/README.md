# WorkflowForge Documentation

Comprehensive documentation for WorkflowForge, the modern workflow orchestration framework for .NET.

[![GitHub Repository](https://img.shields.io/badge/GitHub-animatlabs%2Fworkflow--forge-blue?logo=github)](https://github.com/animatlabs/workflow-forge)
[![Documentation](https://img.shields.io/badge/Docs-Latest-green?logo=gitbook)](https://github.com/animatlabs/workflow-forge/tree/main/docs)

## 🎯 Quick Start Learning Path

### 🚀 Interactive Samples (Recommended Starting Point)
**The fastest way to learn WorkflowForge is through our comprehensive interactive samples:**

```bash
cd src/samples/WorkflowForge.Samples.BasicConsole
dotnet run
```

**Learning Path:** 18 hands-on examples from basic to advanced
- **Beginner (1-4)**: Hello World, Data Passing, Conditions, Inline Operations
- **Intermediate (5-12)**: Control Flow, Error Handling, Configuration, Middleware
- **Advanced (13-18)**: Extensions, Observability, Resilience, Comprehensive Integration

**➜ [View Complete Sample Collection](../src/samples/WorkflowForge.Samples.BasicConsole/)**

### 📖 Core Documentation
- **[Getting Started Guide](getting-started.md)** - Step-by-step introduction
- **[Architecture Overview](architecture.md)** - Core design principles
- **[Operations Guide](operations.md)** - Building custom operations
- **[Configuration Reference](configuration.md)** - Complete configuration options

### 🔧 Reference Documentation
- **[API Reference](api-reference.md)** - Complete API documentation
- **[Extensions Overview](extensions.md)** - Available extensions and usage

## 🎯 Why WorkflowForge?

### Zero Dependencies, Maximum Power
- **Minimal deployment footprint** (~50KB core)
- **No version conflicts** with your existing dependencies
- **Maximum compatibility** across .NET versions
- **Lightweight containers** and edge deployments

### Performance That Scales
- **15x better concurrency scaling** - 16 concurrent workflows vs sequential execution
- **Microsecond-level operations** - Custom operations execute in 4-56 μs range
- **Memory efficient** with <2KB per operation
- **Concurrent execution** with excellent parallel performance

### Feature-Rich Architecture
- **Built-in compensation** (saga pattern) for automatic rollback
- **Middleware pipeline** similar to ASP.NET Core
- **Comprehensive observability** with metrics, tracing, and health checks
- **Advanced resilience** with circuit breakers and retries

## 📚 Documentation Structure

### Getting Started
| Document | Description | Audience |
|----------|-------------|----------|
| [Getting Started](getting-started.md) | Step-by-step introduction | New users |
| [Interactive Samples](../src/samples/WorkflowForge.Samples.BasicConsole/) | **Recommended starting point** | All users |

### Core Concepts
| Document | Description | Audience |
|----------|-------------|----------|
| [Architecture](architecture.md) | Core design principles | Developers |
| [Operations](operations.md) | Building custom operations | Developers |
| [Configuration](configuration.md) | Configuration management | All users |

### Extensions
| Document | Description | Audience |
|----------|-------------|----------|
| [Extensions Overview](extensions.md) | Available extensions | All users |

### Reference
| Document | Description | Audience |
|----------|-------------|----------|
| [API Reference](api-reference.md) | Complete API documentation | Developers |

## 🏆 Quick Navigation

### For New Users
1. **[Interactive Samples](../src/samples/WorkflowForge.Samples.BasicConsole/)** - Learn by doing (recommended)
2. **[Getting Started Guide](getting-started.md)** - Traditional tutorial approach
3. **[Architecture Overview](architecture.md)** - Understand the design

### For Experienced Developers
1. **[API Reference](api-reference.md)** - Complete API documentation
2. **[Operations Guide](operations.md)** - Building custom operations
3. **[Extensions](extensions.md)** - Adding capabilities

### For DevOps/Configuration
1. **[Configuration Reference](configuration.md)** - All configuration options
2. **[Extensions Overview](extensions.md)** - Available extensions
3. **[Performance Benchmarks](../src/benchmarks/WorkflowForge.Benchmarks/)** - Performance data

## 🏭 WorkflowForge Metaphor

WorkflowForge uses an **industrial metaphor** that makes workflow concepts intuitive:

- **🏭 The Forge** - Main factory for creating workflows and components
- **⚒️ Foundries** - Execution environments where operations are performed  
- **👨‍🔧 Smiths** - Skilled craftsmen who manage foundries and forge workflows
- **⚙️ Operations** - Individual tasks performed in the foundry
- **📋 Workflows** - Complete workflow definitions with operations

This metaphor provides a consistent mental model throughout the framework.

## 🔧 Core Features Highlighted

### Developer Experience
- **Fluent API** with IntelliSense support
- **Industrial metaphor** for intuitive understanding
- **Comprehensive examples** with real-world scenarios
- **Test-first design** with mockable interfaces

### Performance & Efficiency
- **Zero-dependency core** for minimal footprint
- **Memory-optimized** with object pooling
- **Async-first** design throughout
- **Benchmark-proven** performance characteristics

### Production Features
- **Automatic compensation** for robust error handling
- **Rich observability** with metrics and distributed tracing
- **Advanced resilience** patterns
- **Configuration management** for different environments

## 📝 Documentation Conventions

### Code Examples
All code examples are tested and maintained:
- **Complete examples** that can be run as-is
- **Clear naming** without emojis or casual language
- **Robust patterns** suitable for production deployment
- **Clear comments** explaining key concepts

### Versioning
Documentation is versioned alongside the codebase:
- **Current version**: Matches the latest release
- **Version compatibility** is noted where applicable
- **Migration guides** are provided for breaking changes

## 🤝 Contributing to Documentation

We welcome contributions to improve the documentation:

1. **Identify gaps** - What's missing or unclear?
2. **Propose improvements** - Submit issues or pull requests
3. **Follow conventions** - Maintain professional tone and structure
4. **Test examples** - Ensure all code examples work correctly

## 🆘 Support

- **Issues**: Report documentation issues on [GitHub Issues](https://github.com/animatlabs/workflow-forge/issues)
- **Discussions**: Ask questions in [GitHub Discussions](https://github.com/animatlabs/workflow-forge/discussions)
- **Repository**: [github.com/animatlabs/workflow-forge](https://github.com/animatlabs/workflow-forge)

---

**WorkflowForge Documentation** - *Comprehensive guides for workflow orchestration mastery* 