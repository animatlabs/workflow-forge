---
title: Internal Benchmarks
description: Internal performance benchmarks validating WorkflowForge's microsecond execution, minimal allocations, and linear scaling characteristics.
---

# WorkflowForge Internal Benchmarks

This document presents WorkflowForge's internal performance benchmarks—comprehensive self-testing that validates the framework's performance characteristics independently of competitor comparisons.

**Version**: 2.1.0  
**Test System**: Windows 11 (25H2), Intel 11th Gen i7-1185G7, .NET SDK 10.0.103  
**Runtimes**: .NET 10.0.3, .NET 8.0.24, .NET Framework 4.8.1  
**Benchmark Framework**: BenchmarkDotNet v0.15.8  
**Methodology**: 50 iterations per benchmark, 5 warmup iterations  
**Last Updated**: February 2026

---

## Table of Contents

- [Executive Summary](#executive-summary)
- [Operation Performance](#operation-performance)
- [Workflow Throughput](#workflow-throughput)
- [Memory Allocation](#memory-allocation)
- [Concurrency Scaling](#concurrency-scaling)
- [Optimization Recommendations](#optimization-recommendations)
- [Related Documentation](#related-documentation)

---

## Executive Summary

WorkflowForge internal benchmarks demonstrate (50 iterations, median values):

| Metric | Result |
|--------|--------|
| **Operation Execution** | 14-135μs median (excluding delays) |
| **Operation Creation** | 1.9-2.5μs median |
| **Workflow Throughput** | 38-272μs for custom operations (1-50 ops, all runtimes) |
| **Memory Baseline** | 3,296 B minimal allocation (constant) |
| **Concurrency Scaling** | Near-perfect (7.9x for 8 workers, 15.7x for 16 workers) |
| **GC Pressure** | Gen0 only for typical workloads |

{% if site.url %}
<div class="perf-stats">
  <div class="perf-stat">
    <div class="perf-stat-value">135μs</div>
    <div class="perf-stat-label">Max CPU-bound Op</div>
  </div>
  <div class="perf-stat">
    <div class="perf-stat-value">3.3KB</div>
    <div class="perf-stat-label">Minimal Footprint</div>
  </div>
  <div class="perf-stat">
    <div class="perf-stat-value">15.7x</div>
    <div class="perf-stat-label">Concurrency Speedup</div>
  </div>
  <div class="perf-stat">
    <div class="perf-stat-value">2.5μs</div>
    <div class="perf-stat-label">Op Creation</div>
  </div>
</div>
{% endif %}

---

## Operation Performance

Tests individual operation types for execution time and memory allocation across .NET 8.0, .NET 10.0, and .NET Framework 4.8.

### Operation Execution (Median Times)

| Operation Type | .NET 8.0 | .NET 10.0 | .NET FX 4.8 | Allocated (.NET 8) |
|----------------|----------|-----------|-------------|--------------------|
| LoggingOperationExecution | 14.6μs | 14.5μs | 10.2μs | 1,912 B |
| ConditionalOperationFalse | 53.5μs | 55.9μs | 35.7μs | 1,072 B |
| ConditionalOperationTrue | 56.1μs | 48.9μs | 32.1μs | 1,016 B |
| CustomOperationExecution | 58.4μs | 54.5μs | 36.7μs | 456 B |
| DelegateOperationExecution | 53.0μs | 55.3μs | 37.4μs | 616 B |
| ActionOperationExecution | 64.7μs | 65.8μs | 53.4μs | 648 B |
| ForEachSmallCollection | 78.6μs | 82.1μs | 47.6μs | 2,336 B |
| ForEachLargeCollection | 102.9μs | 92.9μs | 85.6μs | 7,128 B |
| WithRestoration | 79.5μs | 72.5μs | 71.5μs | 632 B |
| DataManipulation | 93.9μs | 92.7μs | 116.1μs | 8,536 B |
| ChainedOperations | 127.2μs | 122.3μs | 96.7μs | 4,288 B |
| ExceptionHandling | 134.5μs | 94.5μs | 115.4μs | 2,944 B |
| DelayOperationExecution | 15,143μs | 15,141μs | 15,327μs | 1,432 B |

*DelayOperationExecution contains a 15ms delay; .NET FX 4.8 does not report allocation metrics.*

### Operation Creation (Median Times)

| Operation Type | .NET 8.0 | .NET 10.0 | .NET FX 4.8 | Allocated (.NET 8) |
|----------------|----------|-----------|-------------|--------------------|
| DelegateCreation | 2.5μs | 2.4μs | 1.9μs | 56 B |
| ActionCreation | 2.4μs | 2.1μs | 2.1μs | 56 B |
| CustomCreation | 2.5μs | 1.9μs | 2.0μs | 32 B |

{% if site.url %}
<div class="perf-vchart">
  <div class="perf-vchart-title">Operation Execution Times (Median, Lower is Better)</div>
  <div class="perf-vchart-subtitle">CPU-bound operations execute in under 135 microseconds across all runtimes</div>
  <div class="perf-vchart-container">
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">14.6μs</div><div class="perf-vchart-fill wf" style="height: 11%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">53.0μs</div><div class="perf-vchart-fill wf" style="height: 39%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">58.4μs</div><div class="perf-vchart-fill wf" style="height: 43%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">64.7μs</div><div class="perf-vchart-fill wf" style="height: 48%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">78.6μs</div><div class="perf-vchart-fill wf" style="height: 58%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">102.9μs</div><div class="perf-vchart-fill wf" style="height: 76%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">127.2μs</div><div class="perf-vchart-fill wf" style="height: 94%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">134.5μs</div><div class="perf-vchart-fill wf" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 8.0</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">14.5μs</div><div class="perf-vchart-fill wf" style="height: 11%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">55.3μs</div><div class="perf-vchart-fill wf" style="height: 41%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">54.5μs</div><div class="perf-vchart-fill wf" style="height: 41%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">65.8μs</div><div class="perf-vchart-fill wf" style="height: 49%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">82.1μs</div><div class="perf-vchart-fill wf" style="height: 61%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">92.9μs</div><div class="perf-vchart-fill wf" style="height: 69%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">122.3μs</div><div class="perf-vchart-fill wf" style="height: 91%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">94.5μs</div><div class="perf-vchart-fill wf" style="height: 70%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 10.0</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">10.2μs</div><div class="perf-vchart-fill wf" style="height: 8%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">37.4μs</div><div class="perf-vchart-fill wf" style="height: 28%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">36.7μs</div><div class="perf-vchart-fill wf" style="height: 27%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">53.4μs</div><div class="perf-vchart-fill wf" style="height: 40%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">47.6μs</div><div class="perf-vchart-fill wf" style="height: 35%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">85.6μs</div><div class="perf-vchart-fill wf" style="height: 64%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">96.7μs</div><div class="perf-vchart-fill wf" style="height: 72%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">115.4μs</div><div class="perf-vchart-fill wf" style="height: 86%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET FX 4.8</div>
    </div>
  </div>
  <div class="perf-vchart-legend">
    <div class="perf-vchart-legend-item"><div class="perf-vchart-legend-color wf"></div>WorkflowForge (Logging → Delegate → Custom → Action → ForEach S → ForEach L → Chained → Exception)</div>
  </div>
</div>
{% endif %}

**Key Findings**:

- Custom operations are the most memory-efficient (456 B)
- Logging operations are fastest (14.6μs)
- Operation creation is extremely fast (2.1-2.5μs)
- Exception handling adds moderate overhead; .NET 10.0 improves it significantly (94.5μs vs 134.5μs)

---

## Workflow Throughput

Tests complete workflow execution patterns with varying operation counts. Delay-bound workflows contain built-in delay operations (~15ms per operation).

### Workflow Patterns (OperationCount=1, Median by Runtime)

| Pattern | .NET 8.0 | .NET 10.0 | .NET FX 4.8 | Memory (.NET 8) | Notes |
|---------|----------|-----------|-------------|-----------------|-------|
| SequentialCustomOperations | 81.6μs | 67.4μs | 37.9μs | 2,984 B | CPU-bound |
| HighPerformanceConfiguration | 112.6μs | 83.7μs | 62.8μs | 3,560 B | CPU-bound |
| ForEachLoopWorkflow | 82.8μs | 88.1μs | 47.1μs | 5,064 B | CPU-bound |
| SequentialDelegateOperations | 15,180μs | 15,026μs | 15,365μs | 3,768 B | Delay-bound |
| DataPassingWorkflow | 15,067μs | 15,198μs | 15,373μs | 3,912 B | Delay-bound |
| ConditionalOperationsWorkflow | 15,194μs | 15,177μs | 15,363μs | 4,240 B | Delay-bound |
| LoggingOperationsWorkflow | 15,188μs | 15,233μs | 15,358μs | 6,072 B | Delay-bound |
| MemoryIntensiveWorkflow | 15,147μs | 15,216μs | 15,325μs | 4,744 B | Delay-bound |

*Memory column shows .NET 8.0 allocation; .NET FX 4.8 allocation metrics are NA.*

### Throughput Scaling (SequentialCustomOperations, Median by Runtime)

| Operations | .NET 8.0 | .NET 10.0 | .NET FX 4.8 | Memory (.NET 8) |
|------------|----------|-----------|-------------|-----------------|
| 1 | 81.6μs | 67.4μs | 37.9μs | 2,984 B |
| 5 | 77.2μs | 74.1μs | 78.7μs | 6,368 B |
| 10 | 92.7μs | 94.2μs | 120.3μs | 10,640 B |
| 25 | 138.2μs | 134.0μs | 186.8μs | 25,736 B |
| 50 | 236.8μs | 214.3μs | 272.3μs | 51,376 B |

{% if site.url %}
<div class="perf-vchart">
  <div class="perf-vchart-title">Custom Operation Throughput Scaling (1-50 Operations)</div>
  <div class="perf-vchart-subtitle">Sub-275μs execution at 50 operations with linear memory growth across runtimes</div>
  <div class="perf-vchart-container">
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">81.6μs</div><div class="perf-vchart-fill wf" style="height: 30%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">77.2μs</div><div class="perf-vchart-fill wf" style="height: 28%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">92.7μs</div><div class="perf-vchart-fill wf" style="height: 34%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">138.2μs</div><div class="perf-vchart-fill wf" style="height: 51%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">236.8μs</div><div class="perf-vchart-fill wf" style="height: 87%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 8.0</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">67.4μs</div><div class="perf-vchart-fill wf" style="height: 25%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">74.1μs</div><div class="perf-vchart-fill wf" style="height: 27%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">94.2μs</div><div class="perf-vchart-fill wf" style="height: 35%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">134μs</div><div class="perf-vchart-fill wf" style="height: 49%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">214.3μs</div><div class="perf-vchart-fill wf" style="height: 79%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 10.0</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">37.9μs</div><div class="perf-vchart-fill wf" style="height: 14%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">78.7μs</div><div class="perf-vchart-fill wf" style="height: 29%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">120.3μs</div><div class="perf-vchart-fill wf" style="height: 44%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">186.8μs</div><div class="perf-vchart-fill wf" style="height: 69%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">272.3μs</div><div class="perf-vchart-fill wf" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET FX 4.8</div>
    </div>
  </div>
  <div class="perf-vchart-legend">
    <div class="perf-vchart-legend-item"><div class="perf-vchart-legend-color wf"></div>SequentialCustomOperations (1→5→10→25→50 ops)</div>
  </div>
</div>
{% endif %}

**Key Findings**:

- CPU-bound workflows execute in 38-272μs for 1-50 operations across runtimes
- Memory scales linearly with operation count
- Delay-bound workflows (~15ms) are dominated by delay duration, not framework overhead

---

## Memory Allocation

Tests memory allocation patterns and GC behavior. All values are median for 10 allocations unless noted.

### Allocation Patterns (10 Allocations, Median by Runtime)

| Pattern | .NET 8.0 | .NET 10.0 | .NET FX 4.8 | Memory (.NET 8) | GC |
|---------|----------|-----------|-------------|-----------------|-----|
| MinimalAllocationWorkflow | 51.8μs | 113.7μs | 36.5μs | 3,296 B | No Gen0/1/2 |
| SmallObjectAllocation | 138.1μs | 188.8μs | 142.3μs | 18,376 B | — |
| StringConcatenationAllocation | 110.6μs | 191.0μs | 211.3μs | 16,352 B | — |
| StringBuilderOptimization | 114.4μs | 173.3μs | 146.4μs | 15,712 B | — |
| CollectionAllocation | 120.1μs | 201.4μs | 122.1μs | 17,360 B | — |
| ObjectPoolingSimulation | 150.0μs | 213.3μs | 186.5μs | 21,448 B | — |
| ArrayReuseOptimization | 128.1μs | 134.6μs | 198.2μs | 22,344 B | — |
| MemoryPressureScenario | 256.5μs | 377.7μs | 297.2μs | 317,472 B | — |
| LargeObjectAllocation | 710.6μs | 992.5μs | 613.2μs | 1,018,056 B | Gen0+Gen1+Gen2 |
| DisposableResourceManagement | 160,024μs | 160,135μs | 159,696μs | 19,232 B | Delay-bound |

*Memory column shows .NET 8.0 allocation; .NET Framework 4.8 allocation metrics are NA (Allocated column shows "—" in benchmarks).*

### Memory Scaling (MinimalAllocationWorkflow, .NET 8.0)

| Allocations | .NET 8.0 Memory | .NET 10.0 Memory |
|-------------|----------------|-----------------|
| 10 | 3,296 B | 3,296 B |
| 50 | 3,296 B | 3,296 B |
| 100 | 3,296 B | 3,296 B |
| 500 | 3,296 B | 3,296 B |

The minimal allocation workflow maintains a **constant footprint** of 3,296 B regardless of allocation count—demonstrating effective object reuse and pooling within the framework. .NET Framework 4.8 does not report allocation metrics.

{% if site.url %}
<div class="perf-vchart">
  <div class="perf-vchart-title">Memory Allocation Patterns (10 Allocations)</div>
  <div class="perf-vchart-subtitle">Minimal workflow stays at 3.3KB; large objects trigger full GC. .NET FX 4.8 allocation metrics NA.</div>
  <div class="perf-vchart-container">
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">3.3KB</div><div class="perf-vchart-fill wf" style="height: 1%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">15.7KB</div><div class="perf-vchart-fill wf" style="height: 5%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">18.4KB</div><div class="perf-vchart-fill wf" style="height: 6%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">22.3KB</div><div class="perf-vchart-fill wf" style="height: 7%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">317KB</div><div class="perf-vchart-fill wc" style="height: 31%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">1.0MB</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 8.0</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">3.3KB</div><div class="perf-vchart-fill wf" style="height: 1%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">15.7KB</div><div class="perf-vchart-fill wf" style="height: 5%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">18.4KB</div><div class="perf-vchart-fill wf" style="height: 6%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">22.3KB</div><div class="perf-vchart-fill wf" style="height: 7%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">317KB</div><div class="perf-vchart-fill wc" style="height: 31%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">1.0MB</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 10.0</div>
    </div>
  </div>
  <div class="perf-vchart-legend">
    <div class="perf-vchart-legend-item"><div class="perf-vchart-legend-color wf"></div>Typical (no GC pressure)</div>
    <div class="perf-vchart-legend-item"><div class="perf-vchart-legend-color wc"></div>Elevated</div>
    <div class="perf-vchart-legend-item"><div class="perf-vchart-legend-color elsa"></div>High (Gen0/1/2)</div>
  </div>
</div>
{% endif %}

**Key Findings**:

- Minimal allocation baseline is constant at 3,296 B across 10-500 allocations
- No GC pressure for typical workflows
- Large object allocations trigger full GC (Gen0/1/2)
- StringBuilder optimization saves ~640 B vs concatenation at 10 allocations

---

## Concurrency Scaling

Tests concurrent workflow execution patterns across .NET 8.0, .NET 10.0, and .NET Framework 4.8.

### Scaling with 8 Workflows (5 ops per workflow)

| Pattern | .NET 8.0 | .NET 10.0 | .NET FX 4.8 | Memory (.NET 8) | Speedup |
|---------|----------|-----------|-------------|-----------------|---------|
| SequentialWorkflows | 627.7 ms | 627.1 ms | 644.8 ms | 80.38 KB | — |
| ConcurrentWorkflows | 79.4 ms | 79.3 ms | 79.8 ms | 83.38 KB | 7.9x |
| ParallelWorkflows | 79.3 ms | 79.3 ms | 79.8 ms | 83.32 KB | 7.9x |

### Scaling with 16 Workflows (5 ops per workflow)

| Pattern | .NET 8.0 | .NET 10.0 | .NET FX 4.8 | Memory (.NET 8) | Speedup |
|---------|----------|-----------|-------------|-----------------|---------|
| SequentialWorkflows | 1,247 ms | 1,256 ms | 1,290 ms | 160.46 KB | — |
| ConcurrentWorkflows | 79.3 ms | 79.2 ms | 79.7 ms | 166.34 KB | 15.7x |

### Scaling with 8 Workflows, 25 ops each

| Pattern | .NET 8.0 | .NET 10.0 | .NET FX 4.8 | Memory (.NET 8) | Speedup |
|---------|----------|-----------|-------------|-----------------|---------|
| SequentialWorkflows | 3,140 ms | 3,143 ms | 3,225 ms | 380.25 KB | — |
| ConcurrentWorkflows | 400.5 ms | 401.1 ms | 402.4 ms | 383.25 KB | 7.8x |

{% if site.url %}
<div class="perf-vchart">
  <div class="perf-vchart-title">Concurrency Scaling (Sequential vs Concurrent Time)</div>
  <div class="perf-vchart-subtitle">Near-perfect linear scaling across runtimes: 7.9x for 8 workflows, 15.7x for 16 workflows</div>
  <div class="perf-vchart-container">
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">628ms</div><div class="perf-vchart-fill wc" style="height: 50%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">79ms</div><div class="perf-vchart-fill wf" style="height: 6%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 8.0 (8 wf, 7.9x)</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">1.25s</div><div class="perf-vchart-fill wc" style="height: 100%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">79ms</div><div class="perf-vchart-fill wf" style="height: 6%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 8.0 (16 wf, 15.7x)</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">627ms</div><div class="perf-vchart-fill wc" style="height: 50%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">79ms</div><div class="perf-vchart-fill wf" style="height: 6%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 10.0 (8 wf, 7.9x)</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">1.26s</div><div class="perf-vchart-fill wc" style="height: 100%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">79ms</div><div class="perf-vchart-fill wf" style="height: 6%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 10.0 (16 wf, 15.9x)</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">645ms</div><div class="perf-vchart-fill wc" style="height: 50%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">80ms</div><div class="perf-vchart-fill wf" style="height: 6%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET FX 4.8 (8 wf, 8.1x)</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">1.29s</div><div class="perf-vchart-fill wc" style="height: 100%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">80ms</div><div class="perf-vchart-fill wf" style="height: 6%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET FX 4.8 (16 wf, 16.2x)</div>
    </div>
  </div>
  <div class="perf-vchart-legend">
    <div class="perf-vchart-legend-item"><div class="perf-vchart-legend-color wc"></div>Sequential Execution</div>
    <div class="perf-vchart-legend-item"><div class="perf-vchart-legend-color wf"></div>Concurrent Execution</div>
  </div>
</div>
{% endif %}

**Key Findings**:

- Near-perfect scaling: 7.9x speedup for 8 workflows, 15.7x for 16 workflows
- Memory overhead scales linearly with workflow count
- Heavier workflows (25 ops) maintain ~7.8x speedup with 8 concurrent workers

---

## Optimization Recommendations

1. **Use Custom Operations** for production—most memory-efficient (456 B per execution).
2. **Prefer Logging operations** for lightweight tasks—fastest at 14.6μs.
3. **Avoid large object allocations** in operations—triggers Gen2 GC and degrades throughput.
4. **Scale horizontally**—concurrency shows near-perfect linear scaling (7.9x for 8 workers, 15.7x for 16).
5. **Minimize allocation in hot paths**—MinimalAllocationWorkflow demonstrates constant 3,296 B footprint.
6. **Use .NET 10.0** where available—improved exception handling (94.5μs vs 134.5μs) and some operation gains.

---

## Related Documentation

- [Performance Overview](performance.md) - Summary and production targets
- [Competitive Analysis](competitive-analysis.md) - Head-to-head comparisons with Workflow Core and Elsa Workflows
