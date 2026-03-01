using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Extensions.Observability.HealthChecks;
using WorkflowForge.Operations;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates health check capabilities for monitoring workflow components.
/// Shows how to create health check services and monitor foundry health status.
/// </summary>
public class HealthChecksSample : ISample
{
    public string Name => "Health Checks";
    public string Description => "Health monitoring and status checking for workflow components";

    public async Task RunAsync()
    {
        Console.WriteLine("Demonstrating WorkflowForge health check capabilities...");

        // Scenario 1: Basic health check service
        await RunBasicHealthCheckDemo();

        // Scenario 2: Periodic health monitoring
        await RunPeriodicHealthCheckDemo();

        // Scenario 3: Health checks during workflow execution
        await RunWorkflowHealthCheckDemo();
    }

    private static async Task RunBasicHealthCheckDemo()
    {
        Console.WriteLine("\n--- Basic Health Check Demo ---");

        using var foundry = WorkflowForge.CreateFoundry("HealthCheckDemo");

        // Create health check service
        var healthCheckService = foundry.CreateHealthCheckService();

        foundry
            .WithOperation(LoggingOperation.Info("Starting basic health check demonstration"))
            .WithOperation(new HealthyOperation("DatabaseConnection"))
            .WithOperation(new HealthyOperation("ExternalService"))
            .WithOperation(LoggingOperation.Info("Health check demonstration completed"));

        // Check health before execution
        var healthStatus = await foundry.CheckFoundryHealthAsync(healthCheckService);
        Console.WriteLine($"   Pre-execution health status: {healthStatus}");

        await foundry.ForgeAsync();

        // Check health after execution
        healthStatus = await foundry.CheckFoundryHealthAsync(healthCheckService);
        Console.WriteLine($"   Post-execution health status: {healthStatus}");
    }

    private static async Task RunPeriodicHealthCheckDemo()
    {
        Console.WriteLine("\n--- Periodic Health Check Demo ---");

        using var foundry = WorkflowForge.CreateFoundry("PeriodicHealthCheckDemo");

        // Create health check service with periodic monitoring (every 500ms)
        var healthCheckService = foundry.CreateHealthCheckService(TimeSpan.FromMilliseconds(500));

        foundry
            .WithOperation(LoggingOperation.Info("Starting periodic health monitoring demonstration"))
            .WithOperation(new HealthyOperation("SystemInitialization"))
            .WithOperation(DelayOperation.FromSeconds(1)) // Allow time for periodic checks
            .WithOperation(new HealthyOperation("DataProcessing"))
            .WithOperation(DelayOperation.FromMilliseconds(750)) // Allow time for more periodic checks
            .WithOperation(LoggingOperation.Info("Periodic health monitoring demonstration completed"));

        // Monitor health status during execution
        _ = MonitorHealthStatusAsync(healthCheckService, foundry);

        await foundry.ForgeAsync();

        // Stop health monitoring
        await Task.Delay(100); // Allow final health check
        Console.WriteLine("   Periodic health monitoring completed");
    }

    private static async Task RunWorkflowHealthCheckDemo()
    {
        Console.WriteLine("\n--- Workflow Health Check Demo ---");

        using var foundry = WorkflowForge.CreateFoundry("WorkflowHealthCheckDemo");

        var healthCheckService = foundry.CreateHealthCheckService();

        foundry.SetProperty("health_check_service", healthCheckService);

        foundry
            .WithOperation(LoggingOperation.Info("Starting workflow health check demonstration"))
            .WithOperation(new HealthAwareOperation("CriticalSystemCheck"))
            .WithOperation(new HealthAwareOperation("BusinessLogicProcessor"))
            .WithOperation(new HealthAwareOperation("DataValidationService"))
            .WithOperation(LoggingOperation.Info("Workflow health check demonstration completed"));

        await foundry.ForgeAsync();

        // Final health summary
        var finalHealth = await foundry.CheckFoundryHealthAsync(healthCheckService);
        Console.WriteLine($"   Final workflow health status: {finalHealth}");
        Console.WriteLine($"   Health checks performed: {(foundry.Properties.TryGetValue("health_checks_performed", out var hcp) ? hcp : 0)}");
    }

    private static async Task MonitorHealthStatusAsync(HealthCheckService healthCheckService, IWorkflowFoundry foundry)
    {
        var monitoringStart = DateTime.UtcNow;
        var checkCount = 0;

        while (DateTime.UtcNow - monitoringStart < TimeSpan.FromSeconds(2))
        {
            await Task.Delay(400); // Check every 400ms

            try
            {
                await healthCheckService.CheckHealthAsync();
                checkCount++;

                Console.WriteLine($"   Periodic health check #{checkCount}: Status = {healthCheckService.OverallStatus}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Health check failed: {ex.Message}");
            }
        }
    }
}

/// <summary>
/// Operation that represents a healthy system component
/// </summary>
public class HealthyOperation : WorkflowOperationBase
{
    private readonly string _componentName;

    public HealthyOperation(string componentName)
    {
        _componentName = componentName;
    }

    public override string Name => $"Healthy_{_componentName}";

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        foundry.Logger.LogInformation("Health check for component: {ComponentName}", _componentName);

        // Simulate component operation
        await Task.Delay(100, cancellationToken);

        var healthInfo = new
        {
            ComponentName = _componentName,
            Status = "Healthy",
            LastChecked = DateTime.UtcNow,
            ResponseTime = "100ms",
            UpTime = TimeSpan.FromHours(24.5) // Simulate uptime
        };

        foundry.Logger.LogInformation("Component {ComponentName} is healthy - Response time: {ResponseTime}",
            _componentName, healthInfo.ResponseTime);

        return healthInfo;
    }
}

/// <summary>
/// Operation that performs health checks during execution
/// </summary>
public class HealthAwareOperation : WorkflowOperationBase
{
    private readonly string _operationName;

    public HealthAwareOperation(string operationName)
    {
        _operationName = operationName;
    }

    public override string Name => _operationName;

    protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        foundry.Logger.LogInformation("Starting health-aware operation: {OperationName}", _operationName);

        // Get health check service from foundry properties
        var healthCheckService = foundry.GetPropertyOrDefault<HealthCheckService>("health_check_service");

        if (healthCheckService != null)
        {
            // Perform health check before operation
            await healthCheckService.CheckHealthAsync(cancellationToken);
            var healthStatus = healthCheckService.OverallStatus;

            foundry.Logger.LogInformation("Pre-operation health status: {HealthStatus}", healthStatus);

            if (healthStatus != HealthStatus.Healthy)
            {
                foundry.Logger.LogWarning("Health check indicates issues, but continuing operation");
            }

            // Update health check counter
            var healthChecks = foundry.GetPropertyOrDefault<int>("health_checks_performed", 0);
            foundry.SetProperty("health_checks_performed", healthChecks + 1);
        }

        // Simulate operation work
        await Task.Delay(200, cancellationToken);

        var result = new
        {
            OperationName = _operationName,
            CompletedAt = DateTime.UtcNow,
            HealthCheckPerformed = healthCheckService != null,
            Status = "Completed"
        };

        foundry.Logger.LogInformation("Health-aware operation completed: {OperationName}", _operationName);

        return result;
    }
}