using System;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowForge.Extensions.Resilience
{
    /// <summary>
    /// Interface for resilience strategies that handle retry logic and failure recovery for workflows.
    /// </summary>
    public interface IWorkflowResilienceStrategy
    {
        /// <summary>
        /// Gets the name of the resilience strategy.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Determines whether a retry should be attempted based on the current attempt and exception.
        /// </summary>
        /// <param name="attemptNumber">The current attempt number (1-based).</param>
        /// <param name="exception">The exception that occurred, if any.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if a retry should be attempted; otherwise, false.</returns>
        Task<bool> ShouldRetryAsync(int attemptNumber, Exception? exception, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the delay before the next retry attempt.
        /// </summary>
        /// <param name="attemptNumber">The current attempt number (1-based).</param>
        /// <param name="exception">The exception that occurred, if any.</param>
        /// <returns>The delay before the next retry attempt.</returns>
        TimeSpan GetRetryDelay(int attemptNumber, Exception? exception);

        /// <summary>
        /// Executes an operation with resilience handling.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken);

        /// <summary>
        /// Executes an operation with resilience handling and returns a result.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation with a result.</returns>
        Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken);
    }
} 
