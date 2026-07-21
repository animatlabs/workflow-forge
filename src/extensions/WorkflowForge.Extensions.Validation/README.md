# WorkflowForge.Extensions.Validation

Run `System.ComponentModel.DataAnnotations` validation before each operation, using a function that reads the object to validate from the foundry.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Validation.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Validation/)

## Install

```bash
dotnet add package WorkflowForge.Extensions.Validation
```

Targets .NET Standard 2.0. Validation uses BCL DataAnnotations plus `Microsoft.Extensions.*` (options/DI integration); no third-party validation library.

## Quick Start

```csharp
using WorkflowForge;
using WorkflowForge.Extensions.Validation;
using System.ComponentModel.DataAnnotations;

// Define validator
public class Order : IValidatableObject
{
    [Required(ErrorMessage = "Customer ID required")]
    public string CustomerId { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be positive")]
    public decimal Amount { get; set; }

    [MinLength(1, ErrorMessage = "Order must have items")]
    public string[] Items { get; set; } = Array.Empty<string>();

    public IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Validate(ValidationContext context)
    {
        if (!CustomerId.StartsWith("CUST-"))
            yield return new System.ComponentModel.DataAnnotations.ValidationResult(
                "Customer ID must start with 'CUST-'",
                new[] { nameof(CustomerId) });
    }
}

// Configure validation
using var foundry = WorkflowForge.CreateFoundry("ValidatedWorkflow");
foundry.SetProperty("Order", order);

foundry.UseValidation(f => f.GetPropertyOrDefault<Order>("Order"));

// Validation runs before every operation
await smith.ForgeAsync(workflow, foundry);
```

## Key points

- Middleware runs the extractor before each operation; failures can throw, log, or stash results based on options.
- Supports attributes plus `IValidatableObject` for cross-field rules.
- `ValidationMiddlewareOptions` controls throw vs log vs store behavior.

## Configuration

### Via appsettings.json

```json
{
  "WorkflowForge": {
    "Extensions": {
      "Validation": {
        "Enabled": true,
        "ThrowOnValidationError": true,
        "LogValidationErrors": true,
        "StoreValidationResults": true,
        "IgnoreValidationFailures": false
      }
    }
  }
}
```

### Via code

```csharp
using WorkflowForge.Extensions.Validation.Options;

var options = new ValidationMiddlewareOptions
{
    Enabled = true,
    ThrowOnValidationError = true,
    LogValidationErrors = true,
    StoreValidationResults = true
};

foundry.UseValidation(f => f.GetPropertyOrDefault<Order>("Order"), options);
```

### Via dependency injection

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Extensions.Validation;

services.AddValidationConfiguration(configuration);
var options = serviceProvider.GetRequiredService<IOptions<ValidationMiddlewareOptions>>().Value;
```

[Validation configuration](../../../docs/core/configuration.md#validation-extension)

## Validation examples

### Complex validation rules

```csharp
public class Order : IValidatableObject
{
    [Required]
    public string CustomerId { get; set; } = string.Empty;

    [Range(0.01, 10000)]
    public decimal Amount { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Validate(ValidationContext context)
    {
        if (CustomerId.Length < 5)
            yield return new System.ComponentModel.DataAnnotations.ValidationResult(
                "Customer ID must be at least 5 characters",
                new[] { nameof(CustomerId) });
    }
}
```

### Custom error handling

```csharp
foundry.UseValidation(
    f => f.GetPropertyOrDefault<Order>("Order"),
    new ValidationMiddlewareOptions { ThrowOnValidationError = false });
```

## Links

- [Getting Started](../../../docs/getting-started/getting-started.md)
- [Configuration Guide](../../../docs/core/configuration.md#validation-extension)
- [Extensions Overview](../../../docs/extensions/index.md)
- [Sample 23: Validation](../../samples/WorkflowForge.Samples.BasicConsole/README.md)
