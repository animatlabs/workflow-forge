<#
.SYNOPSIS
    Creates WorkflowForge work items as GitHub issues using gh CLI.
    Idempotent: skips creation if issue with same title exists.
#>

$ErrorActionPreference = "Stop"
$Repo = "animatlabs/workflow-forge"

# ---------------- LABEL COLORS ----------------
$LabelColors = @{
    "breaking-change" = "d73a4a"
    "enhancement"     = "a2eeef"
    "core"            = "0075ca"
    "bug"             = "d73a4a"
    "extensions"      = "7057ff"
    "performance"     = "fbca04"
    "packaging"       = "e4e669"
    "code-quality"    = "0e8a16"
    "tests"           = "c5def5"
    "samples"         = "bfd4f2"
    "documentation"   = "0075ca"
    "infrastructure"  = "d4c5f9"
    "ci-cd"           = "d876e3"
    "tooling"         = "d876e3"
    "maintenance"     = "fef2c0"
    "persistence"     = "5319e7"
    "refactoring"     = "fbca04"
    "benchmarks"      = "c2e0c6"
    "architecture"    = "006b75"
    "testing"         = "c5def5"
    "net48-compat"    = "f9d0c4"
    "api"             = "0e8a16"
    "style"           = "fef2c0"
    "breaking-internal" = "e99695"
    "internal"        = "ededed"
}

# ---------------- LABEL NORMALIZATION ----------------
function Standardize-Label {
    param([string]$Label)

    $map = @{
        "docs"    = "documentation"
        "infra"   = "infrastructure"
        "bug-fix" = "bug"
    }

    if ($map.ContainsKey($Label)) {
        return $map[$Label]
    }

    return $Label
}

# Work items: WF-ID, Title, Description, Labels (raw), AffectedFiles
$WorkItems = @(
    @{
        Id = "WF-001"
        Title = "Remove ``SupportsRestore`` from ``IWorkflowOperation`` and ``IWorkflow`` interfaces"
        Description = "Remove the ``bool SupportsRestore { get; }`` property from both ``IWorkflowOperation`` and ``IWorkflow`` interfaces. This is a breaking change - any direct implementor of these interfaces will need to remove their ``SupportsRestore`` property. Update the ``RestoreAsync`` XML doc to remove the ``<exception cref=""NotSupportedException"">`` tag."
        Labels = @("breaking-change", "enhancement", "core")
        AffectedFiles = @("src/core/WorkflowForge/Abstractions/IWorkflowOperation.cs", "src/core/WorkflowForge/Abstractions/IWorkflow.cs")
    },
    @{
        Id = "WF-002"
        Title = "Make ``RestoreAsync`` a virtual no-op default in ``WorkflowOperationBase``"
        Description = "In ``WorkflowOperationBase``, remove the ``SupportsRestore`` property and change ``RestoreAsync`` from throwing ``NotSupportedException`` to returning ``Task.CompletedTask`` (no-op). This applies to both the untyped and typed variants, as well as the sealed untyped override in the generic base."
        Labels = @("enhancement", "core")
        AffectedFiles = @("src/core/WorkflowForge/Operations/WorkflowOperationBase.cs")
    },
    @{
        Id = "WF-003"
        Title = "Remove workflow-level compensation gate in ``WorkflowSmith``"
        Description = "Remove the ``if (workflow.SupportsRestore)`` gate that prevents ALL compensation if any operation doesn't support restore. Remove ``SupportsRestore`` from ``Workflow.cs``. The smith should always enter the compensation path on failure. In ``CompensateForgedOperationsAsync``, remove the per-operation ``SupportsRestore`` skip and just call ``RestoreAsync`` on every completed operation. Add ``catch (NotSupportedException)`` for backward compat. Also fix Bug #10 (Operations downcast) using ``.AsReadOnly()``."
        Labels = @("enhancement", "core", "bug")
        AffectedFiles = @("src/core/WorkflowForge/Workflow.cs", "src/core/WorkflowForge/WorkflowSmith.cs")
    },
    @{
        Id = "WF-004"
        Title = "Remove ``SupportsRestore`` from all operation implementations"
        Description = "Remove the ``SupportsRestore`` property and all ``if (!SupportsRestore) throw`` guards from 8 operation classes. For delegate/action operations, change ``RestoreAsync`` to check if ``_restoreFunc != null`` and invoke it, else return ``Task.CompletedTask``. Update comments in ``LoggingOperation`` and ``DelayOperation``."
        Labels = @("enhancement", "core")
        AffectedFiles = @("src/core/WorkflowForge/Operations/DelegateWorkflowOperation.cs", "src/core/WorkflowForge/Operations/ActionWorkflowOperation.cs", "src/core/WorkflowForge/Operations/ConditionalWorkflowOperation.cs", "src/core/WorkflowForge/Operations/ForEachWorkflowOperation.cs", "src/core/WorkflowForge/Operations/LoggingOperation.cs", "src/core/WorkflowForge/Operations/DelayOperation.cs", "src/extensions/WorkflowForge.Extensions.Resilience/RetryWorkflowOperation.cs", "src/extensions/WorkflowForge.Extensions.Resilience.Polly/PollyRetryOperation.cs")
    },
    @{
        Id = "WF-005"
        Title = "Add optional ``restoreAction`` parameter to ``WorkflowBuilder.AddOperation`` and factory methods"
        Description = "Add an optional ``restoreAction`` parameter to the async and sync ``AddOperation`` overloads in ``WorkflowBuilder``. Add matching optional ``restoreFunc`` parameters to ``DelegateWorkflowOperation`` static factory methods and the ``WorkflowOperations`` factory class. Also fix Bug #11 by caching the ``ReadOnlyCollection`` for the ``Operations`` property."
        Labels = @("enhancement", "core")
        AffectedFiles = @("src/core/WorkflowForge/WorkflowBuilder.cs", "src/core/WorkflowForge/Operations/DelegateWorkflowOperation.cs")
    },
    @{
        Id = "WF-006"
        Title = "Fix: WorkflowTimeoutMiddleware doesn't propagate cancellation"
        Description = "The timeout middleware creates a linked ``CancellationTokenSource`` but never propagates it to ``next()``. Timed-out workflows continue executing in background. Fix by storing the linked CTS token in ``foundry.Properties`` and cancelling after timeout detection."
        Labels = @("bug", "core")
        AffectedFiles = @("src/core/WorkflowForge/Middleware/WorkflowTimeoutMiddleware.cs")
    },
    @{
        Id = "WF-007"
        Title = "Fix: ConditionalWorkflowOperation ``_lastConditionResult`` thread safety"
        Description = "``_lastConditionResult`` is a non-volatile ``bool`` shared between ``ForgeAsync`` (write) and ``RestoreAsync`` (read) which can be on different threads. Add ``volatile`` modifier."
        Labels = @("bug", "core")
        AffectedFiles = @("src/core/WorkflowForge/Operations/ConditionalWorkflowOperation.cs")
    },
    @{
        Id = "WF-008"
        Title = "Fix: Event handler memory leaks on Dispose in WorkflowSmith/WorkflowFoundry"
        Description = "Neither ``WorkflowSmith`` nor ``WorkflowFoundry`` clears event subscriptions on ``Dispose()``. Since the smith is registered as Singleton in DI, event handlers create GC roots for the app's lifetime. Set all event fields to ``null`` in ``Dispose()``."
        Labels = @("bug", "core")
        AffectedFiles = @("src/core/WorkflowForge/WorkflowSmith.cs", "src/core/WorkflowForge/WorkflowFoundry.cs")
    },
    @{
        Id = "WF-009"
        Title = "Fix: Recovery extension silently swallows exceptions"
        Description = "``RecoveryExtensions.cs`` and ``RecoveryCoordinator.cs`` have ``catch { /* ignore */ }`` with no logging. Failed resume/recovery is invisible. Log the exception at Warning level before continuing."
        Labels = @("bug", "extensions")
        AffectedFiles = @("src/extensions/WorkflowForge.Extensions.Persistence.Recovery/RecoveryExtensions.cs", "src/extensions/WorkflowForge.Extensions.Persistence.Recovery/RecoveryCoordinator.cs")
    },
    @{
        Id = "WF-010"
        Title = "Fix: WorkflowSmith Dispose race condition with concurrency limiter"
        Description = "If ``ForgeAsync`` is mid-flight and calls ``_concurrencyLimiter.Release()`` after ``Dispose()`` has called ``_concurrencyLimiter?.Dispose()``, it throws ``ObjectDisposedException``. Wrap the ``Release()`` call in a ``try-catch(ObjectDisposedException)``."
        Labels = @("bug", "core")
        AffectedFiles = @("src/core/WorkflowForge/WorkflowSmith.cs")
    },
    @{
        Id = "WF-011"
        Title = "Fix: WorkflowSmith doesn't clone options defensively"
        Description = "``_options = options ?? new WorkflowForgeOptions();`` stores the caller's reference. Mutations to the options object after smith construction affect the smith. Fix by cloning: ``_options = options?.CloneTyped() ?? new WorkflowForgeOptions();``"
        Labels = @("bug", "core")
        AffectedFiles = @("src/core/WorkflowForge/WorkflowSmith.cs")
    },
    @{
        Id = "WF-012"
        Title = "Fix: Event handler exceptions conflated with operation failures in WorkflowFoundry"
        Description = "``OperationStarted?.Invoke(...)`` is inside the try block. If an event handler throws, it's caught by the outer catch and treated as an operation failure. Wrap event invocations in individual try-catch blocks that log but don't fail the operation."
        Labels = @("bug", "core")
        AffectedFiles = @("src/core/WorkflowForge/WorkflowFoundry.cs")
    },
    @{
        Id = "WF-013"
        Title = "Fix: PersistenceMiddleware O(n^2) operation indexing"
        Description = "``operations.ToList().FindIndex(op => op.Id == operation.Id)`` is called twice per operation execution, giving O(n^2) total. Build a ``Dictionary<Guid, int>`` index once per workflow execution and reuse it."
        Labels = @("bug", "performance", "extensions")
        AffectedFiles = @("src/extensions/WorkflowForge.Extensions.Persistence/PersistenceMiddleware.cs")
    },
    @{
        Id = "WF-014"
        Title = "Fix: Serilog extension missing Version tag in csproj"
        Description = "The Serilog extension csproj has no ``<Version>`` or ``<PackageVersion>`` tag, defaulting to 1.0.0 when packed. Add ``<Version>2.1.0</Version>``."
        Labels = @("bug", "packaging")
        AffectedFiles = @("src/extensions/WorkflowForge.Extensions.Logging.Serilog/WorkflowForge.Extensions.Logging.Serilog.csproj")
    },
    @{
        Id = "WF-015"
        Title = "Fix: Workflow.Operations can be downcast and mutated"
        Description = "``new List<IWorkflowOperation>(operations)`` cast to ``IReadOnlyList<T>`` can be downcast back to ``List<T>`` by consumers. Use ``.AsReadOnly()``."
        Labels = @("bug", "core")
        AffectedFiles = @("src/core/WorkflowForge/Workflow.cs")
    },
    @{
        Id = "WF-016"
        Title = "Fix: WorkflowBuilder.Operations allocates ReadOnlyCollection on every access"
        Description = "``Operations`` returns ``new ReadOnlyCollection<IWorkflowOperation>(_operations)`` every time. Cache the collection and invalidate on mutation."
        Labels = @("bug", "performance", "core")
        AffectedFiles = @("src/core/WorkflowForge/WorkflowBuilder.cs")
    },
    @{
        Id = "WF-017"
        Title = "Extract foundry property key constants into ``FoundryPropertyKeys.cs``"
        Description = "Create a new ``FoundryPropertyKeys`` static class with all framework-owned property keys currently scattered as magic strings. Includes operation output/state, timing, error, timeout, correlation keys, plus ``Workflow.Name``, ``Validation.Status``, and ``Validation.Errors`` discovered in audit. Update all files using these magic strings to reference the constants."
        Labels = @("code-quality", "core")
        AffectedFiles = @("src/core/WorkflowForge/Constants/FoundryPropertyKeys.cs", "WorkflowFoundry.cs", "WorkflowSmith.cs", "TimingMiddleware.cs", "ErrorHandlingMiddleware.cs", "WorkflowTimeoutMiddleware.cs", "OperationTimeoutMiddleware.cs", "FoundryPropertyExtensions.cs", "AuditMiddleware.cs", "AuditExtensions.cs", "ValidationMiddleware.cs")
    },
    @{
        Id = "WF-018"
        Title = "Seal all EventArgs and concrete Exception classes"
        Description = "Add ``sealed`` modifier to all 11 EventArgs classes and all concrete (non-base) exception classes. Enables JIT devirtualization."
        Labels = @("code-quality", "core")
        AffectedFiles = @("src/core/WorkflowForge/Events/*.cs", "src/core/WorkflowForge/Exceptions/*.cs")
    },
    @{
        Id = "WF-019"
        Title = "Fix filename typo: ``WorkflowForgeLoggerExtesions.cs``"
        Description = "Rename ``WorkflowForgeLoggerExtesions.cs`` (missing 'n') to ``WorkflowForgeLoggerExtensions.cs``."
        Labels = @("code-quality", "core")
        AffectedFiles = @("src/core/WorkflowForge/Extensions/WorkflowForgeLoggerExtesions.cs", "WorkflowForgeLoggerExtensions.cs")
    },
    @{
        Id = "WF-020"
        Title = "Normalize middleware options to extend ``WorkflowForgeOptionsBase``"
        Description = "Make ``TimingMiddlewareOptions``, ``LoggingMiddlewareOptions``, and ``ErrorHandlingMiddlewareOptions`` extend ``WorkflowForgeOptionsBase`` with proper ``Validate()`` and ``Clone()`` implementations."
        Labels = @("code-quality", "core")
        AffectedFiles = @("src/core/WorkflowForge/Options/Middleware/TimingMiddlewareOptions.cs", "src/core/WorkflowForge/Options/Middleware/LoggingMiddlewareOptions.cs", "src/core/WorkflowForge/Options/Middleware/ErrorHandlingMiddlewareOptions.cs")
    },
    @{
        Id = "WF-021"
        Title = "Add missing guard clauses and null-element validation"
        Description = "Add ``foundry ?? throw new ArgumentNullException`` to ``WorkflowOperationBase.ForgeAsync``, null-element validation to ``ForEachWorkflowOperation`` constructor, and throw on null operations in ``FoundryPropertyExtensions.WithOperations`` instead of silent skip."
        Labels = @("code-quality", "core")
        AffectedFiles = @("src/core/WorkflowForge/Operations/WorkflowOperationBase.cs", "src/core/WorkflowForge/Operations/ForEachWorkflowOperation.cs", "src/core/WorkflowForge/Extensions/FoundryPropertyExtensions.cs")
    },
    @{
        Id = "WF-022"
        Title = "Add consistent ``GC.SuppressFinalize`` to all Dispose methods"
        Description = "Add ``GC.SuppressFinalize(this)`` to all ``Dispose()`` methods that are currently missing it, for consistency with CA1816 and Microsoft's dispose pattern guidance."
        Labels = @("code-quality", "core", "extensions")
        AffectedFiles = @("src/core/WorkflowForge/Operations/WorkflowOperationBase.cs", "src/extensions/WorkflowForge.Extensions.Resilience/RetryWorkflowOperation.cs", "src/extensions/WorkflowForge.Extensions.Resilience/CircuitBreakerMiddleware.cs", "src/extensions/WorkflowForge.Extensions.Observability.HealthChecks/HealthCheckService.cs", "src/extensions/WorkflowForge.Extensions.Observability.OpenTelemetry/WorkflowForgeOpenTelemetryService.cs")
    },
    @{
        Id = "WF-023"
        Title = "Dispose middleware instances in WorkflowSmith.Dispose"
        Description = "In ``WorkflowSmith.Dispose()``, iterate through workflow and operation middleware collections and dispose any that implement ``IDisposable``."
        Labels = @("code-quality", "core")
        AffectedFiles = @("src/core/WorkflowForge/WorkflowSmith.cs")
    },
    @{
        Id = "WF-024"
        Title = "Add missing ``ConfigureAwait(false)`` across all library await calls"
        Description = "Library code should use ``ConfigureAwait(false)`` on all ``await`` calls to avoid capturing the synchronization context. Add it to 15+ locations across extension projects."
        Labels = @("code-quality", "extensions")
        AffectedFiles = @("src/extensions/WorkflowForge.Extensions.Validation/ValidationMiddleware.cs", "src/extensions/WorkflowForge.Extensions.Audit/AuditMiddleware.cs", "src/extensions/WorkflowForge.Extensions.Audit/AuditExtensions.cs", "src/extensions/WorkflowForge.Extensions.Observability.HealthChecks/WorkflowFoundryHealthCheckExtensions.cs", "src/extensions/WorkflowForge.Extensions.Resilience/Strategies/RandomIntervalStrategy.cs", "src/extensions/WorkflowForge.Extensions.Resilience/Strategies/ExponentialBackoffStrategy.cs", "src/core/WorkflowForge/Extensions/FoundryPropertyExtensions.cs", "src/extensions/WorkflowForge.Extensions.Validation/ValidationExtensions.cs")
    },
    @{
        Id = "WF-025"
        Title = "Fix: HealthCheckService.LastResults thread safety"
        Description = "``LastResults`` is updated from a timer callback and read without synchronization. Use ``Interlocked.Exchange`` or ``lock`` to ensure thread-safe reads and writes."
        Labels = @("bug", "extensions")
        AffectedFiles = @("src/extensions/WorkflowForge.Extensions.Observability.HealthChecks/HealthCheckService.cs")
    },
    @{
        Id = "WF-026"
        Title = "Fix: RetryWorkflowOperation.Dispose swallows exceptions without logging"
        Description = "``RetryWorkflowOperation.Dispose()`` catches all exceptions silently. Add logging for swallowed exceptions."
        Labels = @("code-quality", "extensions")
        AffectedFiles = @("src/extensions/WorkflowForge.Extensions.Resilience/RetryWorkflowOperation.cs")
    },
    @{
        Id = "WF-027"
        Title = "Update tests for SupportsRestore removal + add new test classes"
        Description = "Update 10 existing test files to remove all ``SupportsRestore`` assertions and references. Add 6 new test classes: builder restore parameter, mixed compensation integration, NotSupportedException backward compat, event memory leak, options clone, dispose race."
        Labels = @("tests")
        AffectedFiles = @("10 existing test files", "6+ new test files")
    },
    @{
        Id = "WF-028"
        Title = "Update 24+ sample files removing SupportsRestore"
        Description = "Remove ``SupportsRestore`` property from all sample operation classes (57+ occurrences across 24+ files). Change any ``RestoreAsync`` that throws ``NotSupportedException`` to ``return Task.CompletedTask``. Update ``CompensationBehaviorSample.cs`` to demonstrate mixed-workflow compensation. Add builder restore example."
        Labels = @("samples")
        AffectedFiles = @("24+ files in src/samples/WorkflowForge.Samples.BasicConsole/Samples/")
    },
    @{
        Id = "WF-029"
        Title = "Update documentation line-by-line + create CHANGELOG.md"
        Description = "Line-by-line update of 11 doc files (27+ occurrences of ``SupportsRestore``). Remove all references, update interface definitions, rewrite compensation guidance, update code examples. Create ``CHANGELOG.md`` at repository root. Update ``README.md``."
        Labels = @("docs")
        AffectedFiles = @("11 doc files", "README.md", "CHANGELOG.md")
    },
    @{
        Id = "WF-030"
        Title = "Version bump all 13 csproj to 2.1.0 + Directory.Build.props + SNK signing"
        Description = "Update ``<Version>`` in all 13 ``.csproj`` files from 2.0.0 to 2.1.0. Create ``src/Directory.Build.props`` with centralized build settings (LangVersion, Nullable, Deterministic, SignAssembly, SourceLink, symbol packages). Generate ``WorkflowForge.snk`` and update ``InternalsVisibleTo`` entries with public key token."
        Labels = @("infra", "packaging")
        AffectedFiles = @("13 .csproj files", "src/Directory.Build.props", "WorkflowForge.snk", "WorkflowForge.csproj")
    },
    @{
        Id = "WF-031"
        Title = "Add CI/CD pipeline with manual publish dispatch"
        Description = "Create ``.github/workflows/build-test.yml`` with build/test on push to ``main`` and PRs. Add a separate ``workflow_dispatch`` trigger for publishing (manual only, NOT auto-publish). Matrix: ``net8.0`` test runner."
        Labels = @("infra", "ci-cd")
        AffectedFiles = @(".github/workflows/build-test.yml")
    },
    @{
        Id = "WF-032"
        Title = "Create cross-platform Python publish script + rename legacy PS1"
        Description = "Create ``publish-packages.py`` (Python 3.8+, standard library only) with ``--version``, ``--api-key``, ``--publish``, ``--sign`` flags. Dry-run by default. Rename ``publish-packages.ps1`` to ``publish-packages.legacy.ps1`` with note at top pointing to Python version. Also remove ``SupportsRestore`` from benchmark file."
        Labels = @("infra", "tooling")
        AffectedFiles = @("publish-packages.py", "publish-packages.ps1", "publish-packages.legacy.ps1", "src/benchmarks/WorkflowForge.Benchmarks.Comparative/Implementations/WorkflowForge/Scenario6_ErrorHandling_WorkflowForge.cs")
    },
    @{
        Id = "WF-033"
        Title = "Document restoreAction/restoreFunc API"
        Description = "The new ``restoreAction`` / ``restoreFunc`` parameters on ``WorkflowBuilder.AddOperation``, ``DelegateWorkflowOperation``, and ``WithOperation`` extension methods were undocumented. Added documentation in ``operations.md``, ``api-reference.md``, ``getting-started.md``, and added ``restoreAction`` overloads to ``WithOperation`` foundry extension methods."
        Labels = @("documentation", "enhancement")
        AffectedFiles = @("docs/core/operations.md", "docs/reference/api-reference.md", "docs/getting-started/getting-started.md", "src/core/WorkflowForge/Extensions/FoundryPropertyExtensions.cs")
    },
    @{
        Id = "WF-034"
        Title = "Update PackageReleaseNotes in all csproj files"
        Description = "All 12 ``.csproj`` files with ``<PackageReleaseNotes>`` still referenced v2.0.0. DI extension had no release notes. Updated all 13 projects to v2.1.0 release notes."
        Labels = @("packaging", "maintenance")
        AffectedFiles = @("All 13 .csproj files under src/")
    },
    @{
        Id = "WF-035"
        Title = "Fix PersistenceMiddleware duplicate operation handling"
        Description = "The operation index dictionary used ``dict[key] = value`` which silently overwrites on duplicate Guid keys. Fixed to use first-occurrence-wins with a debug warning. Also cached the dictionary on ``foundry.Properties`` (was rebuilt per middleware call) and computed ``ResolveKeys`` once per ``ExecuteAsync`` (was called 3 times)."
        Labels = @("bug", "persistence")
        AffectedFiles = @("src/extensions/WorkflowForge.Extensions.Persistence/PersistenceMiddleware.cs")
    },
    @{
        Id = "WF-036"
        Title = "Complete FoundryPropertyKeys constants"
        Description = "Added missing constants (``ErrorStackTrace``, ``TimingDurationTicks``, ``TimingFailed``, ``OperationTimeoutFormat``) to ``FoundryPropertyKeys``. Replaced all magic strings in core code with constant references. Replaced extension magic strings (``Workflow.Name``, ``Validation.Status``, ``Validation.Errors``) with constants. Created ``PerformancePropertyKeys`` for the Performance extension."
        Labels = @("code-quality", "enhancement")
        AffectedFiles = @("src/core/WorkflowForge/Constants/FoundryPropertyKeys.cs", "src/core/WorkflowForge/Middleware/ErrorHandlingMiddleware.cs", "src/core/WorkflowForge/Middleware/TimingMiddleware.cs", "src/core/WorkflowForge/Middleware/OperationTimeoutMiddleware.cs", "src/core/WorkflowForge/WorkflowSmith.cs", "src/core/WorkflowForge/Extensions/WorkflowForgeLoggerExtensions.cs", "src/extensions/WorkflowForge.Extensions.Audit/AuditMiddleware.cs", "src/extensions/WorkflowForge.Extensions.Audit/AuditExtensions.cs", "src/extensions/WorkflowForge.Extensions.Validation/ValidationMiddleware.cs", "src/extensions/WorkflowForge.Extensions.Observability.Performance/Constants/PerformancePropertyKeys.cs", "src/extensions/WorkflowForge.Extensions.Observability.Performance/WorkflowFoundryPerformanceExtensions.cs")
    },
    @{
        Id = "WF-037"
        Title = "Refactor all samples and benchmarks to use WorkflowOperationBase"
        Description = "Refactored 103 operation classes (97 in samples, 6 in benchmarks) from direct ``IWorkflowOperation`` implementation to extending ``WorkflowOperationBase``. Eliminates ~4-5 lines of boilerplate per class (Id, Dispose, RestoreAsync) and gains lifecycle hooks. Operations with real ``RestoreAsync`` logic kept as overrides."
        Labels = @("refactoring", "samples", "benchmarks")
        AffectedFiles = @("35 sample files", "src/benchmarks/WorkflowForge.Benchmarks/OperationPerformanceBenchmark.cs", "src/benchmarks/WorkflowForge.Benchmarks/WorkflowThroughputBenchmark.cs", "src/benchmarks/WorkflowForge.Benchmarks.Comparative/Implementations/WorkflowForge/Scenario6_ErrorHandling_WorkflowForge.cs")
    },
    @{
        Id = "WF-038"
        Title = "Fix publish script .snupkg handling and move to scripts/"
        Description = "The Python publish script only handled ``.nupkg`` (not ``.snupkg``). Fixed signing and publishing to include both package types. Moved scripts to ``scripts/`` folder. Created ``scripts/README.md`` with SNK guide, signing instructions, and GitHub secrets setup."
        Labels = @("infrastructure", "packaging")
        AffectedFiles = @("scripts/publish-packages.py", "scripts/publish-packages.legacy.ps1", "scripts/README.md")
    },
    @{
        Id = "WF-039"
        Title = "Fix CI/CD workflow artifact reuse and .snupkg support"
        Description = "The CI/CD publish job rebuilt from source instead of using the tested build artifacts. Fixed to download artifacts from the build job. Added ``.snupkg`` handling to upload, signing, and push steps. Added cert cleanup after signing."
        Labels = @("infrastructure", "ci-cd")
        AffectedFiles = @(".github/workflows/build-test.yml")
    },
    @{
        Id = "WF-040"
        Title = "Fix Scenario5 benchmark closure bug"
        Description = "Loop variables ``i`` and ``j`` were captured by closures in ``Task.Run`` and ``WithOperation`` lambdas. Fixed by capturing into local variables before the closures."
        Labels = @("bug", "benchmarks")
        AffectedFiles = @("src/benchmarks/WorkflowForge.Benchmarks.Comparative/Implementations/WorkflowForge/Scenario5_ConcurrentExecution_WorkflowForge.cs")
    },
    @{
        Id = "WF-041"
        Title = "Fix ForEachLoopWorkflow misleading return value"
        Description = "The benchmark set items in Properties but ``ForEachWorkflowOperation`` with ``SharedInput`` never used them. Fixed misleading return value."
        Labels = @("bug", "benchmarks")
        AffectedFiles = @("src/benchmarks/WorkflowForge.Benchmarks/WorkflowThroughputBenchmark.cs")
    },
    @{
        Id = "WF-042"
        Title = "Emphasize WorkflowOperationBase in all documentation"
        Description = "Updated ``IWorkflowOperation`` XML docs with ``<remarks>`` recommending ``WorkflowOperationBase``. Updated all documentation files to position ``IWorkflowOperation`` as an advanced/escape-hatch API. Updated all code examples in docs to use ``WorkflowOperationBase`` / ``ForgeAsyncCore`` pattern."
        Labels = @("documentation")
        AffectedFiles = @("src/core/WorkflowForge/Abstractions/IWorkflowOperation.cs", "docs/core/operations.md", "docs/reference/api-reference.md", "docs/getting-started/getting-started.md", "docs/getting-started/samples-guide.md")
    },
    @{
        Id = "WF-043"
        Title = "Make FoundryPropertyKeys and PerformancePropertyKeys internal"
        Description = "``FoundryPropertyKeys`` and ``PerformancePropertyKeys`` were ``public static class`` but contain implementation details that external consumers should not couple to. Changed both to ``internal static class``. Added ``InternalsVisibleTo`` entries for Persistence, Audit, Validation, Performance, and Benchmark projects. Added ``GetOperationOutput`` / ``GetOperationOutput<T>`` public extension methods as the intended orchestrator-level API for reading operation outputs. Fixed Scenario2 benchmark to use the new public API instead of internal constants."
        Labels = @("architecture", "breaking-internal")
        AffectedFiles = @("src/core/WorkflowForge/Constants/FoundryPropertyKeys.cs", "src/core/WorkflowForge/WorkflowForge.csproj", "src/core/WorkflowForge/Extensions/FoundryPropertyExtensions.cs", "src/extensions/WorkflowForge.Extensions.Observability.Performance/Constants/PerformancePropertyKeys.cs", "src/benchmarks/WorkflowForge.Benchmarks.Comparative/Implementations/WorkflowForge/Scenario2_DataPassing_WorkflowForge.cs")
    },
    @{
        Id = "WF-044"
        Title = "Change operation property keys to index:name composite format"
        Description = "The operation property key format ""Operation.{guid}.Output"" was unreadable in logs/debug and caused silent collisions when the same operation instance was added twice to a workflow. Changed to ""Operation.{index}:{name}.Output"" where index is the zero-based position and name is the operation name. This produces readable keys like ""Operation.0:ValidateOrder.Output"" and eliminates collisions because the index is always unique."
        Labels = @("architecture", "internal")
        AffectedFiles = @("src/core/WorkflowForge/Constants/FoundryPropertyKeys.cs", "src/core/WorkflowForge/WorkflowFoundry.cs", "src/core/WorkflowForge/WorkflowSmith.cs", "src/core/WorkflowForge/Middleware/OperationTimeoutMiddleware.cs", "src/core/WorkflowForge/Extensions/FoundryPropertyExtensions.cs", "tests/WorkflowForge.Tests/WorkflowFoundryTests.cs", "tests/WorkflowForge.Tests/Concurrency/ConcurrencyTests.cs")
    },
    @{
        Id = "WF-045"
        Title = "Rewrite PersistenceMiddleware with counter-based tracking"
        Description = "The ``PersistenceMiddleware`` used a ``Dictionary<Guid, int>`` to map operation IDs to their indices. This broke when the same operation instance was added twice (same GUID, dictionary collision). The ""first-win"" approach masked the bug. Rewrote to read the current operation index from ``FoundryPropertyKeys.CurrentOperationIndex`` (set by the foundry), with a fallback internal counter for backward compatibility. Removed the dictionary, removed ``GetOrBuildOperationIndex``, removed ``OperationIndexCacheKey``, and removed all ``#if`` directives."
        Labels = @("architecture", "bug-fix")
        AffectedFiles = @("src/extensions/WorkflowForge.Extensions.Persistence/PersistenceMiddleware.cs", "tests/WorkflowForge.Extensions.Persistence.Tests/PersistenceMiddlewareTests.cs")
    },
    @{
        Id = "WF-046"
        Title = "Multi-target tests/benchmarks for net48;net8.0;net10.0"
        Description = "Multi-targeted all test, benchmark, and sample projects to validate cross-framework compatibility and enable performance comparisons across .NET versions. Tests target ``net48;net8.0;net10.0``. Samples target ``net8.0;net10.0``. Internal benchmarks target ``net48;net8.0;net10.0``. Comparative benchmarks target ``net48;net8.0;net10.0`` with Elsa conditionally included on net8.0+ only."
        Labels = @("infrastructure", "testing")
        AffectedFiles = @("All 7 test project .csproj files", "src/samples/WorkflowForge.Samples.BasicConsole/WorkflowForge.Samples.BasicConsole.csproj", "src/benchmarks/WorkflowForge.Benchmarks/WorkflowForge.Benchmarks.csproj", "src/benchmarks/WorkflowForge.Benchmarks.Comparative/WorkflowForge.Benchmarks.Comparative.csproj")
    },
    @{
        Id = "WF-047"
        Title = "Add GetOperationOutput public orchestrator API"
        Description = "Added ``GetOperationOutput(this IWorkflowFoundry foundry, int operationIndex, string operationName)`` and generic ``GetOperationOutput<T>`` extension methods to ``FoundryPropertyExtensions``. These are orchestrator-level APIs for workflow composition and test inspection. Operations should NOT use them to read other operations' outputs (use ``ForgeAsyncCore`` input parameter or ``foundry.Properties`` with domain-specific keys instead)."
        Labels = @("enhancement", "api")
        AffectedFiles = @("src/core/WorkflowForge/Extensions/FoundryPropertyExtensions.cs")
    },
    @{
        Id = "WF-048"
        Title = "Fix ``new Random()`` TickCount-collision across samples and tests"
        Description = "On .NET Framework 4.8, ``new Random()`` seeds from ``Environment.TickCount`` (millisecond resolution). Two ``new Random()`` calls within the same millisecond produce identical sequences. This affected 8 call sites across 6 sample files and 2 test files. Fix: Created a ``ThreadSafeRandom`` internal static helper class."
        Labels = @("bug", "net48-compat")
        AffectedFiles = @("src/samples/WorkflowForge.Samples.BasicConsole/Helpers/ThreadSafeRandom.cs", "tests/WorkflowForge.Tests/Helpers/ThreadSafeRandom.cs", "src/samples/WorkflowForge.Samples.BasicConsole/Samples/DataPassingSample.cs", "src/samples/WorkflowForge.Samples.BasicConsole/Samples/ClassBasedOperationsSample.cs", "src/samples/WorkflowForge.Samples.BasicConsole/Samples/ErrorHandlingSample.cs", "src/samples/WorkflowForge.Samples.BasicConsole/Samples/ForEachLoopSample.cs", "tests/WorkflowForge.Tests/Concurrency/ConcurrencyTests.cs", "tests/WorkflowForge.Tests/Operations/ActionWorkflowOperationEnhancedTests.cs")
    },
    @{
        Id = "WF-049"
        Title = "Add ``--validate`` CLI mode for non-interactive sample smoke testing"
        Description = "The samples console application was interactive (menu-driven), making it impossible to automate cross-framework validation. Added a ``--validate`` command-line argument that runs all 33 samples non-interactively, reporting pass/fail status for each sample with runtime and OS information. Returns exit code 0 if all pass, 1 if any fail."
        Labels = @("enhancement", "testing", "infrastructure")
        AffectedFiles = @("src/samples/WorkflowForge.Samples.BasicConsole/Program.cs")
    },
    @{
        Id = "WF-050"
        Title = "Fix PersistenceMiddleware skip path overwriting restored outputs"
        Description = "When the ``PersistenceMiddleware`` skipped an already-completed operation during workflow resume, it returned ``inputData`` instead of the stored output from the snapshot. The caller (``WorkflowFoundry``) then overwrote the restored operation output with this wrong value, breaking output chaining for subsequent operations, compensation logic that reads outputs, and ``GetOperationOutput()`` returning stale/null data. Fixed by looking up the stored output key from ``foundry.Properties`` when skipping, and returning it if found."
        Labels = @("bug", "persistence")
        AffectedFiles = @("src/extensions/WorkflowForge.Extensions.Persistence/PersistenceMiddleware.cs")
    },
    @{
        Id = "WF-051"
        Title = "Normalize const visibility on internal property key classes"
        Description = "``FoundryPropertyKeys`` and ``PerformancePropertyKeys`` were declared as ``internal static class`` but contained ``public const`` members. In C#, ``public`` members on an ``internal`` class are effectively ``internal``, so this was functionally harmless but a style inconsistency. Changed all 27 ``public const`` members in ``FoundryPropertyKeys`` and 1 ``public const`` member in ``PerformancePropertyKeys`` to ``internal const``."
        Labels = @("code-quality", "style")
        AffectedFiles = @("src/core/WorkflowForge/Constants/FoundryPropertyKeys.cs", "src/extensions/WorkflowForge.Extensions.Observability.Performance/Constants/PerformancePropertyKeys.cs")
    },
    @{
        Id = "WF-052"
        Title = "Wire up CI/CD version input and fix publish script matching"
        Description = "1) CI/CD version input ignored: The Pack step did not pass the version workflow dispatch input to dotnet pack. Added conditional -p:PackageVersion argument. 2) Publish script substring matching bug: In publish-packages.py, the summary tracker used r['name'] in nupkg.name to match packages. Since 'WorkflowForge' is a substring of extension package names, the core package was falsely marked as signed/published. Changed to nupkg.name.startswith(r['name'] + '.') for exact prefix matching."
        Labels = @("infrastructure", "ci-cd")
        AffectedFiles = @(".github/workflows/build-test.yml", "scripts/publish-packages.py")
    },
    @{
        Id = "WF-053"
        Title = "Integrate SonarCloud with CI/CD pipeline"
        Description = "Integrated SonarCloud for continuous code quality analysis. Added dotnet-sonarscanner to the build pipeline with OpenCover coverage reporting. Configured sonar.exclusions to exclude benchmarks, samples, and tests from analysis. Configured sonar.coverage.exclusions for the same. Added SonarCloud package caching for faster CI runs. Required SONAR_TOKEN repository secret and Java 17 as SonarScanner prerequisites."
        Labels = @("infrastructure", "code-quality", "ci-cd")
        AffectedFiles = @(".github/workflows/build-test.yml")
    },
    @{
        Id = "WF-054"
        Title = "Fix structured logging across codebase (string interpolation removal)"
        Description = "Converted all string interpolation in logger calls to structured logging format with named placeholders ({OperationName}, {WorkflowName}, etc.). Fixed ConsoleLogger.FormatMessage to convert named placeholders to positional format before string.Format. Affected 28+ logger calls in core/extensions and 11 calls in samples."
        Labels = @("code-quality", "core", "extensions", "samples")
        AffectedFiles = @("src/core/WorkflowForge/Loggers/ConsoleLogger.cs", "src/core/WorkflowForge/Middleware/WorkflowTimeoutMiddleware.cs", "src/core/WorkflowForge/Middleware/OperationTimeoutMiddleware.cs", "src/extensions/WorkflowForge.Extensions.Audit/AuditMiddleware.cs", "src/extensions/WorkflowForge.Extensions.Validation/ValidationMiddleware.cs", "src/extensions/WorkflowForge.Extensions.Resilience/Strategies/ExponentialBackoffStrategy.cs", "src/extensions/WorkflowForge.Extensions.Resilience/Strategies/RandomIntervalStrategy.cs", "src/extensions/WorkflowForge.Extensions.Resilience.Polly/PollyRetryOperation.cs", "src/extensions/WorkflowForge.Extensions.Resilience.Polly/PollyResilienceStrategy.cs", "src/samples/WorkflowForge.Samples.BasicConsole/Samples/CancellationAndTimeoutSample.cs", "src/samples/WorkflowForge.Samples.BasicConsole/Samples/WorkflowMiddlewareSample.cs", "src/samples/WorkflowForge.Samples.BasicConsole/Samples/ContinueOnErrorSample.cs", "src/samples/WorkflowForge.Samples.BasicConsole/Samples/CompensationBehaviorSample.cs")
    },
    @{
        Id = "WF-055"
        Title = "Resolve SonarCloud quality gate failures (code smells, security hotspots, duplication)"
        Description = "Addressed SonarCloud quality gate failures: 8 security hotspots, 3.0% code duplication, C reliability and security ratings. Fixes included: extracting duplicate retry logic into ResilienceStrategyBase, correcting IDisposable pattern across 5 operation classes, removing unused fields, making methods static where appropriate, fixing null checks on unconstrained generics, and suppressing intentional patterns."
        Labels = @("code-quality", "core", "extensions")
        AffectedFiles = @("src/extensions/WorkflowForge.Extensions.Resilience/Abstractions/ResilienceStrategyBase.cs", "src/extensions/WorkflowForge.Extensions.Resilience/Strategies/ExponentialBackoffStrategy.cs", "src/extensions/WorkflowForge.Extensions.Resilience/Strategies/RandomIntervalStrategy.cs", "src/extensions/WorkflowForge.Extensions.Resilience/Strategies/FixedIntervalStrategy.cs", "src/core/WorkflowForge/Operations/WorkflowOperationBase.cs", "src/core/WorkflowForge/Operations/ForEachWorkflowOperation.cs", "src/core/WorkflowForge/Operations/ConditionalWorkflowOperation.cs", "src/extensions/WorkflowForge.Extensions.Resilience/RetryWorkflowOperation.cs", "src/extensions/WorkflowForge.Extensions.Resilience.Polly/PollyRetryOperation.cs", "src/core/WorkflowForge/WorkflowFoundry.cs", "src/extensions/WorkflowForge.Extensions.Logging.Serilog/SerilogWorkflowForgeLogger.cs", "src/extensions/WorkflowForge.Extensions.Validation/DataAnnotationsWorkflowValidator.cs", "src/extensions/WorkflowForge.Extensions.Observability.HealthChecks/HealthCheckService.cs", "src/extensions/WorkflowForge.Extensions.Observability.OpenTelemetry/WorkflowForgeOpenTelemetryService.cs", "src/extensions/WorkflowForge.Extensions.Audit/InMemoryAuditProvider.cs")
    },
    @{
        Id = "WF-056"
        Title = "Remove SonarAnalyzer from test/benchmark/sample projects"
        Description = "Removed SonarAnalyzer.CSharp package reference from all test (7), benchmark (2), and sample (1) projects. These projects are excluded from SonarCloud analysis via sonar.exclusions, so the analyzer adds zero value while increasing build time. Removed the NoWarn Sonar rule suppressions from tests/Directory.Build.props. Added minimal ci.yml workflow for main branch OSS/SignPath verification."
        Labels = @("infrastructure", "code-quality")
        AffectedFiles = @("tests/WorkflowForge.Tests/WorkflowForge.Tests.csproj", "tests/WorkflowForge.Extensions.Audit.Tests/WorkflowForge.Extensions.Audit.Tests.csproj", "tests/WorkflowForge.Extensions.Observability.HealthChecks.Tests/WorkflowForge.Extensions.Observability.HealthChecks.Tests.csproj", "tests/WorkflowForge.Extensions.Persistence.Tests/WorkflowForge.Extensions.Persistence.Tests.csproj", "tests/WorkflowForge.Extensions.Resilience.Tests/WorkflowForge.Extensions.Resilience.Tests.csproj", "tests/WorkflowForge.Extensions.Validation.Tests/WorkflowForge.Extensions.Validation.Tests.csproj", "tests/WorkflowForge.Extensions.DependencyInjection.Tests/WorkflowForge.Extensions.DependencyInjection.Tests.csproj", "src/benchmarks/WorkflowForge.Benchmarks/WorkflowForge.Benchmarks.csproj", "src/benchmarks/WorkflowForge.Benchmarks.Comparative/WorkflowForge.Benchmarks.Comparative.csproj", "src/samples/WorkflowForge.Samples.BasicConsole/WorkflowForge.Samples.BasicConsole.csproj", "tests/Directory.Build.props", ".github/workflows/ci.yml")
    }
)

# ---------------- HELPER: BUILD BODY ----------------
function Build-IssueBody {
    param($Item)

    $files = ""
    if ($Item.AffectedFiles -and $Item.AffectedFiles.Count -gt 0) {
        $files = "- " + ($Item.AffectedFiles -join "`n- ")
    }

    return @"
## Description
$($Item.Description)

## Affected Files
$files
"@
}

# ---------------- HELPER: CHECK EXISTING ISSUE ----------------
function Get-ExistingIssueNumber {
    param([string]$Title)

    $json = gh issue list --repo $Repo --state all --limit 500 --json number,title 2>$null
    if (-not $json) { return $null }

    $issues = $json | ConvertFrom-Json
    foreach ($issue in $issues) {
        if ($issue.title -eq $Title) {
            return $issue.number
        }
    }

    return $null
}

# ---------------- CREATE LABELS (OPTIMIZED) ----------------
Write-Host "Creating labels..." -ForegroundColor Cyan

$labelsJson = gh label list --repo $Repo --limit 500 --json name 2>$null
$existingLabels = @()

if ($labelsJson) {
    $existingLabels = ($labelsJson | ConvertFrom-Json).name
}

$AllLabels = $WorkItems | ForEach-Object { $_.Labels } |
    ForEach-Object { Standardize-Label $_ } |
    Sort-Object -Unique

foreach ($label in $AllLabels) {

    if (-not $LabelColors.ContainsKey($label)) {
        Write-Warning "Label '$label' has no color defined. Using default."
        $color = "ededed"
    }
    else {
        $color = $LabelColors[$label]
    }

    if ($label -notin $existingLabels) {
        gh label create $label --repo $Repo --color $color | Out-Null
        Write-Host "  Created label: $label"
    }
    else {
        Write-Host "  Label exists: $label"
    }
}

# ---------------- CREATE ISSUES ----------------
$Mapping = @{}

Write-Host "`nCreating issues..." -ForegroundColor Cyan

foreach ($item in $WorkItems) {

    $fullTitle = "[$($item.Id)] $($item.Title)"
    $stdLabels = $item.Labels | ForEach-Object { Standardize-Label $_ }

    $existingNum = Get-ExistingIssueNumber -Title $fullTitle
    if ($existingNum) {
        Write-Host "  $($item.Id): Issue #$existingNum already exists (skipping)" -ForegroundColor Yellow
        $Mapping[$item.Id] = $existingNum
        continue
    }

    $body = Build-IssueBody $item
    $tempFile = [System.IO.Path]::GetTempFileName()
    $body | Out-File -FilePath $tempFile -Encoding utf8

    $args = @("issue","create","--repo",$Repo,"--title",$fullTitle,"--body-file",$tempFile)
    foreach ($l in $stdLabels) {
        $args += "-l"
        $args += $l
    }

    $output = & gh $args 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "$($item.Id): Failed to create issue. $output"
        Remove-Item $tempFile -Force
        continue
    }

    $match = [regex]::Match($output, '/(\d+)\s*$')
    if ($match.Success) {
        $num = $match.Groups[1].Value
        gh issue close $num --repo $Repo --reason completed | Out-Null
        Write-Host "  $($item.Id): Created #$num and closed" -ForegroundColor Green
        $Mapping[$item.Id] = [int]$num
    }
    else {
        Write-Warning "$($item.Id): Could not parse issue number from output."
    }

    Remove-Item $tempFile -Force
}

# ---------------- EXPORT MAPPING ----------------
Write-Host "`n=== WF-ID to GitHub Issue Number Mapping ===" -ForegroundColor Cyan

$sorted = [ordered]@{}
$Mapping.GetEnumerator() | Sort-Object Key | ForEach-Object {
    $sorted[$_.Key] = $_.Value
    Write-Host "  $($_.Key) -> #$($_.Value)"
}

$sorted | ConvertTo-Json | Out-File "wf-to-github-issues.json" -Encoding utf8

Write-Host "`nMapping saved to wf-to-github-issues.json" -ForegroundColor Cyan
