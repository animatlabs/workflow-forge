# Changelog

All notable changes to WorkflowForge will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [2.2.0] - 2026-09-15

### Added
- **`IFoundryServices` / `Services`** on `IWorkflowExecutionContext` for foundry-scoped extension
  resources with dispose-on-lease-end semantics. `IFoundryServices` also exposes `DisposeAll()`.
- **`WorkflowForgeLoggerBase`** — optional public base class for `IWorkflowForgeLogger`
  implementations. It supplies a `MinimumLevel`-driven `IsEnabled`, and is the migration path for
  the new `IsEnabled` requirement.
- **OpenTelemetry auto-instrumentation.** `EnableOpenTelemetry` now registers
  `OpenTelemetryOperationMiddleware`, which creates one span and one set of metrics per operation.
  `CreateOpenTelemetryWorkflowMiddleware()` returns the workflow-level middleware that parents
  those spans. No instrumentation code is needed in operations.
- `docs/core/foundry-lifetime.md` and DocFX scaffold under `docfx/`.
- Central package management via `src/Directory.Packages.props`.
- Linux CI job (`build-linux`) for `net8.0` and `net10.0` on Ubuntu.
- Four samples (34–37): workflow timeout, OpenTelemetry auto-instrumentation, audit detail levels,
  and persistence triggers.

### Breaking changes
- **`IWorkflowExecutionContext.Services`** is a new interface member. Any third-party
  `IWorkflowFoundry` / `IWorkflowExecutionContext` implementation must expose
  `IFoundryServices Services`; return `new FoundryServices()` per context.
- **`IWorkflowForgeLogger.IsEnabled(WorkflowForgeLogLevel)`** is a new interface member. Custom
  loggers must implement it. Derive from `WorkflowForgeLoggerBase` to inherit an implementation.
- **`IFoundryServices.DisposeAll()`** is a new interface member.
- **The foundry disposes only what it owns.** `Reset()` and `Dispose()` no longer dispose the
  operations and middleware you pass to `AddOperation`, `ReplaceOperations`, `AddMiddleware` or
  `AddMiddlewares`; they release the references only. Register anything the foundry should tear
  down on `Services`. This fixes a `using`-scoped smith disposing an `IWorkflow`'s operations and a
  pooled foundry destroying shared middleware such as a circuit breaker after one run.
- **Removed `WorkflowForgeLoggers`** static façade (`Null`, `Console`). The type shipped in
  2.0.0–2.1.2 but was never documented; use `ConsoleLogger`, host DI, or Serilog/MEL instead
  (`CreateSmith()` already defaults to no-op logging).
- **Removed public `PropertyNameConstants` and `WorkflowLogMessageConstants`** (now internal).
- **Removed `SerilogLoggerFactory.CreateLogger(ILogger)`.** Its parameter was an internalized
  Serilog type, so the overload was not callable from outside the assembly. Use
  `CreateLogger(SerilogLoggerOptions)` or `CreateLogger(ILoggerFactory)`.
- **Removed `PersistenceOptions.MaxVersions`** and the duplicate `PersistenceMiddlewareOptions`
  type. `IWorkflowPersistenceProvider` stores one snapshot per key, so versioning was not
  implementable; the duplicate type bound the same configuration section with different defaults.
- **Removed `PollyMiddlewareOptions.RateLimiter`, `PollyRateLimiterSettings` and
  `PollyTimeoutSettings.UseOptimisticTimeout`.** Rate limiting was validated and cloned but never
  built into a pipeline, and Polly v8 timeouts are always cooperative. Bulkhead isolation and rate
  limiting have been struck from the package description and docs; neither was implemented.
- **`RecoveryCoordinator`** now requires a logger: `RecoveryCoordinator(provider, logger, options?)`.
  `ResumeAllAsync` continues past snapshots it cannot resume, and the logger is the only record.
- **`InMemoryAuditProvider(int maxEntries)`** throws `ArgumentOutOfRangeException` for values below
  one instead of silently substituting 10,000.

### Fixed
- **Retried operations no longer bypass inner middleware.** The operation pipeline advanced a cursor
  stored on a per-foundry state object, so a second `next()` call from `RetryMiddleware` or
  `CircuitBreakerMiddleware` entered with the cursor exhausted and invoked the operation directly —
  skipping timeout, logging, validation and persistence on every retried attempt. The cursor is now
  per-invocation.
- **`LoggingMiddleware` is no longer inert under default options.** Its whole body, including the
  failure `LogError`, was gated on `Trace` while `MinimumLevel` defaults to `Information`, so
  `UseLogging()` could never emit a line. The logging scope and the error path are now
  unconditional; `MinimumLevel` gates only the per-operation trace messages.
- **`WorkflowTimeoutMiddleware` now actually cancels the workflow.** Its timeout token reached
  nobody, so a 5s timeout on a 10-minute workflow threw after 10 minutes. `WorkflowSmith` now links
  the published timeout token into the token it passes to `foundry.ForgeAsync`.
- **Serilog structured properties and scopes reached no sink.** The embedded pipeline never called
  `Enrich.FromLogContext()`, so everything pushed through `LogContext` was dropped.
- **ILRepack now runs for every shipped asset.** Extensions had been multi-targeted to
  `netstandard2.0;net8.0;net10.0` while the merge scripts were gated to `netstandard2.0`, so the
  `net8.0` and `net10.0` assets shipped referencing Polly/Serilog/OpenTelemetry assemblies that were
  neither merged in nor declared as dependencies — a `FileNotFoundException` on first use. All
  extensions ship a single `netstandard2.0` asset again.
- **The `netstandard2.0` Polly asset declares its four BCL dependencies again.** Inverted conditions
  plus a shim exclusion had dropped `Microsoft.Bcl.TimeProvider`, `Microsoft.Bcl.AsyncInterfaces`,
  `System.ComponentModel.Annotations` and `System.Threading.Tasks.Extensions`, reintroducing the
  v2.1.2 defect.
- `FoundryServices.Set` is now atomic. Two concurrent calls could both dispose the same previous
  value and leave one new value neither stored nor disposed. `DisposeAll` drains instead of
  iterate-then-clear, and a `Dispose()` that throws is now logged instead of silently swallowed.
- `ForgeAsync(workflow, data)` no longer clears the caller's dictionary on return, so results are
  readable after the call.
- `EnablePerformanceMonitoring()` starts from fresh counters when re-enabled; it had kept the
  previous accumulator and discarded the new one.
- `EnableOpenTelemetry` is idempotent; it previously built a second instrument graph and disposed
  the first, silently stopping instruments already handed out.
- `DisableOpenTelemetry` now unregisters its operation middleware instead of leaving it in the
  pipeline; a later `EnableOpenTelemetry` no longer accumulates a second, inert middleware
  instance.
- `HealthCheckService` periodic checks no longer stack up when a check outlasts the interval, and
  `CreateHealthCheckService` registers the service on `foundry.Services` so its timer stops even if
  the caller forgets to dispose it.
- `RandomIntervalStrategy` synchronises the `Random` on the hot path, not just at seeding.
- `WorkflowFoundry` operation-tracking fields are read and written through `Volatile`/lock; the
  `Guid` in particular drives compensation targeting and was not read atomically.
- `InMemoryAuditProvider` evicts through a queue instead of `List.RemoveAt(0)`, which was an O(n)
  memmove on every write once at cap.
- `FakeWorkflowFoundry.Dispose()` is re-entrant.
- `WorkflowSmith` no longer clones its options twice per pooled reset.
- SourceLink supplied by the .NET SDK (removed the explicit `Microsoft.SourceLink.GitHub` reference
  that pulled vulnerable build tasks into pack).
- Operation event handlers cleared on pool reset; options cloned on reset.
- Skip copying `Properties` when `WorkflowCompleted` has no subscribers.
- Health check samples dispose `HealthCheckService`; the periodic sample awaits its monitor task
  instead of racing it against disposal.
- Comparative benchmarks scenarios 7–8 move DI setup to `SetupAsync()` for fair timing.

### Changed
- **Benchmark documentation** refreshed from September 2026 BenchmarkDotNet runs (10 iterations
  per job): competitive analysis, internal benchmarks, `docs/_includes/benchmark-data.md`, and
  summary pages aligned to the latest harness output.
- **Options that existed but did nothing are now wired.** `PersistOnOperationComplete`,
  `PersistOnWorkflowComplete` and `PersistOnFailure` each change when a checkpoint is written;
  `AuditDetailLevel` values are now distinct and `LogDataPayloads` captures input and output;
  `EnableTracing`, `EnableMetrics`, `EnableSystemMetrics` and `EnableOperationMetrics` all take
  effect; the Polly pipeline is composed from whichever of `Retry`, `Timeout` and `CircuitBreaker`
  is enabled, honouring `BackoffType`, `UseJitter`, `SamplingDuration` and `MinimumThroughput`;
  `DefaultTags` and `EnableDetailedLogging` are applied by `PollyMiddleware`.
- **`AddWorkflowForgePolly(configuration)`** defaults to `PollyMiddlewareOptions.DefaultSectionName`
  (`WorkflowForge:Extensions:Polly`). It previously defaulted to `WorkflowForge:Polly`, which no
  sample or document used, so the call silently bound defaults.
- **DI options validation is fail-fast.** `AddWorkflowForge` now calls `ValidateOnStart()`, so on a
  generic host bad configuration fails at startup rather than on first `IOptions<T>.Value`
  resolution. Adds a `Microsoft.Extensions.Hosting.Abstractions` reference to the DI extension.
- **`Observability.OpenTelemetry` no longer depends on the OpenTelemetry SDK.** It referenced no
  OpenTelemetry type; the packages are removed along with their 10.0.10 floors and
  GHSA-g94r-2vxg-569j. The package now declares `System.Diagnostics.DiagnosticSource 8.0.1` alone
  and is no longer an ILRepack package. Consumers own the SDK and subscribe with
  `.AddSource(serviceName)` / `.AddMeter(serviceName)`.
- **`Logging.Serilog` drops the unused `Serilog.Extensions.Logging` reference**, removing the last
  10.0.10 floor. Its graph is now `Microsoft.Extensions.Logging.Abstractions 8.0.2`,
  `System.Diagnostics.DiagnosticSource 8.0.1`, `System.Threading.Channels 8.0.0`.
- Shipped libraries remain **`netstandard2.0`** only.
- Clarified `Properties` vs pooled reset lifecycle, and foundry ownership, in XML documentation.

### Testing and CI
- Packaging guards now assert something. A new `PackageClosureShould` test packs the solution and
  reads the real `AssemblyRef` table of each shipped assembly, failing when a reference is neither
  merged in nor declared in the nuspec. The smoke test installs **and runs** the Polly and Serilog
  packages from a local feed. `release-pack` gained `needs: [build-linux, build-windows-net48]`.
- Added behaviour tests for every option wired above, plus resource-lifetime tests (a `WeakReference`
  across `Reset()`, a timer-holding service proven to stop firing, a throwing `Dispose()` proven to
  be reported).

### Migration
- See [Foundry lifetime](docs/core/foundry-lifetime.md) for `IFoundryServices` / `Services`, custom
  `IWorkflowFoundry` implementers, and the ownership rule.
- Custom `IWorkflowForgeLogger` implementations: derive from `WorkflowForgeLoggerBase`.
- Anything relying on the foundry disposing operations or middleware must now dispose them itself,
  or register them on `Services`.

## [2.1.2] - 2026-07-21

### Fixed
- **Resilience.Polly**: declared Polly's transitive dependencies (`Microsoft.Bcl.TimeProvider`, `Microsoft.Bcl.AsyncInterfaces`, `System.ComponentModel.Annotations`, `System.Threading.Tasks.Extensions`, and the `Microsoft.Extensions.DependencyInjection`/`Configuration` abstractions) so they flow to consumers. Previously the ILRepack-merged Polly referenced `Microsoft.Bcl.TimeProvider` but it was never declared in the package, causing a runtime `FileNotFoundException` (e.g. calling `UsePollyComprehensive` on .NET 8+).
- **Observability.OpenTelemetry** and **Logging.Serilog**: fixed the same class of missing transitive dependency (`Microsoft.Extensions.*`, `System.Diagnostics.DiagnosticSource`, `System.Threading.Channels`) for their ILRepack-merged libraries.
- **Observability.Performance**: `EnablePerformanceMonitoring()` / `GetPerformanceStatistics()` now work on standard foundries. Added `FoundryPerformanceStatistics`, `OperationStatistics`, and `PerformanceStatisticsMiddleware`; previously the API was a no-op (it required an interface nothing implemented) and always returned `null`.
- **Core**: pooled foundries no longer leak `Properties` between executions; `WorkflowFoundry.Reset()` now clears per-execution state, preventing a stale operation index from crashing compensation on a subsequent (smaller) workflow.
- **Audit**: audit entries now record the real workflow name from the foundry's current workflow instead of always `"Unknown"`.
- **Core**: lifecycle-event subscribers throwing no longer mask the workflow's real exception or abort compensation mid-loop; `WorkflowSmith.Dispose()` iterates middleware under its lock; a check-then-act race that could leak a foundry on concurrent dispose is closed.
- Guarded the ILRepack extensions so packing outside `Release` fails fast instead of shipping an unmerged package.
- **Persistence**: suppressed the SonarAnalyzer `S4790` build error on the SHA1 used to derive snapshot keys (scoped `#pragma` with rationale); SHA1 here is a non-cryptographic key digest, and changing it would break existing persisted snapshot keys.

### Changed
- Package version and shared package metadata (`Copyright`, `PackageReleaseNotes`) are centralized in `src/Directory.Build.props`; all packages release under one version. The release workflow now overrides both `Version` and `PackageVersion` so assembly and package versions stay in lockstep.
- Documentation corrected: performance-monitoring examples, false "dependency-free" claims on the Performance/Resilience extensions, and broken configuration-guide anchor links. Added `docs/RELEASING.md`.

## [2.1.1] - 2026-03-07

### Fixed
- Fixed NuGet symbol packages (.snupkg) containing no .pdb files due to `DebugType` override conflict; all projects now correctly inherit `portable` from `Directory.Build.props`
- Fixed `actions/attest-build-provenance` SHA typo in CI workflow
- Removed duplicate `.snupkg` push loop from CI publish step

### Changed
- `DebugType` property removed from individual .csproj files; centralized in `src/Directory.Build.props` as `portable`

## [2.1.0] - 2026-02-15

### Added
- Inline compensation support via optional recovery delegates (`restoreAction` / `restoreFunc`) across workflow builder and operation factory APIs
- Public operation output inspection APIs: `GetOperationOutput` and `GetOperationOutput<T>`
- Centralized build/test settings with `Directory.Build.props` and strong-name signing infrastructure
- New/reworked extension test coverage across Persistence Recovery, Resilience, HealthChecks, OpenTelemetry, Serilog, and core logger/middleware/orchestration paths
- `SerilogLoggerFactory.CreateLogger(ILoggerFactory)` bridge to integrate WorkflowForge logging with the host `Microsoft.Extensions.Logging` pipeline
- Comparative benchmarks now include .NET Framework 4.8 runtime alongside .NET 8.0 and .NET 10.0, producing full cross-runtime comparison graphs for all 12 scenarios (WorkflowForge vs WorkflowCore; Elsa skipped on net48)
- GitHub Actions build provenance attestation for `.nupkg`, `.snupkg`, and CycloneDX SBOM artifacts via Sigstore (`actions/attest-build-provenance`)
- NuGet dependency vulnerability auditing (`NuGetAudit`) across all direct and transitive dependencies on every restore; any known CVE fails the build
- Dependabot configuration for automated weekly NuGet and GitHub Actions dependency update PRs
- SDK version pinning via `global.json` to prevent CI/local SDK drift
- Release process documentation (`docs/RELEASING.md`) covering prerequisites, checklist, pipeline walkthrough, attestation verification, rollback, and future signing options
- `CODE_OF_CONDUCT.md` (Contributor Covenant v2.1)

### Changed
- **Serilog extension**: Added `CreateLogger(ILoggerFactory)` overload for host MEL integration; `CreateLogger(SerilogLoggerOptions?)` remains available
- CI/CD moved to GitHub Actions with SonarCloud analysis, artifact reuse for publish, and package signing flow
- Core orchestration and operations: aligned compensation paths, defensive options cloning, stricter disposal and event cleanup, smaller mutable surface
- Middleware and persistence internals simplified with index-based operation tracking and consolidated internal key constants
- Repository-wide multi-target validation expanded to `net48`, `net8.0`, and `net10.0` across tests/samples/benchmarks
- All GitHub Actions in the CI/CD pipeline pinned to immutable commit SHAs
- Publish job now protected by a `nuget-publish` GitHub Environment requiring human approval before any push to NuGet.org
- `PublishRepositoryUrl` and `DebugType` (`portable`) centralized into `src/Directory.Build.props` so every package carries SourceLink metadata the same way
- Coverage reports uploaded as a separate retained artifact alongside test results for independent auditing

### Removed
- **BREAKING**: `SupportsRestore` removed from `IWorkflowOperation`, `IWorkflow`, and operation implementations
- **BREAKING**: `RestoreAsync` parameter renamed from `context` to `outputData`
