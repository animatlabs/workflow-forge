# WorkflowForge Persistence Extension

Persistence extension for WorkflowForge enabling resumable workflows via a pluggable provider (bring your own storage). Zero-dependency core integration.

## What's new in 2.2.0

- WorkflowForge **2.2.0** — see [CHANGELOG](https://github.com/animatlabs/workflow-forge/blob/main/CHANGELOG.md#220---2026-09-15) on GitHub.
- All packages ship a single **netstandard2.0** `lib` asset.
- **BREAKING:** removed `PersistenceOptions.MaxVersions` and the duplicate `PersistenceMiddlewareOptions` type.
- **Fixed:** `PersistOnOperationComplete`, `PersistOnWorkflowComplete` and `PersistOnFailure` each now change when a checkpoint is written. `PersistOnFailure` previously wrote nothing.

## Full documentation

Guides, samples, and the long-form package readme: [WorkflowForge.Extensions.Persistence README](https://github.com/animatlabs/workflow-forge/blob/main/src/extensions/WorkflowForge.Extensions.Persistence/README.md) on GitHub.

## Install

```bash
dotnet add package WorkflowForge.Extensions.Persistence
```
