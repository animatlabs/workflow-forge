## Competitive Benchmark Summary (Median, 10 iterations)

### Execution Time (.NET 8.0)

| # | Scenario | WorkflowForge | Workflow Core | Elsa | Speed Advantage |
|---|----------|---------------|---------------|------|-----------------|
| 1 | Sequential (10 ops) | 127μs | 5661μs | 18257μs | 45-144x |
| 2 | Data Passing (10 ops) | 133μs | 5571μs | 20319μs | 42-153x |
| 3 | Conditional (10 ops) | 129μs | 5559μs | 18548μs | 43-143x |
| 4 | Loop (50 items) | 337μs | 26325μs | 63196μs | 78-188x |
| 5 | Concurrent (8 workers) | 284μs | 39289μs | 110279μs | 138-388x |
| 6 | Error Handling | 107μs | 1027μs | 7383μs | 10-69x |
| 7 | Creation Overhead | 22.1μs | 44.4μs | 6.05μs | 2x |
| 8 | Complete Lifecycle | 105μs | N/A | 6580μs | 63x |
| 9 | State Machine (25) | 59.7μs | 13798μs | 33184μs | 231-556x |
| 10 | Long Running* | 72ms | 71ms | 85ms | 1x |
| 11 | Parallel (16 ops) | 51.7μs | 1847μs | 27308μs | 36-528x |
| 12 | Event-Driven* | 8ms | 9ms | 21ms | 1-3x |

*Long Running and Event-Driven are delay-bound; advantage is in memory.

### Execution Time (.NET 10.0)

| # | Scenario | WorkflowForge | Workflow Core | Elsa | Speed Advantage |
|---|----------|---------------|---------------|------|-----------------|
| 1 | Sequential (10 ops) | 119μs | 7074μs | 22028μs | 60-186x |
| 2 | Data Passing (10 ops) | 129μs | 7070μs | 22211μs | 55-172x |
| 3 | Conditional (10 ops) | 138μs | 7117μs | 21034μs | 52-153x |
| 4 | Loop (50 items) | 322μs | 32922μs | 58601μs | 102-182x |
| 5 | Concurrent (8 workers) | 260μs | 47648μs | 99384μs | 183-383x |
| 6 | Error Handling | 69.4μs | 1225μs | 9249μs | 18-133x |
| 7 | Creation Overhead | 17.9μs | 45.6μs | 4.25μs | 3x |
| 8 | Complete Lifecycle | 108μs | N/A | 7464μs | 69x |
| 9 | State Machine (25) | 59.3μs | 17133μs | 34627μs | 289-584x |
| 10 | Long Running* | 72ms | 72ms | 86ms | 1x |
| 11 | Parallel (16 ops) | 50.2μs | 2214μs | 22285μs | 44-444x |
| 12 | Event-Driven* | 8ms | 8ms | 21ms | 1-3x |

### Execution Time (.NET Framework 4.8)

| # | Scenario | WorkflowForge | Workflow Core | Speed Advantage |
|---|----------|---------------|---------------|-----------------|
| 1 | Sequential (10 ops) | 110μs | 5896μs | 54x |
| 2 | Data Passing (10 ops) | 122μs | 5904μs | 48x |
| 3 | Conditional (10 ops) | 117μs | 5892μs | 50x |
| 4 | Loop (50 items) | 326μs | 28308μs | 87x |
| 5 | Concurrent (8 workers) | 141μs | 37556μs | 267x |
| 6 | Error Handling | 81.0μs | 10284μs | 127x |
| 7 | Creation Overhead | 6.90μs | 13.9μs | 2x |
| 8 | Complete Lifecycle | 33.6μs | N/A | N/A |
| 9 | State Machine (25) | 55.9μs | 15166μs | 272x |
| 11 | Parallel (16 ops) | 30.6μs | 1561μs | 51x |

Elsa does not support .NET Framework 4.8 and is excluded from this comparison.

## Competitive Memory Summary (.NET 8.0)

| # | Scenario | WorkflowForge | Workflow Core | Elsa | Memory Advantage |
|---|----------|---------------|---------------|------|------------------|
| 1 | Sequential (10 ops) | 18.20KB | 415.16KB | 3.05MB | 23-172x |
| 2 | Data Passing (10 ops) | 16.84KB | 415.30KB | 3.03MB | 25-184x |
| 3 | Conditional (10 ops) | 20KB | 416.54KB | 3.03MB | 21-156x |
| 4 | Loop (50 items) | 96.18KB | 2.01MB | 10.76MB | 21-115x |
| 5 | Concurrent (8 workers) | 158.55KB | 3.10MB | 19.18MB | 20-124x |
| 6 | Error Handling | 9.24KB | 44.91KB | 1.08MB | 5-119x |
| 7 | Creation Overhead | 4.49KB | 8.58KB | 424 B | 0.1-2x |
| 8 | Complete Lifecycle | 4.46KB | N/A | 1.02MB | 235x |
| 9 | State Machine (25) | 23.94KB | 1.05MB | 5.89MB | 45-252x |
| 10 | Long Running* | 5.77KB | 260.61KB | 2.21MB | 45-393x |
| 11 | Parallel (16 ops) | 9.00KB | 122.84KB | 4.67MB | 14-531x |
| 12 | Event-Driven* | 4.21KB | 36.21KB | 1.03MB | 9-249x |

## Competitive Memory Summary (.NET 10.0)

| # | Scenario | WorkflowForge | Workflow Core | Elsa | Memory Advantage |
|---|----------|---------------|---------------|------|------------------|
| 1 | Sequential (10 ops) | 18.22KB | 408.23KB | 3.01MB | 22-169x |
| 2 | Data Passing (10 ops) | 16.86KB | 408KB | 3.02MB | 24-183x |
| 3 | Conditional (10 ops) | 20KB | 407.38KB | 3.01MB | 20-154x |
| 4 | Loop (50 items) | 96.20KB | 1.96MB | 10.89MB | 21-116x |
| 5 | Concurrent (8 workers) | 158.66KB | 3.04MB | 19.07MB | 20-123x |
| 6 | Error Handling | 7.90KB | 43KB | 1.12MB | 5-145x |
| 7 | Creation Overhead | 4.51KB | 8.59KB | 424 B | 0.1-2x |
| 8 | Complete Lifecycle | 4.48KB | N/A | 1.00MB | 229x |
| 9 | State Machine (25) | 24KB | 1.03MB | 5.85MB | 44-250x |
| 10 | Long Running* | 5.78KB | 255.54KB | 2.19MB | 44-389x |
| 11 | Parallel (16 ops) | 8.75KB | 120.43KB | 4.56MB | 14-533x |
| 12 | Event-Driven* | 4.23KB | 35.73KB | 1.06MB | 8-258x |

---

**Summary bands (all measured scenarios; delay-heavy 10/12 excluded from speed band math, September 2026 run):** **2–583x** faster execution; **1–533x** lower allocation vs Workflow Core/Elsa.

- Results captured on Windows 11 (25H2), Intel i7-1185G7, BenchmarkDotNet v0.15.8, **10 iterations** per job.
- Median values used; Elsa omitted on .NET Framework 4.8.
