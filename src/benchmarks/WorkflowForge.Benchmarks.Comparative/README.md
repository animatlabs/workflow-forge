# WorkflowForge comparative benchmarks

**Last updated:** March 2026

Head-to-head numbers: **WorkflowForge** vs **Workflow Core** vs **Elsa Workflows** on the same logical scenarios, each written the way that library expects.

## Frameworks

- **WorkflowForge 2.1.1:** zero dependency, in-process orchestration
- **Workflow Core:** persistence-first engine
- **Elsa Workflows:** designer-friendly, HTTP-heavy workflows

## Scenarios

| # | Scenario | What we measure | WorkflowForge vs others |
|---|----------|-----------------|-------------------------|
| 1 | Simple sequential workflow | 10 operations in sequence | **26–51×** faster (237–422μs vs 9,878–18,675μs) |
| 2 | Data passing workflow | Data across 10 operations | **31–56×** faster (304–325μs vs 9,751–18,510μs) |
| 3 | Conditional branching | If/else over 10 operations | **29–62×** faster (301–333μs vs 9,247–19,165μs) |
| 4 | Loop/ForEach processing | 50 items through the pipeline | **64–121×** faster (450–494μs vs 30,742–58,346μs) |
| 5 | Concurrent execution | Eight workflows at once | **123–251×** faster (276–372μs vs 7,588–87,491μs) |
| 6 | Error handling | Exceptions and recovery paths | **13–108×** faster (70–114μs vs 1,349–7,737μs) |
| 7 | Creation overhead | Instantiation cost only | **38–207×** faster (11–11.4μs vs 819–2,329μs) |
| 8 | Complete lifecycle | Create, execute, tear down | Workflow Core omitted: `WorkflowHost.Start()` background threads skew rapid repeat lifecycles (see exclusions doc). **165–272×** faster than Elsa (36–59μs vs 9,723–9,878μs) |
| 9 | State machine workflow | 25 conditional transitions | **305–511×** faster (65–71μs vs 21,683–33,062μs) |
| 10 | Long running workflow | Multi-phase run with one delay | **51–63×** faster (39ms vs 51ms) |
| 11 | Parallel scaling | 16 operations in parallel | **43–429×** faster (57–68μs vs 2,794–24,637μs) |
| 12 | Event-driven workflow | Event-triggered execution | **2.7–2.9×** faster than Elsa; **11–288×** lower allocation |

## Running

```bash
dotnet run --project WorkflowForge.Benchmarks.csproj --configuration Release
dotnet run --project WorkflowForge.Benchmarks.Comparative.csproj --configuration Release
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

- **13–511×** faster execution across the 12 scenarios in these runs
- **6–575×** lower allocation where we measured allocations
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
- **Iterations:** 50 per benchmark, 5 warmup

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
