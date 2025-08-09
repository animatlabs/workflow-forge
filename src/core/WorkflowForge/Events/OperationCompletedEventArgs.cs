using System;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Events
{
    /// <summary>
    /// Event arguments for operation completed events.
    /// </summary>
    public class OperationCompletedEventArgs : BaseWorkflowForgeEventArgs
    {
        /// <summary>
        /// Gets the operation that completed.
        /// </summary>
        public IWorkflowOperation Operation { get; }

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
            : base(foundry)
        {
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
            OutputData = outputData;
            Duration = duration;
        }
    }
}