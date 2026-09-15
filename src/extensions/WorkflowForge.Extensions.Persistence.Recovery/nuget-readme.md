# WorkflowForge Persistence Recovery Extension

Recovery orchestration for WorkflowForge persistence: resume workflows from last checkpoints, with configurable retry and hooks.

## What's new in 2.2.0

- WorkflowForge **2.2.0** — see [CHANGELOG](https://github.com/animatlabs/workflow-forge/blob/main/CHANGELOG.md#220---2026-09-15) on GitHub.
- All packages ship a single **netstandard2.0** `lib` asset.
- **BREAKING:** `RecoveryCoordinator` requires a logger: `RecoveryCoordinator(provider, logger, options?)`.
- The `foundryFactory` you pass **must** attach `PersistenceMiddleware` with the same provider; without it every completed operation re-executes on resume.

## Full documentation

Guides, samples, and the long-form package readme: [WorkflowForge.Extensions.Persistence.Recovery README](https://github.com/animatlabs/workflow-forge/blob/main/src/extensions/WorkflowForge.Extensions.Persistence.Recovery/README.md) on GitHub.

## Install

```bash
dotnet add package WorkflowForge.Extensions.Persistence.Recovery
```
