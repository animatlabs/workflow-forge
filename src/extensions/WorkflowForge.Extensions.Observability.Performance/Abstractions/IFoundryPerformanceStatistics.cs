using System;

namespace WorkflowForge.Extensions.Observability.Performance
{
    /// <summary>
    /// Represents performance statistics for a foundry execution.
    /// Provides insights into the foundry's operational efficiency and resource utilization.
    /// This is an extension interface available when the WorkflowForge.Extensions.Observability.Performance package is installed.
    /// </summary>
    public interface IFoundryPerformanceStatistics
    {
        /// <summary>
        /// Gets the total number of operations executed in the foundry.
        /// </summary>
        int TotalOperations { get; }

        /// <summary>
        /// Gets the number of successfully completed operations.
        /// </summary>
        int SuccessfulOperations { get; }

        /// <summary>
        /// Gets the number of failed operations.
        /// </summary>
        int FailedOperations { get; }

        /// <summary>
        /// Gets the overall success rate as a percentage (0.0 to 1.0).
        /// </summary>
        double SuccessRate { get; }

        /// <summary>
        /// Gets the average execution duration across all operations.
        /// </summary>
        TimeSpan AverageDuration { get; }

        /// <summary>
        /// Gets the minimum execution duration recorded.
        /// </summary>
        TimeSpan MinimumDuration { get; }

        /// <summary>
        /// Gets the maximum execution duration recorded.
        /// </summary>
        TimeSpan MaximumDuration { get; }

        /// <summary>
        /// Gets the total memory allocated during foundry operations.
        /// </summary>
        long TotalMemoryAllocated { get; }

        /// <summary>
        /// Gets the average memory allocated per operation.
        /// </summary>
        long AverageMemoryPerOperation { get; }

        /// <summary>
        /// Gets the timestamp when statistics collection started.
        /// </summary>
        DateTimeOffset StartTime { get; }

        /// <summary>
        /// Gets the timestamp when statistics collection ended or current time if still running.
        /// </summary>
        DateTimeOffset EndTime { get; }

        /// <summary>
        /// Gets the total duration of the foundry execution.
        /// </summary>
        TimeSpan TotalDuration { get; }

        /// <summary>
        /// Gets the operations per second throughput.
        /// </summary>
        double OperationsPerSecond { get; }

        /// <summary>
        /// Gets detailed statistics for a specific operation by name.
        /// </summary>
        /// <param name="operationName">The name of the operation.</param>
        /// <returns>Operation-specific statistics or null if not found.</returns>
        IOperationStatistics? GetOperationStatistics(string operationName);

        /// <summary>
        /// Gets performance statistics for all operations.
        /// </summary>
        /// <returns>A collection of operation statistics.</returns>
        System.Collections.Generic.IReadOnlyList<IOperationStatistics> GetAllOperationStatistics();
    }

    /// <summary>
    /// Represents performance statistics for a specific operation.
    /// </summary>
    public interface IOperationStatistics
    {
        /// <summary>
        /// Gets the name of the operation.
        /// </summary>
        string OperationName { get; }

        /// <summary>
        /// Gets the unique identifier of the operation.
        /// </summary>
        string OperationId { get; }

        /// <summary>
        /// Gets the number of times this operation was executed.
        /// </summary>
        int ExecutionCount { get; }

        /// <summary>
        /// Gets the number of successful executions.
        /// </summary>
        int SuccessfulExecutions { get; }

        /// <summary>
        /// Gets the number of failed executions.
        /// </summary>
        int FailedExecutions { get; }

        /// <summary>
        /// Gets the success rate for this operation (0.0 to 1.0).
        /// </summary>
        double SuccessRate { get; }

        /// <summary>
        /// Gets the average execution time for this operation.
        /// </summary>
        TimeSpan AverageExecutionTime { get; }

        /// <summary>
        /// Gets the minimum execution time recorded.
        /// </summary>
        TimeSpan MinimumExecutionTime { get; }

        /// <summary>
        /// Gets the maximum execution time recorded.
        /// </summary>
        TimeSpan MaximumExecutionTime { get; }

        /// <summary>
        /// Gets the total memory allocated by this operation.
        /// </summary>
        long TotalMemoryAllocated { get; }

        /// <summary>
        /// Gets the average memory allocated per execution.
        /// </summary>
        long AverageMemoryPerExecution { get; }
    }
} 
