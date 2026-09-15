# WorkflowForge Audit Extension

Comprehensive audit logging extension for WorkflowForge providing detailed tracking of workflow execution, data changes, and compliance reporting.

## What's new in 2.2.0

- WorkflowForge **2.2.0** — see [CHANGELOG](https://github.com/animatlabs/workflow-forge/blob/main/CHANGELOG.md#220---2026-09-15) on GitHub.
- All packages ship a single **netstandard2.0** `lib` asset.
- **BREAKING:** `InMemoryAuditProvider(int maxEntries)` throws for values below one instead of silently substituting 10,000.
- **Fixed:** `AuditDetailLevel` values are now distinct, and `LogDataPayloads` captures operation input and output.

## Full documentation

Guides, samples, and the long-form package readme: [WorkflowForge.Extensions.Audit README](https://github.com/animatlabs/workflow-forge/blob/main/src/extensions/WorkflowForge.Extensions.Audit/README.md) on GitHub.

## Install

```bash
dotnet add package WorkflowForge.Extensions.Audit
```
