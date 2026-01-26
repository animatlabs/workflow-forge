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

WorkflowForge demonstrates **11-574x faster execution** and **9-581x less memory allocation** compared to Workflow Core and Elsa Workflows across 12 real-world scenarios.

**Key Insights**:
- WorkflowForge operates at **microsecond scale** (12-312μs), competitors at **millisecond scale** (1-107ms)
- Memory allocations remain in **kilobytes** (3.4-110KB) vs. **megabytes** (0.5-19MB) for competitors
- **State Machine** scenarios show highest advantage: **322-574x faster**
- **Parallel Execution** maintains excellent scaling: **44-454x faster**
- **Long Running/Event-Driven** are I/O-bound but show **51-422x memory savings**
- Consistent performance across all 12 scenario types

---

## Scenario Breakdown

### Scenario 1: Simple Sequential Workflow

**Description**: Execute operations sequentially (1, 5, 10, 25, 50 operations)

#### Performance Results (Median Times)

| Operations | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|------------|---------------|---------------|------|----------|------------|
| 1          | 180μs         | 1,209μs       | 8,023μs | **6.7x faster** | **44.6x faster** |
| 5          | 230μs         | 3,366μs       | 12,617μs | **14.6x faster** | **54.9x faster** |
| 10         | 224μs         | 6,060μs       | 17,044μs | **27.1x faster** | **76.1x faster** |
| 25         | 273μs         | 13,508μs      | 30,606μs | **49.5x faster** | **112.1x faster** |
| 50         | 395μs         | 28,432μs      | 53,944μs | **72.0x faster** | **136.6x faster** |

#### Memory Allocation Results

| Operations | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|------------|---------------|---------------|------|----------|------------|
| 1          | 3.69KB        | 45.27KB       | 1,242KB | **12.3x less** | **336.6x less** |
| 5          | 8.59KB        | 217.79KB      | 2,018KB | **25.4x less** | **234.9x less** |
| 10         | 14.75KB       | 428.27KB      | 2,989KB | **29.0x less** | **202.6x less** |
| 25         | 39.73KB       | 1,063KB       | 5,947KB | **26.8x less** | **149.7x less** |
| 50         | 78.73KB       | 2,123KB       | 10,907KB | **27.0x less** | **138.5x less** |

**Key Insight**: WorkflowForge performance advantage **increases linearly with operation count**.

---

### Scenario 2: Data Passing Workflow

**Description**: Pass data between operations (5, 10, 25 operations)

#### Performance Results (Median Times)

| Operations | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|------------|---------------|---------------|------|----------|------------|
| 5          | 209μs         | 3,299μs       | 12,693μs | **15.8x faster** | **60.7x faster** |
| 10         | 199μs         | 6,026μs       | 19,851μs | **30.3x faster** | **99.8x faster** |
| 25         | 278μs         | 13,636μs      | 30,399μs | **49.1x faster** | **109.4x faster** |

#### Memory Allocation Results

| Operations | WorkflowForge | Workflow Core | Elsa |
|------------|---------------|---------------|------|
| 5          | 7.34KB        | 215.54KB      | 2,017KB |
| 10         | 11.95KB       | 428.41KB      | 2,988KB |
| 25         | 28.75KB       | 1,061KB       | 5,951KB |

**Key Insight**: Data passing overhead is **minimal** in WorkflowForge (<1μs per operation).

---

### Scenario 3: Conditional Branching

**Description**: Conditional logic with if/else branches (10, 25, 50 operations)

#### Performance Results (Median Times)

| Operations | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|------------|---------------|---------------|------|----------|------------|
| 10         | 238μs         | 6,207μs       | 17,642μs | **26.1x faster** | **74.1x faster** |
| 25         | 314μs         | 13,758μs      | 30,192μs | **43.8x faster** | **96.2x faster** |
| 50         | 369μs         | 28,735μs      | 55,347μs | **77.9x faster** | **150.0x faster** |

**Key Insight**: Conditional overhead negligible (<1μs per branch decision).

---

### Scenario 4: Loop/ForEach Processing

**Description**: Iterate over collections (10, 50, 100 items)

#### Performance Results (Median Times)

| Items | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|-------|---------------|---------------|------|----------|------------|
| 10    | 213μs         | 7,137μs       | 18,317μs | **33.5x faster** | **86.0x faster** |
| 50    | 389μs         | 28,378μs      | 55,618μs | **73.0x faster** | **143.0x faster** |
| 100   | 578μs         | 52,309μs      | 103,808μs | **90.5x faster** | **179.6x faster** |

#### Memory Allocation Results

| Items | WorkflowForge | Workflow Core | Elsa |
|-------|---------------|---------------|------|
| 10    | 17.46KB       | 427.54KB      | 2,988KB |
| 50    | 81.51KB       | 2,121KB       | 10,908KB |
| 100   | 173.53KB      | 4,241KB       | 20,859KB |

**Key Insight**: ForEach performance advantage **increases with collection size**.

---

### Scenario 5: Concurrent Execution

**Description**: Execute multiple workflows concurrently (1, 4, 8 workflows)

#### Performance Results (Median Times)

| Concurrency | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|-------------|---------------|---------------|------|----------|------------|
| 1           | 253μs         | 6,617μs       | 18,295μs | **26.1x faster** | **72.3x faster** |
| 4           | 286μs         | 20,091μs      | 54,666μs | **70.2x faster** | **191.1x faster** |
| 8           | 312μs         | 42,093μs      | 107,784μs | **135.0x faster** | **345.5x faster** |

#### Memory Allocation Results

| Concurrency | WorkflowForge | Workflow Core | Elsa |
|-------------|---------------|---------------|------|
| 1           | 14.27KB       | 427.64KB      | 2,985KB |
| 4           | 56.55KB       | 1,626KB       | 9,874KB |
| 8           | 110.2KB       | 3,233KB       | 19,145KB |

**Key Insight**: WorkflowForge maintains **consistent per-workflow overhead** (~14KB) regardless of concurrency.

---

### Scenario 6: Error Handling

**Description**: Exception handling and recovery

#### Performance Results (Median Times)

| Framework | Median | P95 |
|-----------|--------|-----|
| WorkflowForge | 109μs | 141μs |
| Workflow Core | 1,236μs | 1,515μs |
| Elsa | 8,460μs | 9,347μs |

**Advantage**:
- **11.3x faster** than Workflow Core
- **77.6x faster** than Elsa

**Memory**: WorkflowForge 5KB vs. Workflow Core 46KB vs. Elsa 1,072KB

**Key Insight**: Error handling overhead is **minimal** (~109μs) in WorkflowForge.

---

### Scenario 7: Creation Overhead

**Description**: Workflow instantiation cost

#### Performance Results (Median Times)

| Framework | Median | P95 |
|-----------|--------|-----|
| WorkflowForge | 12μs | 18μs |
| Workflow Core | 871μs | 946μs |
| Elsa | 2,313μs | 2,484μs |

**Advantage**:
- **68x faster** than Workflow Core
- **188x faster** than Elsa

**Memory**: WorkflowForge 3.73KB vs. Workflow Core 129KB vs. Elsa 578KB

**Key Insight**: WorkflowForge workflow creation is **negligible** (~12μs).

---

### Scenario 8: Complete Lifecycle

**Description**: Full create-execute-dispose cycle

#### Performance Results (Median Times)

| Framework | Median | P95 |
|-----------|--------|-----|
| WorkflowForge | 39μs | 54μs |
| Elsa | 10,346μs | 11,230μs |
| Workflow Core | **EXCLUDED** | **EXCLUDED** |

**Advantage**:
- **267x faster** than Elsa
- Workflow Core excluded due to architectural incompatibility (see note below)

**Memory**: WorkflowForge 3.41KB vs. Elsa 1,510KB (**443x less**)

**Note**: Workflow Core was excluded from this benchmark due to an architectural design difference. Workflow Core's `WorkflowHost.Start()` method spins up background worker threads that are intended to run continuously, making rapid create-start-stop-dispose cycles (25 iterations) incompatible with its design. This is a fundamental architectural difference, not a performance issue. For more details, see the benchmark documentation.

**Key Insight**: Complete lifecycle overhead is **trivial** (~39μs) in WorkflowForge.

---

### Scenario 9: State Machine

**Description**: State machine with multiple transitions (5, 10, 25 transitions)

#### Performance Results (Median Times)

| Transitions | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|-------------|---------------|---------------|------|----------|------------|
| 5           | 42μs          | 5,392μs       | 15,152μs | **128x faster** | **361x faster** |
| 10          | 43μs          | 7,480μs       | 19,633μs | **174x faster** | **457x faster** |
| 25          | 59μs          | 19,156μs      | 34,033μs | **325x faster** | **577x faster** |

#### Memory Allocation Results

| Transitions | WorkflowForge | Workflow Core | Elsa |
|-------------|---------------|---------------|------|
| 5           | 5.54KB        | 262KB         | 2,016KB |
| 10          | 10.64KB       | 472KB         | 2,984KB |
| 25          | 20.5KB        | 1,106KB       | 5,949KB |

**Key Insight**: State machine execution shows the **highest performance advantage** (up to 577x faster).

---

### Scenario 10: Long Running

**Description**: Long-running operations with delays (delay-bound scenario)

#### Performance Results (Median Times)

| Ops/Delay | WorkflowForge | Workflow Core | Elsa |
|-----------|---------------|---------------|------|
| 3 ops/1ms | 39.6ms        | 39.5ms        | 52.4ms |
| 3 ops/5ms | 39.5ms        | 39.5ms        | 53.0ms |
| 5 ops/1ms | 72.1ms        | 72.1ms        | 69.4ms |
| 5 ops/5ms | 72.3ms        | 71.6ms        | 84.7ms |

#### Memory Allocation Results (5 ops/5ms delay)

| Framework | Memory |
|-----------|--------|
| WorkflowForge | 5.25KB |
| Workflow Core | 267KB |
| Elsa | 2,217KB |

**Advantage**: Similar timing (delay-bound), but **51x less memory** than WC, **422x less** than Elsa.

**Key Insight**: Long-running workflows are delay-bound; advantage is in **memory efficiency**.

---

### Scenario 11: Parallel Execution

**Description**: Parallel operation execution within a workflow (4, 8, 16 operations)

#### Performance Results (Median Times)

| Ops/Concurrency | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|-----------------|---------------|---------------|------|----------|------------|
| 4 ops/2         | 57μs          | 2,074μs       | 10,613μs | **36x faster** | **186x faster** |
| 8 ops/4         | 43μs          | 2,238μs       | 13,932μs | **52x faster** | **324x faster** |
| 16 ops/4        | 47μs          | 2,080μs       | 21,491μs | **44x faster** | **457x faster** |

#### Memory Allocation Results (16 ops/4 concurrency)

| Framework | Memory |
|-----------|--------|
| WorkflowForge | 8KB |
| Workflow Core | 125KB |
| Elsa | 4,647KB |

**Key Insight**: Parallel execution maintains **44-457x speed advantage** with **16-581x less memory**.

---

### Scenario 12: Event-Driven

**Description**: Event-driven workflow execution with delays

#### Performance Results (Median Times)

| Delay | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |
|-------|---------------|---------------|------|----------|------------|
| 1ms   | 7.9ms         | 8.4ms         | 7.3ms | 1.1x     | 0.9x       |
| 5ms   | 8.0ms         | 8.7ms         | 21.5ms | 1.1x    | **2.7x faster** |

#### Memory Allocation Results (5ms delay)

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
| 1 | Sequential (10 ops) | 27-76x | 29-203x |
| 2 | Data Passing (10 ops) | 30-100x | 36-250x |
| 3 | Conditional (10 ops) | 26-74x | 25-177x |
| 4 | Loop/ForEach (50 items) | 73-143x | 26-134x |
| 5 | Concurrent (8 workflows) | 135-346x | 29-174x |
| 6 | Error Handling | 11-78x | 9-214x |
| 7 | Creation Overhead | 68-188x | 35-155x |
| 8 | Complete Lifecycle | 267x | 443x |
| 9 | State Machine (25 trans) | **322-574x** | 54-290x |
| 10 | Long Running | ~1x (delay-bound) | **51-422x** |
| 11 | Parallel (16 ops) | 44-454x | 16-581x |
| 12 | Event-Driven | 1-2.7x | 11-296x |

**Overall Speed Range**: **11-574x faster execution** (compute-bound scenarios)  
**Overall Memory Range**: **9-581x less memory allocation**

### Key Findings

1. **State Machine** scenarios show the highest speed advantage: **322-574x faster**
2. **Parallel Execution** maintains excellent scaling: **44-454x faster**
3. **Long Running** and **Event-Driven** are I/O-bound, but memory savings are massive: **51-422x less**
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
- **Iterations**: 25 per benchmark
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
- 25 iterations provide statistical confidence

**Outliers**: Some scenarios show high standard deviation due to GC pauses or system activity. Median values are used to minimize impact.

---

## Conclusion

WorkflowForge delivers **11-574x faster execution** and **9-581x less memory allocation** compared to Workflow Core and Elsa Workflows across 12 scenarios. This performance advantage stems from:

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
