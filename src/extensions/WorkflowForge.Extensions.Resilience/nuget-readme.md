# WorkflowForge Resilience Extension

Resilience and retry extension for WorkflowForge workflow engine. Provides circuit breakers, retry strategies, and timeout management for robust workflow execution.

## What's new in 2.2.0

- WorkflowForge **2.2.0** — see [CHANGELOG](https://github.com/animatlabs/workflow-forge/blob/main/CHANGELOG.md#220---2026-09-15) on GitHub.
- All packages ship a single **netstandard2.0** `lib` asset.
- **Fixed:** `RandomIntervalStrategy` synchronises its `Random` on the hot path, not just at seeding; corrupted state had silently removed the jitter the class exists to provide.

## Full documentation

Guides, samples, and the long-form package readme: [WorkflowForge.Extensions.Resilience README](https://github.com/animatlabs/workflow-forge/blob/main/src/extensions/WorkflowForge.Extensions.Resilience/README.md) on GitHub.

## Install

```bash
dotnet add package WorkflowForge.Extensions.Resilience
```
