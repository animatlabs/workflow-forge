# WorkflowForge v2.0.1 — Comprehensive Patch Plan

> **Version**: 2.0.1 (patch — no breaking API changes)  
> **Date**: February 15, 2026  
> **Scope**: Restore gate fix, bug fixes, code quality, package signing, cross-platform publish script

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Restore Mechanism Redesign (KISS)](#2-restore-mechanism-redesign-kiss)
3. [Bugs](#3-bugs)
4. [Code Quality Fixes](#4-code-quality-fixes)
5. [Test Updates](#5-test-updates)
6. [Sample Updates](#6-sample-updates)
7. [Documentation Updates](#7-documentation-updates)
8. [Package Signing](#8-package-signing)
9. [Cross-Platform Publish Script (Python)](#9-cross-platform-publish-script-python)
10. [Version Bump & Packaging Fixes](#10-version-bump--packaging-fixes)
11. [CI/CD Pipeline](#11-cicd-pipeline)
12. [Verification Checklist](#12-verification-checklist)
13. [File Change Index](#13-file-change-index)

---

## 1. Executive Summary

The original problem: `Workflow.SupportsRestore = operations.All(op => op.SupportsRestore)` creates an all-or-nothing gate. Adding a single `LoggingOperation`, `DelayOperation`, or inline `AddOperation(name, action)` silently disables compensation for the entire workflow — even when all state-modifying operations properly implement `RestoreAsync`. The per-operation skip logic in `CompensateForgedOperationsAsync` (which gracefully handles mixed scenarios) is dead code because the workflow-level gate prevents it from ever executing.

**The KISS fix**: Remove the `SupportsRestore` boolean from influencing behavior. Make `RestoreAsync` virtual with a no-op default in the base class. The smith always attempts compensation — if an operation didn't override `RestoreAsync`, the no-op runs harmlessly. If it did, real restore logic executes. No new interfaces, no new base classes, no new concepts. Pure virtual dispatch.

During audit, 20+ additional issues were discovered across the codebase ranging from bugs to thread safety to missing constants. All are addressed in this plan.

---

## 2. Restore Mechanism Redesign (KISS)

### 2.1 Deprecate `SupportsRestore` Property

**Rationale**: The boolean is redundant — if `RestoreAsync` is a no-op, calling it is harmless. If it's overridden, it does real work. The method's implementation IS the signal.

#### 2.1.1 `IWorkflowOperation` — `src/core/WorkflowForge/Abstractions/IWorkflowOperation.cs`

- **Line 30**: Add `[Obsolete("No longer used by the framework. Override RestoreAsync instead. Will be removed in v3.0.0.")]` above `bool SupportsRestore { get; }`

#### 2.1.2 `IWorkflowOperation<TInput, TOutput>` — same file

- No change needed (typed interface doesn't re-declare `SupportsRestore`)

#### 2.1.3 `IWorkflow` — `src/core/WorkflowForge/Abstractions/IWorkflow.cs`

- **Line 39**: Add `[Obsolete("No longer used by the framework. Will be removed in v3.0.0.")]` above `bool SupportsRestore { get; }`

### 2.2 Base Class: No-Op Default for `RestoreAsync`

#### 2.2.1 `WorkflowOperationBase` (untyped) — `src/core/WorkflowForge/Operations/WorkflowOperationBase.cs`

- **Line 22**: Change `public virtual bool SupportsRestore => false;` → `public virtual bool SupportsRestore => true;`  
  Add `#pragma warning disable CS0618` / `#pragma warning restore CS0618` around it
- **Lines 80-84**: Change `RestoreAsync` from:
  ```csharp
  if (!SupportsRestore)
      throw new NotSupportedException($"Operation '{Name}' does not support restoration.");
  return Task.CompletedTask;
  ```
  To just:
  ```csharp
  return Task.CompletedTask;
  ```
  This makes it a virtual no-op. Operations that need restore override it with real logic.

#### 2.2.2 `WorkflowOperationBase<TInput, TOutput>` (typed) — same file

- **Lines 161-163**: Change typed `RestoreAsync(TOutput, ...)` from:
  ```csharp
  if (!SupportsRestore)
      throw new NotSupportedException(...);
  return Task.CompletedTask;
  ```
  To just:
  ```csharp
  return Task.CompletedTask;
  ```
- **Lines 200-224**: In the sealed `RestoreAsync(object?, ...)` override — remove the `if (!SupportsRestore) throw` guard at lines 201-202. The body just does type conversion and delegates to the typed `RestoreAsync` (which is now a no-op by default).

### 2.3 Workflow-Level: Always Enable Compensation

#### 2.3.1 `Workflow.cs` — `src/core/WorkflowForge/Workflow.cs`

- **Line 42**: Change `SupportsRestore = operations.All(op => op.SupportsRestore);` → `SupportsRestore = true;`
  Add `#pragma warning disable CS0618` / `#pragma warning restore CS0618` around the property usage

#### 2.3.2 `WorkflowSmith.cs` — `src/core/WorkflowForge/WorkflowSmith.cs`

- **Line 260**: Remove the `if (workflow.SupportsRestore)` gate — always enter the compensation path on failure
  Add `#pragma warning disable CS0618` / `#pragma warning restore CS0618` where `workflow.SupportsRestore` was referenced
- **Lines 377-390**: Remove the `if (!operation.SupportsRestore)` skip check in `CompensateForgedOperationsAsync`. Instead, just call `RestoreAsync` on every completed operation. The base class no-op handles non-restorable operations. Add a `catch (NotSupportedException)` handler that treats it as a skip (for backward compat with direct `IWorkflowOperation` implementors who still throw)

### 2.4 Operation-Level: Remove `SupportsRestore` Guards from `RestoreAsync`

All operations that have `if (!SupportsRestore) throw new NotSupportedException(...)` in their `RestoreAsync` — remove the guard. The method should just execute its restore logic (or no-op if none).

#### 2.4.1 `DelegateWorkflowOperation` — `src/core/WorkflowForge/Operations/DelegateWorkflowOperation.cs`

- **Line 78-79** (untyped): Remove `if (!SupportsRestore) throw`. Change to: if `_restoreFunc != null`, invoke it; else return `Task.CompletedTask` (no-op)
- **Line 214-215** (typed): Same
- **Line 48** (untyped `SupportsRestore`): Add `#pragma warning disable CS0618` / `#pragma warning restore CS0618`
- **Line 186** (typed `SupportsRestore`): Same

#### 2.4.2 `ActionWorkflowOperation` — `src/core/WorkflowForge/Operations/ActionWorkflowOperation.cs`

- **Line 81** (untyped): Remove `if (!SupportsRestore) throw`. Change to: if `_restoreFunc != null`, invoke it; else return `Task.CompletedTask`
- **Line 160** (typed): Same
- **Lines 43, 134** (`SupportsRestore`): Add `#pragma warning disable CS0618`

#### 2.4.3 `ConditionalWorkflowOperation` — `src/core/WorkflowForge/Operations/ConditionalWorkflowOperation.cs`

- **Line 105** (`SupportsRestore`): Add pragma suppression. Logic no longer gates behavior, but keep it for backward-compat consumers who read the property
- **Line 127**: Remove `if (!SupportsRestore) throw` guard. The `RestoreAsync` should just delegate to whichever branch ran (using `_lastConditionResult`). If that branch's `RestoreAsync` is a no-op, fine
- **Line 18**: Make `_lastConditionResult` volatile (thread safety fix — see Bug #2)

#### 2.4.4 `ForEachWorkflowOperation` — `src/core/WorkflowForge/Operations/ForEachWorkflowOperation.cs`

- **Line 74** (`SupportsRestore`): Add pragma suppression
- **Line 134**: Remove `if (!SupportsRestore) throw` guard. Just call `RestoreAsync` on all child operations

#### 2.4.5 `RetryWorkflowOperation` — `src/extensions/WorkflowForge.Extensions.Resilience/RetryWorkflowOperation.cs`

- **Line 53** (`SupportsRestore`): Add pragma suppression
- **Line 73**: Remove `if (!SupportsRestore) throw`. Just delegate to `_operation.RestoreAsync(...)`

#### 2.4.6 `PollyRetryOperation` — `src/extensions/WorkflowForge.Extensions.Resilience.Polly/PollyRetryOperation.cs`

- **Line 53** (`SupportsRestore`): Add pragma suppression
- **Line 106**: Remove `if (!_innerOperation.SupportsRestore) throw`. Just delegate to `_innerOperation.RestoreAsync(...)`

#### 2.4.7 `LoggingOperation` — `src/core/WorkflowForge/Operations/LoggingOperation.cs`

- No changes needed. Inherits from `WorkflowOperationBase` which now has a no-op `RestoreAsync`
- **Line 89**: Update the comment from "Uses base RestoreAsync behavior which throws when SupportsRestore is false" to "Inherits base no-op RestoreAsync — logging has no state to restore"

#### 2.4.8 `DelayOperation` — `src/core/WorkflowForge/Operations/DelayOperation.cs`

- No changes needed. Inherits the no-op `RestoreAsync` from base
- **Line 67**: Update comment similarly

### 2.5 Builder: Add Optional Restore Parameter

#### 2.5.1 `WorkflowBuilder.cs` — `src/core/WorkflowForge/WorkflowBuilder.cs`

- **~Line 231** (async overload): Change signature from:
  ```csharp
  AddOperation(string name, Func<IWorkflowFoundry, CancellationToken, Task> action)
  ```
  To:
  ```csharp
  AddOperation(string name, Func<IWorkflowFoundry, CancellationToken, Task> action, Func<IWorkflowFoundry, CancellationToken, Task>? restoreAction = null)
  ```
  Pass `restoreAction` through to `ActionWorkflowOperation` constructor (which already accepts it)

- **~Line 260** (sync overload): Change signature from:
  ```csharp
  AddOperation(string name, Action<IWorkflowFoundry> action)
  ```
  To:
  ```csharp
  AddOperation(string name, Action<IWorkflowFoundry> action, Action<IWorkflowFoundry>? restoreAction = null)
  ```
  Adapt `restoreAction` to the `Func<object?, IWorkflowFoundry, CancellationToken, Task>` signature expected by `ActionWorkflowOperation`

#### 2.5.2 `DelegateWorkflowOperation` Static Factories — `src/core/WorkflowForge/Operations/DelegateWorkflowOperation.cs`

Add optional `restoreFunc` parameter to all static factory methods:

- **~Line 108** `FromSync` — add `Func<object?, IWorkflowFoundry, CancellationToken, Task>? restoreFunc = null`
- **~Line 117** `FromAsync` — same
- **~Line 126** `FromAction` — same
- **~Line 136** `FromAsyncAction` — same
- **~Lines 282-337** `WorkflowOperations` factory class — add matching optional params to `Create`, `CreateAsync`, `CreateAction`, `CreateAsyncAction`

---

## 3. Bugs

### Bug #1 — CRITICAL: WorkflowTimeoutMiddleware Doesn't Propagate Cancellation

**File**: `src/core/WorkflowForge/Middleware/WorkflowTimeoutMiddleware.cs` lines 97-98  
**Problem**: Creates a linked `CancellationTokenSource` for timeout but never propagates it to `next()`. The `Func<Task> next` signature has no token parameter. Timed-out workflows continue executing in background.  
**Fix**: The `IWorkflowMiddleware.ExecuteAsync` receives a `CancellationToken`. The middleware should store the linked CTS token in the foundry (e.g., `foundry.Properties["Workflow.TimeoutCancellationToken"]`) so the foundry's `ForgeAsync` can check it. Alternatively, redesign the workflow middleware signature to accept and return `CancellationToken`. The simplest fix for 2.0.1: cancel the linked CTS after `Task.WhenAny` detects the timeout branch won, and document that the inner task receives cancellation through the original token only if the caller's token is linked.

### Bug #2 — HIGH: ConditionalWorkflowOperation Thread Safety

**File**: `src/core/WorkflowForge/Operations/ConditionalWorkflowOperation.cs` line 18  
**Problem**: `_lastConditionResult` is a non-volatile `bool` shared between `ForgeAsync` (write) and `RestoreAsync` (read). These can be on different threads.  
**Fix**: Add `volatile` modifier: `private volatile bool _lastConditionResult;`

### Bug #3 — HIGH: Event Handler Memory Leaks on Dispose

**Files**:
- `src/core/WorkflowForge/WorkflowSmith.cs` line 503+ (`Dispose`)
- `src/core/WorkflowForge/WorkflowFoundry.cs` line 349+ (`Dispose`)

**Problem**: Neither clears event subscriptions on `Dispose()`. `WorkflowSmith` has 8 events, `WorkflowFoundry` has 3. Since the smith is registered as Singleton in DI, event handlers create GC roots for the app's lifetime.  
**Fix**: In `Dispose()`, set all event fields to `null`:
```csharp
// WorkflowSmith.Dispose()
WorkflowStarted = null;
WorkflowCompleted = null;
WorkflowFailed = null;
CompensationTriggered = null;
CompensationCompleted = null;
OperationRestoreStarted = null;
OperationRestoreCompleted = null;
OperationRestoreFailed = null;

// WorkflowFoundry.Dispose()
OperationStarted = null;
OperationCompleted = null;
OperationFailed = null;
```

### Bug #4 — HIGH: Recovery Extension Silently Swallows Exceptions

**Files**:
- `src/extensions/WorkflowForge.Extensions.Persistence.Recovery/RecoveryExtensions.cs` line 57
- `src/extensions/WorkflowForge.Extensions.Persistence.Recovery/RecoveryCoordinator.cs` line 100

**Problem**: `catch { /* ignore */ }` with no logging. Failed resume/recovery is invisible.  
**Fix**: Log the exception at Warning level before continuing. At minimum: `logger?.LogWarning(ex, "Recovery attempt failed");`

### Bug #5 — MEDIUM: WorkflowSmith Dispose Race Condition

**File**: `src/core/WorkflowForge/WorkflowSmith.cs` lines 503-511  
**Problem**: If `ForgeAsync` is mid-flight and calls `_concurrencyLimiter.Release()` after `Dispose()` has called `_concurrencyLimiter?.Dispose()`, it throws `ObjectDisposedException`.  
**Fix**: Wrap the `Release()` call in a try-catch for `ObjectDisposedException`, or set a disposed flag checked before `Release()`. The existing `_disposed` flag is already checked at the start of `ForgeAsync`, but not around the `finally` block's `Release()`.

### Bug #6 — MEDIUM: WorkflowSmith Doesn't Clone Options

**File**: `src/core/WorkflowForge/WorkflowSmith.cs` line 79  
**Problem**: `_options = options ?? new WorkflowForgeOptions();` stores the caller's reference. Mutations to the options object after smith construction affect the smith.  
**Fix**: `_options = options?.CloneTyped() ?? new WorkflowForgeOptions();`

### Bug #7 — MEDIUM: Event Handler Exceptions Conflated with Operation Failures

**File**: `src/core/WorkflowForge/WorkflowFoundry.cs` line 240  
**Problem**: `OperationStarted?.Invoke(...)` is inside the try block. If an event handler throws, it's caught by the outer `catch (Exception ex)` and treated as an operation failure.  
**Fix**: Wrap event invocations in individual try-catch blocks:
```csharp
try { OperationStarted?.Invoke(this, new OperationStartedEventArgs(...)); }
catch (Exception) { /* log but don't fail the operation */ }
```

### Bug #8 — MEDIUM: PersistenceMiddleware O(n²) Indexing

**File**: `src/extensions/WorkflowForge.Extensions.Persistence/PersistenceMiddleware.cs` line 85  
**Problem**: `operations.ToList().FindIndex(op => op.Id == operation.Id)` is called twice per operation execution, giving O(n²) total for n operations.  
**Fix**: Build a `Dictionary<Guid, int>` index once per workflow execution and reuse it.

### Bug #9 — LOW: Serilog Extension Missing Version Tag

**File**: `src/extensions/WorkflowForge.Extensions.Logging.Serilog/WorkflowForge.Extensions.Logging.Serilog.csproj`  
**Problem**: No `<Version>` or `<PackageVersion>` tag. Defaults to 1.0.0 when packed.  
**Fix**: Add `<Version>2.0.1</Version>` (aligned with all other packages).

### Bug #10 — LOW: Workflow.Operations Downcast Risk

**File**: `src/core/WorkflowForge/Workflow.cs` line 37  
**Problem**: `new List<IWorkflowOperation>(operations)` cast to `IReadOnlyList<T>` can be downcast back to `List<T>` by consumers.  
**Fix**: Use `new List<IWorkflowOperation>(operations).AsReadOnly()` or `Array.AsReadOnly()`.

### Bug #11 — LOW: WorkflowBuilder.Operations Allocation on Every Access

**File**: `src/core/WorkflowForge/WorkflowBuilder.cs` line 31  
**Problem**: Returns `new ReadOnlyCollection<IWorkflowOperation>(_operations)` every time.  
**Fix**: Cache the `ReadOnlyCollection` and invalidate on mutation (`AddOperation`, `Fluent` ops).

---

## 4. Code Quality Fixes

### 4.1 Extract Foundry Property Key Constants

**New file**: `src/core/WorkflowForge/Constants/FoundryPropertyKeys.cs`

Create a static class with all framework-owned property keys currently scattered as magic strings:

```csharp
public static class FoundryPropertyKeys
{
    // Operation execution state
    public const string OperationOutputFormat = "Operation.{0}.Output";
    public const string LastCompletedIndex = "Operation.LastCompletedIndex";
    public const string LastCompletedName = "Operation.LastCompletedName";
    public const string LastCompletedId = "Operation.LastCompletedId";
    public const string LastFailedIndex = "Operation.LastFailedIndex";
    public const string LastFailedName = "Operation.LastFailedName";
    public const string LastFailedId = "Operation.LastFailedId";
    
    // Timing
    public const string TimingDuration = "Timing.Duration";
    public const string TimingStartTime = "Timing.StartTime";
    public const string TimingEndTime = "Timing.EndTime";
    
    // Error handling
    public const string ErrorMessage = "Error.Message";
    public const string ErrorType = "Error.Type";
    public const string ErrorException = "Error.Exception";
    public const string ErrorTimestamp = "Error.Timestamp";
    
    // Timeout
    public const string WorkflowTimeout = "Workflow.Timeout";
    public const string WorkflowTimedOut = "Workflow.TimedOut";
    public const string OperationTimedOut = "Operation.TimedOut";
    public const string OperationTimeoutDuration = "Operation.TimeoutDuration";
    
    // Correlation
    public const string CorrelationId = "CorrelationId";
    public const string ParentWorkflowExecutionId = "ParentWorkflowExecutionId";
}
```

**Files to update** (replace magic strings with constants):
- `src/core/WorkflowForge/WorkflowFoundry.cs` — lines 250-265
- `src/core/WorkflowForge/WorkflowSmith.cs` — line 398
- `src/core/WorkflowForge/Middleware/TimingMiddleware.cs` — lines 67-90
- `src/core/WorkflowForge/Middleware/ErrorHandlingMiddleware.cs` — lines 95-103
- `src/core/WorkflowForge/Middleware/WorkflowTimeoutMiddleware.cs` — lines 116-117
- `src/core/WorkflowForge/Middleware/OperationTimeoutMiddleware.cs` — lines 115-116
- `src/core/WorkflowForge/Extensions/FoundryPropertyExtensions.cs` — lines 22, 42

### 4.2 Seal EventArgs and Exception Classes

Add `sealed` modifier to all EventArgs and exception classes that are not designed for inheritance. This enables JIT devirtualization.

**EventArgs classes** (all in `src/core/WorkflowForge/Events/`):
- `WorkflowStartedEventArgs`
- `WorkflowCompletedEventArgs`
- `WorkflowFailedEventArgs`
- `OperationStartedEventArgs`
- `OperationCompletedEventArgs`
- `OperationFailedEventArgs`
- `CompensationTriggeredEventArgs`
- `CompensationCompletedEventArgs`
- `OperationRestoreStartedEventArgs`
- `OperationRestoreCompletedEventArgs`
- `OperationRestoreFailedEventArgs`

**Exception classes** (in `src/core/WorkflowForge/Exceptions/`):
- All concrete exception types (check each — leave abstract ones unsealed)

### 4.3 Fix Filename Typo

**Rename**: `src/core/WorkflowForge/Extensions/WorkflowForgeLoggerExtesions.cs` → `WorkflowForgeLoggerExtensions.cs`

### 4.4 Normalize Options Classes

Make middleware options consistent by having all extend `WorkflowForgeOptionsBase`:

- `src/core/WorkflowForge/Middleware/TimingMiddlewareOptions.cs` — extend `WorkflowForgeOptionsBase`, add `Validate()` and `Clone()`
- `src/core/WorkflowForge/Middleware/LoggingMiddlewareOptions.cs` — same
- `src/core/WorkflowForge/Middleware/ErrorHandlingMiddlewareOptions.cs` — same

### 4.5 Add Missing Guard Clauses

- **`WorkflowOperationBase.ForgeAsync`** (`src/core/WorkflowForge/Operations/WorkflowOperationBase.cs` line 68): Add `foundry ?? throw new ArgumentNullException(nameof(foundry));`
- **`ForEachWorkflowOperation` constructor** (`src/core/WorkflowForge/Operations/ForEachWorkflowOperation.cs` line 46): Add null-element validation: `if (operations.Any(op => op == null)) throw new ArgumentException("Operations collection contains null elements.", nameof(operations));`

### 4.6 Fix `WithOperations` Silent Null Skip

**File**: `src/core/WorkflowForge/Extensions/FoundryPropertyExtensions.cs` line 109  
**Change**: Instead of `if (operation != null)`, throw: `?? throw new ArgumentException("Operations collection contains null elements.")`

### 4.7 Consistent `GC.SuppressFinalize`

**Decision**: Since none of these classes have finalizers, either add `GC.SuppressFinalize(this)` to ALL `Dispose()` methods (following CA1816 analysis rule) or remove from all. Recommendation: keep it everywhere per Microsoft's dispose pattern guidance (it's a safeguard if a derived class adds a finalizer). Add to:
- `WorkflowOperationBase.Dispose()` — currently missing
- `DelegateWorkflowOperation.Dispose()` — check if present
- `ActionWorkflowOperation.Dispose()` — check if present

### 4.8 `WorkflowSmith` Middleware Disposal

**File**: `src/core/WorkflowForge/WorkflowSmith.cs` line 503+  
In `Dispose()`, dispose middleware that implements `IDisposable`:
```csharp
foreach (var middleware in _workflowMiddlewares)
    (middleware as IDisposable)?.Dispose();
foreach (var middleware in _operationMiddlewares)
    (middleware as IDisposable)?.Dispose();
```

---

## 5. Test Updates

### 5.1 Files Requiring Updates

| # | Test File | Changes Needed |
|---|-----------|---------------|
| 1 | `tests/WorkflowForge.Tests/Orchestration/WorkflowTests.cs` | `SupportsRestore` assertions — all workflows now return `true`. Remove/invert tests that assert `false` for mixed operations |
| 2 | `tests/WorkflowForge.Tests/Orchestration/WorkflowSmithTests.cs` | Update compensation tests — compensation now always runs. Add test for mixed restorable/non-restorable workflow |
| 3 | `tests/WorkflowForge.Tests/Operations/WorkflowOperationTests.cs` | `SupportsRestore` default is now `true`. `RestoreAsync` no longer throws — returns `Task.CompletedTask` |
| 4 | `tests/WorkflowForge.Tests/Operations/LoggingOperationTests.cs` | Remove tests asserting `SupportsRestore == false` and `RestoreAsync throws`. Add test confirming no-op restore |
| 5 | `tests/WorkflowForge.Tests/Operations/DelayOperationTests.cs` | Same as LoggingOperation |
| 6 | `tests/WorkflowForge.Tests/Operations/ForEachWorkflowOperationTests.cs` | `SupportsRestore` logic changes. Restore no longer throws for mixed children |
| 7 | `tests/WorkflowForge.Tests/Operations/ActionWorkflowOperationEnhancedTests.cs` | `SupportsRestore` with no restore func — still `false` via `_restoreFunc != null`. RestoreAsync with no func — now returns Task.CompletedTask instead of throwing |
| 8 | `tests/WorkflowForge.Tests/WorkflowFoundryTests.cs` | Suppress `[Obsolete]` warnings |
| 9 | `tests/WorkflowForge.Extensions.Validation.Tests/ValidationMiddlewareTests.cs` | Suppress warnings for test helpers using `SupportsRestore` |
| 10 | `tests/WorkflowForge.Extensions.Audit.Tests/AuditMiddlewareTests.cs` | Same |

### 5.2 New Tests to Add

- **Builder restore parameter test**: Verify `AddOperation("name", action, restoreAction)` creates an operation where `RestoreAsync` invokes `restoreAction`
- **Mixed compensation integration test**: Workflow with `LoggingOperation` + restorable operation → trigger failure → verify compensation runs for restorable op, logging op runs no-op
- **NotSupportedException backward compat test**: Direct `IWorkflowOperation` implementor that throws `NotSupportedException` in `RestoreAsync` → verify smith treats it as skip, not failure
- **Event memory leak test**: Subscribe to smith events, dispose smith, verify handlers are cleared
- **Options clone test**: Verify `WorkflowSmith` clones options defensively

---

## 6. Sample Updates

### 6.1 Scope

32 sample files with ~100+ `SupportsRestore` references. All in `src/samples/WorkflowForge.Samples.BasicConsole/Samples/`.

### 6.2 For Operations Extending `WorkflowOperationBase` (7 classes)

- No changes needed — they inherit the new no-op `RestoreAsync` default
- Remove any `SupportsRestore => false` overrides (now unnecessary; suppress deprecation warning if kept)

### 6.3 For Operations Implementing `IWorkflowOperation` Directly (~75 classes)

For each operation that had:
```csharp
public bool SupportsRestore => false;
public Task RestoreAsync(...) => Task.CompletedTask;
// or
public Task RestoreAsync(...) { throw new NotSupportedException(); }
```

Change `RestoreAsync` implementations that throw to return `Task.CompletedTask` (since the smith now calls RestoreAsync on all operations). Add `#pragma warning disable CS0618` around `SupportsRestore` usages, OR suppress project-wide in a `Directory.Build.props` for the samples project.

**Note**: Since sample operations implement the interface directly and the interface still requires `SupportsRestore`, they must keep the property. Add `[Obsolete]` pragma suppression. In v3.0.0 when the property is removed from the interface, these all get cleaned up.

### 6.4 New/Updated Samples

- Update `CompensationBehaviorSample.cs` to demonstrate mixed-workflow compensation working
- Add inline builder restore example:
  ```csharp
  builder.AddOperation("Process", async (foundry, ct) => { /* work */ },
      restoreAction: async (foundry, ct) => { /* undo work */ });
  ```

---

## 7. Documentation Updates

### 7.1 Files Requiring Updates

| # | File | Key Sections to Update |
|---|------|----------------------|
| 1 | `docs/core/operations.md` | Lines 707-748: "Compensation and Rollback" — rewrite to explain no-op default, override for restore, deprecation of `SupportsRestore` |
| 2 | `docs/reference/api-reference.md` | Lines 116-272: Mark `SupportsRestore` deprecated, document new builder overloads |
| 3 | `docs/architecture/overview.md` | Lines 467-510: Update compensation architecture description |
| 4 | `docs/getting-started/getting-started.md` | Lines 377-405: Update operation creation guide — remove mandatory `SupportsRestore` + `RestoreAsync` boilerplate |
| 5 | `docs/core/configuration.md` | Lines 57-80, 238-266: Update compensation-related config docs |
| 6 | `docs/core/events.md` | Lines 116-136: Update compensation event docs |
| 7 | `docs/getting-started/samples-guide.md` | Lines 121-226: Update sample descriptions |
| 8 | `docs/extensions/index.md` | Lines 174-254: Update extension docs referencing restore |
| 9 | `docs/index.md` | Lines 50, 102, 127, 142, 145: Update landing page |
| 10 | `docs/llms.txt` | Lines 7, 33, 68: Update LLM-friendly docs |
| 11 | `README.md` | Line 42: Update Saga Pattern bullet |

### 7.2 Key Documentation Changes

- **Before**: "Operations must implement `SupportsRestore => true` and override `RestoreAsync` to support compensation"
- **After**: "Override `RestoreAsync` in your operation to support compensation. The base class provides a no-op default — operations that don't override it are safely skipped during compensation. The `SupportsRestore` property is deprecated and will be removed in v3.0.0."

### 7.3 Add CHANGELOG.md

**New file**: `CHANGELOG.md` at repository root

Document all changes in v2.0.1 using [Keep a Changelog](https://keepachangelog.com/) format with sections for Added, Changed, Deprecated, Fixed.

---

## 8. Package Signing

### 8.1 Certificate: SignPath Foundation (Free for OSS)

1. **Apply at [signpath.org/open-source](https://signpath.org/open-source)** with the GitHub repository link
2. SignPath provides a free code signing certificate for open-source projects
3. Most widely accepted free option in the .NET ecosystem
4. Integrates with CI/CD (GitHub Actions)

### 8.2 Strong-Name Assembly Signing

Add to a root `Directory.Build.props` (see Section 10):

```xml
<PropertyGroup>
  <SignAssembly>true</SignAssembly>
  <AssemblyOriginatorKeyFile>$(MSBuildThisFileDirectory)WorkflowForge.snk</AssemblyOriginatorKeyFile>
</PropertyGroup>
```

Generate the key: `sn -k WorkflowForge.snk`

**Important**: Once `InternalsVisibleTo` assemblies are used with strong naming, the `InternalsVisibleTo` attributes in `WorkflowForge.csproj` must include the public key token. Update all 7 entries.

### 8.3 NuGet Package Signing

After packing, sign packages with the `dotnet nuget sign` command:

```bash
dotnet nuget sign **/*.nupkg \
  --certificate-path path/to/certificate.pfx \
  --certificate-password $CERT_PASSWORD \
  --timestamper http://timestamp.digicert.com
```

This is integrated into the Python publish script (Section 9).

### 8.4 SourceLink + Deterministic Builds

Add to root `Directory.Build.props`:

```xml
<PropertyGroup>
  <Deterministic>true</Deterministic>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.*" PrivateAssets="All"/>
</ItemGroup>
```

---

## 9. Cross-Platform Publish Script (Python)

### 9.1 Replace `publish-packages.ps1` with `publish-packages.py`

The current PowerShell script is 217 lines. Convert to Python for cross-platform support (Windows/Linux/macOS).

### 9.2 Python Script Requirements

- **Arguments**: `--version` (all packages), `--api-key`, `--publish` (dry-run by default), `--sign` (enable NuGet signing), `--cert-path`, `--cert-password`
- **Package list**: Same 13 packages, defined as a list for easy extension
- **Steps**: Restore → Build → Pack → Verify (README/icon in .nupkg) → Sign (optional) → Push (optional)
- **Cross-platform**: Use `subprocess.run()` with `dotnet` CLI, `pathlib.Path` for paths
- **Error handling**: Fail fast on any step failure with clear error messages
- **Summary**: Print table of packages with status (packed/signed/published)
- **Dependencies**: Python 3.8+ standard library only (subprocess, pathlib, zipfile, argparse, sys)

### 9.3 Keep PowerShell Script

Rename to `publish-packages.legacy.ps1` for users who prefer PowerShell. Add a note at the top pointing to the Python version.

---

## 10. Version Bump & Packaging Fixes

### 10.1 Version: 2.0.0 → 2.0.1

Update ALL 13 `.csproj` files (listed in Section 13).

### 10.2 Fix Serilog Extension Missing Version

Add `<Version>2.0.1</Version>` to `src/extensions/WorkflowForge.Extensions.Logging.Serilog/WorkflowForge.Extensions.Logging.Serilog.csproj`

### 10.3 Add Root `Directory.Build.props`

**New file**: `src/Directory.Build.props`

Centralize common settings across all source projects:

```xml
<Project>
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <Deterministic>true</Deterministic>
    <SignAssembly>true</SignAssembly>
    <AssemblyOriginatorKeyFile>$(MSBuildThisFileDirectory)..\WorkflowForge.snk</AssemblyOriginatorKeyFile>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.*" PrivateAssets="All"/>
  </ItemGroup>
</Project>
```

### 10.4 Update `InternalsVisibleTo` for Strong Naming

After generating the `.snk` file, extract the public key and update all 7 `InternalsVisibleTo` entries in `src/core/WorkflowForge/WorkflowForge.csproj` with the public key token:

```xml
<InternalsVisibleTo Include="WorkflowForge.Tests, PublicKey=00240000..."/>
```

---

## 11. CI/CD Pipeline

### 11.1 Current State

Only `.github/workflows/pages.yml` exists (Jekyll docs deployment). **No build/test/publish pipeline.**

### 11.2 Add GitHub Actions Workflow

**New file**: `.github/workflows/build-test.yml`

- **Trigger**: Push to `main`, pull requests
- **Jobs**: Restore → Build → Test → Pack → (on tag) Sign + Publish
- **Matrix**: `net8.0` test runner
- **Secrets**: `NUGET_API_KEY`, `SIGNING_CERT_BASE64`, `SIGNING_CERT_PASSWORD`

---

## 12. Verification Checklist

- [ ] `dotnet build WorkflowForge.sln` — clean compile (only `[Obsolete]` warnings in external-facing code)
- [ ] `dotnet test WorkflowForge.sln` — all tests pass
- [ ] Integration test: workflow with `LoggingOperation` + restorable op → failure → compensation runs, logging op no-ops
- [ ] Integration test: direct `IWorkflowOperation` implementor that throws `NotSupportedException` in `RestoreAsync` → smith treats as skip
- [ ] Verify all 13 .nupkg files have correct version (2.0.1)
- [ ] Verify Serilog extension package is 2.0.1 (not 1.0.0)
- [ ] Verify `[Obsolete]` warnings appear for consumers using `SupportsRestore`
- [ ] Run benchmarks to verify no performance regression
- [ ] Review all doc pages for accuracy

---

## 13. File Change Index

### New Files

| # | File | Purpose |
|---|------|---------|
| 1 | `src/core/WorkflowForge/Constants/FoundryPropertyKeys.cs` | Foundry property key constants |
| 2 | `CHANGELOG.md` | Release changelog |
| 3 | `publish-packages.py` | Cross-platform Python publish script |
| 4 | `src/Directory.Build.props` | Centralized build properties |
| 5 | `WorkflowForge.snk` | Strong-name key file |
| 6 | `.github/workflows/build-test.yml` | CI/CD pipeline |

### Renamed Files

| # | From | To |
|---|------|----|
| 1 | `src/core/WorkflowForge/Extensions/WorkflowForgeLoggerExtesions.cs` | `WorkflowForgeLoggerExtensions.cs` |
| 2 | `publish-packages.ps1` | `publish-packages.legacy.ps1` |

### Modified Files — Core Library (src/core/WorkflowForge/)

| # | File | Changes |
|---|------|---------|
| 1 | `Abstractions/IWorkflowOperation.cs` | `[Obsolete]` on `SupportsRestore` |
| 2 | `Abstractions/IWorkflow.cs` | `[Obsolete]` on `SupportsRestore` |
| 3 | `Operations/WorkflowOperationBase.cs` | No-op `RestoreAsync`, `SupportsRestore => true`, remove throw guards |
| 4 | `Operations/DelegateWorkflowOperation.cs` | Remove throw guards, add optional restore to factories |
| 5 | `Operations/ActionWorkflowOperation.cs` | Remove throw guards |
| 6 | `Operations/ConditionalWorkflowOperation.cs` | Remove throw guard, volatile `_lastConditionResult` |
| 7 | `Operations/ForEachWorkflowOperation.cs` | Remove throw guard, null-element validation |
| 8 | `Operations/LoggingOperation.cs` | Update comment only |
| 9 | `Operations/DelayOperation.cs` | Update comment only |
| 10 | `Workflow.cs` | `SupportsRestore = true`, use `AsReadOnly()` |
| 11 | `WorkflowSmith.cs` | Remove `SupportsRestore` gates, catch `NotSupportedException`, clone options, clear events on dispose, dispose middlewares |
| 12 | `WorkflowFoundry.cs` | Clear events on dispose, wrap event invocations in try-catch, use property key constants |
| 13 | `WorkflowBuilder.cs` | Add optional restore params, cache `Operations` property |
| 14 | `Middleware/WorkflowTimeoutMiddleware.cs` | Fix cancellation propagation |
| 15 | `Middleware/TimingMiddleware.cs` | Use property key constants |
| 16 | `Middleware/ErrorHandlingMiddleware.cs` | Use property key constants |
| 17 | `Middleware/OperationTimeoutMiddleware.cs` | Use property key constants |
| 18 | `Extensions/FoundryPropertyExtensions.cs` | Use constants, throw on null operations |
| 19 | `Extensions/WorkflowForgeLoggerExtensions.cs` | Renamed (typo fix) |
| 20 | `Options/TimingMiddlewareOptions.cs` | Extend `WorkflowForgeOptionsBase` |
| 21 | `Options/LoggingMiddlewareOptions.cs` | Extend `WorkflowForgeOptionsBase` |
| 22 | `Options/ErrorHandlingMiddlewareOptions.cs` | Extend `WorkflowForgeOptionsBase` |
| 23 | `Events/*.cs` | Add `sealed` modifier (~11 files) |
| 24 | `Exceptions/*.cs` | Add `sealed` modifier where applicable |
| 25 | `WorkflowForge.csproj` | Version 2.0.1, `InternalsVisibleTo` with public key |

### Modified Files — Extensions

| # | File | Changes |
|---|------|---------|
| 26 | `Extensions.Resilience/RetryWorkflowOperation.cs` | Remove `SupportsRestore` throw guard |
| 27 | `Extensions.Resilience.Polly/PollyRetryOperation.cs` | Remove `SupportsRestore` throw guard |
| 28 | `Extensions.Persistence/PersistenceMiddleware.cs` | Fix O(n²) indexing |
| 29 | `Extensions.Persistence.Recovery/RecoveryExtensions.cs` | Log swallowed exceptions |
| 30 | `Extensions.Persistence.Recovery/RecoveryCoordinator.cs` | Log swallowed exceptions |
| 31 | `Extensions.Logging.Serilog/WorkflowForge.Extensions.Logging.Serilog.csproj` | Add missing `<Version>2.0.1</Version>` |
| 32-43 | All 12 remaining extension `.csproj` files | Version bump to 2.0.1 |

### Modified Files — Tests

| # | File | Changes |
|---|------|---------|
| 44 | `WorkflowForge.Tests/Orchestration/WorkflowTests.cs` | Update `SupportsRestore` assertions |
| 45 | `WorkflowForge.Tests/Orchestration/WorkflowSmithTests.cs` | Update compensation tests |
| 46 | `WorkflowForge.Tests/Operations/WorkflowOperationTests.cs` | `SupportsRestore` now `true`, `RestoreAsync` no-ops |
| 47 | `WorkflowForge.Tests/Operations/LoggingOperationTests.cs` | Remove throw assertions, add no-op assertions |
| 48 | `WorkflowForge.Tests/Operations/DelayOperationTests.cs` | Same |
| 49 | `WorkflowForge.Tests/Operations/ForEachWorkflowOperationTests.cs` | Update restore logic tests |
| 50 | `WorkflowForge.Tests/Operations/ActionWorkflowOperationEnhancedTests.cs` | Update restore tests |
| 51 | `WorkflowForge.Tests/WorkflowFoundryTests.cs` | Suppress warnings |
| 52 | `WorkflowForge.Extensions.Validation.Tests/ValidationMiddlewareTests.cs` | Suppress warnings |
| 53 | `WorkflowForge.Extensions.Audit.Tests/AuditMiddlewareTests.cs` | Suppress warnings |
| 54 | New test files | Builder restore, mixed compensation, NotSupportedException compat, event cleanup, options cloning |

### Modified Files — Samples

| # | File | Changes |
|---|------|---------|
| 55-86 | 32 sample files (see Section 6) | Update `SupportsRestore` usage, change throwing `RestoreAsync` to no-op, suppress `[Obsolete]` |

### Modified Files — Documentation

| # | File | Changes |
|---|------|---------|
| 87-97 | 11 docs files (see Section 7.1) | Update compensation/restore guidance, deprecation notices |

### Modified Files — Benchmarks

| # | File | Changes |
|---|------|---------|
| 98 | `src/benchmarks/WorkflowForge.Benchmarks.Comparative/Implementations/WorkflowForge/Scenario6_ErrorHandling_WorkflowForge.cs` | Suppress `[Obsolete]` warning |

---

**Total files changed: ~98**  
**Total new files: 6**  
**Total renamed files: 2**
