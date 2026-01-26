# WorkflowForge Comparative Benchmarks

<p align="center">
  <img src="https://raw.githubusercontent.com/animatlabs/workflow-forge/main/icon.png" alt="WorkflowForge" width="120" height="120">
</p>

**Head-to-head performance comparison: WorkflowForge vs Workflow Core vs Elsa Workflows**

**Last Updated**: January 2026

## Overview

This project contains fair, apple-to-apple performance benchmarks comparing WorkflowForge against popular .NET workflow frameworks using identical workflow scenarios implemented in each framework's idiomatic style.

## Frameworks Tested

- **WorkflowForge 2.0.0** - Zero-dependency, microsecond-scale orchestration
- **Workflow Core** - Popular persistence-first workflow engine
- **Elsa Workflows** - Modern workflow engine with designer support

## Benchmark Scenarios

### Scenario 1: Simple Sequential Workflow
**Test**: 10 operations executed in sequence  
**Result**: WorkflowForge 26-71x faster (247μs vs 6,531-17,617μs)

### Scenario 2: Data Passing Workflow
**Test**: Pass data between 10 operations  
**Result**: WorkflowForge 26-70x faster (262μs vs 6,737-18,222μs)

### Scenario 3: Conditional Branching
**Test**: Conditional logic with if/else branches (10 operations)  
**Result**: WorkflowForge 32-80x faster (266μs vs 8,543-21,333μs)

### Scenario 4: Loop/ForEach Processing
**Test**: Process 50 items through operations  
**Result**: WorkflowForge 71-129x faster (497μs vs 35,421-64,171μs)

### Scenario 5: Concurrent Execution
**Test**: 8 workflows executing in parallel  
**Result**: WorkflowForge 109-264x faster (356μs vs 38,833-94,018μs)

### Scenario 6: Error Handling
**Test**: Exception handling and recovery  
**Result**: WorkflowForge 11-64x faster (111μs vs 1,228-7,150μs)

### Scenario 7: Creation Overhead
**Test**: Workflow instantiation cost  
**Result**: WorkflowForge 63-162x faster (13μs vs 814-2,107μs)

### Scenario 8: Complete Lifecycle
**Test**: Create + Execute + Cleanup  
**Note**: WorkflowCore excluded due to architectural incompatibility with rapid lifecycle benchmarking (background worker threads)  
**Result**: WorkflowForge 236x faster vs Elsa (42μs vs 9,933μs)

## Running Benchmarks

### Run All Scenarios

```bash
dotnet run --project WorkflowForge.Benchmarks.csproj --configuration Release
dotnet run --project WorkflowForge.Benchmarks.Comparative.csproj --configuration Release
```

This runs all 12 scenarios and saves results to `BenchmarkDotNet.Artifacts/`.

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

- **11-540x faster execution** across 12 scenarios
- **9-573x less memory allocation**
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

- **OS**: Windows 11 (25H2)
- **CPU**: Intel 11th Gen i7-1185G7
- **.NET**: 8.0.23
- **BenchmarkDotNet**: v0.15.8
- **Iterations**: 50 per benchmark, 5 warmup

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
