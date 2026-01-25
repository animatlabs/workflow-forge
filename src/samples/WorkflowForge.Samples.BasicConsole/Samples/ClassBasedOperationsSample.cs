using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates class-based operations as the preferred pattern.
/// Shows how operation output flows into the next operation.
/// </summary>
public class ClassBasedOperationsSample : ISample
{
    public string Name => "Class-Based Operations Workflow";
    public string Description => "Preferred pattern using operation classes with output chaining";

    public async Task RunAsync()
    {
        Console.WriteLine("Creating a workflow with class-based operations...");

        using var foundry = WorkflowForge.CreateFoundry("ClassBasedOperationsWorkflow");

        // Set initial data
        foundry.Properties["user_name"] = "Alice Johnson";
        foundry.Properties["order_total"] = 250.75m;
        foundry.Properties["items_count"] = 3;

        Console.WriteLine($"Processing order for {foundry.Properties["user_name"]} - ${foundry.Properties["order_total"]} ({foundry.Properties["items_count"]} items)");

        foundry
            .WithOperation(new ValidateOrderOperation())
            .WithOperation(new CalculateShippingOperation())
            .WithOperation(new GenerateOrderSummaryOperation())
            .WithOperation(new ProcessPaymentOperation())
            .WithOperation(new SendConfirmationOperation());

        Console.WriteLine("\nExecuting class-based operations workflow...");
        await foundry.ForgeAsync();

        // Show final workflow state
        Console.WriteLine("\n[INFO] Final Workflow State:");
        Console.WriteLine($"   Order Valid: {foundry.Properties["order_valid"]}");
        Console.WriteLine($"   Free Shipping: {foundry.Properties["free_shipping"]}");
        Console.WriteLine($"   Payment Status: {foundry.Properties["payment_status"]}");
        Console.WriteLine($"   Confirmation Sent: {foundry.Properties["confirmation_sent"]}");
        Console.WriteLine($"   Workflow Completed: {foundry.Properties["workflow_completed"]}");
    }

    private sealed class OrderContext
    {
        public string UserName { get; init; } = string.Empty;
        public decimal OrderTotal { get; init; }
        public int ItemsCount { get; init; }
        public bool IsValid { get; set; }
        public string ValidationMessage { get; set; } = string.Empty;
        public decimal ShippingCost { get; set; }
        public bool FreeShipping { get; set; }
        public string OrderSummary { get; set; } = string.Empty;
        public decimal FinalTotal { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string AuthCode { get; set; } = string.Empty;
        public string ConfirmationId { get; set; } = string.Empty;
    }

    private sealed class ValidateOrderOperation : IWorkflowOperation
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name => "ValidateOrder";
        public bool SupportsRestore => false;

        public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            Console.WriteLine("   [INFO] Validating order...");
            await Task.Delay(50, cancellationToken);

            var userName = (string)foundry.Properties["user_name"]!;
            var total = (decimal)foundry.Properties["order_total"]!;
            var itemsCount = (int)foundry.Properties["items_count"]!;

            var isValid = total > 0 && total < 10000m;
            var context = new OrderContext
            {
                UserName = userName,
                OrderTotal = total,
                ItemsCount = itemsCount,
                IsValid = isValid,
                ValidationMessage = isValid ? "Order is valid" : "Order validation failed"
            };

            foundry.Properties["order_valid"] = isValid;
            foundry.Properties["validation_message"] = context.ValidationMessage;

            Console.WriteLine($"   [SUCCESS] {context.ValidationMessage}");
            return context;
        }

        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public void Dispose()
        { }
    }

    private sealed class CalculateShippingOperation : IWorkflowOperation
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name => "CalculateShipping";
        public bool SupportsRestore => false;

        public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            var context = inputData as OrderContext ?? throw new InvalidOperationException("Order context missing.");
            Console.WriteLine("   [INFO] Calculating shipping...");
            await Task.Delay(75, cancellationToken);

            decimal shippingCost = context.OrderTotal switch
            {
                >= 200m => 0m,
                >= 100m => 5.99m,
                _ => 9.99m
            };

            if (context.ItemsCount > 5)
            {
                shippingCost += 2m;
            }

            context.ShippingCost = shippingCost;
            context.FreeShipping = shippingCost == 0m;

            foundry.Properties["shipping_cost"] = shippingCost;
            foundry.Properties["free_shipping"] = context.FreeShipping;

            var message = shippingCost == 0m ? "Free shipping applied!" : $"Shipping cost: ${shippingCost:F2}";
            Console.WriteLine($"   [INFO] {message}");
            return context;
        }

        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public void Dispose()
        { }
    }

    private sealed class GenerateOrderSummaryOperation : IWorkflowOperation
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name => "GenerateOrderSummary";
        public bool SupportsRestore => false;

        public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            var context = inputData as OrderContext ?? throw new InvalidOperationException("Order context missing.");
            Console.WriteLine("   [INFO] Generating order summary...");
            await Task.Delay(60, cancellationToken);

            var finalTotal = context.OrderTotal + context.ShippingCost;
            var summary = $"""
               Order Summary for {context.UserName}:
               - Subtotal: ${context.OrderTotal:F2}
               - Shipping: ${context.ShippingCost:F2}
               - Total: ${finalTotal:F2}
               """;

            context.OrderSummary = summary;
            context.FinalTotal = finalTotal;

            foundry.Properties["order_summary"] = summary;
            foundry.Properties["final_total"] = finalTotal;

            Console.WriteLine("   [INFO] Order summary generated");
            Console.WriteLine(summary.Replace("\n", "\n     "));
            return context;
        }

        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public void Dispose()
        { }
    }

    private sealed class ProcessPaymentOperation : IWorkflowOperation
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name => "ProcessPayment";
        public bool SupportsRestore => false;

        public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            var context = inputData as OrderContext ?? throw new InvalidOperationException("Order context missing.");
            Console.WriteLine("   [INFO] Processing payment...");

            Console.WriteLine("   [INFO] Contacting payment gateway...");
            await Task.Delay(100, cancellationToken);

            Console.WriteLine("   [INFO] Verifying payment method...");
            await Task.Delay(80, cancellationToken);

            Console.WriteLine("   [INFO] Authorizing transaction...");
            await Task.Delay(120, cancellationToken);

            context.TransactionId = $"TXN-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(10000, 99999)}";
            context.AuthCode = Random.Shared.Next(100000, 999999).ToString();

            foundry.Properties["transaction_id"] = context.TransactionId;
            foundry.Properties["auth_code"] = context.AuthCode;
            foundry.Properties["payment_status"] = "Completed";
            foundry.Properties["payment_timestamp"] = DateTime.UtcNow;

            Console.WriteLine($"   [SUCCESS] Payment processed! Transaction: {context.TransactionId}, Auth: {context.AuthCode}");
            return context;
        }

        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public void Dispose()
        { }
    }

    private sealed class SendConfirmationOperation : IWorkflowOperation
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name => "SendConfirmation";
        public bool SupportsRestore => false;

        public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            var context = inputData as OrderContext ?? throw new InvalidOperationException("Order context missing.");
            Console.WriteLine("   [INFO] Sending order confirmation...");

            await Task.Delay(90, cancellationToken);

            context.ConfirmationId = $"CONF-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
            foundry.Properties["confirmation_id"] = context.ConfirmationId;
            foundry.Properties["confirmation_sent"] = true;
            foundry.Properties["workflow_completed"] = true;

            Console.WriteLine($"   [SUCCESS] Order complete! Confirmation: {context.ConfirmationId}");
            Console.WriteLine($"   [INFO] Final amount charged: ${context.FinalTotal:F2}");
            return context;
        }

        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public void Dispose()
        { }
    }
}
