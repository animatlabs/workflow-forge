using System;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Configurations
{
    /// <summary>
    /// Configuration class for foundry operations with thread-safe defaults.
    /// Contains essential settings for workflow foundry initialization.
    /// </summary>
    public sealed class FoundryConfiguration
    {
        /// <summary>
        /// Gets or sets the maximum timeout for operations.
        /// Default is 30 seconds for balanced performance and reliability.
        /// </summary>
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the logger instance for foundry operations.
        /// Can be null for scenarios where logging is not required.
        /// </summary>
        public IWorkflowForgeLogger? Logger { get; set; }

        /// <summary>
        /// Gets or sets the service provider for dependency injection.
        /// Can be null for scenarios where dependency injection is not required.
        /// </summary>
        public IServiceProvider? ServiceProvider { get; set; }

        /// <summary>
        /// Gets or sets the maximum retry attempts for failed operations.
        /// Default is 3 attempts for resilient operation execution.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets whether operations should run in parallel by default.
        /// Default is false for predictable sequential execution.
        /// </summary>
        public bool EnableParallelExecution { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum degree of parallelism when parallel execution is enabled.
        /// Default is the number of logical processors for optimal resource utilization.
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// Gets or sets whether detailed operation timing should be tracked.
        /// Default is true for performance monitoring and optimization.
        /// </summary>
        public bool EnableDetailedTiming { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the foundry should automatically dispose of operations after execution.
        /// Default is true for proper resource management.
        /// </summary>
        public bool AutoDisposeOperations { get; set; } = true;

        /// <summary>
        /// Creates a new configuration instance with default values.
        /// Useful for fluent configuration patterns.
        /// </summary>
        /// <returns>A new configuration instance with sensible defaults.</returns>
        public static FoundryConfiguration Default() => new();

        /// <summary>
        /// Creates a new configuration instance with minimal settings for basic scenarios.
        /// Alias for Default() to maintain API compatibility.
        /// </summary>
        /// <returns>A new minimal configuration instance.</returns>
        public static FoundryConfiguration Minimal() => Default();

        /// <summary>
        /// Creates a new configuration instance optimized for high-performance scenarios.
        /// Disables detailed timing and enables parallel execution.
        /// </summary>
        /// <returns>A new high-performance configuration instance.</returns>
        public static FoundryConfiguration HighPerformance() => new()
        {
            EnableDetailedTiming = false,
            EnableParallelExecution = true,
            MaxDegreeOfParallelism = Environment.ProcessorCount * 2,
            DefaultTimeout = TimeSpan.FromMinutes(5)
        };

        /// <summary>
        /// Creates a new configuration instance optimized for high-performance scenarios.
        /// Alias for HighPerformance() to maintain API compatibility.
        /// </summary>
        /// <returns>A new high-performance configuration instance.</returns>
        public static FoundryConfiguration ForHighPerformance() => HighPerformance();

        /// <summary>
        /// Creates a new configuration instance optimized for development and debugging.
        /// Enables all logging and timing features with conservative timeouts.
        /// </summary>
        /// <returns>A new development-friendly configuration instance.</returns>
        public static FoundryConfiguration Development() => new()
        {
            EnableDetailedTiming = true,
            EnableParallelExecution = false,
            DefaultTimeout = TimeSpan.FromMinutes(10),
            MaxRetryAttempts = 1
        };

        /// <summary>
        /// Creates a new configuration instance optimized for development and debugging.
        /// Alias for Development() to maintain API compatibility.
        /// </summary>
        /// <returns>A new development-friendly configuration instance.</returns>
        public static FoundryConfiguration ForDevelopment() => Development();

        /// <summary>
        /// Creates a new configuration instance optimized for production scenarios.
        /// Provides balanced performance and monitoring for production workloads.
        /// </summary>
        /// <returns>A new production-optimized configuration instance.</returns>
        public static FoundryConfiguration ForProduction() => new()
        {
            EnableDetailedTiming = true,
            EnableParallelExecution = false,
            DefaultTimeout = TimeSpan.FromMinutes(2),
            MaxRetryAttempts = 3,
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };
    }
}