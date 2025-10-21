# WorkflowForge.Extensions.Validation

<p align="center">
  <img src="../../../icon.png" alt="WorkflowForge" width="120" height="120">
</p>

Validation extension for WorkflowForge with FluentValidation integration for comprehensive input validation.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Validation.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Validation/)

## Zero Version Conflicts

**This extension uses Costura.Fody to embed FluentValidation.** This means:

- NO DLL Hell - No conflicts with your application's FluentValidation version
- NO Version Conflicts - Works with ANY version of FluentValidation in your app
- Clean Deployment - Professional dependency isolation

**How it works**: FluentValidation is embedded as compressed resources at build time and loaded at runtime, completely isolated from your application.

## Installation

```bash
dotnet add package WorkflowForge.Extensions.Validation
```

**Requires**: .NET Standard 2.0 or later

## Quick Start

```csharp
using WorkflowForge.Extensions.Validation;
using FluentValidation;

// Define validator
public class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("Customer ID required");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be positive");
        RuleFor(x => x.Items).NotEmpty().WithMessage("Order must have items");
    }
}

// Configure validation
using var foundry = WorkflowForge.CreateFoundry("ValidatedWorkflow");
foundry.SetProperty("Order", order);

var validator = new OrderValidator();
foundry.AddMiddleware(new ValidationMiddleware<Order>(
    validator,
    f => f.GetPropertyOrDefault<Order>("Order"),
    throwOnFailure: true
));

// Validation runs before every operation
await smith.ForgeAsync(workflow, foundry);
```

## Key Features

- **FluentValidation Integration**: Full FluentValidation API access
- **Middleware-Based**: Validates before operations execute
- **Flexible Extraction**: Custom data extraction from foundry
- **Configurable Behavior**: Throw or log validation failures
- **Property Validation**: Validate foundry properties
- **Rich Error Messages**: Detailed validation error information

## Configuration

```csharp
// From foundry properties
var validator = new OrderValidator();
foundry.AddMiddleware(new ValidationMiddleware<Order>(
    validator,
    f => f.GetPropertyOrDefault<Order>("Order"),
    throwOnFailure: true  // Throw ValidationException on failure
));
```

See [Configuration Guide](../../../docs/configuration.md#validation-extension) for complete options.

## Validation Examples

### Complex Validation Rules

```csharp
public class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .Length(5, 20);
            
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .LessThan(10000);
            
        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email));
            
        RuleForEach(x => x.Items)
            .SetValidator(new OrderItemValidator());
    }
}
```

### Custom Error Handling

```csharp
foundry.AddMiddleware(new ValidationMiddleware<Order>(
    validator,
    f => f.GetPropertyOrDefault<Order>("Order"),
    throwOnFailure: false  // Log instead of throw
));
```

## Documentation

- **[Getting Started](../../../docs/getting-started.md)**
- **[Configuration Guide](../../../docs/configuration.md#validation-extension)**
- **[Extensions Overview](../../../docs/extensions.md)**
- **[Sample 23: Validation](../../samples/WorkflowForge.Samples.BasicConsole/)**

---

**WorkflowForge.Extensions.Validation** - *Build workflows with industrial strength*
