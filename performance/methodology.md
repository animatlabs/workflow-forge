# Benchmark Methodology

This document defines how we run comparative benchmarks and keep results fair across WorkflowForge, WorkflowCore, and Elsa.

**Update Note**: Methodology remains stable, but benchmark outputs will be refreshed after the upcoming rerun.

## Goals

- Keep scenarios functionally equivalent across frameworks.
- Use identical input sizes, delays, and iteration counts.
- Measure runtime and memory in a repeatable, transparent way.

## Execution Environment

- Build: `Release`
- Target: `net8.0`
- Tooling: `BenchmarkDotNet`
- Diagnostics: `MemoryDiagnoser` + Markdown/HTML exporters
- Iterations: 5 warmups, 25 measurements (per scenario)
- Config: Optimizations validator disabled (BenchmarkDotNet config)

## Fairness Principles

- **Same work per scenario:** identical operation counts and data sizes.
- **Same concurrency inputs:** matching parallelism and workflow counts.
- **Same delay model:** fixed millisecond delays for long-running and event-driven scenarios.
- **Same lifecycle boundaries:** setup/cleanup handled per-iteration to avoid hidden reuse.
- **No hidden caching:** scenarios build new workflows each iteration unless the scenario is explicitly about reuse.

## Scenario Consistency

### 1–8 (Existing Scenarios)

All implementations now follow the same structure:
- Setup: instantiate framework services and workflow definitions.
- Execute: run the same logical workflow operations.
- Cleanup: dispose resources deterministically.

Scenario 8 excludes WorkflowCore due to architectural incompatibility with repeated start/stop cycles. Details are in `src/benchmarks/WorkflowForge.Benchmarks.Comparative/BENCHMARK_EXCLUSIONS.md`.

### 9–12 (New Scenarios)

- **Scenario 9: State Machine**  
  Sequential transitions with a fixed count. Each transition increments state.

- **Scenario 10: Long Running**  
  Sequential operations with fixed millisecond delays. Delay occurs per step.

- **Scenario 11: Parallel Execution**  
  Parallel branches within a single workflow. Branch count and concurrency are parameterized.

- **Scenario 12: Event-Driven**  
  External event simulated by a delayed signal. The workflow waits, then continues.

## How to Run

From `src/benchmarks/WorkflowForge.Benchmarks.Comparative`:

```bash
dotnet run --configuration Release -- scenario1
dotnet run --configuration Release -- scenario9
dotnet run --configuration Release -- scenario12
```

To run all scenarios:

```bash
dotnet run --configuration Release
```

## Output Validation

Each benchmark returns a `ScenarioResult` with:
- `Success` flag to confirm functional equivalence
- `OperationsExecuted` count for sanity checks
- `OutputData` for quick verification
- `Metadata` that includes framework identity

If any scenario reports `Success = false`, treat the benchmark as invalid and investigate before comparing results.
