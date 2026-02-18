using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Operations;
using WorkflowForge.Testing;

namespace WorkflowForge.Tests;

/// <summary>
/// Comprehensive tests for WorkflowBuilder covering fluent API, validation,
/// complex scenarios, and edge cases.
/// </summary>
public class WorkflowBuilderTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithoutServiceProvider_CreatesBuilder()
    {
        // Act
        var builder = WorkflowForge.CreateWorkflow();

        // Assert
        Assert.NotNull(builder);
        Assert.Null(builder.ServiceProvider);
        Assert.Equal("1.0.0", builder.Version);
    }

    [Fact]
    public void Constructor_WithServiceProvider_CreatesBuilder()
    {
        // Arrange
        var serviceProvider = new TestServiceProvider();

        // Act
        var builder = WorkflowForge.CreateWorkflow(serviceProvider: serviceProvider);

        // Assert
        Assert.NotNull(builder);
        Assert.Same(serviceProvider, builder.ServiceProvider);
        Assert.Equal("1.0.0", builder.Version);
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_CreatesBuilder()
    {
        // Act
        var builder = WorkflowForge.CreateWorkflow(null);

        // Assert
        Assert.NotNull(builder);
        Assert.Null(builder.ServiceProvider);
        Assert.Equal("1.0.0", builder.Version);
    }

    [Fact]
    public void Create_WithNameAndServiceProvider_SetsNameCorrectly()
    {
        // Arrange
        const string workflowName = "TestWorkflow";
        var serviceProvider = new TestServiceProvider();

        // Act
        var builder = WorkflowForge.CreateWorkflow(workflowName, serviceProvider);

        // Assert
        Assert.NotNull(builder);
        Assert.Equal(workflowName, builder.Name);
        Assert.Same(serviceProvider, builder.ServiceProvider);
    }

    [Fact]
    public void Create_WithNameOnly_SetsNameCorrectly()
    {
        // Arrange
        const string workflowName = "TestWorkflow";

        // Act
        var builder = WorkflowForge.CreateWorkflow(workflowName);

        // Assert
        Assert.NotNull(builder);
        Assert.Equal(workflowName, builder.Name);
        Assert.Null(builder.ServiceProvider);
    }

    #endregion Constructor Tests

    #region WithName Tests

    [Fact]
    public void WithName_SetsWorkflowName()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow();
        const string workflowName = "TestWorkflow";

        // Act
        var result = builder.WithName(workflowName);

        // Assert
        Assert.Same(builder, result); // Should return the same instance for fluent API
        Assert.Equal(workflowName, builder.Name);
    }

    [Fact]
    public void WithName_WithNullName_ThrowsArgumentException()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithName(null!));
        Assert.Throws<ArgumentException>(() => builder.WithName(""));
        Assert.Throws<ArgumentException>(() => builder.WithName(" "));
    }

    [Fact]
    public void WithName_WithNameAndServiceProvider_ReturnsNamedWorkflowBuilder()
    {
        // Arrange
        const string workflowName = "TestWorkflow";
        var serviceProvider = new TestServiceProvider();

        // Act
        var builder = WorkflowForge.CreateWorkflow(serviceProvider: serviceProvider).WithName(workflowName);

        // Assert
        Assert.NotNull(builder);
        Assert.IsType<WorkflowBuilder>(builder);
        Assert.Equal(workflowName, ((WorkflowBuilder)builder).Name);
    }

    [Fact]
    public void WithName_CalledMultipleTimes_UsesLastName()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow();

        // Act
        var result = builder
            .WithName("FirstName")
            .WithName("SecondName")
            .WithName("FinalName");

        // Assert
        Assert.Same(builder, result);
        Assert.Equal("FinalName", builder.Name);
    }

    #endregion WithName Tests

    #region WithDescription Tests

    [Fact]
    public void WithDescription_SetsWorkflowDescription()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow();
        const string description = "Test workflow description";

        // Act
        var result = builder.WithDescription(description);

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(description, builder.Description);
    }

    [Fact]
    public void WithDescription_WithNullDescription_SetsNull()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow();

        // Act
        var result = builder.WithDescription(null);

        // Assert
        Assert.Same(builder, result);
        Assert.Null(builder.Description);
    }

    [Fact]
    public void WithDescription_CalledMultipleTimes_UsesLastDescription()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow();

        // Act
        var result = builder
            .WithDescription("First description")
            .WithDescription("Final description");

        // Assert
        Assert.Same(builder, result);
        Assert.Equal("Final description", builder.Description);
    }

    #endregion WithDescription Tests

    #region WithVersion Tests

    [Fact]
    public void WithVersion_SetsWorkflowVersion()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow();
        const string version = "2.1.0";

        // Act
        var result = builder.WithVersion(version);

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(version, builder.Version);
    }

    [Fact]
    public void WithVersion_WithNullOrEmptyVersion_ThrowsArgumentException()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithVersion(null!));
        Assert.Throws<ArgumentException>(() => builder.WithVersion(""));
        Assert.Throws<ArgumentException>(() => builder.WithVersion(" "));
    }

    [Fact]
    public void WithVersion_CalledMultipleTimes_UsesLastVersion()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow();

        // Act
        var result = builder
            .WithVersion("1.0.0")
            .WithVersion("2.0.0");

        // Assert
        Assert.Same(builder, result);
        Assert.Equal("2.0.0", builder.Version);
    }

    #endregion WithVersion Tests

    #region AddOperation Tests

    [Fact]
    public void AddOperation_WithOperation_AddsOperation()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow();
        var operation = new TestOperation();

        // Act
        var result = builder.AddOperation(operation);

        // Assert
        Assert.Same(builder, result); // Should return the same instance for fluent API
        Assert.Single(builder.Operations);
        Assert.Same(operation, builder.Operations[0]);
    }

    [Fact]
    public void AddOperation_WithNullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.AddOperation((IWorkflowOperation)null!));
    }

    [Fact]
    public void AddOperation_WithAction_AddsActionOperation()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow();
        const string actionName = "TestAction";
        Action<IWorkflowFoundry> action = (foundry) => { };

        // Act
        var result = builder.AddOperation(actionName, action);

        // Assert
        Assert.Same(builder, result); // Should return the same instance for fluent API
        Assert.Single(builder.Operations);
        Assert.IsType<ActionWorkflowOperation>(builder.Operations[0]);
        Assert.Equal(actionName, builder.Operations[0].Name);
    }

    [Fact]
    public void AddOperation_WithAsyncAction_AddsActionOperation()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow();
        const string actionName = "TestAsyncAction";
        Func<IWorkflowFoundry, CancellationToken, Task> action = (foundry, ct) => Task.CompletedTask;

        // Act
        var result = builder.AddOperation(actionName, action);

        // Assert
        Assert.Same(builder, result);
        Assert.Single(builder.Operations);
        Assert.IsType<ActionWorkflowOperation>(builder.Operations[0]);
        Assert.Equal(actionName, builder.Operations[0].Name);
    }

    [Fact]
    public void AddOperation_WithNullActionName_ThrowsArgumentException()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow();
        Action<IWorkflowFoundry> action = (foundry) => { };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.AddOperation(null!, action));
        Assert.Throws<ArgumentException>(() => builder.AddOperation("", action));
        Assert.Throws<ArgumentException>(() => builder.AddOperation(" ", action));
    }

    [Fact]
    public void AddOperation_WithNullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow();
        const string actionName = "TestAction";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.AddOperation(actionName, (Action<IWorkflowFoundry>)null!));
        Assert.Throws<ArgumentNullException>(() => builder.AddOperation(actionName, (Func<IWorkflowFoundry, CancellationToken, Task>)null!));
    }

    [Fact]
    public async Task AddOperation_GivenNameActionAndRestoreAction_InvokesRestoreDelegate_WhenRestoreAsyncCalled()
    {
        // Arrange
        var restoreInvoked = false;
        var restoreAction = new Func<IWorkflowFoundry, CancellationToken, Task>((foundry, ct) =>
        {
            restoreInvoked = true;
            return Task.CompletedTask;
        });

        var workflow = WorkflowForge.CreateWorkflow("RestoreTest")
            .AddOperation("RestorableOp", (foundry, ct) => Task.CompletedTask, restoreAction)
            .Build();

        var operation = workflow.Operations[0];
        var foundry = new FakeWorkflowFoundry();

        // Act
        await operation.RestoreAsync("output", foundry, CancellationToken.None);

        // Assert
        Assert.True(restoreInvoked);
    }

    [Fact]
    public async Task AddOperation_GivenAsyncRestoreAction_InvokesRestoreDelegate_WhenRestoreAsyncCalled()
    {
        // Arrange
        var restoreInvoked = false;
        var restoreAction = new Func<IWorkflowFoundry, CancellationToken, Task>(async (foundry, ct) =>
        {
            await Task.Yield();
            restoreInvoked = true;
        });

        var workflow = WorkflowForge.CreateWorkflow("RestoreTest")
            .AddOperation("RestorableOp", (foundry, ct) => Task.CompletedTask, restoreAction)
            .Build();

        var operation = workflow.Operations[0];
        var foundry = new FakeWorkflowFoundry();

        // Act
        await operation.RestoreAsync("output", foundry, CancellationToken.None);

        // Assert
        Assert.True(restoreInvoked);
    }

    [Fact]
    public async Task AddOperation_GivenOperationWithoutRestoreAction_DoesNotThrow_WhenRestoreAsyncCalled()
    {
        // Arrange - ActionWorkflowOperation without restoreAction has no-op RestoreAsync
        var workflow = WorkflowForge.CreateWorkflow("NoRestoreTest")
            .AddOperation("NoRestoreOp", (foundry, ct) => Task.CompletedTask)
            .Build();

        var operation = Assert.IsType<ActionWorkflowOperation>(workflow.Operations[0]);
        var foundry = new FakeWorkflowFoundry();

        // Act & Assert - Should not throw
        await operation.RestoreAsync("output", foundry, CancellationToken.None);
    }

    [Fact]
    public void AddOperation_WithMultipleOperations_AddsAllOperations()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow();
        var operation1 = new TestOperation("Op1");
        var operation2 = new TestOperation("Op2");
        var operation3 = new TestOperation("Op3");

        // Act
        var result = builder
            .AddOperation(operation1)
            .AddOperation(operation2)
            .AddOperation(operation3);

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(3, builder.Operations.Count);
        Assert.Same(operation1, builder.Operations[0]);
        Assert.Same(operation2, builder.Operations[1]);
        Assert.Same(operation3, builder.Operations[2]);
    }

    [Fact]
    public void AddOperation_WithMixedOperationTypes_AddsAllOperations()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow();
        var operation = new TestOperation("OperationStep");
        Action<IWorkflowFoundry> action = (foundry) => { };

        // Act
        var result = builder
            .AddOperation(operation)
            .AddOperation("ActionOperation", action)
            .AddOperation(new TestOperation("AnotherOp"));

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(3, builder.Operations.Count);
        Assert.Same(operation, builder.Operations[0]);
        Assert.IsType<ActionWorkflowOperation>(builder.Operations[1]);
        Assert.IsType<TestOperation>(builder.Operations[2]);
    }

    [Fact]
    public void AddOperation_Generic_WithServiceProvider_CreatesOperationFromDI()
    {
        // Arrange
        var serviceProvider = new TestServiceProvider();
        serviceProvider.RegisterService<TestParameterlessOperation>(() => new TestParameterlessOperation("FromDI"));
        var builder = WorkflowForge.CreateWorkflow(serviceProvider: serviceProvider);

        // Act
        var result = builder.AddOperation<TestParameterlessOperation>();

        // Assert
        Assert.Same(builder, result);
        Assert.Single(builder.Operations);
        Assert.Equal("FromDI", builder.Operations[0].Name);
    }

    [Fact]
    public void AddOperation_Generic_WithoutServiceProvider_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => builder.AddOperation<TestParameterlessOperation>());
    }

    #endregion AddOperation Tests

    #region Build Tests

    [Fact]
    public void Build_WithNameAndOperations_ReturnsWorkflow()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow()
            .WithName("TestWorkflow")
            .AddOperation(new TestOperation());

        // Act
        var workflow = builder.Build();

        // Assert
        Assert.NotNull(workflow);
        Assert.Equal("TestWorkflow", workflow.Name);
        Assert.Single(workflow.Operations);
    }

    [Fact]
    public void Build_WithCompleteConfiguration_ReturnsWorkflowWithAllProperties()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow()
            .WithName("CompleteWorkflow")
            .WithDescription("A complete test workflow")
            .WithVersion("2.0.0")
            .AddOperation(new TestOperation());

        // Act
        var workflow = builder.Build();

        // Assert
        Assert.NotNull(workflow);
        Assert.Equal("CompleteWorkflow", workflow.Name);
        Assert.Equal("A complete test workflow", workflow.Description);
        Assert.Equal("2.0.0", workflow.Version);
        Assert.Single(workflow.Operations);
    }

    [Fact]
    public void Build_WithoutName_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow()
            .AddOperation(new TestOperation());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithoutOperations_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow()
            .WithName("EmptyWorkflow");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithServiceProvider_PassesServiceProviderToWorkflow()
    {
        // Arrange
        var serviceProvider = new TestServiceProvider();
        var builder = WorkflowForge.CreateWorkflow(serviceProvider: serviceProvider)
            .WithName("ServiceWorkflow")
            .AddOperation(new TestOperation());

        // Act
        var workflow = builder.Build();

        // Assert
        Assert.NotNull(workflow);
        Assert.Equal("ServiceWorkflow", workflow.Name);
    }

    [Fact]
    public void Build_CalledMultipleTimes_ReturnsNewWorkflowInstances()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow()
            .WithName("ReusableWorkflow")
            .AddOperation(new TestOperation());

        // Act
        var workflow1 = builder.Build();
        var workflow2 = builder.Build();

        // Assert
        Assert.NotNull(workflow1);
        Assert.NotNull(workflow2);
        Assert.NotSame(workflow1, workflow2);
        Assert.NotEqual(workflow1.Id, workflow2.Id);
    }

    #endregion Build Tests

    #region Sequential and Parallel Tests

    [Fact]
    public void Sequential_WithOperations_CreatesSequentialWorkflow()
    {
        // Arrange
        var operation1 = new TestOperation("Op1");
        var operation2 = new TestOperation("Op2");

        // Act
        var workflow = WorkflowBuilder.Sequential(operation1, operation2);

        // Assert
        Assert.NotNull(workflow);
        Assert.Equal(2, workflow.Operations.Count);
        Assert.Same(operation1, workflow.Operations[0]);
        Assert.Same(operation2, workflow.Operations[1]);
    }

    [Fact]
    public void Sequential_WithNullOperations_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => WorkflowBuilder.Sequential(null!));
    }

    [Fact]
    public void Sequential_WithEmptyOperations_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => WorkflowBuilder.Sequential());
    }

    [Fact]
    public void Parallel_WithOperations_CreatesParallelWorkflow()
    {
        // Arrange
        var operation1 = new TestOperation("Op1");
        var operation2 = new TestOperation("Op2");

        // Act
        var workflow = WorkflowBuilder.Parallel(operation1, operation2);

        // Assert
        Assert.NotNull(workflow);
        Assert.Single(workflow.Operations); // Should contain a ForEachWorkflowOperation
        Assert.IsType<ForEachWorkflowOperation>(workflow.Operations[0]);
    }

    [Fact]
    public void Parallel_WithNullOperations_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => WorkflowBuilder.Parallel(null!));
    }

    [Fact]
    public void Parallel_WithEmptyOperations_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => WorkflowBuilder.Parallel());
    }

    #endregion Sequential and Parallel Tests

    #region Fluent API Tests

    [Fact]
    public void FluentAPI_CanChainMultipleOperations()
    {
        // Arrange & Act
        var workflow = WorkflowForge.CreateWorkflow()
            .WithName("ChainedWorkflow")
            .WithDescription("A workflow built with fluent API")
            .WithVersion("3.0.0")
            .AddOperation(new TestOperation("Initialize"))
            .AddOperation("ValidateInput", foundry =>
            {
                // Validation logic here
            })
            .AddOperation(new TestOperation("ProcessData"))
            .AddOperation("LogResults", foundry =>
            {
                // Logging logic here
            })
            .AddOperation(new TestOperation("Cleanup"))
            .Build();

        // Assert
        Assert.NotNull(workflow);
        Assert.Equal("ChainedWorkflow", workflow.Name);
        Assert.Equal("A workflow built with fluent API", workflow.Description);
        Assert.Equal("3.0.0", workflow.Version);
        Assert.Equal(5, workflow.Operations.Count);
    }

    [Fact]
    public void FluentAPI_WithServiceProvider_MaintainsServiceProvider()
    {
        // Arrange
        var serviceProvider = new TestServiceProvider();
        var builder = WorkflowForge.CreateWorkflow(serviceProvider: serviceProvider)
            .WithName("ServiceWorkflow")
            .AddOperation(new TestOperation("Step1"))
            .AddOperation("Step2", foundry => { });

        // Act
        var workflow = builder.Build();

        // Assert
        Assert.NotNull(workflow);
        Assert.Equal(serviceProvider, builder.ServiceProvider);
        Assert.Equal(2, workflow.Operations.Count);
    }

    #endregion Fluent API Tests

    #region Edge Cases

    [Fact]
    public void Builder_WithVeryLongWorkflowName_HandlesCorrectly()
    {
        // Arrange
        var longName = new string('A', 500);
        var builder = WorkflowForge.CreateWorkflow();

        // Act
        var result = builder.WithName(longName);

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(longName, builder.Name);
    }

    [Fact]
    public void Builder_WithSpecialCharactersInName_HandlesCorrectly()
    {
        // Arrange
        const string specialName = "Special-Workflow_Name@2023!";
        var builder = WorkflowForge.CreateWorkflow();

        // Act
        var result = builder.WithName(specialName);

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(specialName, builder.Name);
    }

    [Fact]
    public void Builder_WithManyOperations_HandlesCorrectly()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow().WithName("ManyOperationsWorkflow");

        // Act
        for (int i = 0; i < 100; i++)
        {
            builder.AddOperation(new TestOperation($"Operation{i}"));
        }

        var workflow = builder.Build();

        // Assert
        Assert.NotNull(workflow);
        Assert.Equal(100, workflow.Operations.Count);
    }

    [Fact]
    public void Builder_AfterBuild_CanBeReused()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow()
            .WithName("ReusableBuilder")
            .AddOperation(new TestOperation("Step1"));

        var workflow1 = builder.Build();

        // Act
        var workflow2 = builder
            .AddOperation(new TestOperation("Step2"))
            .WithDescription("Updated description")
            .Build();

        // Assert
        Assert.NotNull(workflow1);
        Assert.NotNull(workflow2);
        Assert.Single(workflow1.Operations);
        Assert.Equal(2, workflow2.Operations.Count);
        Assert.Null(workflow1.Description);
        Assert.Equal("Updated description", workflow2.Description);
    }

    [Fact]
    public void Builder_StateIsolation_InternalPropertiesAreReadOnly()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow()
            .WithName("IsolationTest")
            .WithDescription("Test description")
            .WithVersion("1.0.0")
            .AddOperation(new TestOperation());

        // Act & Assert
        Assert.IsType<ReadOnlyCollection<IWorkflowOperation>>(builder.Operations);
        Assert.NotNull(builder.Name);
        Assert.NotNull(builder.Description);
        Assert.NotNull(builder.Version);
    }

    #endregion Edge Cases

    #region AddOperations Tests

    [Fact]
    public void AddOperations_Params_AddsAllOperations()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow().WithName("Test");
        var op1 = new TestOperation("Op1");
        var op2 = new TestOperation("Op2");
        var op3 = new TestOperation("Op3");

        // Act
        builder.AddOperations(op1, op2, op3);
        var workflow = builder.Build();

        // Assert
        Assert.Equal(3, workflow.Operations.Count);
        Assert.Same(op1, workflow.Operations[0]);
        Assert.Same(op2, workflow.Operations[1]);
        Assert.Same(op3, workflow.Operations[2]);
    }

    [Fact]
    public void AddOperations_IEnumerable_AddsAllOperations()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow().WithName("Test");
        var operations = new List<IWorkflowOperation>
        {
            new TestOperation("Op1"),
            new TestOperation("Op2")
        };

        // Act
        builder.AddOperations(operations);
        var workflow = builder.Build();

        // Assert
        Assert.Equal(2, workflow.Operations.Count);
    }

    [Fact]
    public void AddOperations_Params_NullThrows()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow().WithName("Test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.AddOperations((IWorkflowOperation[])null!));
    }

    [Fact]
    public void AddOperations_IEnumerable_NullThrows()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow().WithName("Test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.AddOperations((IEnumerable<IWorkflowOperation>)null!));
    }

    [Fact]
    public void AddOperations_ReturnsSameBuilder()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow().WithName("Test");

        // Act
        var result = builder.AddOperations(new TestOperation());

        // Assert
        Assert.Same(builder, result);
    }

    #endregion AddOperations Tests

    #region AddParallelOperations Tests

    [Fact]
    public void AddParallelOperations_Params_CreatesForEachOperation()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow().WithName("Test");
        var op1 = new TestOperation("Op1");
        var op2 = new TestOperation("Op2");

        // Act
        builder.AddParallelOperations(op1, op2);
        var workflow = builder.Build();

        // Assert
        Assert.Single(workflow.Operations);
        Assert.IsType<ForEachWorkflowOperation>(workflow.Operations[0]);
    }

    [Fact]
    public void AddParallelOperations_IEnumerable_WithOptions()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow().WithName("Test");
        var operations = new List<IWorkflowOperation>
        {
            new TestOperation("Op1"),
            new TestOperation("Op2")
        };

        // Act
        builder.AddParallelOperations(
            operations,
            maxConcurrency: 2,
            timeout: TimeSpan.FromSeconds(30),
            name: "ParallelGroup");
        var workflow = builder.Build();

        // Assert
        Assert.Single(workflow.Operations);
        var forEachOp = Assert.IsType<ForEachWorkflowOperation>(workflow.Operations[0]);
        Assert.Equal("ParallelGroup", forEachOp.Name);
    }

    [Fact]
    public void AddParallelOperations_Params_EmptyThrows()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow().WithName("Test");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.AddParallelOperations());
    }

    [Fact]
    public void AddParallelOperations_IEnumerable_EmptyThrows()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow().WithName("Test");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            builder.AddParallelOperations(new List<IWorkflowOperation>()));
    }

    [Fact]
    public void AddParallelOperations_ReturnsSameBuilder()
    {
        // Arrange
        var builder = WorkflowForge.CreateWorkflow().WithName("Test");

        // Act
        var result = builder.AddParallelOperations(new TestOperation());

        // Assert
        Assert.Same(builder, result);
    }

    #endregion AddParallelOperations Tests

    #region Helper Classes

    private class TestOperation : WorkflowOperationBase
    {
        public TestOperation(string? name = null)
        {
            Name = name ?? "TestOperation";
        }

        public override string Name { get; }

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<object?>("TestResult");
        }
    }

    private class TestParameterlessOperation : WorkflowOperationBase
    {
        public TestParameterlessOperation()
        {
            Name = "Parameterless";
        }

        public TestParameterlessOperation(string name)
        {
            Name = name;
        }

        public override string Name { get; }

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<object?>("TestResult");
        }
    }

    private class TestServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, Func<object>> _services = new();

        public void RegisterService<T>(Func<T> factory) where T : class
        {
            _services[typeof(T)] = () => factory();
        }

        public object? GetService(Type serviceType)
        {
            return _services.TryGetValue(serviceType, out var factory) ? factory() : null;
        }
    }

    #endregion Helper Classes
}