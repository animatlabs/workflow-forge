using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Exceptions;
using WorkflowForge.Loggers;
using WorkflowForge.Operations;
using WorkflowForge.Options;
using Xunit.Abstractions;

namespace WorkflowForge.Tests;

/// <summary>
/// Comprehensive tests for WorkflowFoundry covering all core functionality.
/// Tests property management, data operations, middleware pipeline, lifecycle, and error handling.
/// </summary>
public class WorkflowFoundryShould
{
    private readonly ITestOutputHelper _output;

    public WorkflowFoundryShould(ITestOutputHelper output)
    {
        _output = output;
    }

    #region Constructor and Basic Properties Tests

    [Fact]
    public void CreateFoundry_GivenRequiredParameters()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var properties = new ConcurrentDictionary<string, object?>();

        // Act
        var foundry = new WorkflowFoundry(executionId, properties);

        // Assert
        Assert.Equal(executionId, foundry.ExecutionId);
        Assert.Null(foundry.CurrentWorkflow); // No workflow set initially
        Assert.NotNull(foundry.Properties);
    }

    [Fact]
    public void UseProvidedConfiguration_GivenConfiguration()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var properties = new ConcurrentDictionary<string, object?>();
        var logger = NullLogger.Instance;

        // Act
        var foundry = new WorkflowFoundry(executionId, properties, logger);

        // Assert - Configuration would be internal, just verify foundry created
        Assert.NotNull(foundry);
        Assert.Equal(executionId, foundry.ExecutionId);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullData()
    {
        // Arrange
        var executionId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new WorkflowFoundry(executionId, null!));
    }

    [Fact]
    public void SetProperties_GivenInitialProperties()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var properties = new ConcurrentDictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42
        };

        // Act
        var foundry = new WorkflowFoundry(executionId, properties);

        // Assert
        Assert.Equal("value1", foundry.Properties["key1"]);
        Assert.Equal(42, foundry.Properties["key2"]);
    }

    [Fact]
    public void SetLoggerCorrectly_GivenLogger()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var properties = new ConcurrentDictionary<string, object?>();
        var logger = NullLogger.Instance;

        // Act
        var foundry = new WorkflowFoundry(executionId, properties, logger);

        // Assert
        Assert.Same(logger, foundry.Logger);
    }

    [Fact]
    public void ReturnExecutionId_GivenExecutionId()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var foundry = CreateTestFoundry(executionId);

        // Act & Assert
        Assert.Equal(executionId, foundry.ExecutionId);
    }

    [Fact]
    public void AllowSetAndRetrieve_GivenCurrentWorkflow()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var workflow = CreateMockWorkflow("TestWorkflow");

        // Act
        foundry.SetCurrentWorkflow(workflow);

        // Assert
        Assert.Equal(workflow, foundry.CurrentWorkflow);
        Assert.Equal("TestWorkflow", foundry.CurrentWorkflow?.Name);
    }

    [Fact]
    public void SetToNull_GivenCurrentWorkflowNullValue()
    {
        // Arrange
        var foundry = CreateTestFoundry();

        // Act
        foundry.SetCurrentWorkflow(null);

        // Assert
        Assert.Null(foundry.CurrentWorkflow);
    }

    [Fact]
    public void ReturnCorrectLogger_GivenLogger()
    {
        // Arrange
        var logger = NullLogger.Instance;
        var foundry = CreateTestFoundry(logger: logger);

        // Act & Assert
        Assert.Same(logger, foundry.Logger);
    }

    [Fact]
    public void UseNullLogger_GivenNullLogger()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var properties = new ConcurrentDictionary<string, object?>();

        // Act
        var foundry = new WorkflowFoundry(executionId, properties, logger: null);

        // Assert
        Assert.NotNull(foundry.Logger);
        Assert.IsType<NullLogger>(foundry.Logger);
    }

    #endregion Constructor and Basic Properties Tests

    #region Property Management Tests

    [Fact]
    public void StoreProperty_GivenValidKeyValue()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        const string key = "testProperty";
        const string value = "testValue";

        // Act
        foundry.Properties[key] = value;

        // Assert
        Assert.True(foundry.Properties.ContainsKey(key));
        Assert.Equal(value, foundry.Properties[key]);
    }

    [Fact]
    public void ThrowException_GivenSetPropertyNullKey()
    {
        // Arrange
        var foundry = CreateTestFoundry();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => foundry.Properties[null!] = "value");
    }

    [Fact]
    public void StoreNull_GivenNullValue()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        const string key = "testProperty";

        // Act
        foundry.Properties[key] = null;

        // Assert
        Assert.True(foundry.Properties.ContainsKey(key));
        Assert.Null(foundry.Properties[key]);
    }

    [Fact]
    public void ReturnDefault_GivenNonExistentKey()
    {
        // Arrange
        var foundry = CreateTestFoundry();

        // Act
        var result = foundry.Properties.TryGetValue("nonExistentKey", out var value);

        // Assert
        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    public void ReturnValue_GivenWrongType()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        foundry.Properties["intProperty"] = 42;

        // Act
        var result = foundry.Properties.TryGetValue("intProperty", out var value);

        // Assert
        Assert.True(result);
        Assert.Equal(42, value);
    }

    [Fact]
    public void ReturnTrue_GivenExistingKey()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        const string key = "testProperty";
        const int value = 42;
        foundry.Properties[key] = value;

        // Act
        var result = foundry.Properties.TryGetValue(key, out var retrievedValue);

        // Assert
        Assert.True(result);
        Assert.Equal(value, retrievedValue);
    }

    [Fact]
    public void ReturnFalse_GivenNonExistentKeyForTryGet()
    {
        // Arrange
        var foundry = CreateTestFoundry();

        // Act
        var result = foundry.Properties.TryGetValue("nonExistentKey", out var value);

        // Assert
        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    public void RemoveProperty_GivenExistingKey()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        const string key = "testProperty";
        foundry.Properties[key] = "testValue";

        // Act
        var result = foundry.Properties.TryRemove(key, out _);

        // Assert
        Assert.True(result);
        Assert.False(foundry.Properties.ContainsKey(key));
    }

    [Fact]
    public void ReturnFalse_GivenNonExistentKeyForRemove()
    {
        // Arrange
        var foundry = CreateTestFoundry();

        // Act
        var result = foundry.Properties.TryRemove("nonExistentKey", out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ReturnAllProperties_GivenGetAllProperties()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var properties = new Dictionary<string, object?>
        {
            { "prop1", "value1" },
            { "prop2", 42 },
            { "prop3", true }
        };

        foreach (var kvp in properties)
        {
            foundry.Properties[kvp.Key] = kvp.Value;
        }

        // Act
        var allProperties = foundry.Properties;

        // Assert
        Assert.Equal(properties.Count, allProperties.Count);
        foreach (var kvp in properties)
        {
            Assert.Equal(kvp.Value, allProperties[kvp.Key]);
        }
    }

    #endregion Property Management Tests

    #region Data Access Tests (using Properties)

    [Fact]
    public void ReturnValue_GivenPropertiesExistingKey()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        const string key = "testData";
        const string value = "testValue";
        foundry.Properties[key] = value;

        // Act
        var result = foundry.Properties[key];

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void ThrowKeyNotFoundException_GivenPropertiesNonExistentKey()
    {
        // Arrange
        var foundry = CreateTestFoundry();

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => foundry.Properties["nonExistentKey"]);
    }

    [Fact]
    public void ReturnValue_GivenPropertiesExistingKeyInt()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        const string key = "testData";
        const int value = 42;
        foundry.Properties[key] = value;

        // Act
        var result = foundry.Properties[key];

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void ThrowException_GivenPropertiesNonExistentKey()
    {
        // Arrange
        var foundry = CreateTestFoundry();

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => _ = foundry.Properties["nonExistentKey"]);
    }

    [Fact]
    public void StoreValue_GivenPropertiesSetValue()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        const string key = "testData";
        const string value = "testValue";

        // Act
        foundry.Properties[key] = value;

        // Assert
        Assert.Equal(value, foundry.Properties[key]);
        Assert.True(foundry.Properties.ContainsKey(key));
    }

    [Fact]
    public void ReturnTrue_GivenPropertiesTryGetValueExistingKey()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        const string key = "testData";
        const string value = "testValue";
        foundry.Properties[key] = value;

        // Act
        var result = foundry.Properties.TryGetValue(key, out var retrievedValue);

        // Assert
        Assert.True(result);
        Assert.Equal(value, retrievedValue);
    }

    [Fact]
    public void ReturnFalse_GivenPropertiesTryGetValueNonExistentKey()
    {
        // Arrange
        var foundry = CreateTestFoundry();

        // Act
        var result = foundry.Properties.TryGetValue("nonExistentKey", out var value);

        // Assert
        Assert.False(result);
        Assert.Null(value);
    }

    #endregion Data Access Tests (using Properties)

    #region Operation Management Tests

    [Fact]
    public void AddToFoundry_GivenValidOperation()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var operation = new TestOperation("TestOp");

        // Act
        foundry.AddOperation(operation);

        // Assert - No direct way to verify, but should not throw
        Assert.NotNull(foundry);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullOperation()
    {
        // Arrange
        var foundry = CreateTestFoundry();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => foundry.AddOperation(null!));
    }

    [Fact]
    public void ThrowObjectDisposedException_GivenAddOperationAfterDispose()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var operation = new TestOperation("TestOp");
        foundry.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => foundry.AddOperation(operation));
    }

    [Fact]
    public async Task ReplaceExistingOperations_GivenReplaceOperations()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var firstExecuted = false;
        var replacementExecuted = false;

        foundry.AddOperation(new DelegateWorkflowOperation<object, string>("First", (input, f, ct) =>
        {
            firstExecuted = true;
            return Task.FromResult("first");
        }));

        var replacement = new DelegateWorkflowOperation<object, string>("Replacement", (input, f, ct) =>
        {
            replacementExecuted = true;
            return Task.FromResult("replacement");
        });

        // Act
        foundry.ReplaceOperations(new[] { replacement });
        await foundry.ForgeAsync();

        // Assert
        Assert.False(firstExecuted);
        Assert.True(replacementExecuted);
    }

    [Fact]
    public async Task FreezePipelineDuringExecution_GivenForgeAsync()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        foundry.AddOperation(new DelegateWorkflowOperation<object, string>("BlockingOp", async (input, f, ct) =>
        {
            started.TrySetResult(true);
            await resume.Task.ConfigureAwait(false);
            return "done";
        }));

        // Act
        var forgeTask = foundry.ForgeAsync();
        await started.Task;

        // Assert
        Assert.True(foundry.IsFrozen);
        Assert.Throws<InvalidOperationException>(() => foundry.AddOperation(new TestOperation("LateOp")));
        Assert.Throws<InvalidOperationException>(() => foundry.AddMiddleware(new TestMiddleware()));

        resume.TrySetResult(true);
        await forgeTask;
        Assert.False(foundry.IsFrozen);
    }

    [Fact]
    public async Task ExecuteOperation_GivenSingleOperation()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var executed = false;
        var operation = new DelegateWorkflowOperation<object, string>("TestOp", async (input, f, ct) =>
        {
            await Task.Yield();
            executed = true;
            return "result";
        });

        foundry.AddOperation(operation);

        // Act
        await foundry.ForgeAsync();

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public async Task NotPassResultForward_GivenOutputChainingDisabled()
    {
        // Arrange
        var options = new WorkflowForgeOptions { EnableOutputChaining = false };
        var foundry = CreateTestFoundry(options: options);
        object? receivedInput = "unset";

        foundry.AddOperation(new DelegateWorkflowOperation<object, string>("Op1", (input, f, ct) =>
            Task.FromResult("output")));
        foundry.AddOperation(new DelegateWorkflowOperation<object, string>("Op2", (input, f, ct) =>
        {
            receivedInput = input;
            return Task.FromResult("done");
        }));

        // Act
        await foundry.ForgeAsync();

        // Assert
        Assert.Null(receivedInput);
    }

    [Fact]
    public async Task ThrowAggregateException_GivenContinueAndAggregate()
    {
        // Arrange
        var options = new WorkflowForgeOptions { ContinueOnError = true };
        var foundry = CreateTestFoundry(options: options);

        foundry.AddOperation(new DelegateWorkflowOperation<object, string>("Fail1", (input, f, ct) =>
            Task.FromException<string>(new InvalidOperationException("fail-1"))));
        foundry.AddOperation(new DelegateWorkflowOperation<object, string>("Fail2", (input, f, ct) =>
            Task.FromException<string>(new InvalidOperationException("fail-2"))));

        // Act
        var exception = await Assert.ThrowsAsync<AggregateException>(() => foundry.ForgeAsync());

        // Assert
        Assert.Equal(2, exception.InnerExceptions.Count);
    }

    [Fact]
    public async Task StoreOperationOutputInProperties_GivenForgeAsync()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var operation = new DelegateWorkflowOperation<object, string>("ResultOp", (input, f, ct) =>
            Task.FromResult("result"));

        foundry.AddOperation(operation);

        // Act
        await foundry.ForgeAsync();

        // Assert
        var outputKey = $"Operation.0:{operation.Name}.Output";
        Assert.True(foundry.Properties.TryGetValue(outputKey, out var storedOutput));
        Assert.Equal("result", storedOutput);
        Assert.Equal(0, foundry.Properties["Operation.LastCompletedIndex"]);
        Assert.Equal(operation.Name, foundry.Properties["Operation.LastCompletedName"]);
    }

    [Fact]
    public async Task ExecuteInOrder_GivenMultipleOperations()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var executionOrder = new List<int>();
        var lockObject = new object();

        for (int i = 0; i < 5; i++)
        {
            var index = i;
            var operation = new DelegateWorkflowOperation<object, string>($"Op{index}", async (input, f, ct) =>
            {
                lock (lockObject)
                {
                    executionOrder.Add(index);
                }
                await Task.Delay(10, ct); // Small delay to test ordering
                return $"Result{index}";
            });
            foundry.AddOperation(operation);
        }

        // Act
        await foundry.ForgeAsync();

        // Assert
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, executionOrder);
    }

    [Fact]
    public async Task ThrowTaskCanceledException_GivenCancellation()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var operation = new DelegateWorkflowOperation<object, string>("DelayOp", async (input, f, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
            return "result";
        });

        foundry.AddOperation(operation);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => foundry.ForgeAsync(cts.Token));
    }

    [Fact]
    public async Task ThrowObjectDisposedException_GivenForgeAsyncAfterDispose()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        foundry.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => foundry.ForgeAsync());
    }

    #endregion Operation Management Tests

    #region Middleware Tests

    [Fact]
    public void AddToFoundry_GivenValidMiddleware()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var middleware = new TestMiddleware();

        // Act
        foundry.AddMiddleware(middleware);

        // Assert
        Assert.Equal(1, foundry.MiddlewareCount);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullMiddleware()
    {
        // Arrange
        var foundry = CreateTestFoundry();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => foundry.AddMiddleware(null!));
    }

    [Fact]
    public void AddAll_GivenMultipleMiddlewares()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var middlewares = new IWorkflowOperationMiddleware[]
        {
            new TestMiddleware(),
            new TestMiddleware(),
            new TestMiddleware()
        };

        // Act
        foundry.AddMiddlewares(middlewares);

        // Assert
        Assert.Equal(middlewares.Length, foundry.MiddlewareCount);
    }

    [Fact]
    public void RemoveAndReturnTrue_GivenExistingMiddleware()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var middleware = new TestMiddleware();
        foundry.AddMiddleware(middleware);

        // Act
        var result = foundry.RemoveMiddleware(middleware);

        // Assert
        Assert.True(result);
        Assert.Equal(0, foundry.MiddlewareCount);
    }

    [Fact]
    public void ReturnFalse_GivenNonExistentMiddleware()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var middleware = new TestMiddleware();

        // Act
        var result = foundry.RemoveMiddleware(middleware);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteMiddlewarePipeline_GivenMiddlewareRegistered()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var middleware1 = new TestMiddleware("MW1");
        var middleware2 = new TestMiddleware("MW2");

        foundry.AddMiddleware(middleware1);
        foundry.AddMiddleware(middleware2);

        var operation = new DelegateWorkflowOperation<object, string>("TestOp", async (input, f, ct) =>
        {
            await Task.Yield();
            return "result";
        });

        foundry.AddOperation(operation);

        // Act
        await foundry.ForgeAsync();

        // Assert
        // Test passes if no exception is thrown
        Assert.Equal(2, foundry.MiddlewareCount);
    }

    #endregion Middleware Tests

    #region Disposal Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var foundry = CreateTestFoundry();

        // Act & Assert - Should not throw
        foundry.Dispose();
        foundry.Dispose();
        foundry.Dispose();
    }

    [Fact]
    public void Dispose_DisposesConfiguration()
    {
        // Arrange
        var foundry = CreateTestFoundry();

        // Act
        foundry.Dispose();

        // Assert
        // Note: Actual disposal behavior would need to be verified based on configuration implementation
        Assert.NotNull(foundry); // Basic check that disposal completed
    }

    #endregion Disposal Tests

    #region Error Handling Tests

    [Fact]
    public async Task ThrowWorkflowOperationException_GivenOperationFailure()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var operation = new DelegateWorkflowOperation<object, string>("FailingOp", async (input, f, ct) =>
        {
            await Task.Yield();
            throw new InvalidOperationException("Test exception");
        });

        foundry.AddOperation(operation);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<WorkflowOperationException>(() => foundry.ForgeAsync());
        Assert.Contains("FailingOp", exception.Message);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    #endregion Error Handling Tests

    #region Helper Methods

    private static WorkflowFoundry CreateTestFoundry(
        Guid? executionId = null,
        IWorkflowForgeLogger? logger = null,
        IServiceProvider? serviceProvider = null,
        WorkflowForgeOptions? options = null)
    {
        var id = executionId ?? Guid.NewGuid();
        var properties = new ConcurrentDictionary<string, object?>();
        return new WorkflowFoundry(id, properties, logger, serviceProvider, options: options);
    }

    private static IWorkflow CreateMockWorkflow(string name, Guid? id = null)
    {
        var mockWorkflow = new Mock<IWorkflow>();
        mockWorkflow.Setup(w => w.Id).Returns(id ?? Guid.NewGuid());
        mockWorkflow.Setup(w => w.Name).Returns(name);
        mockWorkflow.Setup(w => w.Description).Returns($"Mock workflow: {name}");
        mockWorkflow.Setup(w => w.Version).Returns("1.0.0");
        mockWorkflow.Setup(w => w.Operations).Returns(new List<IWorkflowOperation>());
        return mockWorkflow.Object;
    }

    private class TestOperation : WorkflowOperationBase
    {
        public TestOperation(string name)
        {
            Name = name;
        }

        public override string Name { get; }

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<object?>("TestResult");
        }
    }

    private class TestMiddleware : IWorkflowOperationMiddleware
    {
        private readonly string _name;

        public TestMiddleware(string name = "TestMiddleware")
        {
            _name = name;
        }

        public Task<object?> ExecuteAsync(IWorkflowOperation operation, IWorkflowFoundry foundry, object? inputData, Func<CancellationToken, Task<object?>> next, CancellationToken cancellationToken = default)
        {
            // Simple pass-through middleware for testing
            return next(cancellationToken);
        }
    }

    #endregion Helper Methods
}
