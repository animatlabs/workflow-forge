# Benchmark Exclusions and Rationale

<p align="center">
  <img src="https://raw.githubusercontent.com/animatlabs/workflow-forge/main/icon.png" alt="WorkflowForge" width="120" height="120">
</p>

**Update Note**: Exclusion rationale remains valid; benchmark results will be refreshed after the upcoming rerun.

## Scenario 8: Complete Lifecycle - WorkflowCore Excluded

### Scenario Description
Measures the overhead of the complete workflow lifecycle:
1. Create workflow engine instance
2. Register workflow definition
3. Execute workflow
4. Cleanup and dispose resources

### Why WorkflowCore is Excluded

**Architectural Incompatibility**: WorkflowCore's design is fundamentally incompatible with this specific benchmark scenario.

#### Technical Details

WorkflowCore uses a **background worker thread model**:
- `IWorkflowHost.Start()` spins up background worker threads for workflow processing
- These threads continuously poll for work from an internal queue
- `IWorkflowHost.Stop()` must gracefully shut down all background threads

#### Impact on Benchmarking

For Scenario 8, the benchmark framework calls the lifecycle 25 times (iterations):

**WorkflowCore Measures**:
- DI container creation
- WorkflowHost instantiation
- **Background thread pool startup** ← ~100-200µs overhead
- Workflow registration
- Workflow execution
- **Background thread pool shutdown** ← ~50-100µs overhead
- Resource disposal

**WorkflowForge/Elsa Measure**:
- Lightweight object creation (~1-2µs)
- Registration (~1µs)
- Execution (actual workflow work)
- Simple disposal (~1µs)

#### Performance Impact

In testing, WorkflowCore Scenario 8 showed:
- 220µs+ per operation (vs 2-5µs for other frameworks)
- 10+ minutes for 25 iterations (vs seconds for other scenarios)
- Stuck at "OverheadJitting" phase due to thread startup costs

#### This is NOT a Flaw

WorkflowCore's architecture is optimized for:
- **Long-running workflows** that execute over hours/days
- **Persistent workflow state** across application restarts
- **High-throughput processing** with background workers

The background thread model is a **strength** for these use cases, but adds overhead to rapid lifecycle operations.

### Comparison to Other Scenarios

| Scenario | WorkflowCore Lifecycle | Result |
|----------|----------------------|--------|
| 1-6 | Start ONCE in setup, reuse for all iterations | Included (fair comparison) |
| 7 (Creation) | Never call Start() - only measure registration | Included (fair comparison) |
| 8 (Complete Lifecycle) | Start + Stop on EVERY iteration | **Excluded** (architectural incompatibility) |

### Conclusion

Excluding WorkflowCore from Scenario 8 provides:
1. **Honest benchmarking** - comparing what each framework does best
2. **Faster results** - no 10+ minute wait for one scenario
3. **Fair comparison** - not penalizing WorkflowCore for an architectural trade-off

If you need rapid workflow lifecycle operations, WorkflowForge or Elsa are better choices.
If you need long-running, persistent workflows, WorkflowCore's architecture is well-suited.

### Alternative Measurement

To measure WorkflowCore's lifecycle overhead in isolation, run Scenario 7 (Creation Overhead) which measures registration without Start/Stop, then estimate:
- Add ~200-300µs for Start() overhead
- Add ~50-100µs for Stop() overhead
- Total lifecycle overhead: ~250-400µs per instance

This is still acceptable for workflows that run for seconds/minutes/hours, but not suitable for rapid create/destroy cycles.

---

**WorkflowForge Benchmarks** - *Build workflows with industrial strength*

