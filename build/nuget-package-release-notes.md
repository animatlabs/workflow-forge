# _default_
- WorkflowForge **2.2.0** — see [CHANGELOG](https://github.com/animatlabs/workflow-forge/blob/main/CHANGELOG.md#220---2026-09-15) on GitHub.
- All packages ship a single **netstandard2.0** `lib` asset.

# WorkflowForge
- **BREAKING:** `IWorkflowExecutionContext.Services` (`IFoundryServices`) is a new interface member. Custom `IWorkflowFoundry` / `IWorkflowExecutionContext` implementations must expose it; return `new FoundryServices()`.
- **BREAKING:** `IWorkflowForgeLogger.IsEnabled(WorkflowForgeLogLevel)` is a new interface member. Derive from the new **`WorkflowForgeLoggerBase`** to inherit an implementation.
- **BREAKING:** the foundry now disposes **only what it owns**. `Reset()` / `Dispose()` no longer dispose caller-supplied operations or middleware; register foundry-owned disposables on `Services`.
- **BREAKING:** removed the undocumented **`WorkflowForgeLoggers`** façade; use `ConsoleLogger`, DI, or Serilog/MEL.
- **BREAKING:** **`PropertyNameConstants`** and **`WorkflowLogMessageConstants`** are internal.
- **Fixed:** retried operations no longer bypass inner middleware — timeout, logging, validation and persistence were all skipped on every retried attempt.
- **Fixed:** `LoggingMiddleware` is no longer inert under default options; `UseLogging()` emits again and failures are always logged.
- **Fixed:** `WorkflowTimeoutMiddleware` now cancels the workflow instead of waiting for it to finish.
- [Migration guide](https://github.com/animatlabs/workflow-forge/blob/main/docs/core/foundry-lifetime.md)

# WorkflowForge.Testing
- `FakeWorkflowFoundry` matches the **2.2.0** ownership rule: `Reset()` / `Dispose()` release operations and middleware without disposing them, and `Dispose()` is re-entrant.

# WorkflowForge.Extensions.Observability.OpenTelemetry
- **BREAKING:** no longer depends on the OpenTelemetry SDK. The package declares `System.Diagnostics.DiagnosticSource 8.0.1` alone, is no longer ILRepacked, and drops the 10.0.10 floors and advisory GHSA-g94r-2vxg-569j. Your application owns the SDK and subscribes with `.AddSource(serviceName)` / `.AddMeter(serviceName)`.
- **Added:** `EnableOpenTelemetry` registers middleware that emits one span and one set of metrics per operation. `CreateOpenTelemetryWorkflowMiddleware()` parents those spans under a workflow span.
- **Fixed:** `DisableOpenTelemetry` now unregisters its middleware instead of leaving an inert copy in the pipeline, so re-enabling on the same foundry no longer runs two instances.

# WorkflowForge.Extensions.Observability.Performance
- **Fixed:** `EnablePerformanceMonitoring()` starts from fresh counters when re-enabled; it previously kept the old accumulator and discarded the new one.

# WorkflowForge.Extensions.Observability.HealthChecks
- **Fixed:** periodic checks no longer stack up when a check outlasts the interval, and `CreateHealthCheckService` registers the service on `foundry.Services` so its timer stops with the lease.

# WorkflowForge.Extensions.Logging.Serilog
- **BREAKING:** removed `SerilogLoggerFactory.CreateLogger(ILogger)`; its parameter was an internalized Serilog type, so it was not callable by consumers.
- **Fixed:** structured properties and scopes reached no sink — the embedded pipeline never enabled `Enrich.FromLogContext()`.
- Dropped the unused `Serilog.Extensions.Logging` reference, removing a 10.0.10 dependency floor. Serilog is still IL-merged; **`THIRD-PARTY-NOTICES.txt`** ships in the nupkg.

# WorkflowForge.Extensions.Resilience.Polly
- **BREAKING:** removed `RateLimiter` / `PollyRateLimiterSettings` and `PollyTimeoutSettings.UseOptimisticTimeout`; neither was implemented. Bulkhead isolation and rate limiting are struck from the package description.
- **BREAKING:** `AddWorkflowForgePolly(configuration)` now defaults to `WorkflowForge:Extensions:Polly`; it previously defaulted to a section nothing used.
- **Fixed:** the `netstandard2.0` asset declares its four BCL dependencies again. Polly is IL-merged; **`THIRD-PARTY-NOTICES.txt`** ships in the nupkg.

# WorkflowForge.Extensions.Persistence
- **BREAKING:** removed `PersistenceOptions.MaxVersions` and the duplicate `PersistenceMiddlewareOptions` type.
- **Fixed:** `PersistOnOperationComplete`, `PersistOnWorkflowComplete` and `PersistOnFailure` each now change when a checkpoint is written. `PersistOnFailure` previously wrote nothing.

# WorkflowForge.Extensions.Persistence.Recovery
- **BREAKING:** `RecoveryCoordinator` requires a logger: `RecoveryCoordinator(provider, logger, options?)`.
- The `foundryFactory` you pass **must** attach `PersistenceMiddleware` with the same provider; without it every completed operation re-executes on resume.

# WorkflowForge.Extensions.DependencyInjection
- **Changed:** options validation is fail-fast. `AddWorkflowForge` calls `ValidateOnStart()`, so on a .NET generic host invalid configuration fails at startup. Adds a `Microsoft.Extensions.Hosting.Abstractions` dependency.

# WorkflowForge.Extensions.Audit
- **BREAKING:** `InMemoryAuditProvider(int maxEntries)` throws for values below one instead of silently substituting 10,000.
- **Fixed:** `AuditDetailLevel` values are now distinct, and `LogDataPayloads` captures operation input and output.

# WorkflowForge.Extensions.Validation
- Compatible with the **2.2.0** execution context and foundry ownership rule.

# WorkflowForge.Extensions.Resilience
- **Fixed:** `RandomIntervalStrategy` synchronises its `Random` on the hot path, not just at seeding; corrupted state had silently removed the jitter the class exists to provide.
