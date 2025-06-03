using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge;
using WorkflowForge.Abstractions;
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
                    var orderAmount = (decimal)foundry.Properties["order_amount"]!;
                    return orderAmount >= 1000m; // Premium threshold
                },
                trueOperation: new PremiumOrderProcessingOperation(),
                falseOperation: new StandardOrderProcessingOperation(),
                name: "OrderTypeConditional"
            ))
            .AddOperation(new ConditionalWorkflowOperation(
                condition: (input, foundry) => 
                {
                    var paymentMethod = foundry.Properties["payment_method"] as string;
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
        using var foundry = WorkflowForge.CreateFoundryWithData($"ConditionalWorkflow-{scenario}", initialData, FoundryConfiguration.Development());
        
        Console.WriteLine($"Order amount: ${amount:F2}, Customer tier: {customerTier}");
        
        // Step 3: Create smith and execute workflow
        using var smith = WorkflowForge.CreateSmith();
        
        Console.WriteLine("Executing conditional workflow...");
        await smith.ForgeAsync(workflow, foundry);
        
        // Show results
        var processingType = foundry.Properties.TryGetValue("processing_type", out var pt) ? (string)pt! : "Unknown";
        var paymentType = foundry.Properties.TryGetValue("payment_type", out var pmt) ? (string)pmt! : "Unknown";
        Console.WriteLine($"   [RESULT] Processing: {processingType}, Payment: {paymentType}");
    }
}

public class InitializeOrderOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "InitializeOrder";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("   [START] Initializing order...");
        await Task.Delay(50, cancellationToken);
        
        foundry.Properties["order_status"] = "Initialized";
        foundry.Properties["processing_start"] = DateTime.UtcNow;
        
        return "Order initialized";
    }

    public Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        foundry.Properties.TryRemove("order_status", out _);
        foundry.Properties.TryRemove("processing_start", out _);
        return Task.CompletedTask;
    }

    public void Dispose() { }
}

public class PremiumOrderProcessingOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "PremiumOrderProcessing";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("   [PREMIUM] Processing high-value order with premium service...");
        await Task.Delay(150, cancellationToken);
        
        foundry.Properties["processing_type"] = "Premium";
        foundry.Properties["priority_level"] = "High";
        foundry.Properties["estimated_completion"] = DateTime.UtcNow.AddHours(1);
        foundry.Properties["premium_benefits"] = "Express processing, dedicated support, priority fulfillment";
        
        Console.WriteLine("   [SUCCESS] Premium processing assigned with express service");
        return "Premium processing assigned";
    }

    public Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        foundry.Properties.TryRemove("processing_type", out _);
        foundry.Properties.TryRemove("priority_level", out _);
        foundry.Properties.TryRemove("estimated_completion", out _);
        foundry.Properties.TryRemove("premium_benefits", out _);
        return Task.CompletedTask;
    }

    public void Dispose() { }
}

public class StandardOrderProcessingOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "StandardOrderProcessing";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("   [STANDARD] Processing order with standard service...");
        await Task.Delay(80, cancellationToken);
        
        foundry.Properties["processing_type"] = "Standard";
        foundry.Properties["priority_level"] = "Normal";
        foundry.Properties["estimated_completion"] = DateTime.UtcNow.AddHours(4);
        
        Console.WriteLine("   [SUCCESS] Standard processing assigned");
        return "Standard processing assigned";
    }

    public Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        foundry.Properties.TryRemove("processing_type", out _);
        foundry.Properties.TryRemove("priority_level", out _);
        foundry.Properties.TryRemove("estimated_completion", out _);
        return Task.CompletedTask;
    }

    public void Dispose() { }
}

public class CreditCardPaymentOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "CreditCardPayment";
    public bool SupportsRestore => true;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("   [PAYMENT] Processing credit card payment...");
        await Task.Delay(120, cancellationToken);
        
        var transactionId = $"CC-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        foundry.Properties["payment_type"] = "CreditCard";
        foundry.Properties["transaction_id"] = transactionId;
        foundry.Properties["payment_status"] = "Processed";
        foundry.Properties["processing_fee"] = 2.9m; // 2.9% processing fee
        
        Console.WriteLine($"   [SUCCESS] Credit card payment processed: {transactionId}");
        return $"Credit card payment: {transactionId}";
    }

    public async Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        var transactionId = foundry.Properties.TryGetValue("transaction_id", out var txnId) ? (string)txnId! : "Unknown";
        Console.WriteLine($"   [REFUND] Refunding credit card transaction {transactionId}...");
        
        await Task.Delay(100, cancellationToken);
        
        foundry.Properties.TryRemove("transaction_id", out _);
        foundry.Properties["payment_status"] = "Refunded";
        
        Console.WriteLine($"   [SUCCESS] Credit card refund completed");
    }

    public void Dispose() { }
}

public class BankTransferPaymentOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "BankTransferPayment";
    public bool SupportsRestore => true;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("   [PAYMENT] Processing bank transfer payment...");
        await Task.Delay(200, cancellationToken);
        
        var transferId = $"BT-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        foundry.Properties["payment_type"] = "BankTransfer";
        foundry.Properties["transfer_id"] = transferId;
        foundry.Properties["payment_status"] = "Pending"; // Bank transfers take longer
        foundry.Properties["processing_fee"] = 0.5m; // Lower processing fee
        foundry.Properties["clearing_time"] = "1-3 business days";
        
        Console.WriteLine($"   [SUCCESS] Bank transfer initiated: {transferId} (clearing in 1-3 business days)");
        return $"Bank transfer: {transferId}";
    }

    public async Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        var transferId = foundry.Properties.TryGetValue("transfer_id", out var tid) ? (string)tid! : "Unknown";
        Console.WriteLine($"   [REVERSAL] Reversing bank transfer {transferId}...");
        
        await Task.Delay(150, cancellationToken);
        
        foundry.Properties.TryRemove("transfer_id", out _);
        foundry.Properties["payment_status"] = "Reversed";
        
        Console.WriteLine($"   [SUCCESS] Bank transfer reversal completed");
    }

    public void Dispose() { }
}

public class FinalizeOrderOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "FinalizeOrder";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("   [INFO] Finalizing order...");
        
        await Task.Delay(60, cancellationToken);
        
        var processingType = foundry.Properties.TryGetValue("processing_type", out var pt) ? (string)pt! : "Unknown";
        var completion = foundry.Properties.TryGetValue("estimated_completion", out var ec) ? (DateTime)ec! : DateTime.UtcNow;
        
        foundry.Properties["order_status"] = "Finalized";
        foundry.Properties["completion_time"] = DateTime.UtcNow;
        
        var duration = DateTime.UtcNow - (DateTime)foundry.Properties["processing_start"]!;
        Console.WriteLine($"   [SUCCESS] Order finalized with {processingType} processing in {duration.TotalMilliseconds:F0}ms");
        Console.WriteLine($"   [INFO] Estimated completion: {completion:yyyy-MM-dd HH:mm}");
        
        return "Order finalized";
    }

    public Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        foundry.Properties.TryRemove("order_status", out _);
        foundry.Properties.TryRemove("completion_time", out _);
        return Task.CompletedTask;
    }

    public void Dispose() { }
} 