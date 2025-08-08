using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Operations;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates data passing between workflow operations.
/// Shows how to pass data through the foundry's data dictionary.
/// Equivalent to workflow-core's data passing examples.
/// </summary>
public class DataPassingSample : ISample
{
    public string Name => "Data Passing Workflow";
    public string Description => "Demonstrates data flow between workflow operations";

    public async Task RunAsync()
    {
        Console.WriteLine("Creating a workflow that passes data between operations...");
        
        // Step 1: Create a workflow using WorkflowBuilder
        var workflow = WorkflowForge.CreateWorkflow()
            .WithName("DataPassingWorkflow")
            .WithDescription("Demonstrates data flow between workflow operations")
            .AddOperation(new ValidateCustomerOperation())
            .AddOperation(new CalculateDiscountOperation())
            .AddOperation(new ProcessPaymentOperation())
            .AddOperation(new GenerateReceiptOperation())
            .Build();
        
        // Step 2: Create foundry with initial data
        var initialData = new Dictionary<string, object?>
        {
            ["customer_id"] = 12345,
            ["order_amount"] = 99.50m
        };
        using var foundry = WorkflowForge.CreateFoundryWithData("DataPassingWorkflow", initialData, FoundryConfiguration.Development());
        
        Console.WriteLine($"Initial data - Customer: {foundry.GetPropertyOrDefault<int>("customer_id")}, Amount: ${foundry.GetPropertyOrDefault<decimal>("order_amount")}");
        
        // Step 3: Create a smith to execute the workflow
        using var smith = WorkflowForge.CreateSmith();
        
        Console.WriteLine("\nExecuting workflow...");
        
        // Step 4: Execute the workflow
        await smith.ForgeAsync(workflow, foundry);
        
        // Show final data
        Console.WriteLine($"\nFinal result - Receipt: {foundry.GetPropertyOrDefault<string>("receipt_number", "N/A")}");
        Console.WriteLine($"Final amount: ${foundry.GetPropertyOrDefault<decimal>("final_amount", 0m)}");
    }
}

public class ValidateCustomerOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "ValidateCustomer";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        var customerId = foundry.GetPropertyOrDefault<int>("customer_id");
        Console.WriteLine($"   [INFO] Validating customer {customerId}...");
        
        // Simulate validation
        await Task.Delay(100, cancellationToken);
        
        // Set customer details
        foundry.SetProperty("customer_name", "John Doe");
        foundry.SetProperty("customer_tier", "Gold");
        
        Console.WriteLine($"   [SUCCESS] Customer validated: {foundry.GetPropertyOrDefault<string>("customer_name")} ({foundry.GetPropertyOrDefault<string>("customer_tier")} tier)");
        return "Customer validated";
    }

    public Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        foundry.Properties.TryRemove("customer_name", out _);
        foundry.Properties.TryRemove("customer_tier", out _);
        return Task.CompletedTask;
    }

    public void Dispose() { }
}

public class CalculateDiscountOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "CalculateDiscount";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        var amount = foundry.GetPropertyOrDefault<decimal>("order_amount");
        var tier = foundry.GetPropertyOrDefault<string>("customer_tier", string.Empty);
        
        Console.WriteLine($"   [INFO] Calculating discount for {tier} customer on ${amount}...");
        
        // Simulate calculation
        await Task.Delay(80, cancellationToken);
        
        var discount = tier switch
        {
            "Gold" => 0.15m,
            "Silver" => 0.10m,
            "Bronze" => 0.05m,
            _ => 0.0m
        };
        
        var discountAmount = amount * discount;
        var finalAmount = amount - discountAmount;
        
        foundry.SetProperty("discount_percent", discount * 100);
        foundry.SetProperty("discount_amount", discountAmount);
        foundry.SetProperty("final_amount", finalAmount);
        
        Console.WriteLine($"   [SUCCESS] Applied {discount * 100}% discount: ${discountAmount:F2} off, final: ${finalAmount:F2}");
        return $"Discount calculated: {discount * 100}%";
    }

    public Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        foundry.Properties.TryRemove("discount_percent", out _);
        foundry.Properties.TryRemove("discount_amount", out _);
        foundry.Properties.TryRemove("final_amount", out _);
        return Task.CompletedTask;
    }

    public void Dispose() { }
}

public class ProcessPaymentOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "ProcessPayment";
    public bool SupportsRestore => true;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        var amount = foundry.GetPropertyOrDefault<decimal>("final_amount");
        var customerName = foundry.GetPropertyOrDefault<string>("customer_name", string.Empty);
        
        Console.WriteLine($"   [INFO] Processing payment of ${amount:F2} for {customerName}...");
        
        // Simulate payment processing
        await Task.Delay(200, cancellationToken);
        
        var transactionId = $"TXN-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        foundry.SetProperty("transaction_id", transactionId);
        foundry.SetProperty("payment_status", "Completed");
        
        Console.WriteLine($"   [SUCCESS] Payment processed successfully! Transaction ID: {transactionId}");
        return $"Payment processed: {transactionId}";
    }

    public async Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        var transactionId = foundry.GetPropertyOrDefault<string>("transaction_id", "Unknown");
        Console.WriteLine($"   [REFUND] Refunding transaction {transactionId}...");
        
        await Task.Delay(100, cancellationToken);
        
        foundry.Properties.TryRemove("transaction_id", out _);
        foundry.SetProperty("payment_status", "Refunded");
        
        Console.WriteLine($"   [SUCCESS] Transaction {transactionId} refunded successfully");
    }

    public void Dispose() { }
}

public class GenerateReceiptOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "GenerateReceipt";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        var customerName = foundry.GetPropertyOrDefault<string>("customer_name", string.Empty);
        var transactionId = foundry.GetPropertyOrDefault<string>("transaction_id", string.Empty);
        
        Console.WriteLine($"   [INFO] Generating receipt for {customerName}...");
        
        // Simulate receipt generation
        await Task.Delay(60, cancellationToken);
        
        var receiptNumber = $"RCP-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        foundry.SetProperty("receipt_number", receiptNumber);
        
        Console.WriteLine($"   [SUCCESS] Receipt generated: {receiptNumber}");
        return $"Receipt: {receiptNumber}";
    }

    public Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        foundry.Properties.TryRemove("receipt_number", out _);
        return Task.CompletedTask;
    }

    public void Dispose() { }
} 