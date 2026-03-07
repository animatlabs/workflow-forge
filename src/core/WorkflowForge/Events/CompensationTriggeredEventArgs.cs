using System;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Events
{
    /// <summary>
    /// Event arguments for compensation triggered events.
    /// Fired when compensation (rollback/saga pattern) is triggered due to a workflow failure.
    /// </summary>
    public sealed class CompensationTriggeredEventArgs : BaseWorkflowForgeEventArgs
    {
        /// <summary>
        /// Gets the timestamp when compensation was triggered.
        /// </summary>
        public DateTimeOffset TriggeredAt => Timestamp;

        /// <summary>
        /// Gets the reason compensation was triggered.
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// Gets the name of the operation that failed and triggered compensation.
        /// </summary>
        public string FailedOperationName { get; }

        /// <summary>
        /// Gets the exception that triggered compensation.
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompensationTriggeredEventArgs"/> class.
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="triggeredAt">The timestamp when compensation was triggered.</param>
        /// <param name="reason">The reason compensation was triggered.</param>
        /// <param name="failedOperationName">The name of the operation that failed.</param>
        /// <param name="exception">The exception that triggered compensation.</param>
        public CompensationTriggeredEventArgs(
            IWorkflowFoundry foundry,
            DateTimeOffset triggeredAt,
            string reason,
            string failedOperationName,
            Exception? exception)
            : base(foundry, triggeredAt)
        {
            Reason = reason ?? string.Empty;
            FailedOperationName = failedOperationName ?? string.Empty;
            Exception = exception;
        }
    }
}