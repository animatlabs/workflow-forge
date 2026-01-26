# WorkflowForge.Extensions.Validation

<p align="center">
  <img src="https://raw.githubusercontent.com/animatlabs/workflow-forge/main/icon.png" alt="WorkflowForge" width="120" height="120">
</p>

Validation extension for WorkflowForge with DataAnnotations-based validation for comprehensive input validation.

[![NuGet](https://img.shields.io/nuget/v/WorkflowForge.Extensions.Validation.svg)](https://www.nuget.org/packages/WorkflowForge.Extensions.Validation/)

## No External Validation Dependencies

This extension uses **System.ComponentModel.DataAnnotations**, so you get validation without additional third-party validation libraries.

## Installation

```bash
dotnet add package WorkflowForge.Extensions.Validation
```

**Requires**: .NET Standard 2.0 or later

## Quick Start

```csharp
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

## Key Features

- **DataAnnotations Validation**: Attributes and IValidatableObject support
- **Middleware-Based**: Validates before operations execute
- **Flexible Extraction**: Custom data extraction from foundry
- **Configurable Behavior**: Throw or log validation failures
- **Property Validation**: Validate foundry properties
- **Rich Error Messages**: Detailed validation error information

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

### Via Code

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

### Via Dependency Injection

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Extensions.Validation;

services.AddValidationConfiguration(configuration);
var options = serviceProvider.GetRequiredService<IOptions<ValidationMiddlewareOptions>>().Value;
```

See [Configuration Guide](../../../docs/core/configuration.md#validation-extension) for complete options.

## Validation Examples

### Complex Validation Rules

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

### Custom Error Handling

```csharp
foundry.UseValidation(
    f => f.GetPropertyOrDefault<Order>("Order"),
    new ValidationMiddlewareOptions { ThrowOnValidationError = false });
```

## Documentation

- **[Getting Started](../../../docs/getting-started/getting-started.md)**
- **[Configuration Guide](../../../docs/core/configuration.md#validation-extension)**
- **[Extensions Overview](../../../docs/extensions/index.md)**
- **[Sample 23: Validation](../../samples/WorkflowForge.Samples.BasicConsole/README.md)**

---

**WorkflowForge.Extensions.Validation** - *Build workflows with industrial strength*
