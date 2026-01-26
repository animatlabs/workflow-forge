using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Extensions.Resilience.Polly;
using WorkflowForge.Extensions.Resilience.Polly.Options;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates advanced resilience patterns using the Polly extension with WorkflowForge.
/// Shows retry policies, circuit breakers, timeouts, and rate limiting.
/// </summary>
public class PollyResilienceSample : ISample
{
    public string Name => "Polly Resilience Patterns";
    public string Description => "Advanced resilience with circuit breakers, retries, and timeouts";

    public async Task RunAsync()
    {
        Console.WriteLine("Demonstrating Polly resilience patterns...");

        // Scenario 1: Development resilience (lenient)
        await RunDevelopmentResilienceScenario();

        // Scenario 2: Production resilience (strict)
        await RunProductionResilienceScenario();

        // Scenario 3: Enterprise resilience (comprehensive)
        await RunEnterpriseResilienceScenario();
    }

    private static async Task RunDevelopmentResilienceScenario()
    {
        Console.WriteLine("\n--- Development Resilience Scenario ---");

        using var foundry = WorkflowForge.CreateFoundry("DevelopmentResilience");

        // Apply development resilience settings (lenient for debugging)
        foundry.UsePollyFromSettings(new PollyMiddlewareOptions());

        foundry.Properties["scenario"] = "development";
        foundry.Properties["max_failures"] = 1; // Fail once, then succeed

        foundry
            .WithOperation(new UnreliableServiceOperation())
            .WithOperation(new DataProcessingOperation())
            .WithOperation(new CompletionOperation());

        try
        {
            Console.WriteLine("Executing workflow with development resilience...");
            await foundry.ForgeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Development workflow failed: {ex.Message}");
        }
    }

    private static async Task RunProductionResilienceScenario()
    {
        Console.WriteLine("\n--- Production Resilience Scenario ---");

        using var foundry = WorkflowForge.CreateFoundry("ProductionResilience");

        // Apply production resilience settings (strict for reliability)
        foundry.UsePollyFromSettings(new PollyMiddlewareOptions
        {
            Retry = { MaxRetryAttempts = 5 },
            CircuitBreaker = { IsEnabled = true }
        });

        foundry.Properties["scenario"] = "production";
        foundry.Properties["max_failures"] = 2; // Fail twice, then succeed

        foundry
            .WithOperation(new UnreliableServiceOperation())
            .WithOperation(new DataProcessingOperation())
            .WithOperation(new CompletionOperation());

        try
        {
            Console.WriteLine("Executing workflow with production resilience...");
            await foundry.ForgeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Production workflow failed: {ex.Message}");
        }
    }

    private static async Task RunEnterpriseResilienceScenario()
    {
        Console.WriteLine("\n--- Enterprise Resilience Scenario ---");

        using var foundry = WorkflowForge.CreateFoundry("EnterpriseResilience");

        // Apply enterprise resilience settings (comprehensive)
        foundry.UsePollyFromSettings(new PollyMiddlewareOptions
        {
            Retry = { MaxRetryAttempts = 7 },
            CircuitBreaker = { IsEnabled = true },
            Timeout = { IsEnabled = true },
            EnableComprehensivePolicies = true
        });

        foundry.Properties["scenario"] = "enterprise";
        foundry.Properties["max_failures"] = 1; // Succeed after one failure

        foundry
            .WithOperation(new UnreliableServiceOperation())
            .WithOperation(new DataProcessingOperation())
            .WithOperation(new CompletionOperation());

        try
        {
            Console.WriteLine("Executing workflow with enterprise resilience...");
            await foundry.ForgeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Enterprise workflow failed: {ex.Message}");
        }
    }
}

/// <summary>
/// Simulates an unreliable external service that fails initially but eventually succeeds
/// </summary>
public class UnreliableServiceOperation : IWorkflowOperation
{
    private static int _attemptCount = 0;

    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "UnreliableService";
    public bool SupportsRestore => true;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var scenario = foundry.Properties["scenario"] as string ?? "unknown";
        var maxFailures = (int)(foundry.Properties["max_failures"] ?? 0);

        _attemptCount++;

        foundry.Logger.LogInformation("Attempting to call unreliable service (attempt {AttemptCount})", _attemptCount);

        // Simulate network delay
        await Task.Delay(100, cancellationToken);

        // Fail for the first few attempts based on scenario
        if (_attemptCount <= maxFailures)
        {
            var errorMessage = scenario switch
            {
                "development" => "Service temporarily unavailable (dev environment)",
                "production" => "Service timeout - high load detected",
                "enterprise" => "Service circuit breaker activated",
                _ => "Unknown service error"
            };

            foundry.Logger.LogWarning("Service call failed: {ErrorMessage}", errorMessage);
            throw new ExternalServiceException(errorMessage);
        }

        // Success after configured failures
        var serviceResponse = new
        {
            RequestId = Guid.NewGuid().ToString("N")[..8],
            Data = $"Service response for {scenario} scenario",
            Timestamp = DateTime.UtcNow,
            AttemptCount = _attemptCount
        };

        foundry.Properties["service_response"] = serviceResponse;
        foundry.Logger.LogInformation("Service call succeeded on attempt {AttemptCount}", _attemptCount);

        return serviceResponse;
    }

    public async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        foundry.Logger.LogWarning("Restoring unreliable service operation due to workflow failure");

        // Simulate cleanup
        await Task.Delay(50, cancellationToken);

        foundry.Properties.TryRemove("service_response", out _);
        foundry.Logger.LogInformation("Service operation restoration completed");
    }

    public void Dispose()
    { }
}

/// <summary>
/// Data processing operation that demonstrates timeout handling
/// </summary>
public class DataProcessingOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "DataProcessing";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var serviceResponse = inputData as dynamic;
        var scenario = foundry.Properties["scenario"] as string ?? "unknown";

        foundry.Logger.LogInformation("Processing data from service response");

        // Simulate data processing with different durations based on scenario
        var processingTime = scenario switch
        {
            "development" => 200,  // Fast for development
            "production" => 500,   // Moderate for production
            "enterprise" => 300,   // Optimized for enterprise
            _ => 1000
        };

        await Task.Delay(processingTime, cancellationToken);

        var processedData = new
        {
            OriginalRequestId = serviceResponse?.RequestId ?? "unknown",
            ProcessedAt = DateTime.UtcNow,
            ProcessingTime = processingTime,
            Scenario = scenario,
            Status = "Processed"
        };

        foundry.Properties["processed_data"] = processedData;
        foundry.Logger.LogInformation("Data processing completed in {ProcessingTime}ms", processingTime);

        return processedData;
    }

    public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Data processing does not support restoration");
    }

    public void Dispose()
    { }
}

/// <summary>
/// Completion operation that summarizes the resilience scenario results
/// </summary>
public class CompletionOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "Completion";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var processedData = inputData as dynamic;
        var scenario = foundry.Properties["scenario"] as string ?? "unknown";

        foundry.Logger.LogInformation("Completing {Scenario} resilience workflow", scenario);

        // Simulate final processing
        await Task.Delay(50, cancellationToken);

        var summary = new
        {
            Scenario = scenario,
            CompletedAt = DateTime.UtcNow,
            RequestId = processedData?.OriginalRequestId ?? "unknown",
            ProcessingTime = processedData?.ProcessingTime ?? 0,
            Status = "Completed Successfully",
            ResiliencePattern = scenario switch
            {
                "development" => "Lenient retries, extended timeouts",
                "production" => "Strict retries, circuit breakers",
                "enterprise" => "Comprehensive policies, rate limiting",
                _ => "Unknown pattern"
            }
        };

        foundry.Properties["workflow_summary"] = summary;

        Console.WriteLine($"   [SUCCESS] {scenario} resilience workflow completed successfully!");
        Console.WriteLine($"   [INFO] Request ID: {summary.RequestId}");
        Console.WriteLine($"   [INFO] Processing time: {summary.ProcessingTime}ms");
        Console.WriteLine($"   [INFO] Resilience pattern: {summary.ResiliencePattern}");

        return summary;
    }

    public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Completion operation does not support restoration");
    }

    public void Dispose()
    { }
}