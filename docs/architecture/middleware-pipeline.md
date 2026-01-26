# Middleware Pipeline Architecture

## Overview

WorkflowForge implements the **Russian Doll Pattern** for middleware execution, which is an industry-standard approach used by ASP.NET Core, Express.js, and other modern frameworks. This document explains how the middleware pipeline works and provides best practices for middleware ordering.

## The Russian Doll Pattern

Middleware wraps in **REVERSE order of addition** to create a "Russian Doll" effect. This is intentional and correct behavior.

### Example Execution Flow

If you add middleware in this order:

```csharp
foundry.AddMiddleware(timingMiddleware);        // Added 1st
foundry.AddMiddleware(errorHandlingMiddleware); // Added 2nd
foundry.AddMiddleware(retryMiddleware);         // Added 3rd
```

The execution flow becomes:

```
Timing.Start
  → ErrorHandling.Start
    → Retry.Start
      → OPERATION EXECUTES
    ← Retry.End
  ← ErrorHandling.End
← Timing.End
```

### How It Works

The reverse iteration builds the execution chain from inside-out:

1. **Start with:** `next = operation.ForgeAsync`
2. **Wrap with retryMiddleware:** `next = () => retry.Execute(next)`
3. **Wrap with errorMiddleware:** `next = () => error.Execute(next)`
4. **Wrap with timingMiddleware:** `next = () => timing.Execute(next)`

**Final execution path:** timing → error → retry → operation → retry → error → timing

## Best Practices for Middleware Ordering

Add middleware in order of desired outer-to-inner wrapping:

1. **Observability (Timing, Logging) first** - Measures everything including error handling
2. **Error Handling second** - Catches all errors including retry failures
3. **Retry/Resilience third** - Wraps just the operation execution
4. **Business Logic (Validation, Audit) last** - Innermost, closest to the operation

### Example Optimal Ordering

```csharp
// Step 1: Observability - measures total execution time
foundry.EnablePerformanceMonitoring();

// Step 2: Resilience - retries failed operations
foundry.UsePollyRetry(maxRetryAttempts: 3);

// Step 3: Business logic - validates and audits
foundry.UseValidation<OrderDto>(f => f.GetPropertyOrDefault<OrderDto>("Order"));
foundry.UseAudit(auditProvider);
```

This ensures:
- Timing includes error handling time
- Error handlers can catch retry failures
- Validation and audit are closest to the operation
- All layers benefit from resilience

## Technical Implementation Details

### Why Reverse Iteration?

The code iterates backwards (`_middlewares.Count - 1` down to `0`) because:

- **Last middleware added should wrap first** (innermost)
- **Each iteration wraps the previous 'next' delegate** creating the chain
- **Results in correct execution order:** first added → first executed (outermost)

### Code Structure

```csharp
// Start with the core operation
Func<CancellationToken, Task<object?>> next = token => operation.ForgeAsync(inputData, this, token);

// Wrap each middleware in reverse order
for (int i = _middlewares.Count - 1; i >= 0; i--)
{
    var middleware = _middlewares[i];
    var currentNext = next;
    next = token => middleware.ExecuteAsync(operation, this, inputData, currentNext, token);
}

// Execute the fully-wrapped chain
return await next(cancellationToken).ConfigureAwait(false);
```

### Middleware Interface

Each middleware must implement:

```csharp
Task<object?> ExecuteAsync(
    IWorkflowOperation operation,
    IWorkflowFoundry foundry,
    object? inputData,
    Func<CancellationToken, Task<object?>> next,
    CancellationToken cancellationToken);
```

The `next` delegate represents "everything that comes after this middleware" in the chain.

## Common Pitfalls

### ❌ Wrong: Adding middleware after workflow execution
```csharp
var workflow = builder.Build();
await smith.ForgeAsync(workflow, foundry);
foundry.AddMiddleware(timingMiddleware); // Too late!
```

### ✅ Correct: Add middleware before execution
```csharp
foundry.AddMiddleware(timingMiddleware);
var workflow = builder.Build();
await smith.ForgeAsync(workflow, foundry);
```

### ❌ Wrong: Order-dependent middleware added in wrong order
```csharp
foundry.UseRetry();        // Innermost
foundry.UseErrorHandling(); // Should be outermost
```

### ✅ Correct: Error handling wraps retry
```csharp
foundry.UseErrorHandling(); // Outermost - catches retry errors
foundry.UseRetry();        // Innermost - wraps operation
```

## Debugging Middleware

To understand execution order, add logging middleware:

```csharp
foundry.AddMiddleware(new LoggingMiddleware("OUTER"));
foundry.AddMiddleware(new LoggingMiddleware("MIDDLE"));
foundry.AddMiddleware(new LoggingMiddleware("INNER"));

// Output will show:
// OUTER: Before
//   MIDDLE: Before
//     INNER: Before
//       [Operation executes]
//     INNER: After
//   MIDDLE: After
// OUTER: After
```

## Performance Considerations

- **Minimal overhead:** Each middleware adds one delegate invocation (~10ns)
- **Zero allocations:** The chain is built once per operation execution
- **Async-friendly:** Full support for async/await throughout the chain
- **Cancellation support:** CancellationToken threading through all layers

---

## Related Documentation

- [Operations Guide](../core/operations.md) - Middleware and operation patterns
- [Samples Guide](../getting-started/samples-guide.md) - Sample 12: Middleware
- [Performance](../performance/performance.md) - Performance best practices
