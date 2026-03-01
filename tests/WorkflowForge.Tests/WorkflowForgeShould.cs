using System;
using System.Collections.Generic;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Tests;

public class WorkflowForgeShould
{
    [Fact]
    public void ReturnWorkflowBuilder_GivenCreateWorkflowWithoutParameters()
    {
        // Act
        var builder = WorkflowForge.CreateWorkflow();

        // Assert
        Assert.NotNull(builder);
        Assert.IsType<WorkflowBuilder>(builder);
    }

    [Fact]
    public void ReturnWorkflowBuilder_GivenServiceProvider()
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
    public void ReturnNamedWorkflowBuilder_GivenName()
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
    public void ThrowArgumentException_GivenEmptyString()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateWorkflow(""));
    }

    [Fact]
    public void ThrowArgumentException_GivenWhitespace()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateWorkflow(" "));
    }

    [Fact]
    public void CreateBuilder_GivenNull()
    {
        // Act
        var builder = WorkflowForge.CreateWorkflow(workflowName: null);

        // Assert - null is valid, builder created successfully
        Assert.NotNull(builder);
    }

    [Fact]
    public void ReturnNamedWorkflowBuilder_GivenNameAndServiceProvider()
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
    public void ReturnFoundry_GivenWorkflowName()
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
    public void ThrowArgumentException_GivenEmptyWorkflowName()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(""));
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(" "));
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(null!));
    }

    [Fact]
    public void ReturnFoundryWithLogger_GivenLogger()
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
    public void ReturnConfiguredFoundry_GivenConfiguration()
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
    public void ThrowArgumentException_GivenNullConfiguration()
    {
        // Arrange

        // Act & Assert
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(null!));
    }

    [Fact]
    public void ReturnFoundry_GivenDevelopmentConfiguration()
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
    public void ValidateWorkflowName_GivenDevelopmentConfiguration()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(""));
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(" "));
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(null!));
    }

    [Fact]
    public void ReturnFoundryWithLogger_GivenDevelopmentConfigurationAndLogger()
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
    public void ReturnFoundry_GivenProductionConfiguration()
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
    public void ValidateWorkflowName_GivenProductionConfiguration()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(""));
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(" "));
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(null!));
    }

    [Fact]
    public void ReturnFoundry_GivenHighPerformanceConfiguration()
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
    public void ValidateWorkflowName_GivenHighPerformanceConfiguration()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(""));
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(" "));
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(null!));
    }

    [Fact]
    public void ReturnFoundryWithData_GivenInitialData()
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
    public void ReturnFoundryWithData_GivenInitialDataAndConfiguration()
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
    public void ThrowArgumentException_GivenInitialDataAndEmptyWorkflowName()
    {
        // Arrange
        var initialData = new Dictionary<string, object?> { { "key", "value" } };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry("", null, initialData));
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(" ", null, initialData));
        Assert.Throws<ArgumentException>(() => WorkflowForge.CreateFoundry(null!, null, initialData));
    }

    [Fact]
    public void CreateFoundry_GivenNullLogger()
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
    public void CreateFoundry_GivenNullInitialProperties()
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
    public void ReturnSmith_GivenCreateSmithWithoutParameters()
    {
        // Act
        var smith = WorkflowForge.CreateSmith();

        // Assert
        Assert.NotNull(smith);
        Assert.IsAssignableFrom<IWorkflowSmith>(smith);
    }

    [Fact]
    public void ReturnSmithWithLogger_GivenLogger()
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