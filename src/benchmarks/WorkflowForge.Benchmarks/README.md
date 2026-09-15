# WorkflowForge internal benchmarks

**Last updated:** March 2026  
**Published write-up:** [Internal benchmarks](../../../docs/performance/internal-benchmarks.md)

BenchmarkDotNet lives here to measure WorkflowForge across scenarios and configuration profiles and to watch how those numbers move over time.

## Categories

### Operation performance (`OperationPerformanceBenchmark`)

Per-type execution time and allocations:

- **DelegateOperationExecution**, **ActionOperationExecution**, **CustomOperationExecution**, **LoggingOperationExecution**
- **DelegateOperationCreation**, **ActionOperationCreation**, **CustomOperationCreation**

**Results:** about **8.75–82 μs** per run (CPU-bound), **56–8,536 B** allocated.

### Workflow throughput (`WorkflowThroughputBenchmark`)

End-to-end workflows with varying operation counts:

- **SequentialDelegateOperations** / **SequentialCustomOperations**
- **HighPerformanceConfiguration**
- **ForEachLoopWorkflow**

**Results:** about **38–272 μs** for CPU-bound workflows (1–50 operations).

### Concurrency (`ConcurrencyBenchmark`)

Eight workflows, sequential vs parallel:

- **SequentialWorkflows**, **ConcurrentWorkflows**, **ParallelWorkflows** (`Parallel.ForEach`)

**Results:** concurrent/parallel runs land near **~8×** faster than strictly sequential (**~79 ms** vs **~627 ms** in the recorded run).

### Memory (`MemoryAllocationBenchmark`)

Allocation and GC shapes:

- **MinimalAllocationWorkflow**, **LargeObjectAllocation**, **MemoryPressureScenario**

**Results:** **~4,126 B** on the minimal path; large-object paths up to about **~1 MB**.

## Running

All benchmarks:

```bash
cd src/benchmarks/WorkflowForge.Benchmarks
dotnet run -c Release -f net10.0
```

Filter:

```bash
dotnet run -c Release --filter *OperationPerformanceBenchmark*
```

Memory diagnoser:

```bash
dotnet run -c Release
```

The memory diagnoser is always on; there is no flag to toggle it.

## Outputs

Under `BenchmarkDotNet.Artifacts/results/`:

- `*-report.html`, `*-report.csv`, `*-report.md`, `*-report-github.md`

## Targets (sanity checks)

| Area | Target | Observed |
|------|--------|----------|
| Operation execution | < 50 μs | ~74ns–9.5μs (excluding 1ms delay op) |
| Workflow creation | < 25 μs | ~48–51ns |
| Memory / op | < 2 KB | 32–8,536 B |
| Concurrent speedup | > 5× | measure per ConcurrencyBenchmark harness |

## Test rig

- **OS:** Windows 11 (25H2)
- **CPU:** Intel 11th Gen i7-1185G7
- **Runtimes:** .NET 10.0.3, .NET 8.0.24, .NET Framework 4.8.1
- **BenchmarkDotNet:** v0.15.8

## Docs

- [Performance](../../../docs/performance/performance.md)
- [Competitive analysis](../../../docs/performance/competitive-analysis.md)
- [Architecture](../../../docs/architecture/overview.md)

## Adding a benchmark

1. One-time setup in `[GlobalSetup]`.
2. Per-iteration resets in `[IterationSetup]`.
3. Dispose expensive resources with `IDisposable` when needed.
4. `[MethodImpl(MethodImplOptions.AggressiveInlining)]` only on truly hot paths.
5. Prefer realistic workloads over toy microbenchmarks.
6. Always **Release**.
7. Close noisy background work for clean CPU numbers.
