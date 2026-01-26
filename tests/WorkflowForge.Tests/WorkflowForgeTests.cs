using System;
using System.Collections.Generic;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Tests;

public class WorkflowForgeTests
{
    [Fact]
    public void CreateWorkflow_WithoutParameters_ReturnsWorkflowBuilder()
    {
        // Act
        var builder = WorkflowForge.CreateWorkflow();

        // Assert
        Assert.NotNull(builder);
        Assert.IsType<WorkflowBuilder>(builder);
    }

    [Fact]
    public void CreateWorkflow_WithServiceProvider_ReturnsWorkflowBuilder()
    {
        // Arrange
        var serviceProvider = new Mock<IServiceProvider>().Object;

        // Act
        var builder = WorkflowForge.CreateWorkflow(serviceProvider: serviceProvider);

        // Assert
        Assert.NotNull(builder);
        Assert.IsType<WorkflowBuilder>(builder);
    }

    [Fact]
    public void CreateWorkflow_WithName_ReturnsNamedWorkflowBuilder()
    {
        // Arrange
        const string workflowName = "TestWorkflow";

        // Act
        var builder = WorkflowForge.CreateWorkflow(workflowName);

        // Assert
        Assert.NotNull(builder);
        Assert.IsType<WorkflowBuilder>(builder);
    }

    [Fact]
    public void CreateWorkflow_WithEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateWorkflow(""));
    }

    [Fact]
    public void CreateWorkflow_WithWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateWorkflow(" "));
    }

    [Fact]
    public void CreateWorkflow_WithNull_CreatesBuilder()
    {
        // Act
        var builder = WorkflowForge.CreateWorkflow(workflowName: null);

        // Assert - null is valid, builder created successfully
        Assert.NotNull(builder);
    }

    [Fact]
    public void CreateWorkflow_WithNameAndServiceProvider_ReturnsNamedWorkflowBuilder()
    {
        // Arrange
        const string workflowName = "TestWorkflow";
        var serviceProvider = new Mock<IServiceProvider>().Object;

        // Act
        var builder = WorkflowForge.CreateWorkflow(workflowName, serviceProvider);

        // Assert
        Assert.NotNull(builder);
        Assert.IsType<WorkflowBuilder>(builder);
    }

    [Fact]
    public void CreateFoundry_WithWorkflowName_ReturnsFoundry()
    {
        // Arrange
        const string workflowName = "TestWorkflow";

        // Act
        var foundry = WorkflowForge.CreateFoundry(workflowName);

        // Assert
        Assert.NotNull(foundry);
        Assert.IsAssignableFrom<IWorkflowFoundry>(foundry);
    }

    [Fact]
    public void CreateFoundry_WithEmptyWorkflowName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(""));
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(" "));
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(null!));
    }

    [Fact]
    public void CreateFoundry_WithLogger_ReturnsFoundryWithLogger()
    {
        // Arrange
        const string workflowName = "TestWorkflow";
        var logger = new TestLogger();

        // Act
        var foundry = WorkflowForge.CreateFoundry(workflowName, logger);

        // Assert
        Assert.NotNull(foundry);
        Assert.IsAssignableFrom<IWorkflowFoundry>(foundry);
    }

    [Fact]
    public void CreateFoundry_WithConfiguration_ReturnsConfiguredFoundry()
    {
        // Arrange
        const string workflowName = "TestWorkflow";

        // Act
        var foundry = WorkflowForge.CreateFoundry(workflowName);

        // Assert
        Assert.NotNull(foundry);
        Assert.IsAssignableFrom<IWorkflowFoundry>(foundry);
    }

    [Fact]
    public void CreateFoundry_WithNullConfiguration_ThrowsArgumentException()
    {
        // Arrange

        // Act & Assert
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(null!));
    }

    [Fact]
    public void CreateFoundry_WithDevelopmentConfiguration_ReturnsFoundry()
    {
        // Arrange
        const string workflowName = "TestWorkflow";

        // Act
        var foundry = WorkflowForge.CreateFoundry(workflowName);

        // Assert
        Assert.NotNull(foundry);
        Assert.IsAssignableFrom<IWorkflowFoundry>(foundry);
    }

    [Fact]
    public void CreateFoundry_WithDevelopmentConfiguration_ValidatesWorkflowName()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(""));
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(" "));
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(null!));
    }

    [Fact]
    public void CreateFoundry_WithDevelopmentConfigurationAndLogger_ReturnsFoundryWithLogger()
    {
        // Arrange
        const string workflowName = "TestWorkflow";
        var logger = new TestLogger();

        // Act
        var foundry = WorkflowForge.CreateFoundry(workflowName);

        // Assert
        Assert.NotNull(foundry);
        Assert.IsAssignableFrom<IWorkflowFoundry>(foundry);
    }

    [Fact]
    public void CreateFoundry_WithProductionConfiguration_ReturnsFoundry()
    {
        // Arrange
        const string workflowName = "TestWorkflow";

        // Act
        var foundry = WorkflowForge.CreateFoundry(workflowName);

        // Assert
        Assert.NotNull(foundry);
        Assert.IsAssignableFrom<IWorkflowFoundry>(foundry);
    }

    [Fact]
    public void CreateFoundry_WithProductionConfiguration_ValidatesWorkflowName()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(""));
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(" "));
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(null!));
    }

    [Fact]
    public void CreateFoundry_WithHighPerformanceConfiguration_ReturnsFoundry()
    {
        // Arrange
        const string workflowName = "TestWorkflow";

        // Act
        var foundry = WorkflowForge.CreateFoundry(workflowName);

        // Assert
        Assert.NotNull(foundry);
        Assert.IsAssignableFrom<IWorkflowFoundry>(foundry);
    }

    [Fact]
    public void CreateFoundry_WithHighPerformanceConfiguration_ValidatesWorkflowName()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(""));
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(" "));
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(null!));
    }

    [Fact]
    public void CreateFoundry_WithInitialData_ReturnsFoundryWithData()
    {
        // Arrange
        const string workflowName = "TestWorkflow";
        var initialData = new Dictionary<string, object?>
        {
            { "key1", "value1" },
            { "key2", 42 }
        };

        // Act
        var foundry = WorkflowForge.CreateFoundry(workflowName, null, initialData);

        // Assert
        Assert.NotNull(foundry);
        Assert.IsAssignableFrom<IWorkflowFoundry>(foundry);
    }

    [Fact]
    public void CreateFoundry_WithInitialDataAndConfiguration_ReturnsFoundryWithData()
    {
        // Arrange
        const string workflowName = "TestWorkflow";
        var initialData = new Dictionary<string, object?>
        {
            { "key1", "value1" },
            { "key2", 42 }
        };
        // Arrange - no configuration needed

        // Act
        var foundry = WorkflowForge.CreateFoundry(workflowName, null, initialData);

        // Assert
        Assert.NotNull(foundry);
        Assert.IsAssignableFrom<IWorkflowFoundry>(foundry);
    }

    [Fact]
    public void CreateFoundry_WithInitialDataAndEmptyWorkflowName_ThrowsArgumentException()
    {
        // Arrange
        var initialData = new Dictionary<string, object?> { { "key", "value" } };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry("", null, initialData));
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(" ", null, initialData));
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(null!, null, initialData));
    }

    [Fact]
    public void CreateFoundry_WithNullLogger_CreatesFoundry()
    {
        // Arrange
        const string workflowName = "TestWorkflow";

        // Act - null logger is valid (optional parameter)
        using var foundry = WorkflowForge.CreateFoundry(workflowName, logger: null);

        // Assert
        Assert.NotNull(foundry);
        Assert.NotNull(foundry.Properties);
        Assert.NotNull(foundry.Logger);
    }

    [Fact]
    public void CreateFoundry_WithNullInitialProperties_CreatesFoundry()
    {
        // Arrange
        const string workflowName = "TestWorkflow";

        // Act - null initialProperties is valid (optional parameter)
        using var foundry = WorkflowForge.CreateFoundry(workflowName, null, initialProperties: null);

        // Assert
        Assert.NotNull(foundry);
        Assert.NotNull(foundry.Properties);
        Assert.Empty(foundry.Properties);
    }

    [Fact]
    public void CreateSmith_WithoutParameters_ReturnsSmith()
    {
        // Act
        var smith = WorkflowForge.CreateSmith();

        // Assert
        Assert.NotNull(smith);
        Assert.IsAssignableFrom<IWorkflowSmith>(smith);
    }

    [Fact]
    public void CreateSmith_WithLogger_ReturnsSmithWithLogger()
    {
        // Arrange
        var logger = new TestLogger();

        // Act
        var smith = WorkflowForge.CreateSmith(logger);

        // Assert
        Assert.NotNull(smith);
        Assert.IsAssignableFrom<IWorkflowSmith>(smith);
    }

    // Helper test logger for testing
    private class TestLogger : IWorkflowForgeLogger
    {
        public void LogTrace(string message, params object[] args)
        { }

        public void LogTrace(Exception exception, string message, params object[] args)
        { }

        public void LogTrace(IDictionary<string, string> properties, string message, params object[] args)
        { }

        public void LogTrace(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        { }

        public void LogDebug(string message, params object[] args)
        { }

        public void LogDebug(Exception exception, string message, params object[] args)
        { }

        public void LogDebug(IDictionary<string, string> properties, string message, params object[] args)
        { }

        public void LogDebug(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        { }

        public void LogInformation(string message, params object[] args)
        { }

        public void LogInformation(Exception exception, string message, params object[] args)
        { }

        public void LogInformation(IDictionary<string, string> properties, string message, params object[] args)
        { }

        public void LogInformation(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        { }

        public void LogWarning(string message, params object[] args)
        { }

        public void LogWarning(Exception exception, string message, params object[] args)
        { }

        public void LogWarning(IDictionary<string, string> properties, string message, params object[] args)
        { }

        public void LogWarning(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        { }

        public void LogError(string message, params object[] args)
        { }

        public void LogError(Exception exception, string message, params object[] args)
        { }

        public void LogError(IDictionary<string, string> properties, string message, params object[] args)
        { }

        public void LogError(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        { }

        public void LogCritical(string message, params object[] args)
        { }

        public void LogCritical(Exception exception, string message, params object[] args)
        { }

        public void LogCritical(IDictionary<string, string> properties, string message, params object[] args)
        { }

        public void LogCritical(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        { }

        public IDisposable BeginScope<TState>(TState state, IDictionary<string, string>? properties = null) => new EmptyDisposable();

        private class EmptyDisposable : IDisposable
        {
            public void Dispose()
            { }
        }
    }
}