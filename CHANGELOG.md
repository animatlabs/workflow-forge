# Changelog

All notable changes to WorkflowForge will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

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
- `PublishRepositoryUrl` and `DebugType` (embedded PDB) centralized into `src/Directory.Build.props` so every package carries SourceLink metadata the same way
- Coverage reports uploaded as a separate retained artifact alongside test results for independent auditing

### Removed
- **BREAKING**: `SupportsRestore` removed from `IWorkflowOperation`, `IWorkflow`, and operation implementations
- **BREAKING**: `RestoreAsync` parameter renamed from `context` to `outputData`
- Legacy restore guards (`if (!SupportsRestore) throw NotSupportedException`) removed from operation implementations
- `RELEASE.md` removed in favor of CI/CD-driven release process and `docs/RELEASING.md`

### Fixed
- Recovery extension behavior corrected so missing snapshots fall back to fresh execution instead of short-circuiting recovery flow
- Recovery resume path now logs resume failures and proceeds through configured fresh-execution retries
- Multiple WorkflowSmith foundry pooling defects fixed (dispose-then-reuse, leak on dispose, and counter race/underflow issues)
- Persistence resume path now preserves restored operation output instead of overwriting with input data
- Cross-target compatibility and reliability fixes applied for `net48` (API compatibility replacements and deterministic random helper)
- Logging and diagnostics fixes including console template formatting, event handler lifecycle cleanup, and OpenTelemetry/Serilog correctness adjustments

## [2.0.0] - 2026-01-01

### Added
- Initial v2.0.0 release
