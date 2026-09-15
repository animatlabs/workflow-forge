# WorkflowForge Dependency Injection Extension

Microsoft.Extensions.DependencyInjection integration for WorkflowForge, providing IOptions pattern support, automatic validation, and seamless ASP.NET Core integration.

## What's new in 2.2.0

- WorkflowForge **2.2.0** — see [CHANGELOG](https://github.com/animatlabs/workflow-forge/blob/main/CHANGELOG.md#220---2026-09-15) on GitHub.
- All packages ship a single **netstandard2.0** `lib` asset.
- **Changed:** options validation is fail-fast. `AddWorkflowForge` calls `ValidateOnStart()`, so on a .NET generic host invalid configuration fails at startup. Adds a `Microsoft.Extensions.Hosting.Abstractions` dependency.

## Full documentation

Guides, samples, and the long-form package readme: [WorkflowForge.Extensions.DependencyInjection README](https://github.com/animatlabs/workflow-forge/blob/main/src/extensions/WorkflowForge.Extensions.DependencyInjection/README.md) on GitHub.

## Install

```bash
dotnet add package WorkflowForge.Extensions.DependencyInjection
```
