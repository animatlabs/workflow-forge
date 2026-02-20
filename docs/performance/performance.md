---
title: Performance & Benchmarks
description: WorkflowForge delivers 13-522x faster execution and 6-578x less memory than alternatives. Microsecond-scale workflows, minimal footprint, near-linear scaling.
---

# Performance & Benchmarks

This document provides a comprehensive overview of WorkflowForge's performance characteristics, including internal benchmarks, competitive comparisons, targets, and optimization guidance.

**Version**: 2.1.0  
**Test System**: Windows 11 (25H2), Intel 11th Gen i7-1185G7, .NET SDK 10.0.103  
**Runtimes**: .NET 10.0.3, .NET 8.0.24, .NET Framework 4.8.1  
**BenchmarkDotNet**: v0.15.8, 50 iterations  
**Last Updated**: February 2026

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

WorkflowForge is designed for **high-performance workflow orchestration**. Its architecture prioritizes:

- **Microsecond-scale execution** — CPU-bound operations complete in 14–135μs median
- **Minimal memory footprint** — 3.3KB baseline, constant regardless of iteration count
- **Near-linear concurrent scaling** — 7.9x speedup with 8 workflows, 15.7x with 16
- **Zero serialization overhead** — Dictionary-based data flow, no reflection per operation
- **No background threads** — Synchronous execution model with explicit parallelism

These design choices yield **13–522x faster execution** and **6–578x less memory allocation** compared to Workflow Core and Elsa Workflows across 12 real-world scenarios on .NET 10.0, .NET 8.0, and .NET Framework 4.8. Benchmarks are factual measurements from BenchmarkDotNet on representative hardware.

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
    <div class="perf-stat-label">Min Execution Time</div>
  </div>
  <div class="perf-stat">
    <div class="perf-stat-value">3.3KB</div>
    <div class="perf-stat-label">Memory Baseline</div>
  </div>
</div>
{% endif %}

---

## Internal Performance Benchmarks

Internal benchmarks validate WorkflowForge's intrinsic performance in isolation across all three runtimes:

| Metric | .NET 8.0 | .NET 10.0 | .NET FX 4.8 |
|--------|----------|-----------|-------------|
| **Operation Execution** | 14–135μs median | 10–131μs median | 10–116μs median |
| **Operation Creation** | 1.9–2.5μs | 1.7–2.3μs | 1.3–2.0μs |
| **Workflow Throughput** | 77–237μs (1–50 ops) | 73–224μs (1–50 ops) | 62–195μs (1–50 ops) |
| **Memory Baseline** | 3.3KB (constant) | 3.3KB (constant) | N/A‡ |
| **Concurrent Scaling** | 7.9x (8 wf), 15.7x (16 wf) | 8.1x (8 wf), 15.9x (16 wf) | 7.6x (8 wf), 14.8x (16 wf) |

‡ .NET Framework 4.8 does not report memory allocation metrics in BenchmarkDotNet.

**Key findings**:

- Custom operations are the most memory-efficient (456 B per execution)
- Logging operations are fastest (14.6μs)
- Minimal allocation workflow maintains constant 3,296 B across 10–500 allocations
- No Gen2 GC collections in typical workloads

For full operation-by-operation results, throughput scaling, memory patterns, and concurrency charts, see [Internal Benchmarks](internal-benchmarks.md).

---

## Competitive Performance Summary

WorkflowForge is compared against Workflow Core and Elsa Workflows across **12 scenarios** with identical logic on .NET 10.0, .NET 8.0, and .NET Framework 4.8. Max speed: **522x faster** (State Machine vs Elsa, .NET 10.0); min execution: **14μs** (Creation Overhead). **Overall ranges**: **13–522x faster**, **6–578x less memory**.

{% include benchmark-data.md %}

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

For scenario breakdowns, parameter sweeps, and architectural comparisons, see [Competitive Analysis](competitive-analysis.md).

---

## Performance Targets and Guidelines

WorkflowForge maintains the following performance targets:

| Metric | Target | Status |
|--------|--------|--------|
| Operation execution | <100μs median for simple operations | Met (14–135μs) |
| Workflow creation | <5μs overhead | Met (1.9–2.5μs) |
| Memory baseline | <5KB for minimal workflows | Met (3.3KB) |
| GC pressure | No Gen2 collections in typical workloads | Met |
| Concurrent scaling | Near-linear | Met (7.9x for 8, 15.7x for 16) |

These targets are validated by internal and competitive benchmarks. When designing workflows, prefer patterns that align with these characteristics.

---

## Optimization Guide

### 1. Choose the Right Operation Type

- **Custom class-based operations** — Most memory-efficient (456 B), recommended for production
- **Logging operations** — Fastest execution (14.6μs) for lightweight tasks
- **Delegate operations** — Convenient but add ~5–10μs overhead vs custom

### 2. Reuse Workflow Definitions

Build workflows once and execute many times. Creation overhead is minimal (1.9–2.5μs) but reuse remains best practice for high-throughput scenarios.

### 3. Optimize Data Passing

- Use `foundry.Properties` for all data flow
- Cache property reads in loops instead of repeated lookups
- Avoid large object allocations in hot paths

### 4. Leverage Concurrency

Use `ForEachWorkflowOperation` for parallel execution of independent operations. Concurrency scales near-linearly (7.9x for 8 workflows, 15.7x for 16).

### 5. Minimize Middleware

Add only necessary middleware. Each middleware adds ~1–5μs overhead per operation.

### 6. Avoid Large Object Allocations

Large allocations (>85KB) trigger Gen2 GC and degrade throughput. Keep operation payloads small where possible.

### 7. Use .NET 10.0 Where Available

.NET 10.0 shows improved exception handling (94.5μs vs 134.5μs) and some operation gains.

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

Results are written to `BenchmarkDotNet.Artifacts/results/`. Full runs may take 30–60 minutes.

---

## Version History

### Version 2.1.0 (Current — February 2026)

- Multi-target .NET 10.0, .NET 8.0, .NET Framework 4.8
- Sealed operation classes
- ConfigureAwait optimization
- Compensation always attempts RestoreAsync (no-op default)

### Version 2.0.0 (January 2026)

- Initial release with 12 competitive scenarios
- Head-to-head comparison with Workflow Core and Elsa Workflows

---

## Related Documentation

- [Internal Benchmarks](internal-benchmarks.md) — Operation performance, throughput, memory, concurrency
- [Competitive Analysis](competitive-analysis.md) — Scenario breakdowns, parameter sweeps, architectural differences
- [Architecture Overview](../architecture/overview.md) — Design principles and execution model
- [Operations](../core/operations.md) — Operation types and middleware pipeline
