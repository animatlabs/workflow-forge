# WorkflowForge.Testing

Testing utilities for WorkflowForge. This package provides test doubles and helpers for unit testing workflow operations without requiring the full workflow infrastructure.

## Installation

```bash
dotnet add package WorkflowForge.Testing
```

## Features

- **FakeWorkflowFoundry**: A lightweight fake implementation of `IWorkflowFoundry` for unit testing
- **Execution Tracking**: Track which operations were executed for assertions
- **Configurable Behavior**: Set up custom logger, options, and service provider

## Quick Start

### Testing Individual Operations

```csharp
using WorkflowForge.Testing;
using Xunit;

public class MyOperationTests
{
    [Fact]
    public async Task Operation_Should_SetProperty()
    {
        // Arrange
        var foundry = new FakeWorkflowFoundry();
        var operation = new MyCustomOperation();
        
        // Act
        await operation.ForgeAsync("input", foundry, CancellationToken.None);
        
        // Assert
        Assert.True(foundry.Properties.ContainsKey("myKey"));
        Assert.Equal("expectedValue", foundry.Properties["myKey"]);
    }
}
```

### Testing Workflow Execution

```csharp
[Fact]
public async Task Workflow_Should_ExecuteAllOperations()
{
    // Arrange
    var foundry = new FakeWorkflowFoundry();
    var op1 = new LoggingOperation("Step 1");
    var op2 = new LoggingOperation("Step 2");
    
    foundry.AddOperation(op1);
    foundry.AddOperation(op2);
    
    // Act
    await foundry.ForgeAsync();
    
    // Assert
    Assert.Equal(2, foundry.ExecutedOperations.Count);
    Assert.Contains(op1, foundry.ExecutedOperations);
    Assert.Contains(op2, foundry.ExecutedOperations);
}
```

### Using with Custom Logger

```csharp
[Fact]
public async Task Operation_Should_Log_Messages()
{
    // Arrange
    var testLogger = new TestLogger(); // Your custom test logger
    var foundry = new FakeWorkflowFoundry
    {
        Logger = testLogger
    };
    
    var operation = new LoggingOperation("Test");
    
    // Act
    await operation.ForgeAsync(null, foundry, CancellationToken.None);
    
    // Assert
    Assert.Contains(testLogger.Messages, m => m.Contains("Test"));
}
```

### Resetting Between Tests

```csharp
private readonly FakeWorkflowFoundry _foundry = new FakeWorkflowFoundry();

public void Cleanup()
{
    _foundry.Reset(); // Clears all state
}
```

## API Reference

### FakeWorkflowFoundry

| Property | Type | Description |
|----------|------|-------------|
| `ExecutionId` | `Guid` | Unique execution identifier (auto-generated, settable) |
| `Properties` | `ConcurrentDictionary<string, object?>` | Thread-safe property storage |
| `CurrentWorkflow` | `IWorkflow?` | Current workflow reference |
| `Logger` | `IWorkflowForgeLogger` | Logger instance (defaults to NullLogger) |
| `Options` | `WorkflowForgeOptions` | Execution options |
| `ServiceProvider` | `IServiceProvider?` | DI service provider |
| `Operations` | `IReadOnlyList<IWorkflowOperation>` | Added operations |
| `Middlewares` | `IReadOnlyList<IWorkflowOperationMiddleware>` | Added middleware |
| `ExecutedOperations` | `IReadOnlyList<IWorkflowOperation>` | Operations executed during ForgeAsync |

| Method | Description |
|--------|-------------|
| `ForgeAsync()` | Executes all operations sequentially |
| `Reset()` | Clears all state for test reuse |
| `TrackExecution(operation)` | Manually track an operation as executed |

## License

MIT License - see LICENSE file for details.
