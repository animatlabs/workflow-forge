---
layout: default
title: WorkflowForge Documentation
description: Build .NET workflows with a zero-dependency core, microsecond-scale benchmarks in our harness, and optional extension packages.
---

<div class="wf-hero">
  <img src="https://raw.githubusercontent.com/animatlabs/workflow-forge/main/icon.png" alt="WorkflowForge logo">
  <div>
    <h1>WorkflowForge Documentation</h1>
    <p>Build .NET workflows with clear guidance, focused examples, and a zero-dependency core.</p>
    <p class="wf-badges">
      <a href="https://github.com/animatlabs/workflow-forge/actions/workflows/build-test.yml"><img src="https://github.com/animatlabs/workflow-forge/actions/workflows/build-test.yml/badge.svg?branch=main" alt="Build and Test"></a>
      <a href="https://www.nuget.org/packages/WorkflowForge/"><img src="https://img.shields.io/nuget/v/WorkflowForge.svg" alt="NuGet"></a>
      <a href="https://www.nuget.org/packages/WorkflowForge/"><img src="https://img.shields.io/nuget/dt/WorkflowForge.svg" alt="NuGet Downloads"></a>
      <a href="https://github.com/animatlabs/workflow-forge/blob/main/LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="License"></a>
      <a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=alert_status" alt="Quality Gate Status"></a>
      <a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=coverage" alt="Coverage"></a>
      <a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=reliability_rating" alt="Reliability Rating"></a>
      <a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=security_rating" alt="Security Rating"></a>
      <a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=sqale_rating" alt="Maintainability Rating"></a>
      <a href="https://ko-fi.com/animat089"><img src="https://img.shields.io/badge/Ko--fi-support-ff5e5b?logo=ko-fi" alt="Ko-fi"></a>
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

WorkflowForge is a **zero-dependency workflow orchestration framework** for .NET. Twelve BenchmarkDotNet scenarios against Workflow Core and Elsa (same scripted logic) show **microsecond-level** medians and **small heaps** on our harness. **Forge**, **foundry**, and **smith** name the pieces so composition and tests read the same everywhere.

### Key Features

- **13x–511x** faster than those libraries on the scenarios we measured (runtime and scenario matter).
- **About 6x–575x** less memory in the same runs, case by case.
- **Zero package dependencies** on the core assembly; extensions are optional NuGet add-ons.
- **Compensation hooks** on every operation (saga-style rollback when you implement `RestoreAsync`).
- **Thirteen packages** total: one core, eleven extensions, plus `WorkflowForge.Testing`, with ILRepack isolating third-party bits.
- **Fluent builders**, the industrial metaphor, and **33 samples** that ramp from hello world to persistence and recovery.

---

## The Industrial Metaphor

WorkflowForge names pieces with a simple industrial metaphor:

- **The Forge** (`WorkflowForge` static class) is the entry point for building workflows.
- **Foundries** (`IWorkflowFoundry`) hold mutable state and services while operations run.
- **Smiths** (`IWorkflowSmith`) drive execution order, compensation, and lifecycle events.
- **Operations** (`IWorkflowOperation`) are the individual units of work in the graph.

Read it literally: *data (raw material) moves through operations (tools) inside a foundry (workspace), while the smith (orchestrator) runs the sequence*.

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

Override `RestoreAsync` when you need compensation behavior. The base class default is a no-op, and WorkflowForge skips those operations during compensation.

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
    <div class="perf-stat-value">511x</div>
    <div class="perf-stat-label">Faster (State Machine)</div>
  </div>
  <div class="perf-stat">
    <div class="perf-stat-value">575x</div>
    <div class="perf-stat-label">Less Memory</div>
  </div>
  <div class="perf-stat">
    <div class="perf-stat-value">8.75μs</div>
    <div class="perf-stat-label">Min Execution</div>
  </div>
  <div class="perf-stat">
    <div class="perf-stat-value">3.4KB</div>
    <div class="perf-stat-label">Min Memory</div>
  </div>
</div>
{% endif %}

### Benchmark Comparison

| Runtime | Scenario | vs WorkflowCore | vs Elsa | Memory Advantage |
|---------|----------|-----------------|---------|------------------|
| .NET 10.0 | **State Machine** | 455x faster | 511x faster | 46-249x less |
| .NET 8.0 | **State Machine** | 305x faster | 485x faster | 46-248x less |
| .NET FX 4.8 | **State Machine** | 303x faster | N/A† | 57x less |
| .NET 10.0 | **Concurrent (8 wf)** | 139x faster | 251x faster | 22-134x less |
| .NET 8.0 | **Concurrent (8 wf)** | 118x faster | 288x faster | 23-134x less |
| .NET FX 4.8 | **Concurrent (8 wf)** | 251x faster | N/A† | 15x less |

† Elsa does not support .NET Framework 4.8. See [Competitive Analysis](performance/competitive-analysis.md) for all 12 scenarios.

{% if site.url %}
<div class="perf-vchart">
  <div class="perf-vchart-title">State Machine Execution (25 Transitions)</div>
  <div class="perf-vchart-subtitle">State machine scenario, .NET 10.0 median (see competitive analysis for caveats)</div>
  <div class="perf-vchart-container">
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">65μs</div><div class="perf-vchart-fill wf" style="height: 37%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">29.5ms</div><div class="perf-vchart-fill wc" style="height: 89%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">33.1ms</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 10.0</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">71μs</div><div class="perf-vchart-fill wf" style="height: 37%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">21.7ms</div><div class="perf-vchart-fill wc" style="height: 63%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">34.4ms</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 8.0</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">61μs</div><div class="perf-vchart-fill wf" style="height: 37%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">18.5ms</div><div class="perf-vchart-fill wc" style="height: 100%;"></div></div>
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
- **Ko-fi**: If this project helps you, consider [supporting on Ko-fi](https://ko-fi.com/animat089)
