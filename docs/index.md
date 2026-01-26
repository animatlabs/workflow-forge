---
layout: default
title: WorkflowForge Documentation
---

<div class="wf-hero">
  <img src="https://raw.githubusercontent.com/animatlabs/workflow-forge/main/icon.png" alt="WorkflowForge logo">
  <div>
    <span class="wf-badge">Enterprise-grade workflows</span>
    <h1>WorkflowForge Documentation</h1>
    <p>Build high-performance workflows in .NET with clear guidance, focused examples, and a zero-dependency core.</p>
    <div class="wf-hero-actions">
      <a href="{{ "/getting-started/getting-started/" | relative_url }}">Get started</a>
      <a class="secondary" href="{{ "/reference/api-reference/" | relative_url }}">API reference</a>
    </div>
  </div>
</div>

<div class="wf-card-grid">
  <div class="wf-card">
    <h3>Core Concepts</h3>
    <p>Understand foundries, smiths, operations, and lifecycle events.</p>
  </div>
  <div class="wf-card">
    <h3>Performance First</h3>
    <p>Benchmark-backed results with microsecond execution and minimal memory.</p>
  </div>
  <div class="wf-card">
    <h3>Extensions Ready</h3>
    <p>Logging, resilience, persistence, and observability without conflicts.</p>
  </div>
</div>

**Welcome to the complete WorkflowForge documentation** - Your guide to building high-performance workflows in .NET.

---

## Table of Contents

- [Quick Navigation](#quick-navigation)
- [What is WorkflowForge?](#what-is-workflowforge)
- [Why Choose WorkflowForge?](#why-choose-workflowforge)
- [Core Architecture](#core-architecture)
- [Documentation Structure](#documentation-structure)
- [Quick Start](#quick-start)
- [Learning Path](#learning-path)
- [Extension Ecosystem](#extension-ecosystem)
- [Performance Highlights](#performance-highlights)
- [Getting Help](#getting-help)

---

## Quick Navigation

### Getting Started
- **[Getting Started Guide](getting-started/getting-started.md)** - Step-by-step tutorial for new users
- **[Quick Start (Root README)](../README.md)** - Installation and first workflow
- **[Interactive Samples](getting-started/samples-guide.md)** - 33 hands-on examples

### Core Concepts
- **[Architecture Overview](architecture/overview.md)** - Design principles, metaphor, and patterns
- **[API Reference](reference/api-reference.md)** - Complete API documentation
- **[Operations Guide](core/operations.md)** - Creating and using operations
- **[Event System](core/events.md)** - Lifecycle events and monitoring
- **[Configuration](core/configuration.md)** - Settings and options

### Performance & Comparison
- **[Performance Analysis](performance/performance.md)** - Internal + comparative benchmark data, artifacts linked
- **[Competitive Analysis](performance/competitive-analysis.md)** - Summary only; details and artifacts inside

### Extensions & Samples
- **[Extensions Overview](extensions/index.md)** - All 10 available extensions
- **[Samples Guide](getting-started/samples-guide.md)** - Complete guide to 33 progressive examples

### Contributing
- **[Contributing Guidelines](../CONTRIBUTING.md)** - How to contribute to WorkflowForge

---

## What is WorkflowForge?

WorkflowForge is a **zero-dependency workflow orchestration framework** for .NET with **microsecond-level performance** and **minimal memory footprint**. It provides a clean, industrial metaphor for building workflows that are fast, maintainable, and production-ready.

### Key Features

- **World-Class Performance**: 13-378x faster than leading alternatives
- **Minimal Memory**: 6-1,495x less memory usage
- **Zero Dependencies**: Core package with no external dependencies
- **Production Ready**: Built-in compensation (saga pattern), comprehensive testing
- **Extension Ecosystem**: 10 optional extensions with zero version conflicts
- **Developer Experience**: Fluent API, clear metaphor, 33 progressive samples

---

## The Industrial Metaphor

WorkflowForge uses an industrial metaphor that makes workflows intuitive:

- **The Forge** (`WorkflowForge` static class) - Main factory for creating workflows
- **Foundries** (`IWorkflowFoundry`) - Execution environments where operations run
- **Smiths** (`IWorkflowSmith`) - Orchestration engines managing workflow execution
- **Operations** (`IWorkflowOperation`) - Individual tasks within workflows

This metaphor provides clarity: *data (raw materials) flows through operations (tools) in a foundry (workspace), orchestrated by a smith (craftsman)*.

---

## Core Abstractions

### IWorkflow
Complete workflow definition with operations and metadata.
```csharp
public interface IWorkflow : IDisposable
{
    Guid Id { get; }
    string Name { get; }
    string? Description { get; }
    string Version { get; }
    IReadOnlyList<IWorkflowOperation> Operations { get; }
    bool SupportsRestore { get; }
}
```

### IWorkflowFoundry
Execution environment providing context, logging, and services.
```csharp
public interface IWorkflowFoundry :
    IWorkflowExecutionContext,
    IWorkflowMiddlewarePipeline,
    IOperationLifecycleEvents,
    IDisposable
{
    Guid ExecutionId { get; }
    IWorkflow? CurrentWorkflow { get; }
    ConcurrentDictionary<string, object?> Properties { get; }
    IWorkflowForgeLogger Logger { get; }
    WorkflowForgeOptions Options { get; }
    IServiceProvider? ServiceProvider { get; }
    Task ForgeAsync(CancellationToken cancellationToken = default);
    void ReplaceOperations(IEnumerable<IWorkflowOperation> operations);
    bool IsFrozen { get; }
}
```

### IWorkflowSmith
Orchestration engine executing workflows.
```csharp
public interface IWorkflowSmith : IDisposable, IWorkflowLifecycleEvents, ICompensationLifecycleEvents
{
    Task ForgeAsync(IWorkflow workflow, CancellationToken cancellationToken = default);
    Task ForgeAsync(IWorkflow workflow, ConcurrentDictionary<string, object?> data, CancellationToken cancellationToken = default);
    Task ForgeAsync(IWorkflow workflow, IWorkflowFoundry foundry, CancellationToken cancellationToken = default);
}
```

Smiths also provide `CreateFoundry*` helpers and `AddWorkflowMiddleware` for workflow-level middleware.

### IWorkflowOperation
Individual executable operation within a workflow.
```csharp
public interface IWorkflowOperation : IDisposable
{
    Guid Id { get; }
    string Name { get; }
    bool SupportsRestore { get; }
    
    Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default);
    Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default);
}
```

For complete API documentation, see [API Reference](reference/api-reference.md).

---

## Data Flow Patterns

WorkflowForge supports two data flow patterns:

### Primary Pattern: Dictionary-Based Context
**Recommended for most workflows**. All data stored in `foundry.Properties` (thread-safe `ConcurrentDictionary`).

```csharp
workflow.AddOperation("StoreData", async (foundry, ct) => {
    foundry.SetProperty("OrderId", orderId);
    foundry.SetProperty("Customer", customer);
});

workflow.AddOperation("RetrieveData", async (foundry, ct) => {
    var orderId = foundry.GetPropertyOrDefault<string>("OrderId");
    var customer = foundry.GetPropertyOrDefault<string>("Customer");
});
```

### Secondary Pattern: Type-Safe Operations
For explicit contracts between operations, use `IWorkflowOperation<TInput, TOutput>`.

```csharp
public class ValidateOrderOperation : WorkflowOperationBase<Order, ValidationResult>
{
    public override async Task<ValidationResult> ForgeAsync(
        Order input, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        // Type-safe input and output
        return new ValidationResult { IsValid = input.IsValid() };
    }
}
```

For detailed patterns, see [Operations Guide](core/operations.md).

---

## Event System (SRP-Compliant)

WorkflowForge provides three focused event interfaces following Single Responsibility Principle:

### IWorkflowLifecycleEvents
Workflow-level events: `WorkflowStarted`, `WorkflowCompleted`, `WorkflowFailed`

### IOperationLifecycleEvents
Operation-level events: `OperationStarted`, `OperationCompleted`, `OperationFailed`

### ICompensationLifecycleEvents
Compensation events: `CompensationStarted`, `CompensationCompleted`, `CompensationFailed`

For comprehensive event handling, see [Event System Guide](core/events.md).

---

## Extensions Ecosystem

WorkflowForge provides **10 optional extensions** for additional capabilities:

| Extension | Purpose | Package |
|-----------|---------|---------|
| **Serilog Logging** | Structured logging | `WorkflowForge.Extensions.Logging.Serilog` |
| **Resilience** | Core retry abstractions | `WorkflowForge.Extensions.Resilience` |
| **Polly Resilience** | Circuit breakers, retries | `WorkflowForge.Extensions.Resilience.Polly` |
| **Validation** | Input validation, DataAnnotations | `WorkflowForge.Extensions.Validation` |
| **Audit Logging** | Compliance & audit trails | `WorkflowForge.Extensions.Audit` |
| **Persistence** | Workflow state storage | `WorkflowForge.Extensions.Persistence` |
| **Persistence Recovery** | Resume interrupted workflows | `WorkflowForge.Extensions.Persistence.Recovery` |
| **Performance Monitoring** | Metrics & profiling | `WorkflowForge.Extensions.Observability.Performance` |
| **Health Checks** | Application health | `WorkflowForge.Extensions.Observability.HealthChecks` |
| **OpenTelemetry** | Distributed tracing | `WorkflowForge.Extensions.Observability.OpenTelemetry` |

**Dependency Isolation**: Extensions internalize Serilog/Polly/OpenTelemetry with ILRepack while keeping Microsoft/System external.

For detailed extension documentation, see [Extensions Guide](extensions/index.md).

---

## Performance Highlights

Based on rigorous BenchmarkDotNet testing against WorkflowCore 3.17 and Elsa Workflows 3.5.1:

| Metric | WorkflowForge | Competitors | Advantage |
|--------|---------------|-------------|-----------|
| **Execution Speed** | 6.9-306 μs | 882-106,115 μs | **13-378x faster** |
| **Memory Usage** | 1.73-87.93 KB | 44.51-19,104.55 KB | **6-1,495x less** |
| **Creation Overhead** | 6.9 μs | 882-2,605 μs | **128-378x faster** |

**Update Note**: Performance numbers will be refreshed after the upcoming benchmark rerun.

**Test System**: Windows 11, .NET 8.0.21, 25 iterations per benchmark

For comprehensive performance analysis, see [Performance Documentation](performance/performance.md).

---

## Code Review Scores (Post-Phase-1)

**Overall**: 89/100 (Target: 95/100)

| Category | Score |
|----------|-------|
| Architecture | 92/100 |
| Testing Support | 92/100 |
| API Usability | 90/100 |
| Feature Completeness | 90/100 |
| Threading & Concurrency | 90/100 |
| Getting Started | 90/100 |
| Documentation | 88/100 |
| Advanced Usage | 88/100 |
| Backward Compatibility | 88/100 |
| Performance | 87/100 |
| Intermediate Usage | 85/100 |
| Error Handling | 85/100 |

Detailed breakdowns are in `CODE_REVIEW_SUMMARY.md` and `COMPREHENSIVE_CODE_REVIEW.md`.

---

## Learning Path

### 1. Start Here: Quick Start
[Root README](../README.md) - Install and run your first workflow in 5 minutes.

### 2. Run Interactive Samples
[Samples Guide](getting-started/samples-guide.md) - 33 progressive examples from "Hello World" to production patterns.

```bash
cd src/samples/WorkflowForge.Samples.BasicConsole
dotnet run
```

### 3. Understand Core Concepts
- [Architecture](architecture/overview.md) - Design principles
- [Operations](core/operations.md) - Operation patterns
- [Events](core/events.md) - Lifecycle monitoring

### 4. Explore Extensions
[Extensions Guide](extensions/index.md) - Add logging, resilience, persistence, etc.

### 5. Deep Dive
- [API Reference](reference/api-reference.md) - Complete API documentation
- [Performance](performance/performance.md) - Optimization techniques
- [Configuration](core/configuration.md) - Advanced settings

---

## Common Use Cases

### High-Throughput APIs
Microsecond-level performance ideal for request processing.

### Cloud/Serverless Functions
Minimal memory footprint reduces costs in AWS Lambda, Azure Functions, etc.

### Microservices Orchestration
Lightweight workflows for service coordination.

### Data Processing Pipelines
ForEach operations efficiently process collections.

### Saga Pattern / Distributed Transactions
Built-in compensation for rollback scenarios.

---

## Support & Community

- **GitHub Repository**: [animatlabs/workflow-forge](https://github.com/animatlabs/workflow-forge)
- **Issues**: [GitHub Issues](https://github.com/animatlabs/workflow-forge/issues)
- **License**: MIT License
- **Contributing**: [Contributing Guidelines](../CONTRIBUTING.md)

---

## Document Index

### Essential Documentation
- [Getting Started](getting-started/getting-started.md) - Tutorial
- [Architecture](architecture/overview.md) - Design & principles
- [API Reference](reference/api-reference.md) - Complete API
- [Operations](core/operations.md) - Operation patterns
- [Events](core/events.md) - Event system
- [Configuration](core/configuration.md) - Settings
- [Extensions](extensions/index.md) - Extensions overview
- [Samples Guide](getting-started/samples-guide.md) - 33 examples

### Performance & Analysis
- [Performance](performance/performance.md) - Benchmark data
- [Competitive Analysis](performance/competitive-analysis.md) - Framework comparison

### Project Information
- [Root README](../README.md) - Project overview
- [Contributing](../CONTRIBUTING.md) - Contribution guidelines
- [License](../LICENSE) - MIT License

---

**WorkflowForge** - *Build workflows with industrial strength*
