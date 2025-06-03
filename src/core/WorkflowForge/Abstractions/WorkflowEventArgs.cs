using System;

namespace WorkflowForge
{
    /// <summary>
    /// Event arguments for workflow started events.
    /// </summary>
    public class WorkflowStartedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the workflow foundry for the execution.
        /// </summary>
        public IWorkflowFoundry Foundry { get; }

        /// <summary>
        /// Gets the timestamp when the workflow started.
        /// </summary>
        public DateTimeOffset StartedAt { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowStartedEventArgs"/> class.
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="startedAt">The start timestamp.</param>
        public WorkflowStartedEventArgs(IWorkflowFoundry foundry, DateTimeOffset startedAt)
        {
            Foundry = foundry ?? throw new ArgumentNullException(nameof(foundry));
            StartedAt = startedAt;
        }
    }

    /// <summary>
    /// Event arguments for workflow completed events.
    /// </summary>
    public class WorkflowCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the workflow foundry for the execution.
        /// </summary>
        public IWorkflowFoundry Foundry { get; }

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
        {
            Foundry = foundry ?? throw new ArgumentNullException(nameof(foundry));
            Duration = duration;
            ResultData = resultData;
        }
    }

    /// <summary>
    /// Event arguments for workflow failed events.
    /// </summary>
    public class WorkflowFailedEventArgs : EventArgs
    {
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
        /// Initializes a new instance of the <see cref="WorkflowFailedEventArgs"/> class.
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="exception">The exception that caused the failure.</param>
        /// <param name="duration">The execution duration before failure.</param>
        public WorkflowFailedEventArgs(IWorkflowFoundry foundry, Exception exception, TimeSpan duration)
        {
            Foundry = foundry ?? throw new ArgumentNullException(nameof(foundry));
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
            Duration = duration;
        }
    }
} 