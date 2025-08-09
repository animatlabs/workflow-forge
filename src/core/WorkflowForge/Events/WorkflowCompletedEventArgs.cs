using System;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Events
{
    /// <summary>
    /// Event arguments for workflow completed events.
    /// </summary>
    public class WorkflowCompletedEventArgs : BaseWorkflowForgeEventArgs
    {
        /// <summary>
        /// Gets the execution duration.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Gets the final result data.
        /// </summary>
        public object? ResultData { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowCompletedEventArgs"/> class.
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="duration">The execution duration.</param>
        /// <param name="resultData">The final result data.</param>
        public WorkflowCompletedEventArgs(IWorkflowFoundry foundry, TimeSpan duration, object? resultData)
            : base(foundry)
        {
            Duration = duration;
            ResultData = resultData;
        }
    }
}