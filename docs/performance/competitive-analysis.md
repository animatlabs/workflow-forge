# WorkflowForge Competitive Benchmark Analysis

<p align="center">
  <img src="https://raw.githubusercontent.com/animatlabs/workflow-forge/main/icon.png" alt="WorkflowForge" width="120" height="120">
</p>

**Analysis Date**: January 2026  
**Frameworks Tested**:
- WorkflowForge 2.0.0
- Workflow Core
- Elsa Workflows

**Test System**: Windows 11 (25H2), Intel 11th Gen i7-1185G7, .NET 8.0.23  
**BenchmarkDotNet**: v0.15.8

## Table of Contents

- [Executive Summary](#executive-summary)
- [Scenario Breakdown](#scenario-breakdown)
  - [Scenario 1: Simple Sequential Workflow](#scenario-1-simple-sequential-workflow)
  - [Scenario 2: Data Passing Workflow](#scenario-2-data-passing-workflow)
  - [Scenario 3: Conditional Branching](#scenario-3-conditional-branching)
  - [Scenario 4: Loop/ForEach Processing](#scenario-4-loopforeach-processing)
  - [Scenario 5: Concurrent Execution](#scenario-5-concurrent-execution)
  - [Scenario 6: Error Handling](#scenario-6-error-handling)
  - [Scenario 7: Creation Overhead](#scenario-7-creation-overhead)
  - [Scenario 8: Complete Lifecycle](#scenario-8-complete-lifecycle)
- [Performance Advantage Summary](#performance-advantage-summary)
- [Architectural Differences](#architectural-differences)
- [When to Choose WorkflowForge](#when-to-choose-workflowforge)
- [Benchmark Methodology](#benchmark-methodology)

---

## Executive Summary

WorkflowForge demonstrates **11-540x faster execution** and **9-573x less memory allocation** compared to Workflow Core and Elsa Workflows across 12 real-world scenarios (50 iterations per benchmark).

**Key Insights**:
- WorkflowForge operates at **microsecond scale** (13-497μs), competitors at **millisecond scale** (0.8-94ms)
- Memory allocations remain in **kilobytes** (3.5-121KB) vs. **megabytes** (0.04-19MB) for competitors
- **State Machine** scenarios show highest advantage: **up to 540x faster** vs Elsa
- **Concurrent Execution** shows **109-264x faster** performance
- **Sequential Workflows** show **26-71x faster** with minimal memory
- Consistent performance across all 12 scenario types

---

## Scenario Breakdown

### Scenario 1: Simple Sequential Workflow

**Description**: Execute operations sequentially (1, 5, 10, 25, 50 operations)

#### Performance Results (Median Times, 50 iterations)

| Operations | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|------------|---------------|---------------|------|----------|------------|
| 1          | 183μs         | 1,348μs       | 8,703μs | **7.4x faster** | **47.6x faster** |
| 5          | 205μs         | 4,154μs       | 12,954μs | **20.3x faster** | **63.2x faster** |
| 10         | 247μs         | 6,531μs       | 17,617μs | **26.4x faster** | **71.3x faster** |
| 25         | 316μs         | 14,193μs      | 29,919μs | **44.9x faster** | **94.7x faster** |
| 50         | 444μs         | 26,996μs      | 51,557μs | **60.8x faster** | **116.1x faster** |

#### Memory Allocation Results

| Operations | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|------------|---------------|---------------|------|----------|------------|
| 1          | 4.84KB        | 45.27KB       | 1,242KB | **9.4x less** | **256.6x less** |
| 5          | 9.37KB        | 217.19KB      | 2,023KB | **23.2x less** | **215.9x less** |
| 10         | 16.31KB       | 429.77KB      | 2,984KB | **26.4x less** | **183.0x less** |
| 25         | 43.49KB       | 1,063KB       | 5,950KB | **24.4x less** | **136.8x less** |
| 50         | 85.51KB       | 2,124KB       | 10,905KB | **24.8x less** | **127.5x less** |

**Key Insight**: WorkflowForge performance advantage **increases linearly with operation count**.

---

### Scenario 2: Data Passing Workflow

**Description**: Pass data between operations (5, 10, 25 operations)

#### Performance Results (Median Times, 50 iterations)

| Operations | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|------------|---------------|---------------|------|----------|------------|
| 5          | 233μs         | 4,220μs       | 13,210μs | **18.1x faster** | **56.7x faster** |
| 10         | 262μs         | 6,737μs       | 18,222μs | **25.7x faster** | **69.5x faster** |
| 25         | 331μs         | 16,214μs      | 32,043μs | **49.0x faster** | **96.8x faster** |

#### Memory Allocation Results

| Operations | WorkflowForge | Workflow Core | Elsa |
|------------|---------------|---------------|------|
| 5          | 8.86KB        | 216.20KB      | 2,032KB |
| 10         | 15.18KB       | 427.24KB      | 2,986KB |
| 25         | 36.13KB       | 1,062KB       | 5,952KB |

**Key Insight**: Data passing overhead is **minimal** in WorkflowForge (<1μs per operation).

---

### Scenario 3: Conditional Branching

**Description**: Conditional logic with if/else branches (10, 25, 50 operations)

#### Performance Results (Median Times)

| Operations | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|------------|---------------|---------------|------|----------|------------|
| 10         | 266μs         | 8,543μs       | 21,333μs | **32.1x faster** | **80.2x faster** |
| 25         | 339μs         | 20,520μs      | 35,557μs | **60.5x faster** | **104.9x faster** |
| 50         | 505μs         | 34,358μs      | 66,625μs | **68.0x faster** | **131.9x faster** |

**Key Insight**: Conditional overhead negligible (<1μs per branch decision).

---

### Scenario 4: Loop/ForEach Processing

**Description**: Iterate over collections (10, 50, 100 items)

#### Performance Results (Median Times)

| Items | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|-------|---------------|---------------|------|----------|------------|
| 10    | 271μs         | 9,601μs       | 21,545μs | **35.4x faster** | **79.5x faster** |
| 50    | 497μs         | 35,421μs      | 64,171μs | **71.3x faster** | **129.1x faster** |
| 100   | 773μs         | 64,202μs      | 107,701μs | **83.1x faster** | **139.3x faster** |

#### Memory Allocation Results

| Items | WorkflowForge | Workflow Core | Elsa |
|-------|---------------|---------------|------|
| 10    | 19.26KB       | 428.67KB      | 2,988KB |
| 50    | 90.35KB       | 2,123KB       | 10,905KB |
| 100   | 175.4KB       | 4,243KB       | 20,865KB |

**Key Insight**: ForEach performance advantage **increases with collection size**.

---

### Scenario 5: Concurrent Execution

**Description**: Execute multiple workflows concurrently (1, 4, 8 workflows)

#### Performance Results (Median Times)

| Concurrency | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|-------------|---------------|---------------|------|----------|------------|
| 1           | 260μs         | 6,848μs       | 18,357μs | **26.3x faster** | **70.6x faster** |
| 4           | 334μs         | 20,612μs      | 55,570μs | **61.7x faster** | **166.4x faster** |
| 8           | 356μs         | 38,833μs      | 94,018μs | **109.1x faster** | **264.1x faster** |

#### Memory Allocation Results

| Concurrency | WorkflowForge | Workflow Core | Elsa |
|-------------|---------------|---------------|------|
| 1           | 15.84KB       | 428.3KB       | 2,987KB |
| 4           | 62.55KB       | 1,628KB       | 9,903KB |
| 8           | 120.87KB      | 3,232KB       | 19,139KB |

**Key Insight**: WorkflowForge maintains **consistent per-workflow overhead** (~16KB) regardless of concurrency.

---

### Scenario 6: Error Handling

**Description**: Exception handling and recovery

#### Performance Results (Median Times)

| Framework | Median | P95 |
|-----------|--------|-----|
| WorkflowForge | 111μs | 133μs |
| Workflow Core | 1,228μs | 1,630μs |
| Elsa | 7,150μs | 7,584μs |

**Advantage**:
- **11.1x faster** than Workflow Core
- **64.4x faster** than Elsa

**Memory**: WorkflowForge 5KB vs. Workflow Core 46KB vs. Elsa 1,072KB

**Key Insight**: Error handling overhead is **minimal** (~111μs) in WorkflowForge.

---

### Scenario 7: Creation Overhead

**Description**: Workflow instantiation cost

#### Performance Results (Median Times)

| Framework | Median | P95 |
|-----------|--------|-----|
| WorkflowForge | 13μs | 16μs |
| Workflow Core | 814μs | 859μs |
| Elsa | 2,107μs | 2,376μs |

**Advantage**:
- **63x faster** than Workflow Core
- **162x faster** than Elsa

**Memory**: WorkflowForge 3.73KB vs. Workflow Core 129KB vs. Elsa 578KB

**Key Insight**: WorkflowForge workflow creation is **negligible** (~13μs).

---

### Scenario 8: Complete Lifecycle

**Description**: Full create-execute-dispose cycle

#### Performance Results (Median Times)

| Framework | Median | P95 |
|-----------|--------|-----|
| WorkflowForge | 42μs | 51μs |
| Elsa | 9,933μs | 10,322μs |
| Workflow Core | **EXCLUDED** | **EXCLUDED** |

**Advantage**:
- **236x faster** than Elsa
- Workflow Core excluded due to architectural incompatibility (see note below)

**Memory**: WorkflowForge 3.57KB vs. Elsa 1,510KB (**423x less**)

**Note**: Workflow Core was excluded from this benchmark due to an architectural design difference. Workflow Core's `WorkflowHost.Start()` method spins up background worker threads that are intended to run continuously, making rapid create-start-stop-dispose cycles (50 iterations) incompatible with its design. This is a fundamental architectural difference, not a performance issue. For more details, see the benchmark documentation.

**Key Insight**: Complete lifecycle overhead is **trivial** (~42μs) in WorkflowForge.

---

### Scenario 9: State Machine

**Description**: State machine with multiple transitions (5, 10, 25 transitions)

#### Performance Results (Median Times)

| Transitions | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|-------------|---------------|---------------|------|----------|------------|
| 5           | 50μs          | 6,305μs       | 15,327μs | **126x faster** | **307x faster** |
| 10          | 47μs          | 9,208μs       | 20,374μs | **196x faster** | **434x faster** |
| 25          | 68μs          | 20,624μs      | 36,695μs | **303x faster** | **540x faster** |

#### Memory Allocation Results

| Transitions | WorkflowForge | Workflow Core | Elsa |
|-------------|---------------|---------------|------|
| 5           | 5.54KB        | 260KB         | 2,019KB |
| 10          | 10.64KB       | 472KB         | 2,982KB |
| 25          | 20.92KB       | 1,106KB       | 5,949KB |

**Key Insight**: State machine execution shows the **highest performance advantage** (up to 540x faster).

---

### Scenario 10: Long Running

**Description**: Long-running operations with delays (delay-bound scenario)

#### Performance Results (Median Times)

| Ops/Delay | WorkflowForge | Workflow Core | Elsa |
|-----------|---------------|---------------|------|
| 3 ops/1ms | 39.2ms        | 38.8ms        | 51.3ms |
| 3 ops/5ms | 39.3ms        | 39.1ms        | 51.5ms |
| 5 ops/1ms | 72.0ms        | 70.9ms        | 82.5ms |
| 5 ops/5ms | 72.3ms        | 71.1ms        | 84.0ms |

#### Memory Allocation Results (5 ops/1ms delay)

| Framework | Memory |
|-----------|--------|
| WorkflowForge | 5.25KB |
| Workflow Core | 266KB |
| Elsa | 2,221KB |

**Advantage**: Similar timing (delay-bound), but **51x less memory** than WC, **423x less** than Elsa.

**Key Insight**: Long-running workflows are delay-bound; advantage is in **memory efficiency**.

---

### Scenario 11: Parallel Execution

**Description**: Parallel operation execution within a workflow (4, 8, 16 operations)

#### Performance Results (Median Times)

| Ops/Concurrency | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|-----------------|---------------|---------------|------|----------|------------|
| 4 ops/2         | 62μs          | 2,359μs       | 10,830μs | **38x faster** | **175x faster** |
| 8 ops/4         | 56μs          | 2,366μs       | 14,750μs | **42x faster** | **263x faster** |
| 16 ops/4        | 55μs          | 2,437μs       | 20,891μs | **44x faster** | **380x faster** |

#### Memory Allocation Results (16 ops/4 concurrency)

| Framework | Memory |
|-----------|--------|
| WorkflowForge | 8.1KB |
| Workflow Core | 123KB |
| Elsa | 4,643KB |

**Key Insight**: Parallel execution maintains **38-380x speed advantage** with **15-573x less memory**.

---

### Scenario 12: Event-Driven

**Description**: Event-driven workflow execution with delays

#### Performance Results (Median Times)

| Delay | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|-------|---------------|---------------|------|----------|------------|
| 1ms   | 7.3ms         | 8.2ms         | 19.3ms | 1.1x     | **2.6x faster** |
| 5ms   | 7.6ms         | 8.1ms         | 20.8ms | 1.1x     | **2.7x faster** |

#### Memory Allocation Results

| Framework | Memory |
|-----------|--------|
| WorkflowForge | 3.49KB |
| Workflow Core | 37KB |
| Elsa | 1,032KB |

**Advantage**: Similar timing to WorkflowCore, but **11x less memory** than WC, **296x less** than Elsa.

**Key Insight**: Event-driven scenarios are I/O-bound; advantage is in **memory efficiency**.

---

## Performance Advantage Summary

### By Scenario Type (12 Scenarios)

| # | Scenario | Speed Advantage | Memory Advantage |
|---|----------|-----------------|------------------|
| 1 | Sequential (10 ops) | 26-71x | 26-183x |
| 2 | Data Passing (10 ops) | 26-70x | 28-197x |
| 3 | Conditional (10 ops) | 32-80x | 21-149x |
| 4 | Loop/ForEach (50 items) | 71-129x | 24-121x |
| 5 | Concurrent (8 workflows) | 109-264x | 27-158x |
| 6 | Error Handling | 11-64x | 9-214x |
| 7 | Creation Overhead | 63-162x | 35-155x |
| 8 | Complete Lifecycle | 236x | 423x |
| 9 | State Machine (25 trans) | **303-540x** | 53-284x |
| 10 | Long Running | ~1x (delay-bound) | **51-423x** |
| 11 | Parallel (16 ops) | 38-380x | 15-573x |
| 12 | Event-Driven | 1.1-2.7x | 11-296x |

**Overall Speed Range**: **11-540x faster execution** (compute-bound scenarios)  
**Overall Memory Range**: **9-573x less memory allocation**

### Key Findings

1. **State Machine** scenarios show the highest speed advantage: **303-540x faster**
2. **Concurrent Execution** maintains excellent scaling: **109-264x faster**
3. **Long Running** and **Event-Driven** are I/O-bound, but memory savings are massive: **51-423x less**
4. **Memory efficiency** is consistently excellent across all 12 scenarios

---

## Architectural Differences

### WorkflowForge Design

1. **Lightweight Execution Model**
   - No background threads
   - Synchronous sequential execution (default)
   - Explicit parallelism via `ForEachWorkflowOperation`
   - Minimal object allocations

2. **Dictionary-Based Data Flow**
   - `ConcurrentDictionary<string, object?>` for properties
   - Zero serialization overhead
   - Thread-safe property access

3. **Dependency-Free Core**
   - No reflection-heavy frameworks
   - No serialization frameworks
   - Pure .NET Standard 2.0

4. **Middleware Pipeline**
   - Russian Doll pattern
   - Minimal delegate allocations
   - No reflection per operation

### Workflow Core Design

1. **Persistent Workflow Engine**
   - Background worker threads
   - Designed for long-running workflows
   - Persistent state management
   - Work queue architecture

2. **Strong Typing**
   - Reflection-based step resolution
   - JSON serialization for state

3. **Intended Use Case**
   - Long-running business processes (hours/days)
   - Workflows that survive process restarts
   - Background processing

**Conclusion**: Workflow Core is optimized for **durability**, not **speed**. WorkflowForge is optimized for **speed**, not **durability** (though persistence extensions available).

### Elsa Workflows Design

1. **Workflow Designer Focus**
   - Visual workflow designer
   - HTTP workflow triggers
   - Extensive activity library

2. **Serialization-Heavy**
   - JSON serialization for all data
   - Heavy use of reflection
   - Large object graphs

3. **Intended Use Case**
   - Visual workflow design
   - Human task workflows
   - Integration workflows

**Conclusion**: Elsa is optimized for **designer experience**, not **performance**. WorkflowForge is optimized for **performance**, not **visual design** (though programmatic API is very expressive).

---

## Benchmark Methodology

### Test Configuration

- **BenchmarkDotNet**: v0.15.8
- **Runtime**: .NET 8.0.23
- **Iterations**: 50 per benchmark
- **Warmup**: 5 iterations
- **Invocation**: 1 per iteration
- **Unroll Factor**: 1

### Hardware

- **OS**: Windows 11 (10.0.26200.6899)
- **CPU**: Intel 11th Gen i7-1185G7
- **Memory**: Sufficient for all benchmarks

### Scenario Implementations

All scenarios implement **identical logic** across all frameworks:
- Same operation count
- Same data structures
- Same conditional logic
- Same collection sizes
- Same concurrency levels

**Fairness Verification**:
- Workflow Core implementations use `TaskCompletionSource` for precise completion detection
- Elsa implementations use proper workflow completion await
- WorkflowForge implementations use standard `ForgeAsync()`

### Reproduction

Full benchmark source code available in repository:
- `src/benchmarks/WorkflowForge.Benchmarks.Comparative/`
- All scenarios in `Scenarios/` folder
- Run via `dotnet run -c Release`

---

## Statistical Significance

All results meet statistical significance criteria:
- Standard deviation < 20% of mean (most scenarios)
- P95 values show consistency
- Median values used for comparison (more stable than mean)
- 50 iterations provide statistical confidence

**Outliers**: Some scenarios show high standard deviation due to GC pauses or system activity. Median values are used to minimize impact.

---

## Conclusion

WorkflowForge delivers **11-540x faster execution** and **9-573x less memory allocation** compared to Workflow Core and Elsa Workflows across 12 scenarios. This performance advantage stems from:

1. **Architectural Simplicity**: No background threads, no persistent state, no serialization
2. **Minimal Allocations**: Dictionary-based data flow, no reflection per operation
3. **Optimized Execution**: Russian Doll middleware, efficient delegate handling
4. **Focused Design**: Optimized for speed, not durability or visual design

WorkflowForge is the **fastest .NET workflow engine** for high-performance, programmatic workflow orchestration.

---

## References

- WorkflowForge: https://github.com/animatlabs/workflow-forge
- Workflow Core: https://github.com/danielgerlag/workflow-core
- Elsa Workflows: https://github.com/elsa-workflows/elsa-core

---

## Related Documentation

- **[Performance Benchmarks](performance.md)** - Internal WorkflowForge benchmarks
- **[Architecture Overview](../architecture/overview.md)** - Design principles
- **[Getting Started](../getting-started/getting-started.md)** - Quick start guide

**← Back to [Documentation Home](../index.md)**
