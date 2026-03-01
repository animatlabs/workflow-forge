using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Operations;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates collection processing using ForEachWorkflowOperation.
/// Shows how to process arrays, lists, and other collections in workflows.
/// Equivalent to workflow-core's ForEach examples.
/// </summary>
public class ForEachLoopSample : ISample
{
    public string Name => "ForEach Loop Processing";
    public string Description => "Demonstrates collection processing and iteration";

    public async Task RunAsync()
    {
        Console.WriteLine("Creating workflows that demonstrate ForEach collection processing...");

        // Scenario 1: Process a list of orders
        await ProcessOrdersScenario();

        // Scenario 2: Process customer notifications in parallel
        await ProcessNotificationsScenario();

        // Scenario 3: Validate and process data items
        await ValidateDataItemsScenario();
    }

    private static async Task ProcessOrdersScenario()
    {
        Console.WriteLine("\n--- Processing Orders Scenario ---");

        using var foundry = WorkflowForge.CreateFoundry("ProcessOrdersWorkflow");

        // Set up order data
        var orders = new[]
        {
            new { Id = 1001, Amount = 150.00m, Customer = "Alice Johnson" },
            new { Id = 1002, Amount = 75.50m, Customer = "Bob Smith" },
            new { Id = 1003, Amount = 299.99m, Customer = "Carol Davis" },
            new { Id = 1004, Amount = 45.00m, Customer = "David Wilson" }
        };

        foundry.Properties["orders"] = orders;
        Console.WriteLine($"Processing {orders.Length} orders...");

        foundry
            .WithOperation(new LoggingOperation("[START] Starting order processing batch"))
            .WithOperation(ForEachWorkflowOperation.CreateSplitInput(new IWorkflowOperation[]
            {
                new ProcessSingleOrderOperation()
            }, name: "ProcessAllOrders"))
            .WithOperation(new LoggingOperation("[SUCCESS] All orders processed successfully"));

        await foundry.ForgeAsync();
    }

    private static async Task ProcessNotificationsScenario()
    {
        Console.WriteLine("\n--- Processing Notifications Scenario ---");

        using var foundry = WorkflowForge.CreateFoundry("ProcessNotificationsWorkflow");

        // Set up notification data
        var notifications = new[]
        {
            new { Type = "Email", Recipient = "user1@example.com", Message = "Welcome to our service!" },
            new { Type = "SMS", Recipient = "+1234567890", Message = "Your order has shipped" },
            new { Type = "Push", Recipient = "device123", Message = "New feature available" },
            new { Type = "Email", Recipient = "user2@example.com", Message = "Monthly newsletter" }
        };

        foundry.Properties["notifications"] = notifications;
        Console.WriteLine($"Sending {notifications.Length} notifications in parallel...");

        foundry
            .WithOperation(new LoggingOperation("[START] Starting notification batch"))
            .WithOperation(ForEachWorkflowOperation.CreateSplitInput(new IWorkflowOperation[]
            {
                new SendNotificationOperation()
            }, maxConcurrency: 2, name: "SendAllNotifications")) // Limit to 2 concurrent notifications
            .WithOperation(new LoggingOperation("[SUCCESS] All notifications sent"));

        await foundry.ForgeAsync();
    }

    private static async Task ValidateDataItemsScenario()
    {
        Console.WriteLine("\n--- Validate Data Items Scenario ---");

        using var foundry = WorkflowForge.CreateFoundry("ValidateDataWorkflow");

        // Set up data items for validation
        var dataItems = new[]
        {
            new { Id = "ITEM001", Value = "ValidData", Category = "A" },
            new { Id = "ITEM002", Value = "", Category = "B" }, // Invalid - empty value
            new { Id = "ITEM003", Value = "AnotherValidItem", Category = "A" },
            new { Id = "ITEM004", Value = "TestData", Category = "C" }
        };

        foundry.Properties["data_items"] = dataItems;
        foundry.Properties["validation_results"] = new List<object>();

        Console.WriteLine($"Validating {dataItems.Length} data items...");

        foundry
            .WithOperation(new LoggingOperation("[START] Starting data validation"))
            .WithOperation(ForEachWorkflowOperation.CreateSplitInput(new IWorkflowOperation[]
            {
                new ValidateDataItemOperation(),
                new ConditionalWorkflowOperation(
                    // Check if validation passed
                    (inputData, foundry, cancellationToken) => Task.FromResult(
                        foundry.Properties.ContainsKey("last_validation_result") &&
                        (bool)foundry.Properties["last_validation_result"]!),
                    // If valid: process the item
                    new ProcessValidDataOperation(),
                    // If invalid: log the error
                    new LoggingOperation("[ERROR] Skipping invalid data item")
                )
            }, name: "ValidateAllItems"))
            .WithOperation(new SummarizeValidationResultsOperation());

        await foundry.ForgeAsync();
    }
}

public class ProcessSingleOrderOperation : WorkflowOperationBase
{
    public override string Name => "ProcessSingleOrder";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        if (inputData == null)
            return null;

        // Extract order data (inputData will be one order from the split)
        var orderData = inputData.ToString()!;
        Console.WriteLine($"   [INFO] Processing order: {orderData}");

        // Simulate order processing
        await Task.Delay(100, cancellationToken);

        // In a real scenario, you'd extract specific properties from the order object
        Console.WriteLine($"   [SUCCESS] Order processed successfully");

        return $"Processed: {orderData}";
    }
}

public class SendNotificationOperation : WorkflowOperationBase
{
    public override string Name => "SendNotification";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        if (inputData == null)
            return null;

        var notificationData = inputData.ToString()!;
        Console.WriteLine($"   [INFO] Sending notification: {notificationData}");

        // Simulate notification sending with variable delay
        var delay = ThreadSafeRandom.Next(50, 200);
        await Task.Delay(delay, cancellationToken);

        Console.WriteLine($"   [SUCCESS] Notification sent successfully (took {delay}ms)");

        return $"Sent: {notificationData}";
    }
}

public class ValidateDataItemOperation : WorkflowOperationBase
{
    public override string Name => "ValidateDataItem";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        if (inputData == null)
        {
            foundry.Properties["last_validation_result"] = false;
            return null;
        }

        var itemData = inputData.ToString()!;
        Console.WriteLine($"   [INFO] Validating data item: {itemData}");

        // Simulate validation logic
        await Task.Delay(50, cancellationToken);

        // Simple validation: check if the item contains "Valid" or has non-empty value
        bool isValid = itemData.Contains("Valid") || (!string.IsNullOrEmpty(itemData) && !itemData.Contains("\"Value\":\"\","));

        foundry.Properties["last_validation_result"] = isValid;

        if (isValid)
        {
            Console.WriteLine($"   [SUCCESS] Data item is valid");
        }
        else
        {
            Console.WriteLine($"   [ERROR] Data item failed validation");
        }

        return isValid;
    }

    public override Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        foundry.Properties.TryRemove("last_validation_result", out _);
        return Task.CompletedTask;
    }
}

public class ProcessValidDataOperation : WorkflowOperationBase
{
    public override string Name => "ProcessValidData";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        Console.WriteLine($"   [INFO] Processing valid data item...");

        // Simulate data processing
        await Task.Delay(75, cancellationToken);

        // Add to validation results
        if (foundry.Properties["validation_results"] is List<object> results)
        {
            results.Add(new { Status = "Processed", Data = inputData, Timestamp = DateTime.UtcNow });
        }

        Console.WriteLine($"   [SUCCESS] Valid data item processed and stored");

        return "Processed";
    }
}

public class SummarizeValidationResultsOperation : WorkflowOperationBase
{
    public override string Name => "SummarizeValidationResults";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        Console.WriteLine($"   [INFO] Summarizing validation results...");

        await Task.Delay(25, cancellationToken);

        if (foundry.Properties["validation_results"] is List<object> results)
        {
            Console.WriteLine($"   [INFO] Validation Summary: {results.Count} items processed successfully");

            foreach (var result in results)
            {
                Console.WriteLine($"      • {result}");
            }
        }
        else
        {
            Console.WriteLine($"   [INFO] No validation results found");
        }

        return "Summary completed";
    }
}
