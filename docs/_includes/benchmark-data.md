## Competitive Benchmark Summary (Median, 50 iterations)

### Execution Time (.NET 8.0)

| # | Scenario | WorkflowForge | Workflow Core | Elsa | Speed Advantage |
|---|----------|---------------|---------------|------|-----------------|
| 1 | Sequential (10 ops) | 377μs | 9,879μs | 19,168μs | 26-51x |
| 2 | Data Passing (10 ops) | 321μs | 9,751μs | 19,164μs | 30-60x |
| 3 | Conditional (10 ops) | 301μs | 9,248μs | 19,361μs | 31-64x |
| 4 | Loop (50 items) | 495μs | 30,742μs | 58,347μs | 62-118x |
| 5 | Concurrent (8 workers) | 357μs | 42,054μs | 103,024μs | 118-289x |
| 6 | Error Handling | 114μs | 1,349μs | 7,737μs | 12-68x |
| 7 | Creation Overhead | 11μs | 819μs | 2,328μs | 74-212x |
| 8 | Complete Lifecycle | 59μs | N/A | 9,723μs | 165x |
| 9 | State Machine (25) | 71μs | 21,683μs | 34,426μs | 305-485x |
| 10 | Long Running* | 72ms | 71ms | 83ms | Memory-focused |
| 11 | Parallel (16 ops) | 63μs | 2,654μs | 24,940μs | 42-396x |
| 12 | Event-Driven* | 7.1ms | 7.4ms | 19.9ms | 1.0-2.8x |

*Long Running and Event-Driven are delay-bound; advantage is in memory.

### Execution Time (.NET 10.0)

| # | Scenario | WorkflowForge | Workflow Core | Elsa | Speed Advantage |
|---|----------|---------------|---------------|------|-----------------|
| 1 | Sequential (10 ops) | 422μs | 13,828μs | 18,676μs | 33-44x |
| 2 | Data Passing (10 ops) | 325μs | 11,651μs | 18,510μs | 36-57x |
| 3 | Conditional (10 ops) | 333μs | 13,427μs | 19,166μs | 40-58x |
| 4 | Loop (50 items) | 450μs | 35,320μs | 54,827μs | 78-122x |
| 5 | Concurrent (8 workers) | 372μs | 47,114μs | 87,491μs | 127-235x |
| 6 | Error Handling | 70μs | 1,498μs | 7,694μs | 21-110x |
| 7 | Creation Overhead | 11μs | 1,001μs | 2,245μs | 91-204x |
| 8 | Complete Lifecycle | 36μs | N/A | 9,877μs | 274x |
| 9 | State Machine (25) | 65μs | 29,537μs | 33,062μs | 455-511x |
| 10 | Long Running* | 72ms | 71ms | 84ms | Memory-focused |
| 11 | Parallel (16 ops) | 56μs | 2,861μs | 24,638μs | 51-440x |
| 12 | Event-Driven* | 7.1ms | 8.3ms | 20.6ms | 1.2-2.9x |

### Execution Time (.NET Framework 4.8)

| # | Scenario | WorkflowForge | Workflow Core | Speed Advantage |
|---|----------|---------------|---------------|-----------------|
| 1 | Sequential (10 ops) | 122μs | 6,743μs | 55x |
| 2 | Data Passing (10 ops) | 118μs | 6,684μs | 57x |
| 3 | Conditional (10 ops) | 118μs | 6,562μs | 56x |
| 4 | Loop (50 items) | 350μs | 34,137μs | 98x |
| 5 | Concurrent (8 workers) | 167μs | 41,934μs | 251x |
| 6 | Error Handling | 88μs | 4,471μs | 51x |
| 7 | Creation Overhead | 7μs | 260μs | 37x |
| 9 | State Machine (25) | 61μs | 18,486μs | 303x |
| 11 | Parallel (16 ops) | 35μs | 1,754μs | 50x |

Elsa does not support .NET Framework 4.8 and is excluded from this comparison.

## Competitive Memory Summary (.NET 8.0)

| # | Scenario | WorkflowForge | Workflow Core | Elsa | Memory Advantage |
|---|----------|---------------|---------------|------|------------------|
| 1 | Sequential (10 ops) | 17.72KB | 429KB | 2,993KB | 24-169x |
| 2 | Data Passing (10 ops) | 16.36KB | 429KB | 2,987KB | 26-183x |
| 3 | Conditional (10 ops) | 19.48KB | 428KB | 2,984KB | 22-153x |
| 4 | Loop (50 items) | 96.3KB | 2,122KB | 10,908KB | 22-113x |
| 5 | Concurrent (8) | 154.7KB | 3,231KB | 19,114KB | 21-124x |
| 6 | Error Handling | 8.38KB | 47.0KB | 1,072KB | 6-128x |
| 7 | Creation Overhead | 3.72KB | 129KB | 578KB | 35-155x |
| 8 | Complete Lifecycle | 3.70KB | N/A | 1,510KB | 408x |
| 9 | State Machine (25) | 23.92KB | 1,105KB | 5,938KB | 46-248x |
| 10 | Long Running | 5.12KB | 266KB | 2,215KB | 52-433x |
| 11 | Parallel (16 ops) | 8.23KB | 125KB | 4,652KB | 15-565x |
| 12 | Event-Driven | 3.48KB | 37.5KB | 1,032KB | 11-297x |

## Competitive Memory Summary (.NET 10.0)

| # | Scenario | WorkflowForge | Workflow Core | Elsa | Memory Advantage |
|---|----------|---------------|---------------|------|------------------|
| 1 | Sequential (10 ops) | 17.72KB | 427KB | 3,024KB | 24-171x |
| 2 | Data Passing (10 ops) | 16.36KB | 425KB | 3,024KB | 26-185x |
| 3 | Conditional (10 ops) | 19.48KB | 426KB | 2,984KB | 22-153x |
| 4 | Loop (50 items) | 96.9KB | 2,086KB | 10,908KB | 22-113x |
| 5 | Concurrent (8) | 154.7KB | 3,171KB | 19,109KB | 21-124x |
| 6 | Error Handling | 7.02KB | 50.7KB | 1,056KB | 7-150x |
| 7 | Creation Overhead | 3.72KB | 125KB | 537KB | 34-146x |
| 8 | Complete Lifecycle | 3.70KB | N/A | 1,513KB | 409x |
| 9 | State Machine (25) | 23.92KB | 1,090KB | 5,966KB | 46-249x |
| 10 | Long Running | 5.12KB | 266KB | 2,244KB | 52-438x |
| 11 | Parallel (16 ops) | 7.96KB | 126KB | 4,576KB | 16-575x |
| 12 | Event-Driven | 3.48KB | 40.0KB | 999KB | 11-287x |

## Competitive Memory Summary (.NET Framework 4.8)

| # | Scenario | WorkflowForge | Workflow Core | Memory Advantage |
|---|----------|---------------|---------------|------------------|
| 1 | Sequential (10 ops) | 40.00KB | 560KB | 14x |
| 2 | Data Passing (10 ops) | 40.00KB | 544KB | 14x |
| 3 | Conditional (10 ops) | 48.00KB | 552KB | 12x |
| 4 | Loop (50 items) | 176KB | 2,512KB | 14x |
| 5 | Concurrent (8) | 272KB | 3,816KB | 14x |
| 9 | State Machine (25) | 24.00KB | 1,344KB | 56x |

Elsa does not support .NET Framework 4.8 and is excluded. Memory allocation metrics are not reported by BenchmarkDotNet for .NET Framework 4.8 in all scenarios.

## Key Insights

- **Concurrent Execution**: Up to **289x faster** than Elsa with parallel workloads
- **State Machine**: Up to **511x faster** with complex state transitions (.NET 10.0)
- **Sequential Workflows**: **26-57x faster** across all runtimes with minimal memory
- **Memory Baseline**: **3.48 KB** (Competitive, Event-Driven scenario); **3.33 KB** (Internal, MinimalAllocationWorkflow)
- **Cross-Runtime**: Consistent advantage on .NET 10.0, .NET 8.0, and .NET Framework 4.8

Notes:
- Results captured on Windows 11 (25H2), Intel i7-1185G7, 50 iterations.
- .NET SDK 10.0.103, runtimes: .NET 10.0.3, .NET 8.0.24, .NET Framework 4.8.1.
- BenchmarkDotNet v0.15.8.
- Elsa does not support .NET Framework 4.8; those results are excluded.
