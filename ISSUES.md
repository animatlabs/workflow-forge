# WorkflowForge v2.1.0 — Work Item Tracker

> **Generated**: February 15, 2026
> **Version**: 2.1.0
> **Total Items**: 42

Use this file to track progress on all v2.1.0 work items. Each entry is structured as a GitHub-issue-style card. You can convert these into actual GitHub issues or update the checkboxes here directly.

---

## Restore Mechanism Redesign (Phase 1)

### WF-001: Remove `SupportsRestore` from `IWorkflowOperation` and `IWorkflow` interfaces

- **Labels**: `breaking-change`, `enhancement`, `core`
- **Priority**: Critical
- **Phase**: 1.1
- **Status**: [ ] Not started

**Description**: Remove the `bool SupportsRestore { get; }` property from both `IWorkflowOperation` and `IWorkflow` interfaces. This is a breaking change — any direct implementor of these interfaces will need to remove their `SupportsRestore` property. Update the `RestoreAsync` XML doc to remove the `<exception cref="NotSupportedException">` tag.

**Acceptance Criteria**:
- [ ] `SupportsRestore` removed from `IWorkflowOperation`
- [ ] `SupportsRestore` removed from `IWorkflow`
- [ ] XML doc for `RestoreAsync` no longer references `NotSupportedException`
- [ ] Solution compiles (after downstream changes)

**Affected Files**:
- `src/core/WorkflowForge/Abstractions/IWorkflowOperation.cs`
- `src/core/WorkflowForge/Abstractions/IWorkflow.cs`

---

### WF-002: Make `RestoreAsync` a virtual no-op default in `WorkflowOperationBase`

- **Labels**: `enhancement`, `core`
- **Priority**: Critical
- **Phase**: 1.2
- **Status**: [ ] Not started

**Description**: In `WorkflowOperationBase`, remove the `SupportsRestore` property and change `RestoreAsync` from throwing `NotSupportedException` to returning `Task.CompletedTask` (no-op). This applies to both the untyped and typed variants, as well as the sealed untyped override in the generic base.

**Acceptance Criteria**:
- [ ] `SupportsRestore` property removed from `WorkflowOperationBase`
- [ ] `SupportsRestore` property removed from `WorkflowOperationBase<TInput, TOutput>`
- [ ] Untyped `RestoreAsync` returns `Task.CompletedTask` (no throw guard)
- [ ] Typed `RestoreAsync` returns `Task.CompletedTask` (no throw guard)
- [ ] Sealed untyped override no longer has throw guard

**Affected Files**:
- `src/core/WorkflowForge/Operations/WorkflowOperationBase.cs`

---

### WF-003: Remove workflow-level compensation gate in `WorkflowSmith`

- **Labels**: `enhancement`, `core`, `bug`
- **Priority**: Critical
- **Phase**: 1.3–1.4
- **Status**: [ ] Not started

**Description**: Remove the `if (workflow.SupportsRestore)` gate that prevents ALL compensation if any operation doesn't support restore. Remove `SupportsRestore` from `Workflow.cs`. The smith should always enter the compensation path on failure. In `CompensateForgedOperationsAsync`, remove the per-operation `SupportsRestore` skip and just call `RestoreAsync` on every completed operation. Add `catch (NotSupportedException)` for backward compat. Also fix Bug #10 (Operations downcast) using `.AsReadOnly()`.

**Acceptance Criteria**:
- [ ] `SupportsRestore` property removed from `Workflow.cs`
- [ ] `SupportsRestore = operations.All(...)` line removed
- [ ] `if (workflow.SupportsRestore)` gate removed from `WorkflowSmith`
- [ ] `if (!operation.SupportsRestore)` skip removed from `CompensateForgedOperationsAsync`
- [ ] `catch (NotSupportedException)` added for backward compat
- [ ] `Operations` uses `.AsReadOnly()` to prevent downcast mutation

**Affected Files**:
- `src/core/WorkflowForge/Workflow.cs`
- `src/core/WorkflowForge/WorkflowSmith.cs`

---

### WF-004: Remove `SupportsRestore` from all operation implementations

- **Labels**: `enhancement`, `core`
- **Priority**: High
- **Phase**: 1.5
- **Status**: [ ] Not started

**Description**: Remove the `SupportsRestore` property and all `if (!SupportsRestore) throw` guards from 8 operation classes. For delegate/action operations, change `RestoreAsync` to check if `_restoreFunc != null` and invoke it, else return `Task.CompletedTask`. Update comments in `LoggingOperation` and `DelayOperation`.

**Acceptance Criteria**:
- [ ] `DelegateWorkflowOperation` — `SupportsRestore` removed, throw guards removed
- [ ] `ActionWorkflowOperation` — same
- [ ] `ConditionalWorkflowOperation` — same
- [ ] `ForEachWorkflowOperation` — same
- [ ] `LoggingOperation` — comment updated
- [ ] `DelayOperation` — comment updated
- [ ] `RetryWorkflowOperation` — `SupportsRestore` removed, throw guard removed
- [ ] `PollyRetryOperation` — same

**Affected Files**:
- `src/core/WorkflowForge/Operations/DelegateWorkflowOperation.cs`
- `src/core/WorkflowForge/Operations/ActionWorkflowOperation.cs`
- `src/core/WorkflowForge/Operations/ConditionalWorkflowOperation.cs`
- `src/core/WorkflowForge/Operations/ForEachWorkflowOperation.cs`
- `src/core/WorkflowForge/Operations/LoggingOperation.cs`
- `src/core/WorkflowForge/Operations/DelayOperation.cs`
- `src/extensions/WorkflowForge.Extensions.Resilience/RetryWorkflowOperation.cs`
- `src/extensions/WorkflowForge.Extensions.Resilience.Polly/PollyRetryOperation.cs`

---

### WF-005: Add optional `restoreAction` parameter to `WorkflowBuilder.AddOperation` and factory methods

- **Labels**: `enhancement`, `core`
- **Priority**: Medium
- **Phase**: 1.6
- **Status**: [ ] Not started

**Description**: Add an optional `restoreAction` parameter to the async and sync `AddOperation` overloads in `WorkflowBuilder`. Add matching optional `restoreFunc` parameters to `DelegateWorkflowOperation` static factory methods and the `WorkflowOperations` factory class. Also fix Bug #11 by caching the `ReadOnlyCollection` for the `Operations` property.

**Acceptance Criteria**:
- [ ] Async `AddOperation` has `restoreAction` parameter
- [ ] Sync `AddOperation` has `restoreAction` parameter
- [ ] `DelegateWorkflowOperation.FromSync/FromAsync/FromAction/FromAsyncAction` have `restoreFunc` parameter
- [ ] `WorkflowOperations.Create/CreateAsync/CreateAction/CreateAsyncAction` have `restoreFunc` parameter
- [ ] `WorkflowBuilder.Operations` caches `ReadOnlyCollection`

**Affected Files**:
- `src/core/WorkflowForge/WorkflowBuilder.cs`
- `src/core/WorkflowForge/Operations/DelegateWorkflowOperation.cs`

---

## Bug Fixes (Phase 2)

### WF-006: Fix: WorkflowTimeoutMiddleware doesn't propagate cancellation

- **Labels**: `bug`, `core`
- **Priority**: Critical
- **Phase**: 2
- **Status**: [ ] Not started

**Description**: The timeout middleware creates a linked `CancellationTokenSource` but never propagates it to `next()`. Timed-out workflows continue executing in background. Fix by storing the linked CTS token in `foundry.Properties` and cancelling after timeout detection.

**Acceptance Criteria**:
- [ ] Linked CTS token stored in foundry properties
- [ ] CTS cancelled after `Task.WhenAny` detects timeout
- [ ] Operations can observe the timeout cancellation

**Affected Files**:
- `src/core/WorkflowForge/Middleware/WorkflowTimeoutMiddleware.cs`

---

### WF-007: Fix: ConditionalWorkflowOperation `_lastConditionResult` thread safety

- **Labels**: `bug`, `core`
- **Priority**: High
- **Phase**: 2
- **Status**: [ ] Not started

**Description**: `_lastConditionResult` is a non-volatile `bool` shared between `ForgeAsync` (write) and `RestoreAsync` (read) which can be on different threads. Add `volatile` modifier.

**Acceptance Criteria**:
- [ ] `_lastConditionResult` is declared as `volatile`

**Affected Files**:
- `src/core/WorkflowForge/Operations/ConditionalWorkflowOperation.cs`

---

### WF-008: Fix: Event handler memory leaks on Dispose in WorkflowSmith/WorkflowFoundry

- **Labels**: `bug`, `core`
- **Priority**: High
- **Phase**: 2
- **Status**: [ ] Not started

**Description**: Neither `WorkflowSmith` nor `WorkflowFoundry` clears event subscriptions on `Dispose()`. Since the smith is registered as Singleton in DI, event handlers create GC roots for the app's lifetime. Set all event fields to `null` in `Dispose()`.

**Acceptance Criteria**:
- [ ] `WorkflowSmith.Dispose()` sets all 8 event fields to `null`
- [ ] `WorkflowFoundry.Dispose()` sets all 3 event fields to `null`

**Affected Files**:
- `src/core/WorkflowForge/WorkflowSmith.cs`
- `src/core/WorkflowForge/WorkflowFoundry.cs`

---

### WF-009: Fix: Recovery extension silently swallows exceptions

- **Labels**: `bug`, `extensions`
- **Priority**: High
- **Phase**: 2
- **Status**: [ ] Not started

**Description**: `RecoveryExtensions.cs` and `RecoveryCoordinator.cs` have `catch { /* ignore */ }` with no logging. Failed resume/recovery is invisible. Log the exception at Warning level before continuing.

**Acceptance Criteria**:
- [ ] `RecoveryExtensions.cs` logs exception at Warning level
- [ ] `RecoveryCoordinator.cs` logs exception at Warning level

**Affected Files**:
- `src/extensions/WorkflowForge.Extensions.Persistence.Recovery/RecoveryExtensions.cs`
- `src/extensions/WorkflowForge.Extensions.Persistence.Recovery/RecoveryCoordinator.cs`

---

### WF-010: Fix: WorkflowSmith Dispose race condition with concurrency limiter

- **Labels**: `bug`, `core`
- **Priority**: Medium
- **Phase**: 2
- **Status**: [ ] Not started

**Description**: If `ForgeAsync` is mid-flight and calls `_concurrencyLimiter.Release()` after `Dispose()` has called `_concurrencyLimiter?.Dispose()`, it throws `ObjectDisposedException`. Wrap the `Release()` call in a `try-catch(ObjectDisposedException)`.

**Acceptance Criteria**:
- [ ] `_concurrencyLimiter.Release()` wrapped in try-catch for `ObjectDisposedException`
- [ ] No crash when Dispose and ForgeAsync overlap

**Affected Files**:
- `src/core/WorkflowForge/WorkflowSmith.cs`

---

### WF-011: Fix: WorkflowSmith doesn't clone options defensively

- **Labels**: `bug`, `core`
- **Priority**: Medium
- **Phase**: 2
- **Status**: [ ] Not started

**Description**: `_options = options ?? new WorkflowForgeOptions();` stores the caller's reference. Mutations to the options object after smith construction affect the smith. Fix by cloning: `_options = options?.CloneTyped() ?? new WorkflowForgeOptions();`

**Acceptance Criteria**:
- [ ] Options are cloned in constructor
- [ ] External mutations don't affect the smith

**Affected Files**:
- `src/core/WorkflowForge/WorkflowSmith.cs`

---

### WF-012: Fix: Event handler exceptions conflated with operation failures in WorkflowFoundry

- **Labels**: `bug`, `core`
- **Priority**: Medium
- **Phase**: 2
- **Status**: [ ] Not started

**Description**: `OperationStarted?.Invoke(...)` is inside the try block. If an event handler throws, it's caught by the outer catch and treated as an operation failure. Wrap event invocations in individual try-catch blocks that log but don't fail the operation.

**Acceptance Criteria**:
- [ ] Each event invocation wrapped in try-catch
- [ ] Handler exceptions logged but don't fail operations

**Affected Files**:
- `src/core/WorkflowForge/WorkflowFoundry.cs`

---

### WF-013: Fix: PersistenceMiddleware O(n^2) operation indexing

- **Labels**: `bug`, `performance`, `extensions`
- **Priority**: Medium
- **Phase**: 2
- **Status**: [ ] Not started

**Description**: `operations.ToList().FindIndex(op => op.Id == operation.Id)` is called twice per operation execution, giving O(n^2) total. Build a `Dictionary<Guid, int>` index once per workflow execution and reuse it.

**Acceptance Criteria**:
- [ ] Dictionary index built once per workflow
- [ ] `ToList().FindIndex()` calls replaced with dictionary lookups

**Affected Files**:
- `src/extensions/WorkflowForge.Extensions.Persistence/PersistenceMiddleware.cs`

---

### WF-014: Fix: Serilog extension missing Version tag in csproj

- **Labels**: `bug`, `packaging`
- **Priority**: Low
- **Phase**: 2
- **Status**: [ ] Not started

**Description**: The Serilog extension csproj has no `<Version>` or `<PackageVersion>` tag, defaulting to 1.0.0 when packed. Add `<Version>2.1.0</Version>`.

**Acceptance Criteria**:
- [ ] `<Version>2.1.0</Version>` added to csproj

**Affected Files**:
- `src/extensions/WorkflowForge.Extensions.Logging.Serilog/WorkflowForge.Extensions.Logging.Serilog.csproj`

---

### WF-015: Fix: Workflow.Operations can be downcast and mutated

- **Labels**: `bug`, `core`
- **Priority**: Low
- **Phase**: 1.3 (covered by WF-003)
- **Status**: [ ] Not started

**Description**: `new List<IWorkflowOperation>(operations)` cast to `IReadOnlyList<T>` can be downcast back to `List<T>` by consumers. Use `.AsReadOnly()`.

**Acceptance Criteria**:
- [ ] `Operations` backed by `ReadOnlyCollection`

**Affected Files**:
- `src/core/WorkflowForge/Workflow.cs`

---

### WF-016: Fix: WorkflowBuilder.Operations allocates ReadOnlyCollection on every access

- **Labels**: `bug`, `performance`, `core`
- **Priority**: Low
- **Phase**: 1.6 (covered by WF-005)
- **Status**: [ ] Not started

**Description**: `Operations` returns `new ReadOnlyCollection<IWorkflowOperation>(_operations)` every time. Cache the collection and invalidate on mutation.

**Acceptance Criteria**:
- [ ] `ReadOnlyCollection` cached
- [ ] Cache invalidated when operations are added

**Affected Files**:
- `src/core/WorkflowForge/WorkflowBuilder.cs`

---

## Code Quality (Phase 3)

### WF-017: Extract foundry property key constants into `FoundryPropertyKeys.cs`

- **Labels**: `code-quality`, `core`
- **Priority**: Medium
- **Phase**: 3.1
- **Status**: [ ] Not started

**Description**: Create a new `FoundryPropertyKeys` static class with all framework-owned property keys currently scattered as magic strings. Includes operation output/state, timing, error, timeout, correlation keys, plus `Workflow.Name`, `Validation.Status`, and `Validation.Errors` discovered in audit. Update all files using these magic strings to reference the constants.

**Acceptance Criteria**:
- [ ] `FoundryPropertyKeys.cs` created with all constants
- [ ] `WorkflowFoundry.cs` uses constants
- [ ] `WorkflowSmith.cs` uses constants
- [ ] `TimingMiddleware.cs` uses constants
- [ ] `ErrorHandlingMiddleware.cs` uses constants
- [ ] `WorkflowTimeoutMiddleware.cs` uses constants
- [ ] `OperationTimeoutMiddleware.cs` uses constants
- [ ] `FoundryPropertyExtensions.cs` uses constants
- [ ] `AuditMiddleware.cs` uses constants
- [ ] `AuditExtensions.cs` uses constants
- [ ] `ValidationMiddleware.cs` uses constants

**Affected Files**:
- `src/core/WorkflowForge/Constants/FoundryPropertyKeys.cs` (new)
- 10+ files updated to use constants

---

### WF-018: Seal all EventArgs and concrete Exception classes

- **Labels**: `code-quality`, `core`
- **Priority**: Low
- **Phase**: 3.2
- **Status**: [ ] Not started

**Description**: Add `sealed` modifier to all 11 EventArgs classes and all concrete (non-base) exception classes. Enables JIT devirtualization.

**Acceptance Criteria**:
- [ ] All 11 EventArgs classes sealed
- [ ] All concrete exception classes sealed (except `WorkflowForgeException` base)

**Affected Files**:
- `src/core/WorkflowForge/Events/*.cs` (11 files)
- `src/core/WorkflowForge/Exceptions/*.cs` (5 files)

---

### WF-019: Fix filename typo: `WorkflowForgeLoggerExtesions.cs`

- **Labels**: `code-quality`, `core`
- **Priority**: Low
- **Phase**: 3.3
- **Status**: [ ] Not started

**Description**: Rename `WorkflowForgeLoggerExtesions.cs` (missing 'n') to `WorkflowForgeLoggerExtensions.cs`.

**Acceptance Criteria**:
- [ ] File renamed correctly
- [ ] No references broken

**Affected Files**:
- `src/core/WorkflowForge/Extensions/WorkflowForgeLoggerExtesions.cs` → `WorkflowForgeLoggerExtensions.cs`

---

### WF-020: Normalize middleware options to extend `WorkflowForgeOptionsBase`

- **Labels**: `code-quality`, `core`
- **Priority**: Low
- **Phase**: 3.4
- **Status**: [ ] Not started

**Description**: Make `TimingMiddlewareOptions`, `LoggingMiddlewareOptions`, and `ErrorHandlingMiddlewareOptions` extend `WorkflowForgeOptionsBase` with proper `Validate()` and `Clone()` implementations.

**Acceptance Criteria**:
- [ ] `TimingMiddlewareOptions` extends `WorkflowForgeOptionsBase`
- [ ] `LoggingMiddlewareOptions` extends `WorkflowForgeOptionsBase`
- [ ] `ErrorHandlingMiddlewareOptions` extends `WorkflowForgeOptionsBase`
- [ ] All have `Validate()` and `Clone()` implementations

**Affected Files**:
- `src/core/WorkflowForge/Options/Middleware/TimingMiddlewareOptions.cs`
- `src/core/WorkflowForge/Options/Middleware/LoggingMiddlewareOptions.cs`
- `src/core/WorkflowForge/Options/Middleware/ErrorHandlingMiddlewareOptions.cs`

---

### WF-021: Add missing guard clauses and null-element validation

- **Labels**: `code-quality`, `core`
- **Priority**: Medium
- **Phase**: 3.5
- **Status**: [ ] Not started

**Description**: Add `foundry ?? throw new ArgumentNullException` to `WorkflowOperationBase.ForgeAsync`, null-element validation to `ForEachWorkflowOperation` constructor, and throw on null operations in `FoundryPropertyExtensions.WithOperations` instead of silent skip.

**Acceptance Criteria**:
- [ ] `WorkflowOperationBase.ForgeAsync` validates `foundry`
- [ ] `ForEachWorkflowOperation` constructor validates no null elements
- [ ] `WithOperations` throws on null operations

**Affected Files**:
- `src/core/WorkflowForge/Operations/WorkflowOperationBase.cs`
- `src/core/WorkflowForge/Operations/ForEachWorkflowOperation.cs`
- `src/core/WorkflowForge/Extensions/FoundryPropertyExtensions.cs`

---

### WF-022: Add consistent `GC.SuppressFinalize` to all Dispose methods

- **Labels**: `code-quality`, `core`, `extensions`
- **Priority**: Low
- **Phase**: 3.6
- **Status**: [ ] Not started

**Description**: Add `GC.SuppressFinalize(this)` to all `Dispose()` methods that are currently missing it, for consistency with CA1816 and Microsoft's dispose pattern guidance.

**Acceptance Criteria**:
- [ ] `WorkflowOperationBase.Dispose()` has `GC.SuppressFinalize`
- [ ] `RetryWorkflowOperation.Dispose()` has `GC.SuppressFinalize`
- [ ] `CircuitBreakerMiddleware.Dispose()` has `GC.SuppressFinalize`
- [ ] `HealthCheckService.Dispose()` has `GC.SuppressFinalize`
- [ ] `WorkflowForgeOpenTelemetryService.Dispose()` has `GC.SuppressFinalize`

**Affected Files**:
- `src/core/WorkflowForge/Operations/WorkflowOperationBase.cs`
- `src/extensions/WorkflowForge.Extensions.Resilience/RetryWorkflowOperation.cs`
- `src/extensions/WorkflowForge.Extensions.Resilience/CircuitBreakerMiddleware.cs`
- `src/extensions/WorkflowForge.Extensions.Observability.HealthChecks/HealthCheckService.cs`
- `src/extensions/WorkflowForge.Extensions.Observability.OpenTelemetry/WorkflowForgeOpenTelemetryService.cs`

---

### WF-023: Dispose middleware instances in WorkflowSmith.Dispose

- **Labels**: `code-quality`, `core`
- **Priority**: Medium
- **Phase**: 3.7
- **Status**: [ ] Not started

**Description**: In `WorkflowSmith.Dispose()`, iterate through workflow and operation middleware collections and dispose any that implement `IDisposable`.

**Acceptance Criteria**:
- [ ] Workflow middlewares disposed
- [ ] Operation middlewares disposed

**Affected Files**:
- `src/core/WorkflowForge/WorkflowSmith.cs`

---

### WF-024: Add missing `ConfigureAwait(false)` across all library await calls

- **Labels**: `code-quality`, `extensions`
- **Priority**: Medium
- **Phase**: 3.8
- **Status**: [ ] Not started

**Description**: Library code should use `ConfigureAwait(false)` on all `await` calls to avoid capturing the synchronization context. Add it to 15+ locations across extension projects.

**Acceptance Criteria**:
- [ ] `ValidationMiddleware.cs` — 4 locations
- [ ] `AuditMiddleware.cs` — 5 locations
- [ ] `AuditExtensions.cs` — 1 location
- [ ] `HealthCheckExtensions.cs` — 1 location
- [ ] `RandomIntervalStrategy.cs` — 1 location
- [ ] `ExponentialBackoffStrategy.cs` — 1 location
- [ ] `FoundryPropertyExtensions.cs` — 1 location
- [ ] `ValidationExtensions.cs` — 3 locations

**Affected Files**:
- `src/extensions/WorkflowForge.Extensions.Validation/ValidationMiddleware.cs`
- `src/extensions/WorkflowForge.Extensions.Audit/AuditMiddleware.cs`
- `src/extensions/WorkflowForge.Extensions.Audit/AuditExtensions.cs`
- `src/extensions/WorkflowForge.Extensions.Observability.HealthChecks/WorkflowFoundryHealthCheckExtensions.cs`
- `src/extensions/WorkflowForge.Extensions.Resilience/Strategies/RandomIntervalStrategy.cs`
- `src/extensions/WorkflowForge.Extensions.Resilience/Strategies/ExponentialBackoffStrategy.cs`
- `src/core/WorkflowForge/Extensions/FoundryPropertyExtensions.cs`
- `src/extensions/WorkflowForge.Extensions.Validation/ValidationExtensions.cs`

---

### WF-025: Fix: HealthCheckService.LastResults thread safety

- **Labels**: `bug`, `extensions`
- **Priority**: Medium
- **Phase**: 3.9
- **Status**: [ ] Not started

**Description**: `LastResults` is updated from a timer callback and read without synchronization. Use `Interlocked.Exchange` or `lock` to ensure thread-safe reads and writes.

**Acceptance Criteria**:
- [ ] `LastResults` updates are thread-safe
- [ ] `OverallStatus` reads are consistent with `LastResults`

**Affected Files**:
- `src/extensions/WorkflowForge.Extensions.Observability.HealthChecks/HealthCheckService.cs`

---

### WF-026: Fix: RetryWorkflowOperation.Dispose swallows exceptions without logging

- **Labels**: `code-quality`, `extensions`
- **Priority**: Low
- **Phase**: 3.10
- **Status**: [ ] Not started

**Description**: `RetryWorkflowOperation.Dispose()` catches all exceptions silently. Add logging for swallowed exceptions.

**Acceptance Criteria**:
- [ ] Swallowed exceptions are logged

**Affected Files**:
- `src/extensions/WorkflowForge.Extensions.Resilience/RetryWorkflowOperation.cs`

---

## Tests, Samples, Docs (Phases 4–6)

### WF-027: Update tests for SupportsRestore removal + add new test classes

- **Labels**: `tests`
- **Priority**: High
- **Phase**: 4
- **Status**: [ ] Not started

**Description**: Update 10 existing test files to remove all `SupportsRestore` assertions and references. Add 6 new test classes: builder restore parameter, mixed compensation integration, NotSupportedException backward compat, event memory leak, options clone, dispose race.

**Acceptance Criteria**:
- [ ] `WorkflowTests.cs` — `SupportsRestore` assertions removed
- [ ] `WorkflowSmithTests.cs` — compensation gate tests updated
- [ ] `WorkflowOperationTests.cs` — `SupportsRestore` removed, RestoreAsync returns CompletedTask
- [ ] `LoggingOperationTests.cs` — `SupportsRestore` tests removed
- [ ] `DelayOperationTests.cs` — same
- [ ] `ForEachWorkflowOperationTests.cs` — restore logic updated
- [ ] `ActionWorkflowOperationEnhancedTests.cs` — `SupportsRestore` tests removed
- [ ] `WorkflowFoundryTests.cs` — test helpers updated
- [ ] `ValidationMiddlewareTests.cs` — test helpers updated
- [ ] `AuditMiddlewareTests.cs` — test helpers updated
- [ ] New: Builder restore parameter test
- [ ] New: Mixed compensation integration test
- [ ] New: NotSupportedException backward compat test
- [ ] New: Event memory leak test
- [ ] New: Options clone test
- [ ] New: Dispose race test

**Affected Files**:
- 10 existing test files (see Phase 4 in plan)
- 6+ new test files

---

### WF-028: Update 24+ sample files removing SupportsRestore

- **Labels**: `samples`
- **Priority**: High
- **Phase**: 5
- **Status**: [ ] Not started

**Description**: Remove `SupportsRestore` property from all sample operation classes (57+ occurrences across 24+ files). Change any `RestoreAsync` that throws `NotSupportedException` to `return Task.CompletedTask`. Update `CompensationBehaviorSample.cs` to demonstrate mixed-workflow compensation. Add builder restore example.

**Acceptance Criteria**:
- [ ] All `SupportsRestore` properties removed from samples
- [ ] All throwing `RestoreAsync` changed to no-op
- [ ] `CompensationBehaviorSample.cs` updated with new behavior
- [ ] Builder restore example added
- [ ] All samples compile and run

**Affected Files**:
- 24+ files in `src/samples/WorkflowForge.Samples.BasicConsole/Samples/`

---

### WF-029: Update documentation line-by-line + create CHANGELOG.md

- **Labels**: `docs`
- **Priority**: High
- **Phase**: 6
- **Status**: [ ] Not started

**Description**: Line-by-line update of 11 doc files (27+ occurrences of `SupportsRestore`). Remove all references, update interface definitions, rewrite compensation guidance, update code examples. Create `CHANGELOG.md` at repository root. Update `README.md`.

**Acceptance Criteria**:
- [ ] `docs/core/operations.md` — 8 occurrences updated
- [ ] `docs/reference/api-reference.md` — 7+ occurrences updated
- [ ] `docs/getting-started/getting-started.md` — 5 occurrences updated
- [ ] `docs/architecture/overview.md` — 3 occurrences updated
- [ ] `docs/index.md` — 2 occurrences updated
- [ ] `docs/getting-started/samples-guide.md` — 1 occurrence updated
- [ ] `docs/core/configuration.md` — 1 occurrence updated
- [ ] `docs/core/events.md` — compensation event descriptions updated
- [ ] `docs/extensions/index.md` — restore references updated
- [ ] `docs/llms.txt` — 3 occurrences updated
- [ ] `README.md` — Saga Pattern bullet updated
- [ ] `CHANGELOG.md` created with Added/Changed/Removed/Fixed sections
- [ ] Zero remaining `SupportsRestore` references in docs

**Affected Files**:
- 11 doc files + `README.md` + `CHANGELOG.md` (new)

---

## Versioning and Infrastructure (Phases 7–8)

### WF-030: Version bump all 13 csproj to 2.1.0 + Directory.Build.props + SNK signing

- **Labels**: `infra`, `packaging`
- **Priority**: High
- **Phase**: 7
- **Status**: [ ] Not started

**Description**: Update `<Version>` in all 13 `.csproj` files from 2.0.0 to 2.1.0. Create `src/Directory.Build.props` with centralized build settings (LangVersion, Nullable, Deterministic, SignAssembly, SourceLink, symbol packages). Generate `WorkflowForge.snk` and update `InternalsVisibleTo` entries with public key token.

**Acceptance Criteria**:
- [ ] All 13 `.csproj` files at version 2.1.0
- [ ] `src/Directory.Build.props` created
- [ ] `WorkflowForge.snk` generated
- [ ] `InternalsVisibleTo` entries updated with public key

**Affected Files**:
- 13 `.csproj` files
- `src/Directory.Build.props` (new)
- `WorkflowForge.snk` (new)
- `src/core/WorkflowForge/WorkflowForge.csproj` (InternalsVisibleTo)

---

### WF-031: Add CI/CD pipeline with manual publish dispatch

- **Labels**: `infra`, `ci-cd`
- **Priority**: Medium
- **Phase**: 8
- **Status**: [ ] Not started

**Description**: Create `.github/workflows/build-test.yml` with build/test on push to `main` and PRs. Add a separate `workflow_dispatch` trigger for publishing (manual only, NOT auto-publish). Matrix: `net8.0` test runner.

**Acceptance Criteria**:
- [ ] Build/test runs on push to `main` and PRs
- [ ] Publish is manual-only via `workflow_dispatch`
- [ ] Tests run on `net8.0`
- [ ] Secrets: `NUGET_API_KEY`, `SIGNING_CERT_BASE64`, `SIGNING_CERT_PASSWORD`

**Affected Files**:
- `.github/workflows/build-test.yml` (new)

---

### WF-032: Create cross-platform Python publish script + rename legacy PS1

- **Labels**: `infra`, `tooling`
- **Priority**: Low
- **Phase**: 8
- **Status**: [ ] Not started

**Description**: Create `publish-packages.py` (Python 3.8+, standard library only) with `--version`, `--api-key`, `--publish`, `--sign` flags. Dry-run by default. Rename `publish-packages.ps1` to `publish-packages.legacy.ps1` with note at top pointing to Python version. Also remove `SupportsRestore` from benchmark file.

**Acceptance Criteria**:
- [ ] `publish-packages.py` created and functional
- [ ] `publish-packages.ps1` renamed to `publish-packages.legacy.ps1`
- [ ] Legacy script has note pointing to Python version
- [ ] Benchmark file updated

**Affected Files**:
- `publish-packages.py` (new)
- `publish-packages.ps1` → `publish-packages.legacy.ps1`
- `src/benchmarks/WorkflowForge.Benchmarks.Comparative/Implementations/WorkflowForge/Scenario6_ErrorHandling_WorkflowForge.cs`

---

## Follow-Up Fixes (Phase 2)

### WF-033: Document restoreAction/restoreFunc API

- **Labels**: `documentation`, `enhancement`
- **Priority**: High
- **Status**: [x] Completed

**Description**: The new `restoreAction` / `restoreFunc` parameters on `WorkflowBuilder.AddOperation`, `DelegateWorkflowOperation`, and `WithOperation` extension methods were undocumented. Added documentation in `operations.md`, `api-reference.md`, `getting-started.md`, and added `restoreAction` overloads to `WithOperation` foundry extension methods.

**Affected Files**:
- `docs/core/operations.md`
- `docs/reference/api-reference.md`
- `docs/getting-started/getting-started.md`
- `src/core/WorkflowForge/Extensions/FoundryPropertyExtensions.cs`

---

### WF-034: Update PackageReleaseNotes in all csproj files

- **Labels**: `packaging`, `maintenance`
- **Priority**: Medium
- **Status**: [x] Completed

**Description**: All 12 `.csproj` files with `<PackageReleaseNotes>` still referenced v2.0.0. DI extension had no release notes. Updated all 13 projects to v2.1.0 release notes.

**Affected Files**:
- All 13 `.csproj` files under `src/`

---

### WF-035: Fix PersistenceMiddleware duplicate operation handling

- **Labels**: `bug`, `persistence`
- **Priority**: High
- **Status**: [x] Completed

**Description**: The operation index dictionary used `dict[key] = value` which silently overwrites on duplicate Guid keys. Fixed to use first-occurrence-wins with a debug warning. Also cached the dictionary on `foundry.Properties` (was rebuilt per middleware call) and computed `ResolveKeys` once per `ExecuteAsync` (was called 3 times).

**Affected Files**:
- `src/extensions/WorkflowForge.Extensions.Persistence/PersistenceMiddleware.cs`

---

### WF-036: Complete FoundryPropertyKeys constants

- **Labels**: `code-quality`, `enhancement`
- **Priority**: Medium
- **Status**: [x] Completed

**Description**: Added missing constants (`ErrorStackTrace`, `TimingDurationTicks`, `TimingFailed`, `OperationTimeoutFormat`) to `FoundryPropertyKeys`. Replaced all magic strings in core code with constant references. Replaced extension magic strings (`Workflow.Name`, `Validation.Status`, `Validation.Errors`) with constants. Created `PerformancePropertyKeys` for the Performance extension.

**Affected Files**:
- `src/core/WorkflowForge/Constants/FoundryPropertyKeys.cs`
- `src/core/WorkflowForge/Middleware/ErrorHandlingMiddleware.cs`
- `src/core/WorkflowForge/Middleware/TimingMiddleware.cs`
- `src/core/WorkflowForge/Middleware/OperationTimeoutMiddleware.cs`
- `src/core/WorkflowForge/WorkflowSmith.cs`
- `src/core/WorkflowForge/Extensions/WorkflowForgeLoggerExtensions.cs`
- `src/extensions/WorkflowForge.Extensions.Audit/AuditMiddleware.cs`
- `src/extensions/WorkflowForge.Extensions.Audit/AuditExtensions.cs`
- `src/extensions/WorkflowForge.Extensions.Validation/ValidationMiddleware.cs`
- `src/extensions/WorkflowForge.Extensions.Observability.Performance/Constants/PerformancePropertyKeys.cs` (new)
- `src/extensions/WorkflowForge.Extensions.Observability.Performance/WorkflowFoundryPerformanceExtensions.cs`

---

### WF-037: Refactor all samples and benchmarks to use WorkflowOperationBase

- **Labels**: `refactoring`, `samples`, `benchmarks`
- **Priority**: High
- **Status**: [x] Completed

**Description**: Refactored 103 operation classes (97 in samples, 6 in benchmarks) from direct `IWorkflowOperation` implementation to extending `WorkflowOperationBase`. Eliminates ~4-5 lines of boilerplate per class (Id, Dispose, RestoreAsync) and gains lifecycle hooks. Operations with real `RestoreAsync` logic kept as overrides.

**Affected Files**:
- All 35 sample files under `src/samples/WorkflowForge.Samples.BasicConsole/Samples/`
- `src/benchmarks/WorkflowForge.Benchmarks/OperationPerformanceBenchmark.cs`
- `src/benchmarks/WorkflowForge.Benchmarks/WorkflowThroughputBenchmark.cs`
- `src/benchmarks/WorkflowForge.Benchmarks.Comparative/Implementations/WorkflowForge/Scenario6_ErrorHandling_WorkflowForge.cs`

---

### WF-038: Fix publish script .snupkg handling and move to scripts/

- **Labels**: `infrastructure`, `packaging`
- **Priority**: Medium
- **Status**: [x] Completed

**Description**: The Python publish script only handled `.nupkg` (not `.snupkg`). Fixed signing and publishing to include both package types. Moved scripts to `scripts/` folder. Created `scripts/README.md` with SNK guide, signing instructions, and GitHub secrets setup.

**Affected Files**:
- `scripts/publish-packages.py` (moved + fixed)
- `scripts/publish-packages.legacy.ps1` (moved)
- `scripts/README.md` (new)
- `publish-packages.py` (deleted)
- `publish-packages.legacy.ps1` (deleted)

---

### WF-039: Fix CI/CD workflow artifact reuse and .snupkg support

- **Labels**: `infrastructure`, `ci-cd`
- **Priority**: Medium
- **Status**: [x] Completed

**Description**: The CI/CD publish job rebuilt from source instead of using the tested build artifacts. Fixed to download artifacts from the build job. Added `.snupkg` handling to upload, signing, and push steps. Added cert cleanup after signing.

**Affected Files**:
- `.github/workflows/build-test.yml`

---

### WF-040: Fix Scenario5 benchmark closure bug

- **Labels**: `bug`, `benchmarks`
- **Priority**: High
- **Status**: [x] Completed

**Description**: Loop variables `i` and `j` were captured by closures in `Task.Run` and `WithOperation` lambdas. Fixed by capturing into local variables before the closures.

**Affected Files**:
- `src/benchmarks/WorkflowForge.Benchmarks.Comparative/Implementations/WorkflowForge/Scenario5_ConcurrentExecution_WorkflowForge.cs`

---

### WF-041: Fix ForEachLoopWorkflow misleading return value

- **Labels**: `bug`, `benchmarks`
- **Priority**: Low
- **Status**: [x] Completed

**Description**: The benchmark set items in Properties but `ForEachWorkflowOperation` with `SharedInput` never used them. Fixed misleading return value.

**Affected Files**:
- `src/benchmarks/WorkflowForge.Benchmarks/WorkflowThroughputBenchmark.cs`

---

### WF-042: Emphasize WorkflowOperationBase in all documentation

- **Labels**: `documentation`
- **Priority**: Medium
- **Status**: [x] Completed

**Description**: Updated `IWorkflowOperation` XML docs with `<remarks>` recommending `WorkflowOperationBase`. Updated all documentation files to position `IWorkflowOperation` as an advanced/escape-hatch API. Updated all code examples in docs to use `WorkflowOperationBase` / `ForgeAsyncCore` pattern.

**Affected Files**:
- `src/core/WorkflowForge/Abstractions/IWorkflowOperation.cs`
- `docs/core/operations.md`
- `docs/reference/api-reference.md`
- `docs/getting-started/getting-started.md`
- `docs/getting-started/samples-guide.md`

---

### WF-043: Make FoundryPropertyKeys and PerformancePropertyKeys internal

- **Labels**: `architecture`, `breaking-internal`
- **Priority**: High
- **Status**: [x] Completed

**Description**: `FoundryPropertyKeys` and `PerformancePropertyKeys` were `public static class` but contain implementation details that external consumers should not couple to. Changed both to `internal static class`. Added `InternalsVisibleTo` entries for Persistence, Audit, Validation, Performance, and Benchmark projects. Added `GetOperationOutput` / `GetOperationOutput<T>` public extension methods as the intended orchestrator-level API for reading operation outputs. Fixed Scenario2 benchmark to use the new public API instead of internal constants.

**Affected Files**:
- `src/core/WorkflowForge/Constants/FoundryPropertyKeys.cs`
- `src/core/WorkflowForge/WorkflowForge.csproj`
- `src/core/WorkflowForge/Extensions/FoundryPropertyExtensions.cs`
- `src/extensions/WorkflowForge.Extensions.Observability.Performance/Constants/PerformancePropertyKeys.cs`
- `src/benchmarks/WorkflowForge.Benchmarks.Comparative/Implementations/WorkflowForge/Scenario2_DataPassing_WorkflowForge.cs`

---

### WF-044: Change operation property keys to index:name composite format

- **Labels**: `architecture`, `internal`
- **Priority**: High
- **Status**: [x] Completed

**Description**: The operation property key format `"Operation.{guid}.Output"` was unreadable in logs/debug and caused silent collisions when the same operation instance was added twice to a workflow (both writes to the same GUID-keyed property). Changed to `"Operation.{index}:{name}.Output"` where index is the zero-based position and name is the operation name. This produces readable keys like `"Operation.0:ValidateOrder.Output"` and eliminates collisions because the index is always unique. Added `CurrentOperationIndex` internal property key set by the foundry before each middleware invocation so middleware can access the operation's position. Updated WorkflowFoundry (output storage), WorkflowSmith (compensation output lookup), OperationTimeoutMiddleware (per-operation timeout lookup), and related tests.

**Affected Files**:
- `src/core/WorkflowForge/Constants/FoundryPropertyKeys.cs`
- `src/core/WorkflowForge/WorkflowFoundry.cs`
- `src/core/WorkflowForge/WorkflowSmith.cs`
- `src/core/WorkflowForge/Middleware/OperationTimeoutMiddleware.cs`
- `src/core/WorkflowForge/Extensions/FoundryPropertyExtensions.cs`
- `tests/WorkflowForge.Tests/WorkflowFoundryTests.cs`
- `tests/WorkflowForge.Tests/Concurrency/ConcurrencyTests.cs`

---

### WF-045: Rewrite PersistenceMiddleware with counter-based tracking

- **Labels**: `architecture`, `bug-fix`
- **Priority**: High
- **Status**: [x] Completed

**Description**: The `PersistenceMiddleware` used a `Dictionary<Guid, int>` to map operation IDs to their indices. This broke when the same operation instance was added twice (same GUID, dictionary collision). The "first-win" approach masked the bug. Additionally, the code used `#if NETSTANDARD2_0` preprocessor directives, but the project targets `netstandard2.0` exclusively, making the `#else` branch dead code. Rewrote to read the current operation index from `FoundryPropertyKeys.CurrentOperationIndex` (set by the foundry), with a fallback internal counter for backward compatibility. Removed the dictionary, removed `GetOrBuildOperationIndex`, removed `OperationIndexCacheKey`, and removed all `#if` directives. Plain netstandard2.0 code only. Safe for nested workflows because each foundry has its own property dictionary.

**Affected Files**:
- `src/extensions/WorkflowForge.Extensions.Persistence/PersistenceMiddleware.cs`
- `tests/WorkflowForge.Extensions.Persistence.Tests/PersistenceMiddlewareTests.cs`

---

### WF-046: Multi-target tests/benchmarks for net48;net8.0;net10.0

- **Labels**: `infrastructure`, `testing`
- **Priority**: Medium
- **Status**: [x] Completed

**Description**: Multi-targeted all test, benchmark, and sample projects to validate cross-framework compatibility and enable performance comparisons across .NET versions. Tests target `net48;net8.0;net10.0`. Samples target `net8.0;net10.0`. Internal benchmarks target `net48;net8.0;net10.0`. Comparative benchmarks target `net48;net8.0;net10.0` with Elsa conditionally included on net8.0+ only (net48 compares WorkflowForge vs WorkflowCore). Fixed net48 API compatibility issues: replaced `Task.IsCompletedSuccessfully` with `task.Status == TaskStatus.RanToCompletion`, replaced `Random.Shared` with `new Random()`, replaced `Array.Fill` with helper methods, replaced `Parallel.ForEachAsync` with conditional compilation, replaced `string.Split(string, StringSplitOptions)` with array overload, replaced `[^1]` range syntax with `[seq.Count - 1]`. Added `Microsoft.NETFramework.ReferenceAssemblies` package for CI builds.

**Known**: Validation extension (9 tests) and HealthChecks extension (1 test) have pre-existing net48 compatibility issues unrelated to this work.

**Affected Files**:
- All 7 test project `.csproj` files
- `src/samples/WorkflowForge.Samples.BasicConsole/WorkflowForge.Samples.BasicConsole.csproj`
- `src/benchmarks/WorkflowForge.Benchmarks/WorkflowForge.Benchmarks.csproj`
- `src/benchmarks/WorkflowForge.Benchmarks.Comparative/WorkflowForge.Benchmarks.Comparative.csproj`
- All 12 Elsa scenario files (added `#if !NET48` guards)
- All 12 comparative benchmark files (added `#if !NET48` guards for Elsa references)
- `tests/WorkflowForge.Tests/Operations/ActionWorkflowOperationEnhancedTests.cs`
- `tests/WorkflowForge.Tests/Operations/WorkflowOperationTests.cs`
- `tests/WorkflowForge.Tests/Operations/DelayOperationTests.cs`
- `tests/WorkflowForge.Tests/Operations/LoggingOperationTests.cs`
- `tests/WorkflowForge.Tests/Concurrency/ConcurrencyTests.cs`
- `tests/WorkflowForge.Tests/Integration/WorkflowIntegrationTests.cs`
- `src/benchmarks/WorkflowForge.Benchmarks/ConcurrencyBenchmark.cs`
- `src/benchmarks/WorkflowForge.Benchmarks/MemoryAllocationBenchmark.cs`
- `src/benchmarks/WorkflowForge.Benchmarks/WorkflowThroughputBenchmark.cs`

---

### WF-047: Add GetOperationOutput public orchestrator API

- **Labels**: `enhancement`, `api`
- **Priority**: Medium
- **Status**: [x] Completed

**Description**: Added `GetOperationOutput(this IWorkflowFoundry foundry, int operationIndex, string operationName)` and generic `GetOperationOutput<T>` extension methods to `FoundryPropertyExtensions`. These are orchestrator-level APIs for workflow composition and test inspection. Operations should NOT use them to read other operations' outputs (use `ForgeAsyncCore` input parameter or `foundry.Properties` with domain-specific keys instead). This preserves the disjoint operation principle.

**Affected Files**:
- `src/core/WorkflowForge/Extensions/FoundryPropertyExtensions.cs`

---

### WF-048: Fix `new Random()` TickCount-collision across samples and tests

- **Labels**: `bug`, `net48-compat`
- **Priority**: High
- **Status**: [x] Completed

**Description**: On .NET Framework 4.8, `new Random()` seeds from `Environment.TickCount` (millisecond resolution). Two `new Random()` calls within the same millisecond produce identical sequences. This affected 8 call sites across 6 sample files and 2 test files. On .NET 6+, `new Random()` uses a globally-incrementing atomic seed, so the issue only manifests on net48.

**Fix**: Created a `ThreadSafeRandom` internal static helper class that delegates to `Random.Shared` on .NET 6+ (zero overhead) and uses a `[ThreadStatic]` local `Random` seeded from a single locked global instance on .NET Framework 4.8. No collisions, no contention, no per-call allocations. Placed one copy in the samples project and one copy in the test project (not in core, which has zero `Random` usage).

**Affected Files**:
- `src/samples/WorkflowForge.Samples.BasicConsole/Helpers/ThreadSafeRandom.cs` (new)
- `tests/WorkflowForge.Tests/Helpers/ThreadSafeRandom.cs` (new)
- `src/samples/WorkflowForge.Samples.BasicConsole/WorkflowForge.Samples.BasicConsole.csproj` (global using)
- `tests/WorkflowForge.Tests/WorkflowForge.Tests.csproj` (global using)
- `src/samples/WorkflowForge.Samples.BasicConsole/Samples/DataPassingSample.cs`
- `src/samples/WorkflowForge.Samples.BasicConsole/Samples/ClassBasedOperationsSample.cs`
- `src/samples/WorkflowForge.Samples.BasicConsole/Samples/ErrorHandlingSample.cs`
- `src/samples/WorkflowForge.Samples.BasicConsole/Samples/ForEachLoopSample.cs`
- `tests/WorkflowForge.Tests/Concurrency/ConcurrencyTests.cs`
- `tests/WorkflowForge.Tests/Operations/ActionWorkflowOperationEnhancedTests.cs`

---

### WF-049: Add `--validate` CLI mode for non-interactive sample smoke testing

- **Labels**: `enhancement`, `testing`, `infrastructure`
- **Priority**: Medium
- **Status**: [x] Completed

**Description**: The samples console application was interactive (menu-driven), making it impossible to automate cross-framework validation. Added a `--validate` command-line argument that runs all 33 samples non-interactively, reporting pass/fail status for each sample with runtime and OS information. Returns exit code 0 if all pass, 1 if any fail. Samples are also now multi-targeted to `net48;net8.0;net10.0` (previously `net8.0;net10.0`).

**Validation Results**:
- `net8.0` (.NET 8.0.24): 33/33 passed
- `net10.0` (.NET 10.0.3): 33/33 passed
- `net48` (.NET Framework 4.8.9325.0): 33/33 passed

**Usage**:
```
dotnet run --project src/samples/WorkflowForge.Samples.BasicConsole -f net48 -- --validate
dotnet run --project src/samples/WorkflowForge.Samples.BasicConsole -f net8.0 -- --validate
dotnet run --project src/samples/WorkflowForge.Samples.BasicConsole -f net10.0 -- --validate
```

**Affected Files**:
- `src/samples/WorkflowForge.Samples.BasicConsole/Program.cs`

---

### WF-050: Fix PersistenceMiddleware skip path overwriting restored outputs

- **Labels**: `bug`, `persistence`
- **Priority**: High
- **Status**: [x] Completed

**Description**: When the `PersistenceMiddleware` skipped an already-completed operation during workflow resume, it returned `inputData` instead of the stored output from the snapshot. The caller (`WorkflowFoundry`) then overwrote the restored operation output with this wrong value, breaking output chaining for subsequent operations, compensation logic that reads outputs, and `GetOperationOutput()` returning stale/null data. Fixed by looking up the stored output key from `foundry.Properties` when skipping, and returning it if found. Falls back to `inputData` if no stored output exists (backward-compatible).

Also changed `workflow.Operations.Count()` (LINQ extension) to `workflow.Operations.Count` (property) since `Operations` is `IReadOnlyList<T>`.

**Affected Files**:
- `src/extensions/WorkflowForge.Extensions.Persistence/PersistenceMiddleware.cs`

---

### WF-051: Normalize const visibility on internal property key classes

- **Labels**: `code-quality`, `style`
- **Priority**: Medium
- **Status**: [x] Completed

**Description**: `FoundryPropertyKeys` and `PerformancePropertyKeys` were declared as `internal static class` but contained `public const` members. In C#, `public` members on an `internal` class are effectively `internal` (the class visibility caps member visibility), so this was functionally harmless but a style inconsistency that could confuse reviewers. Changed all 27 `public const` members in `FoundryPropertyKeys` and 1 `public const` member in `PerformancePropertyKeys` to `internal const`. Zero-impact binary change (identical IL).

**Affected Files**:
- `src/core/WorkflowForge/Constants/FoundryPropertyKeys.cs`
- `src/extensions/WorkflowForge.Extensions.Observability.Performance/Constants/PerformancePropertyKeys.cs`

---

### WF-052: Wire up CI/CD version input and fix publish script matching

- **Labels**: `infrastructure`, `ci-cd`
- **Priority**: Low
- **Status**: [x] Completed

**Description**: Two infrastructure fixes:

1. **CI/CD version input ignored**: The `build-test.yml` Pack step did not pass the `version` workflow dispatch input to `dotnet pack`. Added conditional `-p:PackageVersion=${{ github.event.inputs.version }}` argument so manual dispatch can override the package version.

2. **Publish script substring matching bug**: In `publish-packages.py`, the summary tracker used `r["name"] in nupkg.name` to match packages. Since `"WorkflowForge"` is a substring of `"WorkflowForge.Extensions.DependencyInjection.2.1.0.nupkg"`, the core package was falsely marked as signed/published when any extension was processed. Changed to `nupkg.name.startswith(r["name"] + ".")` for exact prefix matching.

**Affected Files**:
- `.github/workflows/build-test.yml`
- `scripts/publish-packages.py`
