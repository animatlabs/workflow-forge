using System;

namespace WorkflowForge
{
    /// <summary>
    /// Event arguments for operation started events.
    /// </summary>
    public class OperationStartedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the operation that started.
        /// </summary>
        public IWorkflowOperation Operation { get; }

        /// <summary>
        /// Gets the workflow foundry for the execution.
        /// </summary>
        public IWorkflowFoundry Foundry { get; }

        /// <summary>
        /// Gets the input data for the operation.
        /// </summary>
        public object? InputData { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationStartedEventArgs"/> class.
        /// </summary>
        /// <param name="operation">The operation that started.</param>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="inputData">The input data for the operation.</param>
        public OperationStartedEventArgs(IWorkflowOperation operation, IWorkflowFoundry foundry, object? inputData)
        {
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
            Foundry = foundry ?? throw new ArgumentNullException(nameof(foundry));
            InputData = inputData;
        }
    }

    /// <summary>
    /// Event arguments for operation completed events.
    /// </summary>
    public class OperationCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the operation that completed.
        /// </summary>
        public IWorkflowOperation Operation { get; }

        /// <summary>
        /// Gets the workflow foundry for the execution.
        /// </summary>
        public IWorkflowFoundry Foundry { get; }

        /// <summary>
        /// Gets the output data from the operation.
        /// </summary>
        public object? OutputData { get; }

        /// <summary>
        /// Gets the execution duration.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationCompletedEventArgs"/> class.
        /// </summary>
        /// <param name="operation">The operation that completed.</param>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="outputData">The output data from the operation.</param>
        /// <param name="duration">The execution duration.</param>
        public OperationCompletedEventArgs(IWorkflowOperation operation, IWorkflowFoundry foundry, object? outputData, TimeSpan duration)
        {
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
            Foundry = foundry ?? throw new ArgumentNullException(nameof(foundry));
            OutputData = outputData;
            Duration = duration;
        }
    }

    /// <summary>
    /// Event arguments for operation failed events.
    /// </summary>
    public class OperationFailedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the operation that failed.
        /// </summary>
        public IWorkflowOperation Operation { get; }

        /// <summary>
        /// Gets the workflow foundry for the execution.
        /// </summary>
        public IWorkflowFoundry Foundry { get; }

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
        {
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
            Foundry = foundry ?? throw new ArgumentNullException(nameof(foundry));
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
            Duration = duration;
        }
    }
} 