# WorkflowForge Performance Extension

Performance monitoring and profiling extension for WorkflowForge providing detailed metrics, execution timing, memory usage tracking, and performance optimization insights for production workflows.

## What's new in 2.2.0

- WorkflowForge **2.2.0** — see [CHANGELOG](https://github.com/animatlabs/workflow-forge/blob/main/CHANGELOG.md#220---2026-09-15) on GitHub.
- All packages ship a single **netstandard2.0** `lib` asset.
- **Fixed:** `EnablePerformanceMonitoring()` starts from fresh counters when re-enabled; it previously kept the old accumulator and discarded the new one.

## Full documentation

Guides, samples, and the long-form package readme: [WorkflowForge.Extensions.Observability.Performance README](https://github.com/animatlabs/workflow-forge/blob/main/src/extensions/WorkflowForge.Extensions.Observability.Performance/README.md) on GitHub.

## Install

```bash
dotnet add package WorkflowForge.Extensions.Observability.Performance
```
