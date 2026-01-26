using System;

namespace WorkflowForge.Extensions.Persistence.Recovery
{
    /// <summary>
    /// Policy settings that control retry behavior during recovery attempts.
    /// When resuming or running a fresh execution, these values define how many times to retry
    /// and how to space those retries in time.
    /// </summary>
    public sealed class RecoveryPolicy
    {
        /// <summary>
        /// Maximum number of attempts before surfacing the last error.
        /// </summary>
        public int MaxAttempts { get; set; } = 3;
        /// <summary>
        /// Base delay between attempts. When exponential backoff is enabled, this is the first delay.
        /// </summary>
        public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);
        /// <summary>
        /// Whether to use exponential backoff (doubling delay each attempt) starting from <see cref="BaseDelay"/>.
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;
    }
}


