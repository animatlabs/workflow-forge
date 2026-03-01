using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Operations;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates error handling, retry logic, and workflow resilience.
/// Shows how to handle exceptions and implement retry patterns.
/// </summary>
public class ErrorHandlingSample : ISample
{
    public string Name => "Error Handling Workflow";
    public string Description => "Exception handling, retry logic, and workflow resilience";

    public async Task RunAsync()
    {
        Console.WriteLine("Creating a workflow that demonstrates error handling...");

        // Scenario 1: Recoverable errors with retry
        await RunRetryScenario();

        // Scenario 2: Error handling with alternative paths
        await RunAlternativePathScenario();

        // Scenario 3: Circuit breaker pattern
        await RunCircuitBreakerScenario();
    }

    private static async Task RunRetryScenario()
    {
        Console.WriteLine("\n--- Retry Logic Scenario ---");

        using var foundry = WorkflowForge.CreateFoundry("RetryWorkflow");

        foundry.Properties["max_retries"] = 3;
        foundry.Properties["service_endpoint"] = "https://api.unreliable-service.com";

        foundry
            .WithOperation(new InitializeConnectionOperation())
            .WithOperation(new RetryableExternalServiceOperation())
            .WithOperation(new ValidateResponseOperation())
            .WithOperation(new LogSuccessOperation());

        try
        {
            Console.WriteLine("Executing workflow with retry logic...");
            await foundry.ForgeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Workflow failed after all retries: {ex.Message}");
        }
    }

    private static async Task RunAlternativePathScenario()
    {
        Console.WriteLine("\n--- Alternative Path Scenario ---");

        using var foundry = WorkflowForge.CreateFoundry("AlternativePathWorkflow");

        foundry.Properties["primary_service"] = "MainPaymentGateway";
        foundry.Properties["fallback_service"] = "BackupPaymentGateway";
        foundry.Properties["amount"] = 99.99m;

        foundry
            .WithOperation(new PreparePaymentOperation())
            .WithOperation(new TryPrimaryPaymentOperation())
            .WithOperation(new ConditionalWorkflowOperation(
                // Check if primary payment failed
                (inputData, foundry, cancellationToken) => Task.FromResult(foundry.Properties.ContainsKey("primary_payment_failed")),
                // If failed, try fallback (composite operation)
                ForEachWorkflowOperation.CreateSharedInput(new IWorkflowOperation[]
                {
                    new LoggingOperation("[WARNING] Primary payment failed, trying fallback..."),
                    new TryFallbackPaymentOperation(),
                    new ConditionalWorkflowOperation(
                        // Check if fallback also failed
                        (inputData, foundry, cancellationToken) => Task.FromResult(foundry.Properties.ContainsKey("fallback_payment_failed")),
                        // Both failed - manual intervention required
                        new RequireManualInterventionOperation(),
                        // Fallback succeeded
                        new LoggingOperation("[SUCCESS] Fallback payment succeeded")
                    )
                }, name: "FallbackPaymentPath"),
                // Primary succeeded
                new LoggingOperation("[SUCCESS] Primary payment succeeded")))
            .WithOperation(new FinalizePaymentOperation());

        try
        {
            Console.WriteLine("Executing workflow with alternative paths...");
            await foundry.ForgeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] All payment methods failed: {ex.Message}");
        }
    }

    private static async Task RunCircuitBreakerScenario()
    {
        Console.WriteLine("\n--- Circuit Breaker Scenario ---");

        using var foundry = WorkflowForge.CreateFoundry("CircuitBreakerWorkflow");

        foundry.Properties["circuit_state"] = "Closed"; // Closed, Open, HalfOpen
        foundry.Properties["failure_count"] = 0;
        foundry.Properties["failure_threshold"] = 3;

        foundry
            .WithOperation(new CheckCircuitStateOperation())
            .WithOperation(new ConditionalWorkflowOperation(
                // Check if circuit is open
                (inputData, foundry, cancellationToken) => Task.FromResult(((string)foundry.Properties["circuit_state"]!).Equals("Open", StringComparison.OrdinalIgnoreCase)),
                // Circuit is open - fail fast
                new FailFastOperation(),
                // Circuit is closed or half-open - try operation (composite operation)
                ForEachWorkflowOperation.CreateSharedInput(new IWorkflowOperation[]
                {
                    new UnstableServiceOperation(),
                    new ResetCircuitOperation()
                }, name: "CircuitClosedPath")));

        try
        {
            Console.WriteLine("Executing workflow with circuit breaker...");
            await foundry.ForgeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Circuit breaker activated: {ex.Message}");
        }
    }
}

public class RetryableExternalServiceOperation : WorkflowOperationBase
{
    private static int _attemptCount = 0;

    public override string Name => "RetryableExternalService";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var maxRetries = (int)foundry.Properties["max_retries"]!;
        var currentAttempt = 0;
        Exception? lastException = null;

        while (currentAttempt <= maxRetries)
        {
            try
            {
                currentAttempt++;
                _attemptCount++;

                Console.WriteLine($"   [RETRY] Attempt {currentAttempt}/{maxRetries + 1} - Calling external service...");

                // Simulate an unreliable service
                await Task.Delay(100, cancellationToken);

                // Fail the first 2 attempts, succeed on the 3rd
                if (_attemptCount < 3)
                {
                    throw new ExternalServiceException($"Service temporarily unavailable (attempt {_attemptCount})");
                }

                foundry.Properties["service_response"] = "Success";
                foundry.Properties["response_time"] = DateTime.UtcNow;

                Console.WriteLine($"   [SUCCESS] External service call succeeded on attempt {currentAttempt}");
                return "Service call successful";
            }
            catch (ExternalServiceException ex)
            {
                lastException = ex;
                Console.WriteLine($"   [ERROR] Attempt {currentAttempt} failed: {ex.Message}");

                if (currentAttempt <= maxRetries)
                {
                    var delayMs = (int)Math.Pow(2, currentAttempt) * 100; // Exponential backoff
                    Console.WriteLine($"   [WAIT] Waiting {delayMs}ms before retry...");
                    await Task.Delay(delayMs, cancellationToken);
                }
            }
        }

        foundry.Properties["all_retries_exhausted"] = true;
        throw new WorkflowExecutionException($"Operation failed after {maxRetries + 1} attempts", lastException!);
    }

    public override Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        foundry.Properties.TryRemove("service_response", out _);
        foundry.Properties.TryRemove("response_time", out _);
        foundry.Properties.TryRemove("all_retries_exhausted", out _);
        return Task.CompletedTask;
    }
}

public class TryPrimaryPaymentOperation : WorkflowOperationBase
{
    public override string Name => "TryPrimaryPayment";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var service = (string)foundry.Properties["primary_service"]!;
        var amount = (decimal)foundry.Properties["amount"]!;

        Console.WriteLine($"   [INFO] Attempting payment via {service} for ${amount:F2}...");

        await Task.Delay(150, cancellationToken);

        // Simulate primary payment failure
        if (ThreadSafeRandom.NextDouble() < 0.7) // 70% chance of failure
        {
            foundry.Properties["primary_payment_failed"] = true;
            foundry.Properties["primary_failure_reason"] = "Service temporarily unavailable";

            Console.WriteLine($"   [ERROR] Primary payment failed: Service temporarily unavailable");
            return "Primary payment failed";
        }

        var transactionId = $"PRI-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        foundry.Properties["transaction_id"] = transactionId;
        foundry.Properties["payment_method"] = "Primary";

        Console.WriteLine($"   [SUCCESS] Primary payment succeeded: {transactionId}");
        return $"Primary payment successful: {transactionId}";
    }

    public override async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        if (foundry.Properties.TryGetValue("transaction_id", out var txnId))
        {
            Console.WriteLine($"   [REFUND] Refunding primary payment: {txnId}");
            await Task.Delay(100, cancellationToken);
            foundry.Properties.TryRemove("transaction_id", out _);
            foundry.Properties.TryRemove("payment_method", out _);
        }
    }
}

public class TryFallbackPaymentOperation : WorkflowOperationBase
{
    public override string Name => "TryFallbackPayment";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var service = (string)foundry.Properties["fallback_service"]!;
        var amount = (decimal)foundry.Properties["amount"]!;

        Console.WriteLine($"   [RETRY] Attempting fallback payment via {service} for ${amount:F2}...");

        await Task.Delay(200, cancellationToken);

        // Fallback has better success rate
        if (ThreadSafeRandom.NextDouble() < 0.2) // 20% chance of failure
        {
            foundry.Properties["fallback_payment_failed"] = true;
            foundry.Properties["fallback_failure_reason"] = "Insufficient funds";

            Console.WriteLine($"   [ERROR] Fallback payment failed: Insufficient funds");
            return "Fallback payment failed";
        }

        var transactionId = $"BAK-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        foundry.Properties["transaction_id"] = transactionId;
        foundry.Properties["payment_method"] = "Fallback";

        Console.WriteLine($"   [SUCCESS] Fallback payment succeeded: {transactionId}");
        return $"Fallback payment successful: {transactionId}";
    }

    public override async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        if (foundry.Properties.TryGetValue("transaction_id", out var txnId))
        {
            Console.WriteLine($"   [REFUND] Refunding fallback payment: {txnId}");
            await Task.Delay(100, cancellationToken);
            foundry.Properties.TryRemove("transaction_id", out _);
            foundry.Properties.TryRemove("payment_method", out _);
        }
    }
}

public class UnstableServiceOperation : WorkflowOperationBase
{
    public override string Name => "UnstableService";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        Console.WriteLine($"   [INFO] Calling unstable service...");

        await Task.Delay(100, cancellationToken);

        // Simulate service instability
        if (ThreadSafeRandom.NextDouble() < 0.6) // 60% chance of failure
        {
            var failureCount = (int)foundry.Properties["failure_count"]! + 1;
            var threshold = (int)foundry.Properties["failure_threshold"]!;

            foundry.Properties["failure_count"] = failureCount;

            if (failureCount >= threshold)
            {
                foundry.Properties["circuit_state"] = "Open";
                Console.WriteLine($"   [ALERT] Circuit breaker opened after {failureCount} failures");
            }

            throw new ExternalServiceException($"Service failure #{failureCount}");
        }

        Console.WriteLine($"   [SUCCESS] Service call succeeded");
        return "Service call successful";
    }
}

// Helper operations
public class InitializeConnectionOperation : WorkflowOperationBase
{
    public override string Name => "InitializeConnection";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        Console.WriteLine("   [INFO] Initializing connection...");
        await Task.Delay(50, cancellationToken);
        foundry.Properties["connection_initialized"] = true;
        return "Connection initialized";
    }
}

public class ValidateResponseOperation : WorkflowOperationBase
{
    public override string Name => "ValidateResponse";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        Console.WriteLine("   [SUCCESS] Validating service response...");
        await Task.Delay(30, cancellationToken);
        return "Response validated";
    }
}

public class LogSuccessOperation : WorkflowOperationBase
{
    public override string Name => "LogSuccess";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        Console.WriteLine("   [INFO] Logging successful operation");
        await Task.Delay(20, cancellationToken);
        return "Success logged";
    }
}

// More helper operations with minimal implementation
public class PreparePaymentOperation : WorkflowOperationBase
{
    public override string Name => "PreparePayment";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        Console.WriteLine("   [INFO] Preparing payment...");
        await Task.Delay(50, cancellationToken);
        return "Payment prepared";
    }
}

public class RequireManualInterventionOperation : WorkflowOperationBase
{
    public override string Name => "RequireManualIntervention";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        Console.WriteLine("   [WARNING] Payment requires manual processing");
        await Task.Delay(30, cancellationToken);
        foundry.Properties["manual_intervention_required"] = true;
        return "Manual intervention flagged";
    }
}

public class FinalizePaymentOperation : WorkflowOperationBase
{
    public override string Name => "FinalizePayment";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        Console.WriteLine("   [INFO] Finalizing payment...");
        await Task.Delay(60, cancellationToken);

        if (foundry.Properties.TryGetValue("transaction_id", out var txnId))
        {
            Console.WriteLine($"   [SUCCESS] Payment finalized: {txnId}");
        }
        else if (foundry.Properties.ContainsKey("manual_intervention_required"))
        {
            Console.WriteLine($"   [WARNING] Payment requires manual processing");
        }

        return "Payment finalized";
    }
}

public class CheckCircuitStateOperation : WorkflowOperationBase
{
    public override string Name => "CheckCircuitState";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var state = (string)foundry.Properties["circuit_state"]!;
        Console.WriteLine($"   [INFO] Circuit breaker state: {state}");
        await Task.Delay(20, cancellationToken);
        return state;
    }
}

public class FailFastOperation : WorkflowOperationBase
{
    public override string Name => "FailFast";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        Console.WriteLine("   [FAST_FAIL] Circuit breaker is open - failing fast");
        await Task.Delay(10, cancellationToken);
        throw new CircuitBreakerOpenException("Circuit breaker is open - service calls are blocked");
    }
}

public class ResetCircuitOperation : WorkflowOperationBase
{
    public override string Name => "ResetCircuit";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        Console.WriteLine("   [RESET] Resetting circuit breaker");
        await Task.Delay(20, cancellationToken);
        foundry.Properties["circuit_state"] = "Closed";
        foundry.Properties["failure_count"] = 0;
        return "Circuit reset";
    }
}

// Custom exception types
public class ExternalServiceException : Exception
{
    public ExternalServiceException(string message) : base(message)
    {
    }

    public ExternalServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class WorkflowExecutionException : Exception
{
    public WorkflowExecutionException(string message) : base(message)
    {
    }

    public WorkflowExecutionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message)
    {
    }
}
