# WorkflowForge Comparative Benchmarks

**Head-to-head performance comparison: WorkflowForge vs Workflow Core vs Elsa Workflows**

**Last Updated**: March 2026

## Overview

This project contains fair, apple-to-apple performance benchmarks comparing WorkflowForge against popular .NET workflow frameworks using identical workflow scenarios implemented in each framework's idiomatic style.

## Frameworks Tested

- **WorkflowForge 2.1.0** - Zero-dependency, microsecond-scale orchestration
- **Workflow Core** - Popular persistence-first workflow engine
- **Elsa Workflows** - Modern workflow engine with designer support

## Benchmark Scenarios

### Scenario 1: Simple Sequential Workflow
**Test**: 10 operations executed in sequence  
**Result**: WorkflowForge 26-51x faster (237-422μs vs 9,878-18,675μs)

### Scenario 2: Data Passing Workflow
**Test**: Pass data between 10 operations  
**Result**: WorkflowForge 31-56x faster (304-325μs vs 9,751-18,510μs)

### Scenario 3: Conditional Branching
**Test**: Conditional logic with if/else branches (10 operations)  
**Result**: WorkflowForge 29-62x faster (301-333μs vs 9,247-19,165μs)

### Scenario 4: Loop/ForEach Processing
**Test**: Process 50 items through operations  
**Result**: WorkflowForge 64-121x faster (450-494μs vs 30,742-58,346μs)

### Scenario 5: Concurrent Execution
**Test**: 8 workflows executing in parallel  
**Result**: WorkflowForge 123-251x faster (276-372μs vs 7,588-87,491μs)

### Scenario 6: Error Handling
**Test**: Exception handling and recovery  
**Result**: WorkflowForge 13-108x faster (70-114μs vs 1,349-7,737μs)

### Scenario 7: Creation Overhead
**Test**: Workflow instantiation cost  
**Result**: WorkflowForge 38-207x faster (11-11.4μs vs 819-2,329μs)

### Scenario 8: Complete Lifecycle
**Test**: Create + Execute + Cleanup  
**Note**: WorkflowCore excluded due to architectural incompatibility with rapid lifecycle benchmarking (background worker threads)  
**Result**: WorkflowForge 165-272x faster vs Elsa (36-59μs vs 9,723-9,878μs)

### Scenario 9: State Machine Workflow
**Test**: State machine pattern with conditional transitions (25 transitions)
**Result**: WorkflowForge 305-511x faster (65-71μs vs 21,683-33,062μs)

### Scenario 10: Long Running Workflow
**Test**: Extended workflow execution with multiple phases
**Result**: WorkflowForge 51-63x faster (39ms vs 51ms, 1 delay)

### Scenario 11: Parallel Scaling
**Test**: Parallel execution with 16 operations
**Result**: WorkflowForge 43-429x faster (57-68μs vs 2,794-24,637μs)

### Scenario 12: Event-Driven Workflow
**Test**: Event-triggered workflow execution
**Result**: WorkflowForge 2.7-2.9x faster vs Elsa, 11-288x less memory

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

- **13-511x faster execution** across 12 scenarios
- **6-575x less memory allocation**
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
- **Runtimes**: .NET 10.0.3, .NET 8.0.24, .NET Framework 4.8.1
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

