using System;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Events
{
    /// <summary>
    /// Event arguments for operation completed events.
    /// Fired when an operation completes successfully.
    /// </summary>
    public sealed class OperationCompletedEventArgs : BaseWorkflowForgeEventArgs
    {
        /// <summary>
        /// Gets the operation that completed.
        /// </summary>
        public IWorkflowOperation Operation { get; }

        /// <summary>
        /// Gets the input data for the operation.
        /// </summary>
        public object? InputData { get; }

        /// <summary>
        /// Gets the output data from the operation.
        /// </summary>
        public object? OutputData { get; }

        /// <summary>
        /// Gets the execution duration for this operation.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationCompletedEventArgs"/> class.
        /// </summary>
        /// <param name="operation">The operation that completed.</param>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="inputData">The input data for the operation.</param>
        /// <param name="outputData">The output data from the operation.</param>
        /// <param name="duration">The execution duration.</param>
        public OperationCompletedEventArgs(
            IWorkflowOperation operation,
            IWorkflowFoundry foundry,
            object? inputData,
            object? outputData,
            TimeSpan duration)
            : base(foundry)
        {
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
            InputData = inputData;
            OutputData = outputData;
            Duration = duration;
        }
    }
}