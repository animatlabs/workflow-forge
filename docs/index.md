---
layout: default
title: WorkflowForge Documentation
description: Build high-performance workflows in .NET with zero dependencies, microsecond execution, and a comprehensive extension ecosystem.
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

- **High Performance**: 13-522x faster than alternatives in benchmarks
- **Minimal Memory**: 6-578x less memory usage
- **Zero Dependencies**: Core package with no external dependencies
- **Production Ready**: Built-in compensation (saga pattern), comprehensive testing
- **Extension Ecosystem**: 13 packages (11 extensions + Testing) with zero version conflicts
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
    
    Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default);
    Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default);
}
```

Override `RestoreAsync` in your operation to support compensation. The base class provides a no-op default — operations that don't override it are safely skipped during compensation.

---

## Extensions Ecosystem

WorkflowForge provides **13 packages** (1 core + 11 extensions + 1 testing utility):

| Package | Purpose |
|---------|---------|
| **Testing** | Unit testing utilities with `FakeWorkflowFoundry` |
| **DependencyInjection** | Microsoft.Extensions.DependencyInjection integration |
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

{% if site.url %}
<div class="perf-stats">
  <div class="perf-stat">
    <div class="perf-stat-value">522x</div>
    <div class="perf-stat-label">Faster (State Machine)</div>
  </div>
  <div class="perf-stat">
    <div class="perf-stat-value">578x</div>
    <div class="perf-stat-label">Less Memory</div>
  </div>
  <div class="perf-stat">
    <div class="perf-stat-value">14μs</div>
    <div class="perf-stat-label">Min Execution</div>
  </div>
  <div class="perf-stat">
    <div class="perf-stat-value">3.5KB</div>
    <div class="perf-stat-label">Min Memory</div>
  </div>
</div>
{% endif %}

### Benchmark Comparison

| Runtime | Scenario | vs WorkflowCore | vs Elsa | Memory Advantage |
|---------|----------|-----------------|---------|------------------|
| .NET 10.0 | **State Machine** | 508x faster | 522x faster | 46-249x less |
| .NET 8.0 | **State Machine** | 356x faster | 412x faster | 46-248x less |
| .NET FX 4.8 | **State Machine** | 256x faster | N/A† | 57x less |
| .NET 10.0 | **Concurrent (8 wf)** | 139x faster | 263x faster | 22-134x less |
| .NET 8.0 | **Concurrent (8 wf)** | 123x faster | 285x faster | 23-134x less |
| .NET FX 4.8 | **Concurrent (8 wf)** | 285x faster | N/A† | 15x less |

† Elsa does not support .NET Framework 4.8. See [Competitive Analysis](performance/competitive-analysis.md) for all 12 scenarios.

{% if site.url %}
<div class="perf-vchart">
  <div class="perf-vchart-title">State Machine Execution (25 Transitions)</div>
  <div class="perf-vchart-subtitle">Up to 522x faster than alternatives (.NET 10.0)</div>
  <div class="perf-vchart-container">
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">83μs</div><div class="perf-vchart-fill wf" style="height: 37%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">42.2ms</div><div class="perf-vchart-fill wc" style="height: 95%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">43.3ms</div><div class="perf-vchart-fill elsa" style="height: 97%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 10.0</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">111μs</div><div class="perf-vchart-fill wf" style="height: 40%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">39.5ms</div><div class="perf-vchart-fill wc" style="height: 92%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">45.7ms</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 8.0</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">101μs</div><div class="perf-vchart-fill wf" style="height: 39%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">25.9ms</div><div class="perf-vchart-fill wc" style="height: 87%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET FX 4.8</div>
    </div>
  </div>
  <div class="perf-vchart-legend">
    <div class="perf-vchart-legend-item"><div class="perf-vchart-legend-color wf"></div>WorkflowForge</div>
    <div class="perf-vchart-legend-item"><div class="perf-vchart-legend-color wc"></div>Workflow Core</div>
    <div class="perf-vchart-legend-item"><div class="perf-vchart-legend-color elsa"></div>Elsa Workflows</div>
  </div>
</div>
{% endif %}

**Test System**: Windows 11 (25H2), .NET 8.0.24 / .NET 10.0.3 / .NET FX 4.8.1, BenchmarkDotNet v0.15.8, 50 iterations

---

## Support & Community

- **GitHub Repository**: [animatlabs/workflow-forge](https://github.com/animatlabs/workflow-forge)
- **Issues**: [GitHub Issues](https://github.com/animatlabs/workflow-forge/issues)
- **License**: MIT License
- **Contributing**: [Contributing Guidelines](https://github.com/animatlabs/workflow-forge/blob/main/CONTRIBUTING.md)
