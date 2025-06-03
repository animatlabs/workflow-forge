using System;

namespace WorkflowForge
{
    /// <summary>
    /// Defines events that can be raised during workflow execution.
    /// Provides hooks for monitoring, logging, and custom logic.
    /// </summary>
    public interface IWorkflowEvents
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

        /// <summary>
        /// Raised when an operation starts executing.
        /// </summary>
        event EventHandler<OperationStartedEventArgs>? OperationStarted;

        /// <summary>
        /// Raised when an operation completes successfully.
        /// </summary>
        event EventHandler<OperationCompletedEventArgs>? OperationCompleted;

        /// <summary>
        /// Raised when an operation fails.
        /// </summary>
        event EventHandler<OperationFailedEventArgs>? OperationFailed;
    }
} 