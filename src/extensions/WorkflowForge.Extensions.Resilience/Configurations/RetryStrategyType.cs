namespace WorkflowForge.Extensions.Resilience.Configurations
{
    /// <summary>
    /// Defines the available retry strategy types for WorkflowForge operations.
    /// </summary>
    public enum RetryStrategyType
    {
        /// <summary>
        /// No retry attempts will be made.
        /// </summary>
        None = 0,

        /// <summary>
        /// Retry with a fixed interval between attempts.
        /// </summary>
        FixedInterval = 1,

        /// <summary>
        /// Retry with exponentially increasing intervals between attempts.
        /// </summary>
        ExponentialBackoff = 2,

        /// <summary>
        /// Retry with random intervals within a specified range.
        /// </summary>
        RandomInterval = 3
    }
}