using System;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Events
{
    /// <summary>
    /// Event arguments for operation restore started events.
    /// Fired when an operation's restoration (compensation/rollback) begins.
    /// </summary>
    public class OperationRestoreStartedEventArgs : BaseWorkflowForgeEventArgs
    {
        /// <summary>
        /// Gets the operation being restored.
        /// </summary>
        public IWorkflowOperation Operation { get; }

        /// <summary>
        /// Gets the timestamp when restoration started.
        /// </summary>
        public DateTimeOffset StartedAt => Timestamp;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationRestoreStartedEventArgs"/> class.
        /// </summary>
        /// <param name="operation">The operation being restored.</param>
        /// <param name="foundry">The workflow foundry.</param>
        public OperationRestoreStartedEventArgs(IWorkflowOperation operation, IWorkflowFoundry foundry)
            : base(foundry)
        {
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
        }
    }
}