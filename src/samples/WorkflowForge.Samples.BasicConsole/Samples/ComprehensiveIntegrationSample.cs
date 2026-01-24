using Serilog;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Extensions.Observability.OpenTelemetry;
using WorkflowForge.Extensions.Resilience.Polly;
using WorkflowForge.Extensions.Resilience.Polly.Options;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates a comprehensive integration of multiple WorkflowForge extensions
/// in a real-world e-commerce order processing scenario.
///
/// This sample combines:
/// - Serilog for structured logging
/// - Polly for resilience patterns
/// - OpenTelemetry for observability
/// - Custom operations with error handling
/// </summary>
public class ComprehensiveIntegrationSample : ISample
{
    public string Name => "Comprehensive Integration";
    public string Description => "Real-world scenario combining Serilog, Polly, and OpenTelemetry";

    public async Task RunAsync()
    {
        Console.WriteLine("Demonstrating comprehensive WorkflowForge integration...");
        Console.WriteLine("This sample combines multiple extensions in a realistic e-commerce scenario.");

        // Configure Serilog for enterprise logging
        var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "ECommerce.OrderProcessing")
            .Enrich.WithProperty("Environment", "Production")
            .Enrich.WithProperty("Version", "2.1.0")
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] [{Application}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            // Create foundry (Serilog configured globally)
            using var foundry = WorkflowForge.CreateFoundry("ECommerceOrderProcessing");

            // Enable Polly resilience patterns
            foundry.UsePollyFromSettings(new PollyMiddlewareOptions 
            {
                Retry = { MaxRetryAttempts = 5 },
                CircuitBreaker = { IsEnabled = true }
            });

            // Enable OpenTelemetry observability
            var telemetryOptions = new WorkflowForgeOpenTelemetryOptions
            {
                ServiceName = "ECommerce.OrderProcessing",
                ServiceVersion = "2.1.0",
                EnableTracing = true,
                EnableMetrics = true,
                EnableSystemMetrics = true,
                EnableOperationMetrics = true
            };
            foundry.EnableOpenTelemetry(telemetryOptions);

            // Set up order context
            var orderId = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
            var customerId = "CUST-12345";
            var correlationId = Guid.NewGuid().ToString();

            foundry.Properties["order_id"] = orderId;
            foundry.Properties["customer_id"] = customerId;
            foundry.Properties["correlation_id"] = correlationId;
            foundry.Properties["order_amount"] = 299.99m;
            foundry.Properties["currency"] = "USD";
            foundry.Properties["priority"] = "high";

            // Build comprehensive workflow
            foundry
                .WithOperation(new OrderValidationWithResilienceOperation())
                .WithOperation(new PaymentProcessingWithRetryOperation())
                .WithOperation(new InventoryReservationOperation())
                .WithOperation(new ShippingArrangementOperation())
                .WithOperation(new NotificationDispatchOperation())
                .WithOperation(new OrderFinalizationOperation());

            Console.WriteLine($"\n[START] Processing order {orderId} for customer {customerId}");
            Console.WriteLine("[INFO] Monitoring: Structured logs, distributed traces, and metrics enabled");
            Console.WriteLine("[INFO] Resilience: Retry policies, circuit breakers, and timeouts active");
            Console.WriteLine();

            await foundry.ForgeAsync();

            Console.WriteLine("\n[SUCCESS] Comprehensive integration sample completed successfully!");
            Console.WriteLine("[INFO] All extensions worked together seamlessly");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERROR] Comprehensive integration failed: {ex.Message}");
            logger.Error(ex, "Comprehensive integration sample failed");
        }
        finally
        {
            logger.Dispose();
        }
    }
}

/// <summary>
/// Order validation with comprehensive resilience and observability
/// </summary>
public class OrderValidationWithResilienceOperation : IWorkflowOperation
{
    private static int _attemptCount = 0;

    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "OrderValidationWithResilience";
    public bool SupportsRestore => true;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var orderId = foundry.Properties["order_id"] as string ?? "unknown";
        var customerId = foundry.Properties["customer_id"] as string ?? "unknown";
        var correlationId = foundry.Properties["correlation_id"] as string ?? "unknown";
        var telemetryService = foundry.GetOpenTelemetryService();

        using var activity = telemetryService?.StartActivity("order.validation.comprehensive");
        activity?.SetTag("order.id", orderId);
        activity?.SetTag("customer.id", customerId);
        activity?.SetTag("correlation.id", correlationId);

        using var scope = foundry.Logger.BeginScope(Name, new Dictionary<string, string>
        {
            ["OrderId"] = orderId,
            ["CustomerId"] = customerId,
            ["CorrelationId"] = correlationId,
            ["Operation"] = "ComprehensiveValidation"
        });

        foundry.Logger.LogInformation("Starting comprehensive order validation for order {OrderId}", orderId);

        _attemptCount++;

        // Simulate validation that might fail initially (for resilience demonstration)
        if (_attemptCount == 1)
        {
            foundry.Logger.LogWarning("Validation service temporarily unavailable, will be retried by Polly");
            throw new ExternalServiceException("Validation service timeout - will retry");
        }

        // Simulate comprehensive validation steps
        var validationSteps = new[]
        {
            ("customer_verification", 200),
            ("fraud_detection", 350),
            ("business_rules", 150),
            ("compliance_check", 100)
        };

        var validationResults = new List<object>();

        foreach (var (stepName, duration) in validationSteps)
        {
            using var stepActivity = telemetryService?.StartActivity($"validation.{stepName}");
            stepActivity?.SetTag("step.name", stepName);

            foundry.Logger.LogDebug("Executing validation step: {StepName}", stepName);

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

            foundry.Logger.LogDebug("Validation step {StepName} completed successfully", stepName);
        }

        var validationSummary = new
        {
            OrderId = orderId,
            CustomerId = customerId,
            ValidationSteps = validationResults,
            TotalDuration = validationSteps.Sum(s => s.Item2),
            Status = "Validated",
            AttemptCount = _attemptCount,
            Timestamp = DateTime.UtcNow
        };

        foundry.RecordOperationMetrics(Name, TimeSpan.FromMilliseconds(validationSummary.TotalDuration), true);
        activity?.SetTag("validation.status", "success");
        activity?.SetTag("validation.attempt_count", _attemptCount.ToString());

        foundry.Properties["validation_result"] = validationSummary;

        foundry.Logger.LogInformation("Order validation completed successfully in {TotalDuration}ms (attempt {AttemptCount})",
            validationSummary.TotalDuration, _attemptCount);

        return validationSummary;
    }

    public async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var orderId = foundry.Properties["order_id"] as string ?? "unknown";

        foundry.Logger.LogWarning("Restoring order validation for order {OrderId}", orderId);

        await Task.Delay(50, cancellationToken);

        foundry.Properties.TryRemove("validation_result", out _);

        foundry.Logger.LogInformation("Order validation restoration completed");
    }

    public void Dispose()
    { }
}

/// <summary>
/// Payment processing with retry logic and comprehensive monitoring
/// </summary>
public class PaymentProcessingWithRetryOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "PaymentProcessingWithRetry";
    public bool SupportsRestore => true;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var validationData = inputData as dynamic;
        var orderId = validationData?.OrderId ?? "unknown";
        var amount = foundry.Properties["order_amount"] as decimal? ?? 0m;
        var currency = foundry.Properties["currency"] as string ?? "USD";
        var telemetryService = foundry.GetOpenTelemetryService();

        using var activity = telemetryService?.StartActivity("payment.processing.comprehensive");
        activity?.SetTag("order.id", orderId);
        activity?.SetTag("payment.amount", amount.ToString());
        activity?.SetTag("payment.currency", currency);

        using var scope = foundry.Logger.BeginScope(Name, new Dictionary<string, string>
        {
            ["OrderId"] = orderId,
            ["Amount"] = amount.ToString(),
            ["Currency"] = currency,
            ["Operation"] = "PaymentProcessing"
        });

        foundry.Logger.LogInformation("Processing payment for order {OrderId}: {Amount} {Currency}", orderId, amount, currency);

        // Simulate payment processing
        await Task.Delay(600, cancellationToken);

        var paymentResult = new
        {
            OrderId = orderId,
            PaymentId = $"PAY-{Guid.NewGuid().ToString("N")[..12].ToUpper()}",
            Amount = amount,
            Currency = currency,
            ProcessorResponse = "APPROVED",
            TransactionId = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}",
            ProcessingTime = 600,
            Status = "Processed",
            Timestamp = DateTime.UtcNow
        };

        foundry.RecordOperationMetrics(Name, TimeSpan.FromMilliseconds(600), true);
        activity?.SetTag("payment.id", paymentResult.PaymentId);
        activity?.SetTag("payment.status", "approved");
        activity?.SetTag("transaction.id", paymentResult.TransactionId);

        foundry.Properties["payment_result"] = paymentResult;

        foundry.Logger.LogInformation("Payment processed successfully: {PaymentId} for {Amount} {Currency}",
            paymentResult.PaymentId, paymentResult.Amount, paymentResult.Currency);

        return paymentResult;
    }

    public async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var paymentData = outputData as dynamic;
        var paymentId = paymentData?.PaymentId ?? "unknown";

        foundry.Logger.LogWarning("Initiating payment reversal for payment {PaymentId}", paymentId);

        await Task.Delay(200, cancellationToken);

        foundry.Properties.TryRemove("payment_result", out _);

        foundry.Logger.LogInformation("Payment reversal completed for payment {PaymentId}", paymentId);
    }

    public void Dispose()
    { }
}

/// <summary>
/// Inventory reservation with observability
/// </summary>
public class InventoryReservationOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "InventoryReservation";
    public bool SupportsRestore => true;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var paymentData = inputData as dynamic;
        var orderId = paymentData?.OrderId ?? "unknown";
        var telemetryService = foundry.GetOpenTelemetryService();

        using var activity = telemetryService?.StartActivity("inventory.reservation");
        activity?.SetTag("order.id", orderId);

        foundry.Logger.LogInformation("Reserving inventory for order {OrderId}", orderId);

        await Task.Delay(250, cancellationToken);

        var inventoryResult = new
        {
            OrderId = orderId,
            ReservationId = $"RES-{Guid.NewGuid().ToString("N")[..10].ToUpper()}",
            ItemsReserved = 1,
            WarehouseLocation = "WH-CENTRAL-001",
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            Status = "Reserved",
            Timestamp = DateTime.UtcNow
        };

        foundry.RecordOperationMetrics(Name, TimeSpan.FromMilliseconds(250), true);
        activity?.SetTag("reservation.id", inventoryResult.ReservationId);
        activity?.SetTag("warehouse.location", inventoryResult.WarehouseLocation);

        foundry.Properties["inventory_result"] = inventoryResult;

        foundry.Logger.LogInformation("Inventory reserved: {ReservationId} at {WarehouseLocation}",
            inventoryResult.ReservationId, inventoryResult.WarehouseLocation);

        return inventoryResult;
    }

    public async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var inventoryData = outputData as dynamic;
        var reservationId = inventoryData?.ReservationId ?? "unknown";

        foundry.Logger.LogWarning("Releasing inventory reservation {ReservationId}", reservationId);

        await Task.Delay(100, cancellationToken);

        foundry.Properties.TryRemove("inventory_result", out _);

        foundry.Logger.LogInformation("Inventory reservation released: {ReservationId}", reservationId);
    }

    public void Dispose()
    { }
}

/// <summary>
/// Shipping arrangement operation
/// </summary>
public class ShippingArrangementOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "ShippingArrangement";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var inventoryData = inputData as dynamic;
        var orderId = inventoryData?.OrderId ?? "unknown";
        var telemetryService = foundry.GetOpenTelemetryService();

        using var activity = telemetryService?.StartActivity("shipping.arrangement");
        activity?.SetTag("order.id", orderId);

        foundry.Logger.LogInformation("Arranging shipping for order {OrderId}", orderId);

        await Task.Delay(300, cancellationToken);

        var shippingResult = new
        {
            OrderId = orderId,
            ShippingId = $"SHIP-{Guid.NewGuid().ToString("N")[..12].ToUpper()}",
            Carrier = "FastShip Express",
            TrackingNumber = $"FS{DateTime.UtcNow:yyyyMMdd}{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            EstimatedDelivery = DateTime.UtcNow.AddDays(2),
            ShippingCost = 15.99m,
            Status = "Arranged",
            Timestamp = DateTime.UtcNow
        };

        foundry.RecordOperationMetrics(Name, TimeSpan.FromMilliseconds(300), true);
        activity?.SetTag("shipping.id", shippingResult.ShippingId);
        activity?.SetTag("tracking.number", shippingResult.TrackingNumber);
        activity?.SetTag("carrier", shippingResult.Carrier);

        foundry.Properties["shipping_result"] = shippingResult;

        foundry.Logger.LogInformation("Shipping arranged: {TrackingNumber} via {Carrier}",
            shippingResult.TrackingNumber, shippingResult.Carrier);

        return shippingResult;
    }

    public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Shipping arrangement does not support restoration");
    }

    public void Dispose()
    { }
}

/// <summary>
/// Notification dispatch operation
/// </summary>
public class NotificationDispatchOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "NotificationDispatch";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var shippingData = inputData as dynamic;
        var orderId = shippingData?.OrderId ?? "unknown";
        var customerId = foundry.Properties["customer_id"] as string ?? "unknown";
        var telemetryService = foundry.GetOpenTelemetryService();

        using var activity = telemetryService?.StartActivity("notification.dispatch");
        activity?.SetTag("order.id", orderId);
        activity?.SetTag("customer.id", customerId);

        foundry.Logger.LogInformation("Dispatching notifications for order {OrderId}", orderId);

        // Simulate sending multiple notifications
        var notifications = new[]
        {
            ("email", "Order Confirmation", 150),
            ("sms", "Shipping Update", 100),
            ("push", "Order Status", 75)
        };

        var notificationResults = new List<object>();

        foreach (var (channel, template, delay) in notifications)
        {
            using var notificationActivity = telemetryService?.StartActivity($"notification.{channel}");
            notificationActivity?.SetTag("channel", channel);
            notificationActivity?.SetTag("template", template);

            await Task.Delay(delay, cancellationToken);

            var notificationResult = new
            {
                Channel = channel,
                Template = template,
                MessageId = $"MSG-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                Status = "Sent",
                Timestamp = DateTime.UtcNow
            };

            notificationResults.Add(notificationResult);
            notificationActivity?.SetTag("message.id", notificationResult.MessageId);

            foundry.Logger.LogDebug("Notification sent via {Channel}: {MessageId}", channel, notificationResult.MessageId);
        }

        var dispatchSummary = new
        {
            OrderId = orderId,
            CustomerId = customerId,
            NotificationsSent = notificationResults,
            TotalNotifications = notificationResults.Count,
            Status = "Dispatched",
            Timestamp = DateTime.UtcNow
        };

        foundry.RecordOperationMetrics(Name, TimeSpan.FromMilliseconds(325), true);
        activity?.SetTag("notifications.count", dispatchSummary.TotalNotifications.ToString());

        foundry.Properties["notification_result"] = dispatchSummary;

        foundry.Logger.LogInformation("Notifications dispatched: {TotalNotifications} messages sent for order {OrderId}",
            dispatchSummary.TotalNotifications, orderId);

        return dispatchSummary;
    }

    public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Notification dispatch does not support restoration");
    }

    public void Dispose()
    { }
}

/// <summary>
/// Order finalization operation with comprehensive summary
/// </summary>
public class OrderFinalizationOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "OrderFinalization";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var notificationData = inputData as dynamic;
        var orderId = notificationData?.OrderId ?? "unknown";
        var customerId = foundry.Properties["customer_id"] as string ?? "unknown";
        var correlationId = foundry.Properties["correlation_id"] as string ?? "unknown";
        var telemetryService = foundry.GetOpenTelemetryService();

        using var activity = telemetryService?.StartActivity("order.finalization");
        activity?.SetTag("order.id", orderId);
        activity?.SetTag("customer.id", customerId);
        activity?.SetTag("correlation.id", correlationId);

        foundry.Logger.LogInformation("Finalizing order {OrderId}", orderId);

        await Task.Delay(100, cancellationToken);

        var finalizationSummary = new
        {
            OrderId = orderId,
            CustomerId = customerId,
            CorrelationId = correlationId,
            Status = "Completed",
            CompletedAt = DateTime.UtcNow,
            ProcessingSteps = new[]
            {
                "Validation",
                "Payment",
                "Inventory",
                "Shipping",
                "Notifications",
                "Finalization"
            },
            TotalSteps = 6,
            WorkflowDuration = "~2.5 seconds",
            ExtensionsUsed = new[]
            {
                "Serilog Logging",
                "Polly Resilience",
                "OpenTelemetry Observability"
            }
        };

        foundry.RecordOperationMetrics(Name, TimeSpan.FromMilliseconds(100), true);
        activity?.SetTag("order.status", "completed");
        activity?.SetTag("processing.steps", finalizationSummary.TotalSteps.ToString());

        foundry.Properties["finalization_result"] = finalizationSummary;

        Console.WriteLine($"\n[COMPLETE] Order Processing Complete!");
        Console.WriteLine($"   [SUCCESS] Status: {finalizationSummary.Status}");
        Console.WriteLine($"   [INFO] Steps: {finalizationSummary.TotalSteps} operations completed");
        Console.WriteLine($"   [EXTENSIONS] Extensions: {string.Join(", ", finalizationSummary.ExtensionsUsed)}");

        foundry.Logger.LogInformation("Order finalization completed successfully for order {OrderId}", orderId);

        return finalizationSummary;
    }

    public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Order finalization does not support restoration");
    }

    public void Dispose()
    { }
}