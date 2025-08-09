using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Middleware;
using WorkflowForge.Operations;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates middleware usage for cross-cutting concerns like timing, logging, and error handling.
/// Shows how to create custom middleware and apply built-in middleware components.
/// </summary>
public class MiddlewareSample : ISample
{
    public string Name => "Middleware Usage";
    public string Description => "Middleware components for timing, logging, error handling, and custom cross-cutting concerns";

    public async Task RunAsync()
    {
        Console.WriteLine("Demonstrating WorkflowForge middleware components...");

        // Scenario 1: Basic built-in middleware
        await RunBuiltInMiddlewareDemo();

        // Scenario 2: Custom middleware
        await RunCustomMiddlewareDemo();

        // Scenario 3: Middleware pipeline
        await RunMiddlewarePipelineDemo();
    }

    private static async Task RunBuiltInMiddlewareDemo()
    {
        Console.WriteLine("\n--- Built-in Middleware Demo ---");

        using var foundry = WorkflowForge.CreateFoundry("BuiltInMiddlewareDemo");

        // Add built-in middleware components
        // Enable standard timing via configuration if needed; direct TimingMiddleware is internal
        foundry.UseLogging();

        foundry
            .WithOperation(LoggingOperation.Info("Starting middleware demonstration"))
            .WithOperation(DelayOperation.FromMilliseconds(200))
            .WithOperation(new ProcessingOperation("DataProcessor", TimeSpan.FromMilliseconds(300)))
            .WithOperation(DelayOperation.FromMilliseconds(150))
            .WithOperation(LoggingOperation.Info("Middleware demonstration completed"));

        await foundry.ForgeAsync();
    }

    private static async Task RunCustomMiddlewareDemo()
    {
        Console.WriteLine("\n--- Custom Middleware Demo ---");

        using var foundry = WorkflowForge.CreateFoundry("CustomMiddlewareDemo");

        // Add custom middleware
        foundry.AddMiddleware(new SecurityMiddleware());
        foundry.AddMiddleware(new ValidationMiddleware());
        foundry.AddMiddleware(new AuditMiddleware());

        foundry.SetProperty("user_id", "user_12345");
        foundry.SetProperty("security_token", "sec_token_abc123");

        foundry
            .WithOperation(LoggingOperation.Info("Starting custom middleware demonstration"))
            .WithOperation(new BusinessOperation("CustomerValidation", "CUSTOMER_001"))
            .WithOperation(new BusinessOperation("OrderProcessing", "ORDER_999"))
            .WithOperation(LoggingOperation.Info("Custom middleware demonstration completed"));

        await foundry.ForgeAsync();
    }

    private static async Task RunMiddlewarePipelineDemo()
    {
        Console.WriteLine("\n--- Middleware Pipeline Demo ---");

        using var foundry = WorkflowForge.CreateFoundry("MiddlewarePipelineDemo");

        // Create a comprehensive middleware pipeline
        foundry.AddMiddleware(new SecurityMiddleware());           // First: Security check
        foundry.AddMiddleware(new TimingMiddleware());             // Second: Performance timing
        foundry.AddMiddleware(new ValidationMiddleware());         // Third: Input validation
        foundry.UseLogging(); // Detailed logging
        foundry.AddMiddleware(new AuditMiddleware());              // Fifth: Audit trail

        foundry.SetProperty("user_id", "admin_user");
        foundry.SetProperty("security_token", "admin_token_xyz789");
        foundry.SetProperty("validation_rules", new[] { "required_field", "format_check", "business_rules" });

        foundry
            .WithOperation(LoggingOperation.Info("Starting comprehensive middleware pipeline"))
            .WithOperation(new BusinessOperation("CriticalOperation", "CRITICAL_DATA"))
            .WithOperation(DelayOperation.FromMilliseconds(100))
            .WithOperation(new BusinessOperation("ReportGeneration", "MONTHLY_REPORT"))
            .WithOperation(LoggingOperation.Info("Middleware pipeline demonstration completed"));

        await foundry.ForgeAsync();

        Console.WriteLine("   Middleware pipeline executed successfully");
        Console.WriteLine($"   Audit entries created: {foundry.Properties.GetValueOrDefault("audit_count", 0)}");
        Console.WriteLine($"   Security checks performed: {foundry.Properties.GetValueOrDefault("security_checks", 0)}");
        Console.WriteLine($"   Validation checks performed: {foundry.Properties.GetValueOrDefault("validation_checks", 0)}");
    }

    private static async Task RunConditionalMiddlewareDemo()
    {
        Console.WriteLine("\n--- Conditional Middleware Demo ---");

        using var foundry = WorkflowForge.CreateFoundry("ConditionalMiddlewareDemo");

        // Set condition for middleware behavior
        foundry.SetProperty("enable_validation", true);
        foundry.SetProperty("enable_caching", false);

        foundry
            .WithOperation(LoggingOperation.Info("Starting conditional middleware demonstration"))
            .UseLogging()
            .UseLogging()
            .WithOperation(new ConditionalOperation("DataProcessor"))
            .WithOperation(new ConditionalOperation("ResultValidator"))
            .WithOperation(LoggingOperation.Info("Conditional middleware demonstration completed"));

        await foundry.ForgeAsync();

        Console.WriteLine("   Conditional middleware processing completed");
    }

    private static async Task RunCustomMiddlewareOrderingDemo()
    {
        Console.WriteLine("\n--- Custom Middleware Ordering Demo ---");

        using var foundry = WorkflowForge.CreateFoundry("MiddlewareOrderingDemo");

        foundry
            .UseLogging()
            .UseLogging()
            .WithOperation(LoggingOperation.Info("Starting middleware ordering demonstration"))
            .WithOperation(new ProcessingOperation("OrderedOperation", TimeSpan.FromMilliseconds(200)))
            .WithOperation(LoggingOperation.Info("Middleware ordering demonstration completed"));

        await foundry.ForgeAsync();

        Console.WriteLine("   Middleware ordering demonstration completed");
    }
}

/// <summary>
/// Custom security middleware for authentication and authorization
/// </summary>
public class SecurityMiddleware : IWorkflowOperationMiddleware
{
    public async Task<object?> ExecuteAsync(IWorkflowOperation operation, IWorkflowFoundry foundry, object? inputData,
        Func<Task<object?>> next, CancellationToken cancellationToken = default)
    {
        foundry.TryGetProperty<string>("user_id", out var userId);
        foundry.TryGetProperty<string>("security_token", out var token);

        foundry.Logger.LogInformation("Security check for operation: {OperationName}, User: {UserId}",
            operation.Name, userId ?? "anonymous");

        // Simulate security validation
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            throw new UnauthorizedAccessException("Missing security credentials");
        }

        if (!token.StartsWith("sec_token_") && !token.StartsWith("admin_token_"))
        {
            throw new UnauthorizedAccessException("Invalid security token");
        }

        // Update security check counter
        var securityChecks = foundry.GetPropertyOrDefault<int>("security_checks", 0);
        foundry.SetProperty("security_checks", securityChecks + 1);

        foundry.Logger.LogDebug("Security validation passed for user: {UserId}", userId);

        // Call next middleware/operation
        return await next();
    }
}

/// <summary>
/// Custom validation middleware for input and business rule validation
/// </summary>
public class ValidationMiddleware : IWorkflowOperationMiddleware
{
    public async Task<object?> ExecuteAsync(IWorkflowOperation operation, IWorkflowFoundry foundry, object? inputData,
        Func<Task<object?>> next, CancellationToken cancellationToken = default)
    {
        foundry.Logger.LogInformation("Validation check for operation: {OperationName}", operation.Name);

        var validationRules = foundry.GetPropertyOrDefault<string[]>("validation_rules");

        if (validationRules != null)
        {
            foreach (var rule in validationRules)
            {
                foundry.Logger.LogDebug("Applying validation rule: {Rule}", rule);

                // Simulate rule validation
                await Task.Delay(10, cancellationToken);

                if (rule == "business_rules" && operation.Name.Contains("Critical"))
                {
                    foundry.Logger.LogDebug("Special business rule validation for critical operation");
                }
            }
        }

        // Update validation check counter
        var validationChecks = foundry.GetPropertyOrDefault<int>("validation_checks", 0);
        foundry.SetProperty("validation_checks", validationChecks + 1);

        foundry.Logger.LogDebug("Validation completed for operation: {OperationName}", operation.Name);

        // Call next middleware/operation
        return await next();
    }
}

/// <summary>
/// Custom audit middleware for compliance and tracking
/// </summary>
public class AuditMiddleware : IWorkflowOperationMiddleware
{
    public async Task<object?> ExecuteAsync(IWorkflowOperation operation, IWorkflowFoundry foundry, object? inputData,
        Func<Task<object?>> next, CancellationToken cancellationToken = default)
    {
        var auditEntry = new
        {
            OperationId = operation.Id,
            OperationName = operation.Name,
            UserId = foundry.Properties.GetValueOrDefault("user_id") as string,
            Timestamp = DateTime.UtcNow,
            WorkflowExecutionId = foundry.ExecutionId
        };

        foundry.Logger.LogInformation("Audit: Recording operation execution - {OperationName} by {UserId}",
            operation.Name, auditEntry.UserId ?? "unknown");

        var startTime = DateTime.UtcNow;

        try
        {
            // Call next middleware/operation
            var result = await next();

            var duration = DateTime.UtcNow - startTime;

            foundry.Logger.LogInformation("Audit: Operation completed - {OperationName}, Duration: {Duration}ms",
                operation.Name, duration.TotalMilliseconds);

            // Update audit counter
            var auditCount = foundry.GetPropertyOrDefault<int>("audit_count", 0);
            foundry.SetProperty("audit_count", auditCount + 1);

            return result;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;

            foundry.Logger.LogError("Audit: Operation failed - {OperationName}, Duration: {Duration}ms, Error: {Error}",
                operation.Name, duration.TotalMilliseconds, ex.Message);

            throw;
        }
    }
}

/// <summary>
/// Sample processing operation for middleware demonstration
/// </summary>
public class ProcessingOperation : IWorkflowOperation
{
    private readonly string _operationName;
    private readonly TimeSpan _processingTime;

    public ProcessingOperation(string operationName, TimeSpan processingTime)
    {
        _operationName = operationName;
        _processingTime = processingTime;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public string Name => _operationName;
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        foundry.Logger.LogInformation("Processing operation started: {OperationName}", _operationName);

        await Task.Delay(_processingTime, cancellationToken);

        var result = new
        {
            OperationName = _operationName,
            ProcessingTime = _processingTime,
            CompletedAt = DateTime.UtcNow,
            Status = "Processed"
        };

        foundry.Logger.LogInformation("Processing operation completed: {OperationName}", _operationName);

        return result;
    }

    public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        throw new NotSupportedException($"Operation {_operationName} does not support restoration");
    }

    public void Dispose()
    { }
}

/// <summary>
/// Sample business operation for middleware demonstration
/// </summary>
public class BusinessOperation : IWorkflowOperation
{
    private readonly string _operationName;
    private readonly string _businessData;

    public BusinessOperation(string operationName, string businessData)
    {
        _operationName = operationName;
        _businessData = businessData;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public string Name => _operationName;
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        foundry.Logger.LogInformation("Business operation started: {OperationName} with data: {BusinessData}",
            _operationName, _businessData);

        // Simulate business processing
        await Task.Delay(150, cancellationToken);

        var result = new
        {
            OperationName = _operationName,
            BusinessData = _businessData,
            ProcessedBy = foundry.Properties.GetValueOrDefault("user_id") as string,
            CompletedAt = DateTime.UtcNow,
            Status = "BusinessCompleted"
        };

        foundry.Logger.LogInformation("Business operation completed: {OperationName}", _operationName);

        return result;
    }

    public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        throw new NotSupportedException($"Operation {_operationName} does not support restoration");
    }

    public void Dispose()
    { }
}

/// <summary>
/// Demo operation that simulates processing with configurable timing
/// </summary>
public class DemoOperation : IWorkflowOperation
{
    private readonly string _operationName;
    private readonly TimeSpan _processingTime;

    public DemoOperation(string operationName, TimeSpan processingTime)
    {
        _operationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
        _processingTime = processingTime;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public string Name => _operationName;
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        foundry.Logger.LogInformation("Executing demo operation: {OperationName}", _operationName);

        // Simulate processing time
        await Task.Delay(_processingTime, cancellationToken);

        foundry.Logger.LogInformation("Completed demo operation: {OperationName}", _operationName);

        return $"Result from {_operationName}";
    }

    public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("This operation does not support restore.");
    }

    public void Dispose()
    { }
}

/// <summary>
/// Conditional operation that behaves differently based on foundry properties
/// </summary>
public class ConditionalOperation : IWorkflowOperation
{
    private readonly string _operationName;

    public ConditionalOperation(string operationName)
    {
        _operationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
    }

    public Guid Id { get; } = Guid.NewGuid();
    public string Name => _operationName;
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var enableValidation = foundry.GetPropertyOrDefault<bool>("enable_validation", false);
        var enableCaching = foundry.GetPropertyOrDefault<bool>("enable_caching", false);

        foundry.Logger.LogInformation("Executing conditional operation: {OperationName}, Validation: {ValidationEnabled}, Caching: {CachingEnabled}",
            _operationName, enableValidation, enableCaching);

        if ((bool)enableValidation)
        {
            foundry.Logger.LogDebug("Performing validation for {OperationName}", _operationName);
            await Task.Delay(50, cancellationToken); // Simulate validation time
        }

        if ((bool)enableCaching)
        {
            foundry.Logger.LogDebug("Using cached result for {OperationName}", _operationName);
            return "Cached result";
        }

        // Simulate normal processing
        await Task.Delay(100, cancellationToken);

        return $"Processed result from {_operationName}";
    }

    public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("This operation does not support restore.");
    }

    public void Dispose()
    { }
}