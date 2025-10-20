using System;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Events
{
    /// <summary>
    /// Event arguments for operation failed events.
    /// Fired when an operation fails with an exception.
    /// </summary>
    public class OperationFailedEventArgs : BaseWorkflowForgeEventArgs
    {
        /// <summary>
        /// Gets the operation that failed.
        /// </summary>
        public IWorkflowOperation Operation { get; }

        /// <summary>
        /// Gets the input data for the operation.
        /// </summary>
        public object? InputData { get; }

        /// <summary>
        /// Gets the exception that occurred during operation execution.
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Gets the execution duration before failure.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationFailedEventArgs"/> class.
        /// </summary>
        /// <param name="operation">The operation that failed.</param>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="inputData">The input data for the operation.</param>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="duration">The execution duration before failure.</param>
        public OperationFailedEventArgs(
            IWorkflowOperation operation,
            IWorkflowFoundry foundry,
            object? inputData,
            Exception? exception,
            TimeSpan duration)
            : base(foundry)
        {
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
            InputData = inputData;
            Exception = exception;
            Duration = duration;
        }
    }
}