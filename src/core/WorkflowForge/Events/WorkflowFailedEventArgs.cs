using System;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Events
{
    /// <summary>
    /// Event arguments for workflow failed events.
    /// </summary>
    public class WorkflowFailedEventArgs : BaseWorkflowForgeEventArgs
    {
        /// <summary>
        /// Gets the exception that caused the failure.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Gets the execution duration before failure.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowFailedEventArgs"/> class.
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="exception">The exception that caused the failure.</param>
        /// <param name="duration">The execution duration before failure.</param>
        public WorkflowFailedEventArgs(IWorkflowFoundry foundry, Exception exception, TimeSpan duration)
            : base(foundry)
        {
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
            Duration = duration;
        }
    }
}