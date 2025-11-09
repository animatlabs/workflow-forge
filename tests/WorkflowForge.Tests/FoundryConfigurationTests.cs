using System;
using WorkflowForge.Configurations;
using WorkflowForge.Loggers;

namespace WorkflowForge.Tests;

public class FoundryConfigurationTests
{
    [Fact]
    public void Default_ReturnsConfigurationWithDefaultValues()
    {
        // Act
        var config = FoundryConfiguration.Default();

        // Assert
        Assert.NotNull(config);
        Assert.Equal(TimeSpan.FromSeconds(30), config.DefaultTimeout);
        Assert.Equal(3, config.MaxRetryAttempts);
        Assert.False(config.EnableParallelExecution);
        Assert.Equal(Environment.ProcessorCount, config.MaxDegreeOfParallelism);
        Assert.True(config.EnableDetailedTiming);
        Assert.True(config.AutoDisposeOperations);
        Assert.Null(config.Logger);
        Assert.Null(config.ServiceProvider);
    }

    [Fact]
    public void Minimal_ReturnsConfigurationWithDefaultValues()
    {
        // Act
        var config = FoundryConfiguration.Minimal();

        // Assert
        Assert.NotNull(config);
        Assert.Equal(TimeSpan.FromSeconds(30), config.DefaultTimeout);
        Assert.Equal(3, config.MaxRetryAttempts);
        Assert.False(config.EnableParallelExecution);
        Assert.True(config.EnableDetailedTiming);
        Assert.True(config.AutoDisposeOperations);
    }

    [Fact]
    public void Development_ReturnsOptimizedForDevelopment()
    {
        // Act
        var config = FoundryConfiguration.Development();

        // Assert
        Assert.NotNull(config);
        Assert.True(config.EnableDetailedTiming);
        Assert.False(config.EnableParallelExecution);
        Assert.Equal(TimeSpan.FromMinutes(10), config.DefaultTimeout);
        Assert.Equal(1, config.MaxRetryAttempts);
    }

    [Fact]
    public void ForDevelopment_ReturnsOptimizedForDevelopment()
    {
        // Act
        var config = FoundryConfiguration.ForDevelopment();

        // Assert
        Assert.NotNull(config);
        Assert.True(config.EnableDetailedTiming);
        Assert.False(config.EnableParallelExecution);
        Assert.Equal(TimeSpan.FromMinutes(10), config.DefaultTimeout);
        Assert.Equal(1, config.MaxRetryAttempts);
    }

    [Fact]
    public void ForProduction_ReturnsOptimizedForProduction()
    {
        // Act
        var config = FoundryConfiguration.ForProduction();

        // Assert
        Assert.NotNull(config);
        Assert.True(config.EnableDetailedTiming);
        Assert.False(config.EnableParallelExecution);
        Assert.Equal(TimeSpan.FromMinutes(2), config.DefaultTimeout);
        Assert.Equal(3, config.MaxRetryAttempts);
        Assert.Equal(Environment.ProcessorCount, config.MaxDegreeOfParallelism);
    }

    [Fact]
    public void HighPerformance_ReturnsOptimizedForPerformance()
    {
        // Act
        var config = FoundryConfiguration.HighPerformance();

        // Assert
        Assert.NotNull(config);
        Assert.False(config.EnableDetailedTiming);
        Assert.True(config.EnableParallelExecution);
        Assert.Equal(Environment.ProcessorCount * 2, config.MaxDegreeOfParallelism);
        Assert.Equal(TimeSpan.FromMinutes(5), config.DefaultTimeout);
    }

    [Fact]
    public void ForHighPerformance_ReturnsOptimizedForPerformance()
    {
        // Act
        var config = FoundryConfiguration.ForHighPerformance();

        // Assert
        Assert.NotNull(config);
        Assert.False(config.EnableDetailedTiming);
        Assert.True(config.EnableParallelExecution);
        Assert.Equal(Environment.ProcessorCount * 2, config.MaxDegreeOfParallelism);
        Assert.Equal(TimeSpan.FromMinutes(5), config.DefaultTimeout);
    }

    [Fact]
    public void Logger_CanBeSetAndRetrieved()
    {
        // Arrange
        var config = FoundryConfiguration.Default();
        var logger = new ConsoleLogger("test");

        // Act
        config.Logger = logger;

        // Assert
        Assert.Equal(logger, config.Logger);
    }

    [Fact]
    public void ServiceProvider_CanBeSetAndRetrieved()
    {
        // Arrange
        var config = FoundryConfiguration.Default();
        var serviceProvider = new Mock<IServiceProvider>().Object;

        // Act
        config.ServiceProvider = serviceProvider;

        // Assert
        Assert.Equal(serviceProvider, config.ServiceProvider);
    }

    [Fact]
    public void Properties_CanBeModified()
    {
        // Arrange
        var config = FoundryConfiguration.Default();

        // Act
        config.DefaultTimeout = TimeSpan.FromHours(1);
        config.MaxRetryAttempts = 5;
        config.EnableParallelExecution = true;
        config.MaxDegreeOfParallelism = 8;
        config.EnableDetailedTiming = false;
        config.AutoDisposeOperations = false;

        // Assert
        Assert.Equal(TimeSpan.FromHours(1), config.DefaultTimeout);
        Assert.Equal(5, config.MaxRetryAttempts);
        Assert.True(config.EnableParallelExecution);
        Assert.Equal(8, config.MaxDegreeOfParallelism);
        Assert.False(config.EnableDetailedTiming);
        Assert.False(config.AutoDisposeOperations);
    }
}