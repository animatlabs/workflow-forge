using WorkflowForge.Configurations;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Tests.Configuration
{
    /// <summary>
    /// Comprehensive tests for FoundryConfiguration covering all configuration options,
    /// validation, and factory method functionality.
    /// </summary>
    public class FoundryConfigurationTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithDefaults_CreatesValidConfiguration()
        {
            // Act
            var config = new FoundryConfiguration();

            // Assert
            Assert.NotNull(config);
            Assert.Null(config.Logger);
            Assert.Null(config.ServiceProvider);
            Assert.Equal(TimeSpan.FromSeconds(30), config.DefaultTimeout);
            Assert.Equal(3, config.MaxRetryAttempts);
            Assert.False(config.EnableParallelExecution);
            Assert.Equal(Environment.ProcessorCount, config.MaxDegreeOfParallelism);
            Assert.True(config.EnableDetailedTiming);
            Assert.True(config.AutoDisposeOperations);
        }

        #endregion Constructor Tests

        #region Property Tests

        [Fact]
        public void DefaultTimeout_SetValidValue_StoresValue()
        {
            // Arrange
            var config = new FoundryConfiguration();
            var timeout = TimeSpan.FromMinutes(10);

            // Act
            config.DefaultTimeout = timeout;

            // Assert
            Assert.Equal(timeout, config.DefaultTimeout);
        }

        [Fact]
        public void Logger_SetValidLogger_StoresLogger()
        {
            // Arrange
            var config = new FoundryConfiguration();
            var logger = Mock.Of<IWorkflowForgeLogger>();

            // Act
            config.Logger = logger;

            // Assert
            Assert.Same(logger, config.Logger);
        }

        [Fact]
        public void Logger_SetNull_AllowsNull()
        {
            // Arrange
            var config = new FoundryConfiguration();

            // Act
            config.Logger = null;

            // Assert
            Assert.Null(config.Logger);
        }

        [Fact]
        public void ServiceProvider_SetValidProvider_StoresProvider()
        {
            // Arrange
            var config = new FoundryConfiguration();
            var serviceProvider = Mock.Of<IServiceProvider>();

            // Act
            config.ServiceProvider = serviceProvider;

            // Assert
            Assert.Same(serviceProvider, config.ServiceProvider);
        }

        [Fact]
        public void ServiceProvider_SetNull_AllowsNull()
        {
            // Arrange
            var config = new FoundryConfiguration
            {
                ServiceProvider = Mock.Of<IServiceProvider>()
            };

            // Act
            config.ServiceProvider = null;

            // Assert
            Assert.Null(config.ServiceProvider);
        }

        [Fact]
        public void MaxRetryAttempts_SetValidValue_StoresValue()
        {
            // Arrange
            var config = new FoundryConfiguration();
            const int maxRetries = 5;

            // Act
            config.MaxRetryAttempts = maxRetries;

            // Assert
            Assert.Equal(maxRetries, config.MaxRetryAttempts);
        }

        [Fact]
        public void EnableParallelExecution_SetValue_StoresValue()
        {
            // Arrange
            var config = new FoundryConfiguration();

            // Act
            config.EnableParallelExecution = true;

            // Assert
            Assert.True(config.EnableParallelExecution);
        }

        [Fact]
        public void MaxDegreeOfParallelism_SetValidValue_StoresValue()
        {
            // Arrange
            var config = new FoundryConfiguration();
            const int maxParallelism = 8;

            // Act
            config.MaxDegreeOfParallelism = maxParallelism;

            // Assert
            Assert.Equal(maxParallelism, config.MaxDegreeOfParallelism);
        }

        [Fact]
        public void EnableDetailedTiming_SetValue_StoresValue()
        {
            // Arrange
            var config = new FoundryConfiguration();

            // Act
            config.EnableDetailedTiming = false;

            // Assert
            Assert.False(config.EnableDetailedTiming);
        }

        [Fact]
        public void AutoDisposeOperations_SetValue_StoresValue()
        {
            // Arrange
            var config = new FoundryConfiguration();

            // Act
            config.AutoDisposeOperations = false;

            // Assert
            Assert.False(config.AutoDisposeOperations);
        }

        #endregion Property Tests

        #region Factory Method Tests

        [Fact]
        public void Default_CreatesConfigurationWithDefaultSettings()
        {
            // Act
            var config = FoundryConfiguration.Default();

            // Assert
            Assert.NotNull(config);
            Assert.Equal(TimeSpan.FromSeconds(30), config.DefaultTimeout);
            Assert.Equal(3, config.MaxRetryAttempts);
            Assert.False(config.EnableParallelExecution);
            Assert.True(config.EnableDetailedTiming);
            Assert.True(config.AutoDisposeOperations);
        }

        [Fact]
        public void Minimal_CreatesConfigurationWithMinimalSettings()
        {
            // Act
            var config = FoundryConfiguration.Minimal();

            // Assert
            Assert.NotNull(config);
            Assert.Equal(TimeSpan.FromSeconds(30), config.DefaultTimeout);
            Assert.Equal(3, config.MaxRetryAttempts);
            Assert.False(config.EnableParallelExecution);
            Assert.True(config.EnableDetailedTiming);
        }

        [Fact]
        public void HighPerformance_CreatesConfigurationWithHighPerformanceSettings()
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
        public void ForHighPerformance_CreatesConfigurationWithHighPerformanceSettings()
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
        public void Development_CreatesConfigurationWithDevelopmentSettings()
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
        public void ForDevelopment_CreatesConfigurationWithDevelopmentSettings()
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
        public void ForProduction_CreatesConfigurationWithProductionSettings()
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

        #endregion Factory Method Tests

        #region Configuration Scenarios Tests

        [Fact]
        public void Configuration_WithCustomSettings_MaintainsAllProperties()
        {
            // Arrange
            var customLogger = Mock.Of<IWorkflowForgeLogger>();
            var serviceProvider = Mock.Of<IServiceProvider>();

            // Act
            var config = new FoundryConfiguration
            {
                EnableDetailedTiming = false,
                EnableParallelExecution = true,
                Logger = customLogger,
                ServiceProvider = serviceProvider,
                MaxRetryAttempts = 5,
                MaxDegreeOfParallelism = 8,
                DefaultTimeout = TimeSpan.FromMinutes(15),
                AutoDisposeOperations = false
            };

            // Assert
            Assert.False(config.EnableDetailedTiming);
            Assert.True(config.EnableParallelExecution);
            Assert.Same(customLogger, config.Logger);
            Assert.Same(serviceProvider, config.ServiceProvider);
            Assert.Equal(5, config.MaxRetryAttempts);
            Assert.Equal(8, config.MaxDegreeOfParallelism);
            Assert.Equal(TimeSpan.FromMinutes(15), config.DefaultTimeout);
            Assert.False(config.AutoDisposeOperations);
        }

        [Fact]
        public void Configuration_FactoryMethods_ProduceDifferentConfigurations()
        {
            // Act
            var development = FoundryConfiguration.ForDevelopment();
            var production = FoundryConfiguration.ForProduction();
            var minimal = FoundryConfiguration.Minimal();
            var highPerformance = FoundryConfiguration.ForHighPerformance();

            // Assert
            Assert.NotSame(development, production);
            Assert.NotSame(development, minimal);
            Assert.NotSame(production, minimal);
            Assert.NotSame(minimal, highPerformance);

            // Development should have conservative settings
            Assert.True(development.EnableDetailedTiming);
            Assert.False(development.EnableParallelExecution);
            Assert.Equal(1, development.MaxRetryAttempts);

            // Production should be balanced
            Assert.True(production.EnableDetailedTiming);
            Assert.False(production.EnableParallelExecution);
            Assert.Equal(3, production.MaxRetryAttempts);

            // High performance should be optimized
            Assert.False(highPerformance.EnableDetailedTiming);
            Assert.True(highPerformance.EnableParallelExecution);
            Assert.Equal(Environment.ProcessorCount * 2, highPerformance.MaxDegreeOfParallelism);
        }

        #endregion Configuration Scenarios Tests

        #region Edge Cases

        [Fact]
        public void Configuration_WithVeryLargeTimeout_HandlesLargeValues()
        {
            // Arrange
            var config = new FoundryConfiguration();
            var largeTimeout = TimeSpan.FromDays(365);

            // Act
            config.DefaultTimeout = largeTimeout;

            // Assert
            Assert.Equal(largeTimeout, config.DefaultTimeout);
        }

        [Fact]
        public void Configuration_WithMaxTimeout_HandlesMaxValue()
        {
            // Arrange
            var config = new FoundryConfiguration();
            var maxTimeout = TimeSpan.MaxValue;

            // Act
            config.DefaultTimeout = maxTimeout;

            // Assert
            Assert.Equal(maxTimeout, config.DefaultTimeout);
        }

        [Fact]
        public void Configuration_WithVeryHighMaxRetryAttempts_HandlesLargeValues()
        {
            // Arrange
            var config = new FoundryConfiguration();
            const int veryHighRetries = int.MaxValue;

            // Act
            config.MaxRetryAttempts = veryHighRetries;

            // Assert
            Assert.Equal(veryHighRetries, config.MaxRetryAttempts);
        }

        [Fact]
        public void Configuration_WithVeryHighMaxDegreeOfParallelism_HandlesLargeValues()
        {
            // Arrange
            var config = new FoundryConfiguration();
            const int veryHighParallelism = int.MaxValue;

            // Act
            config.MaxDegreeOfParallelism = veryHighParallelism;

            // Assert
            Assert.Equal(veryHighParallelism, config.MaxDegreeOfParallelism);
        }

        #endregion Edge Cases

        #region Thread Safety Tests

        [Fact]
        public async Task Configuration_ConcurrentPropertyAccess_ThreadSafe()
        {
            // Arrange
            var config = new FoundryConfiguration();
            const int iterations = 1000;
            var exceptions = new List<Exception>();

            // Act
            var tasks = new List<System.Threading.Tasks.Task>();

            for (int i = 0; i < 10; i++)
            {
                int threadId = i;
                tasks.Add(System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        for (int j = 0; j < iterations; j++)
                        {
                            config.MaxRetryAttempts = threadId + j;
                            config.EnableParallelExecution = j % 2 == 0;
                            config.EnableDetailedTiming = j % 3 == 0;
                            _ = config.MaxRetryAttempts;
                            _ = config.EnableParallelExecution;
                            _ = config.EnableDetailedTiming;
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }));
            }

            await System.Threading.Tasks.Task.WhenAll(tasks);

            // Assert
            Assert.Empty(exceptions);
        }

        #endregion Thread Safety Tests
    }
}