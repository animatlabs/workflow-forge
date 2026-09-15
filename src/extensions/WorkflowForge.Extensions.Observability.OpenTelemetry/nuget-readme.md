# WorkflowForge OpenTelemetry Extension

OpenTelemetry integration for WorkflowForge providing distributed tracing, metrics collection, and observability instrumentation for comprehensive workflow monitoring and debugging.

## What's new in 2.2.0

- WorkflowForge **2.2.0** — see [CHANGELOG](https://github.com/animatlabs/workflow-forge/blob/main/CHANGELOG.md#220---2026-09-15) on GitHub.
- All packages ship a single **netstandard2.0** `lib` asset.
- **BREAKING:** no longer depends on the OpenTelemetry SDK. The package declares `System.Diagnostics.DiagnosticSource 8.0.1` alone, is no longer ILRepacked, and drops the 10.0.10 floors and advisory GHSA-g94r-2vxg-569j. Your application owns the SDK and subscribes with `.AddSource(serviceName)` / `.AddMeter(serviceName)`.
- **Added:** `EnableOpenTelemetry` registers middleware that emits one span and one set of metrics per operation. `CreateOpenTelemetryWorkflowMiddleware()` parents those spans under a workflow span.
- **Fixed:** `DisableOpenTelemetry` now unregisters its middleware instead of leaving an inert copy in the pipeline, so re-enabling on the same foundry no longer runs two instances.

## Full documentation

Guides, samples, and the long-form package readme: [WorkflowForge.Extensions.Observability.OpenTelemetry README](https://github.com/animatlabs/workflow-forge/blob/main/src/extensions/WorkflowForge.Extensions.Observability.OpenTelemetry/README.md) on GitHub.

## Install

```bash
dotnet add package WorkflowForge.Extensions.Observability.OpenTelemetry
```
