# Benchmark exclusions

![WorkflowForge](https://raw.githubusercontent.com/animatlabs/workflow-forge/main/icon.png)

**Last updated:** March 2026

## Scenario 8: complete lifecycle (Workflow Core omitted)

### What it measures

End-to-end lifecycle cost:

1. Create workflow engine instance  
2. Register definition  
3. Execute workflow  
4. Dispose / clean up  

### Why Workflow Core is excluded

The host model does not match what Scenario 8 is trying to measure: tight loops of create / start / stop.

#### How Workflow Core behaves

Workflow Core uses **background worker threads**:

- `IWorkflowHost.Start()` spins workers that poll an internal queue  
- `IWorkflowHost.Stop()` must drain and shut those threads down cleanly  

#### Effect on the benchmark

Scenario 8 runs **50** lifecycle iterations.

**Workflow Core time includes:**

- DI container creation  
- `WorkflowHost` construction  
- **Thread pool start** (often ~100–200 μs)  
- Registration  
- Execution  
- **Thread pool shutdown** (often ~50–100 μs)  
- Disposal  

**WorkflowForge / Elsa (as exercised here) look more like:**

- Cheap object creation (~1–2 μs)  
- Registration (~1 μs)  
- Actual workflow work  
- Simple disposal (~1 μs)  

#### What we saw

Workflow Core Scenario 8 landed north of **220 μs** per iteration versus **2–5 μs** for the lighter stacks, pushed multi-minute runs for 50 iterations, and sometimes stalled in **OverheadJitting** because thread startup dominated.

#### Design choice, not a bug

Workflow Core targets:

- **Long-running** workflows (hours or days)  
- **Durable** state across restarts  
- **High throughput** with dedicated workers  

That threading model helps those jobs. It punishes micro-benchmarks that restart the host every iteration.

### How other scenarios treat Workflow Core

| Scenario | Host reuse | Verdict |
|----------|------------|---------|
| 1–6 | `Start()` once in global setup | Included |
| 7 (creation) | Never calls `Start()` | Included |
| 8 (full lifecycle) | `Start()` + `Stop()` every iteration | **Excluded** |

### Why exclude instead of “fixing” the numbers?

1. Stays honest about what each framework optimizes for  
2. Avoids **10+ minute** runs for a single scenario  
3. Avoids blaming Workflow Core for behavior that is intentional for long-lived hosts  

Need sub-millisecond lifecycle churn? WorkflowForge or Elsa fit that shape better. Need durable, multi-day processes? Workflow Core’s model is built for it.

### If you still want a rough host overhead number

Scenario 7 isolates creation without `Start()` / `Stop()`. Rule-of-thumb add-ons from our measurements:

- ~200–300 μs for `Start()`  
- ~50–100 μs for `Stop()`  
- **~250–400 μs** total host churn per instance  

Fine when workflows run for seconds or longer. Wrong tool for tight per-request spin-up loops.
