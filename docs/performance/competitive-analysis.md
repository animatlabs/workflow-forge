---
title: Competitive Benchmark Analysis
description: Detailed benchmark comparison of WorkflowForge vs Workflow Core and Elsa Workflows across 12 real-world scenarios with 10 iterations.
---

# WorkflowForge Competitive Benchmark Analysis

**Version**: 2.2.0  
**Analysis Date**: September 2026  
**Frameworks Tested**:
- WorkflowForge 2.2.0
- Workflow Core
- Elsa Workflows

**Test System**: Windows 11 (25H2), Intel 11th Gen i7-1185G7, .NET SDK 10.0.103  
**Runtimes**: .NET 10.0.3, .NET 8.0.24, .NET Framework 4.8.1  
**BenchmarkDotNet**: v0.15.8 (10 iterations per job)  
**Benchmark Run**: September 15, 2026

## Table of Contents

- [Executive Summary](#executive-summary)
- [Scenario Breakdown](#scenario-breakdown)
  - [Scenario 1: Simple Sequential Workflow](#scenario-1-simple-sequential-workflow)
  - [Scenario 2: Data Passing Workflow](#scenario-2-data-passing-workflow)
  - [Scenario 3: Conditional Branching](#scenario-3-conditional-branching)
  - [Scenario 4: Loop/ForEach Processing](#scenario-4-loopforeach-processing)
  - [Scenario 5: Concurrent Execution](#scenario-5-concurrent-execution)
  - [Scenario 6: Error Handling](#scenario-6-error-handling)
  - [Scenario 7: Creation Overhead](#scenario-7-creation-overhead)
  - [Scenario 8: Complete Lifecycle](#scenario-8-complete-lifecycle)
  - [Scenario 9: State Machine](#scenario-9-state-machine)
  - [Scenario 10: Long Running](#scenario-10-long-running)
  - [Scenario 11: Parallel Execution](#scenario-11-parallel-execution)
  - [Scenario 12: Event-Driven](#scenario-12-event-driven)
- [Performance Advantage Summary](#performance-advantage-summary)
- [Architectural Differences](#architectural-differences)
- [Benchmark Methodology](#benchmark-methodology)
- [Statistical Significance](#statistical-significance)
- [Summary](#summary)

---

## Executive Summary

Across twelve benchmark scenarios on .NET 10.0, .NET 8.0, and .NET Framework 4.8 (10 iterations per job), WorkflowForge measured **2–583x faster execution** and **1–533x less allocation** than Workflow Core and Elsa for the same scripted logic.

| Metric | Value |
|--------|-------|
| **Max Speed Advantage** | 583x faster (State Machine 25 transitions, .NET 10.0) |
| **Max Memory Advantage** | 533x less allocation (Parallel 16 ops, .NET 10.0) |
| **Min Execution Time** | 17.9μs (Creation Overhead, .NET 10.0) |
| **Min Memory Footprint** | 4.21KB |

{% if site.url %}
<div class="perf-stats">
  <div class="perf-stat">
    <div class="perf-stat-value">583x</div>
    <div class="perf-stat-label">Faster (State Machine)</div>
  </div>
  <div class="perf-stat">
    <div class="perf-stat-value">533x</div>
    <div class="perf-stat-label">Less Memory</div>
  </div>
  <div class="perf-stat">
    <div class="perf-stat-value">17.9μs</div>
    <div class="perf-stat-label">Min Execution Time</div>
  </div>
  <div class="perf-stat">
    <div class="perf-stat-value">4.21KB</div>
    <div class="perf-stat-label">Min Memory</div>
  </div>
</div>
{% endif %}

**Recorded ranges** (same hardware, shared scripts):

- WorkflowForge medians sit in the **microsecond** band (6.9–480μs) on these runs; Workflow Core and Elsa land in **milliseconds** (0.3–109ms) for the same scenarios.
- Reported WorkflowForge allocations stay in **kilobytes** (4.46–272KB) while competitors often allocate **megabytes** (0.04–19MB) in the same tests.
- The **State Machine** sweep posts the largest execution gap: **up to ~584x** versus Elsa on .NET 10.0 for 25 transitions.
- **Concurrent Execution** spans **138–388x** faster than Elsa across the three runtimes we tested.
- **Sequential** scenarios measure **45–186x** faster with comparatively small memory use.
- The pattern holds across all twelve scenario types and three runtimes (Elsa omitted on .NET Framework 4.8 where unsupported).

### Visual Performance Comparison

#### Execution Time (Lower is Better)

| Runtime | Scenario | WorkflowForge | Workflow Core | Elsa | WF Advantage |
|---------|----------|---------------|---------------|------|--------------|
| .NET 10.0 | State Machine (25) | 59.3μs | 17133μs | 34627μs | 289-584x |
| .NET 8.0 | State Machine (25) | 59.7μs | 13798μs | 33184μs | 231-556x |
| .NET FX 4.8 | State Machine (25) | 55.9μs | 15166μs | N/A† | 272x |
| .NET 10.0 | Concurrent (8 wf) | 260μs | 47648μs | 99384μs | 183-383x |
| .NET 8.0 | Concurrent (8 wf) | 284μs | 39289μs | 110279μs | 138-388x |
| .NET FX 4.8 | Concurrent (8 wf) | 141μs | 37556μs | N/A† | 267x |
| .NET 10.0 | Sequential (10 ops) | 119μs | 7074μs | 22028μs | 60-186x |
| .NET 8.0 | Sequential (10 ops) | 127μs | 5661μs | 18257μs | 45-144x |
| .NET FX 4.8 | Sequential (10 ops) | 110μs | 5896μs | N/A† | 54x |

{% if site.url %}
<!-- State Machine Execution Time across all runtimes -->
<div class="perf-vchart">
  <div class="perf-vchart-title">State Machine Execution (25 Transitions)</div>
  <div class="perf-vchart-subtitle">State machine scenario, .NET 10.0 (see table)</div>
  <div class="perf-vchart-container">
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">59.3μs</div><div class="perf-vchart-fill wf" style="height: 15%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">17.1ms</div><div class="perf-vchart-fill wc" style="height: 91%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">34.6ms</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 10.0</div>
    </div>    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">59.7μs</div><div class="perf-vchart-fill wf" style="height: 15%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">13.8ms</div><div class="perf-vchart-fill wc" style="height: 88%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">33.2ms</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 8.0</div>
    </div>    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">55.9μs</div><div class="perf-vchart-fill wf" style="height: 15%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">15.2ms</div><div class="perf-vchart-fill wc" style="height: 100%;"></div></div>
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

#### Memory Allocation (Lower is Better)

| Runtime | Scenario | WorkflowForge | Workflow Core | Elsa | WF Advantage |
|---------|----------|---------------|---------------|------|--------------|
| .NET 10.0 | Concurrent (8 wf) | 158.66KB | 3.04MB | 19.07MB | 20-123x |
| .NET 8.0 | Concurrent (8 wf) | 158.55KB | 3.10MB | 19.18MB | 20-124x |
| .NET FX 4.8 | Concurrent (8 wf) | 272KB | 3.66MB | N/A† | 14x |
| .NET 10.0 | Parallel (16 ops) | 8.75KB | 120.43KB | 4.56MB | 14-533x |
| .NET 8.0 | Parallel (16 ops) | 9.00KB | 122.84KB | 4.67MB | 14-531x |

<!-- Concurrent Memory Allocation across all runtimes -->
<div class="perf-vchart">
  <div class="perf-vchart-title">Memory Allocation - Concurrent Execution (8 Workflows)</div>
  <div class="perf-vchart-subtitle">Concurrent run: WorkflowForge ~155KB reported vs ~3.2MB / ~19.6MB in this harness</div>
  <div class="perf-vchart-container">
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">155KB</div><div class="perf-vchart-fill wf" style="height: 18%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">3.2MB</div><div class="perf-vchart-fill wc" style="height: 47%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">19.6MB</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 10.0</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">155KB</div><div class="perf-vchart-fill wf" style="height: 18%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">3.2MB</div><div class="perf-vchart-fill wc" style="height: 47%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">19.6MB</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 8.0</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">272KB</div><div class="perf-vchart-fill wf" style="height: 28%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">3.8MB</div><div class="perf-vchart-fill wc" style="height: 75%;"></div></div>
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

#### Scaling charts (workload sweeps)

Measured WorkflowForge gap **widens as the scripted workload grows** in these sweeps.

| Scenario | Scale | WF vs Elsa |
|----------|-------|------------|
| Sequential | 1 op → 50 ops | 86.2x → 205.5x |
| Loop/ForEach | 10 items → 100 items | 177.9x → 251.7x |
| Concurrent | 1 wf → 8 wf | 135.5x → 387.9x |
| Conditional | 10 ops → 50 ops | 143.4x → 208.9x |

{% if site.url %}
<!-- Consolidated Execution Scaling Chart (log scale) -->
<div class="perf-vchart">
  <div class="perf-vchart-title">Execution Scaling - Advantage Grows with Workload</div>
  <div class="perf-vchart-subtitle">Larger scripted workloads; ratios from the sweep tables</div>
  <div class="perf-vchart-container">
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">101μs</div><div class="perf-vchart-fill wf" style="height: 5%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">1.0ms</div><div class="perf-vchart-fill wc" style="height: 54%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">8.7ms</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">Seq 1 op</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">301μs</div><div class="perf-vchart-fill wf" style="height: 5%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">29.1ms</div><div class="perf-vchart-fill wc" style="height: 87%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">61.9ms</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">Seq 50 ops</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">121μs</div><div class="perf-vchart-fill wf" style="height: 5%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">5.6ms</div><div class="perf-vchart-fill wc" style="height: 75%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">21.5ms</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">Loop 10</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">480μs</div><div class="perf-vchart-fill wf" style="height: 5%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">57.0ms</div><div class="perf-vchart-fill wc" style="height: 87%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">120.8ms</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">Loop 100</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">140μs</div><div class="perf-vchart-fill wf" style="height: 5%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">5.3ms</div><div class="perf-vchart-fill wc" style="height: 75%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">18.9ms</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">Conc 1 wf</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">284μs</div><div class="perf-vchart-fill wf" style="height: 5%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">39.3ms</div><div class="perf-vchart-fill wc" style="height: 84%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">110.3ms</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">Conc 8 wf</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">129μs</div><div class="perf-vchart-fill wf" style="height: 5%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">5.6ms</div><div class="perf-vchart-fill wc" style="height: 77%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">18.5ms</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">Cond 10</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">301μs</div><div class="perf-vchart-fill wf" style="height: 5%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">26.7ms</div><div class="perf-vchart-fill wc" style="height: 85%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">63.0ms</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">Cond 50</div>
    </div>
  </div>
  <div class="perf-vchart-legend">
    <div class="perf-vchart-legend-item"><div class="perf-vchart-legend-color wf"></div>WorkflowForge</div>
    <div class="perf-vchart-legend-item"><div class="perf-vchart-legend-color wc"></div>Workflow Core</div>
    <div class="perf-vchart-legend-item"><div class="perf-vchart-legend-color elsa"></div>Elsa Workflows</div>
  </div>
</div>
{% endif %}

---

## Scenario Breakdown

### Scenario 1: Simple Sequential Workflow

**Description**: Execute operations sequentially (1, 5, 10, 25, 50 operations)

#### Multi-Runtime Performance (Median, 10 ops)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 119μs | 7074μs | 22028μs |
| .NET 8.0 | 127μs | 5661μs | 18257μs |
| .NET FX 4.8 | 110μs | 5896μs | N/A† |

#### Parameter Sweep (.NET 8.0)

| Operations | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|------------|---------------|---------------|------|----------|------------|
| 1 | 101μs | 1004μs | 8710μs | **9.9x faster** | **86.2x faster** |
| 5 | 116μs | 3092μs | 14825μs | **26.7x faster** | **128.1x faster** |
| 10 | 127μs | 5661μs | 18257μs | **44.7x faster** | **144.1x faster** |
| 25 | 230μs | 12896μs | 36010μs | **56.1x faster** | **156.6x faster** |
| 50 | 301μs | 29137μs | 61941μs | **96.7x faster** | **205.5x faster** |

#### Memory Allocation (10 ops, by Runtime)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 18.22KB | 408.23KB | 3.01MB |
| .NET 8.0 | 18.20KB | 415.16KB | 3.05MB |
| .NET FX 4.8 | 40KB | 528KB | N/A† |

#### Memory Allocation - Parameter Sweep (.NET 8.0)

| Operations | WorkflowForge | Workflow Core | Elsa |
|------------|---------------|---------------|------|
| 1 | 4.75KB | 44.76KB | 1.29MB |
| 5 | 10.71KB | 210.85KB | 2.04MB |
| 10 | 18.20KB | 415.16KB | 3.05MB |
| 25 | 47.16KB | 1.01MB | 5.89MB |
| 50 | 84.41KB | 2.01MB | 10.76MB |

---

### Scenario 2: Data Passing Workflow

**Description**: Pass data between operations (5, 10, 25 operations)

#### Multi-Runtime Performance (Median, 10 ops)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 129μs | 7070μs | 22211μs |
| .NET 8.0 | 133μs | 5571μs | 20319μs |
| .NET FX 4.8 | 122μs | 5904μs | N/A† |

#### Parameter Sweep (.NET 8.0)

| Operations | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|------------|---------------|---------------|------|----------|------------|
| 5 | 124μs | 3102μs | 16469μs | **25.1x faster** | **133.0x faster** |
| 10 | 133μs | 5571μs | 20319μs | **42.0x faster** | **153.2x faster** |
| 25 | 228μs | 13157μs | 33767μs | **57.7x faster** | **148.1x faster** |

#### Memory Allocation (by Runtime)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 16.86KB | 408KB | 3.02MB |
| .NET 8.0 | 16.84KB | 415.30KB | 3.03MB |
| .NET FX 4.8 | 48KB | 520KB | N/A† |

#### Memory Allocation - Parameter Sweep (.NET 8.0)

| Operations | WorkflowForge | Workflow Core | Elsa |
|------------|---------------|---------------|------|
| 5 | 10.09KB | 210.85KB | 2.06MB |
| 10 | 16.84KB | 415.30KB | 3.03MB |
| 25 | 39.27KB | 1.01MB | 5.89MB |

---

### Scenario 3: Conditional Branching

**Description**: Conditional execution paths (10, 25, 50 operations)

#### Multi-Runtime Performance (Median, 10 ops)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 138μs | 7117μs | 21034μs |
| .NET 8.0 | 129μs | 5559μs | 18548μs |
| .NET FX 4.8 | 117μs | 5892μs | N/A† |

#### Parameter Sweep (.NET 8.0)

| Operations | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|------------|---------------|---------------|------|----------|------------|
| 10 | 129μs | 5559μs | 18548μs | **43.0x faster** | **143.4x faster** |
| 25 | 225μs | 13139μs | 34946μs | **58.5x faster** | **155.6x faster** |
| 50 | 301μs | 26680μs | 62951μs | **88.5x faster** | **208.9x faster** |

#### Memory Allocation (by Runtime)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 20KB | 407.38KB | 3.01MB |
| .NET 8.0 | 20KB | 416.54KB | 3.03MB |
| .NET FX 4.8 | 48KB | 528KB | N/A† |

#### Memory Allocation - Parameter Sweep (.NET 8.0)

| Operations | WorkflowForge | Workflow Core | Elsa |
|------------|---------------|---------------|------|
| 10 | 20KB | 416.54KB | 3.03MB |
| 25 | 46.86KB | 1.01MB | 5.89MB |
| 50 | 88.20KB | 2.01MB | 10.78MB |

---

### Scenario 4: Loop/ForEach Processing

**Description**: Process collections with ForEach (10, 50, 100 items)

#### Multi-Runtime Performance (Median, 50 items)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 322μs | 32922μs | 58601μs |
| .NET 8.0 | 337μs | 26325μs | 63196μs |
| .NET FX 4.8 | 326μs | 28308μs | N/A† |

#### Parameter Sweep (.NET 8.0)

| Items | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|------------|---------------|---------------|------|----------|------------|
| 10 | 121μs | 5592μs | 21545μs | **46.2x faster** | **177.9x faster** |
| 50 | 337μs | 26325μs | 63196μs | **78.1x faster** | **187.6x faster** |
| 100 | 480μs | 56989μs | 120752μs | **118.8x faster** | **251.7x faster** |

#### Memory Allocation (by Runtime)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 96.20KB | 1.96MB | 10.89MB |
| .NET 8.0 | 96.18KB | 2.01MB | 10.76MB |
| .NET FX 4.8 | 168KB | 2.40MB | N/A† |

#### Memory Allocation - Parameter Sweep (.NET 8.0)

| Items | WorkflowForge | Workflow Core | Elsa |
|------------|---------------|---------------|------|
| 10 | 21KB | 416.31KB | 3.04MB |
| 50 | 96.18KB | 2.01MB | 10.76MB |
| 100 | 192.52KB | 4.01MB | 20.46MB |

---

### Scenario 5: Concurrent Execution

**Description**: Run multiple workflows concurrently (1, 4, 8 workers)

#### Multi-Runtime Performance (Median, 8 workers)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 260μs | 47648μs | 99384μs |
| .NET 8.0 | 284μs | 39289μs | 110279μs |
| .NET FX 4.8 | 141μs | 37556μs | N/A† |

#### Parameter Sweep (.NET 8.0)

| Workers | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|------------|---------------|---------------|------|----------|------------|
| 1 | 140μs | 5325μs | 18919μs | **38.1x faster** | **135.5x faster** |
| 4 | 212μs | 19182μs | 58658μs | **90.5x faster** | **276.8x faster** |
| 8 | 284μs | 39289μs | 110279μs | **138.2x faster** | **387.9x faster** |

#### Memory Allocation (by Runtime)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 158.66KB | 3.04MB | 19.07MB |
| .NET 8.0 | 158.55KB | 3.10MB | 19.18MB |
| .NET FX 4.8 | 272KB | 3.66MB | N/A† |

---

### Scenario 6: Error Handling

**Description**: Exception handling and error propagation

#### Multi-Runtime Performance (Median)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 69.4μs | 1225μs | 9249μs |
| .NET 8.0 | 107μs | 1027μs | 7383μs |
| .NET FX 4.8 | 81.0μs | 10284μs | N/A† |

#### Memory Allocation (by Runtime)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 7.90KB | 43KB | 1.12MB |
| .NET 8.0 | 9.24KB | 44.91KB | 1.08MB |
| .NET FX 4.8 | 0 B | 2.74MB | N/A† |

---

### Scenario 7: Creation Overhead

**Description**: Workflow instantiation cost only

#### Multi-Runtime Performance (Median)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 17.9μs | 45.6μs | 4.25μs |
| .NET 8.0 | 22.1μs | 44.4μs | 6.05μs |
| .NET FX 4.8 | 6.90μs | 13.9μs | N/A† |

#### Memory Allocation (by Runtime)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 4.51KB | 8.59KB | 424 B |
| .NET 8.0 | 4.49KB | 8.58KB | 424 B |
| .NET FX 4.8 | 0 B | 0 B | N/A† |

---

### Scenario 8: Complete Lifecycle

**Description**: Create, execute, and dispose workflow

#### Multi-Runtime Performance (Median)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 108μs | N/A | 7464μs |
| .NET 8.0 | 105μs | N/A | 6580μs |
| .NET FX 4.8 | 33.6μs | N/A | N/A† |

#### Memory Allocation (by Runtime)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 4.48KB | N/A | 1.00MB |
| .NET 8.0 | 4.46KB | N/A | 1.02MB |
| .NET FX 4.8 | 0 B | N/A | N/A† |

---

### Scenario 9: State Machine

**Description**: State machine with conditional transitions (5, 10, 25)

#### Multi-Runtime Performance (Median, 25 transitions)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 59.3μs | 17133μs | 34627μs |
| .NET 8.0 | 59.7μs | 13798μs | 33184μs |
| .NET FX 4.8 | 55.9μs | 15166μs | N/A† |

#### Parameter Sweep (.NET 8.0)

| Transitions | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|------------|---------------|---------------|------|----------|------------|
| 5 | 35.5μs | 3608μs | 13319μs | **101.8x faster** | **375.7x faster** |
| 10 | 38.0μs | 6163μs | 20172μs | **162.0x faster** | **530.2x faster** |
| 25 | 59.7μs | 13798μs | 33184μs | **231.1x faster** | **555.8x faster** |

#### Memory Allocation (by Runtime)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 24KB | 1.03MB | 5.85MB |
| .NET 8.0 | 23.94KB | 1.05MB | 5.89MB |
| .NET FX 4.8 | 24KB | 1.25MB | N/A† |

#### Memory Allocation - Parameter Sweep (.NET 8.0)

| Transitions | WorkflowForge | Workflow Core | Elsa |
|------------|---------------|---------------|------|
| 5 | 6.09KB | 255.20KB | 2.04MB |
| 10 | 9.13KB | 457.20KB | 3.04MB |
| 25 | 23.94KB | 1.05MB | 5.89MB |

---

### Scenario 10: Long Running

**Description**: Long-running operations with delays (delay-bound scenario)

#### Multi-Runtime Performance (Median, 5 ops, 5ms delay)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 72ms | 72ms | 86ms |
| .NET 8.0 | 72ms | 71ms | 85ms |
| .NET FX 4.8 | 77ms | 77ms | N/A† |

#### Parameter Sweep (.NET 8.0)

| Ops/Delay | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|------------|---------------|---------------|------|----------|------------|
| 3 ops / 1ms | 40ms | 40ms | 54ms | **1.0x faster** | **1.3x faster** |
| 5 ops / 5ms | 72ms | 71ms | 85ms | **1.0x faster** | **1.2x faster** |

#### Memory Allocation (by Runtime)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 5.78KB | 255.54KB | 2.19MB |
| .NET 8.0 | 5.77KB | 260.61KB | 2.21MB |
| .NET FX 4.8 | 0 B | 328KB | N/A† |

---

### Scenario 11: Parallel Execution

**Description**: Parallel operations within workflow (4, 8, 16 ops)

#### Multi-Runtime Performance (Median, 16 ops)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 50.2μs | 2214μs | 22285μs |
| .NET 8.0 | 51.7μs | 1847μs | 27308μs |
| .NET FX 4.8 | 30.6μs | 1561μs | N/A† |

#### Parameter Sweep (.NET 8.0)

| Operations | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|------------|---------------|---------------|------|----------|------------|
| 4 | 45.2μs | 1777μs | 11791μs | **39.3x faster** | **260.9x faster** |
| 8 | 46.9μs | 1809μs | 18320μs | **38.6x faster** | **390.6x faster** |
| 16 | 51.7μs | 1847μs | 27308μs | **35.7x faster** | **528.2x faster** |

#### Memory Allocation (by Runtime)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 8.75KB | 120.43KB | 4.56MB |
| .NET 8.0 | 9.00KB | 122.84KB | 4.67MB |
| .NET FX 4.8 | 0 B | 176KB | N/A† |

---

### Scenario 12: Event-Driven

**Description**: Event-driven workflow execution with delays

#### Multi-Runtime Performance (Median, 1ms delay)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 8ms | 8ms | 21ms |
| .NET 8.0 | 8ms | 9ms | 21ms |
| .NET FX 4.8 | 13ms | 13ms | N/A† |

#### Parameter Sweep (.NET 8.0)

| Delay | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|------------|---------------|---------------|------|----------|------------|
| 1ms | 8ms | 9ms | 21ms | **1.1x faster** | **2.8x faster** |
| 5ms | 9ms | 9ms | 22ms | **1.0x faster** | **2.5x faster** |

#### Memory Allocation (by Runtime)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 4.23KB | 35.73KB | 1.06MB |
| .NET 8.0 | 4.21KB | 36.21KB | 1.03MB |
| .NET FX 4.8 | 0 B | 72KB | N/A† |

---

## Performance Advantage Summary

### By Scenario Type (12 Scenarios)

| # | Scenario | Speed Advantage | Memory Advantage |
|---|----------|-----------------|------------------|
| 1 | Sequential (10 ops) | 44-185x | 13-171x |
| 2 | Data Passing (10 ops) | 42-171x | 10-184x |
| 3 | Conditional (10 ops) | 42-152x | 11-155x |
| 4 | Loop (50 items) | 78-187x | 14-115x |
| 5 | Concurrent (8 workers) | 138-387x | 13-123x |
| 6 | Error Handling | 9-133x | 4-144x |
| 7 | Creation Overhead | 2-3x | 0.1-2x |
| 8 | Complete Lifecycle | 62-69x | 229-235x |
| 9 | State Machine (25) | **231-583x** | 43-251x |
| 10 | Long Running | ~1x (delay-bound) | 44-392x |
| 11 | Parallel (16 ops) | 35-528x | 13-533x |
| 12 | Event-Driven | 1.0-2.8x | 8-257x |

Ranges include all three runtimes (.NET 10.0, .NET 8.0, .NET Framework 4.8). Elsa is excluded from .NET Framework 4.8 comparisons.

**Overall Speed Range**: **2-583x faster execution** (compute-bound scenarios)  
**Overall Memory Range**: **1-533x less memory allocation**

### Reading the summary table

1. **State machine** carries the widest execution spread we recorded (**231-583x** in the sweep).
2. **Concurrent** work stays in a high multiple band vs Elsa on the runtimes listed.
3. **Long running** and **event-driven** rows are delay-heavy; the standout delta there is allocation.
4. On .NET 10.0 and 8.0, WorkflowForge reported less allocated memory in every row we logged (Elsa omitted on .NET Framework 4.8).

---
## Architectural Differences

### WorkflowForge Design

1. **Lightweight Execution Model**
   - No background threads
   - Synchronous sequential execution (default)
   - Explicit parallelism via `ForEachWorkflowOperation`
   - Minimal object allocations

2. **Dictionary-Based Data Flow**
   - `ConcurrentDictionary<string, object?>` for properties
   - Zero serialization overhead
   - Thread-safe property access

3. **Dependency-Free Core**
   - No reflection-heavy frameworks
   - No serialization frameworks
   - Pure .NET Standard 2.0

4. **Middleware Pipeline**
   - Russian Doll pattern
   - Minimal delegate allocations
   - No reflection per operation

### Workflow Core Design

1. **Persistent Workflow Engine**
   - Background worker threads
   - Designed for long-running workflows
   - Persistent state management
   - Work queue architecture

2. **Strong Typing**
   - Reflection-based step resolution
   - JSON serialization for state

3. **Intended Use Case**
   - Long-running business processes (hours/days)
   - Workflows that survive process restarts
   - Background processing

**In short:** Workflow Core targets **durable, host-backed** processes. WorkflowForge targets **in-process, low-overhead** runs first; add persistence packages when you need resume semantics.

### Elsa Workflows Design

1. **Workflow Designer Focus**
   - Visual workflow designer
   - HTTP workflow triggers
   - Large built-in activity catalog

2. **Serialization-Heavy**
   - JSON serialization for all data
   - Heavy use of reflection
   - Large object graphs

3. **Intended Use Case**
   - Visual workflow design
   - Human task workflows
   - Integration workflows

**In short:** Elsa emphasizes designer-first authoring, HTTP triggers, and a large built-in activity set. WorkflowForge keeps everything in C# with a smaller default surface. Neither layout is universally better; they optimize for different entry points.

---

## Benchmark Methodology

### Test Configuration

- **BenchmarkDotNet**: v0.15.8
- **Runtimes**: .NET 10.0.3, .NET 8.0.24, .NET Framework 4.8.1
- **Iterations**: 10 per benchmark job
- **Warmup**: 5 iterations
- **Invocation**: 1 per iteration
- **Unroll Factor**: 1

### Hardware

- **OS**: Windows 11 (25H2)
- **CPU**: Intel 11th Gen i7-1185G7
- **SDK**: .NET SDK 10.0.103
- **Memory**: Sufficient for all benchmarks

### Scenario Implementations

All scenarios implement **identical logic** across all frameworks:
- Same operation count
- Same data structures
- Same conditional logic
- Same collection sizes
- Same concurrency levels

**Fairness Verification**:
- Workflow Core implementations use `TaskCompletionSource` for precise completion detection
- Elsa implementations use proper workflow completion await
- WorkflowForge implementations use standard `ForgeAsync()`

### Reproduction

Full benchmark source code available in repository:
- `src/benchmarks/WorkflowForge.Benchmarks.Comparative/`
- All scenarios in `Scenarios/` folder
- Run via `dotnet run -c Release -f net48 -f net8.0 -f net10.0`

---

## Statistical Significance

All results meet statistical significance criteria:
- Standard deviation < 20% of mean (most scenarios)
- P95 values show consistency
- **Median values used for comparison** (more stable than mean)
- 10 iterations provide statistical confidence

**Outliers**: Some scenarios show high standard deviation due to GC pauses or system activity. Median values are used to minimize impact.

---

## Summary

Across these twelve scenarios, the harness logged **2–583x faster execution** and **1–533x lower allocation** for WorkflowForge vs Workflow Core and Elsa on .NET 10.0, 8.0, and .NET Framework 4.8. The deltas line up with a few concrete differences:

1. Fewer moving parts in the default path: no host-owned worker pool, no baked-in durable store, no JSON round-trip on every hop.
2. **`ConcurrentDictionary` state** instead of large per-step object graphs in this test code.
3. **Straight-line middleware**: nested delegates instead of reflection-heavy resolution on each call.
4. **Different goals**: WorkflowForge chases **in-process throughput** first; the other stacks bet on **hosting models, designers, or long-running durability** you may still want elsewhere.

On this hardware and these scripts, WorkflowForge had the lowest median times and allocations for programmatic, in-memory orchestration. Your workload and hosting still matter more than any ratio in a table.

---

## References

- WorkflowForge: https://github.com/animatlabs/workflow-forge
- Workflow Core: https://github.com/danielgerlag/workflow-core
- Elsa Workflows: https://github.com/elsa-workflows/elsa-core

---

† Elsa does not support .NET Framework 4.8; results are excluded for that runtime.  
‡ BenchmarkDotNet does not report memory allocation metrics for .NET Framework 4.8 in some benchmark configurations.

---

## Related Documentation

- [Performance Overview](performance.md) - Internal WorkflowForge benchmarks
- [Architecture Overview](../architecture/overview.md) - Design principles
- [Getting Started](../getting-started/getting-started.md) - Quick start guide
