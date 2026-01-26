---
layout: default
title: WorkflowForge Documentation
---

<div class="wf-hero">
  <img src="https://raw.githubusercontent.com/animatlabs/workflow-forge/main/icon.png" alt="WorkflowForge logo">
  <div>
    <h1>WorkflowForge Documentation</h1>
    <p>Build high-performance workflows in .NET with clear guidance, focused examples, and a zero-dependency core.</p>
    <p class="wf-badges">
      <a href="https://www.nuget.org/packages/WorkflowForge/"><img src="https://img.shields.io/nuget/v/WorkflowForge.svg" alt="NuGet"></a>
      <a href="https://www.nuget.org/packages/WorkflowForge/"><img src="https://img.shields.io/nuget/dt/WorkflowForge.svg" alt="NuGet Downloads"></a>
      <a href="https://github.com/animatlabs/workflow-forge/blob/main/LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="License"></a>
    </p>
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

---

## What is WorkflowForge?

WorkflowForge is a **zero-dependency workflow orchestration framework** for .NET with **microsecond-level performance** and **minimal memory footprint**. It provides a clean, industrial metaphor for building workflows that are fast, maintainable, and production-ready.

### Key Features

- **High Performance**: 11-540x faster than alternatives in benchmarks
- **Minimal Memory**: 9-573x less memory usage
- **Zero Dependencies**: Core package with no external dependencies
- **Production Ready**: Built-in compensation (saga pattern), comprehensive testing
- **Extension Ecosystem**: 11 packages (10 extensions + Testing) with zero version conflicts
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

## Quick Start

```bash
dotnet add package WorkflowForge
```

```csharp
using WorkflowForge;

var workflow = WorkflowForge.CreateWorkflow("HelloWorld")
    .AddOperation("SayHello", async (foundry, ct) => {
        foundry.Logger.LogInformation("Hello, WorkflowForge!");
    })
    .Build();

using var smith = WorkflowForge.CreateSmith();
await smith.ForgeAsync(workflow);
```

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

---

## Extensions Ecosystem

WorkflowForge provides **11 packages** (10 extensions + 1 testing utility):

| Package | Purpose |
|---------|---------|
| **Testing** | Unit testing utilities with `FakeWorkflowFoundry` |
| **Serilog Logging** | Structured logging integration |
| **Resilience** | Core retry abstractions |
| **Polly Resilience** | Circuit breakers, retries, timeout policies |
| **Validation** | Input validation with DataAnnotations |
| **Audit Logging** | Compliance & audit trails |
| **Persistence** | Workflow state storage |
| **Persistence Recovery** | Resume interrupted workflows |
| **Performance Monitoring** | Metrics & profiling |
| **Health Checks** | Application health monitoring |
| **OpenTelemetry** | Distributed tracing |

**Dependency Isolation**: Extensions internalize dependencies with ILRepack while keeping Microsoft/System packages external.

---

## Performance Highlights

Based on BenchmarkDotNet testing (12 scenarios, 50 iterations) against Workflow Core and Elsa Workflows:

| Scenario | vs WorkflowCore | vs Elsa | Memory Advantage |
|----------|-----------------|---------|------------------|
| **Simple Sequential** | 26-71x faster | 48-117x faster | 26-183x less |
| **State Machine** | 126-303x faster | 307-540x faster | 47-284x less |
| **Conditional Branching** | 32-68x faster | 80-132x faster | 21-149x less |
| **Concurrent Execution** | 26-109x faster | 71-264x faster | 27-158x less |

**Key Metrics**:
- Minimal memory footprint: **3.49 KB** baseline
- Custom operation execution: **~13 μs** median
- State machine workflows: **303-540x faster** than competitors

**Test System**: Windows 11 (25H2), .NET 8.0.23, BenchmarkDotNet v0.15.8, 50 iterations

---

## Support & Community

- **GitHub Repository**: [animatlabs/workflow-forge](https://github.com/animatlabs/workflow-forge)
- **Issues**: [GitHub Issues](https://github.com/animatlabs/workflow-forge/issues)
- **License**: MIT License
- **Contributing**: [Contributing Guidelines](https://github.com/animatlabs/workflow-forge/blob/main/CONTRIBUTING.md)
