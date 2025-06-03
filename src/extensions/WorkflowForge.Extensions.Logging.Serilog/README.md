# WorkflowForge.Extensions.Logging.Serilog

Professional structured logging extension for WorkflowForge using Serilog. This extension provides seamless integration between WorkflowForge's logging infrastructure and Serilog's powerful structured logging capabilities.

## 🎯 Extension Overview

The Serilog extension brings professional-grade structured logging to WorkflowForge applications, including:

- **📝 Structured Logging**: Rich structured logs with context and properties
- **🔧 Zero Configuration**: Works out-of-the-box with existing Serilog setup
- **📊 Context Preservation**: Maintains workflow context across log entries
- **🎯 Property Enrichment**: Automatic enrichment with workflow metadata
- **🔗 Scope Support**: Proper scope management for correlated logging
- **⚡ High Performance**: Minimal overhead with efficient property handling
- **📋 Custom Properties**: Support for custom properties in log entries
- **🌍 Global Logger**: Support for both instance and global Serilog loggers
- **🏭 Foundry Integration**: Deep integration with WorkflowForge foundries and operations

## 📦 Installation

```bash
dotnet add package WorkflowForge.Extensions.Logging.Serilog
```

## 🔧 Prerequisites

You need Serilog configured in your application. This extension works with your existing Serilog configuration.

```bash
# Core Serilog packages (usually already installed)
dotnet add package Serilog
dotnet add package Serilog.Extensions.Logging
dotnet add package Serilog.Sinks.Console  # or your preferred sinks
```

## 🚀 Quick Start

### 1. Configure Serilog (if not already done)

```csharp
using Serilog;

// Configure Serilog with WorkflowForge-optimized settings
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "WorkflowForge")
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/workflows/workflow-.txt", 
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();
```

### 2. Enable Serilog for WorkflowForge

```csharp
using WorkflowForge;
using WorkflowForge.Extensions.Logging.Serilog;

// Option 1: Use global Serilog logger with foundry configuration
var foundryConfig = FoundryConfiguration.ForProduction()
    .UseSerilog();

var foundry = WorkflowForge.CreateFoundry("MyWorkflow", foundryConfig);

// Option 2: Use specific Serilog logger instance
var workflowLogger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Component", "WorkflowForge")
    .WriteTo.Console()
    .CreateLogger();

var foundryConfig = FoundryConfiguration.ForDevelopment()
    .UseSerilog(workflowLogger);

var foundry = WorkflowForge.CreateFoundry("MyWorkflow", foundryConfig);
```

### 3. Rich Structured Logging in Operations

```csharp
public class ProcessOrderOperation : IWorkflowOperation
{
    public string Name => "ProcessOrder";
    public bool SupportsRestore => true;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var order = (Order)inputData!;
        
        // Rich structured logging with context
        foundry.Logger.LogInformation("Processing order {OrderId} for customer {CustomerId} with amount {Amount:C}", 
            order.Id, order.CustomerId, order.Amount);

        // Log with custom properties for enhanced filtering
        var orderProperties = new Dictionary<string, object>
        {
            ["OrderType"] = order.Type.ToString(),
            ["PaymentMethod"] = order.PaymentMethod,
            ["Region"] = order.ShippingAddress.Region,
            ["Priority"] = order.Priority,
            ["EstimatedValue"] = order.Amount
        };

        foundry.Logger.LogInformation(orderProperties, "Order validation started for {OrderId}", order.Id);

        try
        {
            var result = await ProcessOrderInternalAsync(order, cancellationToken);
            
            foundry.Logger.LogInformation("Order {OrderId} processed successfully with transaction {TransactionId} in {Duration}ms", 
                order.Id, result.TransactionId, result.ProcessingTime.TotalMilliseconds);
            
            return result;
        }
        catch (PaymentException ex)
        {
            foundry.Logger.LogError(ex, "Payment failed for order {OrderId}: {ErrorCode} - {ErrorMessage}", 
                order.Id, ex.ErrorCode, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            foundry.Logger.LogError(ex, "Unexpected error processing order {OrderId}", order.Id);
            throw;
        }
    }

    public async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var result = (OrderResult)outputData!;
        
        foundry.Logger.LogWarning("Initiating compensation for order {OrderId} due to workflow failure", result.OrderId);
        
        try
        {
            await RevertOrderAsync(result, cancellationToken);
            foundry.Logger.LogInformation("Order {OrderId} successfully compensated", result.OrderId);
        }
        catch (Exception ex)
        {
            foundry.Logger.LogError(ex, "Failed to compensate order {OrderId} - manual intervention required", result.OrderId);
            throw;
        }
    }
}
```

## 🔧 Advanced Configuration

### Workflow-Specific Logger Configuration

```csharp
// Create specialized loggers for different workflow types
var orderProcessingLogger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("WorkflowType", "OrderProcessing")
    .Enrich.WithProperty("Component", "WorkflowForge")
    .Enrich.WithProperty("Service", "OrderService")
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{WorkflowType}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/workflows/orders/workflow-{Date}.txt", 
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{WorkflowType}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

var foundryConfig = FoundryConfiguration.ForProduction()
    .UseSerilog(orderProcessingLogger);

var foundry = WorkflowForge.CreateFoundry("OrderProcessing", foundryConfig);
```

### Environment-Specific Logging Configuration

```csharp
// Development environment - verbose logging
var devFoundryConfig = FoundryConfiguration.ForDevelopment()
    .UseSerilog(logger => logger
        .MinimumLevel.Verbose()
        .Enrich.WithProperty("Environment", "Development")
        .WriteTo.Console()
        .WriteTo.Debug());

// Production environment - structured file logging
var prodFoundryConfig = FoundryConfiguration.ForProduction()
    .UseSerilog(logger => logger
        .MinimumLevel.Information()
        .Enrich.WithProperty("Environment", "Production")
        .WriteTo.File("logs/workflows/production-.txt", 
            rollingInterval: RollingInterval.Hour,
            retainedFileCountLimit: 48)
        .WriteTo.Seq("http://seq-server:5341"));

// Enterprise environment - comprehensive logging
var enterpriseFoundryConfig = FoundryConfiguration.ForEnterprise()
    .UseSerilog(logger => logger
        .MinimumLevel.Information()
        .Enrich.WithProperty("Environment", "Enterprise")
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .WriteTo.Console()
        .WriteTo.File("logs/workflows/enterprise-.txt", rollingInterval: RollingInterval.Day)
        .WriteTo.Seq("http://seq-server:5341")
        .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://elasticsearch:9200"))));
```

## 🔗 Structured Logging with Scopes

### Workflow-Level Scopes

```csharp
public class OrderProcessingWorkflow
{
    public async Task ExecuteAsync(Order order, IWorkflowFoundry foundry)
    {
        // Create a correlation scope for the entire workflow
        using var workflowScope = foundry.Logger.BeginScope("ProcessOrder", new Dictionary<string, object>
        {
            ["WorkflowExecutionId"] = foundry.ExecutionId,
            ["OrderId"] = order.Id,
            ["CustomerId"] = order.CustomerId,
            ["CorrelationId"] = order.CorrelationId,
            ["Timestamp"] = DateTimeOffset.UtcNow
        });

        foundry.Logger.LogInformation("Starting order processing workflow for order {OrderId}", order.Id);

        try
        {
            // All operations within this scope will include the scope properties
            await ExecuteWorkflowStepsAsync(order, foundry);
            
            foundry.Logger.LogInformation("Order processing workflow completed successfully for order {OrderId}", order.Id);
        }
        catch (Exception ex)
        {
            foundry.Logger.LogError(ex, "Order processing workflow failed for order {OrderId}", order.Id);
            throw;
        }
    }
}
```

### Operation-Level Scopes

```csharp
public class AuditableOperation : IWorkflowOperation
{
    public string Name => "AuditableProcessing";
    public bool SupportsRestore => true;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        // Create audit scope for compliance and tracking
        var auditProperties = new Dictionary<string, object>
        {
            ["UserId"] = GetCurrentUserId(),
            ["SessionId"] = GetSessionId(),
            ["OperationId"] = Id.ToString(),
            ["OperationName"] = Name,
            ["StartTime"] = DateTimeOffset.UtcNow,
            ["InputType"] = inputData?.GetType().Name ?? "null"
        };

        using var auditScope = foundry.Logger.BeginScope("AuditableOperation", auditProperties);
        
        foundry.Logger.LogInformation("Starting auditable operation {OperationName}", Name);

        try
        {
            var result = await ProcessWithAuditingAsync(inputData, cancellationToken);
            
            foundry.Logger.LogInformation("Auditable operation {OperationName} completed successfully", Name);
            return result;
        }
        catch (Exception ex)
        {
            foundry.Logger.LogError(ex, "Auditable operation {OperationName} failed", Name);
            throw;
        }
    }
}
```

## 📊 Advanced Logging Patterns

### Performance Logging

```csharp
public class PerformanceAwareOperation : IWorkflowOperation
{
    public string Name => "PerformanceAware";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            foundry.Logger.LogDebug("Starting performance-critical operation {OperationName}", Name);
            
            var result = await ProcessDataAsync(inputData, cancellationToken);
            
            stopwatch.Stop();
            
            // Log performance metrics
            foundry.Logger.LogInformation("Operation {OperationName} completed in {Duration}ms with {ResultSize} bytes", 
                Name, stopwatch.ElapsedMilliseconds, GetResultSize(result));
            
            // Log detailed timing for slow operations
            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                foundry.Logger.LogWarning("Slow operation detected: {OperationName} took {Duration}ms", 
                    Name, stopwatch.ElapsedMilliseconds);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            foundry.Logger.LogError(ex, "Operation {OperationName} failed after {Duration}ms", 
                Name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

### Conditional Logging

```csharp
public class ConditionalLoggingOperation : IWorkflowOperation
{
    public string Name => "ConditionalLogging";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var data = (ProcessingData)inputData!;
        
        // Conditional verbose logging for debugging specific scenarios
        if (data.IsHighPriority || data.Amount > 10000)
        {
            foundry.Logger.LogInformation("Processing high-value/priority item: {ItemId} with amount {Amount:C}", 
                data.Id, data.Amount);
        }
        
        // Log different levels based on business rules
        if (data.RequiresApproval)
        {
            foundry.Logger.LogWarning("Item {ItemId} requires manual approval - routing to approval queue", data.Id);
        }
        
        var result = await ProcessDataAsync(data, cancellationToken);
        
        // Structured logging with dynamic properties
        var resultProperties = new Dictionary<string, object>
        {
            ["ProcessingTime"] = result.Duration.TotalMilliseconds,
            ["ApprovalRequired"] = result.RequiredApproval,
            ["RiskScore"] = result.RiskScore,
            ["ProcessingStatus"] = result.Status.ToString()
        };
        
        foundry.Logger.LogInformation(resultProperties, 
            "Processing completed for item {ItemId} with status {Status}", 
            data.Id, result.Status);
        
        return result;
    }
}
```

## 🎯 Integration with Other Extensions

### With Performance Monitoring

```csharp
var foundryConfig = FoundryConfiguration.ForProduction()
    .UseSerilog()
    .EnablePerformanceMonitoring();

var foundry = WorkflowForge.CreateFoundry("MonitoredWorkflow", foundryConfig);

// Execute workflow
await smith.ForgeAsync(workflow, inputData, foundry);

// Log performance statistics
var perfStats = foundry.GetPerformanceStatistics();
foundry.Logger.LogInformation("Workflow performance: {SuccessRate:P2} success rate, {AvgDuration}ms average duration, {TotalOps} operations", 
    perfStats.SuccessRate, perfStats.AverageDuration, perfStats.TotalOperations);
```

### With OpenTelemetry

```csharp
var foundryConfig = FoundryConfiguration.ForProduction()
    .UseSerilog()
    .EnableOpenTelemetry("OrderService", "1.0.0");

var foundry = WorkflowForge.CreateFoundry("TracedWorkflow", foundryConfig);

// Logs will be correlated with OpenTelemetry traces
using var activity = foundry.StartActivity("ProcessOrder");
foundry.Logger.LogInformation("Processing order {OrderId} with trace {TraceId}", 
    order.Id, activity.TraceId);
```

### With Health Checks

```csharp
var foundryConfig = FoundryConfiguration.ForProduction()
    .UseSerilog()
    .EnableHealthChecks();

var foundry = WorkflowForge.CreateFoundry("HealthyWorkflow", foundryConfig);

// Log health check results
var healthResults = await foundry.CheckFoundryHealthAsync();
foundry.Logger.LogInformation("System health check: {Status} - {TotalChecks} checks in {Duration}ms", 
    healthResults.Status, healthResults.Results.Count, healthResults.TotalDuration.TotalMilliseconds);
```

## 🔧 Configuration Examples

### appsettings.json Integration

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "WorkflowForge": "Debug",
        "System": "Warning",
        "Microsoft": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/workflows/workflow-.txt",
          "rollingInterval": "Day",
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"],
    "Properties": {
      "Application": "WorkflowForge",
      "Environment": "Production"
    }
  }
}
```

### Dependency Injection Configuration

```csharp
// In Startup.cs or Program.cs
services.AddSerilog((services, lc) => lc
    .ReadFrom.Configuration(configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "WorkflowForge"));

// Configure WorkflowForge with DI-provided logger
services.AddWorkflowForge(options =>
{
    options.UseSerilogFromServiceProvider();
});
```

## 🎯 Best Practices

### 1. Structured Property Design
```csharp
// ✅ Good: Consistent property naming
foundry.Logger.LogInformation("Order {OrderId} processed for customer {CustomerId}", 
    order.Id, order.CustomerId);

// ❌ Avoid: Inconsistent naming
foundry.Logger.LogInformation("Order {id} processed for customer {customer_id}", 
    order.Id, order.CustomerId);
```

### 2. Performance Considerations
```csharp
// ✅ Good: Use structured logging
foundry.Logger.LogInformation("Processing {Count} items with total value {TotalValue:C}", 
    items.Count, items.Sum(x => x.Value));

// ❌ Avoid: String interpolation in log messages
foundry.Logger.LogInformation($"Processing {items.Count} items with total value {items.Sum(x => x.Value):C}");
```

### 3. Exception Logging
```csharp
// ✅ Good: Include relevant context with exceptions
try
{
    await ProcessOrderAsync(order);
}
catch (PaymentException ex)
{
    foundry.Logger.LogError(ex, "Payment processing failed for order {OrderId} with payment method {PaymentMethod}", 
        order.Id, order.PaymentMethod);
    throw;
}

// ❌ Avoid: Generic exception logging
catch (Exception ex)
{
    foundry.Logger.LogError(ex, "Something went wrong");
    throw;
}
```

## 📚 Additional Resources

- [Core Framework Documentation](../WorkflowForge/README.md)
- [Performance Monitoring Extension](../WorkflowForge.Extensions.Observability.Performance/README.md)
- [OpenTelemetry Extension](../WorkflowForge.Extensions.Observability.OpenTelemetry/README.md)
- [Main Project Documentation](../../README.md)
- [Serilog Documentation](https://serilog.net/)

---

**WorkflowForge.Extensions.Logging.Serilog** - *Professional structured logging for workflows* 