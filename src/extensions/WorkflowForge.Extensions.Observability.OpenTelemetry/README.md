# WorkflowForge.Extensions.Observability.OpenTelemetry

Advanced distributed tracing and observability extension for WorkflowForge using OpenTelemetry. This extension provides industry-standard distributed tracing, metrics, and telemetry using OpenTelemetry protocols and standards.

## 🎯 Extension Overview

The OpenTelemetry extension brings professional observability to WorkflowForge applications, including:

- **🔍 Distributed Tracing**: Full distributed tracing using OpenTelemetry Activity API
- **📊 Metrics Collection**: Comprehensive metrics using System.Diagnostics.Metrics
- **🏭 Foundry Integration**: Deep integration with WorkflowForge foundries and operations
- **🚀 Standard Protocols**: OTLP, Jaeger, Zipkin, Prometheus compatibility
- **⚡ High Performance**: Built on .NET's native observability APIs
- **🔧 Easy Integration**: Simple foundry extension methods with minimal configuration
- **📈 System Metrics**: Automatic process, memory, and GC metrics collection
- **🎯 Operation Tracking**: Built-in operation lifecycle monitoring and correlation
- **🌍 Multi-Exporter Support**: Export to multiple observability backends simultaneously

## 📦 Installation

```bash
dotnet add package WorkflowForge.Extensions.Observability.OpenTelemetry
```

## 🚀 Quick Start

### 1. Enable OpenTelemetry in Your Foundry

```csharp
using WorkflowForge;
using WorkflowForge.Extensions.Observability.OpenTelemetry;

// Option 1: Enable via foundry configuration
var foundryConfig = FoundryConfiguration.ForProduction()
    .EnableOpenTelemetry("MyWorkflowService", "1.0.0");

var foundry = WorkflowForge.CreateFoundry("OrderProcessing", foundryConfig);

// Option 2: Enable on existing foundry with custom options
var foundry = WorkflowForge.CreateFoundry("OrderProcessing");
foundry.EnableOpenTelemetry(new WorkflowForgeOpenTelemetryOptions
{
    ServiceName = "MyWorkflowService",
    ServiceVersion = "2.1.0",
    EnableTracing = true,
    EnableMetrics = true,
    EnableSystemMetrics = true,
    EnableDetailedLogging = true
});
```

### 2. Configure OpenTelemetry SDK

Configure the OpenTelemetry SDK in your application startup to export telemetry data:

```csharp
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Configure OpenTelemetry with comprehensive setup
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("MyWorkflowService", "1.0.0")
        .AddAttributes(new Dictionary<string, object>
        {
            ["environment"] = "production",
            ["team"] = "workflow-team",
            ["deployment.environment"] = "prod",
            ["service.namespace"] = "ecommerce"
        }))
    .WithTracing(tracing => tracing
        .AddSource("MyWorkflowService") // Match your service name
        .SetSampler(new TraceIdRatioBasedSampler(1.0)) // Sample all traces in dev
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://jaeger:14268/api/traces");
        })
        .AddJaegerExporter()
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddMeter("MyWorkflowService") // Match your service name
        .AddRuntimeInstrumentation()
        .AddProcessInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://prometheus:9090/api/v1/otlp/v1/metrics");
        })
        .AddPrometheusExporter());

var app = builder.Build();
```

### 3. Distributed Tracing in Operations

```csharp
public class OrderProcessingOperation : IWorkflowOperation
{
    public string Name => "ProcessOrder";
    public bool SupportsRestore => true;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        // Start distributed tracing with comprehensive context
        using var activity = foundry.StartActivity("ProcessOrder", ActivityKind.Server);
        
        var order = (Order)inputData!;
        
        // Add rich trace context
        activity?.SetTag("order.id", order.Id);
        activity?.SetTag("order.amount", order.Amount);
        activity?.SetTag("customer.id", order.CustomerId);
        activity?.SetTag("order.type", order.Type);
        activity?.SetTag("payment.method", order.PaymentMethod);
        
        // Track active operations for real-time monitoring
        foundry.IncrementActiveOperations("ProcessOrder", 
            ("order.type", order.Type),
            ("customer.tier", order.CustomerTier));

        var stopwatch = Stopwatch.StartNew();
        var success = false;
        
        try
        {
            // Add operation events for detailed tracing
            activity?.AddEvent(new ActivityEvent("Order validation started"));
            
            var result = await ProcessOrderInternalAsync(order, cancellationToken);
            
            activity?.AddEvent(new ActivityEvent("Order processing completed", 
                DateTimeOffset.UtcNow,
                new ActivityTagsCollection(new[]
                {
                    new KeyValuePair<string, object?>("result.transaction_id", result.TransactionId),
                    new KeyValuePair<string, object?>("result.status", result.Status)
                })));
            
            success = true;
            activity?.SetStatus(ActivityStatusCode.Ok);
            
            return result;
        }
        catch (Exception ex)
        {
            // Record exception in trace
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddEvent(new ActivityEvent("Operation failed", 
                DateTimeOffset.UtcNow,
                new ActivityTagsCollection(new[]
                {
                    new KeyValuePair<string, object?>("exception.type", ex.GetType().Name),
                    new KeyValuePair<string, object?>("exception.message", ex.Message)
                })));
            
            throw;
        }
        finally
        {
            stopwatch.Stop();
            
            // Record comprehensive operation metrics
            foundry.RecordOperationMetrics(
                operationName: "ProcessOrder",
                duration: stopwatch.Elapsed,
                success: success,
                memoryAllocated: GC.GetAllocatedBytesForCurrentThread(),
                ("order.type", order.Type),
                ("customer.tier", order.CustomerTier),
                ("payment.method", order.PaymentMethod)
            );
            
            foundry.DecrementActiveOperations("ProcessOrder", 
                ("order.type", order.Type),
                ("customer.tier", order.CustomerTier));
        }
    }
}
```

### 4. Custom Metrics and Instrumentation

```csharp
public class OrderMetricsCollector
{
    private readonly IWorkflowFoundry _foundry;
    private readonly Counter<long> _orderCounter;
    private readonly Histogram<double> _responseTimeHistogram;
    private readonly UpDownCounter<int> _queueSizeCounter;

    public OrderMetricsCollector(IWorkflowFoundry foundry)
    {
        _foundry = foundry;
        
        // Create custom business metrics
        _orderCounter = foundry.CreateCounter<long>("orders.processed", "order", 
            "Total orders processed by the workflow");
        _responseTimeHistogram = foundry.CreateHistogram<double>("order.processing.duration", "s", 
            "Order processing time distribution");
        _queueSizeCounter = foundry.CreateUpDownCounter<int>("order.queue.size", "order", 
            "Current number of orders in processing queue");
    }

    public void RecordOrderProcessed(Order order, TimeSpan processingTime)
    {
        // Record with detailed dimensions
        var tags = new TagList
        {
            { "order.type", order.Type },
            { "customer.tier", order.CustomerTier },
            { "payment.method", order.PaymentMethod },
            { "region", order.ShippingAddress.Region }
        };

        _orderCounter?.Add(1, tags);
        _responseTimeHistogram?.Record(processingTime.TotalSeconds, tags);
    }

    public void IncrementQueueSize(string orderType) => 
        _queueSizeCounter?.Add(1, new TagList { { "order.type", orderType } });

    public void DecrementQueueSize(string orderType) => 
        _queueSizeCounter?.Add(-1, new TagList { { "order.type", orderType } });
}
```

## 📊 Built-in Metrics

The extension automatically provides comprehensive standard metrics:

### Operation Metrics

```csharp
// Built-in operation metrics automatically collected:

// Counter: Total operations executed
"workflowforge.operations.total" 
// Tags: operation.name, service.name, operation.success

// Counter: Total operation errors
"workflowforge.operations.errors.total"
// Tags: operation.name, service.name, error.type

// Histogram: Operation execution time distribution
"workflowforge.operations.duration"
// Tags: operation.name, service.name, operation.success
// Unit: seconds

// Histogram: Memory allocated per operation
"workflowforge.operations.memory.allocations"
// Tags: operation.name, service.name
// Unit: bytes

// UpDownCounter: Currently active operations
"workflowforge.operations.active"
// Tags: operation.name, service.name
```

### System Metrics

```csharp
// Built-in system metrics for foundry health:

// Gauge: Process memory usage
"workflowforge.process.memory.usage"
// Tags: service.name, memory.type (working_set, private)

// Counter: Total garbage collections
"workflowforge.process.gc.collections.total"
// Tags: service.name, gc.generation

// Gauge: Available thread pool threads
"workflowforge.process.threadpool.threads.available"
// Tags: service.name, thread.type (worker, completion)

// Gauge: Foundry count
"workflowforge.foundries.active"
// Tags: service.name
```

## 🔧 Advanced Configuration

### Environment-Specific Configuration

```csharp
// Development environment - verbose tracing
var devFoundryConfig = FoundryConfiguration.ForDevelopment()
    .EnableOpenTelemetry("MyService", "1.0.0", options =>
    {
        options.EnableTracing = true;
        options.EnableMetrics = true;
        options.EnableSystemMetrics = true;
        options.EnableDetailedLogging = true;
        options.SampleRate = 1.0; // Trace everything in dev
        options.AddCustomTags = new Dictionary<string, object>
        {
            ["environment"] = "development",
            ["developer"] = Environment.UserName
        };
    });

// Production environment - optimized performance
var prodFoundryConfig = FoundryConfiguration.ForProduction()
    .EnableOpenTelemetry("MyService", "1.0.0", options =>
    {
        options.EnableTracing = true;
        options.EnableMetrics = true;
        options.EnableSystemMetrics = false; // Reduce overhead
        options.EnableDetailedLogging = false;
        options.SampleRate = 0.1; // Sample 10% of traces
        options.AddCustomTags = new Dictionary<string, object>
        {
            ["environment"] = "production",
            ["datacenter"] = "us-east-1"
        };
    });

// Enterprise environment - comprehensive observability
var enterpriseFoundryConfig = FoundryConfiguration.ForEnterprise()
    .EnableOpenTelemetry("MyService", "1.0.0", options =>
    {
        options.EnableTracing = true;
        options.EnableMetrics = true;
        options.EnableSystemMetrics = true;
        options.EnableDetailedLogging = true;
        options.SampleRate = 1.0;
        options.EnableCorrelationIds = true;
        options.AddCustomTags = new Dictionary<string, object>
        {
            ["environment"] = "enterprise",
            ["compliance.level"] = "high",
            ["audit.enabled"] = true
        };
    });
```

### Multi-Backend Export Configuration

```csharp
// Configure multiple exporters for different purposes
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("MyWorkflowService")
        // Export to Jaeger for distributed tracing
        .AddJaegerExporter(options =>
        {
            options.AgentHost = "jaeger";
            options.AgentPort = 6831;
        })
        // Export to OTLP for vendor-neutral collection
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://otel-collector:4317");
            options.Headers = "x-api-key=your-api-key";
        })
        // Export to console for debugging
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddMeter("MyWorkflowService")
        // Export to Prometheus for metrics
        .AddPrometheusExporter(options =>
        {
            options.HttpListenerPrefixes = new[] { "http://localhost:9090/" };
        })
        // Export to OTLP for centralized collection
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://metrics-collector:4318/v1/metrics");
        }));
```

## 🔗 Integration with Other Extensions

### With Performance Monitoring

```csharp
var foundryConfig = FoundryConfiguration.ForProduction()
    .EnableOpenTelemetry("OrderService", "1.0.0")
    .EnablePerformanceMonitoring();

var foundry = WorkflowForge.CreateFoundry("OrderWorkflow", foundryConfig);

// Performance metrics will be automatically exported via OpenTelemetry
var stats = foundry.GetPerformanceStatistics();
using var activity = foundry.StartActivity("PerformanceAnalysis");
activity?.SetTag("workflow.success_rate", stats.SuccessRate.ToString("P2"));
activity?.SetTag("workflow.avg_duration_ms", stats.AverageDuration.ToString());
activity?.SetTag("workflow.operations_per_sec", stats.OperationsPerSecond.ToString("F2"));
```

### With Health Checks

```csharp
var foundryConfig = FoundryConfiguration.ForProduction()
    .EnableOpenTelemetry("OrderService", "1.0.0")
    .EnableHealthChecks();

var foundry = WorkflowForge.CreateFoundry("OrderWorkflow", foundryConfig);

// Health check results will be traced and metricsized
public class TracedHealthCheck : IHealthCheck
{
    private readonly IWorkflowFoundry _foundry;

    public string Name => "WorkflowHealth";
    public string Description => "Monitors workflow system health with tracing";

    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        using var activity = _foundry.StartActivity("HealthCheck");
        activity?.SetTag("health_check.name", Name);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Perform health checks
            var healthResults = await _foundry.CheckFoundryHealthAsync(cancellationToken);
            
            stopwatch.Stop();
            
            // Record health check metrics
            var healthMetric = _foundry.CreateCounter<long>("health_checks.total", "check", "Health check executions");
            healthMetric?.Add(1, new TagList
            {
                { "check.name", Name },
                { "check.status", healthResults.Status.ToString() }
            });

            var durationMetric = _foundry.CreateHistogram<double>("health_checks.duration", "s", "Health check duration");
            durationMetric?.Record(stopwatch.Elapsed.TotalSeconds, new TagList
            {
                { "check.name", Name },
                { "check.status", healthResults.Status.ToString() }
            });

            activity?.SetTag("health_check.status", healthResults.Status.ToString());
            activity?.SetTag("health_check.duration_ms", stopwatch.ElapsedMilliseconds);

            return HealthCheckResult.Healthy($"All systems operational in {stopwatch.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
```

### With Serilog Logging

```csharp
var foundryConfig = FoundryConfiguration.ForProduction()
    .EnableOpenTelemetry("OrderService", "1.0.0")
    .UseSerilog();

var foundry = WorkflowForge.CreateFoundry("OrderWorkflow", foundryConfig);

// Logs will be correlated with traces automatically
public class TracedOperation : IWorkflowOperation
{
    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        using var activity = foundry.StartActivity("TracedOperation");
        
        // Logs will include trace and span IDs for correlation
        foundry.Logger.LogInformation("Starting operation with trace {TraceId} span {SpanId}", 
            activity?.TraceId, activity?.SpanId);

        // OpenTelemetry context automatically flows through async operations
        var result = await ProcessWithLogging(inputData, foundry, cancellationToken);
        
        foundry.Logger.LogInformation("Operation completed successfully with trace {TraceId}", 
            activity?.TraceId);
        
        return result;
    }
}
```

## 🎯 Advanced Scenarios

### Workflow Orchestration with Distributed Tracing

```csharp
public class DistributedWorkflowOrchestrator
{
    public async Task<WorkflowResult> ExecuteDistributedWorkflowAsync(
        WorkflowRequest request, 
        IWorkflowFoundry foundry)
    {
        // Create parent span for the entire workflow
        using var workflowActivity = foundry.StartActivity("DistributedWorkflow", ActivityKind.Server);
        workflowActivity?.SetTag("workflow.id", request.WorkflowId);
        workflowActivity?.SetTag("workflow.type", request.Type);
        workflowActivity?.SetTag("request.correlation_id", request.CorrelationId);

        // Set baggage for cross-service correlation
        workflowActivity?.SetBaggage("workflow.correlation_id", request.CorrelationId);
        workflowActivity?.SetBaggage("customer.id", request.CustomerId.ToString());

        var result = new WorkflowResult();

        try
        {
            // Step 1: Validate request
            using (var validateActivity = foundry.StartActivity("ValidateRequest", ActivityKind.Internal))
            {
                validateActivity?.SetTag("validation.type", "business_rules");
                await ValidateRequestAsync(request);
                validateActivity?.SetStatus(ActivityStatusCode.Ok);
            }

            // Step 2: Call external service with trace context propagation
            using (var externalActivity = foundry.StartActivity("CallExternalService", ActivityKind.Client))
            {
                externalActivity?.SetTag("service.name", "payment-service");
                externalActivity?.SetTag("service.endpoint", "https://payment-api/process");
                
                var paymentResult = await CallExternalPaymentServiceAsync(request, foundry);
                result.PaymentId = paymentResult.PaymentId;
                
                externalActivity?.SetTag("payment.transaction_id", paymentResult.PaymentId);
                externalActivity?.SetStatus(ActivityStatusCode.Ok);
            }

            // Step 3: Update local state
            using (var updateActivity = foundry.StartActivity("UpdateLocalState", ActivityKind.Internal))
            {
                await UpdateWorkflowStateAsync(request.WorkflowId, result);
                updateActivity?.SetStatus(ActivityStatusCode.Ok);
            }

            workflowActivity?.SetStatus(ActivityStatusCode.Ok);
            workflowActivity?.SetTag("workflow.result", "success");
            
            return result;
        }
        catch (Exception ex)
        {
            workflowActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            workflowActivity?.SetTag("workflow.result", "error");
            workflowActivity?.SetTag("error.type", ex.GetType().Name);
            throw;
        }
    }
}
```

### Custom Span Processors and Exporters

```csharp
// Custom span processor for business logic
public class BusinessLogicSpanProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity span)
    {
        // Extract business metrics from spans
        if (span.GetTagItem("operation.name")?.ToString() == "ProcessOrder")
        {
            var orderAmount = span.GetTagItem("order.amount");
            var customerTier = span.GetTagItem("customer.tier");
            
            // Send to business intelligence system
            RecordBusinessMetrics(orderAmount, customerTier, span.Duration);
        }
    }
}

// Register custom processor
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddProcessor(new BusinessLogicSpanProcessor())
        .AddSource("MyWorkflowService"));
```

## 🎯 Best Practices

### 1. Trace Sampling Strategy
```csharp
// ✅ Good: Environment-appropriate sampling
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetSampler(new TraceIdRatioBasedSampler(0.1)) // 10% in production
        .AddSource("MyService"));

// ❌ Avoid: Always sampling everything in high-volume production
.SetSampler(new AlwaysOnSampler()) // Can overwhelm systems
```

### 2. Tag and Attribute Management
```csharp
// ✅ Good: Meaningful, consistent tags
activity?.SetTag("order.id", order.Id);
activity?.SetTag("customer.tier", customer.Tier);
activity?.SetTag("operation.type", "business_critical");

// ❌ Avoid: High cardinality tags
activity?.SetTag("timestamp", DateTime.Now.ToString()); // Creates too many unique spans
activity?.SetTag("customer.email", customer.Email); // PII concerns
```

### 3. Error Handling and Status
```csharp
// ✅ Good: Proper error handling with context
try
{
    await ProcessOrderAsync(order);
    activity?.SetStatus(ActivityStatusCode.Ok);
}
catch (BusinessException ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    activity?.SetTag("error.category", "business_rule_violation");
    throw;
}
catch (Exception ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, "Unexpected error");
    activity?.SetTag("error.category", "system_error");
    throw;
}
```

## 📚 Additional Resources

- [Core Framework Documentation](../WorkflowForge/README.md)
- [Performance Monitoring Extension](../WorkflowForge.Extensions.Observability.Performance/README.md)
- [Health Checks Extension](../WorkflowForge.Extensions.Observability.HealthChecks/README.md)
- [Serilog Logging Extension](../WorkflowForge.Extensions.Logging.Serilog/README.md)
- [Main Project Documentation](../../README.md)
- [OpenTelemetry Documentation](https://opentelemetry.io/docs/)
- [.NET OpenTelemetry Documentation](https://github.com/open-telemetry/opentelemetry-dotnet)

---

**WorkflowForge.Extensions.Observability.OpenTelemetry** - *Professional distributed observability for workflows* 