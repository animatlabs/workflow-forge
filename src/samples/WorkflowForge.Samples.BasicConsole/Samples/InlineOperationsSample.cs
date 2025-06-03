using System;
using System.Threading.Tasks;
using WorkflowForge;
using WorkflowForge.Extensions;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates inline operations - quick operations without creating separate classes.
/// Shows how to use lambda expressions and delegates for simple workflow steps.
/// </summary>
public class InlineOperationsSample : ISample
{
    public string Name => "Inline Operations Workflow";
    public string Description => "Quick operations using lambda expressions and delegates";

    public async Task RunAsync()
    {
        Console.WriteLine("Creating a workflow with inline operations...");
        
        using var foundry = WorkflowForge.CreateFoundry("InlineOperationsWorkflow", FoundryConfiguration.Development());
        
        // Set initial data
        foundry.Properties["user_name"] = "Alice Johnson";
        foundry.Properties["order_total"] = 250.75m;
        foundry.Properties["items_count"] = 3;
        
        Console.WriteLine($"Processing order for {foundry.Properties["user_name"]} - ${foundry.Properties["order_total"]} ({foundry.Properties["items_count"]} items)");
        
        foundry
            // Inline operation using WithOperation extension method
            .WithOperation("ValidateOrder", async (foundry) =>
            {
                Console.WriteLine("   [INFO] Validating order...");
                await Task.Delay(50);
                
                var total = (decimal)foundry.Properties["order_total"]!;
                var isValid = total > 0 && total < 10000m;
                
                foundry.Properties["order_valid"] = isValid;
                foundry.Properties["validation_message"] = isValid ? "Order is valid" : "Order validation failed";
                
                Console.WriteLine($"   [SUCCESS] {foundry.Properties["validation_message"]}");
            })
            
            // Another inline operation with conditional logic
            .WithOperation("CalculateShipping", async (foundry) =>
            {
                Console.WriteLine("   [INFO] Calculating shipping...");
                await Task.Delay(75);
                
                var total = (decimal)foundry.Properties["order_total"]!;
                var itemsCount = (int)foundry.Properties["items_count"]!;
                
                decimal shippingCost = total switch
                {
                    >= 200m => 0m,           // Free shipping
                    >= 100m => 5.99m,        // Reduced shipping
                    _ => 9.99m               // Standard shipping
                };
                
                // Add extra shipping for multiple items
                if (itemsCount > 5)
                {
                    shippingCost += 2m;
                }
                
                foundry.Properties["shipping_cost"] = shippingCost;
                foundry.Properties["free_shipping"] = shippingCost == 0m;
                
                var message = shippingCost == 0m ? "Free shipping applied!" : $"Shipping cost: ${shippingCost:F2}";
                Console.WriteLine($"   [INFO] {message}");
            })
            
            // Inline operation with data transformation
            .WithOperation("GenerateOrderSummary", async (foundry) =>
            {
                Console.WriteLine("   [INFO] Generating order summary...");
                await Task.Delay(60);
                
                var userName = (string)foundry.Properties["user_name"]!;
                var orderTotal = (decimal)foundry.Properties["order_total"]!;
                var shippingCost = (decimal)foundry.Properties["shipping_cost"]!;
                var finalTotal = orderTotal + shippingCost;
                
                var summary = $"""
                   Order Summary for {userName}:
                   - Subtotal: ${orderTotal:F2}
                   - Shipping: ${shippingCost:F2}
                   - Total: ${finalTotal:F2}
                   """;
                
                foundry.Properties["order_summary"] = summary;
                foundry.Properties["final_total"] = finalTotal;
                
                Console.WriteLine($"   [INFO] Order summary generated");
                Console.WriteLine(summary.Replace("\n", "\n     "));
            })
            
            // Inline operation using more complex async logic
            .WithOperation("ProcessPayment", async (foundry) =>
            {
                Console.WriteLine("   [INFO] Processing payment...");
                
                var finalTotal = (decimal)foundry.Properties["final_total"]!;
                var userName = (string)foundry.Properties["user_name"]!;
                
                // Simulate payment processing with multiple steps
                Console.WriteLine("   [INFO] Contacting payment gateway...");
                await Task.Delay(100);
                
                Console.WriteLine("   [INFO] Verifying payment method...");
                await Task.Delay(80);
                
                Console.WriteLine("   [INFO] Authorizing transaction...");
                await Task.Delay(120);
                
                // Generate transaction details
                var transactionId = $"TXN-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(10000, 99999)}";
                var authCode = Random.Shared.Next(100000, 999999).ToString();
                
                foundry.Properties["transaction_id"] = transactionId;
                foundry.Properties["auth_code"] = authCode;
                foundry.Properties["payment_status"] = "Completed";
                foundry.Properties["payment_timestamp"] = DateTime.UtcNow;
                
                Console.WriteLine($"   [SUCCESS] Payment processed! Transaction: {transactionId}, Auth: {authCode}");
            })
            
            // Final inline operation with error handling
            .WithOperation("SendConfirmation", async (foundry) =>
            {
                Console.WriteLine("   [INFO] Sending order confirmation...");
                
                try
                {
                    await Task.Delay(90);
                    
                    var userName = (string)foundry.Properties["user_name"]!;
                    var transactionId = (string)foundry.Properties["transaction_id"]!;
                    var finalTotal = (decimal)foundry.Properties["final_total"]!;
                    
                    var confirmationId = $"CONF-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
                    
                    foundry.Properties["confirmation_id"] = confirmationId;
                    foundry.Properties["confirmation_sent"] = true;
                    foundry.Properties["workflow_completed"] = true;
                    
                    Console.WriteLine($"   [SUCCESS] Order complete! Confirmation: {confirmationId}");
                    Console.WriteLine($"   [INFO] Final amount charged: ${finalTotal:F2}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   [ERROR] Failed to send confirmation: {ex.Message}");
                    foundry.Properties["confirmation_error"] = ex.Message;
                    throw; // Re-throw to be handled by workflow error handling
                }
            });
        
        Console.WriteLine("\nExecuting inline operations workflow...");
        await foundry.ForgeAsync();
        
        // Show final workflow state
        Console.WriteLine("\n[INFO] Final Workflow State:");
        Console.WriteLine($"   Order Valid: {foundry.Properties["order_valid"]}");
        Console.WriteLine($"   Free Shipping: {foundry.Properties["free_shipping"]}");
        Console.WriteLine($"   Payment Status: {foundry.Properties["payment_status"]}");
        Console.WriteLine($"   Confirmation Sent: {foundry.Properties["confirmation_sent"]}");
        Console.WriteLine($"   Workflow Completed: {foundry.Properties["workflow_completed"]}");
    }
} 