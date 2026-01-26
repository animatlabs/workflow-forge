## Competitive Benchmark Summary (Median)

| # | Scenario | WorkflowForge | Workflow Core | Elsa | Speed Advantage |
|---|----------|---------------|---------------|------|-----------------|
| 1 | Sequential (10 ops) | 224μs | 6,060μs | 17,044μs | 27-76x |
| 2 | Data Passing (10 ops) | 199μs | 6,026μs | 19,851μs | 30-100x |
| 3 | Conditional (10 ops) | 238μs | 6,207μs | 17,642μs | 26-74x |
| 4 | Loop (50 items) | 389μs | 28,378μs | 55,618μs | 73-143x |
| 5 | Concurrent (8 workflows) | 312μs | 42,093μs | 107,784μs | 135-346x |
| 6 | Error Handling | 109μs | 1,236μs | 8,460μs | 11-78x |
| 7 | Creation Overhead | 12μs | 871μs | 2,313μs | 68-188x |
| 8 | Complete Lifecycle | 39μs | N/A | 10,346μs | 267x |
| 9 | State Machine (25) | 59μs | 19,156μs | 34,033μs | 322-574x |
| 10 | Long Running* | 72ms | 72ms | 85ms | Memory-focused |
| 11 | Parallel (16 ops) | 47μs | 2,080μs | 21,491μs | 44-454x |
| 12 | Event-Driven | 8ms | 8.7ms | 21.5ms | 1.1-2.7x |

*Long Running and Event-Driven are delay-bound; advantage is in memory.

## Competitive Memory Summary

| # | Scenario | WorkflowForge | Workflow Core | Elsa | Memory Advantage |
|---|----------|---------------|---------------|------|------------------|
| 1 | Sequential (10 ops) | 14.75KB | 428KB | 2,989KB | 29-203x |
| 5 | Concurrent (8) | 110KB | 3,233KB | 19,145KB | 29-174x |
| 6 | Error Handling | 5KB | 46KB | 1,072KB | 9-214x |
| 7 | Creation Overhead | 3.73KB | 129KB | 578KB | 35-155x |
| 9 | State Machine (25) | 20.5KB | 1,106KB | 5,949KB | 54-290x |
| 10 | Long Running | 5.25KB | 267KB | 2,217KB | 51-422x |
| 11 | Parallel (16 ops) | 8KB | 125KB | 4,647KB | 16-581x |
| 12 | Event-Driven | 3.49KB | 37KB | 1,032KB | 11-296x |

## Key Insights

- **State Machine**: Highest advantage at **322-574x faster**
- **Parallel Execution**: **44-454x faster** with tiny memory footprint
- **Long Running**: Same timing (delay-bound) but **51-422x less memory**
- **Event-Driven**: Similar timing to WC, 2.7x faster than Elsa, **11-296x less memory**

Notes:
- Results captured on Windows 11 (25H2), .NET 8.0.23, Intel i7-1185G7, 25 iterations.
- BenchmarkDotNet v0.15.8, .NET SDK 10.0.102.
