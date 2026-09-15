---
title: Performance & Benchmarks
description: "Performance overview and benchmarks: comparative speedups and allocation ratios from BenchmarkDotNet, plus targets and tuning notes."
---

# Performance & Benchmarks

Where WorkflowForge sits on BenchmarkDotNet runs: internal numbers, comparisons, targets, and tuning notes.

**Version**: 2.2.0  
**Test System**: Windows 11 (25H2), Intel 11th Gen i7-1185G7, .NET SDK 10.0.103  
**Runtimes**: .NET 10.0.3, .NET 8.0.24, .NET Framework 4.8.1  
**BenchmarkDotNet**: v0.15.8, 10 iterations per job  
**Last Updated**: 14 September 2026 (2.2.0 hot-path release; full comparative BenchmarkDotNet re-run on release runner before tag)

> **Reading the 2.1.0 → 2.2.0 deltas.** Several numbers below moved by 10–1000x since the last
> published run. Before treating that as a real speedup (or a regression, where a competitive
> multiplier shrank), know which of three things caused it:
> 1. **Harness fix, not code.** The 2.1.0 run used a `RunStrategy`/iteration-count combination meant
>    for tens-of-ms+ work; on nanosecond/microsecond operations it measured JIT/GC/scheduler noise
>    instead of the real cost. Fixed this release. Control case: `DelayOperationExecution`, which
>    actually sleeps, moved from 15,192μs to 16.1ms — i.e. ~0%, because real work isn't affected by
>    the fix. Fast in-process operations moved 100–1000x (e.g. operation creation: ~1.7μs → ~50ns) —
>    that's the noise leaving, not new code making creation 30x faster.
> 2. **A fairer comparison, not a regression.** Scenario 7 ("Creation Overhead")'s advantage vs.
>    Elsa fell from 74–212x to a plain 2–3x. Elsa's old number included rebuilding its DI container
>    on every measured call (a bug in *our* benchmark harness, since fixed by moving that setup out
>    of the timed region) — so the old multiplier was comparing our real cost against Elsa's
>    DI-bootstrap cost, not its creation cost. The new number is the fair one.
> 3. **Real code changes.** Where a change touched the exact hot path being measured — e.g. the
>    retried-operations-bypass-middleware fix, `LoggingMiddleware` no longer being inert — some of
>    the remaining delta is genuine. Concurrency scaling (8.0x @ 8 workers, 15.9x @ 16) is unchanged
>    from 2.1.0, which is the control for "did real throughput change": it didn't move, because that
>    ratio cancels out most harness noise on its own.

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

- **Nanoseconds to low microseconds on the hot path**: CPU-bound ops median about **74ns–9.5μs** in the harness (delays excluded).
- **Small baseline**: ~4.03KB, flat from 10 through 500 iterations in the minimal workflow test.
- **Concurrency**: about 8.0x with 8 workflows and 15.9x with 16 in the same suite.
- **Property bag data flow**: no per-step serialization or reflection on that path.
- **No host-owned worker pool**: parallelism is explicit (`ForEachWorkflowOperation`, your schedulers).

On .NET 10, 8, and .NET Framework 4.8 the comparative harness reports **2-583x faster wall times** and **1–533x lower allocations** than Workflow Core and Elsa for the scripted scenarios we mirrored. Ratios are tied to those scripts and hardware, not every app.

{% if site.url %}
<div class="perf-stats">
  <div class="perf-stat">
    <div class="perf-stat-value">584x</div>
    <div class="perf-stat-label">Faster (State Machine vs Elsa)</div>
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
    <div class="perf-stat-value">4.03KB</div>
    <div class="perf-stat-label">Memory Baseline</div>
  </div>
</div>
{% endif %}

---

## Internal Performance Benchmarks

These internal benchmarks measure WorkflowForge in isolation across all three runtimes:

| Metric | .NET 8.0 | .NET 10.0 | .NET FX 4.8 |
|--------|----------|-----------|-------------|
| **Operation Execution** | 76ns–9.5μs median | 74ns–9.5μs median | 216ns–32μs median |
| **Operation Creation** | ~51ns median | ~48ns median | ~60ns median |
| **Workflow Throughput** | ~4–211μs (custom, 1–50 ops) | ~4–211μs (custom, 1–50 ops) | ~5–345μs (1–50 ops) |
| **Memory Baseline** | 4.03KB (constant) | 4.03KB (constant) | N/A‡ |
| **Concurrent Scaling** | 8.0x (8 wf), 15.9x (16 wf) | 8.0x (8 wf), 15.9x (16 wf) | 8.1x (8 wf), 16.2x (16 wf) |

‡ .NET Framework 4.8 does not report memory allocation metrics in BenchmarkDotNet.

**Observations**

- Custom operations allocate the least here (about **392 B** per run in the harness).
- Logging operations post the shortest times (**~74ns** on .NET 10.0).
- The minimal-allocation workflow holds 4,126 B from 10 through 500 iterations.
- Ordinary paths in this matrix stayed off Gen2.

For operation-by-operation results, throughput scaling, memory patterns, and concurrency charts, see [Internal Benchmarks](internal-benchmarks.md).

---

## Competitive Performance Summary

WorkflowForge is run beside Workflow Core and Elsa on **twelve shared scenarios** (same script) on .NET 10.0, 8.0, and .NET Framework 4.8. Peak ratio in our log: **584x** wall time (state machine vs Elsa, .NET 10.0). Shortest median: **6.90μs** (creation overhead, .NET FX 4.8). Aggregate bands from the tables: **2–583x** time, **1–533x** allocation.

{% include benchmark-data.md %}

{% if site.url %}
<div class="perf-vchart">
  <div class="perf-vchart-title">State Machine Execution (25 Transitions)</div>
  <div class="perf-vchart-subtitle">State machine, 25 transitions (.NET 10.0); values from tables below</div>
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
{% endif %}

For scenario breakdowns, parameter sweeps, and architectural comparisons, see [Competitive Analysis](competitive-analysis.md).

---

## Performance Targets and Guidelines

WorkflowForge maintains the following performance targets:

| Metric | Target | Status |
|--------|--------|--------|
| Operation execution | <100μs median for simple operations | Met (sub-μs for logging; chained ops ~10μs) |
| Workflow creation | <5μs overhead | Met (~48ns op creation; ~18μs workflow creation in comparative harness) |
| Memory baseline | <5KB for minimal workflows | Met (4.03KB) |
| GC pressure | No Gen2 collections in typical workloads | Met |
| Concurrent scaling | Near-linear | Met (8.0x for 8, 15.9x for 16) |

Targets below come straight from the same BenchmarkDotNet matrix. Keep workflows on custom ops, modest property payloads, and tight middleware stacks if you need the same envelope.

---

## Optimization Guide

### 1. Choose the Right Operation Type

- **Custom class-based operations** use the least memory in benchmarks (~392 B); they are the default choice when you care about allocation.
- **Logging operations** measure fastest (~74ns on .NET 10.0) for tiny steps.
- **Delegate operations** are handy but cost a bit more than custom ops (~1.5μs vs ~1.3μs in the same harness).

### 2. Reuse Workflow Definitions

Build workflows once and execute many times. Operation creation is tens of nanoseconds; reuse still helps when definitions are heavy.

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

.NET 10.0 shows improved exception handling (16.6μs vs 33.9μs) and some operation gains.

For detailed code examples and patterns, see [Internal Benchmarks](internal-benchmarks.md#optimization-recommendations) and [Operations](../core/operations.md).

---

## Benchmark Methodology

### Configuration

- **Framework**: BenchmarkDotNet v0.15.8
- **Runtimes**: .NET 10.0.3, .NET 8.0.24, .NET Framework 4.8.1
- **Iterations**: 10 per benchmark job
- **Warmup**: per BenchmarkDotNet default job
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
dotnet run -c Release -f net48 -f net8.0 -f net10.0
```

**Competitive benchmarks**:

```bash
cd src/benchmarks/WorkflowForge.Benchmarks.Comparative
dotnet run -c Release -f net48 -f net8.0 -f net10.0
```

BenchmarkDotNet writes output under `BenchmarkDotNet.Artifacts/results/`. Expect full comparative runs to take about 30–60 minutes.

---

## Version History

### Version 2.2.0 (September 2026)

- Refreshed competitive and internal benchmark documentation from September 2026 BenchmarkDotNet runs (10 iterations per job).

### Version 2.1.2 (July 2026)

- Fixed ILRepacked extensions (Polly/OpenTelemetry/Serilog) missing transitive dependencies
- Implemented foundry performance monitoring (`EnablePerformanceMonitoring` / `GetPerformanceStatistics`)
- Fixed pooled-foundry state leak and audit workflow-name resolution
- Centralized package versioning and shared metadata

### Version 2.1.1 (March 2026)

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
