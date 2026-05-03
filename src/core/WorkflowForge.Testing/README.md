# WorkflowForge.Testing

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Testing.svg)](https://www.nuget.org/packages/WorkflowForge.Testing/)

Test doubles and helpers for WorkflowForge. Exercise operations with **`FakeWorkflowFoundry`** instead of spinning up the full runtime.

## Installation

```bash
dotnet add package WorkflowForge.Testing
```

## Features

- **`FakeWorkflowFoundry`:** implements `IWorkflowFoundry` for unit tests
- **Execution tracking:** `ExecutedOperations` for order and coverage assertions
- **Plumbing:** swap in logger, `WorkflowForgeOptions`, and `IServiceProvider` when you need them

## Quick start

### Single operation

```csharp
using WorkflowForge.Testing;
using Xunit;

public class MyOperationTests
{
    [Fact]
    public async Task Operation_Should_SetProperty()
    {
        var foundry = new FakeWorkflowFoundry();
        var operation = new MyCustomOperation();

        await operation.ForgeAsync("input", foundry, CancellationToken.None);

        Assert.True(foundry.Properties.ContainsKey("myKey"));
        Assert.Equal("expectedValue", foundry.Properties["myKey"]);
    }
}
```

### Full foundry run

```csharp
[Fact]
public async Task Workflow_Should_ExecuteAllOperations()
{
    var foundry = new FakeWorkflowFoundry();
    var op1 = new LoggingOperation("Step 1");
    var op2 = new LoggingOperation("Step 2");

    foundry.AddOperation(op1);
    foundry.AddOperation(op2);

    await foundry.ForgeAsync();

    Assert.Equal(2, foundry.ExecutedOperations.Count);
    Assert.Contains(op1, foundry.ExecutedOperations);
    Assert.Contains(op2, foundry.ExecutedOperations);
}
```

### Logger and reset

```csharp
[Fact]
public async Task Operation_Should_Log_Messages()
{
    var testLogger = new TestLogger();
    var foundry = new FakeWorkflowFoundry { Logger = testLogger };
    var operation = new LoggingOperation("Test");

    await operation.ForgeAsync(null, foundry, CancellationToken.None);

    Assert.Contains(testLogger.Messages, m => m.Contains("Test"));
}
```

```csharp
private readonly FakeWorkflowFoundry _foundry = new();

public void Cleanup() => _foundry.Reset();
```

## `FakeWorkflowFoundry` API

| Property / method | Notes |
|-------------------|--------|
| `ExecutionId` | `Guid` (generated; you can set it) |
| `Properties` | `ConcurrentDictionary<string, object?>` |
| `CurrentWorkflow` | `IWorkflow?` |
| `Logger` | `IWorkflowForgeLogger` (defaults to null logger) |
| `Options` | `WorkflowForgeOptions` |
| `ServiceProvider` | `IServiceProvider?` |
| `Operations` | Operations queued on the fake |
| `Middlewares` | Middleware list |
| `ExecutedOperations` | What ran during `ForgeAsync` |
| `ForgeAsync()` | Runs operations in order |
| `Reset()` | Clears state between tests |
| `TrackExecution(operation)` | Marks an op as executed (manual) |

## License

MIT. See the repository [LICENSE](../../../LICENSE).
