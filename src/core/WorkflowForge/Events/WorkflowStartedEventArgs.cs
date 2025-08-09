using System;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Events
{
    /// <summary>
    /// Event arguments for workflow started events.
    /// </summary>
    public class WorkflowStartedEventArgs : BaseWorkflowForgeEventArgs
    {
        /// <summary>
        /// Gets the timestamp when the workflow started.
        /// </summary>
        public DateTimeOffset StartedAt => Timestamp;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowStartedEventArgs"/> class.
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="startedAt">The start timestamp.</param>
        public WorkflowStartedEventArgs(IWorkflowFoundry foundry, DateTimeOffset startedAt)
            : base(foundry, startedAt)
        {
        }
    }
}