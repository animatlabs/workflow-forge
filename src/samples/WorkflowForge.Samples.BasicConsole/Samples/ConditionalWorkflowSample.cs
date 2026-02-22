using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Operations;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates conditional branching in workflows using ConditionalWorkflowOperation.
/// Shows how to create if/then logic in workflow execution.
/// Equivalent to workflow-core's conditional examples.
/// </summary>
public class ConditionalWorkflowSample : ISample
{
    public string Name => "Conditional Workflow";
    public string Description => "Demonstrates conditional branching and decision making";

    public async Task RunAsync()
    {
        Console.WriteLine("Creating a workflow with conditional logic...");

        // Test different scenarios
        await RunScenario("Premium Customer", 1500.00m, "Gold");
        await RunScenario("Regular Customer", 75.00m, "Bronze");
        await RunScenario("VIP Customer", 500.00m, "Platinum");
    }

    private static async Task RunScenario(string scenario, decimal amount, string customerTier)
    {
        Console.WriteLine($"\n--- {scenario} Scenario ---");

        // Step 1: Create workflow with conditional operations
        var workflow = WorkflowForge.CreateWorkflow()
            .WithName($"ConditionalWorkflow-{scenario}")
            .WithDescription("Demonstrates conditional branching and decision making")
            .AddOperation(new InitializeOrderOperation())
            .AddOperation(new ConditionalWorkflowOperation(
                condition: (input, foundry) =>
                {
                    var orderAmount = foundry.GetPropertyOrDefault<decimal>("order_amount");
                    return orderAmount >= 1000m; // Premium threshold
                },
                trueOperation: new PremiumOrderProcessingOperation(),
                falseOperation: new StandardOrderProcessingOperation(),
                name: "OrderTypeConditional"
            ))
            .AddOperation(new ConditionalWorkflowOperation(
                condition: (input, foundry) =>
                {
                    foundry.TryGetProperty<string>("payment_method", out var paymentMethod);
                    return paymentMethod == "CreditCard";
                },
                trueOperation: new CreditCardPaymentOperation(),
                falseOperation: new BankTransferPaymentOperation(),
                name: "PaymentMethodConditional"
            ))
            .AddOperation(new FinalizeOrderOperation())
            .Build();

        // Step 2: Create foundry with initial data
        var initialData = new Dictionary<string, object?>
        {
            ["order_amount"] = amount,
            ["customer_tier"] = customerTier,
            ["payment_method"] = amount > 500m ? "CreditCard" : "BankTransfer" // Higher amounts prefer credit card
        };
        using var foundry = WorkflowForge.CreateFoundry($"ConditionalWorkflow-{scenario}", null, initialData);

        Console.WriteLine($"Order amount: ${amount:F2}, Customer tier: {customerTier}");

        // Step 3: Create smith and execute workflow
        using var smith = WorkflowForge.CreateSmith();

        Console.WriteLine("Executing conditional workflow...");
        await smith.ForgeAsync(workflow, foundry);

        // Show results
        var processingType = foundry.GetPropertyOrDefault<string>("processing_type", "Unknown");
        var paymentType = foundry.GetPropertyOrDefault<string>("payment_type", "Unknown");
        Console.WriteLine($"   [RESULT] Processing: {processingType}, Payment: {paymentType}");
    }
}

public class InitializeOrderOperation : WorkflowOperationBase
{
    public override string Name => "InitializeOrder";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        Console.WriteLine("   [START] Initializing order...");
        await Task.Delay(50, cancellationToken);

        foundry.SetProperty("order_status", "Initialized");
        foundry.SetProperty("processing_start", DateTime.UtcNow);

        return "Order initialized";
    }

    public override Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        foundry.Properties.TryRemove("order_status", out _);
        foundry.Properties.TryRemove("processing_start", out _);
        return Task.CompletedTask;
    }
}

public class PremiumOrderProcessingOperation : WorkflowOperationBase
{
    public override string Name => "PremiumOrderProcessing";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        Console.WriteLine("   [PREMIUM] Processing high-value order with premium service...");
        await Task.Delay(150, cancellationToken);

        foundry.SetProperty("processing_type", "Premium");
        foundry.SetProperty("priority_level", "High");
        foundry.SetProperty("estimated_completion", DateTime.UtcNow.AddHours(1));
        foundry.SetProperty("premium_benefits", "Express processing, dedicated support, priority fulfillment");

        Console.WriteLine("   [SUCCESS] Premium processing assigned with express service");
        return "Premium processing assigned";
    }

    public override Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        foundry.Properties.TryRemove("processing_type", out _);
        foundry.Properties.TryRemove("priority_level", out _);
        foundry.Properties.TryRemove("estimated_completion", out _);
        foundry.Properties.TryRemove("premium_benefits", out _);
        return Task.CompletedTask;
    }
}

public class StandardOrderProcessingOperation : WorkflowOperationBase
{
    public override string Name => "StandardOrderProcessing";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        Console.WriteLine("   [STANDARD] Processing order with standard service...");
        await Task.Delay(80, cancellationToken);

        foundry.SetProperty("processing_type", "Standard");
        foundry.SetProperty("priority_level", "Normal");
        foundry.SetProperty("estimated_completion", DateTime.UtcNow.AddHours(4));

        Console.WriteLine("   [SUCCESS] Standard processing assigned");
        return "Standard processing assigned";
    }

    public override Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        foundry.Properties.TryRemove("processing_type", out _);
        foundry.Properties.TryRemove("priority_level", out _);
        foundry.Properties.TryRemove("estimated_completion", out _);
        return Task.CompletedTask;
    }
}

public class CreditCardPaymentOperation : WorkflowOperationBase
{
    public override string Name => "CreditCardPayment";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        Console.WriteLine("   [PAYMENT] Processing credit card payment...");
        await Task.Delay(120, cancellationToken);

        var transactionId = $"CC-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        foundry.SetProperty("payment_type", "CreditCard");
        foundry.SetProperty("transaction_id", transactionId);
        foundry.SetProperty("payment_status", "Processed");
        foundry.SetProperty("processing_fee", 2.9m); // 2.9% processing fee

        Console.WriteLine($"   [SUCCESS] Credit card payment processed: {transactionId}");
        return $"Credit card payment: {transactionId}";
    }

    public override async Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var transactionId = foundry.GetPropertyOrDefault<string>("transaction_id", "Unknown");
        Console.WriteLine($"   [REFUND] Refunding credit card transaction {transactionId}...");

        await Task.Delay(100, cancellationToken);

        foundry.Properties.TryRemove("transaction_id", out _);
        foundry.SetProperty("payment_status", "Refunded");

        Console.WriteLine($"   [SUCCESS] Credit card refund completed");
    }
}

public class BankTransferPaymentOperation : WorkflowOperationBase
{
    public override string Name => "BankTransferPayment";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        Console.WriteLine("   [PAYMENT] Processing bank transfer payment...");
        await Task.Delay(200, cancellationToken);

        var transferId = $"BT-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        foundry.SetProperty("payment_type", "BankTransfer");
        foundry.SetProperty("transfer_id", transferId);
        foundry.SetProperty("payment_status", "Pending"); // Bank transfers take longer
        foundry.SetProperty("processing_fee", 0.5m); // Lower processing fee
        foundry.SetProperty("clearing_time", "1-3 business days");

        Console.WriteLine($"   [SUCCESS] Bank transfer initiated: {transferId} (clearing in 1-3 business days)");
        return $"Bank transfer: {transferId}";
    }

    public override async Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var transferId = foundry.GetPropertyOrDefault<string>("transfer_id", "Unknown");
        Console.WriteLine($"   [REVERSAL] Reversing bank transfer {transferId}...");

        await Task.Delay(150, cancellationToken);

        foundry.Properties.TryRemove("transfer_id", out _);
        foundry.SetProperty("payment_status", "Reversed");

        Console.WriteLine($"   [SUCCESS] Bank transfer reversal completed");
    }
}

public class FinalizeOrderOperation : WorkflowOperationBase
{
    public override string Name => "FinalizeOrder";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        Console.WriteLine("   [INFO] Finalizing order...");

        await Task.Delay(60, cancellationToken);

        var processingType = foundry.GetPropertyOrDefault<string>("processing_type", "Unknown");
        var completion = foundry.GetPropertyOrDefault<DateTime>("estimated_completion", DateTime.UtcNow);

        foundry.SetProperty("order_status", "Finalized");
        foundry.SetProperty("completion_time", DateTime.UtcNow);

        var start = foundry.GetPropertyOrDefault<DateTime>("processing_start", DateTime.UtcNow);
        var duration = DateTime.UtcNow - start;
        Console.WriteLine($"   [SUCCESS] Order finalized with {processingType} processing in {duration.TotalMilliseconds:F0}ms");
        Console.WriteLine($"   [INFO] Estimated completion: {completion:yyyy-MM-dd HH:mm}");

        return "Order finalized";
    }

    public override Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        foundry.Properties.TryRemove("order_status", out _);
        foundry.Properties.TryRemove("completion_time", out _);
        return Task.CompletedTask;
    }
}