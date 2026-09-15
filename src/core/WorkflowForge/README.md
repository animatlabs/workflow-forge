# WorkflowForge Core

**Zero-dependency workflow orchestration framework for .NET**

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.svg)](https://www.nuget.org/packages/WorkflowForge/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/animatlabs/workflow-forge/blob/main/LICENSE)

## Overview

Foundational orchestration with **zero external NuGet dependencies** and the forge / foundry / smith metaphor: fast runs, saga-style compensation, middleware you control.

- **Zero deps + platform:** no extra packages; **.NET Standard 2.0** (.NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+)
- **Speed, flow, builder:** sub-20μs ops when tuned; thread-safe `foundry.Properties`; `CreateWorkflow(...).AddOperation(...).Build()`
- **Saga, middleware, contracts, events:** `RestoreAsync`; Russian Doll middleware; optional `IWorkflowOperation<TIn, TOut>` and `EnableOutputChaining`; lifecycle hooks

## Quick start

```bash
dotnet add package WorkflowForge  # .NET Standard 2.0+
```

Inline delegates: [Operations](https://animatlabs.com/workflow-forge/core/operations/).

```csharp
using WorkflowForge;
using WorkflowForge.Extensions;

// Create workflow
var workflow = WorkflowForge.CreateWorkflow("OrderProcessing")
    .AddOperation(new ValidateOrderOperation())
    .AddOperation(new ChargePaymentOperation())
    .AddOperation(new ReserveInventoryOperation())
    .AddOperation(new CreateShipmentOperation())
    .Build();

// Create execution environment
using var foundry = WorkflowForge.CreateFoundry("Order-12345");
foundry.SetProperty("OrderId", "12345");
foundry.SetProperty("CustomerId", "CUST-001");

// Execute workflow
using var smith = WorkflowForge.CreateSmith();
await smith.ForgeAsync(workflow, foundry);

// Read results
var shipmentId = foundry.GetPropertyOrDefault<string>("ShipmentId");
```

## Architecture

**Forge** creates runtime pieces. **Foundry** (`IWorkflowFoundry`) holds context; **Smith** (`IWorkflowSmith`) runs the workflow; each **operation** (`IWorkflowOperation`) is one step, with shared data in `foundry.Properties`. Interface sources: [`IWorkflowFoundry`](Abstractions/IWorkflowFoundry.cs) · [`IWorkflowSmith`](Abstractions/IWorkflowSmith.cs) · [`IWorkflowOperation`](Abstractions/IWorkflowOperation.cs)

## Built-in operations ([guide](https://animatlabs.com/workflow-forge/core/operations/))

- **DelegateWorkflowOperation:** delegate/lambda steps
- **ActionWorkflowOperation:** action-style steps
- **ConditionalWorkflowOperation:** branch on foundry state
- **ForEachWorkflowOperation:** collections, concurrency caps
- **DelayOperation:** `TimeSpan` wait
- **LoggingOperation:** fixed log line

## Custom operations

Subclass `WorkflowOperationBase`, override `ForgeAsyncCore`; add `RestoreAsync` when you need compensation. Typed bases: [Operations](https://animatlabs.com/workflow-forge/core/operations/).

```csharp
public class CalculateTotalOperation : WorkflowOperationBase
{
    public override string Name => "CalculateTotal";

    protected override async Task<object?> ForgeAsyncCore(
        object? inputData,
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken = default)
    {
        var items = foundry.GetPropertyOrDefault<List<OrderItem>>("Items");
        var total = items.Sum(x => x.Price * x.Quantity);

        foundry.SetProperty("Total", total);
        foundry.Logger.LogInformation("Calculated total: {Total}", total);

        return total;
    }
}
```

## Compensation (saga pattern)

```csharp
public class ChargePaymentOperation : WorkflowOperationBase
{
    public override string Name => "ChargePayment";

    protected override async Task<object?> ForgeAsyncCore(
        object? inputData,
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken)
    {
        var orderId = foundry.GetPropertyOrDefault<string>("OrderId");
        var amount = foundry.GetPropertyOrDefault<decimal>("Total");

        var paymentId = await _paymentService.ChargeAsync(orderId, amount, cancellationToken);

        foundry.SetProperty("PaymentId", paymentId);
        foundry.Logger.LogInformation("Payment charged: {PaymentId}", paymentId);

        return paymentId;
    }

    public override async Task RestoreAsync(
        object? outputData,
        IWorkflowFoundry foundry,
        CancellationToken cancellationToken)
    {
        var paymentId = foundry.GetPropertyOrDefault<string>("PaymentId");

        if (!string.IsNullOrEmpty(paymentId))
        {
            await _paymentService.RefundAsync(paymentId, cancellationToken);
            foundry.Logger.LogInformation("Payment refunded: {PaymentId}", paymentId);
        }
    }
}
```

## Middleware

```csharp
foundry.UseTiming();
foundry.UseErrorHandling(rethrowExceptions: true);
```

Custom `IWorkflowOperationMiddleware`: [Architecture](https://animatlabs.com/workflow-forge/architecture/overview/) · [Operations](https://animatlabs.com/workflow-forge/core/operations/)

## Event system

- **Smith:** `WorkflowStarted`, `WorkflowCompleted`, `WorkflowFailed`, `CompensationTriggered`, `OperationRestoreStarted`, and related compensation hooks
- **Foundry:** `OperationStarted`, `OperationCompleted`, `OperationFailed`

[Events](https://animatlabs.com/workflow-forge/core/events/)

## Configuration

`WorkflowForgeOptions` extends `WorkflowForgeOptionsBase` (`Enabled`, `SectionName`, `Validate()`, `Clone()`). Bind JSON with **WorkflowForge.Extensions.DependencyInjection**. [Configuration](https://animatlabs.com/workflow-forge/core/configuration/).

**Programmatic:**

```csharp
var options = new WorkflowForgeOptions
{
    Enabled = true,
    MaxConcurrentWorkflows = 10,
    ContinueOnError = false,
    FailFastCompensation = false,
    ThrowOnCompensationError = true,
    EnableOutputChaining = true
};

var foundry = WorkflowForge.CreateFoundry("MyWorkflow", options: options);
```

**`appsettings.json`:**

```json
{
  "WorkflowForge": {
    "Enabled": true,
    "MaxConcurrentWorkflows": 10,
    "ContinueOnError": false,
    "FailFastCompensation": false,
    "ThrowOnCompensationError": true,
    "EnableOutputChaining": true
  }
}
```

## Performance

12 scenarios (10 iterations per job each) vs Workflow Core and Elsa: **2–583×** execution, **1–533×** allocation, up to **584×** (.NET 10.0 state machine vs Elsa), **~15.9×** at 16 concurrent workflows.

| Scenario | WorkflowForge | Workflow Core | Elsa | Advantage |
|----------|---------------|---------------|------|-----------|
| Sequential (10 ops) | 127μs | 5661μs | 18257μs | 45-144x |
| State machine (25) | 59.7μs | 13798μs | 33184μs | 231–555× |
| Concurrent (8 workers) | 284μs | 39289μs | 110279μs | 138-388x |

*Benchmark data from .NET 8.0; up to 584× on .NET 10.0 (state machine vs Elsa).* [All scenarios](https://animatlabs.com/workflow-forge/performance/performance/).

## Documentation

- [Getting started](https://animatlabs.com/workflow-forge/getting-started/getting-started/) · [Samples (37)](https://github.com/animatlabs/workflow-forge/blob/main/src/samples/WorkflowForge.Samples.BasicConsole/README.md)
- [Architecture](https://animatlabs.com/workflow-forge/architecture/overview/) · [Operations](https://animatlabs.com/workflow-forge/core/operations/) · [Events](https://animatlabs.com/workflow-forge/core/events/) · [Configuration](https://animatlabs.com/workflow-forge/core/configuration/)
- [Extensions](https://animatlabs.com/workflow-forge/extensions/) · [API hub](https://animatlabs.com/workflow-forge/reference/api-reference/) · [WorkflowForge.Testing](https://github.com/animatlabs/workflow-forge/blob/main/src/core/WorkflowForge.Testing/README.md) (`FakeWorkflowFoundry`)

## Extensions

- **WorkflowForge.Testing:** `FakeWorkflowFoundry` and helpers
- **WorkflowForge.Extensions.Logging.Serilog:** structured logging
- **WorkflowForge.Extensions.Resilience:** retry (no extra deps)
- **WorkflowForge.Extensions.Resilience.Polly:** Polly-based resilience
- **WorkflowForge.Extensions.Validation:** DataAnnotations validation
- **WorkflowForge.Extensions.Audit:** audit logging
- **WorkflowForge.Extensions.Persistence:** workflow state persistence
- **WorkflowForge.Extensions.Persistence.Recovery:** recovery coordinator
- **WorkflowForge.Extensions.Observability.Performance:** performance hooks
- **WorkflowForge.Extensions.Observability.HealthChecks:** health checks
- **WorkflowForge.Extensions.Observability.OpenTelemetry:** tracing

`Resilience.Polly` and `Logging.Serilog` merge their third-party library with ILRepack; every other extension depends only on Microsoft/runtime packages, which stay external. **License:** MIT ([LICENSE](https://github.com/animatlabs/workflow-forge/blob/main/LICENSE)).
