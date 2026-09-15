---
title: Internal Benchmarks
description: "Internal BenchmarkDotNet results for WorkflowForge: microsecond execution, small allocations, and concurrency scaling."
---

# WorkflowForge Internal Benchmarks

WorkflowForge-only BenchmarkDotNet results: per-operation cost, throughput sweeps, memory, concurrency. No competitor mix-ins.

**Version**: 2.2.0  
**Test System**: Windows 11 (25H2), Intel 11th Gen i7-1185G7, .NET SDK 10.0.103  
**Runtimes**: .NET 10.0.3, .NET 8.0.24, .NET Framework 4.8.1  
**Benchmark Framework**: BenchmarkDotNet v0.15.8  
**Methodology**: 10 iterations per benchmark job  
**Last Updated**: September 15, 2026

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

Median numbers (10 iterations per job):

| Metric | Result |
|--------|--------|
| **Operation Execution** | 73.6ns–9.5μs median (excluding delays) |
| **Operation Creation** | 48.1ns median |
| **Workflow Throughput** | ~4–211μs for custom operations (1–50 ops, .NET 8/10) |
| **Memory Baseline** | 4,126 B minimal allocation (constant) |
| **Concurrency Scaling** | Roughly linear (8.0x for 8 workers, 15.9x for 16 workers) |
| **GC Pressure** | Gen0 only for typical workloads |

{% if site.url %}
<div class="perf-stats">
  <div class="perf-stat">
    <div class="perf-stat-value">33.9μs</div>
    <div class="perf-stat-label">Max CPU-bound Op</div>
  </div>
  <div class="perf-stat">
    <div class="perf-stat-value">4.03KB</div>
    <div class="perf-stat-label">Minimal Footprint</div>
  </div>
  <div class="perf-stat">
    <div class="perf-stat-value">15.9x</div>
    <div class="perf-stat-label">Concurrency Speedup</div>
  </div>
  <div class="perf-stat">
    <div class="perf-stat-value">48.1ns</div>
    <div class="perf-stat-label">Op Creation</div>
  </div>
</div>
{% endif %}

---

## Operation Performance

Per-operation timings and allocations on .NET 8.0, 10.0, and .NET Framework 4.8.

### Operation Execution (Median Times)

| Operation Type | .NET 8.0 | .NET 10.0 | .NET FX 4.8 | Allocated (.NET 8) |
|----------------|----------|-----------|-------------|--------------------|
| LoggingOperationExecution | 76.2ns | 73.6ns | 216.2ns | 136 B |
| ConditionalOperationFalse | 1.9μs | 1.9μs | 6.1μs | 1,008 B |
| ConditionalOperationTrue | 1.9μs | 1.9μs | 6.1μs | 952 B |
| CustomOperationExecution | 1.3μs | 1.3μs | 3.5μs | 392 B |
| DelegateOperationExecution | 1.4μs | 1.5μs | 3.9μs | 552 B |
| ActionOperationExecution | 1.5μs | 1.5μs | 4.0μs | 584 B |
| ForEachSmallCollection | 2.6μs | 2.5μs | 7.5μs | 2,279 B |
| ForEachLargeCollection | 7.1μs | 4.7μs | 32.3μs | 7,073 B |
| WithRestoration | 2.1μs | 2.1μs | 7.8μs | 568 B |
| DataManipulation | 23.9μs | 15.5μs | 57.1μs | 8,536 B |
| ChainedOperations | 10.0μs | 9.5μs | 32.3μs | 5,378 B |
| ExceptionHandling | 33.9μs | 16.6μs | 43.6μs | 2,560 B |
| DelayOperationExecution | 16.1ms | 16.1ms | 16.1ms | 752 B |

*DelayOperationExecution contains a 1ms delay; .NET FX 4.8 does not report allocation metrics.*

### Operation Creation (Median Times)

| Operation Type | .NET 8.0 | .NET 10.0 | .NET FX 4.8 | Allocated (.NET 8) |
|----------------|----------|-----------|-------------|--------------------|
| DelegateCreation | 51.2ns | 51.0ns | 67.9ns | 56 B |
| ActionCreation | 51.2ns | 51.0ns | 67.9ns | 56 B |
| CustomCreation | 50.6ns | 48.1ns | 59.7ns | 32 B |

{% if site.url %}
<div class="perf-vchart">
  <div class="perf-vchart-title">Operation Execution Times (Median, Lower is Better)</div>
  <div class="perf-vchart-subtitle">CPU-bound ops stay under 43.6μs median in this slice</div>
  <div class="perf-vchart-container">
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">73.6ns</div><div class="perf-vchart-fill wf" style="height: 15%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">1.5μs</div><div class="perf-vchart-fill wf" style="height: 27%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">1.3μs</div><div class="perf-vchart-fill wf" style="height: 23%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">1.5μs</div><div class="perf-vchart-fill wf" style="height: 26%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">2.5μs</div><div class="perf-vchart-fill wf" style="height: 43%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">4.7μs</div><div class="perf-vchart-fill wf" style="height: 62%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">9.5μs</div><div class="perf-vchart-fill wf" style="height: 83%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">16.6μs</div><div class="perf-vchart-fill wf" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 10.0</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">76.2ns</div><div class="perf-vchart-fill wf" style="height: 15%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">1.4μs</div><div class="perf-vchart-fill wf" style="height: 24%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">1.3μs</div><div class="perf-vchart-fill wf" style="height: 21%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">1.5μs</div><div class="perf-vchart-fill wf" style="height: 25%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">2.6μs</div><div class="perf-vchart-fill wf" style="height: 38%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">7.1μs</div><div class="perf-vchart-fill wf" style="height: 62%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">10.0μs</div><div class="perf-vchart-fill wf" style="height: 70%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">33.9μs</div><div class="perf-vchart-fill wf" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 8.0</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">216.2ns</div><div class="perf-vchart-fill wf" style="height: 15%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">3.9μs</div><div class="perf-vchart-fill wf" style="height: 46%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">3.5μs</div><div class="perf-vchart-fill wf" style="height: 43%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">4.0μs</div><div class="perf-vchart-fill wf" style="height: 46%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">7.5μs</div><div class="perf-vchart-fill wf" style="height: 60%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">32.3μs</div><div class="perf-vchart-fill wf" style="height: 93%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">32.3μs</div><div class="perf-vchart-fill wf" style="height: 93%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">43.6μs</div><div class="perf-vchart-fill wf" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET FX 4.8</div>
    </div>
  </div>
  <div class="perf-vchart-legend">
    <div class="perf-vchart-legend-item"><div class="perf-vchart-legend-color wf"></div>WorkflowForge (Logging → Delegate → Custom → Action → ForEach S → ForEach L → Chained → Exception)</div>
  </div>
</div>
{% endif %}

**Observations**

- Custom ops remain the cheapest allocation (392 B here).
- Logging ops are fastest (73.6ns–216.2ns).
- Construction costs ~48–68ns median.
- Exception handling is visible; .NET 10.0 trims it (16.6μs vs 33.9μs on .NET 8.0 for the same benchmark).

---

## Workflow Throughput

End-to-end workflow shapes with increasing operation counts. Delay-heavy cases still include the intentional ~1ms sleeps.

### Workflow Patterns (OperationCount=1, Median by Runtime)

| Pattern | .NET 8.0 | .NET 10.0 | .NET FX 4.8 | Memory (.NET 8) | Notes |
|---------|----------|-----------|-------------|-----------------|-------|
| SequentialCustomOperations | 4.1μs | 4.0μs | 8.7μs | 3,819 B | CPU-bound |
| HighPerformanceConfiguration | 4.6μs | 4.3μs | 11.2μs | 4,392 B | CPU-bound |
| ForEachLoopWorkflow | 6.3μs | 5.9μs | 15.1μs | 5,888 B | CPU-bound |
| SequentialDelegateOperations | 15.9ms | 15.9ms | 15.9ms | 4,587 B | Delay-bound |
| DataPassingWorkflow | 15.9ms | 15.9ms | 15.9ms | 4,741 B | Delay-bound |
| ConditionalOperationsWorkflow | 15.9ms | 15.9ms | 15.9ms | 5,079 B | Delay-bound |
| LoggingOperationsWorkflow | 15.9ms | 15.9ms | 15.9ms | 5,068 B | Delay-bound |
| MemoryIntensiveWorkflow | 15.9ms | 15.9ms | 15.8ms | 5,591 B | Delay-bound |

*Memory column shows .NET 8.0 allocation; .NET FX 4.8 allocation metrics are NA.*

### Throughput Scaling (SequentialCustomOperations, Median by Runtime)

| Operations | .NET 8.0 | .NET 10.0 | .NET FX 4.8 | Memory (.NET 8) |
|------------|----------|-----------|-------------|-----------------|
| 1 | 4.1μs | 4.0μs | 8.7μs | 3,819 B |
| 5 | 34.9μs | 32.2μs | 65.1μs | 7,761 B |
| 10 | 50.2μs | 47.6μs | 99.0μs | 12,677 B |
| 25 | 57.1μs | 53.1μs | 175.7μs | 29,685 B |
| 50 | 109.8μs | 98.2μs | 320.8μs | 58,521 B |

{% if site.url %}
<div class="perf-vchart">
  <div class="perf-vchart-title">Custom Operation Throughput Scaling (1-50 Operations)</div>
  <div class="perf-vchart-subtitle">50 custom ops finish under 321μs median; memory rises linearly with op count</div>
  <div class="perf-vchart-container">
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">4.0μs</div><div class="perf-vchart-fill wf" style="height: 15%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">32.2μs</div><div class="perf-vchart-fill wf" style="height: 70%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">47.6μs</div><div class="perf-vchart-fill wf" style="height: 81%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">53.1μs</div><div class="perf-vchart-fill wf" style="height: 84%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">98.2μs</div><div class="perf-vchart-fill wf" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 10.0</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">4.1μs</div><div class="perf-vchart-fill wf" style="height: 15%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">34.9μs</div><div class="perf-vchart-fill wf" style="height: 70%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">50.2μs</div><div class="perf-vchart-fill wf" style="height: 80%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">57.1μs</div><div class="perf-vchart-fill wf" style="height: 83%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">109.8μs</div><div class="perf-vchart-fill wf" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 8.0</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">8.7μs</div><div class="perf-vchart-fill wf" style="height: 15%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">65.1μs</div><div class="perf-vchart-fill wf" style="height: 62%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">99.0μs</div><div class="perf-vchart-fill wf" style="height: 72%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">175.7μs</div><div class="perf-vchart-fill wf" style="height: 86%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">320.8μs</div><div class="perf-vchart-fill wf" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET FX 4.8</div>
    </div>
  </div>
  <div class="perf-vchart-legend">
    <div class="perf-vchart-legend-item"><div class="perf-vchart-legend-color wf"></div>SequentialCustomOperations (1→5→10→25→50 ops)</div>
  </div>
</div>
{% endif %}

**Numbers**:

- CPU-bound workflows finish in about 4–321μs for 1–50 operations across the tested runtimes.
- Allocations grow in step with operation count in the sequential custom-op sweep.
- Delay-heavy rows are dominated by the sleeps, not the orchestration loop.

---

## Memory Allocation

Allocation and GC behavior (median over 10 allocations unless noted).

### Allocation Patterns (10 Allocations, Median by Runtime)

| Pattern | .NET 8.0 | .NET 10.0 | .NET FX 4.8 | Memory (.NET 8) | GC |
|---------|----------|-----------|-------------|-----------------|-----|
| MinimalAllocationWorkflow | 3.5μs | 3.4μs | 7.6μs | 4,126 B | — |
| SmallObjectAllocation | 49.3μs | 44.0μs | 105.6μs | 20,398 B | — |
| StringConcatenationAllocation | 48.2μs | 42.1μs | 102.1μs | 18,370 B | — |
| StringBuilderOptimization | 45.3μs | 40.8μs | 101.2μs | 17,489 B | — |
| CollectionAllocation | 49.7μs | 46.2μs | 108.7μs | 19,384 B | — |
| ObjectPoolingSimulation | 57.0μs | 52.7μs | 115.9μs | 23,040 B | — |
| ArrayReuseOptimization | 35.6μs | 30.9μs | 139.4μs | 24,381 B | — |
| MemoryPressureScenario | 195.6μs | 175.0μs | 243.9μs | 319,518 B | — |
| LargeObjectAllocation | 591.3μs | 521.1μs | 694.0μs | 1,020,293 B | — |
| DisposableResourceManagement | 160.8ms | 160.7ms | 161.2ms | 21,278 B | — |

*Memory column shows .NET 8.0 allocation; .NET Framework 4.8 allocation metrics are NA (Allocated column shows "—" in benchmarks).*

### Memory Scaling (MinimalAllocationWorkflow, .NET 8.0)

| Allocations | .NET 8.0 Memory | .NET 10.0 Memory |
|-------------|----------------|-----------------|
| 10 | 4,126 B | 4,147 B |
| 50 | 4,126 B | 4,147 B |
| 100 | 4,126 B | 4,147 B |
| 500 | 4,126 B | 4,147 B |

The minimal allocation workflow holds a **flat 4,126 B** across 10–500 allocations in this benchmark, which matches the table above. .NET Framework 4.8 does not report allocation metrics here.

{% if site.url %}
<div class="perf-vchart">
  <div class="perf-vchart-title">Memory Allocation Patterns (10 Allocations)</div>
  <div class="perf-vchart-subtitle">Minimal workflow holds ~4.03KB; large-object path stresses Gen2. .NET FX 4.8 skips alloc metrics.</div>
  <div class="perf-vchart-container">
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">4.05KB</div><div class="perf-vchart-fill wf" style="height: 1%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">17.11KB</div><div class="perf-vchart-fill wf" style="height: 27%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">19.94KB</div><div class="perf-vchart-fill wf" style="height: 30%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">23.83KB</div><div class="perf-vchart-fill wf" style="height: 33%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">312KB</div><div class="perf-vchart-fill wc" style="height: 79%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">996.4KB</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 10.0</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">4.03KB</div><div class="perf-vchart-fill wf" style="height: 1%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">17.08KB</div><div class="perf-vchart-fill wf" style="height: 27%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">19.92KB</div><div class="perf-vchart-fill wf" style="height: 30%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">23.81KB</div><div class="perf-vchart-fill wf" style="height: 33%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">312KB</div><div class="perf-vchart-fill wc" style="height: 79%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">996.38KB</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 8.0</div>
    </div>
  </div>
  <div class="perf-vchart-legend">
    <div class="perf-vchart-legend-item"><div class="perf-vchart-legend-color wf"></div>Typical (no GC pressure)</div>
    <div class="perf-vchart-legend-item"><div class="perf-vchart-legend-color wc"></div>Elevated</div>
    <div class="perf-vchart-legend-item"><div class="perf-vchart-legend-color elsa"></div>High (Gen0/1/2)</div>
  </div>
</div>
{% endif %}

**Observations**

- Still 4,126 B from 10 through 500 iterations in the minimal row.
- Typical patterns skipped GC pressure in the minimal scenario.
- `LargeObjectAllocation` hits Gen0+1+2 as expected.
- `StringBuilderOptimization` saves ~881 B vs raw concatenation at ten passes (17,489 B vs 18,370 B).

---

## Concurrency Scaling

Concurrent workflow fan-out versus sequential baselines on .NET 8.0, 10.0, and .NET Framework 4.8.

### Scaling with 8 Workflows (5 ops per workflow)

| Pattern | .NET 8.0 | .NET 10.0 | .NET FX 4.8 | Memory (.NET 8) | Speedup |
|---------|----------|-----------|-------------|-----------------|---------|
| SequentialWorkflows | 626.1 ms | 629.2 ms | 641.6 ms | 86.25 KB | — |
| ConcurrentWorkflows | 78.9 ms | 78.8 ms | 79.2 ms | 89.25 KB | 8.0x |
| ParallelWorkflows | 78.8 ms | 78.8 ms | 79.2 ms | 89.2 KB | 8.0x |

### Scaling with 16 Workflows (5 ops per workflow)

| Pattern | .NET 8.0 | .NET 10.0 | .NET FX 4.8 | Memory (.NET 8) | Speedup |
|---------|----------|-----------|-------------|-----------------|---------|
| SequentialWorkflows | 1,239 ms | 1,255 ms | 1,289 ms | 172.21 KB | — |
| ConcurrentWorkflows | 79.0 ms | 79.1 ms | 79.6 ms | 178.09 KB | 15.9x |

### Scaling with 8 Workflows, 25 ops each

| Pattern | .NET 8.0 | .NET 10.0 | .NET FX 4.8 | Memory (.NET 8) | Speedup |
|---------|----------|-----------|-------------|-----------------|---------|
| SequentialWorkflows | 3,130 ms | 3,147 ms | 3,221 ms | 411.13 KB | — |
| ConcurrentWorkflows | 399.2 ms | 400.1 ms | 400.9 ms | 414.13 KB | 7.8x |

{% if site.url %}
<div class="perf-vchart">
  <div class="perf-vchart-title">Concurrency Scaling (Sequential vs Concurrent Time)</div>
  <div class="perf-vchart-subtitle">8 workflows ≈8.0x, 16 workflows ≈15.9x vs sequential in this harness</div>
  <div class="perf-vchart-container">
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">629ms</div><div class="perf-vchart-fill wc" style="height: 50%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">79ms</div><div class="perf-vchart-fill wf" style="height: 6%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 10.0 (8 wf, 8.0x)</div>
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
        <div class="perf-vchart-bar"><div class="perf-vchart-val">626ms</div><div class="perf-vchart-fill wc" style="height: 50%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">79ms</div><div class="perf-vchart-fill wf" style="height: 6%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 8.0 (8 wf, 8.0x)</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">1.24s</div><div class="perf-vchart-fill wc" style="height: 100%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">79ms</div><div class="perf-vchart-fill wf" style="height: 6%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 8.0 (16 wf, 15.7x)</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">642ms</div><div class="perf-vchart-fill wc" style="height: 50%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">79ms</div><div class="perf-vchart-fill wf" style="height: 6%;"></div></div>
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

**What matters**:

- Measured speedups land at 8.0x for eight workflows and 15.9x for sixteen versus sequential baselines in this harness.
- Reported memory rises in proportion to workflow count.
- With 25 operations per workflow, eight concurrent workers still reach about 7.8x versus sequential.

---

## Optimization Recommendations

1. **Custom operations** for the smallest allocations in this matrix (392 B per execution).
2. **Logging operations** when the step is tiny; they lead the timing table at 73.6ns–216.2ns.
3. **Avoid LOH churn** in hot loops; big allocations invite Gen2 pauses.
4. **Parallelize deliberately**; scaling here is roughly linear (8.0x for 8 workers, 15.9x for 16).
5. **Reuse buffers and properties**; `MinimalAllocationWorkflow` flatlines at 4,126 B from 10 through 500 iterations.
6. **Use .NET 10.0** where you can; several ops and exception paths improve vs .NET 8.0 in the same harness.

---

## Related Documentation

- [Performance Overview](performance.md)
- [Competitive Analysis](competitive-analysis.md)
