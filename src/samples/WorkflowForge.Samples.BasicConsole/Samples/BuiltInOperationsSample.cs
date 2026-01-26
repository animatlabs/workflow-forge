using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Operations;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates all built-in operations available in WorkflowForge.
/// Shows proper usage of LoggingOperation, DelayOperation, DelegateWorkflowOperation,
/// ActionWorkflowOperation, and other common operations.
/// </summary>
public class BuiltInOperationsSample : ISample
{
    public string Name => "Built-in Operations";
    public string Description => "Comprehensive demonstration of all built-in WorkflowForge operations";

    public async Task RunAsync()
    {
        Console.WriteLine("Demonstrating built-in WorkflowForge operations...");

        // Scenario 1: Logging Operations
        await RunLoggingOperationsDemo();

        // Scenario 2: Delay Operations
        await RunDelayOperationsDemo();

        // Scenario 3: Delegate and Action Operations
        await RunDelegateOperationsDemo();

        // Scenario 4: Combined Operations Workflow
        await RunCombinedOperationsDemo();
    }

    private static async Task RunLoggingOperationsDemo()
    {
        Console.WriteLine("\n--- Logging Operations Demo ---");

        using var foundry = WorkflowForge.CreateFoundry("LoggingOperationsDemo");

        // Demonstrate logging operations
        foundry.AddOperation(LoggingOperation.Info("Starting logging operations demonstration"));
        foundry.AddOperation(LoggingOperation.Debug("Debug information: System initialized"));
        foundry.AddOperation(LoggingOperation.Warning("Warning: This is a demonstration warning"));
        foundry.AddOperation(LoggingOperation.Error("Error simulation: This is not a real error"));
        foundry.AddOperation(LoggingOperation.Info("Logging operations demonstration completed"));

        await foundry.ForgeAsync();
    }

    private static async Task RunDelayOperationsDemo()
    {
        Console.WriteLine("\n--- Delay Operations Demo ---");

        using var foundry = WorkflowForge.CreateFoundry("DelayOperationsDemo");

        // Demonstrate delay operations
        foundry.AddOperation(LoggingOperation.Info("Starting delay operations demonstration"));
        foundry.AddOperation(DelayOperation.FromMilliseconds(250));
        foundry.AddOperation(LoggingOperation.Info("Completed 250ms delay"));
        foundry.AddOperation(DelayOperation.FromSeconds(1));
        foundry.AddOperation(LoggingOperation.Info("Completed 1 second delay"));
        foundry.AddOperation(new CustomTimedOperation("Custom operation with timing", TimeSpan.FromMilliseconds(300)));
        foundry.AddOperation(LoggingOperation.Info("Delay operations demonstration completed"));

        await foundry.ForgeAsync();
    }

    private static async Task RunDelegateOperationsDemo()
    {
        Console.WriteLine("\n--- Delegate Operations Demo ---");

        using var foundry = WorkflowForge.CreateFoundry("DelegateOperationsDemo");

        // Set up initial data
        foundry.SetProperty("counter", 0);
        foundry.SetProperty("message_list", new List<string>());

        foundry.AddOperation(LoggingOperation.Info("Starting delegate operations demonstration"));

        // Simple delegate operation
        foundry.AddOperation(new DelegateWorkflowOperation("SimpleDelegate", (input, foundry, token) =>
        {
            foundry.Logger.LogInformation("Executing simple delegate operation");
            foundry.SetProperty("simple_result", "Delegate executed successfully");
            return Task.FromResult<object?>("Simple delegate completed");
        }));

        // Delegate with counter increment
        foundry.AddOperation(new DelegateWorkflowOperation("CounterIncrement", (input, foundry, token) =>
        {
            var counter = foundry.GetPropertyOrDefault<int>("counter");
            counter++;
            foundry.SetProperty("counter", counter);
            foundry.Logger.LogInformation("Counter incremented to: {Counter}", counter);
            return Task.FromResult<object?>(counter);
        }));

        // Delegate with data processing
        foundry.AddOperation(new DelegateWorkflowOperation("DataProcessor", (input, foundry, token) =>
        {
            var messageList = foundry.GetPropertyOrDefault<List<string>>("message_list") ?? new List<string>();
            var newMessage = $"Processed at {DateTime.UtcNow:HH:mm:ss.fff}";
            messageList.Add(newMessage);
            foundry.Logger.LogInformation("Added message: {Message}", newMessage);
            return Task.FromResult<object?>(messageList.Count);
        }));

        // Action operation
        foundry.AddOperation(new ActionWorkflowOperation("ActionOperation", async (input, foundry, token) =>
        {
            foundry.Logger.LogInformation("Executing action operation");
            foundry.SetProperty("action_executed", DateTime.UtcNow);
            await Task.Delay(100, token);
        }));

        foundry.AddOperation(LoggingOperation.Info("Delegate operations demonstration completed"));

        await foundry.ForgeAsync();

        // Display results
        Console.WriteLine($"   Final counter value: {foundry.GetPropertyOrDefault<int>("counter")}");
        Console.WriteLine($"   Simple result: {foundry.GetPropertyOrDefault<string>("simple_result", "")}");
        Console.WriteLine($"   Messages processed: {(foundry.GetPropertyOrDefault<List<string>>("message_list")?.Count ?? 0)}");
        Console.WriteLine($"   Action executed at: {foundry.GetPropertyOrDefault<DateTime>("action_executed")}");
    }

    private static async Task RunCombinedOperationsDemo()
    {
        Console.WriteLine("\n--- Combined Operations Demo ---");

        using var foundry = WorkflowForge.CreateFoundry("CombinedOperationsDemo");

        foundry.SetProperty("process_id", Guid.NewGuid().ToString("N")[..8]);
        foundry.SetProperty("step_count", 0);

        foundry.AddOperation(LoggingOperation.Info("=== Combined Operations Workflow Started ==="));

        // Step 1: Initialize
        foundry.AddOperation(new DelegateWorkflowOperation("Initialize", async (input, foundry, token) =>
        {
            foundry.TryGetProperty<string>("process_id", out var processId);
            foundry.Logger.LogInformation("Initializing process: {ProcessId}", processId);
            foundry.SetProperty("step_count", 1);
            foundry.SetProperty("start_time", DateTime.UtcNow);
            return "Initialized";
        }));

        foundry.AddOperation(DelayOperation.FromMilliseconds(150));

        // Step 2: Process data
        foundry.AddOperation(LoggingOperation.Debug("Processing data..."));
        foundry.AddOperation(new DelegateWorkflowOperation("ProcessData", async (input, foundry, token) =>
        {
            foundry.SetProperty("step_count", 2);
            foundry.SetProperty("data_processed", new
            {
                RecordsProcessed = 42,
                ProcessingTime = TimeSpan.FromMilliseconds(300),
                Status = "Completed"
            });
            foundry.Logger.LogInformation("Data processing completed: 42 records processed");
            return "Data processed";
        }));

        foundry.AddOperation(DelayOperation.FromMilliseconds(200));

        // Step 3: Validate results
        foundry.AddOperation(LoggingOperation.Debug("Validating results..."));
        foundry.AddOperation(new ActionWorkflowOperation("ValidateResults", async (input, foundry, token) =>
        {
            foundry.SetProperty("step_count", 3);
            var processedData = foundry.GetPropertyOrDefault<object>("data_processed");
            foundry.Logger.LogInformation("Validation completed for processed data: {HasData}", processedData is not null);
            foundry.SetProperty("validation_result", "Valid");
            await Task.Delay(100, token);
        }));

        foundry.AddOperation(DelayOperation.FromMilliseconds(100));

        // Step 4: Finalize
        foundry.AddOperation(new DelegateWorkflowOperation("Finalize", async (input, foundry, token) =>
        {
            foundry.TryGetProperty<string>("process_id", out var processId);
            var startTime = foundry.GetPropertyOrDefault<DateTime>("start_time");
            var totalDuration = DateTime.UtcNow - startTime;

            foundry.SetProperty("step_count", 4);
            foundry.SetProperty("total_duration", totalDuration);

            foundry.Logger.LogInformation("Process {ProcessId} completed in {Duration}ms",
                processId, totalDuration.TotalMilliseconds);

            return "Finalized";
        }));

        foundry.AddOperation(LoggingOperation.Info("=== Combined Operations Workflow Completed ==="));

        await foundry.ForgeAsync();

        // Display final results
        foundry.TryGetProperty<string>("process_id", out var processId);
        var stepCount = foundry.GetPropertyOrDefault<int>("step_count");
        var totalDuration = foundry.GetPropertyOrDefault<TimeSpan>("total_duration");

        Console.WriteLine($"   Process ID: {processId}");
        Console.WriteLine($"   Steps completed: {stepCount}");
        Console.WriteLine($"   Total duration: {totalDuration.TotalMilliseconds:F0}ms");
        Console.WriteLine($"   Validation result: {foundry.GetPropertyOrDefault<string>("validation_result", "")}");
    }
}

/// <summary>
/// Custom operation demonstrating timing and custom behavior
/// </summary>
public class CustomTimedOperation : IWorkflowOperation
{
    private readonly string _operationName;
    private readonly TimeSpan _duration;

    public CustomTimedOperation(string operationName, TimeSpan duration)
    {
        _operationName = operationName;
        _duration = duration;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public string Name => _operationName;
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        foundry.Logger.LogInformation("Starting custom timed operation: {OperationName} (Duration: {Duration}ms)",
            _operationName, _duration.TotalMilliseconds);

        await Task.Delay(_duration, cancellationToken);

        foundry.Logger.LogInformation("Completed custom timed operation: {OperationName}", _operationName);

        return new
        {
            OperationName = _operationName,
            Duration = _duration,
            CompletedAt = DateTime.UtcNow
        };
    }

    public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        throw new NotSupportedException($"Operation {_operationName} does not support restoration");
    }

    public void Dispose() { }
}