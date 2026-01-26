# WorkflowForge Internal Benchmarks

<p align="center">
  <img src="https://raw.githubusercontent.com/animatlabs/workflow-forge/main/icon.png" alt="WorkflowForge" width="120" height="120">
</p>

**Internal performance benchmarks for WorkflowForge core**

**Last Updated**: January 2026  
**Published Results**: [Internal Benchmarks Documentation](../../../docs/performance/internal-benchmarks.md)

## Overview

This project contains comprehensive internal performance benchmarks using BenchmarkDotNet to measure and track WorkflowForge's performance characteristics across different scenarios and configuration profiles.

## Benchmark Categories

### 1. Operation Performance (`OperationPerformanceBenchmark`)

Measures execution time and memory allocation for different operation types:

- **DelegateOperationExecution**: Lambda-based operations
- **ActionOperationExecution**: Action-based operations
- **CustomOperationExecution**: Custom `IWorkflowOperation` implementations
- **LoggingOperationExecution**: Built-in logging operations
- **DelegateOperationCreation**: Operation creation overhead
- **ActionOperationCreation**: Action creation overhead
- **CustomOperationCreation**: Custom operation instantiation

**Results**: 12-290 μs execution, 296-1912 bytes allocated

### 2. Workflow Throughput (`WorkflowThroughputBenchmark`)

Measures complete workflow execution with varying operation counts:

- **SequentialDelegateOperations**: Delegate operations in sequence
- **SequentialCustomOperations**: Custom operations in sequence
- **HighPerformanceConfiguration**: Optimized configuration
- **ForEachLoopWorkflow**: Collection processing workflows

**Results**: 577-158,065 μs for 10 operations

### 3. Concurrency Performance (`ConcurrencyBenchmark`)

Measures parallel vs sequential workflow execution:

- **SequentialWorkflows**: Execute 8 workflows sequentially
- **ConcurrentWorkflows**: Execute 8 workflows concurrently
- **ParallelWorkflows**: Execute using `Parallel.ForEach`

**Results**: ~8x faster with concurrent/parallel execution (78-79ms vs 632ms)

### 4. Memory Allocation (`MemoryAllocationBenchmark`)

Tracks memory usage and GC behavior:

- **MinimalAllocationWorkflow**: Optimized for low memory
- **LargeObjectAllocation**: Handling large objects
- **MemoryPressureScenario**: High memory scenarios

**Results**: 2.65 KB for minimal, up to 4.9 MB for large objects

### 5. Configuration Profiles (`ConfigurationProfilesBenchmark`)

Measures overhead of different configuration approaches:

- **MinimalConfiguration**: Zero-config baseline
- **OptionsPatternWithValidation**: IOptions<T> with validation
- **ConfigurationBinding**: Configuration system binding

**Results**: 35-85 μs

## Running Benchmarks

### Run All Benchmarks

```bash
cd src/benchmarks/WorkflowForge.Benchmarks
dotnet run -c Release
```

### Run Specific Benchmark

```bash
dotnet run -c Release --filter *OperationPerformanceBenchmark*
```

### Run with Memory Diagnoser

```bash
dotnet run -c Release --memory
```

## Benchmark Results

Results are saved to `BenchmarkDotNet.Artifacts/results/` in multiple formats:

- `*-report.html` - Visual HTML report
- `*-report.csv` - CSV for analysis
- `*-report.md` - Markdown summary
- `*-report-github.md` - GitHub-flavored markdown

## Performance Targets

| Category | Target | Actual |
|----------|--------|--------|
| Operation Execution | < 50 μs | 12-290 μs ✅ |
| Workflow Creation | < 25 μs | 12-22 μs ✅ |
| Memory per Operation | < 2 KB | 296-1912 B ✅ |
| Concurrent Speedup | > 5x | 8x ✅ |

## Test System

- **OS**: Windows 11 (25H2)
- **CPU**: Intel 11th Gen i7-1185G7
- **.NET**: 8.0.23
- **BenchmarkDotNet**: v0.15.8

## Documentation

- **[Performance Documentation](../../../docs/performance/performance.md)** - Detailed analysis and interpretation
- **[Competitive Analysis](../../../docs/performance/competitive-analysis.md)** - Comparison with other frameworks
- **[Architecture](../../../docs/architecture/overview.md)** - Design decisions affecting performance

## Benchmark Best Practices

When writing new benchmarks:

1. Use `[GlobalSetup]` for one-time initialization
2. Use `[IterationSetup]` for per-iteration setup
3. Implement `IDisposable` for cleanup
4. Mark hot paths with `[MethodImpl(MethodImplOptions.AggressiveInlining)]`
5. Use realistic scenarios, not microbenchmarks
6. Run with Release configuration
7. Close all other applications during benchmarking

---

