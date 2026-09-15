# WorkflowForge.Testing

Testing utilities for WorkflowForge including test doubles, fakes, and testing helpers. Provides FakeWorkflowFoundry for unit testing workflow operations without requiring the full workflow infrastructure.

## What's new in 2.2.0

- WorkflowForge **2.2.0** — see [CHANGELOG](https://github.com/animatlabs/workflow-forge/blob/main/CHANGELOG.md#220---2026-09-15) on GitHub.
- All packages ship a single **netstandard2.0** `lib` asset.
- `FakeWorkflowFoundry` matches the **2.2.0** ownership rule: `Reset()` / `Dispose()` release operations and middleware without disposing them, and `Dispose()` is re-entrant.

## Full documentation

Guides, samples, and the long-form package readme: [WorkflowForge.Testing README](https://github.com/animatlabs/workflow-forge/blob/main/src/core/WorkflowForge.Testing/README.md) on GitHub.

## Install

```bash
dotnet add package WorkflowForge.Testing
```
