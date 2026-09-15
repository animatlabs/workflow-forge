# WorkflowForge Polly Resilience Extension

Polly integration for WorkflowForge providing resilience patterns including circuit breakers, retries and timeouts for robust workflow execution.

## What's new in 2.2.0

- WorkflowForge **2.2.0** — see [CHANGELOG](https://github.com/animatlabs/workflow-forge/blob/main/CHANGELOG.md#220---2026-09-15) on GitHub.
- All packages ship a single **netstandard2.0** `lib` asset.
- **BREAKING:** removed `RateLimiter` / `PollyRateLimiterSettings` and `PollyTimeoutSettings.UseOptimisticTimeout`; neither was implemented. Bulkhead isolation and rate limiting are struck from the package description.
- **BREAKING:** `AddWorkflowForgePolly(configuration)` now defaults to `WorkflowForge:Extensions:Polly`; it previously defaulted to a section nothing used.
- **Fixed:** the `netstandard2.0` asset declares its four BCL dependencies again. Polly is IL-merged; **`THIRD-PARTY-NOTICES.txt`** ships in the nupkg.

## Full documentation

Guides, samples, and the long-form package readme: [WorkflowForge.Extensions.Resilience.Polly README](https://github.com/animatlabs/workflow-forge/blob/main/src/extensions/WorkflowForge.Extensions.Resilience.Polly/README.md) on GitHub.

## Third-party notices

This package IL-merges third-party libraries. See `THIRD-PARTY-NOTICES.txt` in the nupkg.

## Install

```bash
dotnet add package WorkflowForge.Extensions.Resilience.Polly
```
