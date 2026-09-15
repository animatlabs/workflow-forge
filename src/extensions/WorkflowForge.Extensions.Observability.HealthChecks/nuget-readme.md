# WorkflowForge Health Checks Extension

Health monitoring and diagnostics extension for WorkflowForge providing comprehensive health checks, dependency monitoring, and system status reporting for production workflows.

## What's new in 2.2.0

- WorkflowForge **2.2.0** — see [CHANGELOG](https://github.com/animatlabs/workflow-forge/blob/main/CHANGELOG.md#220---2026-09-15) on GitHub.
- All packages ship a single **netstandard2.0** `lib` asset.
- **Fixed:** periodic checks no longer stack up when a check outlasts the interval, and `CreateHealthCheckService` registers the service on `foundry.Services` so its timer stops with the lease.

## Full documentation

Guides, samples, and the long-form package readme: [WorkflowForge.Extensions.Observability.HealthChecks README](https://github.com/animatlabs/workflow-forge/blob/main/src/extensions/WorkflowForge.Extensions.Observability.HealthChecks/README.md) on GitHub.

## Install

```bash
dotnet add package WorkflowForge.Extensions.Observability.HealthChecks
```
