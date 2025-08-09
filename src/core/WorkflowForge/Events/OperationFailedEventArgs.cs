using System;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Events
{
    /// <summary>
    /// Event arguments for operation failed events.
    /// </summary>
    public class OperationFailedEventArgs : BaseWorkflowForgeEventArgs
    {
        /// <summary>
        /// Gets the operation that failed.
        /// </summary>
        public IWorkflowOperation Operation { get; }

        /// <summary>
        /// Gets the exception that caused the failure.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Gets the execution duration before failure.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationFailedEventArgs"/> class.
        /// </summary>
        /// <param name="operation">The operation that failed.</param>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="exception">The exception that caused the failure.</param>
        /// <param name="duration">The execution duration before failure.</param>
        public OperationFailedEventArgs(IWorkflowOperation operation, IWorkflowFoundry foundry, Exception exception, TimeSpan duration)
            : base(foundry)
        {
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
            Duration = duration;
        }
    }
}