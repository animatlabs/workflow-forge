using System;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Events
{
    /// <summary>
    /// Event arguments for operation restore failed events.
    /// Fired when an operation's restoration (compensation/rollback) fails with an exception.
    /// </summary>
    public class OperationRestoreFailedEventArgs : BaseWorkflowForgeEventArgs
    {
        /// <summary>
        /// Gets the operation that failed to restore.
        /// </summary>
        public IWorkflowOperation Operation { get; }

        /// <summary>
        /// Gets the timestamp when restoration failed.
        /// </summary>
        public DateTimeOffset FailedAt => Timestamp;

        /// <summary>
        /// Gets the exception that occurred during restoration.
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Gets the execution duration before failure.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationRestoreFailedEventArgs"/> class.
        /// </summary>
        /// <param name="operation">The operation that failed to restore.</param>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="exception">The exception that occurred during restoration.</param>
        /// <param name="duration">The execution duration before failure.</param>
        public OperationRestoreFailedEventArgs(
            IWorkflowOperation operation,
            IWorkflowFoundry foundry,
            Exception? exception,
            TimeSpan duration)
            : base(foundry)
        {
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
            Exception = exception;
            Duration = duration;
        }
    }
}