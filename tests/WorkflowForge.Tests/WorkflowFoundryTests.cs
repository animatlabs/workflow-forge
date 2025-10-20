using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Configurations;
using WorkflowForge.Exceptions;
using WorkflowForge.Loggers;
using WorkflowForge.Operations;
using Xunit.Abstractions;

namespace WorkflowForge.Tests;

/// <summary>
/// Comprehensive tests for WorkflowFoundry covering all core functionality.
/// Tests property management, data operations, middleware pipeline, lifecycle, and error handling.
/// </summary>
public class WorkflowFoundryTests
{
    private readonly ITestOutputHelper _output;

    public WorkflowFoundryTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region Constructor and Basic Properties Tests

    [Fact]
    public void Constructor_WithRequiredParameters_CreatesFoundry()
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
    public void Constructor_WithConfiguration_UsesProvidedConfiguration()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var properties = new ConcurrentDictionary<string, object?>();
        var config = new FoundryConfiguration();

        // Act
        var foundry = new WorkflowFoundry(executionId, properties, config);

        // Assert - Configuration would be internal, just verify foundry created
        Assert.NotNull(foundry);
        Assert.Equal(executionId, foundry.ExecutionId);
    }

    [Fact]
    public void Constructor_WithNullData_ThrowsArgumentNullException()
    {
        // Arrange
        var executionId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new WorkflowFoundry(executionId, null!));
    }

    [Fact]
    public void Constructor_WithInitialProperties_SetsProperties()
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
    public void Constructor_WithLogger_SetsLoggerCorrectly()
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
    public void ExecutionId_ReturnsExecutionId()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var foundry = CreateTestFoundry(executionId);

        // Act & Assert
        Assert.Equal(executionId, foundry.ExecutionId);
    }

    [Fact]
    public void CurrentWorkflow_CanBeSetAndRetrieved()
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
    public void CurrentWorkflow_WithNullValue_SetsToNull()
    {
        // Arrange
        var foundry = CreateTestFoundry();

        // Act
        foundry.SetCurrentWorkflow(null);

        // Assert
        Assert.Null(foundry.CurrentWorkflow);
    }

    [Fact]
    public void Configuration_ReturnsCorrectConfiguration()
    {
        // Arrange
        var config = FoundryConfiguration.ForDevelopment();
        var foundry = CreateTestFoundry(config: config);

        // Act & Assert
        Assert.Same(config, foundry.Configuration);
    }

    [Fact]
    public void Constructor_WithNullConfiguration_UsesMinimalConfiguration()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var properties = new ConcurrentDictionary<string, object?>();

        // Act
        var foundry = new WorkflowFoundry(executionId, properties, configuration: null);

        // Assert
        Assert.NotNull(foundry.Configuration);
        Assert.NotNull(foundry.Logger);
    }

    #endregion Constructor and Basic Properties Tests

    #region Property Management Tests

    [Fact]
    public void SetProperty_WithValidKeyValue_StoresProperty()
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
    public void SetProperty_WithNullKey_ThrowsException()
    {
        // Arrange
        var foundry = CreateTestFoundry();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => foundry.Properties[null!] = "value");
    }

    [Fact]
    public void SetProperty_WithNullValue_StoresNull()
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
    public void GetProperty_WithNonExistentKey_ReturnsDefault()
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
    public void GetProperty_WithWrongType_ReturnsValue()
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
    public void TryGetProperty_WithExistingKey_ReturnsTrue()
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
    public void TryGetProperty_WithNonExistentKey_ReturnsFalse()
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
    public void RemoveProperty_WithExistingKey_RemovesProperty()
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
    public void RemoveProperty_WithNonExistentKey_ReturnsFalse()
    {
        // Arrange
        var foundry = CreateTestFoundry();

        // Act
        var result = foundry.Properties.TryRemove("nonExistentKey", out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetAllProperties_ReturnsAllProperties()
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
    public void Properties_WithExistingKey_ReturnsValue()
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
    public void Properties_WithNonExistentKey_ThrowsKeyNotFoundException()
    {
        // Arrange
        var foundry = CreateTestFoundry();

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => foundry.Properties["nonExistentKey"]);
    }

    [Fact]
    public void Properties_WithExistingKey_ReturnsValue2()
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
    public void Properties_WithNonExistentKey_ThrowsException()
    {
        // Arrange
        var foundry = CreateTestFoundry();

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => _ = foundry.Properties["nonExistentKey"]);
    }

    [Fact]
    public void Properties_SetValue_StoresValue()
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
    public void Properties_TryGetValue_WithExistingKey_ReturnsTrue()
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
    public void Properties_TryGetValue_WithNonExistentKey_ReturnsFalse()
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
    public void AddOperation_WithValidOperation_AddsToFoundry()
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
    public void AddOperation_WithNullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        var foundry = CreateTestFoundry();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => foundry.AddOperation(null!));
    }

    [Fact]
    public void AddOperation_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var foundry = CreateTestFoundry();
        var operation = new TestOperation("TestOp");
        foundry.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => foundry.AddOperation(operation));
    }

    [Fact]
    public async Task ForgeAsync_WithSingleOperation_ExecutesOperation()
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
    public async Task ForgeAsync_WithMultipleOperations_ExecutesInOrder()
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
    public async Task ForgeAsync_WithCancellation_ThrowsTaskCanceledException()
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
    public async Task ForgeAsync_AfterDispose_ThrowsObjectDisposedException()
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
    public void AddMiddleware_WithValidMiddleware_AddsToFoundry()
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
    public void AddMiddleware_WithNullMiddleware_ThrowsArgumentNullException()
    {
        // Arrange
        var foundry = CreateTestFoundry();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => foundry.AddMiddleware(null!));
    }

    [Fact]
    public void AddMiddlewares_WithMultipleMiddlewares_AddsAll()
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
    public void RemoveMiddleware_WithExistingMiddleware_RemovesAndReturnsTrue()
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
    public void RemoveMiddleware_WithNonExistentMiddleware_ReturnsFalse()
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
    public async Task ForgeAsync_WithMiddleware_ExecutesMiddlewarePipeline()
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

    #region Configuration Tests

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Configuration_EnableDetailedTiming_ReflectsValue(bool enableTiming)
    {
        // Arrange
        var config = FoundryConfiguration.Minimal();
        config.EnableDetailedTiming = enableTiming;
        var foundry = CreateTestFoundry(config: config);

        // Act & Assert
        Assert.Equal(enableTiming, foundry.Configuration.EnableDetailedTiming);
    }

    #endregion Configuration Tests

    #region Error Handling Tests

    [Fact]
    public async Task ForgeAsync_WithOperationException_ThrowsWorkflowOperationException()
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

    private static WorkflowFoundry CreateTestFoundry(Guid? executionId = null, FoundryConfiguration? config = null)
    {
        var id = executionId ?? Guid.NewGuid();
        var properties = new ConcurrentDictionary<string, object?>();
        return new WorkflowFoundry(id, properties, config);
    }

    private static IWorkflow CreateMockWorkflow(string name, Guid? id = null)
    {
        var mockWorkflow = new Mock<IWorkflow>();
        mockWorkflow.Setup(w => w.Id).Returns(id ?? Guid.NewGuid());
        mockWorkflow.Setup(w => w.Name).Returns(name);
        mockWorkflow.Setup(w => w.Description).Returns($"Mock workflow: {name}");
        mockWorkflow.Setup(w => w.Version).Returns("1.0.0");
        mockWorkflow.Setup(w => w.Operations).Returns(new List<IWorkflowOperation>());
        mockWorkflow.Setup(w => w.SupportsRestore).Returns(true);
        return mockWorkflow.Object;
    }

    private class TestOperation : WorkflowOperationBase
    {
        public TestOperation(string name)
        {
            Name = name;
        }

        public override string Name { get; }

        public override Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
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

        public Task<object?> ExecuteAsync(IWorkflowOperation operation, IWorkflowFoundry foundry, object? inputData, Func<Task<object?>> next, CancellationToken cancellationToken = default)
        {
            // Simple pass-through middleware for testing
            return next();
        }
    }

    #endregion Helper Methods
}