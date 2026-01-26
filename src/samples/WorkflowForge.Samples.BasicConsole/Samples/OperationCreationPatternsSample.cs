using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Operations;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates all the different ways to create operations in WorkflowForge.
/// This sample showcases the various patterns available for consumers to use.
/// </summary>
public class OperationCreationPatternsSample : ISample
{
    public string Name => "Operation Creation Patterns";
    public string Description => "Comprehensive demo of all operation creation patterns";

    public async Task RunAsync()
    {
        Console.WriteLine("Creating a workflow that demonstrates all operation creation patterns...");
        Console.WriteLine();

        // Pattern 1: Direct Constructor with new
        Console.WriteLine("Pattern 1: Direct Constructor (new)");
        var workflow1 = WorkflowForge.CreateWorkflow()
            .WithName("DirectConstructorWorkflow")
            .AddOperation(new CustomBusinessOperation("Process-001", "Customer Data"))
            .Build();

        using var foundry1 = WorkflowForge.CreateFoundry("DirectConstructor");
        foundry1.SetProperty("input", "Starting with direct constructors");

        using var smith1 = WorkflowForge.CreateSmith();
        await smith1.ForgeAsync(workflow1, foundry1);
        Console.WriteLine("Direct constructor pattern completed\n");

        // Demonstrate inline operations with foundry-based data passing
        Console.WriteLine("Demonstrating inline math operations...");
        var mathWorkflow = WorkflowForge.CreateWorkflow()
            .WithName("MathWorkflow")
            .AddOperation("PrepareNumber", (foundry, ct) =>
            {
                foundry.SetProperty("numberInput", 10);
                foundry.Logger.LogInformation("Prepared number: 10");
                return Task.CompletedTask;
            })
            .AddOperation("Calculate", (foundry, ct) =>
            {
                var number = foundry.GetPropertyOrDefault<int>("numberInput");
                var result = number * 42 * 1.5;
                foundry.Logger.LogInformation("Calculating {Number} * 42 * 1.5 = {Result}", number, result);
                foundry.SetProperty("mathResult", result);
                return Task.CompletedTask;
            })
            .Build();

        using var mathFoundry = WorkflowForge.CreateFoundry("MathDemo");
        mathFoundry.SetProperty("input", "Starting math operations");

        using var mathSmith = WorkflowForge.CreateSmith();
        await mathSmith.ForgeAsync(mathWorkflow, mathFoundry);
        Console.WriteLine("Math operation pattern completed\n");

        // Separate workflow for PaymentOperation
        var paymentWorkflow = WorkflowForge.CreateWorkflow()
            .WithName("PaymentWorkflow")
            .AddOperation(new PaymentOperation(99.99m))
            .Build();

        using var paymentFoundry = WorkflowForge.CreateFoundry("PaymentDemo");
        paymentFoundry.SetProperty("input", "Payment processing");

        using var paymentSmith = WorkflowForge.CreateSmith();
        await paymentSmith.ForgeAsync(paymentWorkflow, paymentFoundry);
        Console.WriteLine("Payment operation pattern completed\n");

        // PATTERN 2: Generic TOperation with Factory Methods

        Console.WriteLine("Pattern 2: Generic TOperation Factory Methods ---");
        var workflow2 = WorkflowForge.CreateWorkflow()
            .WithName("GenericFactoryWorkflow")
            // Untyped operations that work with any input
            .AddOperation(WorkflowOperations.Create("StringProcessor",
                input => $"Processed: {input?.ToString()?.ToUpperInvariant()}"))

            // Another untyped async operation
            .AddOperation(WorkflowOperations.CreateAsync("StringLengthCalculator",
                async input =>
                {
                    await Task.Delay(50); // Simulate async work
                    return input?.ToString()?.Length ?? 0;
                }))

            // Untyped with foundry access
            .AddOperation(DelegateWorkflowOperation.FromAsync("ScaleCalculator",
                async input =>
                {
                    await Task.Delay(25);
                    var length = input?.ToString()?.Length ?? 0;
                    return length * 2.5;
                }))

            .Build();

        using var foundry2 = WorkflowForge.CreateFoundry("GenericFactory");
        foundry2.SetProperty("input", "hello world");

        using var smith2 = WorkflowForge.CreateSmith();
        await smith2.ForgeAsync(workflow2, foundry2);
        Console.WriteLine($"Generic factory pattern completed - Final result: {foundry2.GetPropertyOrDefault<object>("result")}\n");

        // PATTERN 3: DelegateWorkflowOperation Patterns

        Console.WriteLine("Pattern 3: DelegateWorkflowOperation Patterns ---");
        var workflow3 = WorkflowForge.CreateWorkflow()
            .WithName("DelegateOperationWorkflow")

            // Untyped delegate with full signature
            .AddOperation(new DelegateWorkflowOperation("CustomValidator",
                async (input, foundry, ct) =>
                {
                    var properties = new Dictionary<string, string>
                    {
                        ["InputType"] = input?.GetType().Name ?? "null",
                        ["ValidationRule"] = "MinimumLength",
                        ["MinLength"] = "3"
                    };
                    foundry.Logger.LogInformation(properties, "Validating input: {Input}", input);
                    await Task.Delay(100, ct);
                    var isValid = input?.ToString()?.Length > 3;
                    foundry.SetProperty("validationResult", isValid);
                    return isValid ? "Valid" : "Invalid";
                }))

            // From sync factory method
            .AddOperation(DelegateWorkflowOperation.FromSync("SimpleTransform",
                input => $"Transformed: {input}"))

            // From async factory method
            .AddOperation(DelegateWorkflowOperation.FromAsync("AsyncProcessor",
                async input =>
                {
                    await Task.Delay(75);
                    return $"Async result: {input}";
                }))

            // Action operation (no return value)
            .AddOperation(DelegateWorkflowOperation.FromAction("LogResults",
                input => Console.WriteLine($"   [ACTION] Final result: {input}")))

            .Build();

        using var foundry3 = WorkflowForge.CreateFoundry("DelegateOperations");
        foundry3.SetProperty("input", "sample data");

        using var smith3 = WorkflowForge.CreateSmith();
        await smith3.ForgeAsync(workflow3, foundry3);
        Console.WriteLine("Delegate operation patterns completed\n");

        // PATTERN 4: WorkflowOperations Factory Class

        Console.WriteLine("Pattern 4: WorkflowOperations Factory Class ---");
        var workflow4 = WorkflowForge.CreateWorkflow()
            .WithName("FactoryClassWorkflow")

            // Simple sync operation
            .AddOperation(WorkflowOperations.Create("DataNormalizer",
                input => input?.ToString()?.Trim().ToLowerInvariant()))

            // Simple async operation
            .AddOperation(WorkflowOperations.CreateAsync("DataEnricher",
                async input =>
                {
                    await Task.Delay(50);
                    return $"enriched_{input}_{DateTime.Now:HHmmss}";
                }))

            // Action operation
            .AddOperation(WorkflowOperations.CreateAction("ResultLogger",
                input => Console.WriteLine($"   [FACTORY ACTION] Processing: {input}")))

            // Async action operation
            .AddOperation(WorkflowOperations.CreateAsyncAction("AsyncNotifier",
                async input =>
                {
                    await Task.Delay(25);
                    Console.WriteLine($"   [ASYNC ACTION] Notification sent for: {input}");
                }))

            .Build();

        using var foundry4 = WorkflowForge.CreateFoundry("FactoryClass");
        foundry4.SetProperty("input", "  RAW DATA  ");

        using var smith4 = WorkflowForge.CreateSmith();
        await smith4.ForgeAsync(workflow4, foundry4);
        Console.WriteLine("Factory class patterns completed\n");

        // PATTERN 5: Inline Lambda Operations (Quick prototyping)

        Console.WriteLine("Pattern 5: Inline Lambda Operations (For Prototyping) ---");
        var workflow5 = WorkflowForge.CreateWorkflow()
            .WithName("InlineLambdaWorkflow")

            // Simple inline operation using WorkflowBuilder.AddOperation
            .AddOperation("QuickValidation", async (foundry, ct) =>
            {
                var input = foundry.GetPropertyOrDefault<object>("input");
                var properties = new Dictionary<string, string>
                {
                    ["ValidationType"] = "QuickValidation",
                    ["InputPresent"] = (input != null).ToString()
                };
                foundry.Logger.LogInformation(properties, "Quick validation started - InputPresent: {InputPresent}", input is not null);
                await Task.Delay(30, ct);
                foundry.SetProperty("validationResult", input != null ? "passed" : "failed");
            })

            // Complex inline operation using WorkflowBuilder.AddOperation
            .AddOperation("ComplexProcessing", async (foundry, ct) =>
            {
                var input = foundry.GetPropertyOrDefault<object>("input");
                var processId = Guid.NewGuid().ToString("N")[..8];
                var properties = new Dictionary<string, string>
                {
                    ["ProcessId"] = processId,
                    ["ProcessingType"] = "ComplexProcessing",
                    ["InputData"] = input?.ToString() ?? "null"
                };
                foundry.Logger.LogInformation(properties, "Complex processing started");

                var processor = new
                {
                    StartTime = DateTime.Now,
                    Input = input?.ToString(),
                    ProcessId = processId
                };

                await Task.Delay(100, ct);

                foundry.SetProperty("processingInfo", processor);

                var result = new
                {
                    Status = "completed",
                    ProcessedAt = DateTime.Now,
                    Duration = DateTime.Now - processor.StartTime,
                    Result = $"processed_{processor.Input}"
                };

                foundry.SetProperty("result", result);
            })

            .Build();

        using var foundry5 = WorkflowForge.CreateFoundry("InlineLambda");
        foundry5.SetProperty("input", "test data");

        using var smith5 = WorkflowForge.CreateSmith();
        await smith5.ForgeAsync(workflow5, foundry5);
        Console.WriteLine($"Inline lambda patterns completed\n");
        Console.WriteLine("   Note: Inline operations are great for prototyping and simple logic,");
        Console.WriteLine("      but consider converting to classes for larger applications.\n");

        // PATTERN 6: Mixed Pattern Workflow (Real-world example)

        Console.WriteLine("Pattern 6: Mixed Patterns (Real-world Example) ---");
        var workflow6 = WorkflowForge.CreateWorkflow()
            .WithName("MixedPatternWorkflow")

            // Start with a custom class (complex business logic)
            .AddOperation(new CustomBusinessOperation("ORDER-001", "Order Processing"))

            // Use factory for simple transformation to decimal
            .AddOperation(WorkflowOperations.Create<object, decimal>("CalculateTotal",
                input => 299.99m)) // Simplified calculation

            // Inline operation for business rules
            .AddOperation("ApplyDiscounts", async (foundry, ct) =>
            {
                var total = (decimal)(foundry.GetPropertyOrDefault<object>("result") ?? 299.99m);
                var discount = total * 0.1m; // 10% discount
                var properties = new Dictionary<string, string>
                {
                    ["OriginalTotal"] = total.ToString("F2"),
                    ["DiscountRate"] = "0.1",
                    ["DiscountAmount"] = discount.ToString("F2"),
                    ["OperationType"] = "DiscountCalculation"
                };
                foundry.Logger.LogInformation(properties, "Applied discount: {Discount:F2}", discount);
                await Task.Delay(25, ct);
                var finalTotal = total - discount;
                foundry.SetProperty("finalTotal", finalTotal);
            })

            // Final action using factory
            .AddOperation(WorkflowOperations.CreateAction("SendConfirmation",
                input => Console.WriteLine($"   [CONFIRMATION] Order completed! Processing finished.")))

            .Build();

        using var foundry6 = WorkflowForge.CreateFoundry("MixedPattern");
        foundry6.SetProperty("input", "Mixed pattern order processing");

        using var smith6 = WorkflowForge.CreateSmith();
        await smith6.ForgeAsync(workflow6, foundry6);
        Console.WriteLine("Mixed patterns workflow completed\n");

        // Summary
        Console.WriteLine("\nOperation Creation Patterns Demonstrated:");
        Console.WriteLine("  All 6 patterns successfully executed");
        Console.WriteLine("  Recommended: Class-based operations (Pattern 1) for production apps");
    }
}

// Custom Operation Classes for Demonstration

/// <summary>
/// Example custom operation using direct constructor pattern
/// </summary>
public class CustomBusinessOperation : WorkflowOperationBase
{
    private readonly string _processId;
    private readonly string _dataType;

    public CustomBusinessOperation(string processId, string dataType)
    {
        _processId = processId ?? throw new ArgumentNullException(nameof(processId));
        _dataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
    }

    public override string Name => $"CustomBusiness_{_processId}";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var properties = new Dictionary<string, string>
        {
            ["ProcessId"] = _processId,
            ["DataType"] = _dataType,
            ["OperationType"] = "CustomBusiness",
            ["InputDataType"] = inputData?.GetType().Name ?? "null"
        };
        foundry.Logger.LogInformation(properties, "Processing {DataType} with ID {ProcessId}", _dataType, _processId);
        await Task.Delay(100, cancellationToken);

        var result = new
        {
            ProcessId = _processId,
            DataType = _dataType,
            Input = inputData?.ToString(),
            ProcessedAt = DateTime.Now,
            Status = "Completed"
        };

        foundry.Properties[$"customResult_{_processId}"] = result;
        return result;
    }
}

/// <summary>
/// Example strongly-typed operation using base class
/// </summary>
public class TypedMathOperation : WorkflowOperationBase<int, double>
{
    private readonly int _multiplier;

    public TypedMathOperation(int multiplier)
    {
        _multiplier = multiplier;
    }

    public override string Name => $"MathOperation_x{_multiplier}";

    protected override async Task<double> ForgeAsyncCore(int inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var properties = new Dictionary<string, string>
        {
            ["Multiplier"] = _multiplier.ToString(),
            ["InputValue"] = inputData.ToString(),
            ["OperationType"] = "MathOperation",
            ["CalculationType"] = "Multiplication"
        };
        foundry.Logger.LogInformation(properties, "Calculating {Input} * {Multiplier}", inputData, _multiplier);
        await Task.Delay(50, cancellationToken);

        var result = inputData * _multiplier * 1.5;
        foundry.Properties["mathResult"] = result;
        return result;
    }
}

/// <summary>
/// Example operation with compensation support
/// </summary>
public class PaymentOperation : WorkflowOperationBase
{
    private readonly decimal _amount;

    public PaymentOperation(decimal amount)
    {
        _amount = amount;
    }

    public override string Name => "PaymentProcessor";
    public override bool SupportsRestore => true;

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var transactionId = $"PAY_{Guid.NewGuid():N}"[..10];
        var properties = new Dictionary<string, string>
        {
            ["Amount"] = _amount.ToString("F2"),
            ["TransactionId"] = transactionId,
            ["OperationType"] = "Payment",
            ["Currency"] = "USD"
        };
        foundry.Logger.LogInformation(properties, "Processing payment: ${Amount:F2}", _amount);

        await Task.Delay(120, cancellationToken);

        foundry.Properties["paymentTransactionId"] = transactionId;
        foundry.Properties["paymentAmount"] = _amount;

        return new { TransactionId = transactionId, Amount = _amount, Status = "Charged" };
    }

    public override async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var transactionId = foundry.Properties.GetValueOrDefault("paymentTransactionId");
        var amount = foundry.Properties.GetValueOrDefault("paymentAmount");

        var properties = new Dictionary<string, string>
        {
            ["TransactionId"] = transactionId?.ToString() ?? "unknown",
            ["Amount"] = amount?.ToString() ?? "0",
            ["OperationType"] = "PaymentRefund",
            ["RestoreAction"] = "Refund"
        };
        foundry.Logger.LogWarning(properties, "Refunding payment: {TransactionId} for ${Amount:F2}", transactionId, amount);
        await Task.Delay(80, cancellationToken);

        foundry.Properties["paymentRefunded"] = true;
    }
}

/// <summary>
/// Example operation for order validation
/// </summary>
public class OrderValidationSampleOperation : WorkflowOperationBase<Order, Order>
{
    public override string Name => "ValidateOrder";

    protected override async Task<Order> ForgeAsyncCore(Order inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var properties = new Dictionary<string, string>
        {
            ["OrderId"] = inputData.Id,
            ["CustomerName"] = inputData.CustomerName,
            ["ItemCount"] = inputData.Items.Count.ToString(),
            ["OperationType"] = "OrderValidation",
            ["ValidationStep"] = "Initial"
        };
        foundry.Logger.LogInformation(properties, "Validating order {OrderId} with {ItemCount} items",
            inputData.Id, inputData.Items.Count);

        await Task.Delay(75, cancellationToken);

        if (string.IsNullOrEmpty(inputData.Id))
            throw new InvalidOperationException("Order ID is required");

        if (!inputData.Items.Any())
            throw new InvalidOperationException("Order must contain at least one item");

        foundry.Properties["orderValidated"] = true;
        return inputData;
    }
}

/// <summary>
/// Example operation with dependency injection support for application scenarios
/// </summary>
public class NotificationOperation : WorkflowOperationBase
{
    private readonly string _templateName;
    private readonly Dictionary<string, object> _parameters;

    public NotificationOperation(string templateName, Dictionary<string, object>? parameters = null)
    {
        _templateName = templateName ?? throw new ArgumentNullException(nameof(templateName));
        _parameters = parameters ?? new Dictionary<string, object>();
    }

    public override string Name => $"EmailNotification_{_templateName}";
    public override bool SupportsRestore => true; // Can send cancellation emails

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var emailId = $"EMAIL_{Guid.NewGuid():N}"[..12];
        var recipient = _parameters.GetValueOrDefault("recipient", "customer@example.com");

        var properties = new Dictionary<string, string>
        {
            ["EmailId"] = emailId,
            ["Template"] = _templateName,
            ["Recipient"] = recipient.ToString(),
            ["OperationType"] = "EmailNotification",
            ["NotificationType"] = "Outbound"
        };
        foundry.Logger.LogInformation(properties, "Sending email notification using template {Template}", _templateName);

        foundry.Logger.LogInformation(properties, "Sending email {EmailId} to {Recipient}", emailId, recipient);
        await Task.Delay(200, cancellationToken); // Simulate email sending

        var result = new
        {
            EmailId = emailId,
            Template = _templateName,
            Recipient = recipient,
            SentAt = DateTime.UtcNow,
            Status = "Sent"
        };

        foundry.Properties["emailId"] = emailId;
        foundry.Properties["emailRecipient"] = recipient;

        return result;
    }

    public override async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var emailId = foundry.Properties.GetValueOrDefault("emailId");
        var recipient = foundry.Properties.GetValueOrDefault("emailRecipient");

        var properties = new Dictionary<string, string>
        {
            ["OriginalEmailId"] = emailId?.ToString() ?? "unknown",
            ["Recipient"] = recipient?.ToString() ?? "unknown",
            ["OperationType"] = "EmailNotification",
            ["NotificationType"] = "Cancellation",
            ["RestoreAction"] = "SendCancellation"
        };
        foundry.Logger.LogWarning(properties, "Sending cancellation email for original {EmailId} to {Recipient}", emailId, recipient);
        await Task.Delay(150, cancellationToken);

        foundry.Properties["cancellationEmailSent"] = true;
    }
}

/// <summary>
/// Example async operation that processes data with external API calls
/// </summary>
public class ExternalApiDataProcessor : WorkflowOperationBase<string, ApiResponse>
{
    private readonly string _apiEndpoint;
    private readonly TimeSpan _timeout;

    public ExternalApiDataProcessor(string apiEndpoint, TimeSpan timeout = default)
    {
        _apiEndpoint = apiEndpoint ?? throw new ArgumentNullException(nameof(apiEndpoint));
        _timeout = timeout == default ? TimeSpan.FromSeconds(30) : timeout;
    }

    public override string Name => "ExternalApiProcessor";

    protected override async Task<ApiResponse> ForgeAsyncCore(string inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        foundry.Logger.LogInformation("Processing data through external API: {Endpoint}", _apiEndpoint);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_timeout);

        try
        {
            // Simulate external API call
            foundry.Logger.LogDebug("Calling external API with data: {Data}", inputData);
            await Task.Delay(300, cts.Token); // Simulate API latency

            var response = new ApiResponse
            {
                Success = true,
                Data = $"API_PROCESSED_{inputData}_{DateTime.Now:HHmmss}",
                RequestId = Guid.NewGuid().ToString("N")[..8],
                ProcessedAt = DateTime.UtcNow
            };

            foundry.Logger.LogInformation("External API call completed successfully. RequestId: {RequestId}", response.RequestId);
            foundry.Properties["apiRequestId"] = response.RequestId;

            return response;
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            foundry.Logger.LogError("External API call timed out after {Timeout}", _timeout);
            throw new TimeoutException($"External API call timed out after {_timeout}");
        }
    }
}

/// <summary>
/// Example operation that demonstrates validation with detailed error reporting
/// </summary>
public class ComprehensiveValidationOperation : WorkflowOperationBase<ValidationRequest, ValidationResult>
{
    private readonly List<IValidationRule> _validationRules;

    public ComprehensiveValidationOperation(params IValidationRule[] rules)
    {
        _validationRules = rules?.ToList() ?? new List<IValidationRule>();
        if (!_validationRules.Any())
        {
            // Add default validation rules
            _validationRules.Add(new NotNullValidationRule());
            _validationRules.Add(new StringLengthValidationRule(1, 100));
        }
    }

    public override string Name => "ComprehensiveValidation";

    protected override async Task<ValidationResult> ForgeAsyncCore(ValidationRequest inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        foundry.Logger.LogInformation("Starting comprehensive validation with {RuleCount} rules", _validationRules.Count);

        var result = new ValidationResult
        {
            IsValid = true,
            Errors = new List<string>(),
            ValidatedAt = DateTime.UtcNow
        };

        foreach (var rule in _validationRules)
        {
            foundry.Logger.LogDebug("Applying validation rule: {RuleName}", rule.GetType().Name);

            if (!await rule.ValidateAsync(inputData, cancellationToken))
            {
                result.IsValid = false;
                result.Errors.Add(rule.ErrorMessage);
                foundry.Logger.LogWarning("Validation failed: {Error}", rule.ErrorMessage);
            }
            else
            {
                foundry.Logger.LogDebug("Validation rule passed: {RuleName}", rule.GetType().Name);
            }
        }

        foundry.Logger.LogInformation("Validation completed. IsValid: {IsValid}, ErrorCount: {ErrorCount}",
            result.IsValid, result.Errors.Count);

        foundry.Properties["validationResult"] = result;
        return result;
    }
}

// Supporting Classes for Advanced Examples

public class ApiResponse
{
    public bool Success { get; set; }
    public string Data { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}

public class ValidationRequest
{
    public string Data { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime ValidatedAt { get; set; }
}

public interface IValidationRule
{
    string ErrorMessage { get; }

    Task<bool> ValidateAsync(ValidationRequest request, CancellationToken cancellationToken);
}

public class NotNullValidationRule : IValidationRule
{
    public string ErrorMessage => "Data cannot be null or empty";

    public Task<bool> ValidateAsync(ValidationRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(!string.IsNullOrEmpty(request.Data));
    }
}

public class StringLengthValidationRule : IValidationRule
{
    private readonly int _minLength;
    private readonly int _maxLength;

    public StringLengthValidationRule(int minLength, int maxLength)
    {
        _minLength = minLength;
        _maxLength = maxLength;
    }

    public string ErrorMessage => $"Data length must be between {_minLength} and {_maxLength} characters";

    public Task<bool> ValidateAsync(ValidationRequest request, CancellationToken cancellationToken)
    {
        var length = request.Data?.Length ?? 0;
        return Task.FromResult(length >= _minLength && length <= _maxLength);
    }
}

// Data Models for Demonstration

public class Order
{
    public string Id { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}