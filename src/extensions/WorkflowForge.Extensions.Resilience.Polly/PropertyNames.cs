namespace WorkflowForge.Extensions.Resilience.Polly
{
    /// <summary>
    /// Property names specific to Polly resilience patterns for structured logging.
    /// These properties extend the core PropertyNames with resilience-specific context.
    /// </summary>
    public static class ResiliencePropertyNames
    {
        #region Retry Properties

        /// <summary>Current retry attempt number</summary>
        public const string RetryAttempt = "RetryAttempt";

        /// <summary>Maximum retry attempts allowed</summary>
        public const string MaxRetryAttempts = "MaxRetryAttempts";

        /// <summary>Delay before next retry in milliseconds</summary>
        public const string RetryDelayMs = "RetryDelayMs";

        /// <summary>Reason for retry attempt</summary>
        public const string RetryReason = "RetryReason";

        #endregion Retry Properties

        #region Circuit Breaker Properties

        /// <summary>Circuit breaker current state (Closed, Open, HalfOpen)</summary>
        public const string CircuitState = "CircuitState";

        /// <summary>Circuit breaker failure threshold</summary>
        public const string FailureThreshold = "FailureThreshold";

        /// <summary>Circuit breaker break duration in milliseconds</summary>
        public const string BreakDurationMs = "BreakDurationMs";

        /// <summary>Number of consecutive failures</summary>
        public const string ConsecutiveFailures = "ConsecutiveFailures";

        #endregion Circuit Breaker Properties

        #region Timeout Properties

        /// <summary>Timeout duration in milliseconds</summary>
        public const string TimeoutMs = "TimeoutMs";

        /// <summary>Indicates if operation was cancelled due to timeout</summary>
        public const string TimedOut = "TimedOut";

        #endregion Timeout Properties

        #region Policy Properties

        /// <summary>Name of the Polly policy being applied</summary>
        public const string PolicyName = "PolicyName";

        /// <summary>Type of resilience policy (Retry, CircuitBreaker, Timeout, Comprehensive)</summary>
        public const string PolicyType = "PolicyType";

        /// <summary>Number of policies in the pipeline</summary>
        public const string PolicyCount = "PolicyCount";

        #endregion Policy Properties
    }
}