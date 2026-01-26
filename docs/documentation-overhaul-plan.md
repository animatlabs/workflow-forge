# Documentation Overhaul Plan

This plan captures the documentation updates needed after the core
feature changes. It is intentionally separate from implementation tasks
and can be executed independently.

## Goals
- Align all docs with current public APIs and behaviors.
- Make key concepts discoverable for new users.
- Ensure extension and sample docs match current configuration options.

## Scope
### Repository-wide Markdown
- All `*.md` files across the codebase, including `docs/`, `src/`, `performance/`, and root docs.

### Core Documentation
- `src/core/WorkflowForge/README.md`
- `docs/core/configuration.md`
- `docs/core/events.md`
- `docs/core/operations.md`
- `docs/architecture/overview.md`
- `docs/architecture/middleware-pipeline.md`
- `docs/reference/api-reference.md`

### Extensions Documentation
- `docs/extensions/index.md`
- All extension `README.md` files under `src/extensions/`

### Getting Started
- `docs/getting-started/getting-started.md`
- `docs/getting-started/samples-guide.md`
- `src/samples/README.md`

### Performance and Benchmarks
- `docs/performance/performance.md`
- `docs/performance/competitive-analysis.md`
- `docs/_includes/benchmark-data.md` (if benchmarks are updated)

## Required Content Updates
### Core API and Behavior Changes
- `IsFrozen` property and immutability during execution.
- `ForgeAsync` on `IWorkflowFoundry`.
- `ReplaceOperations` method behavior and usage.
- `EnableOutputChaining` option.
- `WorkflowForgeOptions` inheritance from `WorkflowForgeOptionsBase`.
- `IAsyncDisposable` support and `await using` guidance.
- ILRepack dependency isolation (not Costura.Fody).
- DataAnnotations validation (not FluentValidation).
- Manual API reference updates until autogen exists.

### Dependency Injection Integration
- `IValidateOptions` validator behavior and startup validation.
- Options binding examples updated to current defaults.

### Extensions and Middleware
- Updated middleware setup in extensions.
- Any renamed extension file references.

## Deliverables
1. Updated markdown files for all scope items.
2. Verified samples that match the documented APIs.
3. Consistent terminology across core, extensions, and getting started.
4. Changelog entry (if required by project conventions).

## Validation Checklist
- All public API references exist and match current signatures.
- Configuration samples match the current options defaults.
- Cross-links between docs resolve and are accurate.
- No references to deprecated packages or behaviors.
- Performance numbers updated after rerun (if benchmarks are refreshed).

## Execution Order
1. Update core README and configuration docs.
2. Update architecture and API reference.
3. Update extension docs and DI integration notes.
4. Update getting started and sample guides.
5. Refresh performance/benchmark docs after rerun.

## Open Questions
- Should API reference be auto-generated or manually maintained?
- Is a docs site rebuild required after updates?
