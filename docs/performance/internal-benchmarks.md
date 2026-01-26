# WorkflowForge Internal Benchmarks

This document presents WorkflowForge's internal performance benchmarks - comprehensive self-testing that validates the framework's performance characteristics independently of competitor comparisons.

**Test System**: Windows 11 (25H2), Intel 11th Gen i7-1185G7, .NET 8.0.23  
**Benchmark Framework**: BenchmarkDotNet v0.15.8  
**Methodology**: 50 iterations per benchmark, 5 warmup iterations  
**Last Updated**: January 2026

---

## Table of Contents

- [Executive Summary](#executive-summary)
- [Operation Performance](#operation-performance)
- [Workflow Throughput](#workflow-throughput)
- [Memory Allocation](#memory-allocation)
- [Concurrency Scaling](#concurrency-scaling)
- [Key Insights](#key-insights)

---

## Executive Summary

WorkflowForge internal benchmarks demonstrate (50 iterations):

| Metric | Result |
|--------|--------|
| **Operation Execution** | 12-45μs median (excluding delays) |
| **Operation Creation** | 1.8-1.9μs median |
| **Workflow Throughput** | 39-178μs for custom operations |
| **Memory Baseline** | 3.2KB minimal allocation |
| **Concurrency Scaling** | Near-linear (16x speedup for 16 workflows) |
| **GC Pressure** | Gen0 only for typical workloads |

---

## Operation Performance

Tests individual operation types for execution time and memory allocation.

### Operation Execution (Median Times)

| Operation Type | Median | Allocated |
|----------------|--------|-----------|
| LoggingOperationExecution | 10.3μs | 1,912 B |
| ConditionalOperationFalse | 22.5μs | 768 B |
| DelegateOperationExecution | 27.3μs | 464 B |
| CustomOperationExecution | 27.5μs | 296 B |
| OperationWithRestoration | 28.5μs | 472 B |
| ActionOperationExecution | 34.3μs | 488 B |
| ForEachOperationSmallCollection | 38.4μs | 1,984 B |
| ConditionalOperationTrue | 53.2μs | 704 B |
| ChainedOperationsExecution | 56.7μs | 3,832 B |
| OperationExceptionHandling | 58.7μs | 1,688 B |
| OperationDataManipulation | 59.4μs | 8,376 B |
| ForEachOperationLargeCollection | 67.4μs | 6,776 B |
| DelayOperationExecution | 15,089μs | 1,272 B |

### Operation Creation (Median Times)

| Operation Type | Median | Allocated |
|----------------|--------|-----------|
| DelegateOperationCreation | 1.5μs | 64 B |
| ActionOperationCreation | 1.8μs | 56 B |
| CustomOperationCreation | 1.8μs | 32 B |

**Key Findings**:
- Custom operations are the most memory-efficient (296 B)
- Logging operations are fastest (10.3μs)
- Operation creation is extremely fast (1.5-1.8μs)
- Exception handling adds minimal overhead

---

## Workflow Throughput

Tests complete workflow execution patterns with varying operation counts.

### Custom Operations vs Delegate Operations (Median Times)

| Operations | Custom Ops | Delegate Ops | Custom Memory | Delegate Memory |
|------------|------------|--------------|---------------|-----------------|
| 1 | 38μs | 15,178μs* | 2.74 KB | 3.5 KB |
| 5 | 64μs | 79,303μs* | 5.55 KB | 8.98 KB |
| 10 | 87μs | 159,824μs* | 9.72 KB | 17.88 KB |
| 25 | 120μs | 400,443μs* | 21.87 KB | 43.97 KB |
| 50 | 190μs | 787,131μs* | 43.41 KB | 85.76 KB |

*Delegate operations include 15ms delay per operation for testing purposes.

### Workflow Patterns (10 Operations, Median Times)

| Pattern | Median | Memory |
|---------|--------|--------|
| ForEachLoopWorkflow | 57.6μs | 4.94 KB |
| SequentialCustomOperations | 87.2μs | 9.72 KB |
| HighPerformanceConfiguration | 96.6μs | 16.3 KB |
| DataPassingWorkflow | 159,605μs* | 18.46 KB |
| ConditionalOperationsWorkflow | 159,489μs* | 19.64 KB |
| MemoryIntensiveWorkflow | 159,382μs* | 28.04 KB |
| LoggingOperationsWorkflow | 158,484μs* | 39.45 KB |

*Includes built-in delays for realistic simulation.

**Key Findings**:
- ForEach loops are highly optimized (57.6μs for 10 items)
- Memory scales linearly with operation count
- Custom operations are ~1000x faster than delegate with delays

---

## Memory Allocation

Tests memory allocation patterns and GC behavior.

### Allocation Patterns (Median Times, 50 Allocations)

| Pattern | Median | Memory | GC Gen0 | GC Gen1 | GC Gen2 |
|---------|--------|--------|---------|---------|---------|
| MinimalAllocationWorkflow | 40.3μs | 3.04 KB | - | - | - |
| StringBuilderOptimization | 245.9μs | 67.12 KB | - | - | - |
| SmallObjectAllocation | 247.6μs | 79.99 KB | - | - | - |
| StringConcatenationAllocation | 255.9μs | 86.72 KB | - | - | - |
| CollectionAllocation | 265.7μs | 73.88 KB | - | - | - |
| MemoryPressureScenario | 300.4μs | 1,530.7 KB | - | - | - |
| ObjectPoolingSimulation | 306.0μs | 91.55 KB | - | - | - |
| ArrayReuseOptimization | 366.5μs | 82.21 KB | - | - | - |
| LargeObjectAllocation | 1,671.8μs | 4,962.6 KB | Yes | Yes | Yes |

### Memory Scaling

| Allocations | Minimal Workflow | Small Objects | Large Objects |
|-------------|------------------|---------------|---------------|
| 10 | 3.04 KB | 16.71 KB | 992.63 KB |
| 50 | 3.04 KB | 79.99 KB | 4,962.6 KB |
| 100 | 3.04 KB | 159.5 KB | 4,961.85 KB |
| 500 | 3.04 KB | 776.02 KB | 4,951.25 KB |

**Key Findings**:
- Minimal allocation baseline is constant at 3.04 KB
- No GC pressure for typical workflows (Gen0 only)
- Large object allocations trigger full GC (Gen0/1/2)
- StringBuilder optimization saves ~20KB vs concatenation at 50 allocations

---

## Concurrency Scaling

Tests concurrent workflow execution patterns.

### Scaling Factor (10 Operations per Workflow)

| Concurrent Workflows | Sequential Time | Concurrent Time | Speedup | Memory per WF |
|---------------------|-----------------|-----------------|---------|---------------|
| 1 | 159.48ms | 159.48ms | 1.0x | 19.47 KB |
| 2 | 318.96ms | 159.17ms | 2.0x | 18.06 KB |
| 4 | 626.34ms | 159.38ms | 3.9x | 19.11 KB |
| 8 | 1,255.18ms | 159.53ms | 7.9x | 18.73 KB |
| 16 | 2,507.85ms | 159.49ms | 15.7x | 18.88 KB |

### Concurrency Patterns (8 Workflows, 10 Operations)

| Pattern | Median | Memory |
|---------|--------|--------|
| SharedResourceConcurrency | 154.5ms | 156.35 KB |
| ConcurrentWorkflows | 159.6ms | 149.87 KB |
| ParallelWorkflows | 159.2ms | 151.34 KB |
| TaskBasedConcurrency | 157.9ms | 151.36 KB |
| ConcurrentDataAccess | 158.1ms | 199 KB |
| WorkflowChainConcurrency | 475.6ms | 448.7 KB |
| HighContentionScenario | 1,285.4ms | 144.81 KB |

**Key Findings**:
- Near-linear scaling up to 16 concurrent workflows
- Consistent memory overhead per workflow (~18-19 KB)
- Shared resource patterns maintain good performance
- High contention scenarios serialize as expected

---

## Key Insights

### Performance Characteristics

1. **Operation Execution**: 10-67μs median for CPU-bound operations
2. **Operation Creation**: Sub-2μs overhead for all operation types
3. **Memory Efficiency**: 3.04 KB baseline, linear scaling
4. **Concurrency**: Near-perfect linear scaling (15.7x for 16 workflows)

### Recommendations

1. **Use Custom Operations** for production - most efficient (296 B allocation)
2. **Prefer ForEach loops** - highly optimized (57.6μs for 10 items)
3. **Avoid Large Object Allocations** - triggers Gen2 GC
4. **Scale Horizontally** - linear scaling supports high throughput

### Comparison with Competitive Benchmarks

These internal benchmarks validate the competitive benchmark findings:

| Metric | Internal Result | Competitive Advantage |
|--------|-----------------|----------------------|
| Sequential Execution | 38-190μs | 27-76x faster than competitors |
| Memory Allocation | 3-86 KB | 29-203x less than competitors |
| Concurrency Scaling | 15.7x for 16 WF | Near-perfect vs degraded competitors |

---

## Related Documentation

- **[Performance Overview](performance.md)** - Summary and production targets
- **[Competitive Analysis](competitive-analysis.md)** - Head-to-head comparisons
- **[Benchmark Methodology](../../performance/methodology.md)** - How we test

**Raw Data**: Full BenchmarkDotNet reports available in `BenchmarkDotNet.Artifacts/results/`
