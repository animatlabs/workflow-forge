# WorkflowForge.Extensions.Validation

FluentValidation bridge extension for WorkflowForge providing comprehensive validation capabilities for workflow operations, inputs, and data flow.

## Installation

```bash
dotnet add package WorkflowForge.Extensions.Validation
```

## Quick Start

### 1. Define Your Validator

```csharp
using FluentValidation;

public class OrderRequest
{
    public string CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
}

public class OrderValidator : AbstractValidator<OrderRequest>
{
    public OrderValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0");

        RuleFor(x => x.Currency)
            .Must(c => c == "USD" || c == "EUR" || c == "GBP")
            .WithMessage("Invalid currency code");
    }
}
```

### 2. Add Validation to Your Workflow

```csharp
using WorkflowForge;
using WorkflowForge.Extensions.Validation;

var foundry = WorkflowForge.CreateFoundry("OrderProcessing");

// Store order request in foundry properties
foundry.SetProperty("OrderRequest", new OrderRequest
{
    CustomerId = "CUST-123",
    Amount = 99.99m,
    Currency = "USD"
});

// Add validation middleware
var validator = new OrderValidator();
foundry.AddValidation(
    validator,
    f => f.GetPropertyOrDefault<OrderRequest>("OrderRequest"),
    throwOnFailure: true);

// Build and execute workflow
var workflow = WorkflowForge.CreateWorkflow("ProcessOrder")
    .AddOperation(new ValidateOrderOperation())
    .AddOperation(new ChargePaymentOperation())
    .AddOperation(new FulfillOrderOperation())
    .Build();

using var smith = WorkflowForge.CreateSmith();
await smith.ForgeAsync(workflow, foundry);
```

## Key Features

- **FluentValidation Bridge**: Seamless integration with FluentValidation library
- **Middleware-Based**: Validation executes as part of the middleware pipeline
- **Flexible Error Handling**: Choose to throw exceptions or log and continue
- **Property-Based**: Validation results stored in foundry properties
- **Type-Safe**: Full generic type support for validation
- **Custom Validators**: Support for both FluentValidation and custom validators

## Usage Patterns

### Middleware Validation (Recommended)

Validation executes before each operation:

```csharp
foundry.AddValidation(
    new CustomerValidator(),
    f => f.GetPropertyOrDefault<Customer>("Customer"),
    throwOnFailure: true);
```

### Manual Validation

Validate data explicitly within operations:

```csharp
public class ProcessCustomerOperation : IWorkflowOperation
{
    public async Task<object?> ForgeAsync(
        IWorkflowFoundry foundry,
        object? inputData,
        CancellationToken cancellationToken)
    {
        var customer = foundry.GetPropertyOrDefault<Customer>("Customer");
        var validator = new CustomerValidator();
        
        var result = await foundry.ValidateAsync(validator, customer);
        
        if (!result.IsValid)
        {
            throw new WorkflowValidationException(
                "Customer validation failed",
                result.Errors);
        }
        
        // Continue processing...
        return null;
    }
}
```

### Custom Validators

Implement `IWorkflowValidator<T>` for custom validation logic:

```csharp
public class CustomBusinessRuleValidator : IWorkflowValidator<Order>
{
    public async Task<ValidationResult> ValidateAsync(
        Order data,
        CancellationToken cancellationToken)
    {
        // Custom validation logic
        if (data.Amount > 10000 && !data.RequiresApproval)
        {
            return ValidationResult.Failure(
                new ValidationError("RequiresApproval", 
                    "Orders over $10,000 require approval"));
        }
        
        return ValidationResult.Success;
    }
}

// Use custom validator
foundry.AddValidation(
    new CustomBusinessRuleValidator(),
    f => f.GetPropertyOrDefault<Order>("Order"));
```

## Error Handling

### Throw on Failure (Default)

```csharp
foundry.AddValidation(validator, dataExtractor, throwOnFailure: true);
// Throws WorkflowValidationException on validation failure
```

### Log and Continue

```csharp
foundry.AddValidation(validator, dataExtractor, throwOnFailure: false);
// Logs error and continues execution
// Check validation status in foundry properties
```

### Accessing Validation Results

```csharp
var isValid = foundry.GetPropertyOrDefault<bool>(
    "Validation.MyOperation.Status");

var errors = foundry.GetPropertyOrDefault<IReadOnlyList<ValidationError>>(
    "Validation.MyOperation.Errors");
```

## Advanced Scenarios

### Multi-Step Validation

```csharp
// Validate at different stages
foundry.AddValidation(
    new InputValidator(),
    f => f.GetPropertyOrDefault<InputData>("Input"));

foundry.AddValidation(
    new BusinessRuleValidator(),
    f => f.GetPropertyOrDefault<ProcessedData>("Processed"));

foundry.AddValidation(
    new OutputValidator(),
    f => f.GetPropertyOrDefault<OutputData>("Output"));
```

### Conditional Validation

```csharp
public class ConditionalValidationOperation : IWorkflowOperation
{
    public async Task<object?> ForgeAsync(
        IWorkflowFoundry foundry,
        object? inputData,
        CancellationToken cancellationToken)
    {
        var orderType = foundry.GetPropertyOrDefault<string>("OrderType");
        
        IValidator<Order> validator = orderType switch
        {
            "Standard" => new StandardOrderValidator(),
            "Express" => new ExpressOrderValidator(),
            "International" => new InternationalOrderValidator(),
            _ => new DefaultOrderValidator()
        };
        
        var order = foundry.GetPropertyOrDefault<Order>("Order");
        var result = await foundry.ValidateAsync(validator, order);
        
        if (!result.IsValid)
        {
            throw new WorkflowValidationException(
                $"Order validation failed for type {orderType}",
                result.Errors);
        }
        
        return null;
    }
}
```

### Validation with Dependencies

```csharp
public class OrderValidator : AbstractValidator<Order>
{
    private readonly ICustomerRepository _customerRepo;
    
    public OrderValidator(ICustomerRepository customerRepo)
    {
        _customerRepo = customerRepo;
        
        RuleFor(x => x.CustomerId)
            .MustAsync(async (id, ct) => await CustomerExists(id, ct))
            .WithMessage("Customer not found");
    }
    
    private async Task<bool> CustomerExists(
        string customerId,
        CancellationToken cancellationToken)
    {
        return await _customerRepo.ExistsAsync(customerId, cancellationToken);
    }
}

// Register validator with DI
var customerRepo = serviceProvider.GetRequiredService<ICustomerRepository>();
var validator = new OrderValidator(customerRepo);
foundry.AddValidation(validator, f => f.GetPropertyOrDefault<Order>("Order"));
```

## Best Practices

1. **Validate Early**: Add validation middleware early in the pipeline
2. **Use FluentValidation**: Leverage the rich FluentValidation ecosystem
3. **Store in Properties**: Keep validated data in foundry properties
4. **Type Safety**: Use strongly-typed validators
5. **Clear Messages**: Provide meaningful error messages
6. **Test Validators**: Unit test validators separately
7. **Fail Fast**: Throw on validation failure in critical paths
8. **Log Everything**: Validation results are automatically logged

## Integration with WorkflowForge

### Middleware Ordering

```csharp
// Best practice: Validation before other middleware
foundry.AddValidation(validator, dataExtractor);      // Validates first
foundry.AddMiddleware(new TimingMiddleware());        // Times valid operations
foundry.AddMiddleware(new ErrorHandlingMiddleware()); // Handles validation exceptions
```

### Event Handling

```csharp
foundry.OperationFailed += (sender, args) =>
{
    if (args.Exception is WorkflowValidationException validationEx)
    {
        foreach (var error in validationEx.Errors)
        {
            Console.WriteLine($"Validation Error: {error}");
        }
    }
};
```

## Comparison with Other Approaches

| Approach | Pros | Cons |
|----------|------|------|
| Middleware Validation | Centralized, consistent, reusable | Less flexible per-operation |
| Manual Validation | Full control, operation-specific | Code duplication, inconsistent |
| Custom Validators | No external dependencies | More code to maintain |
| FluentValidation | Rich API, community support, testable | External dependency |

## Performance Considerations

- Validation adds ~1-5ms overhead per operation
- Async validators support cancellation
- Validation results cached in foundry properties
- FluentValidation is optimized for performance

## License

MIT License - See LICENSE file for details

