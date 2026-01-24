# WorkflowForge Performance Benchmarks

<p align="center">
  <img src="../../icon.png" alt="WorkflowForge" width="120" height="120">
</p>

This document provides comprehensive performance analysis of WorkflowForge, including both internal performance characteristics and competitive comparisons.

**Test System**: Windows 11, Intel 11th Gen i7-1185G7, .NET 8.0.21  
**Benchmark Framework**: BenchmarkDotNet v0.13.12  
**Iterations**: 25 per benchmark, 5 warmup iterations

---

## Table of Contents

1. [Internal Performance Benchmarks](#internal-performance-benchmarks)
2. [Competitive Performance Summary](#competitive-performance-summary)
3. [Performance Optimization Guide](#performance-optimization-guide)
4. [Benchmark Methodology](#benchmark-methodology)

---

## Internal Performance Benchmarks

These benchmarks measure WorkflowForge's intrinsic performance characteristics in isolation.

### Operation Performance

**Single Operation Execution Times** (Median):

| Operation Type | Median | Mean | Memory |
|----------------|--------|------|--------|
| Logging Operation | 9.8μs | 120.0μs | 1,912 B |
| Custom Operation | 26.1μs | 234.3μs | 296 B |
| Action Operation | 29.5μs | 290.8μs | 488 B |
| Delegate Operation | 37.8μs | 289.8μs | 464 B |

**Operation Creation Overhead** (Median):

| Operation Type | Median | Memory |
|----------------|--------|--------|
| Custom | 2.0μs | 32 B |
| Delegate | 1.8μs | 64 B |
| Action | 1.8μs | 56 B |

**Key Insights**:
- Operation execution: 9.8-37.8μs median (microsecond scale)
- Operation creation: <2.1μs (negligible overhead)
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

WorkflowForge vs. Workflow Core 3.17 and Elsa Workflows 3.5.1.

### Performance Advantage Overview

**Execution Speed**:
- **13-378x faster** across 8 scenarios
- Operates at **microsecond scale** vs. **millisecond scale**
- Advantage **increases with workload complexity**

**Memory Efficiency**:
- **6-1,495x less** memory allocation
- **Kilobytes** vs. **megabytes** for competitors
- No Gen2 GC collections in typical workflows

### By Scenario

| Scenario | WorkflowForge | Workflow Core | Elsa | Advantage |
|----------|---------------|---------------|------|-----------|
| Sequential (10 ops) | 231μs | 8,594μs | 20,898μs | 37-90x |
| Data Passing (10 ops) | 267μs | 9,037μs | 19,747μs | 34-74x |
| Conditional (10 ops) | 259μs | 8,840μs | 20,183μs | 34-78x |
| Loop (50 items) | 186μs | 31,914μs | 65,590μs | 172-353x |
| Concurrent (8 workflows) | 305μs | 45,532μs | 104,863μs | 149-343x |
| Error Handling | 110μs | 1,473μs | 8,407μs | 13-77x |
| Creation Overhead | 6.7μs | 871μs | 2,568μs | 130-383x |
| Complete Lifecycle | 36μs | N/A | 10,713μs | 296x |

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
    public override async Task<object?> ForgeAsync(
        IWorkflowFoundry foundry,
        object? inputData,
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

**Use ForEachWorkflowOperation.CreateParallel** for independent operations:
```csharp
var items = GetItemsToProcess();
var operation = ForEachWorkflowOperation.CreateParallel(
    items,
    new ProcessItemOperation(),
    maxDegreeOfParallelism: Environment.ProcessorCount
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

**Creation overhead is minimal** (6.7μs), but reuse is still best practice.

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
public override async Task<object?> ForgeAsync(...)
{
    var result = await httpClient.GetAsync(url);
    return result;
}

// BAD (blocks thread)
public override async Task<object?> ForgeAsync(...)
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
- **Framework**: BenchmarkDotNet v0.13.12
- **Runtime**: .NET 8.0.21
- **Mode**: Median-focused (more stable than mean)
- **Iterations**: 25 per benchmark
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
- 25 iterations ensure statistical confidence

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
| Workflow creation | <10μs | 6.7μs |
| 10-operation workflow | <200μs | 96.9μs |
| Memory per workflow | <20KB | 6.02KB |
| Concurrent scaling | Linear | Near-perfect |
| GC pressure | Minimal | Gen0 only |

**All targets met or exceeded.**

---

## Performance History

### Version 2.0.0 (Current)

- 13-378x faster than competitors
- 6-1,495x less memory
- Near-perfect concurrent scaling
- Microsecond-level operation execution

### Version 1.x

- No official benchmarks published
- Internal testing showed strong performance
- Competitive analysis not conducted

---

## Conclusion

WorkflowForge delivers **exceptional performance** for high-throughput, low-latency workflow orchestration:

- **Microsecond-scale execution** (9.8-231μs typical)
- **Minimal memory footprint** (2.65KB baseline)
- **Near-perfect concurrent scaling** (16x speedup for 16 workflows)
- **13-378x faster than competitors** (Workflow Core, Elsa)

**Best suited for**:
- High-throughput processing (>1,000 workflows/sec)
- Real-time orchestration (<1ms latency)
- Microservices and API orchestration
- Memory-constrained environments

For detailed competitive analysis and architectural comparisons, see [competitive-analysis.md](competitive-analysis.md).

---

**End of Performance Documentation**
