# WorkflowForge

A powerful, extensible workflow orchestration framework for .NET applications. WorkflowForge enables you to build, execute, and manage complex workflows with support for dependency injection, logging, resilience patterns, and comprehensive observability.

## What's new in 2.2.0

- WorkflowForge **2.2.0** — see [CHANGELOG](https://github.com/animatlabs/workflow-forge/blob/main/CHANGELOG.md#220---2026-09-15) on GitHub.
- All packages ship a single **netstandard2.0** `lib` asset.
- **BREAKING:** `IWorkflowExecutionContext.Services` (`IFoundryServices`) is a new interface member. Custom `IWorkflowFoundry` / `IWorkflowExecutionContext` implementations must expose it; return `new FoundryServices()`.
- **BREAKING:** `IWorkflowForgeLogger.IsEnabled(WorkflowForgeLogLevel)` is a new interface member. Derive from the new **`WorkflowForgeLoggerBase`** to inherit an implementation.
- **BREAKING:** the foundry now disposes **only what it owns**. `Reset()` / `Dispose()` no longer dispose caller-supplied operations or middleware; register foundry-owned disposables on `Services`.
- **BREAKING:** removed the undocumented **`WorkflowForgeLoggers`** façade; use `ConsoleLogger`, DI, or Serilog/MEL.
- **BREAKING:** **`PropertyNameConstants`** and **`WorkflowLogMessageConstants`** are internal.
- **Fixed:** retried operations no longer bypass inner middleware — timeout, logging, validation and persistence were all skipped on every retried attempt.
- **Fixed:** `LoggingMiddleware` is no longer inert under default options; `UseLogging()` emits again and failures are always logged.
- **Fixed:** `WorkflowTimeoutMiddleware` now cancels the workflow instead of waiting for it to finish.
- [Migration guide](https://github.com/animatlabs/workflow-forge/blob/main/docs/core/foundry-lifetime.md)

## Full documentation

Guides, samples, and the long-form package readme: [WorkflowForge README](https://github.com/animatlabs/workflow-forge/blob/main/src/core/WorkflowForge/README.md) on GitHub.

## Install

```bash
dotnet add package WorkflowForge
```
