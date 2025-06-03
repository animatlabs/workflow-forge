using System;

namespace WorkflowForge
{
    /// <summary>
    /// Defines configuration settings for enterprise workflow execution behavior.
    /// These settings control critical production aspects like resilience, performance, and observability.
    /// </summary>
    public interface IWorkflowSettings
    {
        /// <summary>
        /// Gets whether automatic restoration should be performed when a workflow fails.
        /// Critical for production resilience and error recovery.
        /// </summary>
        bool AutoRestore { get; }

        /// <summary>
        /// Gets whether to continue restoration even if some restoration operations fail.
        /// Affects system behavior under partial failure scenarios.
        /// </summary>
        bool ContinueOnRestorationFailure { get; }

        /// <summary>
        /// Gets the maximum number of concurrent workflows that can be executed.
        /// Essential for resource management and system scaling.
        /// </summary>
        int MaxConcurrentFlows { get; }

        /// <summary>
        /// Gets the number of retry attempts for restoration operations.
        /// Controls reliability behavior and system resilience.
        /// </summary>
        int RestorationRetryAttempts { get; }

        /// <summary>
        /// Gets the timeout for individual operations.
        /// Prevents hanging operations and ensures system responsiveness.
        /// </summary>
        TimeSpan OperationTimeout { get; }

        /// <summary>
        /// Gets the timeout for the entire workflow execution.
        /// Critical for preventing runaway processes in production.
        /// </summary>
        TimeSpan FlowTimeout { get; }

        /// <summary>
        /// Gets whether to enable detailed performance metrics collection.
        /// Required for production monitoring and performance analysis.
        /// </summary>
        bool EnableMetrics { get; }

        /// <summary>
        /// Gets whether to enable distributed tracing.
        /// Essential for debugging and monitoring in distributed systems.
        /// </summary>
        bool EnableTracing { get; }

        /// <summary>
        /// Gets the minimum log level for workflow operations.
        /// Valid values: "Trace", "Debug", "Information", "Warning", "Error", "Critical".
        /// </summary>
        string MinimumLogLevel { get; }
    }
} 
