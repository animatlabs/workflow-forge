using System;

namespace WorkflowForge.Abstractions
{
    /// <summary>
    /// Base class for all WorkflowForge event arguments.
    /// Provides common properties shared across workflow and operation events.
    /// </summary>
    public abstract class BaseWorkflowForgeEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the workflow foundry for the execution context.
        /// </summary>
        public IWorkflowFoundry Foundry { get; }

        /// <summary>
        /// Gets the timestamp when the event occurred.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseWorkflowForgeEventArgs"/> class.
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="timestamp">The timestamp when the event occurred. If not provided, uses current UTC time.</param>
        protected BaseWorkflowForgeEventArgs(IWorkflowFoundry foundry, DateTimeOffset? timestamp = null)
        {
            Foundry = foundry ?? throw new ArgumentNullException(nameof(foundry));
            Timestamp = timestamp ?? DateTimeOffset.UtcNow;
        }
    }
}