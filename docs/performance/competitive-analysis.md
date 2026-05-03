---
title: Competitive Benchmark Analysis
description: Detailed benchmark comparison of WorkflowForge vs Workflow Core and Elsa Workflows across 12 real-world scenarios with 50 iterations.
---

# WorkflowForge Competitive Benchmark Analysis

**Version**: 2.1.1  
**Analysis Date**: March 2026  
**Frameworks Tested**:
- WorkflowForge 2.1.1
- Workflow Core
- Elsa Workflows

**Test System**: Windows 11 (25H2), Intel 11th Gen i7-1185G7, .NET SDK 10.0.103  
**Runtimes**: .NET 10.0.3, .NET 8.0.24, .NET Framework 4.8.1  
**BenchmarkDotNet**: v0.15.8 (50 iterations, 5 warmup)  
**Benchmark Run**: March 7, 2026

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

Across twelve benchmark scenarios on .NET 10.0, .NET 8.0, and .NET Framework 4.8 (50 iterations each), WorkflowForge measured **13–511x faster execution** and **6–575x less allocation** than Workflow Core and Elsa for the same scripted logic.

| Metric | Value |
|--------|-------|
| **Max Speed Advantage** | 511x faster (State Machine 25 transitions, .NET 10.0) |
| **Max Memory Advantage** | 575x less allocation (Parallel 16 ops, .NET 10.0) |
| **Min Execution Time** | 11μs (Creation Overhead, .NET 10.0) |
| **Min Memory Footprint** | 3.6KB |

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
    <div class="perf-stat-value">11μs</div>
    <div class="perf-stat-label">Min Execution Time</div>
  </div>
  <div class="perf-stat">
    <div class="perf-stat-value">3.6KB</div>
    <div class="perf-stat-label">Min Memory</div>
  </div>
</div>
{% endif %}

**Recorded ranges** (same hardware, shared scripts):

- WorkflowForge medians sit in the **microsecond** band (11–706μs) on these runs; Workflow Core and Elsa land in **milliseconds** (0.3–109ms) for the same scenarios.
- Reported WorkflowForge allocations stay in **kilobytes** (3.5–256KB) while competitors often allocate **megabytes** (0.04–19MB) in the same tests.
- The **State Machine** sweep posts the largest execution gap: **up to ~511x** versus Elsa on .NET 10.0 for 25 transitions.
- **Concurrent Execution** spans **118–288x** faster than Elsa across the three runtimes we tested.
- **Sequential** scenarios measure **26–55x** faster with comparatively small memory use.
- The pattern holds across all twelve scenario types and three runtimes (Elsa omitted on .NET Framework 4.8 where unsupported).

### Visual Performance Comparison

#### Execution Time (Lower is Better)

| Runtime | Scenario | WorkflowForge | Workflow Core | Elsa | WF Advantage |
|---------|----------|---------------|---------------|------|--------------|
| .NET 10.0 | State Machine (25) | 65μs | 29,537μs | 33,062μs | 455-511x |
| .NET 8.0 | State Machine (25) | 71μs | 21,683μs | 34,426μs | 305-485x |
| .NET FX 4.8 | State Machine (25) | 61μs | 18,486μs | N/A† | 303x |
| .NET 10.0 | Concurrent (8 wf) | 372μs | 47,114μs | 87,491μs | 127-235x |
| .NET 8.0 | Concurrent (8 wf) | 357μs | 42,054μs | 103,024μs | 118-288x |
| .NET FX 4.8 | Concurrent (8 wf) | 167μs | 41,934μs | N/A† | 250x |
| .NET 10.0 | Sequential (10 ops) | 422μs | 13,828μs | 18,676μs | 33-44x |
| .NET 8.0 | Sequential (10 ops) | 377μs | 9,879μs | 19,168μs | 26-51x |
| .NET FX 4.8 | Sequential (10 ops) | 122μs | 6,743μs | N/A† | 55x |

{% if site.url %}
<!-- State Machine Execution Time across all runtimes -->
<div class="perf-vchart">
  <div class="perf-vchart-title">State Machine Execution (25 Transitions)</div>
  <div class="perf-vchart-subtitle">State machine scenario, .NET 10.0 (see table)</div>
  <div class="perf-vchart-container">
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">65μs</div><div class="perf-vchart-fill wf" style="height: 20%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">29.5ms</div><div class="perf-vchart-fill wc" style="height: 89%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">33.1ms</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 10.0</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">71μs</div><div class="perf-vchart-fill wf" style="height: 21%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">21.7ms</div><div class="perf-vchart-fill wc" style="height: 63%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">34.4ms</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 8.0</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">61μs</div><div class="perf-vchart-fill wf" style="height: 33%;"></div></div>
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

#### Memory Allocation (Lower is Better)

| Runtime | Scenario | WorkflowForge | Workflow Core | Elsa | WF Advantage |
|---------|----------|---------------|---------------|------|--------------|
| .NET 10.0 | Concurrent (8 wf) | 155 KB | 3,247 KB | 19,568 KB | 21-126x |
| .NET 8.0 | Concurrent (8 wf) | 155 KB | 3,308 KB | 19,572 KB | 21-126x |
| .NET FX 4.8 | Concurrent (8 wf) | 272 KB | 3,816 KB | N/A† | 14x |
| .NET 10.0 | Parallel (16 ops) | 8.0 KB | 126 KB | 4,576 KB | 16-575x |
| .NET 8.0 | Parallel (16 ops) | 8.2 KB | 125 KB | 4,651 KB | 15-567x |

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
| Sequential | 1 op → 50 ops | 35.8x → 95.4x |
| Loop/ForEach | 10 items → 100 items | 71.8x → 156.0x |
| Concurrent | 1 wf → 8 wf | 74.2x → 288.3x |
| Conditional | 10 ops → 50 ops | 64.3x → 109.6x |

{% if site.url %}
<!-- Consolidated Execution Scaling Chart (log scale) -->
<div class="perf-vchart">
  <div class="perf-vchart-title">Execution Scaling - Advantage Grows with Workload</div>
  <div class="perf-vchart-subtitle">Larger scripted workloads; ratios from the sweep tables</div>
  <div class="perf-vchart-container">
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">257μs</div><div class="perf-vchart-fill wf" style="height: 5%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">1.9ms</div><div class="perf-vchart-fill wc" style="height: 18%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">9.2ms</div><div class="perf-vchart-fill elsa" style="height: 89%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">Seq 1 op</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">615μs</div><div class="perf-vchart-fill wf" style="height: 6%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">31.8ms</div><div class="perf-vchart-fill wc" style="height: 31%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">58.6ms</div><div class="perf-vchart-fill elsa" style="height: 57%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">Seq 50 ops</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">270μs</div><div class="perf-vchart-fill wf" style="height: 5%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">7.2ms</div><div class="perf-vchart-fill wc" style="height: 7%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">19.4ms</div><div class="perf-vchart-fill elsa" style="height: 19%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">Loop 10</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">660μs</div><div class="perf-vchart-fill wf" style="height: 6%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">60.2ms</div><div class="perf-vchart-fill wc" style="height: 58%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">103ms</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">Loop 100</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">260μs</div><div class="perf-vchart-fill wf" style="height: 5%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">7.6ms</div><div class="perf-vchart-fill wc" style="height: 7%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">19.3ms</div><div class="perf-vchart-fill elsa" style="height: 19%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">Conc 1 wf</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">357μs</div><div class="perf-vchart-fill wf" style="height: 5%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">42.1ms</div><div class="perf-vchart-fill wc" style="height: 41%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">103ms</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">Conc 8 wf</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">301μs</div><div class="perf-vchart-fill wf" style="height: 5%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">9.2ms</div><div class="perf-vchart-fill wc" style="height: 9%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">19.4ms</div><div class="perf-vchart-fill elsa" style="height: 19%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">Cond 10</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">526μs</div><div class="perf-vchart-fill wf" style="height: 5%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">32.1ms</div><div class="perf-vchart-fill wc" style="height: 31%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">57.7ms</div><div class="perf-vchart-fill elsa" style="height: 56%;"></div></div>
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
| .NET 10.0 | 422μs | 13,828μs | 18,676μs |
| .NET 8.0 | 377μs | 9,879μs | 19,168μs |
| .NET FX 4.8 | 122μs | 6,743μs | N/A† |

#### Parameter Sweep (.NET 8.0)

| Operations | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|------------|---------------|---------------|------|----------|------------|
| 1 | 257μs | 1,878μs | 9,175μs | **7.3x faster** | **35.8x faster** |
| 5 | 319μs | 6,078μs | 14,168μs | **19.0x faster** | **44.4x faster** |
| 10 | 377μs | 9,879μs | 19,168μs | **26.2x faster** | **50.9x faster** |
| 25 | 462μs | 27,075μs | 34,395μs | **58.6x faster** | **74.5x faster** |
| 50 | 615μs | 31,768μs | 58,648μs | **51.7x faster** | **95.4x faster** |

#### Memory Allocation (10 ops, by Runtime)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 17.72KB | 427KB | 3,024KB |
| .NET 8.0 | 17.72KB | 429KB | 2,992KB |
| .NET FX 4.8 | 40.00KB | 560KB | N/A† |

#### Memory Allocation - Parameter Sweep (.NET 8.0)

| Operations | WorkflowForge | Workflow Core | Elsa |
|------------|---------------|---------------|------|
| 1 | 3.98KB | 46KB | 1,254KB |
| 5 | 10.07KB | 218KB | 2,018KB |
| 10 | 17.72KB | 429KB | 2,992KB |
| 25 | 48.93KB | 1,064KB | 5,956KB |
| 50 | 83.86KB | 2,126KB | 10,879KB |

**Numbers**: the execution gap versus Workflow Core and Elsa **widens as the operation count rises** in the sequential sweep.

---

### Scenario 2: Data Passing Workflow

**Description**: Pass data between operations (5, 10, 25 operations)

#### Multi-Runtime Performance (Median, 10 ops)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 325μs | 11,651μs | 18,510μs |
| .NET 8.0 | 321μs | 9,751μs | 19,164μs |
| .NET FX 4.8 | 118μs | 6,684μs | N/A† |

#### Parameter Sweep (.NET 8.0)

| Operations | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|------------|---------------|---------------|------|----------|------------|
| 5 | 299μs | 5,063μs | 14,052μs | **16.9x faster** | **47.0x faster** |
| 10 | 321μs | 9,751μs | 19,164μs | **30.4x faster** | **59.7x faster** |
| 25 | 483μs | 17,318μs | 33,825μs | **35.9x faster** | **70.0x faster** |

#### Memory Allocation (10 ops, by Runtime)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 16.36KB | 425KB | 3,024KB |
| .NET 8.0 | 16.36KB | 429KB | 2,988KB |
| .NET FX 4.8 | 40.00KB | 544KB | N/A† |

#### Memory Allocation - Parameter Sweep (.NET 8.0)

| Operations | WorkflowForge | Workflow Core | Elsa |
|------------|---------------|---------------|------|
| 5 | 9.45KB | 216KB | 2,018KB |
| 10 | 16.36KB | 429KB | 2,988KB |
| 25 | 39.26KB | 1,063KB | 5,956KB |

**What matters**: extra data-passing steps add **under about a microsecond per operation** in WorkflowForge for this scenario.

---

### Scenario 3: Conditional Branching

**Description**: Conditional logic with if/else branches (10, 25, 50 operations)

#### Multi-Runtime Performance (Median, 10 ops)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 333μs | 13,427μs | 19,166μs |
| .NET 8.0 | 301μs | 9,248μs | 19,361μs |
| .NET FX 4.8 | 118μs | 6,562μs | N/A† |

#### Parameter Sweep (.NET 8.0)

| Operations | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|------------|---------------|---------------|------|----------|------------|
| 10 | 301μs | 9,248μs | 19,361μs | **30.7x faster** | **64.3x faster** |
| 25 | 382μs | 16,844μs | 33,480μs | **44.1x faster** | **87.6x faster** |
| 50 | 526μs | 32,140μs | 57,654μs | **61.1x faster** | **109.6x faster** |

#### Memory Allocation (10 ops, by Runtime)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 19.48KB | 424KB | 3,019KB |
| .NET 8.0 | 19.48KB | 427KB | 2,991KB |
| .NET FX 4.8 | 48.00KB | 552KB | N/A† |

#### Memory Allocation - Parameter Sweep (.NET 8.0)

| Operations | WorkflowForge | Workflow Core | Elsa |
|------------|---------------|---------------|------|
| 10 | 19.48KB | 427KB | 2,991KB |
| 25 | 48.04KB | 1,061KB | 5,947KB |
| 50 | 88.97KB | 2,121KB | 10,907KB |

**Takeaway**: branch decisions add **negligible time** in WorkflowForge here (under ~1μs per decision in the harness).

---

### Scenario 4: Loop/ForEach Processing

**Description**: Iterate over collections (10, 50, 100 items)

#### Multi-Runtime Performance (Median, 50 items)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 450μs | 35,320μs | 54,827μs |
| .NET 8.0 | 495μs | 30,742μs | 58,347μs |
| .NET FX 4.8 | 350μs | 34,137μs | N/A† |

#### Parameter Sweep (.NET 8.0)

| Items | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|-------|---------------|---------------|------|----------|------------|
| 10 | 270μs | 7,242μs | 19,404μs | **26.8x faster** | **71.8x faster** |
| 50 | 495μs | 30,742μs | 58,347μs | **62.1x faster** | **117.9x faster** |
| 100 | 660μs | 60,218μs | 102,879μs | **91.2x faster** | **156.0x faster** |

#### Memory Allocation (50 items, by Runtime)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 96.93KB | 2,086KB | 10,907KB |
| .NET 8.0 | 96.34KB | 2,121KB | 10,907KB |
| .NET FX 4.8 | 176.00KB | 2,512KB | N/A† |

#### Memory Allocation - Parameter Sweep (.NET 8.0)

| Items | WorkflowForge | Workflow Core | Elsa |
|-------|---------------|---------------|------|
| 10 | 20.48KB | 428KB | 2,985KB |
| 50 | 96.34KB | 2,121KB | 10,907KB |
| 100 | 194.85KB | 4,241KB | 20,859KB |

**Numbers**: the ForEach scenario shows a **larger speedup as the collection grows** from 10 to 100 items.

---

### Scenario 5: Concurrent Execution

**Description**: Execute multiple workflows concurrently (1, 4, 8 workflows)

#### Multi-Runtime Performance (Median, 8 workflows)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 372μs | 47,114μs | 87,491μs |
| .NET 8.0 | 357μs | 42,054μs | 103,024μs |
| .NET FX 4.8 | 167μs | 41,934μs | N/A† |

#### Parameter Sweep (.NET 8.0)

| Concurrency | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|-------------|---------------|---------------|------|----------|------------|
| 1 | 260μs | 7,588μs | 19,265μs | **29.2x faster** | **74.2x faster** |
| 4 | 322μs | 21,717μs | 56,360μs | **67.4x faster** | **175.0x faster** |
| 8 | 357μs | 42,054μs | 103,024μs | **117.8x faster** | **288.3x faster** |

#### Memory Allocation (8 workflows, by Runtime)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 154.66KB | 3,247KB | 19,568KB |
| .NET 8.0 | 154.67KB | 3,308KB | 19,572KB |
| .NET FX 4.8 | 272.00KB | 3,816KB | N/A† |

#### Memory Allocation - Parameter Sweep (.NET 8.0)

| Concurrency | WorkflowForge | Workflow Core | Elsa |
|-------------|---------------|---------------|------|
| 1 | 20.00KB | 426KB | 2,983KB |
| 4 | 79.68KB | 1,627KB | 9,861KB |
| 8 | 154.67KB | 3,308KB | 19,572KB |

**What matters**: total time scales with the number of workflows, but **per-workflow overhead stays flat** in WorkflowForge as concurrency rises in this test.

---

### Scenario 6: Error Handling

**Description**: Exception handling and recovery

#### Multi-Runtime Performance (Median)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 70μs | 1,498μs | 7,694μs |
| .NET 8.0 | 114μs | 1,349μs | 7,737μs |
| .NET FX 4.8 | 88μs | 4,471μs | N/A† |

#### Memory Allocation (by Runtime)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 7.02KB | 51KB | 1,056KB |
| .NET 8.0 | 8.38KB | 47KB | 1,072KB |
| .NET FX 4.8 | N/A‡ | 864KB | N/A† |

**Advantage**: **13-110x faster** than competitors, **6-150x less memory**.

**Takeaway**: error-handling paths finish in about **70–114μs** median on WorkflowForge versus milliseconds on the others in this scenario.

---

### Scenario 7: Creation Overhead

**Description**: Workflow instantiation cost

#### Multi-Runtime Performance (Median)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 11μs | 1,001μs | 2,245μs |
| .NET 8.0 | 11μs | 819μs | 2,328μs |
| .NET FX 4.8 | 7μs | 260μs | N/A† |

#### Memory Allocation (by Runtime)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 3.72KB | 125KB | 537KB |
| .NET 8.0 | 3.72KB | 129KB | 578KB |
| .NET FX 4.8 | N/A‡ | 128KB | N/A† |

**Advantage**: **37-206x faster** than competitors, **33-155x less memory**.

**Numbers**: creating a workflow instance costs about **7–11μs** median on WorkflowForge in the creation-overhead benchmark.

---

### Scenario 8: Complete Lifecycle

**Description**: Full create-execute-dispose cycle (Workflow Core excluded)

#### Multi-Runtime Performance (Median)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 36μs | N/A | 9,877μs |
| .NET 8.0 | 59μs | N/A | 9,723μs |
| .NET FX 4.8 | 33μs | N/A | N/A† |

#### Memory Allocation (by Runtime)

| Runtime | WorkflowForge | Elsa |
|---------|---------------|------|
| .NET 10.0 | 3.69KB | 1,513KB |
| .NET 8.0 | 3.69KB | 1,510KB |
| .NET FX 4.8 | N/A‡ | N/A† |

**Advantage**: **165-274x faster** than Elsa, **410x less memory**.

**Note**: Workflow Core was excluded from this benchmark because `WorkflowHost.Start()` starts background worker threads meant to run continuously, which does not line up with tight create-start-stop-dispose loops at 50 iterations. That is a model mismatch for this particular test, not a claim about raw CPU speed.

**What matters**: WorkflowForge completes the full create-execute-dispose cycle in about **33–59μs** median here.

---

### Scenario 9: State Machine

**Description**: State machine with multiple transitions (5, 10, 25 transitions)

#### Multi-Runtime Performance (Median, 25 transitions)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 65μs | 29,537μs | 33,062μs |
| .NET 8.0 | 71μs | 21,683μs | 34,426μs |
| .NET FX 4.8 | 61μs | 18,486μs | N/A† |

#### Parameter Sweep (.NET 8.0)

| Transitions | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|-------------|---------------|---------------|------|----------|------------|
| 5 | 36μs | 6,275μs | 14,444μs | **174.3x faster** | **401.2x faster** |
| 10 | 43μs | 10,028μs | 19,626μs | **233.2x faster** | **456.4x faster** |
| 25 | 71μs | 21,683μs | 34,426μs | **305.4x faster** | **484.9x faster** |

#### Memory Allocation (25 transitions, by Runtime)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 23.92KB | 1,090KB | 5,966KB |
| .NET 8.0 | 23.92KB | 1,105KB | 5,937KB |
| .NET FX 4.8 | 24.00KB | 1,344KB | N/A† |

#### Memory Allocation - Parameter Sweep (.NET 8.0)

| Transitions | WorkflowForge | Workflow Core | Elsa |
|-------------|---------------|---------------|------|
| 5 | 5.45KB | 261KB | 2,017KB |
| 10 | 8.65KB | 472KB | 2,986KB |
| 25 | 23.92KB | 1,105KB | 5,937KB |

**Takeaway**: the state-machine sweep posts the **largest execution deltas** in the suite (up to ~511x versus Elsa on .NET 10.0 for 25 transitions).

---

### Scenario 10: Long Running

**Description**: Long-running operations with delays (delay-bound scenario)

#### Multi-Runtime Performance (Median, 5 ops, 5ms delay)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 71,678μs | 70,868μs | 83,599μs |
| .NET 8.0 | 71,885μs | 70,672μs | 82,982μs |
| .NET FX 4.8 | 76,447μs | 75,129μs | N/A† |

#### Parameter Sweep (.NET 8.0)

| Ops/Delay | WorkflowForge | Workflow Core | Elsa |
|-----------|---------------|---------------|------|
| 3 ops/1ms | 38,836μs | 38,599μs | 50,641μs |
| 5 ops/1ms | 71,802μs | 70,252μs | 81,715μs |
| 5 ops/5ms | 71,885μs | 70,672μs | 82,982μs |

#### Memory Allocation (by Runtime)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 5.12KB | 267KB | 2,246KB |
| .NET 8.0 | 5.12KB | 266KB | 2,216KB |
| .NET FX 4.8 | N/A‡ | 393KB | N/A† |

**Advantage**: Similar timing (delay-bound); advantage is in **52-439x less memory**.

**Numbers**: long-running scenarios are **delay-bound**, so wall-clock time tracks the configured delays. The measurable gap is **allocation**, not raw orchestration time.

---

### Scenario 11: Parallel Execution

**Description**: Parallel operation execution within a workflow (4, 8, 16 operations)

#### Multi-Runtime Performance (Median, 16 ops, 4 concurrency)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 56μs | 2,861μs | 24,638μs |
| .NET 8.0 | 63μs | 2,654μs | 24,940μs |
| .NET FX 4.8 | 35μs | 1,754μs | N/A† |

#### Parameter Sweep (.NET 8.0)

| Ops/Concurrency | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|-----------------|---------------|---------------|------|----------|------------|
| 4 ops/2 | 68μs | 2,771μs | 13,147μs | **40.8x faster** | **193.3x faster** |
| 8 ops/4 | 72μs | 2,736μs | 13,546μs | **38.0x faster** | **188.1x faster** |
| 16 ops/4 | 63μs | 2,654μs | 24,940μs | **42.1x faster** | **395.9x faster** |

#### Memory Allocation (by Runtime)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 7.96KB | 126KB | 4,576KB |
| .NET 8.0 | 8.23KB | 125KB | 4,651KB |
| .NET FX 4.8 | N/A‡ | 184KB | N/A† |

**What matters**: parallel execution keeps a **38–396x** execution advantage in this sweep alongside **15–575x** less allocated memory versus the listed competitors.

---

### Scenario 12: Event-Driven

**Description**: Event-driven workflow execution with delays

#### Multi-Runtime Performance (Median, 1ms delay)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 7,083μs | 8,268μs | 20,590μs |
| .NET 8.0 | 7,128μs | 7,362μs | 19,916μs |
| .NET FX 4.8 | 12,585μs | 12,400μs | N/A† |

#### Parameter Sweep (.NET 8.0)

| Delay | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|-------|---------------|---------------|------|----------|------------|
| 1ms | 7,128μs | 7,362μs | 19,916μs | **1.0x** | **2.8x faster** |
| 5ms | 7,147μs | 8,613μs | 20,608μs | **1.2x faster** | **2.9x faster** |

#### Memory Allocation (by Runtime)

| Runtime | WorkflowForge | Workflow Core | Elsa |
|---------|---------------|---------------|------|
| .NET 10.0 | 3.48KB | 40KB | 999KB |
| .NET 8.0 | 3.48KB | 37KB | 1,032KB |
| .NET FX 4.8 | N/A‡ | 90KB | N/A† |

**Advantage**: WorkflowForge and Workflow Core are **near-parity** on execution time (1ms delay); WorkflowForge is **2.8-2.9x faster** vs Elsa. Memory advantage: **11-297x less**.

**Takeaway**: event-driven runs are **I/O-bound** at the tested delays; WorkflowForge mainly separates from Elsa on **memory use** and steadier timing at 1–5ms delays.

---

## Performance Advantage Summary

### By Scenario Type (12 Scenarios)

| # | Scenario | Speed Advantage | Memory Advantage |
|---|----------|-----------------|------------------|
| 1 | Sequential (10 ops) | 26-55x | 24-171x |
| 2 | Data Passing (10 ops) | 30-60x | 26-185x |
| 3 | Conditional (10 ops) | 31-64x | 22-155x |
| 4 | Loop/ForEach (50 items) | 62-118x | 22-113x |
| 5 | Concurrent (8 workflows) | 118-288x | 21-126x |
| 6 | Error Handling | 13-110x | 6-150x |
| 7 | Creation Overhead | 37-206x | 33-155x |
| 8 | Complete Lifecycle | 165-274x | 410x |
| 9 | State Machine (25 trans) | **303-511x** | 46-249x |
| 10 | Long Running | ~1x (delay-bound) | **52-439x** |
| 11 | Parallel (16 ops) | 38-396x | 15-575x |
| 12 | Event-Driven | 1.0-2.9x | 11-297x |

Ranges include all three runtimes (.NET 10.0, .NET 8.0, .NET Framework 4.8). Elsa is excluded from .NET Framework 4.8 comparisons.

**Overall Speed Range**: **13-511x faster execution** (compute-bound scenarios)  
**Overall Memory Range**: **6-575x less memory allocation**

### Reading the summary table

1. **State machine** carries the widest execution spread we recorded (**303–511x** in the sweep).
2. **Concurrent** work stays in the **118–288x** band vs Elsa on the runtimes listed.
3. **Long running** and **event-driven** rows are delay-heavy; the standout delta there is allocation (WorkflowForge **52–439x** lower in those tests).
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
- **Iterations**: 50 per benchmark
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
- Run via `dotnet run -c Release`

---

## Statistical Significance

All results meet statistical significance criteria:
- Standard deviation < 20% of mean (most scenarios)
- P95 values show consistency
- **Median values used for comparison** (more stable than mean)
- 50 iterations provide statistical confidence

**Outliers**: Some scenarios show high standard deviation due to GC pauses or system activity. Median values are used to minimize impact.

---

## Summary

Across these twelve scenarios, the harness logged **13–511x faster execution** and **6–575x lower allocation** for WorkflowForge vs Workflow Core and Elsa on .NET 10.0, 8.0, and .NET Framework 4.8. The deltas line up with a few concrete differences:

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
