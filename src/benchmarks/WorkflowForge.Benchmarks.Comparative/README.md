# WorkflowForge Comparative Benchmarks

<p align="center">
  <img src="../../../icon.png" alt="WorkflowForge" width="120" height="120">
</p>

**Head-to-head performance comparison: WorkflowForge vs Workflow Core vs Elsa Workflows**

## Overview

This project contains fair, apple-to-apple performance benchmarks comparing WorkflowForge against popular .NET workflow frameworks using identical workflow scenarios implemented in each framework's idiomatic style.

## Frameworks Tested

- **WorkflowForge 2.0.0** - Zero-dependency, microsecond-scale orchestration
- **Workflow Core 3.17.0** - Popular persistence-first workflow engine
- **Elsa Workflows 3.5.1** - Modern workflow engine with designer support

## Benchmark Scenarios

### Scenario 1: Simple Sequential Workflow
**Test**: 3 operations executed in sequence  
**Result**: WorkflowForge 13-23x faster (25 μs vs 311-575 μs)

### Scenario 2: Data Passing Workflow
**Test**: Pass data between 3 operations  
**Result**: WorkflowForge 44-59x faster (17 μs vs 757-1,003 μs)

### Scenario 3: Conditional Branching
**Test**: If/else logic with 2 operations  
**Result**: WorkflowForge 22-42x faster (12 μs vs 260-510 μs)

### Scenario 4: Loop/ForEach Processing
**Test**: Process 5 items through operation  
**Result**: WorkflowForge 49-98x faster (25 μs vs 1,222-2,456 μs)

### Scenario 5: Concurrent Execution
**Test**: 3 operations executing in parallel  
**Result**: WorkflowForge 15-75x faster (118 μs vs 1,728-8,819 μs)

### Scenario 6: Error Handling
**Test**: Operation fails, handle gracefully  
**Result**: WorkflowForge 54-105x faster (26 μs vs 1,399-2,751 μs)

### Scenario 7: Creation Overhead
**Test**: Create workflow definition  
**Result**: WorkflowForge 89-378x faster (2 μs vs 178-775 μs)

### Scenario 8: Complete Lifecycle
**Test**: Create + Execute + Cleanup  
**Note**: WorkflowCore excluded due to architectural incompatibility with rapid lifecycle benchmarking (background worker threads)  
**Result**: WorkflowForge 117x faster vs Elsa (30 μs vs 3,516 μs)

## Running Benchmarks

### Run All Scenarios

```bash
dotnet run --project WorkflowForge.Benchmarks.csproj --configuration Release
dotnet run --project WorkflowForge.Benchmarks.Comparative.csproj --configuration Release
```

This runs all 8 scenarios and saves results to `BenchmarkDotNet.Artifacts/`.

### Run Specific Scenario

```bash
dotnet run -c Release --filter *Scenario1*
```

### Quick Validation (1 iteration)

```bash
dotnet run -c Release --job short
```

## Results Location

Benchmark results are saved to:
- `BenchmarkDotNet.Artifacts/results/` - Detailed reports (HTML, CSV, MD)
- Individual scenario subdirectories with comparative data

## Key Findings

### Performance Advantage

- **13-378x faster execution** across all scenarios
- **6-405x less memory allocation**
- **Consistent microsecond-scale performance** vs millisecond-scale competitors

### Why WorkflowForge is Faster

1. **Zero Dependencies**: No framework overhead
2. **Direct Execution**: No intermediary abstractions or event loops
3. **Optimized Memory**: Minimal allocations, efficient data structures
4. **No Background Threads**: Synchronous execution model
5. **Lightweight Operations**: Direct delegate invocation

### Trade-offs

**WorkflowForge strengths**:
- Raw execution speed
- Memory efficiency
- Simple deployment
- Flexible data flow

**Workflow Core strengths**:
- Built-in persistence
- Long-running workflows
- Event-driven architecture
- Mature ecosystem

**Elsa Workflows strengths**:
- Visual designer
- HTTP workflows
- Rich activity library
- UI/API integration

## Architectural Differences

### WorkflowForge
- In-memory, direct execution
- Dictionary-based data flow
- Microsecond-scale operations
- Zero external dependencies

### Workflow Core
- Persistence-first design
- Background worker threads
- Event-driven state machine
- Requires external persistence

### Elsa Workflows
- HTTP-first workflows
- Rich activity library
- Designer integration
- Modular architecture

## Fair Comparison Notes

All benchmarks:
- Implement identical workflow logic
- Use framework-idiomatic patterns
- Run with realistic scenarios
- Measure complete execution time
- Include memory allocation data
- Use latest stable versions

**Exclusions**:
- Scenario 8 excludes Workflow Core due to architectural incompatibility with `WorkflowHost.Start()` background threads

See [BENCHMARK_EXCLUSIONS.md](BENCHMARK_EXCLUSIONS.md) for technical details on exclusions.

## Test System

- **OS**: Windows 11
- **CPU**: Intel 11th Gen i7-1185G7
- **.NET**: 8.0.21
- **BenchmarkDotNet**: v0.13.12
- **Iterations**: 25 per benchmark, 5 warmup

## Documentation

- **[Performance Documentation](../../../docs/performance/performance.md)** - Detailed performance analysis
- **[Competitive Analysis](../../../docs/performance/competitive-analysis.md)** - Framework comparison
- **[Architecture](../../../docs/architecture/overview.md)** - Design decisions

## Reproducing Results

1. Clone repository
2. Restore NuGet packages: `dotnet restore`
3. Run benchmarks for everything or the desired scenario(s)
4. Results saved to `BenchmarkDotNet.Artifacts/`

All benchmark code is open source and auditable.

---

**WorkflowForge Comparative Benchmarks** - *Build workflows with industrial strength*
