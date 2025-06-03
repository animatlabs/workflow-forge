using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge;
using WorkflowForge.Abstractions;
using WorkflowForge.Operations;
using WorkflowForge.Extensions;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates workflow events and event handling capabilities.
/// Shows how to subscribe to workflow lifecycle events for monitoring and custom logic.
/// </summary>
public class WorkflowEventsSample : ISample
{
    public string Name => "Workflow Events";
    public string Description => "Workflow event subscription and handling for monitoring and custom logic";

    public async Task RunAsync()
    {
        Console.WriteLine("Demonstrating workflow events and event handling...");
        
        // Scenario 1: Basic workflow events
        await RunBasicWorkflowEventsDemo();
        
        // Scenario 2: Operation-level events
        await RunOperationEventsDemo();
        
        // Scenario 3: Error handling with events
        await RunErrorHandlingEventsDemo();
    }

    private static async Task RunBasicWorkflowEventsDemo()
    {
        Console.WriteLine("\n--- Basic Workflow Events Demo ---");
        
        using var foundry = WorkflowForge.CreateFoundry("WorkflowEventsDemo");
        
        // Subscribe to workflow events
        if (foundry is IWorkflowEvents workflowEvents)
        {
            workflowEvents.WorkflowStarted += OnWorkflowStarted;
            workflowEvents.WorkflowCompleted += OnWorkflowCompleted;
            workflowEvents.WorkflowFailed += OnWorkflowFailed;
        }
        
        foundry
            .WithOperation(LoggingOperation.Info("Step 1: Initializing workflow"))
            .WithOperation(DelayOperation.FromMilliseconds(200))
            .WithOperation(LoggingOperation.Info("Step 2: Processing data"))
            .WithOperation(DelayOperation.FromMilliseconds(150))
            .WithOperation(LoggingOperation.Info("Step 3: Finalizing workflow"));
        
        await foundry.ForgeAsync();
    }

    private static async Task RunOperationEventsDemo()
    {
        Console.WriteLine("\n--- Operation Events Demo ---");
        
        using var foundry = WorkflowForge.CreateFoundry("OperationEventsDemo");
        
        // Subscribe to operation events
        if (foundry is IWorkflowEvents workflowEvents)
        {
            workflowEvents.OperationStarted += OnOperationStarted;
            workflowEvents.OperationCompleted += OnOperationCompleted;
            workflowEvents.OperationFailed += OnOperationFailed;
        }
        
        foundry.Properties["operation_count"] = 0;
        foundry.Properties["total_duration"] = TimeSpan.Zero;
        
        foundry
            .WithOperation(new MonitoredOperation("InitializeSystem", TimeSpan.FromMilliseconds(100)))
            .WithOperation(new MonitoredOperation("ProcessRecords", TimeSpan.FromMilliseconds(250)))
            .WithOperation(new MonitoredOperation("ValidateResults", TimeSpan.FromMilliseconds(150)))
            .WithOperation(new MonitoredOperation("GenerateReport", TimeSpan.FromMilliseconds(200)));
        
        await foundry.ForgeAsync();
        
        Console.WriteLine($"   Total operations executed: {foundry.Properties["operation_count"]}");
        Console.WriteLine($"   Total processing time: {((TimeSpan)foundry.Properties["total_duration"]).TotalMilliseconds:F0}ms");
    }

    private static async Task RunErrorHandlingEventsDemo()
    {
        Console.WriteLine("\n--- Error Handling Events Demo ---");
        
        using var foundry = WorkflowForge.CreateFoundry("ErrorEventsDemo");
        
        // Subscribe to all events for comprehensive monitoring
        if (foundry is IWorkflowEvents workflowEvents)
        {
            workflowEvents.WorkflowStarted += OnWorkflowStarted;
            workflowEvents.WorkflowCompleted += OnWorkflowCompleted;
            workflowEvents.WorkflowFailed += OnWorkflowFailed;
            workflowEvents.OperationStarted += OnOperationStarted;
            workflowEvents.OperationCompleted += OnOperationCompleted;
            workflowEvents.OperationFailed += OnOperationFailed;
        }
        
        foundry
            .WithOperation(LoggingOperation.Info("Starting error handling demo"))
            .WithOperation(new SuccessfulOperation("NormalOperation"))
            .WithOperation(new FailingOperation("SimulatedFailure"))
            .WithOperation(LoggingOperation.Info("This step should not execute due to failure"));
        
        try
        {
            await foundry.ForgeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   Expected failure caught: {ex.Message}");
        }
    }

    // Event handlers
    private static void OnWorkflowStarted(object? sender, WorkflowStartedEventArgs e)
    {
        Console.WriteLine($"   [WORKFLOW STARTED] Foundry: {e.Foundry?.CurrentWorkflow?.Name ?? "Unknown"} at {e.StartedAt}");
    }

    private static void OnWorkflowCompleted(object? sender, WorkflowCompletedEventArgs e)
    {
        Console.WriteLine($"   [WORKFLOW COMPLETED] Foundry: {e.Foundry?.CurrentWorkflow?.Name ?? "Unknown"}, Duration: {e.Duration}, Result: {e.ResultData}");
    }

    private static void OnWorkflowFailed(object? sender, WorkflowFailedEventArgs e)
    {
        Console.WriteLine($"   [WORKFLOW FAILED] Foundry: {e.Foundry?.CurrentWorkflow?.Name ?? "Unknown"}, Exception: {e.Exception.Message}");
    }

    private static void OnOperationStarted(object? sender, OperationStartedEventArgs e)
    {
        Console.WriteLine($"   EVENT: Operation started - {e.Operation.Name} (ID: {e.Operation.Id})");
    }

    private static void OnOperationCompleted(object? sender, OperationCompletedEventArgs e)
    {
        Console.WriteLine($"   EVENT: Operation completed - {e.Operation.Name}, Duration: {e.Duration.TotalMilliseconds:F0}ms");
        
        // Update foundry statistics if available
        if (sender is IWorkflowFoundry foundry)
        {
            var currentCount = (int)foundry.Properties.GetValueOrDefault("operation_count", 0);
            var currentDuration = (TimeSpan)foundry.Properties.GetValueOrDefault("total_duration", TimeSpan.Zero);
            
            foundry.Properties["operation_count"] = currentCount + 1;
            foundry.Properties["total_duration"] = currentDuration + e.Duration;
        }
    }

    private static void OnOperationFailed(object? sender, OperationFailedEventArgs e)
    {
        Console.WriteLine($"   EVENT: Operation failed - {e.Operation.Name}, Error: {e.Exception.Message}, Duration: {e.Duration.TotalMilliseconds:F0}ms");
    }
}

/// <summary>
/// Operation that can be monitored for timing and execution patterns
/// </summary>
public class MonitoredOperation : IWorkflowOperation
{
    private readonly string _operationName;
    private readonly TimeSpan _simulatedDuration;

    public MonitoredOperation(string operationName, TimeSpan simulatedDuration)
    {
        _operationName = operationName;
        _simulatedDuration = simulatedDuration;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public string Name => _operationName;
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        foundry.Logger.LogInformation("Executing monitored operation: {OperationName}", _operationName);
        
        // Simulate work
        await Task.Delay(_simulatedDuration, cancellationToken);
        
        var result = new
        {
            OperationName = _operationName,
            ExecutedAt = DateTime.UtcNow,
            Duration = _simulatedDuration,
            Status = "Completed"
        };
        
        foundry.Logger.LogInformation("Monitored operation completed: {OperationName}", _operationName);
        
        return result;
    }

    public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        throw new NotSupportedException($"Operation {_operationName} does not support restoration");
    }

    public void Dispose() { }
}

/// <summary>
/// Operation that always succeeds for testing event flows
/// </summary>
public class SuccessfulOperation : IWorkflowOperation
{
    private readonly string _operationName;

    public SuccessfulOperation(string operationName)
    {
        _operationName = operationName;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public string Name => _operationName;
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        foundry.Logger.LogInformation("Executing successful operation: {OperationName}", _operationName);
        
        await Task.Delay(100, cancellationToken);
        
        foundry.Logger.LogInformation("Successful operation completed: {OperationName}", _operationName);
        
        return "Success";
    }

    public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        throw new NotSupportedException($"Operation {_operationName} does not support restoration");
    }

    public void Dispose() { }
}

/// <summary>
/// Operation that always fails for testing error event flows
/// </summary>
public class FailingOperation : IWorkflowOperation
{
    private readonly string _operationName;

    public FailingOperation(string operationName)
    {
        _operationName = operationName;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public string Name => _operationName;
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        foundry.Logger.LogInformation("Executing failing operation: {OperationName}", _operationName);
        
        await Task.Delay(75, cancellationToken);
        
        foundry.Logger.LogWarning("Failing operation about to throw exception: {OperationName}", _operationName);
        
        throw new InvalidOperationException($"Simulated failure in operation: {_operationName}");
    }

    public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        throw new NotSupportedException($"Operation {_operationName} does not support restoration");
    }

    public void Dispose() { }
} 