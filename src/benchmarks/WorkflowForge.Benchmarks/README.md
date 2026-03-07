# WorkflowForge Internal Benchmarks

**Internal performance benchmarks for WorkflowForge core**

**Last Updated**: March 2026  
**Published Results**: [Internal Benchmarks Documentation](../../../docs/performance/internal-benchmarks.md)

## Overview

This project contains comprehensive internal performance benchmarks using BenchmarkDotNet to measure and track WorkflowForge's performance characteristics across different scenarios and configuration profiles.

**Note**: ConfigurationProfilesBenchmark is not in the latest run results at BenchmarkDotNet.Artifacts.

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

**Results**: 8.75-82 μs execution (CPU-bound), 56-8,536 bytes allocated

### 2. Workflow Throughput (`WorkflowThroughputBenchmark`)

Measures complete workflow execution with varying operation counts:

- **SequentialDelegateOperations**: Delegate operations in sequence
- **SequentialCustomOperations**: Custom operations in sequence
- **HighPerformanceConfiguration**: Optimized configuration
- **ForEachLoopWorkflow**: Collection processing workflows

**Results**: 38-272 μs for CPU-bound workflows (1-50 operations)

### 3. Concurrency Performance (`ConcurrencyBenchmark`)

Measures parallel vs sequential workflow execution:

- **SequentialWorkflows**: Execute 8 workflows sequentially
- **ConcurrentWorkflows**: Execute 8 workflows concurrently
- **ParallelWorkflows**: Execute using `Parallel.ForEach`

**Results**: ~8x faster with concurrent/parallel execution (79ms vs 627ms)

### 4. Memory Allocation (`MemoryAllocationBenchmark`)

Tracks memory usage and GC behavior:

- **MinimalAllocationWorkflow**: Optimized for low memory
- **LargeObjectAllocation**: Handling large objects
- **MemoryPressureScenario**: High memory scenarios

**Results**: 3,408 B (3.3 KB) for minimal, up to ~1 MB for large objects

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
| Operation Execution | < 50 μs | 8.75-82 μs ✅ |
| Workflow Creation | < 25 μs | 1.2-1.9 μs ✅ |
| Memory per Operation | < 2 KB | 56-8,536 B ✅ |
| Concurrent Speedup | > 5x | 8x ✅ |

## Test System

- **OS**: Windows 11 (25H2)
- **CPU**: Intel 11th Gen i7-1185G7
- **Runtimes**: .NET 10.0.3, .NET 8.0.24, .NET Framework 4.8.1
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

