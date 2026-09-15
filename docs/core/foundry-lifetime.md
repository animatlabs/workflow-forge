---
title: Foundry lease and resource lifetime
description: Properties vs Services, pooled smith Reset, and IDisposable ownership in WorkflowForge.
---

# Foundry lease and resource lifetime

## Properties vs Services

| Bag | Purpose | Cleared when the lease ends | Auto-dispose |
|-----|---------|-----------------------------|--------------|
| `Properties` | Workflow / execution data (outputs, correlation, validation results) | Yes | No |
| `Services` | Extension capabilities (OpenTelemetry, performance monitoring, etc.) | Yes | Yes (`IDisposable`) |

Use `Properties` / `SetProperty` for business data. Extensions register long-lived objects on `Services`.

`Services.TryAdd` does not dispose a value it rejects — the caller keeps it. `Services.TryRemove` returns
the removed instance **already disposed**.

## What the foundry owns

The foundry disposes only what it owns, which is `Services`. Operations and middleware you pass to
`AddOperation`, `ReplaceOperations`, `AddMiddleware` or `AddMiddlewares` stay yours: ending the lease
releases the references but never calls `Dispose()` on them. Register anything the foundry should tear
down on `Services` instead.

## Pooled vs caller-owned foundry

- **`await smith.ForgeAsync(workflow)`** — foundry is pooled; its execution state is cleared after each run (`Properties` emptied, `Services` disposed).
- **`await smith.ForgeAsync(workflow, data)`** — the smith runs over your dictionary and leaves its contents in place, so results are readable after the call.
- **`await smith.ForgeAsync(workflow, foundry)`** — you own the foundry until you dispose it.

## Health checks

`CreateHealthCheckService()` registers the returned `HealthCheckService` on `foundry.Services`, so
its periodic timer stops when the foundry lease ends even if you never dispose it. Disposing it
yourself as well is safe — `HealthCheckService.Dispose()` is idempotent — and is still the clearer
choice when the service outlives the run that created it.

## Cancellation and compensation

Forward cancellation (`OperationCanceledException`) does **not** run saga compensation. After a real workflow failure, compensation runs in reverse order; if `RestoreAsync` throws (including `OperationCanceledException` on the same token), the error is logged and collected unless `FailFastCompensation` stops the walk early.

Default foundry options:

| Option | Default | Effect |
|--------|---------|--------|
| `FailFastCompensation` | `false` | When `true`, stop compensating after the first restore failure. |
| `ThrowOnCompensationError` | `false` | When `true` (or fail-fast), throw `AggregateException` including compensation errors after the original failure. |

When both are `false`, compensation errors are logged and the original workflow exception is rethrown.

## Upgrading custom contexts

If you implement **`IWorkflowExecutionContext`** or **`IWorkflowFoundry`** yourself, expose **`IFoundryServices Services`** on the execution context (typically `new FoundryServices()` per context). Extension-owned state belongs on **`Services`** (`Set` / `TryAdd`); workflow data stays in **`Properties`**. Custom foundries should call `Services.DisposeAll()` when the lease ends, and must not dispose caller-supplied operations or middleware. Release-specific notes live in the repository [CHANGELOG](https://github.com/animatlabs/workflow-forge/blob/main/CHANGELOG.md).
