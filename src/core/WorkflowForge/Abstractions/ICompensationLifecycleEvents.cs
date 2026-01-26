using System;
using WorkflowForge.Events;

namespace WorkflowForge.Abstractions
{
    /// <summary>
    /// Compensation (saga pattern) lifecycle events.
    /// </summary>
    public interface ICompensationLifecycleEvents
    {
        /// <summary>
        /// Raised when compensation (rollback) is triggered due to a workflow failure.
        /// </summary>
        event EventHandler<CompensationTriggeredEventArgs>? CompensationTriggered;

        /// <summary>
        /// Raised when compensation (rollback) completes.
        /// </summary>
        event EventHandler<CompensationCompletedEventArgs>? CompensationCompleted;

        /// <summary>
        /// Raised when an operation restoration (compensation) starts.
        /// </summary>
        event EventHandler<OperationRestoreStartedEventArgs>? OperationRestoreStarted;

        /// <summary>
        /// Raised when an operation restoration (compensation) completes successfully.
        /// </summary>
        event EventHandler<OperationRestoreCompletedEventArgs>? OperationRestoreCompleted;

        /// <summary>
        /// Raised when an operation restoration (compensation) fails.
        /// </summary>
        event EventHandler<OperationRestoreFailedEventArgs>? OperationRestoreFailed;
    }
}