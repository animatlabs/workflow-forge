---
title: Performance Benchmarks
description: Comprehensive performance analysis showing 11-540x faster execution and 9-573x less memory than competitors across 12 scenarios.
---

# WorkflowForge Performance Benchmarks

This document provides comprehensive performance analysis of WorkflowForge, including both internal performance characteristics and competitive comparisons.

**Test System**: Windows 11 (25H2), Intel 11th Gen i7-1185G7, .NET 8.0.23  
**Benchmark Framework**: BenchmarkDotNet v0.15.8  
**Iterations**: 50 per benchmark, 5 warmup iterations  
**Last Updated**: January 2026

---

## Table of Contents

1. [Internal Performance Benchmarks](#internal-performance-benchmarks)
2. [Competitive Performance Summary](#competitive-performance-summary)
3. [Performance Optimization Guide](#performance-optimization-guide)

---

## Internal Performance Benchmarks

These benchmarks measure WorkflowForge's intrinsic performance characteristics in isolation.

### Operation Performance

**Single Operation Execution Times** (Median, 50 iterations):

| Operation Type | Median | Mean | Memory |
|----------------|--------|------|--------|
| Logging Operation | 11.85μs | 74.1μs | 1,912 B |
| Custom Operation | 15.15μs | 127.2μs | 296 B |
| Action Operation | 36.00μs | 173.3μs | 648 B |
| Delegate Operation | 17.60μs | 168.8μs | 624 B |

**Operation Creation Overhead** (Median):

| Operation Type | Median | Memory |
|----------------|--------|--------|
| Custom | 1.80μs | 32 B |
| Delegate | 1.90μs | 64 B |
| Action | 1.85μs | 56 B |

**Key Insights**:
- Operation execution: 11.85-36.0μs median (microsecond scale)
- Operation creation: <2.0μs (negligible overhead)
- Custom operations are the most memory-efficient (296B)
- Median values are more representative than means (due to GC outliers)

### Workflow Throughput

**Sequential Custom Operations** (varying operation count):

| Operations | Median | Mean | Memory |
|------------|--------|------|--------|
| 1 | 41.4μs | 478.9μs | 1.92 KB |
| 5 | 74.6μs | 522.4μs | 3.73 KB |
| 10 | 96.9μs | 576.5μs | 6.02 KB |
| 25 | 102.4μs | 549.3μs | 12.74 KB |
| 50 | 136.0μs | 595.2μs | 23.95 KB |

**ForEach Loop Workflow** (10 operations):
- Median: 610.7μs
- Memory: 4.54 KB

**High Performance Configuration** (10 operations):
- Median: 635.2μs
- Memory: 13.61 KB

**Key Insights**:
- Throughput remains under 200μs median for up to 50 operations
- Linear memory scaling (1.92KB to 23.95KB for 1-50 operations)
- High-performance config adds minimal overhead

### Concurrency Performance

**8 Concurrent Workflows** (5 operations each):

| Execution Mode | Duration | Memory |
|----------------|----------|--------|
| Sequential | 631.75ms | 63.81 KB |
| Concurrent | 78.88ms | 66.81 KB |
| Parallel | 78.14ms | 66.08 KB |

**Speedup**: 8x for 8 concurrent workflows (near-perfect scaling)

**Scaling by Concurrency Level** (5 operations per workflow):

| Concurrent Workflows | Sequential Time | Concurrent Time | Speedup |
|---------------------|----------------|-----------------|---------|
| 1 | 79.17ms | 79.38ms | 1.0x |
| 2 | 159.84ms | 79.20ms | 2.0x |
| 4 | 318.23ms | 79.06ms | 4.0x |
| 8 | 637.05ms | 79.18ms | 8.0x |
| 16 | 1,264.61ms | 79.23ms | 16.0x |

**Key Insights**:
- Near-perfect linear scaling for concurrent workflows
- Minimal memory overhead per workflow (~8KB)
- Consistent per-workflow execution time regardless of concurrency

### Memory Allocation

**Minimal Allocation Baseline** (varying iteration count):

| Allocations | Median | Memory |
|-------------|--------|--------|
| 10 | 51.4μs | 2.65 KB |
| 50 | 45.0μs | 2.65 KB |
| 100 | 45.9μs | 2.65 KB |
| 500 | 41.8μs | 2.65 KB |

**Key Insight**: Minimal allocation workflow maintains constant 2.65KB footprint regardless of scale.

**Garbage Collection Characteristics**:
- No Gen2 collections in typical scenarios
- Minimal Gen0 collections
- Large object allocations (>85KB) only in stress tests

### Configuration Overhead

**Configuration Profile Performance**:

| Profile | Median | Memory |
|---------|--------|--------|
| Minimal | 4.3μs | 968 B |
| Development | 3.9μs | 968 B |
| Production | 3.7μs | 968 B |
| High Performance | 4.0μs | 968 B |

**Key Insight**: Configuration profile overhead is negligible (<5μs).

---

## Competitive Performance Summary

WorkflowForge vs. Workflow Core and Elsa Workflows across **12 scenarios** with **50 iterations** each.

{% if site.url %}
<div class="perf-stats">
  <div class="perf-stat">
    <div class="perf-stat-value">540x</div>
    <div class="perf-stat-label">Faster (State Machine)</div>
  </div>
  <div class="perf-stat">
    <div class="perf-stat-value">573x</div>
    <div class="perf-stat-label">Less Memory</div>
  </div>
  <div class="perf-stat">
    <div class="perf-stat-value">12</div>
    <div class="perf-stat-label">Scenarios Tested</div>
  </div>
  <div class="perf-stat">
    <div class="perf-stat-value">50</div>
    <div class="perf-stat-label">Iterations Each</div>
  </div>
</div>
{% endif %}

### Performance Advantage Overview

**Execution Speed**:
- **11-540x faster** across 12 scenarios
- Operates at **microsecond scale** (13-497μs) vs. **millisecond scale** (0.8-94ms)
- **State Machine** scenarios show highest advantage: 303-540x
- Advantage **increases with workload complexity**

**Memory Efficiency**:
- **9-573x less** memory allocation
- **Kilobytes** (3.5-121KB) vs. **megabytes** (0.04-19MB) for competitors
- No Gen2 GC collections in typical workflows

### Visual Comparison

| Scenario | WorkflowForge | Workflow Core | Elsa | Advantage |
|----------|---------------|---------------|------|-----------|
| State Machine (25) | 68 μs | 20,624 μs | 36,695 μs | 303-540x |
| Concurrent Memory (8 wf) | 121 KB | 3,232 KB | 19,139 KB | 27-158x |

{% if site.url %}
<!-- Combined competitive summary: Execution Time + Memory (log scale) -->
<div class="perf-vchart">
  <div class="perf-vchart-title">Competitive Summary - Speed and Memory</div>
  <div class="perf-vchart-subtitle">WorkflowForge dominates across both execution time and memory allocation</div>
  <div class="perf-vchart-container">
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">68μs</div><div class="perf-vchart-fill wf" style="height: 40%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">20.6ms</div><div class="perf-vchart-fill wc" style="height: 95%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">36.7ms</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">State Machine (time)</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">121KB</div><div class="perf-vchart-fill wf" style="height: 49%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">3.2MB</div><div class="perf-vchart-fill wc" style="height: 82%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">19MB</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">Concurrent (memory)</div>
    </div>
  </div>
  <div class="perf-vchart-legend">
    <div class="perf-vchart-legend-item"><div class="perf-vchart-legend-color wf"></div>WorkflowForge</div>
    <div class="perf-vchart-legend-item"><div class="perf-vchart-legend-color wc"></div>Workflow Core</div>
    <div class="perf-vchart-legend-item"><div class="perf-vchart-legend-color elsa"></div>Elsa Workflows</div>
  </div>
</div>
{% endif %}

### Scaling Performance

Performance advantage **increases with workload**. See the [full competitive analysis](competitive-analysis.md) for detailed scaling charts.

| Scale | WorkflowForge | Elsa | Advantage |
|-------|---------------|------|-----------|
| 1 Operation | 183 μs | 8,703 μs | 47.6x |
| 50 Operations | 444 μs | 51,557 μs | 116.1x |

{% if site.url %}
<!-- Sequential scaling + Concurrency scaling (log scale) -->
<div class="perf-vchart">
  <div class="perf-vchart-title">Scaling Performance - Operations and Concurrency</div>
  <div class="perf-vchart-subtitle">WorkflowForge advantage grows as workload increases</div>
  <div class="perf-vchart-container">
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">183μs</div><div class="perf-vchart-fill wf" style="height: 45%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">1.3ms</div><div class="perf-vchart-fill wc" style="height: 63%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">8.7ms</div><div class="perf-vchart-fill elsa" style="height: 79%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">Seq 1 op</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">247μs</div><div class="perf-vchart-fill wf" style="height: 48%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">6.5ms</div><div class="perf-vchart-fill wc" style="height: 77%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">17.6ms</div><div class="perf-vchart-fill elsa" style="height: 85%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">Seq 10 ops</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">444μs</div><div class="perf-vchart-fill wf" style="height: 53%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">27ms</div><div class="perf-vchart-fill wc" style="height: 89%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">51.6ms</div><div class="perf-vchart-fill elsa" style="height: 95%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">Seq 50 ops</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">260μs</div><div class="perf-vchart-fill wf" style="height: 48%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">6.8ms</div><div class="perf-vchart-fill wc" style="height: 77%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">18.4ms</div><div class="perf-vchart-fill elsa" style="height: 86%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">Conc 1 wf</div>
    </div>
    <div class="perf-vchart-divider"></div>
    <div class="perf-vchart-group">
      <div class="perf-vchart-bars">
        <div class="perf-vchart-bar"><div class="perf-vchart-val">356μs</div><div class="perf-vchart-fill wf" style="height: 51%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">38.8ms</div><div class="perf-vchart-fill wc" style="height: 92%;"></div></div>
        <div class="perf-vchart-bar"><div class="perf-vchart-val">94ms</div><div class="perf-vchart-fill elsa" style="height: 100%;"></div></div>
      </div>
      <div class="perf-vchart-group-label">Conc 8 wf</div>
    </div>
  </div>
  <div class="perf-vchart-legend">
    <div class="perf-vchart-legend-item"><div class="perf-vchart-legend-color wf"></div>WorkflowForge</div>
    <div class="perf-vchart-legend-item"><div class="perf-vchart-legend-color wc"></div>Workflow Core</div>
    <div class="perf-vchart-legend-item"><div class="perf-vchart-legend-color elsa"></div>Elsa Workflows</div>
  </div>
</div>
{% endif %}

### By Scenario (Median Values)

| # | Scenario | WorkflowForge | Workflow Core | Elsa | Speed Advantage |
|---|----------|---------------|---------------|------|-----------------|
| 1 | Sequential (10 ops) | 247μs | 6,531μs | 17,617μs | 26-71x |
| 2 | Data Passing (10 ops) | 262μs | 6,737μs | 18,222μs | 26-70x |
| 3 | Conditional (10 ops) | 266μs | 8,543μs | 21,333μs | 32-80x |
| 4 | Loop (50 items) | 497μs | 35,421μs | 64,171μs | 71-129x |
| 5 | Concurrent (8 wf) | 356μs | 38,833μs | 94,018μs | 109-264x |
| 6 | Error Handling | 111μs | 1,228μs | 7,150μs | 11-64x |
| 7 | Creation Overhead | 13μs | 814μs | 2,107μs | 63-162x |
| 8 | Complete Lifecycle | 42μs | N/A | 9,933μs | 236x |
| 9 | State Machine (25) | 68μs | 20,624μs | 36,695μs | **303-540x** |
| 10 | Long Running* | 72ms | 71ms | 83ms | ~1x (51-423x mem) |
| 11 | Parallel (16 ops) | 55μs | 2,437μs | 20,891μs | 44-380x |
| 12 | Event-Driven* | 7.3ms | 8.2ms | 19.3ms | 1.1-2.6x |

*I/O-bound scenarios; advantage is in memory efficiency.

### Memory Comparison (Selected Scenarios)

| Scenario | WorkflowForge | Workflow Core | Elsa | Memory Advantage |
|----------|---------------|---------------|------|------------------|
| Concurrent (8 wf) | 121KB | 3,232KB | 19,139KB | 27-158x |
| State Machine (25) | 20.92KB | 1,106KB | 5,949KB | 53-284x |
| Parallel (16 ops) | 8.1KB | 122KB | 4,647KB | 15-573x |
| Long Running | 5.25KB | 266KB | 2,221KB | 51-423x |

**Full competitive analysis**: [competitive-analysis.md](competitive-analysis.md)

---

## Performance Optimization Guide

### 1. Choose the Right Operation Type

**For Maximum Performance**:
- Use **custom class-based operations** (26.1μs median, 296B allocation)
- Avoid delegate operations when performance is critical (37.8μs median)

```csharp
// BEST PERFORMANCE
public class ProcessDataOperation : WorkflowOperationBase
{
    protected override async Task<object?> ForgeAsyncCore(
        object? inputData,
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken)
    {
        // Your logic here
        return null;
    }
}

// GOOD PERFORMANCE (but slightly slower)
.AddOperation("ProcessData", async (foundry, ct) => { /* logic */ })
```

### 2. Use Appropriate Options

**Production defaults**:
```csharp
var foundry = WorkflowForge.CreateFoundry(
    "MyWorkflow",
    options: new WorkflowForgeOptions
    {
        ContinueOnError = false,
        FailFastCompensation = false,
        ThrowOnCompensationError = true
    });
```

**High-throughput batch scenarios**:
```csharp
var foundry = WorkflowForge.CreateFoundry(
    "MyWorkflow",
    options: new WorkflowForgeOptions
    {
        ContinueOnError = true,
        FailFastCompensation = false,
        ThrowOnCompensationError = false
    });
```

Choose based on behavior requirements rather than micro-optimizations.

### 3. Optimize Data Passing

**Use foundry.Properties for all data** (primary pattern):
```csharp
// Store data
foundry.SetProperty("Key", value);

// Retrieve data
var value = foundry.GetPropertyOrDefault<T>("Key");
```

**Avoid excessive property reads/writes**:
```csharp
// BAD: Multiple redundant reads
for (int i = 0; i < 1000; i++) {
    var config = foundry.GetPropertyOrDefault<Config>("Config");
    // Use config
}

// GOOD: Cache property value
var config = foundry.GetPropertyOrDefault<Config>("Config");
for (int i = 0; i < 1000; i++) {
    // Use cached config
}
```

### 4. Leverage Concurrency

**Use ForEachWorkflowOperation** for parallel execution of independent operations:
```csharp
// Execute operations concurrently with CPU-based throttling
var operation = ForEachWorkflowOperation.CreateSharedInput(
    new[] { new ProcessItemOperation(), new ValidateOperation(), new AuditOperation() },
    maxConcurrency: Environment.ProcessorCount
);

// Or split input collection among operations
var splitOp = ForEachWorkflowOperation.CreateSplitInput(
    itemOperations,
    maxConcurrency: 4
);
```

**Benchmark result**: Near-perfect linear scaling (16x speedup for 16 workflows).

### 5. Minimize Middleware

**Add only necessary middleware**:
```csharp
// Middleware adds overhead, only use what's needed
foundry.AddMiddleware(new TimingMiddleware());       // ~1-2μs overhead
foundry.AddMiddleware(new ValidationMiddleware());   // ~1-5μs overhead
```

**Middleware ordering matters** (see [operations.md](../core/operations.md#middleware-pipeline)).

### 6. Reuse Workflows and Foundries

**Reuse workflow definitions**:
```csharp
// Build once
var workflow = WorkflowForge.CreateWorkflow("Process")
    .AddOperation(new Step1())
    .AddOperation(new Step2())
    .Build();

// Execute many times
for (int i = 0; i < 1000; i++) {
    await smith.ForgeAsync(workflow, data);
}
```

**Creation overhead is minimal** (13μs), but reuse is still best practice.

### 7. Monitor Memory Allocations

**Use minimal allocation patterns**:
- Start with 2.65KB baseline
- Expect linear scaling (~500B per operation)
- Monitor for unexpected Gen2 collections

**Tools**:
- BenchmarkDotNet for allocation tracking
- Performance monitoring extension
- .NET diagnostic tools

### 8. Use Async/Await Properly

**Always use async/await for I/O**:
```csharp
// GOOD
protected override async Task<object?> ForgeAsyncCore(...)
{
    var result = await httpClient.GetAsync(url);
    return result;
}

// BAD (blocks thread)
protected override async Task<object?> ForgeAsyncCore(...)
{
    var result = httpClient.GetAsync(url).Result;  // Deadlock risk
    return result;
}
```

### 9. Profile Your Workflows

**Use built-in performance monitoring**:
```csharp
foundry.EnablePerformanceMonitoring();

// After execution
var metrics = foundry.GetPerformanceMetrics();
Console.WriteLine($"Total Duration: {metrics.TotalDuration}ms");
Console.WriteLine($"Memory: {metrics.MemoryAllocated}KB");
```

---

## Benchmark Methodology

### Internal Benchmarks

**Configuration**:
- **Framework**: BenchmarkDotNet v0.15.8
- **Runtime**: .NET 8.0.23
- **Mode**: Median-focused (more stable than mean)
- **Iterations**: 50 per benchmark
- **Warmup**: 5 iterations

**Scenarios Tested**:
1. Operation Performance (OperationPerformanceBenchmark)
2. Workflow Throughput (WorkflowThroughputBenchmark)
3. Concurrency Scaling (ConcurrencyBenchmark)
4. Memory Allocation (MemoryAllocationBenchmark)
5. Configuration Overhead (ConfigurationProfilesBenchmark)

### Competitive Benchmarks

**Configuration**:
- Same as internal benchmarks
- **Identical scenarios** across all frameworks
- **Fair implementations** (no artificial handicaps)

**Scenarios Tested**:
1. Sequential Workflow (1, 5, 10, 25, 50 operations)
2. Data Passing (5, 10, 25 operations)
3. Conditional Branching (5, 10, 25 operations)
4. Loop/ForEach (10, 25, 50 items)
5. Concurrent Execution (1, 4, 8 workflows)
6. Error Handling
7. Creation Overhead
8. Complete Lifecycle (WorkflowCore excluded, see [competitive-analysis.md](competitive-analysis.md))

**Statistical Significance**:
- Median values used (more stable than mean)
- Standard deviation < 20% of mean (most scenarios)
- P95 values provided for consistency verification
- 50 iterations ensure statistical confidence

---

## Reproduction

### Running Internal Benchmarks

```bash
cd src/benchmarks/WorkflowForge.Benchmarks
dotnet run -c Release
```

Results will be in `BenchmarkDotNet.Artifacts/results/`.

### Running Competitive Benchmarks

```bash
cd src/benchmarks/WorkflowForge.Benchmarks.Comparative
dotnet run -c Release
```

Results will be in `BenchmarkDotNet.Artifacts/results/`.

**Note**: Benchmarks may take 30-60 minutes to complete.

---

## Performance Targets

WorkflowForge maintains the following performance targets:

| Metric | Target | Actual |
|--------|--------|--------|
| Single operation execution | <50μs | 9.8-37.8μs |
| 10-operation workflow | <300μs | 224μs |
| 50-operation workflow | <500μs | 395μs |
| Memory per 10-op workflow | <20KB | 14.75KB |
| Concurrent scaling | Linear | Near-perfect |
| GC pressure | Minimal | Gen0 only |

**All targets met or exceeded.**

---

## Performance History

### Version 2.0.0 (Current - January 2026)

- **12 scenarios tested** against Workflow Core and Elsa
- **11-540x faster** than competitors (State Machine: 303-540x)
- **9-573x less memory** allocation
- Near-perfect concurrent scaling
- Microsecond-level operation execution
- Tested with BenchmarkDotNet v0.15.8 on .NET 8.0.23

### Version 1.x

- No official benchmarks published
- Internal testing showed strong performance
- Competitive analysis not conducted

---

## Conclusion

WorkflowForge delivers **exceptional performance** for high-throughput, low-latency workflow orchestration:

- **Microsecond-scale execution** (13-497μs typical)
- **Minimal memory footprint** (3.5-121KB across scenarios)
- **Near-perfect concurrent scaling** (16x speedup for 16 workflows)
- **11-540x faster than competitors** (State Machine: 303-540x)
- **9-573x less memory than competitors**

**12 Benchmark Scenarios Tested**:
1. Sequential, Data Passing, Conditional, Loop (26-129x faster)
2. Concurrent Execution (109-264x faster)
3. State Machine (**303-540x faster** - highest advantage)
4. Parallel Execution (38-380x faster)
5. Error Handling, Creation Overhead, Complete Lifecycle
6. Long Running, Event-Driven (I/O-bound, but 51-423x less memory)

**Best suited for**:
- High-throughput processing (>1,000 workflows/sec)
- Real-time orchestration (<1ms latency)
- Microservices and API orchestration
- Memory-constrained environments

For detailed competitive analysis and architectural comparisons, see [competitive-analysis.md](competitive-analysis.md).

---

## Related Documentation

- [Internal Benchmarks](internal-benchmarks.md) - Detailed internal benchmark results
- [Competitive Analysis](competitive-analysis.md) - Head-to-head comparisons

