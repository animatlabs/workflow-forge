using WorkflowForge.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Middleware;
using WorkflowForge.Operations;
using Xunit;
using Xunit.Abstractions;
using Moq;
using System.Collections.Concurrent;

namespace WorkflowForge.Tests.Middleware;

/// <summary>
/// Comprehensive tests for middleware functionality covering pipeline execution,
/// order, exception handling, and complex middleware scenarios.
/// </summary>
public class MiddlewareTests
{
    private readonly ITestOutputHelper _output;

    public MiddlewareTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region Middleware Pipeline Tests

    [Fact]
    public async Task MiddlewarePipeline_WithSingleMiddleware_ExecutesCorrectly()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var executionOrder = new List<string>();
        var middleware = new OrderTrackingMiddleware("MW1", executionOrder);

        foundry.AddMiddleware(middleware);

        var operation = new DelegateWorkflowOperation<object, string>("TestOp", async (input, f, ct) =>
        {
            await Task.Yield();
            executionOrder.Add("Operation");
            return "result";
        });

        foundry.AddOperation(operation);

        // Act
        await foundry.ForgeAsync();

        // Assert
        Assert.Contains("MW1-Before", executionOrder);
        Assert.Contains("Operation", executionOrder);
        Assert.Contains("MW1-After", executionOrder);
        
        var mw1BeforeIndex = executionOrder.IndexOf("MW1-Before");
        var operationIndex = executionOrder.IndexOf("Operation");
        var mw1AfterIndex = executionOrder.IndexOf("MW1-After");
        
        Assert.True(mw1BeforeIndex < operationIndex);
        Assert.True(operationIndex < mw1AfterIndex);
    }

    [Fact]
    public async Task MiddlewarePipeline_WithMultipleMiddlewares_ExecutesInCorrectOrder()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var executionOrder = new List<string>();

        var middleware1 = new OrderTrackingMiddleware("MW1", executionOrder);
        var middleware2 = new OrderTrackingMiddleware("MW2", executionOrder);
        var middleware3 = new OrderTrackingMiddleware("MW3", executionOrder);

        foundry.AddMiddleware(middleware1);
        foundry.AddMiddleware(middleware2);
        foundry.AddMiddleware(middleware3);

        var operation = new DelegateWorkflowOperation<object, string>("TestOp", async (input, f, ct) =>
        {
            await Task.Yield();
            executionOrder.Add("Operation");
            return "result";
        });

        foundry.AddOperation(operation);

        // Act
        await foundry.ForgeAsync();

        // Assert
        var expectedOrder = new[]
        {
            "MW1-Before", "MW2-Before", "MW3-Before",
            "Operation",
            "MW3-After", "MW2-After", "MW1-After"
        };

        Assert.Equal(expectedOrder.Length, executionOrder.Count);
        for (int i = 0; i < expectedOrder.Length; i++)
        {
            Assert.Equal(expectedOrder[i], executionOrder[i]);
        }
    }

    [Fact]
    public async Task MiddlewarePipeline_WithConditionalMiddleware_ExecutesConditionally()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var executionOrder = new List<string>();

        var conditionalMiddleware = new ConditionalMiddleware("Conditional", 
            executionOrder, shouldExecute: false);
        var regularMiddleware = new OrderTrackingMiddleware("Regular", executionOrder);

        foundry.AddMiddleware(conditionalMiddleware);
        foundry.AddMiddleware(regularMiddleware);

        var operation = new DelegateWorkflowOperation<object, string>("TestOp", async (input, f, ct) =>
        {
            await Task.Yield();
            executionOrder.Add("Operation");
            return "result";
        });

        foundry.AddOperation(operation);

        // Act
        await foundry.ForgeAsync();

        // Assert
        Assert.Contains("Regular-Before", executionOrder);
        Assert.Contains("Operation", executionOrder);
        Assert.Contains("Regular-After", executionOrder);
        Assert.DoesNotContain("Conditional-Before", executionOrder);
        Assert.DoesNotContain("Conditional-After", executionOrder);
    }

    #endregion

    #region Data Transformation Middleware Tests

    [Fact]
    public async Task DataTransformationMiddleware_TransformsInputAndOutput()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var transformationMiddleware = new DataTransformationMiddleware();

        foundry.AddMiddleware(transformationMiddleware);

        var operation = new DelegateWorkflowOperation<object, string>("TestOp", async (input, f, ct) =>
        {
            await Task.Yield();
            var inputString = input?.ToString() ?? "";
            return $"processed-{inputString}";
        });

        foundry.AddOperation(operation);

        // Act
        await foundry.ForgeAsync();

        // Assert
        // The middleware should have transformed the data
        Assert.True(foundry.Properties.ContainsKey("transformed-input"));
        Assert.True(foundry.Properties.ContainsKey("transformed-output"));
    }

    [Fact]
    public async Task ValidationMiddleware_ValidatesInput()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var validationMiddleware = new ValidationMiddleware();

        foundry.AddMiddleware(validationMiddleware);

        var operation = new DelegateWorkflowOperation<object, string>("TestOp", async (input, f, ct) =>
        {
            await Task.Yield();
            return "result";
        });

        foundry.AddOperation(operation);

        // Set invalid data
        foundry.Properties["validation-input"] = "invalid";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => foundry.ForgeAsync());
        Assert.Contains("validation failed", exception.Message.ToLower());
    }

    [Fact]
    public async Task ValidationMiddleware_WithValidInput_PassesThrough()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var validationMiddleware = new ValidationMiddleware();

        foundry.AddMiddleware(validationMiddleware);

        var operation = new DelegateWorkflowOperation<object, string>("TestOp", async (input, f, ct) =>
        {
            await Task.Yield();
            return "result";
        });

        foundry.AddOperation(operation);

        // Set valid data
        foundry.Properties["validation-input"] = "valid";

        // Act
        await foundry.ForgeAsync();

        // Assert - No exception should be thrown
        Assert.True(foundry.Properties.ContainsKey("validation-passed"));
    }

    #endregion

    #region Exception Handling Middleware Tests

    [Fact]
    public async Task ExceptionHandlingMiddleware_CatchesAndTransformsExceptions()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var exceptionMiddleware = new ExceptionHandlingMiddleware();

        foundry.AddMiddleware(exceptionMiddleware);

        var operation = new DelegateWorkflowOperation<object, string>("FailingOp", async (input, f, ct) =>
        {
            await Task.Yield();
            throw new InvalidOperationException("Test exception");
        });

        foundry.AddOperation(operation);

        // Act
        await foundry.ForgeAsync();

        // Assert
        Assert.True(foundry.Properties.ContainsKey("exception-handled"));
        Assert.Equal("InvalidOperationException", foundry.Properties["exception-type"]);
        Assert.Equal("Test exception", foundry.Properties["exception-message"]);
    }

    [Fact]
    public async Task ExceptionHandlingMiddleware_WithUnhandledException_PropagatesException()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var exceptionMiddleware = new ExceptionHandlingMiddleware(handleAll: false);

        foundry.AddMiddleware(exceptionMiddleware);

        var operation = new DelegateWorkflowOperation<object, string>("FailingOp", async (input, f, ct) =>
        {
            await Task.Yield();
            throw new ArgumentException("Unhandled exception type");
        });

        foundry.AddOperation(operation);

        // Act & Assert
        await Assert.ThrowsAsync<WorkflowOperationException>(() => foundry.ForgeAsync());
    }

    #endregion

    #region Performance and Timing Middleware Tests

    [Fact]
    public async Task TimingMiddleware_RecordsExecutionTime()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var timingMiddleware = new TimingMiddleware();

        foundry.AddMiddleware(timingMiddleware);

        var operation = new DelegateWorkflowOperation<object, string>("TimedOp", async (input, f, ct) =>
        {
            await Task.Yield();
            await Task.Delay(50, ct);
            return "result";
        });

        foundry.AddOperation(operation);

        // Act
        await foundry.ForgeAsync();

        // Assert
        Assert.True(foundry.Properties.ContainsKey("execution-time"));
        var executionTime = (TimeSpan)(foundry.Properties["execution-time"] ?? TimeSpan.Zero);
        // Use more lenient bounds to avoid flaky test failures
        Assert.True(executionTime.TotalMilliseconds >= 30, $"Expected at least 30ms, got {executionTime.TotalMilliseconds}ms");
        Assert.True(executionTime.TotalMilliseconds < 300, $"Expected less than 300ms, got {executionTime.TotalMilliseconds}ms");
    }

    [Fact]
    public async Task LoggingMiddleware_LogsOperationExecution()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var logs = new List<string>();
        var loggingMiddleware = new LoggingMiddleware(logs);

        foundry.AddMiddleware(loggingMiddleware);

        var operation = new DelegateWorkflowOperation<object, string>("LoggedOp", async (input, f, ct) =>
        {
            await Task.Yield();
            return "result";
        });

        foundry.AddOperation(operation);

        // Act
        await foundry.ForgeAsync();

        // Assert
        Assert.NotEmpty(logs);
        Assert.Contains(logs, log => log.Contains("LoggedOp") && log.Contains("starting"));
        Assert.Contains(logs, log => log.Contains("LoggedOp") && log.Contains("completed"));
    }

    #endregion

    #region Complex Middleware Scenarios

    [Fact]
    public async Task ComplexMiddlewarePipeline_WithAllMiddlewareTypes_ExecutesCorrectly()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var executionOrder = new List<string>();
        var logs = new List<string>();

        // Add multiple types of middleware
        foundry.AddMiddleware(new LoggingMiddleware(logs));
        foundry.AddMiddleware(new TimingMiddleware());
        foundry.AddMiddleware(new OrderTrackingMiddleware("Order", executionOrder));
        foundry.AddMiddleware(new DataTransformationMiddleware());

        var operation = new DelegateWorkflowOperation<object, string>("ComplexOp", async (input, f, ct) =>
        {
            await Task.Yield();
            executionOrder.Add("Operation");
            await Task.Delay(25, ct);
            return "complex-result";
        });

        foundry.AddOperation(operation);

        // Set valid validation data
        foundry.Properties["validation-input"] = "valid";

        // Act
        await foundry.ForgeAsync();

        // Assert
        Assert.NotEmpty(logs);
        Assert.NotEmpty(executionOrder);
        Assert.True(foundry.Properties.ContainsKey("execution-time"));
        Assert.True(foundry.Properties.ContainsKey("transformed-input"));
        Assert.True(foundry.Properties.ContainsKey("transformed-output"));
    }

    [Fact]
    public async Task MiddlewareWithCancellation_HandlesTokenCorrectly()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var cancellationAwareMiddleware = new CancellationAwareMiddleware();

        foundry.AddMiddleware(cancellationAwareMiddleware);

        var operation = new DelegateWorkflowOperation<object, string>("CancellableOp", async (input, f, ct) =>
        {
            await Task.Yield();
            await Task.Delay(1000, ct);
            return "result";
        });

        foundry.AddOperation(operation);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => foundry.ForgeAsync(cts.Token));
        Assert.True(foundry.Properties.ContainsKey("cancellation-requested"));
    }

    [Fact]
    public async Task OperationTimeoutMiddleware_ThrowsTimeoutException_WhenOperationExceedsTimeout()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var logger = Mock.Of<IWorkflowForgeLogger>();
        var timeoutMiddleware = new OperationTimeoutMiddleware(TimeSpan.FromMilliseconds(50), logger);
        var operation = new DelegateWorkflowOperation<object, string>("SlowOp", async (input, f, ct) =>
        {
            await Task.Delay(200);
            return "result";
        });

        // Act & Assert
        var inputData = new object();
        await Assert.ThrowsAsync<TimeoutException>(() =>
            timeoutMiddleware.ExecuteAsync(
                operation,
                foundry,
                inputData,
                async ct => (object?)await operation.ForgeAsync(inputData, foundry, ct)));

        Assert.True(foundry.Properties.TryGetValue("Operation.TimedOut", out var timedOut) && (bool)timedOut!);
    }

    [Fact]
    public async Task WorkflowTimeoutMiddleware_ThrowsTimeoutException_WhenWorkflowExceedsTimeout()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var logger = Mock.Of<IWorkflowForgeLogger>();
        var timeoutMiddleware = new WorkflowTimeoutMiddleware(TimeSpan.FromMilliseconds(50), logger);
        var workflow = Mock.Of<IWorkflow>(w => w.Name == "SlowWorkflow");

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(() =>
            timeoutMiddleware.ExecuteAsync(workflow, foundry, async () => await Task.Delay(200)));

        Assert.True(foundry.Properties.TryGetValue("Workflow.TimedOut", out var timedOut) && (bool)timedOut!);
    }

    #endregion

    #region Helper Methods and Test Middleware Classes

    private static WorkflowFoundry CreateTestFoundry()
    {
        var executionId = Guid.NewGuid();
        var properties = new ConcurrentDictionary<string, object?>();
        return new WorkflowFoundry(executionId, properties);
    }

    #endregion
}

#region Test Middleware Implementations

/// <summary>
/// Middleware that tracks execution order for testing purposes.
/// </summary>
public class OrderTrackingMiddleware : IWorkflowOperationMiddleware
{
    private readonly string _name;
    private readonly List<string> _executionOrder;

    public OrderTrackingMiddleware(string name, List<string> executionOrder)
    {
        _name = name;
        _executionOrder = executionOrder;
    }

    public async Task<object?> ExecuteAsync(IWorkflowOperation operation, IWorkflowFoundry foundry, 
        object? inputData, Func<CancellationToken, Task<object?>> next, CancellationToken cancellationToken = default)
    {
        _executionOrder.Add($"{_name}-Before");
        
        try
        {
            var result = await next(cancellationToken);
            return result;
        }
        finally
        {
            _executionOrder.Add($"{_name}-After");
        }
    }
}

/// <summary>
/// Middleware that conditionally executes based on a flag.
/// </summary>
public class ConditionalMiddleware : IWorkflowOperationMiddleware
{
    private readonly string _name;
    private readonly List<string> _executionOrder;
    private readonly bool _shouldExecute;

    public ConditionalMiddleware(string name, List<string> executionOrder, bool shouldExecute)
    {
        _name = name;
        _executionOrder = executionOrder;
        _shouldExecute = shouldExecute;
    }

    public async Task<object?> ExecuteAsync(IWorkflowOperation operation, IWorkflowFoundry foundry, 
        object? inputData, Func<CancellationToken, Task<object?>> next, CancellationToken cancellationToken = default)
    {
        if (_shouldExecute)
        {
            _executionOrder.Add($"{_name}-Before");
            var result = await next(cancellationToken);
            _executionOrder.Add($"{_name}-After");
            return result;
        }

        return await next(cancellationToken);
    }
}

/// <summary>
/// Middleware that transforms input and output data.
/// </summary>
public class DataTransformationMiddleware : IWorkflowOperationMiddleware
{
    public async Task<object?> ExecuteAsync(IWorkflowOperation operation, IWorkflowFoundry foundry, 
        object? inputData, Func<CancellationToken, Task<object?>> next, CancellationToken cancellationToken = default)
    {
        // Transform input data
        foundry.Properties["transformed-input"] = inputData?.ToString()?.ToUpper() ?? "EMPTY";
        
        // Execute the next middleware/operation
        var result = await next(cancellationToken);
        
        // Transform output data
        foundry.Properties["transformed-output"] = result?.ToString()?.ToLower() ?? "empty";
        
        return result;
    }
}

/// <summary>
/// Middleware that validates input data.
/// </summary>
public class ValidationMiddleware : IWorkflowOperationMiddleware
{
    public async Task<object?> ExecuteAsync(IWorkflowOperation operation, IWorkflowFoundry foundry, 
        object? inputData, Func<CancellationToken, Task<object?>> next, CancellationToken cancellationToken = default)
    {
        // Check for validation input
        if (foundry.Properties.TryGetValue("validation-input", out var validationData) && validationData?.ToString() == "invalid")
        {
            throw new InvalidOperationException("Validation failed");
        }
        
        // Execute the next middleware/operation
        var result = await next(cancellationToken);
        
        // Mark validation as passed
        foundry.Properties["validation-passed"] = true;
        
        return result;
    }
}

/// <summary>
/// Middleware that handles exceptions and transforms them.
/// </summary>
public class ExceptionHandlingMiddleware : IWorkflowOperationMiddleware
{
    private readonly bool _handleAll;

    public ExceptionHandlingMiddleware(bool handleAll = true)
    {
        _handleAll = handleAll;
    }

    public async Task<object?> ExecuteAsync(IWorkflowOperation operation, IWorkflowFoundry foundry, 
        object? inputData, Func<CancellationToken, Task<object?>> next, CancellationToken cancellationToken = default)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (WorkflowOperationException wex) when (_handleAll || wex.InnerException is InvalidOperationException)
        {
            // Extract the original exception from the WorkflowOperationException
            var originalException = wex.InnerException ?? wex;
            
            // Handle the exception
            foundry.Properties["exception-handled"] = true;
            foundry.Properties["exception-type"] = originalException.GetType().Name;
            foundry.Properties["exception-message"] = originalException.Message;
            
            return null; // Return null to indicate handled exception
        }
        catch (Exception ex) when (_handleAll || ex is InvalidOperationException)
        {
            // Handle direct exceptions (in case they're not wrapped)
            foundry.Properties["exception-handled"] = true;
            foundry.Properties["exception-type"] = ex.GetType().Name;
            foundry.Properties["exception-message"] = ex.Message;
            
            return null; // Return null to indicate handled exception
        }
    }
}

/// <summary>
/// Middleware that records execution timing.
/// </summary>
public class TimingMiddleware : IWorkflowOperationMiddleware
{
    public async Task<object?> ExecuteAsync(IWorkflowOperation operation, IWorkflowFoundry foundry, 
        object? inputData, Func<CancellationToken, Task<object?>> next, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            return await next(cancellationToken);
        }
        finally
        {
            stopwatch.Stop();
            foundry.Properties["execution-time"] = stopwatch.Elapsed;
        }
    }
}

/// <summary>
/// Middleware that logs operation execution.
/// </summary>
public class LoggingMiddleware : IWorkflowOperationMiddleware
{
    private readonly List<string> _logs;

    public LoggingMiddleware(List<string> logs)
    {
        _logs = logs;
    }

    public async Task<object?> ExecuteAsync(IWorkflowOperation operation, IWorkflowFoundry foundry, 
        object? inputData, Func<CancellationToken, Task<object?>> next, CancellationToken cancellationToken = default)
    {
        _logs.Add($"Operation {operation.Name} starting");
        
        try
        {
            var result = await next(cancellationToken);
            _logs.Add($"Operation {operation.Name} completed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logs.Add($"Operation {operation.Name} failed: {ex.Message}");
            throw;
        }
    }
}

/// <summary>
/// Middleware that is aware of cancellation tokens.
/// </summary>
public class CancellationAwareMiddleware : IWorkflowOperationMiddleware
{
    public async Task<object?> ExecuteAsync(IWorkflowOperation operation, IWorkflowFoundry foundry, 
        object? inputData, Func<CancellationToken, Task<object?>> next, CancellationToken cancellationToken = default)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
            {
                foundry.Properties["cancellation-requested"] = true;
                cancellationToken.ThrowIfCancellationRequested();
            }
            
            return await next(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            foundry.Properties["cancellation-requested"] = true;
            throw;
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            foundry.Properties["cancellation-requested"] = true;
            throw;
        }
    }
}

#endregion 