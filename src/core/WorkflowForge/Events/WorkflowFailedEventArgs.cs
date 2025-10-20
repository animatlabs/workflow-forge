using System;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Events
{
    /// <summary>
    /// Event arguments for workflow failed events.
    /// Fired when a workflow fails due to an uncaught exception.
    /// </summary>
    public class WorkflowFailedEventArgs : BaseWorkflowForgeEventArgs
    {
        /// <summary>
        /// Gets the timestamp when the workflow failed.
        /// </summary>
        public DateTimeOffset FailedAt => Timestamp;

        /// <summary>
        /// Gets the exception that caused the workflow to fail.
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Gets the name of the operation that failed.
        /// </summary>
        public string FailedOperationName { get; }

        /// <summary>
        /// Gets the total execution duration before failure.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowFailedEventArgs"/> class.
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="failedAt">The failure timestamp.</param>
        /// <param name="exception">The exception that caused the failure.</param>
        /// <param name="failedOperationName">The name of the operation that failed.</param>
        /// <param name="duration">The total execution duration before failure.</param>
        public WorkflowFailedEventArgs(
            IWorkflowFoundry foundry,
            DateTimeOffset failedAt,
            Exception? exception,
            string failedOperationName,
            TimeSpan duration)
            : base(foundry, failedAt)
        {
            Exception = exception;
            FailedOperationName = failedOperationName ?? string.Empty;
            Duration = duration;
        }
    }
}