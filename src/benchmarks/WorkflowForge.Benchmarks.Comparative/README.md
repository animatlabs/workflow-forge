# WorkflowForge comparative benchmarks

**Last updated:** September 2026

Head-to-head numbers: **WorkflowForge** vs **Workflow Core** vs **Elsa Workflows** on the same logical scenarios, each written the way that library expects.

## Frameworks

- **WorkflowForge 2.2.0:** zero dependency, in-process orchestration
- **Workflow Core:** persistence-first engine
- **Elsa Workflows:** designer-friendly, HTTP-heavy workflows

## Scenarios

| # | Scenario | What we measure | WorkflowForge vs others |
|---|----------|-----------------|-------------------------|
| 1 | Simple sequential workflow | 10 operations in sequence | **60–186×** faster (119μs vs 7,074–22,028μs) |
| 2 | Data passing workflow | Data across 10 operations | **55–172×** faster (129μs vs 7,070–22,211μs) |
| 3 | Conditional branching | If/else over 10 operations | **52–153×** faster (138μs vs 7,117–21,034μs) |
| 4 | Loop/ForEach processing | 50 items through the pipeline | **102–182×** faster (322μs vs 32,922–58,601μs) |
| 5 | Concurrent execution | Eight workflows at once | **183–383×** faster (260μs vs 47,648–99,384μs) |
| 6 | Error handling | Exceptions and recovery paths | **18–133×** faster (69μs vs 1,225–9,249μs) |
| 7 | Creation overhead | Instantiation cost only | **2–3×** faster vs Workflow Core (18μs vs 46μs); Elsa ~4μs |
| 8 | Complete lifecycle | Create, execute, tear down | Workflow Core omitted (see exclusions doc). **69×** faster than Elsa (108μs vs 7,464μs) |
| 9 | State machine workflow | 25 conditional transitions | **289–584×** faster (59μs vs 17,133–34,627μs) |
| 10 | Long running workflow | Multi-phase run with one delay | Delay-bound (~72ms); memory advantage dominates |
| 11 | Parallel scaling | 16 operations in parallel | **44–444×** faster (50μs vs 2,214–22,285μs) |
| 12 | Event-driven workflow | Event-triggered execution | **1–3×** faster than Elsa on execution; large memory gap |

## Running

```bash
dotnet run --project src/benchmarks/WorkflowForge.Benchmarks --configuration Release -f net10.0
dotnet run --project src/benchmarks/WorkflowForge.Benchmarks.Comparative --configuration Release -f net10.0
```

Artifacts land under `BenchmarkDotNet.Artifacts/`.

Single scenario:

```bash
dotnet run -c Release --filter *Scenario1*
```

Smoke run:

```bash
dotnet run -c Release --job short
```

Reports sit in `BenchmarkDotNet.Artifacts/results/` and per-scenario folders.

## Headline results

- **2–583×** faster execution across the 12 scenarios in these runs
- **2–533×** lower allocation where we measured allocations
- WorkflowForge stayed in the microsecond band while several competitors sat in milliseconds

### Why WorkflowForge reads faster here

1. No extra stack on top of BCL (**zero dependencies** in core).
2. Operations invoke without a separate dispatcher or hosted event loop (**direct execution**).
3. Fewer allocations and simpler shapes (**memory**).
4. No background worker pool in the default model (**no host threads**).
5. Delegates call straight through (**lightweight operations**).

### Trade-offs

**WorkflowForge**

- Wins on raw speed, allocations, and deployment surface in these tests
- Shared state is the `foundry.Properties` dictionary model

**Workflow Core**

- Brings persistence, long-running hosts, and an event-driven state machine
- Large ecosystem around that model

**Elsa**

- Visual designer and HTTP-centric workflows
- Broad activity library and UI/API integration

## Architectural snapshot

**WorkflowForge:** in-memory runs, no required external services, dictionary-backed context, microsecond-scale overhead in these benchmarks, zero third-party deps in core.

**Workflow Core:** persistence-first, background workers, event-driven state machine, expects storage.

**Elsa:** HTTP workflows with minimal setup, large built-in activity surface, designer + modular hosting, UI/API paths.

## Fair comparison

Each scenario:

- Implements the same logical work
- Uses idiomatic patterns for that framework
- Uses realistic shapes
- Times full execution
- Records allocations where applicable
- Pins to current stable versions

**Exclusions:** Scenario 8 drops Workflow Core because `WorkflowHost.Start()` background threads do not match rapid create/start/stop loops. Details: [BENCHMARK_EXCLUSIONS.md](BENCHMARK_EXCLUSIONS.md).

## Test rig

- **OS:** Windows 11 (25H2)
- **CPU:** Intel 11th Gen i7-1185G7
- **Runtimes:** .NET 10.0.3, .NET 8.0.24, .NET Framework 4.8.1
- **BenchmarkDotNet:** v0.15.8
- **Iterations:** 10 per benchmark job

## Docs

- [Performance](../../../docs/performance/performance.md)
- [Competitive analysis](../../../docs/performance/competitive-analysis.md)
- [Architecture](../../../docs/architecture/overview.md)

## Reproducing

1. Clone the repo.
2. `dotnet restore`
3. Run the comparative project (or filter to a scenario).
4. Inspect `BenchmarkDotNet.Artifacts/`.

Sources live beside the library code in this repository.
