using System;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Events
{
    /// <summary>
    /// Event arguments for compensation completed events.
    /// Fired when compensation (rollback) completes, successfully or with failures.
    /// </summary>
    public sealed class CompensationCompletedEventArgs : BaseWorkflowForgeEventArgs
    {
        /// <summary>
        /// Gets the timestamp when compensation completed.
        /// </summary>
        public DateTimeOffset CompletedAt => Timestamp;

        /// <summary>
        /// Gets the number of operations successfully compensated.
        /// </summary>
        public int SuccessCount { get; }

        /// <summary>
        /// Gets the number of compensation operations that failed.
        /// </summary>
        public int FailureCount { get; }

        /// <summary>
        /// Gets the total compensation duration.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompensationCompletedEventArgs"/> class.
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="completedAt">The completion timestamp.</param>
        /// <param name="successCount">The number of successful compensations.</param>
        /// <param name="failureCount">The number of failed compensations.</param>
        /// <param name="duration">The total compensation duration.</param>
        public CompensationCompletedEventArgs(
            IWorkflowFoundry foundry,
            DateTimeOffset completedAt,
            int successCount,
            int failureCount,
            TimeSpan duration)
            : base(foundry, completedAt)
        {
            SuccessCount = successCount;
            FailureCount = failureCount;
            Duration = duration;
        }
    }
}