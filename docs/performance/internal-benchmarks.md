---
title: Internal Benchmarks
description: Internal performance benchmarks validating WorkflowForge's microsecond execution, minimal allocations, and linear scaling characteristics.
---

# WorkflowForge Internal Benchmarks

This document presents WorkflowForge's internal performance benchmarks—comprehensive self-testing that validates the framework's performance characteristics independently of competitor comparisons.

**Version**: 2.1.1  
**Test System**: Windows 11 (25H2), Intel 11th Gen i7-1185G7, .NET SDK 10.0.103  
**Runtimes**: .NET 10.0.3, .NET 8.0.24, .NET Framework 4.8.1  
**Benchmark Framework**: BenchmarkDotNet v0.15.8  
**Methodology**: 50 iterations per benchmark, 5 warmup iterations  
**Last Updated**: March 6, 2026

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
| **Operation Execution** | 8.8-82μs median (excluding delays) |
| **Operation Creation** | 1.2-1.9μs median |
| **Workflow Throughput** | 35-272μs for custom operations (1-50 ops, all runtimes) |
| **Memory Baseline** | 3,408 B minimal allocation (constant) |
| **Concurrency Scaling** | Near-perfect (8.0x for 8 workers, 15.9x for 16 workers) |
| **GC Pressure** | Gen0 only for typical workloads |

{% if site.url %}
<div class="perf-stats">
  <div class="perf-stat">
    <div class="perf-stat-value">82μs</div>
    <div class="perf-stat-label">Max CPU-bound Op</div>
  </div>
  <div class="perf-stat">
    <div class="perf-stat-value">3.3KB</div>
    <div class="perf-stat-label">Minimal Footprint</div>
  </div>
  <div class="perf-stat">
    <div class="perf-stat-value">15.9x</div>
    <div class="perf-stat-label">Concurrency Speedup</div>
  </div>
  <div class="perf-stat">
    <div class="perf-stat-value">1.9μs</div>
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
| LoggingOperationExecution | 12.1μs | 10.9μs | 8.8μs | 1,912 B |
| ConditionalOperationFalse | 34.3μs | 33.2μs | 29.2μs | 1,072 B |
| ConditionalOperationTrue | 34.7μs | 33.2μs | 31.6μs | 1,016 B |
| CustomOperationExecution | 33.7μs | 33.8μs | 27.9μs | 456 B |
| DelegateOperationExecution | 33.2μs | 42.6μs | 29.0μs | 616 B |
| ActionOperationExecution | 42.6μs | 42.1μs | 29.9μs | 648 B |
| ForEachSmallCollection | 44.7μs | 49.1μs | 32.7μs | 2,336 B |
| ForEachLargeCollection | 63.5μs | 70.2μs | 50.2μs | 7,128 B |
| WithRestoration | 50.7μs | 45.5μs | 29.7μs | 568 B |
| DataManipulation | 61.1μs | 55.1μs | 64.6μs | 8,536 B |
| ChainedOperations | 79.5μs | 78.0μs | 54.9μs | 4,720 B |
| ExceptionHandling | 81.7μs | 59.3μs | 66.3μs | 2,944 B |
| DelayOperationExecution | 15,192μs | 15,132μs | 15,353μs | 1,432 B |

*DelayOperationExecution contains a 1ms delay; .NET FX 4.8 does not report allocation metrics.*

### Operation Creation (Median Times)

| Operation Type | .NET 8.0 | .NET 10.0 | .NET FX 4.8 | Allocated (.NET 8) |
|----------------|----------|-----------|-------------|--------------------|
| DelegateCreation | 1.9μs | 1.6μs | 1.6μs | 56 B |
| ActionCreation | 1.9μs | 1.7μs | 1.3μs | 56 B |
| CustomCreation | 1.7μs | 1.4μs | 1.2μs | 32 B |

{% if site.url %}
<div class="perf-vchart">
  <div class="perf-vchart-title">Operation Execution Times (Median, Lower is Better)</div>
  <div class="perf-vchart-subtitle">CPU-bound operations execute in under 82 microseconds across all runtimes</div>
  <div class="perf-vchart-container">
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">10.9μs</div><div class="perf-vchart-fill wf" style="height: 13%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">42.6μs</div><div class="perf-vchart-fill wf" style="height: 52%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">33.8μs</div><div class="perf-vchart-fill wf" style="height: 41%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">42.1μs</div><div class="perf-vchart-fill wf" style="height: 51%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">49.1μs</div><div class="perf-vchart-fill wf" style="height: 60%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">70.2μs</div><div class="perf-vchart-fill wf" style="height: 86%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">78μs</div><div class="perf-vchart-fill wf" style="height: 95%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">59.3μs</div><div class="perf-vchart-fill wf" style="height: 72%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 10.0</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">12.1μs</div><div class="perf-vchart-fill wf" style="height: 15%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">33.2μs</div><div class="perf-vchart-fill wf" style="height: 40%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">33.7μs</div><div class="perf-vchart-fill wf" style="height: 41%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">42.6μs</div><div class="perf-vchart-fill wf" style="height: 52%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">44.7μs</div><div class="perf-vchart-fill wf" style="height: 55%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">63.5μs</div><div class="perf-vchart-fill wf" style="height: 77%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">79.5μs</div><div class="perf-vchart-fill wf" style="height: 97%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">81.7μs</div><div class="perf-vchart-fill wf" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 8.0</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">8.8μs</div><div class="perf-vchart-fill wf" style="height: 11%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">29μs</div><div class="perf-vchart-fill wf" style="height: 35%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">27.9μs</div><div class="perf-vchart-fill wf" style="height: 34%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">29.9μs</div><div class="perf-vchart-fill wf" style="height: 36%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">32.7μs</div><div class="perf-vchart-fill wf" style="height: 40%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">50.2μs</div><div class="perf-vchart-fill wf" style="height: 61%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">54.9μs</div><div class="perf-vchart-fill wf" style="height: 67%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">66.3μs</div><div class="perf-vchart-fill wf" style="height: 81%;"></div></div>
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
- Logging operations are fastest (8.8-12.1μs)
- Operation creation is extremely fast (1.2-1.9μs)
- Exception handling adds moderate overhead; .NET 10.0 improves it significantly (59.3μs vs 81.7μs)

---

## Workflow Throughput

Tests complete workflow execution patterns with varying operation counts. Delay-bound workflows contain built-in delay operations (~1ms per operation).

### Workflow Patterns (OperationCount=1, Median by Runtime)

| Pattern | .NET 8.0 | .NET 10.0 | .NET FX 4.8 | Memory (.NET 8) | Notes |
|---------|----------|-----------|-------------|-----------------|-------|
| SequentialCustomOperations | 50.6μs | 47.6μs | 34.6μs | 3,096 B | CPU-bound |
| HighPerformanceConfiguration | 53.6μs | 52.2μs | 33.0μs | 3,672 B | CPU-bound |
| ForEachLoopWorkflow | 60.8μs | 59.2μs | 39.0μs | 5,176 B | CPU-bound |
| SequentialDelegateOperations | 15,047μs | 15,086μs | 15,313μs | 3,880 B | Delay-bound |
| DataPassingWorkflow | 15,066μs | 15,068μs | 15,354μs | 4,024 B | Delay-bound |
| ConditionalOperationsWorkflow | 15,136μs | 15,096μs | 15,271μs | 4,352 B | Delay-bound |
| LoggingOperationsWorkflow | 15,059μs | 15,115μs | 15,302μs | 6,184 B | Delay-bound |
| MemoryIntensiveWorkflow | 15,036μs | 15,170μs | 15,331μs | 4,856 B | Delay-bound |

*Memory column shows .NET 8.0 allocation; .NET FX 4.8 allocation metrics are NA.*

### Throughput Scaling (SequentialCustomOperations, Median by Runtime)

| Operations | .NET 8.0 | .NET 10.0 | .NET FX 4.8 | Memory (.NET 8) |
|------------|----------|-----------|-------------|-----------------|
| 1 | 50.6μs | 47.6μs | 34.6μs | 3,096 B |
| 5 | 74.4μs | 72.0μs | 67.3μs | 7,120 B |
| 10 | 88.6μs | 88.3μs | 99.1μs | 12,192 B |
| 25 | 148.7μs | 150.2μs | 154.0μs | 29,688 B |
| 50 | 215.3μs | 211.1μs | 264.3μs | 59,328 B |

{% if site.url %}
<div class="perf-vchart">
  <div class="perf-vchart-title">Custom Operation Throughput Scaling (1-50 Operations)</div>
  <div class="perf-vchart-subtitle">Sub-272μs execution at 50 operations with linear memory growth across runtimes</div>
  <div class="perf-vchart-container">
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">47.6μs</div><div class="perf-vchart-fill wf" style="height: 18%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">72μs</div><div class="perf-vchart-fill wf" style="height: 27%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">88.3μs</div><div class="perf-vchart-fill wf" style="height: 33%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">150.2μs</div><div class="perf-vchart-fill wf" style="height: 57%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">211.1μs</div><div class="perf-vchart-fill wf" style="height: 80%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 10.0</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">50.6μs</div><div class="perf-vchart-fill wf" style="height: 19%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">74.4μs</div><div class="perf-vchart-fill wf" style="height: 28%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">88.6μs</div><div class="perf-vchart-fill wf" style="height: 34%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">148.7μs</div><div class="perf-vchart-fill wf" style="height: 56%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">215.3μs</div><div class="perf-vchart-fill wf" style="height: 81%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 8.0</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">34.6μs</div><div class="perf-vchart-fill wf" style="height: 13%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">67.3μs</div><div class="perf-vchart-fill wf" style="height: 25%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">99.1μs</div><div class="perf-vchart-fill wf" style="height: 37%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">154μs</div><div class="perf-vchart-fill wf" style="height: 58%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">264.3μs</div><div class="perf-vchart-fill wf" style="height: 100%;"></div></div>
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

- CPU-bound workflows execute in 35-272μs for 1-50 operations across runtimes
- Memory scales linearly with operation count
- Delay-bound workflows (~1ms) are dominated by delay duration, not framework overhead

---

## Memory Allocation

Tests memory allocation patterns and GC behavior. All values are median for 10 allocations unless noted.

### Allocation Patterns (10 Allocations, Median by Runtime)

| Pattern | .NET 8.0 | .NET 10.0 | .NET FX 4.8 | Memory (.NET 8) | GC |
|---------|----------|-----------|-------------|-----------------|-----|
| MinimalAllocationWorkflow | 32.2μs | 34.6μs | 32.2μs | 3,408 B | No Gen0/1/2 |
| SmallObjectAllocation | 101.2μs | 93.3μs | 88.6μs | 19,928 B | — |
| StringConcatenationAllocation | 97.5μs | 90.1μs | 87.9μs | 17,904 B | — |
| StringBuilderOptimization | 93.0μs | 96.5μs | 93.8μs | 17,264 B | — |
| CollectionAllocation | 97.7μs | 101.3μs | 81.2μs | 18,912 B | — |
| ObjectPoolingSimulation | 124.8μs | 120.5μs | 90.6μs | 23,000 B | — |
| ArrayReuseOptimization | 121.6μs | 120.2μs | 114.0μs | 23,248 B | — |
| MemoryPressureScenario | 241.9μs | 224.1μs | 190.9μs | 319,024 B | — |
| LargeObjectAllocation | 664.1μs | 608.3μs | 622.9μs | 1,019,608 B | Gen0+Gen1+Gen2 |
| DisposableResourceManagement | 159,520μs | 159,606μs | 160,583μs | 20,784 B | Delay-bound |

*Memory column shows .NET 8.0 allocation; .NET Framework 4.8 allocation metrics are NA (Allocated column shows "—" in benchmarks).*

### Memory Scaling (MinimalAllocationWorkflow, .NET 8.0)

| Allocations | .NET 8.0 Memory | .NET 10.0 Memory |
|-------------|----------------|-----------------|
| 10 | 3,408 B | 3,408 B |
| 50 | 3,408 B | 3,408 B |
| 100 | 3,408 B | 3,408 B |
| 500 | 3,408 B | 3,408 B |

The minimal allocation workflow maintains a **constant footprint** of 3,408 B regardless of allocation count—demonstrating effective object reuse and pooling within the framework. .NET Framework 4.8 does not report allocation metrics.

{% if site.url %}
<div class="perf-vchart">
  <div class="perf-vchart-title">Memory Allocation Patterns (10 Allocations)</div>
  <div class="perf-vchart-subtitle">Minimal workflow stays at 3.3KB; large objects trigger full GC. .NET FX 4.8 allocation metrics NA.</div>
  <div class="perf-vchart-container">
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">3.3KB</div><div class="perf-vchart-fill wf" style="height: 1%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">17.3KB</div><div class="perf-vchart-fill wf" style="height: 5%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">19.9KB</div><div class="perf-vchart-fill wf" style="height: 6%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">23.2KB</div><div class="perf-vchart-fill wf" style="height: 7%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">319KB</div><div class="perf-vchart-fill wc" style="height: 31%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">1.0MB</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">.NET 10.0</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">3.3KB</div><div class="perf-vchart-fill wf" style="height: 1%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">17.3KB</div><div class="perf-vchart-fill wf" style="height: 5%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">19.9KB</div><div class="perf-vchart-fill wf" style="height: 6%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">23.2KB</div><div class="perf-vchart-fill wf" style="height: 7%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">319KB</div><div class="perf-vchart-fill wc" style="height: 31%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">1.0MB</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
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

**Key Findings**:

- Minimal allocation baseline is constant at 3,408 B across 10-500 allocations
- No GC pressure for typical workflows
- Large object allocations trigger full GC (Gen0/1/2)
- StringBuilder optimization saves ~640 B vs concatenation at 10 allocations (17,264 B vs 17,904 B)

---

## Concurrency Scaling

Tests concurrent workflow execution patterns across .NET 8.0, .NET 10.0, and .NET Framework 4.8.

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
  <div class="perf-vchart-subtitle">Near-perfect linear scaling across runtimes: 8.0x for 8 workflows, 15.9x for 16 workflows</div>
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

**Key Findings**:

- Near-perfect scaling: 8.0x speedup for 8 workflows, 15.9x for 16 workflows
- Memory overhead scales linearly with workflow count
- Heavier workflows (25 ops) maintain ~7.8x speedup with 8 concurrent workers

---

## Optimization Recommendations

1. **Use Custom Operations** for production—most memory-efficient (456 B per execution).
2. **Prefer Logging operations** for lightweight tasks—fastest at 8.8-12.1μs.
3. **Avoid large object allocations** in operations—triggers Gen2 GC and degrades throughput.
4. **Scale horizontally**—concurrency shows near-perfect linear scaling (8.0x for 8 workers, 15.9x for 16).
5. **Minimize allocation in hot paths**—MinimalAllocationWorkflow demonstrates constant 3,408 B footprint.
6. **Use .NET 10.0** where available—improved exception handling (59.3μs vs 81.7μs) and some operation gains.

---

## Related Documentation

- [Performance Overview](performance.md) - Summary and production targets
- [Competitive Analysis](competitive-analysis.md) - Head-to-head comparisons with Workflow Core and Elsa Workflows
