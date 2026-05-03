---
title: Performance & Benchmarks
description: "Performance overview and benchmarks: comparative speedups and allocation ratios from BenchmarkDotNet, plus targets and tuning notes."
---

# Performance & Benchmarks

Where WorkflowForge sits on BenchmarkDotNet runs: internal numbers, comparisons, targets, and tuning notes.

**Version**: 2.1.1  
**Test System**: Windows 11 (25H2), Intel 11th Gen i7-1185G7, .NET SDK 10.0.103  
**Runtimes**: .NET 10.0.3, .NET 8.0.24, .NET Framework 4.8.1  
**BenchmarkDotNet**: v0.15.8, 50 iterations  
**Last Updated**: March 2026

---

## Table of Contents

1. [Overview](#overview)
2. [Internal Performance Benchmarks](#internal-performance-benchmarks)
3. [Competitive Performance Summary](#competitive-performance-summary)
4. [Performance Targets and Guidelines](#performance-targets-and-guidelines)
5. [Optimization Guide](#optimization-guide)
6. [Benchmark Methodology](#benchmark-methodology)
7. [Version History](#version-history)
8. [Related Documentation](#related-documentation)

---

## Overview

The engine optimizes for **low-latency in-process orchestration**:

- **Microseconds on the hot path**: simple ops median about 11–80μs in the harness.
- **Small baseline**: ~3.33KB, flat from 10 through 500 iterations in the minimal workflow test.
- **Concurrency**: about 8.0x with 8 workflows and 15.9x with 16 in the same suite.
- **Property bag data flow**: no per-step serialization or reflection on that path.
- **No host-owned worker pool**: parallelism is explicit (`ForEachWorkflowOperation`, your schedulers).

On .NET 10, 8, and .NET Framework 4.8 the comparative harness reports **13–511x faster wall times** and **6–575x lower allocations** than Workflow Core and Elsa for the scripted scenarios we mirrored. Ratios are tied to those scripts and hardware, not every app.

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
    <div class="perf-stat-value">3.33KB</div>
    <div class="perf-stat-label">Memory Baseline</div>
  </div>
</div>
{% endif %}

---

## Internal Performance Benchmarks

These internal benchmarks measure WorkflowForge in isolation across all three runtimes:

| Metric | .NET 8.0 | .NET 10.0 | .NET FX 4.8 |
|--------|----------|-----------|-------------|
| **Operation Execution** | 12–80μs median | 11–78μs median | 9–55μs median |
| **Operation Creation** | 1.65–1.9μs | 1.4–1.65μs | 1.2–1.6μs |
| **Workflow Throughput** | 50–292μs (1–50 ops) | 47–258μs (1–50 ops) | 33–345μs (1–50 ops) |
| **Memory Baseline** | 3.33KB (constant) | 3.33KB (constant) | N/A‡ |
| **Concurrent Scaling** | 8.0x (8 wf), 15.9x (16 wf) | 8.0x (8 wf), 15.9x (16 wf) | 8.1x (8 wf), 16.2x (16 wf) |

‡ .NET Framework 4.8 does not report memory allocation metrics in BenchmarkDotNet.

**Observations**

- Custom operations allocate the least here (456–592 B per run in the harness).
- Logging operations post the shortest times (10.85–12.1μs).
- The minimal-allocation workflow holds 3,408 B from 10 through 500 iterations.
- Ordinary paths in this matrix stayed off Gen2.

For operation-by-operation results, throughput scaling, memory patterns, and concurrency charts, see [Internal Benchmarks](internal-benchmarks.md).

---

## Competitive Performance Summary

WorkflowForge is run beside Workflow Core and Elsa on **twelve shared scenarios** (same script) on .NET 10.0, 8.0, and .NET Framework 4.8. Peak ratio in our log: **511x** wall time (state machine vs Elsa, .NET 10.0). Shortest median: **7μs** (creation overhead, .NET FX 4.8). Aggregate bands from the tables: **13–511x** time, **6–575x** allocation.

{% include benchmark-data.md %}

{% if site.url %}
<div class="perf-vchart">
  <div class="perf-vchart-title">State Machine Execution (25 Transitions)</div>
  <div class="perf-vchart-subtitle">State machine, 25 transitions (.NET 10.0); values from tables below</div>
  <div class="perf-vchart-container">
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">65μs</div><div class="perf-vchart-fill wf" style="height: 37%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">29.5ms</div><div class="perf-vchart-fill wc" style="height: 95%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">33.1ms</div><div class="perf-vchart-fill elsa" style="height: 97%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 10.0</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">71μs</div><div class="perf-vchart-fill wf" style="height: 40%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">21.7ms</div><div class="perf-vchart-fill wc" style="height: 92%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">34.4ms</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 8.0</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">61μs</div><div class="perf-vchart-fill wf" style="height: 39%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">18.5ms</div><div class="perf-vchart-fill wc" style="height: 87%;"></div></div>
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

For scenario breakdowns, parameter sweeps, and architectural comparisons, see [Competitive Analysis](competitive-analysis.md).

---

## Performance Targets and Guidelines

WorkflowForge maintains the following performance targets:

| Metric | Target | Status |
|--------|--------|--------|
| Operation execution | <100μs median for simple operations | Met (11–80μs) |
| Workflow creation | <5μs overhead | Met (1.4–1.9μs) |
| Memory baseline | <5KB for minimal workflows | Met (3.33KB) |
| GC pressure | No Gen2 collections in typical workloads | Met |
| Concurrent scaling | Near-linear | Met (8.0x for 8, 15.9x for 16) |

Targets below come straight from the same BenchmarkDotNet matrix. Keep workflows on custom ops, modest property payloads, and tight middleware stacks if you need the same envelope.

---

## Optimization Guide

### 1. Choose the Right Operation Type

- **Custom class-based operations** use the least memory in benchmarks (456–592 B); they are the default choice when you care about allocation.
- **Logging operations** measure fastest (10.85–12.1μs) for tiny steps.
- **Delegate operations** are handy but add roughly 5–10μs versus a custom operation in the same harness.

### 2. Reuse Workflow Definitions

Build workflows once and execute many times. Creation overhead is minimal (1.4–1.9μs) but reuse is still recommended for high-throughput scenarios.

### 3. Optimize Data Passing

- Use `foundry.Properties` for all data flow
- Cache property reads in loops instead of repeated lookups
- Avoid large object allocations in hot paths

### 4. Use Concurrency

Use `ForEachWorkflowOperation` for parallel execution of independent operations. Concurrency scales near-linearly (8.0x for 8 workflows, 15.9x for 16).

### 5. Minimize Middleware

Add only necessary middleware. Each middleware adds ~1–5μs overhead per operation.

### 6. Avoid Large Object Allocations

Large allocations (>85KB) trigger Gen2 GC and degrade throughput. Keep operation payloads small where possible.

### 7. Use .NET 10.0 Where Available

.NET 10.0 shows improved exception handling (70μs vs 114μs) and some operation gains.

For detailed code examples and patterns, see [Internal Benchmarks](internal-benchmarks.md#optimization-recommendations) and [Operations](../core/operations.md).

---

## Benchmark Methodology

### Configuration

- **Framework**: BenchmarkDotNet v0.15.8
- **Runtimes**: .NET 10.0.3, .NET 8.0.24, .NET Framework 4.8.1
- **Iterations**: 50 per benchmark
- **Warmup**: 5 iterations
- **Invocation**: 1 per iteration

### Hardware

- **OS**: Windows 11 (25H2)
- **CPU**: Intel 11th Gen i7-1185G7
- **SDK**: .NET SDK 10.0.103

### Statistical Approach

- **Median values** used for comparison (more stable than mean)
- Standard deviation < 20% of mean for most scenarios
- P95 values available for consistency verification
- All scenarios implement identical logic across frameworks

### Reproduction

**Internal benchmarks**:

```bash
cd src/benchmarks/WorkflowForge.Benchmarks
dotnet run -c Release
```

**Competitive benchmarks**:

```bash
cd src/benchmarks/WorkflowForge.Benchmarks.Comparative
dotnet run -c Release
```

BenchmarkDotNet writes output under `BenchmarkDotNet.Artifacts/results/`. Expect full comparative runs to take about 30–60 minutes.

---

## Version History

### Version 2.1.1 (current, March 2026)

- Multi-target .NET 10.0, .NET 8.0, .NET Framework 4.8
- Sealed operation classes
- ConfigureAwait optimization
- Compensation always attempts RestoreAsync (no-op default)

### Version 2.0.0 (January 2026)

- Initial release with 12 competitive scenarios
- Head-to-head comparison with Workflow Core and Elsa Workflows

---

## Related Documentation

- [Internal Benchmarks](internal-benchmarks.md): operation performance, throughput, memory, concurrency
- [Competitive Analysis](competitive-analysis.md): scenario breakdowns, parameter sweeps, architectural differences
- [Architecture Overview](../architecture/overview.md): design principles and execution model
- [Operations](../core/operations.md): operation types and middleware pipeline
