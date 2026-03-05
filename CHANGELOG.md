# Changelog

All notable changes to WorkflowForge will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [2.1.0] - 2026-02-15

### Added
- Inline compensation support via optional recovery delegates (`restoreAction` / `restoreFunc`) across workflow builder and operation factory APIs
- Public operation output inspection APIs: `GetOperationOutput` and `GetOperationOutput<T>`
- Centralized build/test settings with `Directory.Build.props` and strong-name signing infrastructure
- New/reworked extension test coverage across Persistence Recovery, Resilience, HealthChecks, OpenTelemetry, Serilog, and core logger/middleware/orchestration paths
- `SerilogLoggerFactory.CreateLogger(ILoggerFactory)` bridge to integrate WorkflowForge logging with the host `Microsoft.Extensions.Logging` pipeline
- Comparative benchmarks now include .NET Framework 4.8 runtime alongside .NET 8.0 and .NET 10.0, producing full cross-runtime comparison graphs for all 12 scenarios (WorkflowForge vs WorkflowCore; Elsa skipped on net48)
- GitHub Actions build provenance attestation for `.nupkg`, `.snupkg`, and CycloneDX SBOM artifacts via Sigstore (`actions/attest-build-provenance`)
- NuGet dependency vulnerability auditing (`NuGetAudit`) across all direct and transitive dependencies on every restore — any known CVE fails the build
- Dependabot configuration for automated weekly NuGet and GitHub Actions dependency update PRs
- SDK version pinning via `global.json` to prevent CI/local SDK drift
- Release process documentation (`docs/RELEASING.md`) covering prerequisites, checklist, pipeline walkthrough, attestation verification, rollback, and future signing options
- `CODE_OF_CONDUCT.md` (Contributor Covenant v2.1)

### Changed
- **BREAKING**: Serilog extension factory now uses `CreateLogger(ILoggerFactory)` instead of the previous `SerilogLoggerOptions` + `ILogEventSink` overload
- CI/CD moved to GitHub Actions with SonarCloud analysis, artifact reuse for publish, and package signing flow
- Core orchestration and operation infrastructure hardened: compensation path consistency, defensive options cloning, stricter disposal/event cleanup, and reduced mutable surface area
- Middleware and persistence internals streamlined with index-based operation tracking and consolidated internal key constants
- Repository-wide multi-target validation expanded to `net48`, `net8.0`, and `net10.0` across tests/samples/benchmarks
- All GitHub Actions in the CI/CD pipeline pinned to immutable commit SHAs (supply-chain integrity hardening)
- Publish job now protected by a `nuget-publish` GitHub Environment requiring human approval before any push to NuGet.org
- `PublishRepositoryUrl` and `DebugType` (embedded PDB) centralized into `src/Directory.Build.props`, ensuring all 13 packages uniformly carry SourceLink metadata
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
