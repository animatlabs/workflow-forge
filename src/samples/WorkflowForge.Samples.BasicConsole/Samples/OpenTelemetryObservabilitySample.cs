using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Extensions.Observability.OpenTelemetry;
using WorkflowForge.Operations;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates OpenTelemetry integration with WorkflowForge for comprehensive observability.
/// Shows distributed tracing, metrics collection, and performance monitoring.
/// </summary>
public class OpenTelemetryObservabilitySample : ISample
{
    public string Name => "OpenTelemetry Observability";
    public string Description => "Distributed tracing and metrics with OpenTelemetry extension";

    public async Task RunAsync()
    {
        Console.WriteLine("Demonstrating OpenTelemetry observability patterns...");

        // Scenario 1: Basic tracing and metrics
        await RunBasicObservabilityScenario();

        // Scenario 2: Advanced observability with custom metrics
        await RunAdvancedObservabilityScenario();

        // Scenario 3: Performance monitoring scenario
        await RunPerformanceMonitoringScenario();
    }

    private static async Task RunBasicObservabilityScenario()
    {
        Console.WriteLine("\n--- Basic Observability Scenario ---");

        using var foundry = WorkflowForge.CreateFoundry("BasicObservability");

        // Enable OpenTelemetry with basic configuration
        var options = new WorkflowForgeOpenTelemetryOptions
        {
            ServiceName = "WorkflowForge.BasicSample",
            ServiceVersion = "1.0.0",
            EnableTracing = true,
            EnableMetrics = true
        };

        foundry.EnableOpenTelemetry(options);

        foundry.Properties["scenario"] = "basic";
        foundry.Properties["customer_id"] = "cust_12345";
        foundry.Properties["order_id"] = "ord_67890";

        foundry
            .WithOperation(new OrderValidationOperation())
            .WithOperation(new PaymentProcessingOperation())
            .WithOperation(new InventoryCheckOperation())
            .WithOperation(new OrderCompletionOperation());

        try
        {
            Console.WriteLine("Executing workflow with basic observability...");
            await foundry.ForgeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Basic observability workflow failed: {ex.Message}");
        }
    }

    private static async Task RunAdvancedObservabilityScenario()
    {
        Console.WriteLine("\n--- Advanced Observability Scenario ---");

        using var foundry = WorkflowForge.CreateFoundry("AdvancedObservability");

        // Enable OpenTelemetry with advanced configuration
        var options = new WorkflowForgeOpenTelemetryOptions
        {
            ServiceName = "WorkflowForge.AdvancedSample",
            ServiceVersion = "2.0.0",
            EnableTracing = true,
            EnableMetrics = true,
            EnableSystemMetrics = true,
            EnableOperationMetrics = true
        };

        foundry.EnableOpenTelemetry(options);

        foundry.Properties["scenario"] = "advanced";
        foundry.Properties["customer_id"] = "cust_54321";
        foundry.Properties["order_id"] = "ord_09876";
        foundry.Properties["priority"] = "high";

        foundry
            .WithOperation(new OrderValidationOperation())
            .WithOperation(new PaymentProcessingOperation())
            .WithOperation(new InventoryCheckOperation())
            .WithOperation(new OrderCompletionOperation());

        try
        {
            Console.WriteLine("Executing workflow with advanced observability...");
            await foundry.ForgeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Advanced observability workflow failed: {ex.Message}");
        }
    }

    private static async Task RunPerformanceMonitoringScenario()
    {
        Console.WriteLine("\n--- Performance Monitoring Scenario ---");

        using var foundry = WorkflowForge.CreateFoundry("PerformanceMonitoring");

        // Enable OpenTelemetry with performance focus
        var options = new WorkflowForgeOpenTelemetryOptions
        {
            ServiceName = "WorkflowForge.PerformanceSample",
            ServiceVersion = "1.5.0",
            EnableTracing = true,
            EnableMetrics = true,
            EnableSystemMetrics = true,
            EnableOperationMetrics = true
        };

        foundry.EnableOpenTelemetry(options);

        foundry.Properties["scenario"] = "performance";
        foundry.Properties["customer_id"] = "cust_99999";
        foundry.Properties["order_id"] = "ord_11111";
        foundry.Properties["batch_size"] = 100;

        foundry
            .WithOperation(new OrderValidationOperation())
            .WithOperation(new PaymentProcessingOperation())
            .WithOperation(new InventoryCheckOperation())
            .WithOperation(new OrderCompletionOperation());

        try
        {
            Console.WriteLine("Executing workflow with performance monitoring...");
            await foundry.ForgeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Performance monitoring workflow failed: {ex.Message}");
        }
    }
}

/// <summary>
/// Order validation operation with OpenTelemetry tracing
/// </summary>
public class OrderValidationOperation : WorkflowOperationBase
{
    public override string Name => "OrderValidation";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var scenario = foundry.Properties["scenario"] as string ?? "unknown";
        var customerId = foundry.Properties["customer_id"] as string ?? "unknown";
        var orderId = foundry.Properties["order_id"] as string ?? "unknown";

        // Get OpenTelemetry service for custom metrics
        var telemetryService = foundry.GetOpenTelemetryService();

        foundry.Logger.LogInformation("Starting order validation for customer {CustomerId}, order {OrderId}", customerId, orderId);

        // Start a custom activity for detailed tracing
        using var activity = telemetryService?.StartActivity("order.validation");
        activity?.SetTag("customer.id", customerId);
        activity?.SetTag("order.id", orderId);
        activity?.SetTag("scenario", scenario);

        // Simulate validation steps with different durations
        var validationSteps = new[]
        {
            ("customer_verification", 150),
            ("order_format_check", 75),
            ("business_rules_validation", 200),
            ("fraud_detection", 300)
        };

        var validationResults = new List<object>();

        foreach (var (stepName, duration) in validationSteps)
        {
            foundry.Logger.LogDebug("Executing validation step: {StepName}", stepName);

            // Create sub-activity for each validation step
            using var stepActivity = telemetryService?.StartActivity($"validation.{stepName}");
            stepActivity?.SetTag("step.name", stepName);
            stepActivity?.SetTag("expected.duration", duration.ToString());

            await Task.Delay(duration, cancellationToken);

            var stepResult = new
            {
                StepName = stepName,
                Status = "Passed",
                Duration = duration,
                Timestamp = DateTime.UtcNow
            };

            validationResults.Add(stepResult);
            stepActivity?.SetTag("step.status", "passed");

            foundry.Logger.LogDebug("Validation step {StepName} completed in {Duration}ms", stepName, duration);
        }

        var validationSummary = new
        {
            CustomerId = customerId,
            OrderId = orderId,
            Scenario = scenario,
            ValidationSteps = validationResults,
            TotalDuration = validationSteps.Sum(s => s.Item2),
            Status = "Validated",
            Timestamp = DateTime.UtcNow
        };

        // Record custom metrics
        foundry.RecordOperationMetrics(Name, TimeSpan.FromMilliseconds(validationSummary.TotalDuration), true);

        activity?.SetTag("validation.status", "success");
        activity?.SetTag("validation.total_duration", validationSummary.TotalDuration.ToString());

        foundry.Properties["validation_result"] = validationSummary;
        foundry.Logger.LogInformation("Order validation completed successfully in {TotalDuration}ms", validationSummary.TotalDuration);

        return validationSummary;
    }

    public override async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var orderId = foundry.Properties["order_id"] as string ?? "unknown";
        var telemetryService = foundry.GetOpenTelemetryService();

        using var activity = telemetryService?.StartActivity("order.validation.restore");
        activity?.SetTag("order.id", orderId);

        foundry.Logger.LogWarning("Restoring order validation for order {OrderId}", orderId);

        await Task.Delay(50, cancellationToken);

        foundry.Properties.TryRemove("validation_result", out _);

        activity?.SetTag("restore.status", "completed");
        foundry.Logger.LogInformation("Order validation restoration completed for order {OrderId}", orderId);
    }
}

/// <summary>
/// Payment processing operation with performance metrics
/// </summary>
public class PaymentProcessingOperation : WorkflowOperationBase
{
    public override string Name => "PaymentProcessing";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var validationData = inputData as dynamic;
        var scenario = foundry.Properties["scenario"] as string ?? "unknown";
        var telemetryService = foundry.GetOpenTelemetryService();

        using var activity = telemetryService?.StartActivity("payment.processing");
        activity?.SetTag("scenario", scenario);
        activity?.SetTag("order.id", validationData?.OrderId ?? "unknown");

        foundry.Logger.LogInformation("Processing payment for order {OrderId}", validationData?.OrderId ?? "unknown");

        // Simulate payment processing with different scenarios
        var processingTime = scenario switch
        {
            "basic" => 500,
            "advanced" => 750,
            "performance" => 300,
            _ => 1000
        };

        await Task.Delay(processingTime, cancellationToken);

        var paymentResult = new
        {
            OrderId = validationData?.OrderId ?? "unknown",
            PaymentId = $"pay_{Guid.NewGuid().ToString("N").Substring(0, 8)}",
            Amount = 99.99m,
            Currency = "USD",
            ProcessingTime = processingTime,
            Status = "Processed",
            Timestamp = DateTime.UtcNow
        };

        // Record payment metrics
        foundry.RecordOperationMetrics(Name, TimeSpan.FromMilliseconds(processingTime), true);

        activity?.SetTag("payment.id", paymentResult.PaymentId);
        activity?.SetTag("payment.amount", paymentResult.Amount.ToString());
        activity?.SetTag("payment.status", "success");

        foundry.Properties["payment_result"] = paymentResult;
        foundry.Logger.LogInformation("Payment processed successfully: {PaymentId} for {Amount} {Currency}",
            paymentResult.PaymentId, paymentResult.Amount, paymentResult.Currency);

        return paymentResult;
    }

    public override async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var paymentData = outputData as dynamic;
        var telemetryService = foundry.GetOpenTelemetryService();

        using var activity = telemetryService?.StartActivity("payment.processing.restore");
        activity?.SetTag("payment.id", paymentData?.PaymentId ?? "unknown");

        foundry.Logger.LogWarning("Restoring payment for payment ID {PaymentId}", paymentData?.PaymentId ?? "unknown");

        await Task.Delay(100, cancellationToken);

        foundry.Properties.TryRemove("payment_result", out _);

        activity?.SetTag("restore.status", "completed");
        foundry.Logger.LogInformation("Payment restoration completed for payment ID {PaymentId}", paymentData?.PaymentId ?? "unknown");
    }
}

/// <summary>
/// Inventory check operation with system metrics
/// </summary>
public class InventoryCheckOperation : WorkflowOperationBase
{
    public override string Name => "InventoryCheck";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var paymentData = inputData as dynamic;
        var scenario = foundry.Properties["scenario"] as string ?? "unknown";
        var telemetryService = foundry.GetOpenTelemetryService();

        using var activity = telemetryService?.StartActivity("inventory.check");
        activity?.SetTag("scenario", scenario);
        activity?.SetTag("order.id", paymentData?.OrderId ?? "unknown");

        foundry.Logger.LogInformation("Checking inventory for order {OrderId}", paymentData?.OrderId ?? "unknown");

        // Simulate inventory check
        await Task.Delay(200, cancellationToken);

        var inventoryResult = new
        {
            OrderId = paymentData?.OrderId ?? "unknown",
            ItemsAvailable = true,
            ReservedQuantity = 1,
            WarehouseLocation = "WH-001",
            CheckDuration = 200,
            Status = "Available",
            Timestamp = DateTime.UtcNow
        };

        // Record inventory metrics
        foundry.RecordOperationMetrics(Name, TimeSpan.FromMilliseconds(200), true);

        activity?.SetTag("inventory.status", "available");
        activity?.SetTag("warehouse.location", inventoryResult.WarehouseLocation);

        foundry.Properties["inventory_result"] = inventoryResult;
        foundry.Logger.LogInformation("Inventory check completed: {ReservedQuantity} items reserved at {WarehouseLocation}",
            inventoryResult.ReservedQuantity, inventoryResult.WarehouseLocation);

        return inventoryResult;
    }
}

/// <summary>
/// Order completion operation with comprehensive observability
/// </summary>
public class OrderCompletionOperation : WorkflowOperationBase
{
    public override string Name => "OrderCompletion";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var inventoryData = inputData as dynamic;
        var scenario = foundry.Properties["scenario"] as string ?? "unknown";
        var telemetryService = foundry.GetOpenTelemetryService();

        using var activity = telemetryService?.StartActivity("order.completion");
        activity?.SetTag("scenario", scenario);
        activity?.SetTag("order.id", inventoryData?.OrderId ?? "unknown");

        foundry.Logger.LogInformation("Completing order {OrderId}", inventoryData?.OrderId ?? "unknown");

        // Simulate order completion
        await Task.Delay(150, cancellationToken);

        var completionResult = new
        {
            OrderId = inventoryData?.OrderId ?? "unknown",
            CompletionId = $"comp_{Guid.NewGuid().ToString("N").Substring(0, 8)}",
            Status = "Completed",
            CompletionTime = 150,
            Scenario = scenario,
            TotalWorkflowDuration = DateTime.UtcNow.Subtract(DateTime.UtcNow.AddMilliseconds(-1000)).TotalMilliseconds,
            Timestamp = DateTime.UtcNow
        };

        // Record completion metrics
        foundry.RecordOperationMetrics(Name, TimeSpan.FromMilliseconds(150), true);

        activity?.SetTag("completion.id", completionResult.CompletionId);
        activity?.SetTag("completion.status", "success");
        activity?.SetTag("workflow.total_duration", completionResult.TotalWorkflowDuration.ToString());

        foundry.Properties["completion_result"] = completionResult;

        Console.WriteLine($"   [SUCCESS] {scenario} observability workflow completed successfully!");
        Console.WriteLine($"   [INFO] Order ID: {completionResult.OrderId}");
        Console.WriteLine($"   [INFO] Completion ID: {completionResult.CompletionId}");
        Console.WriteLine($"   [INFO] Total duration: {completionResult.TotalWorkflowDuration:F0}ms");
        Console.WriteLine($"   [INFO] Observability: Traces and metrics recorded");

        foundry.Logger.LogInformation("Order completion finished: {CompletionId} for order {OrderId}",
            completionResult.CompletionId, completionResult.OrderId);

        return completionResult;
    }
}