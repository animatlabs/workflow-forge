using System;
using System.Collections.Generic;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Events
{
    /// <summary>
    /// Event arguments for workflow completed events.
    /// Fired when a workflow completes successfully.
    /// </summary>
    public sealed class WorkflowCompletedEventArgs : BaseWorkflowForgeEventArgs
    {
        /// <summary>
        /// Gets the timestamp when the workflow completed.
        /// </summary>
        public DateTimeOffset CompletedAt => Timestamp;

        /// <summary>
        /// Gets the final foundry properties at completion.
        /// </summary>
        public IReadOnlyDictionary<string, object?> FinalProperties { get; }

        /// <summary>
        /// Gets the total execution duration.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowCompletedEventArgs"/> class.
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="completedAt">The completion timestamp.</param>
        /// <param name="finalProperties">The final properties from the foundry.</param>
        /// <param name="duration">The total execution duration.</param>
        public WorkflowCompletedEventArgs(
            IWorkflowFoundry foundry,
            DateTimeOffset completedAt,
            IReadOnlyDictionary<string, object?> finalProperties,
            TimeSpan duration)
            : base(foundry, completedAt)
        {
            FinalProperties = finalProperties ?? throw new ArgumentNullException(nameof(finalProperties));
            Duration = duration;
        }
    }
}