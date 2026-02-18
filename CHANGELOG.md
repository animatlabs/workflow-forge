# Changelog

All notable changes to WorkflowForge will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [2.1.0] - 2026-02-15

### Added
- Optional `restoreAction` parameter on `WorkflowBuilder.AddOperation` overloads for inline compensation logic
- Optional `restoreFunc` parameter on `DelegateWorkflowOperation` and `WorkflowOperations` factory methods
- Optional `restoreAction` parameter on `WithOperation` foundry extension methods for inline compensation
- `FoundryPropertyKeys` constant class replacing magic string property keys (internal)
- `PerformancePropertyKeys` constant class for Performance extension property keys (internal)
- `ErrorStackTrace`, `TimingDurationTicks`, `TimingFailed`, `OperationTimeoutFormat` constants added to `FoundryPropertyKeys`
- `GetOperationOutput` / `GetOperationOutput<T>` public extension methods for orchestrator-level operation output inspection
- `CurrentOperationIndex` internal property key set by foundry before each middleware invocation
- CI/CD pipeline with GitHub Actions (build, test, pack; manual publish dispatch with artifact reuse)
- Cross-platform Python publish script (`scripts/publish-packages.py`) with `.snupkg` support
- `src/Directory.Build.props` for centralized build settings and SourceLink
- Strong-name assembly signing infrastructure
- `scripts/README.md` with SNK generation guide, signing instructions, and GitHub secrets setup
- XML doc `<remarks>` on `IWorkflowOperation` recommending `WorkflowOperationBase`

### Changed
- Compensation always attempts `RestoreAsync` on all operations (no-op base class default handles non-restorable operations)
- `WorkflowOperationBase.RestoreAsync` now returns `Task.CompletedTask` by default instead of throwing `NotSupportedException`
- `WorkflowSmith` always enters the compensation path on failure (removed workflow-level gate)
- `Workflow.Operations` now returns `ReadOnlyCollection` preventing downcast mutation
- `WorkflowBuilder.Operations` caches `ReadOnlyCollection` (no allocation on every access)
- `WorkflowSmith` clones options defensively in constructor
- Middleware options (`TimingMiddlewareOptions`, `LoggingMiddlewareOptions`, `ErrorHandlingMiddlewareOptions`) now extend `WorkflowForgeOptionsBase`
- All EventArgs classes sealed for JIT devirtualization
- All concrete exception classes sealed
- Consistent `GC.SuppressFinalize` across all `IDisposable` implementations
- `ConfigureAwait(false)` added to all library await calls in extensions
- All 103 sample and benchmark operations refactored from direct `IWorkflowOperation` to `WorkflowOperationBase`
- All magic strings in core middleware replaced with `FoundryPropertyKeys` constants
- All magic strings in Audit, Validation, and Performance extensions replaced with constants
- `FoundryPropertyKeys` and `PerformancePropertyKeys` changed from `public` to `internal` (implementation details, not for consumer coupling)
- Operation property key format changed from `Operation.{guid}.Output` to `Operation.{index}:{name}.Output` (readable, stable, collision-safe)
- `PersistenceMiddleware` rewritten to use index-based counter tracking instead of `Dictionary<Guid, int>` lookup (O(1), no dictionary)
- `PersistenceMiddleware` removed dead `#if NETSTANDARD2_0` preprocessor code (plain netstandard2.0 code only)
- Tests multi-targeted to `net48;net8.0;net10.0` for cross-framework validation
- Benchmarks (internal) multi-targeted to `net48;net8.0;net10.0` for performance timeline
- Comparative benchmarks: Elsa conditionally included on net8.0+ only (net48 compares WorkflowForge vs WorkflowCore)
- Samples multi-targeted to `net48;net8.0;net10.0`
- `PackageReleaseNotes` updated to v2.1.0 across all 13 csproj files
- CI/CD publish job downloads build artifacts instead of rebuilding (ensures tested = published)
- CI/CD workflow uploads and signs both `.nupkg` and `.snupkg` packages
- Scripts moved to `scripts/` folder
- Documentation updated to emphasize `WorkflowOperationBase` over direct `IWorkflowOperation` implementation
- All documentation examples updated to use `WorkflowOperationBase` / `ForgeAsyncCore` pattern

### Removed
- **BREAKING**: `SupportsRestore` property removed from `IWorkflowOperation` interface
- **BREAKING**: `SupportsRestore` property removed from `IWorkflow` interface
- **BREAKING**: `SupportsRestore` property removed from `WorkflowOperationBase` and all operation implementations
- All `if (!SupportsRestore) throw NotSupportedException` guards removed from `RestoreAsync` methods

### Fixed
- WorkflowTimeoutMiddleware now propagates cancellation token via foundry properties
- ConditionalWorkflowOperation `_lastConditionResult` made volatile for thread safety
- Event handler memory leaks: WorkflowSmith and WorkflowFoundry clear events on Dispose
- Recovery extension now logs exceptions instead of silently swallowing them
- WorkflowSmith Dispose race condition with concurrency limiter
- Event handler exceptions no longer conflated with operation failures in WorkflowFoundry
- PersistenceMiddleware: same-instance-added-twice no longer causes silent property key collision (index-based tracking)
- PersistenceMiddleware: removed dead `#if NETSTANDARD2_0` preprocessor code
- Serilog extension missing Version tag in csproj
- HealthCheckService.LastResults thread safety with locking
- RetryWorkflowOperation.Dispose now logs swallowed exceptions
- Filename typo: `WorkflowForgeLoggerExtesions.cs` renamed to `WorkflowForgeLoggerExtensions.cs`
- Missing guard clauses added to WorkflowOperationBase.ForgeAsync and ForEachWorkflowOperation constructor
- WorkflowSmith now disposes middleware instances on Dispose
- Closure bug in Scenario5 benchmark (loop variables `i` and `j` captured by reference in parallel tasks)
- ForEachLoopWorkflow benchmark misleading return value corrected
- Scenario2 benchmark now uses public `GetOperationOutput` API instead of internal constants
- Range/index syntax (`[^1]`) replaced with net48-compatible equivalent for .NET Framework 4.8 compatibility
- `Task.IsCompletedSuccessfully`, `Random.Shared`, `Array.Fill`, `Parallel.ForEachAsync` replaced with net48-compatible alternatives in tests and benchmarks
- `new Random()` replaced with `ThreadSafeRandom` helper across all samples and tests to prevent TickCount-collision on .NET Framework 4.8
- Samples `--validate` CLI mode added for non-interactive smoke testing across all targeted frameworks
- PersistenceMiddleware skip path now returns stored output instead of `inputData`, preventing overwrite of restored outputs during resume
- All `public const` members on internal `FoundryPropertyKeys` and `PerformancePropertyKeys` classes changed to `internal const` for consistent visibility
- PersistenceMiddleware uses `.Count` property instead of LINQ `Count()` extension on `IReadOnlyList`
- CI/CD `version` input now passed to `dotnet pack` as `-p:PackageVersion` (was previously ignored)
- Publish script summary tracker uses `startswith` matching instead of substring `in` to prevent false attribution across package names

## [2.0.0] - 2026-01-01

### Added
- Initial v2.0.0 release
