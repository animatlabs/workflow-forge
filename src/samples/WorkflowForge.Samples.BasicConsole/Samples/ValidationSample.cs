using FluentValidation;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Extensions.Validation;
using WorkflowForge.Extensions.Validation.Options;
using WF = WorkflowForge;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates validation capabilities using WorkflowForge.Extensions.Validation
/// </summary>
public class ValidationSample : ISample
{
    public string Name => "Validation Extension";
    public string Description => "Demonstrates FluentValidation integration and validation patterns";

    public async Task RunAsync()
    {
        Console.WriteLine("WorkflowForge Validation Extension Sample");
        Console.WriteLine("==========================================");
        Console.WriteLine();

        // Sample 1: Basic Validation
        await RunBasicValidation();
        Console.WriteLine();

        // Sample 2: Middleware Validation
        await RunMiddlewareValidation();
        Console.WriteLine();

        // Sample 3: Multiple Validators
        await RunMultipleValidators();
        Console.WriteLine();

        // Sample 4: Validation with Error Handling
        await RunValidationWithErrorHandling();
    }

    private async Task RunBasicValidation()
    {
        Console.WriteLine("1. Basic Manual Validation");
        Console.WriteLine("   -----------------------");

        using var foundry = WF.WorkflowForge.CreateFoundry("OrderValidation");

        // Create test order
        var order = new Order
        {
            CustomerId = "CUST-123",
            Amount = 99.99m,
            Currency = "USD",
            Items = new[] { "Product1", "Product2" }
        };

        foundry.SetProperty("Order", order);

        // Create workflow with manual validation
        var workflow = WF.WorkflowForge.CreateWorkflow()
            .WithName("ProcessOrder")
            .AddOperation(new ValidateOrderOperation())
            .AddOperation(new ProcessOrderOperation())
            .Build();

        using var smith = WF.WorkflowForge.CreateSmith();
        await smith.ForgeAsync(workflow, foundry);

        Console.WriteLine($"   Order validated and processed successfully");
        Console.WriteLine($"   Order ID: {order.CustomerId}");
        Console.WriteLine($"   Amount: {order.Amount:C} {order.Currency}");
    }

    private async Task RunMiddlewareValidation()
    {
        Console.WriteLine("2. Automatic Middleware Validation");
        Console.WriteLine("   --------------------------------");

        using var foundry = WF.WorkflowForge.CreateFoundry("AutoValidation");

        // Add validation middleware
        var validator = new OrderValidator();
        var options = new ValidationMiddlewareOptions { ThrowOnValidationError = true };
        foundry.UseValidation(
            validator,
            f => f.GetPropertyOrDefault<Order>("Order"),
            options);

        // Create order
        var order = new Order
        {
            CustomerId = "CUST-456",
            Amount = 250.00m,
            Currency = "EUR",
            Items = new[] { "Premium Product" }
        };

        foundry.SetProperty("Order", order);

        // Workflow - validation happens automatically via middleware
        var workflow = WF.WorkflowForge.CreateWorkflow()
            .WithName("AutoValidatedOrder")
            .AddOperation(new ProcessOrderOperation())
            .Build();

        using var smith = WF.WorkflowForge.CreateSmith();
        await smith.ForgeAsync(workflow, foundry);

        Console.WriteLine($"   Order auto-validated and processed");
        Console.WriteLine($"   Validation Status: {foundry.GetPropertyOrDefault<string>("Validation.ProcessOrder.Status")}");
    }

    private async Task RunMultipleValidators()
    {
        Console.WriteLine("3. Multiple Validation Stages");
        Console.WriteLine("   ---------------------------");

        using var foundry = WF.WorkflowForge.CreateFoundry("MultiStageValidation");

        var order = new Order
        {
            CustomerId = "CUST-789",
            Amount = 1500.00m,
            Currency = "GBP",
            Items = new[] { "High Value Item" }
        };

        foundry.SetProperty("Order", order);

        // Stage 1: Basic validation
        var basicValidator = new OrderValidator();
        var basicResult = await foundry.ValidateAsync(basicValidator, order, "BasicValidation");
        Console.WriteLine($"   Basic Validation: {(basicResult.IsValid ? "PASSED" : "FAILED")}");

        // Stage 2: Business rules validation
        var businessValidator = new BusinessRuleValidator();
        var businessResult = await foundry.ValidateAsync(businessValidator, order, "BusinessValidation");
        Console.WriteLine($"   Business Rules: {(businessResult.IsValid ? "PASSED" : "FAILED")}");

        if (basicResult.IsValid && businessResult.IsValid)
        {
            Console.WriteLine($"   All validations passed - processing order");
            foundry.SetProperty("ValidationComplete", true);
        }
    }

    private async Task RunValidationWithErrorHandling()
    {
        Console.WriteLine("4. Validation with Error Handling");
        Console.WriteLine("   -------------------------------");

        using var foundry = WF.WorkflowForge.CreateFoundry("ValidationErrorHandling");

        // Invalid order
        var invalidOrder = new Order
        {
            CustomerId = "",  // Invalid - empty
            Amount = -10,     // Invalid - negative
            Currency = "XXX", // Invalid - unknown currency
            Items = Array.Empty<string>() // Invalid - no items
        };

        foundry.SetProperty("Order", invalidOrder);

        // Add validation with throwOnFailure = false
        var validator = new OrderValidator();
        var options = new ValidationMiddlewareOptions { ThrowOnValidationError = false };
        foundry.UseValidation(
            validator,
            f => f.GetPropertyOrDefault<Order>("Order"),
            options);

        var workflow = WF.WorkflowForge.CreateWorkflow()
            .WithName("ErrorHandledOrder")
            .AddOperation(new CheckValidationOperation())
            .Build();

        using var smith = WF.WorkflowForge.CreateSmith();
        await smith.ForgeAsync(workflow, foundry);

        // Check validation results
        var status = foundry.GetPropertyOrDefault<string>("Validation.CheckValidation.Status");
        var errors = foundry.GetPropertyOrDefault<System.Collections.Generic.IReadOnlyList<ValidationError>>(
            "Validation.CheckValidation.Errors");

        Console.WriteLine($"   Validation Status: {status}");
        if (errors != null && errors.Count > 0)
        {
            Console.WriteLine($"   Validation Errors:");
            foreach (var error in errors)
            {
                Console.WriteLine($"     - {error.PropertyName}: {error.ErrorMessage}");
            }
        }
    }

    // Model classes
    public class Order
    {
        public string CustomerId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string[] Items { get; set; } = Array.Empty<string>();
    }

    // Validators
    public class OrderValidator : AbstractValidator<Order>
    {
        public OrderValidator()
        {
            RuleFor(x => x.CustomerId)
                .NotEmpty().WithMessage("Customer ID is required")
                .Must(id => id.StartsWith("CUST-")).WithMessage("Customer ID must start with 'CUST-'");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than 0");

            RuleFor(x => x.Currency)
                .Must(c => c == "USD" || c == "EUR" || c == "GBP")
                .WithMessage("Currency must be USD, EUR, or GBP");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("At least one item is required");
        }
    }

    public class BusinessRuleValidator : AbstractValidator<Order>
    {
        public BusinessRuleValidator()
        {
            RuleFor(x => x.Amount)
                .LessThan(10000).WithMessage("Orders over $10,000 require manager approval");
        }
    }

    // Operations
    public class ValidateOrderOperation : IWorkflowOperation
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name => "ValidateOrder";
        public bool SupportsRestore => false;

        public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            var order = foundry.GetPropertyOrDefault<Order>("Order");
            if (order == null)
            {
                throw new InvalidOperationException("Order not found");
            }

            var validator = new OrderValidator();
            var result = await foundry.ValidateAsync(validator, order);

            if (!result.IsValid)
            {
                throw new WorkflowValidationException("Order validation failed", result.Errors);
            }

            return null;
        }

        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        { }
    }

    public class ProcessOrderOperation : IWorkflowOperation
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name => "ProcessOrder";
        public bool SupportsRestore => false;

        public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            // Processing logic would go here
            return Task.FromResult<object?>("Order processed successfully");
        }

        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        { }
    }

    public class CheckValidationOperation : IWorkflowOperation
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name => "CheckValidation";
        public bool SupportsRestore => false;

        public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            var status = foundry.GetPropertyOrDefault<string>("Validation.CheckValidation.Status");

            if (status == "Failed")
            {
                foundry.Logger.LogWarning("Validation failed - order cannot be processed");
                return Task.FromResult<object?>("Order rejected due to validation errors");
            }

            return Task.FromResult<object?>("Validation check completed");
        }

        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        { }
    }
}