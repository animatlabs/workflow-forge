using System;
using WorkflowForge.Events;

namespace WorkflowForge.Abstractions
{
    /// <summary>
    /// Workflow-level lifecycle events.
    /// </summary>
    public interface IWorkflowLifecycleEvents
    {
        /// <summary>
        /// Raised when a workflow execution starts.
        /// </summary>
        event EventHandler<WorkflowStartedEventArgs>? WorkflowStarted;

        /// <summary>
        /// Raised when a workflow execution completes successfully.
        /// </summary>
        event EventHandler<WorkflowCompletedEventArgs>? WorkflowCompleted;

        /// <summary>
        /// Raised when a workflow execution fails.
        /// </summary>
        event EventHandler<WorkflowFailedEventArgs>? WorkflowFailed;
    }
}