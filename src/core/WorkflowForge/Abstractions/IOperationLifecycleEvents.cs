using System;
using WorkflowForge.Events;

namespace WorkflowForge.Abstractions
{
    /// <summary>
    /// Operation-level lifecycle events.
    /// </summary>
    public interface IOperationLifecycleEvents
    {
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