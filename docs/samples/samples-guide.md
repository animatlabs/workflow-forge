---
title: Basic console samples guide
description: How to run and read WorkflowForge.Samples.BasicConsole
---

# Basic console samples

Run the sample host from the repository root:

```bash
dotnet run --project src/samples/WorkflowForge.Samples.BasicConsole -f net8.0
```

Pick a sample by number at the prompt. Each sample is self-contained in `src/samples/WorkflowForge.Samples.BasicConsole/Samples/`.

For the full **37-sample catalog**, learning path, and pattern reference, see the canonical [Samples Guide](../getting-started/samples-guide.md).

## Conventions

- Dispose **`IWorkflowSmith`** with `using` when you create one; it owns concurrency limits and the foundry pool.
- Dispose **caller-owned** foundries (`CreateFoundry`, health check services) with `using` or `Dispose()`.
- Extension capabilities (OpenTelemetry, performance monitoring) register on **`foundry.Services`** and are torn down on foundry `Reset()` / `Dispose()`.
- Use **`foundry.Properties`** for workflow data only.

## Highlights

| Sample | Topic |
|--------|--------|
| Compensation behaviors | `FailFastCompensation`, `ThrowOnCompensationError` |
| Validation | Data annotations + `IValidatableObject` |
| Persistence / recovery | Checkpoints and `ForgeWithRecoveryAsync` |
| Health checks | Periodic checks — dispose `HealthCheckService` |
