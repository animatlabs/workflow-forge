using System;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Events
{
    /// <summary>
    /// Event arguments for operation restore completed events.
    /// Fired when an operation's restoration (compensation/rollback) completes successfully.
    /// </summary>
    public class OperationRestoreCompletedEventArgs : BaseWorkflowForgeEventArgs
    {
        /// <summary>
        /// Gets the operation that was restored.
        /// </summary>
        public IWorkflowOperation Operation { get; }

        /// <summary>
        /// Gets the timestamp when restoration completed.
        /// </summary>
        public DateTimeOffset CompletedAt => Timestamp;

        /// <summary>
        /// Gets the restoration duration.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationRestoreCompletedEventArgs"/> class.
        /// </summary>
        /// <param name="operation">The operation that was restored.</param>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="duration">The restoration duration.</param>
        public OperationRestoreCompletedEventArgs(
            IWorkflowOperation operation,
            IWorkflowFoundry foundry,
            TimeSpan duration)
            : base(foundry)
        {
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
            Duration = duration;
        }
    }
}