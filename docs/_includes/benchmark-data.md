## Competitive Benchmark Summary (Median, 50 iterations)

| # | Scenario | WorkflowForge | Workflow Core | Elsa | Speed Advantage |
|---|----------|---------------|---------------|------|-----------------|
| 1 | Sequential (10 ops) | 247μs | 6,531μs | 17,617μs | 26-71x |
| 2 | Data Passing (10 ops) | 262μs | 6,737μs | 18,222μs | 26-70x |
| 3 | Conditional (10 ops) | 266μs | 8,543μs | 21,333μs | 32-80x |
| 4 | Loop (50 items) | 497μs | 35,421μs | 64,171μs | 71-129x |
| 5 | Concurrent (8 workers) | 356μs | 38,833μs | 94,018μs | 109-264x |
| 6 | Error Handling | 111μs | 1,228μs | 7,150μs | 11-64x |
| 7 | Creation Overhead | 13μs | 814μs | 2,107μs | 63-162x |
| 8 | Complete Lifecycle | 42μs | N/A | 9,933μs | 236x |
| 9 | State Machine (25) | 68μs | 20,624μs | 36,695μs | 303-540x |
| 10 | Long Running* | 72ms | 71ms | 83ms | Memory-focused |
| 11 | Parallel (16 ops) | 55μs | 2,437μs | 20,891μs | 44-380x |
| 12 | Event-Driven | 7.3ms | 8.2ms | 19.3ms | 1.1-2.6x |

*Long Running and Event-Driven are delay-bound; advantage is in memory.

## Competitive Memory Summary

| # | Scenario | WorkflowForge | Workflow Core | Elsa | Memory Advantage |
|---|----------|---------------|---------------|------|------------------|
| 1 | Sequential (10 ops) | 16.31KB | 430KB | 2,984KB | 26-183x |
| 2 | Data Passing (10 ops) | 15.18KB | 427KB | 2,986KB | 28-197x |
| 3 | Conditional (10 ops) | 20.09KB | 427KB | 2,984KB | 21-149x |
| 4 | Loop (50 items) | 90.35KB | 2,123KB | 10,905KB | 24-121x |
| 5 | Concurrent (8) | 121KB | 3,232KB | 19,139KB | 27-158x |
| 6 | Error Handling | 5KB | 46KB | 1,072KB | 9-214x |
| 7 | Creation Overhead | 3.73KB | 129KB | 578KB | 35-155x |
| 8 | Complete Lifecycle | 3.57KB | N/A | 1,510KB | 423x |
| 9 | State Machine (25) | 20.92KB | 1,106KB | 5,949KB | 53-284x |
| 10 | Long Running | 5.25KB | 267KB | 2,217KB | 51-422x |
| 11 | Parallel (16 ops) | 8.1KB | 122KB | 4,647KB | 15-573x |
| 12 | Event-Driven | 3.49KB | 37KB | 1,032KB | 11-296x |

## Key Insights

- **Concurrent Execution**: Up to **264x faster** than Elsa with parallel workloads
- **State Machine**: Up to **540x faster** with complex state transitions
- **Sequential Workflows**: **26-71x faster** with minimal memory
- **Memory Baseline**: **3.49 KB** minimal allocation footprint

Notes:
- Results captured on Windows 11 (25H2), .NET 8.0.23, Intel i7-1185G7, 50 iterations.
- BenchmarkDotNet v0.15.8, .NET SDK 10.0.102.
