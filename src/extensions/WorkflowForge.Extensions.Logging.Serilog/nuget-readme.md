# WorkflowForge Serilog Extension

Serilog adapter for WorkflowForge providing professional structured logging capabilities with rich context and correlation.

## What's new in 2.2.0

- WorkflowForge **2.2.0** — see [CHANGELOG](https://github.com/animatlabs/workflow-forge/blob/main/CHANGELOG.md#220---2026-09-15) on GitHub.
- All packages ship a single **netstandard2.0** `lib` asset.
- **BREAKING:** removed `SerilogLoggerFactory.CreateLogger(ILogger)`; its parameter was an internalized Serilog type, so it was not callable by consumers.
- **Fixed:** structured properties and scopes reached no sink — the embedded pipeline never enabled `Enrich.FromLogContext()`.
- Dropped the unused `Serilog.Extensions.Logging` reference, removing a 10.0.10 dependency floor. Serilog is still IL-merged; **`THIRD-PARTY-NOTICES.txt`** ships in the nupkg.

## Full documentation

Guides, samples, and the long-form package readme: [WorkflowForge.Extensions.Logging.Serilog README](https://github.com/animatlabs/workflow-forge/blob/main/src/extensions/WorkflowForge.Extensions.Logging.Serilog/README.md) on GitHub.

## Third-party notices

This package IL-merges third-party libraries. See `THIRD-PARTY-NOTICES.txt` in the nupkg.

## Install

```bash
dotnet add package WorkflowForge.Extensions.Logging.Serilog
```
