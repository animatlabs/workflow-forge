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

Inline delegates: [Operations](../../../docs/core/operations.md).

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

## Built-in operations ([guide](../../../docs/core/operations.md))

- **DelegateWorkflowOperation:** delegate/lambda steps
- **ActionWorkflowOperation:** action-style steps
- **ConditionalWorkflowOperation:** branch on foundry state
- **ForEachWorkflowOperation:** collections, concurrency caps
- **DelayOperation:** `TimeSpan` wait
- **LoggingOperation:** fixed log line

## Custom operations

Subclass `WorkflowOperationBase`, override `ForgeAsyncCore`; add `RestoreAsync` when you need compensation. Typed bases: [Operations](../../../docs/core/operations.md).

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

Custom `IWorkflowOperationMiddleware`: [Architecture](../../../docs/architecture/overview.md) · [Operations](../../../docs/core/operations.md)

## Event system

- **Smith:** `WorkflowStarted`, `WorkflowCompleted`, `WorkflowFailed`, `CompensationTriggered`, `OperationRestoreStarted`, and related compensation hooks
- **Foundry:** `OperationStarted`, `OperationCompleted`, `OperationFailed`

[Events](../../../docs/core/events.md)

## Configuration

`WorkflowForgeOptions` extends `WorkflowForgeOptionsBase` (`Enabled`, `SectionName`, `Validate()`, `Clone()`). Bind JSON with **WorkflowForge.Extensions.DependencyInjection**. [Configuration](../../../docs/core/configuration.md).

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

12 scenarios (50 iterations each) vs Workflow Core and Elsa: **13–511×** execution, **6–575×** allocation, up to **511×** (.NET 10.0 state machine), **~15.9×** at 16 concurrent workflows.

| Scenario | WorkflowForge | Workflow Core | Elsa | Advantage |
|----------|---------------|---------------|------|-----------|
| Sequential (10 ops) | 314μs | 15,997μs | 26,881μs | 51–86× |
| State machine (25) | 111μs | 39,500μs | 45,714μs | 356–412× |
| Concurrent (8 workers) | 482μs | 59,141μs | 137,342μs | 123–285× |

*Benchmark data from .NET 8.0; up to 511× on .NET 10.0 (state machine).* [All scenarios](../../../docs/performance/performance.md).

## Documentation

- [Getting started](../../../docs/getting-started/getting-started.md) · [Samples (33)](../../samples/WorkflowForge.Samples.BasicConsole/README.md)
- [Architecture](../../../docs/architecture/overview.md) · [Operations](../../../docs/core/operations.md) · [Events](../../../docs/core/events.md) · [Configuration](../../../docs/core/configuration.md)
- [Extensions](../../../docs/extensions/index.md) · [API reference](../../../docs/reference/api-reference.md) · [WorkflowForge.Testing](../WorkflowForge.Testing/README.md) (`FakeWorkflowFoundry`)

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

Third-party assemblies are often merged with ILRepack; Microsoft/runtime refs stay external. **License:** MIT ([LICENSE](../../../LICENSE)).
