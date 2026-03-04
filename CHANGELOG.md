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

### Changed
- **BREAKING**: Serilog extension factory now uses `CreateLogger(ILoggerFactory)` instead of the previous `SerilogLoggerOptions` + `ILogEventSink` overload
- CI/CD moved to GitHub Actions with SonarCloud analysis, artifact reuse for publish, and package signing flow
- Core orchestration and operation infrastructure hardened: compensation path consistency, defensive options cloning, stricter disposal/event cleanup, and reduced mutable surface area
- Middleware and persistence internals streamlined with index-based operation tracking and consolidated internal key constants
- Repository-wide multi-target validation expanded to `net48`, `net8.0`, and `net10.0` across tests/samples/benchmarks

### Removed
- **BREAKING**: `SupportsRestore` removed from `IWorkflowOperation`, `IWorkflow`, and operation implementations
- **BREAKING**: `RestoreAsync` parameter renamed from `context` to `outputData`
- Legacy restore guards (`if (!SupportsRestore) throw NotSupportedException`) removed from operation implementations
- `RELEASE.md` removed in favor of CI/CD-driven release process

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
