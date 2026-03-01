## Competitive Benchmark Summary (Median, 50 iterations)

### Execution Time (.NET 8.0)

| # | Scenario | WorkflowForge | Workflow Core | Elsa | Speed Advantage |
|---|----------|---------------|---------------|------|-----------------|
| 1 | Sequential (10 ops) | 314μs | 15,997μs | 26,881μs | 51-86x |
| 2 | Data Passing (10 ops) | 288μs | 15,509μs | 26,100μs | 54-91x |
| 3 | Conditional (10 ops) | 317μs | 15,543μs | 26,477μs | 49-84x |
| 4 | Loop (50 items) | 570μs | 72,985μs | 82,407μs | 128-145x |
| 5 | Concurrent (8 workers) | 482μs | 59,141μs | 137,342μs | 123-285x |
| 6 | Error Handling | 140μs | 1,781μs | 10,799μs | 13-77x |
| 7 | Creation Overhead | 16μs | 1,252μs | 3,396μs | 77-209x |
| 8 | Complete Lifecycle | 74μs | N/A | 12,684μs | 171x |
| 9 | State Machine (25) | 111μs | 39,500μs | 45,714μs | 356-412x |
| 10 | Long Running* | 72ms | 71ms | 84ms | Memory-focused |
| 11 | Parallel (16 ops) | 69μs | 2,945μs | 32,419μs | 43-471x |
| 12 | Event-Driven* | 6.6ms | 6.7ms | 19.4ms | 1.0-2.9x |

*Long Running and Event-Driven are delay-bound; advantage is in memory.

### Execution Time (.NET 10.0)

| # | Scenario | WorkflowForge | Workflow Core | Elsa | Speed Advantage |
|---|----------|---------------|---------------|------|-----------------|
| 1 | Sequential (10 ops) | 290μs | 15,428μs | 26,595μs | 53-92x |
| 2 | Data Passing (10 ops) | 267μs | 15,515μs | 27,316μs | 58-102x |
| 3 | Conditional (10 ops) | 303μs | 16,951μs | 28,192μs | 56-93x |
| 4 | Loop (50 items) | 602μs | 77,416μs | 64,750μs | 108-129x |
| 5 | Concurrent (8 workers) | 448μs | 62,218μs | 117,964μs | 139-263x |
| 6 | Error Handling | 94μs | 2,008μs | 10,891μs | 21-116x |
| 7 | Creation Overhead | 14μs | 1,396μs | 3,105μs | 100-222x |
| 8 | Complete Lifecycle | 73μs | N/A | 14,294μs | 195x |
| 9 | State Machine (25) | 83μs | 42,205μs | 43,328μs | 508-522x |
| 10 | Long Running* | 72ms | 71ms | 84ms | Memory-focused |
| 11 | Parallel (16 ops) | 65μs | 3,140μs | 30,926μs | 48-477x |
| 12 | Event-Driven* | 6.0ms | 6.5ms | 19.5ms | 1.1-3.2x |

### Execution Time (.NET Framework 4.8)

| # | Scenario | WorkflowForge | Workflow Core | Speed Advantage |
|---|----------|---------------|---------------|-----------------|
| 1 | Sequential (10 ops) | 179μs | 10,325μs | 58x |
| 2 | Data Passing (10 ops) | 185μs | 10,278μs | 56x |
| 3 | Conditional (10 ops) | 173μs | 9,892μs | 57x |
| 4 | Loop (50 items) | 516μs | 49,575μs | 96x |
| 5 | Concurrent (8 workers) | 218μs | 62,193μs | 285x |
| 6 | Error Handling | 117μs | 4,633μs | 40x |
| 7 | Creation Overhead | 9μs | 346μs | 38x |
| 9 | State Machine (25) | 101μs | 25,884μs | 256x |
| 11 | Parallel (16 ops) | 45μs | 2,157μs | 48x |

Elsa does not support .NET Framework 4.8 and is excluded from this comparison.

## Competitive Memory Summary (.NET 8.0)

| # | Scenario | WorkflowForge | Workflow Core | Elsa | Memory Advantage |
|---|----------|---------------|---------------|------|------------------|
| 1 | Sequential (10 ops) | 16.20KB | 427.9KB | 2,988KB | 26-184x |
| 2 | Data Passing (10 ops) | 14.84KB | 429.7KB | 2,989KB | 29-201x |
| 3 | Conditional (10 ops) | 17.97KB | 428.6KB | 2,984KB | 24-166x |
| 4 | Loop (50 items) | 89.18KB | 2,124KB | 10,896KB | 24-122x |
| 5 | Concurrent (8) | 142.5KB | 3,232KB | 19,164KB | 23-134x |
| 6 | Error Handling | 7.77KB | 46.9KB | 1,072KB | 6-138x |
| 7 | Creation Overhead | 3.73KB | 128.9KB | 578KB | 35-155x |
| 8 | Complete Lifecycle | 3.59KB | N/A | 1,510KB | 421x |
| 9 | State Machine (25) | 23.94KB | 1,108KB | 5,940KB | 46-248x |
| 10 | Long Running | 5.14KB | 266.4KB | 2,215KB | 52-431x |
| 11 | Parallel (16 ops) | 8.17KB | 125.5KB | 4,644KB | 15-568x |
| 12 | Event-Driven | 3.49KB | 37.5KB | 1,032KB | 11-296x |

## Competitive Memory Summary (.NET 10.0)

| # | Scenario | WorkflowForge | Workflow Core | Elsa | Memory Advantage |
|---|----------|---------------|---------------|------|------------------|
| 1 | Sequential (10 ops) | 17.23KB | 426.6KB | 3,024KB | 25-175x |
| 2 | Data Passing (10 ops) | 14.84KB | 426.4KB | 3,023KB | 29-204x |
| 3 | Conditional (10 ops) | 17.97KB | 425.8KB | 3,017KB | 24-168x |
| 4 | Loop (50 items) | 92.72KB | 2,086KB | 10,921KB | 23-118x |
| 5 | Concurrent (8) | 142.5KB | 3,171KB | 19,127KB | 22-134x |
| 6 | Error Handling | 5.68KB | 50.1KB | 1,053KB | 9-185x |
| 7 | Creation Overhead | 3.73KB | 124.6KB | 537KB | 33-144x |
| 8 | Complete Lifecycle | 3.59KB | N/A | 1,513KB | 421x |
| 9 | State Machine (25) | 23.94KB | 1,090KB | 5,963KB | 46-249x |
| 10 | Long Running | 5.14KB | 266.1KB | 2,244KB | 52-437x |
| 11 | Parallel (16 ops) | 7.91KB | 128.0KB | 4,571KB | 16-578x |
| 12 | Event-Driven | 3.49KB | 41.3KB | 1,001KB | 12-287x |

## Competitive Memory Summary (.NET Framework 4.8)

| # | Scenario | WorkflowForge | Workflow Core | Memory Advantage |
|---|----------|---------------|---------------|------------------|
| 1 | Sequential (10 ops) | 40.00KB | 600.0KB | 15x |
| 2 | Data Passing (10 ops) | 32.00KB | 568.0KB | 18x |
| 3 | Conditional (10 ops) | 48.00KB | 568.0KB | 12x |
| 4 | Loop (50 items) | 168.00KB | 2,576KB | 15x |
| 5 | Concurrent (8) | 256.00KB | 3,880KB | 15x |
| 9 | State Machine (25) | 24.00KB | 1,368KB | 57x |

Elsa does not support .NET Framework 4.8 and is excluded. Memory allocation metrics are not reported by BenchmarkDotNet for .NET Framework 4.8 in all scenarios.

## Key Insights

- **Concurrent Execution**: Up to **285x faster** than Elsa with parallel workloads
- **State Machine**: Up to **522x faster** with complex state transitions (.NET 10.0)
- **Sequential Workflows**: **51-92x faster** across all runtimes with minimal memory
- **Memory Baseline**: **3.49 KB** minimal allocation footprint
- **Cross-Runtime**: Consistent advantage on .NET 10.0, .NET 8.0, and .NET Framework 4.8

Notes:
- Results captured on Windows 11 (25H2), Intel i7-1185G7, 50 iterations.
- .NET SDK 10.0.103, runtimes: .NET 10.0.3, .NET 8.0.24, .NET Framework 4.8.1.
- BenchmarkDotNet v0.15.8.
- Elsa does not support .NET Framework 4.8; those results are excluded.
