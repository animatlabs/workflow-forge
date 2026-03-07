using System;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Events
{
    /// <summary>
    /// Event arguments for operation started events.
    /// </summary>
    public sealed class OperationStartedEventArgs : BaseWorkflowForgeEventArgs
    {
        /// <summary>
        /// Gets the operation that started.
        /// </summary>
        public IWorkflowOperation Operation { get; }

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
            : base(foundry)
        {
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
            InputData = inputData;
        }
    }
}