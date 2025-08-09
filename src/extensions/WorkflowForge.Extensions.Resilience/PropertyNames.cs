namespace WorkflowForge.Extensions.Resilience
{
    /// <summary>
    /// Property names specific to basic resilience patterns for structured logging.
    /// These properties extend the core PropertyNames with resilience-specific context.
    /// </summary>
    public static class ResiliencePropertyNames
    {
        #region Retry Strategy Properties

        /// <summary>Current attempt number</summary>
        public const string AttemptNumber = "AttemptNumber";

        /// <summary>Maximum number of attempts allowed</summary>
        public const string MaxAttempts = "MaxAttempts";

        /// <summary>Retry strategy name</summary>
        public const string StrategyName = "StrategyName";

        /// <summary>Retry delay in milliseconds</summary>
        public const string RetryDelayMs = "RetryDelayMs";

        /// <summary>Indicates if retry predicate was used</summary>
        public const string CustomPredicateUsed = "CustomPredicateUsed";

        /// <summary>Backoff multiplier for exponential strategies</summary>
        public const string BackoffMultiplier = "BackoffMultiplier";

        #endregion Retry Strategy Properties

        #region Circuit Breaker Properties

        /// <summary>Circuit breaker state (Closed, Open, HalfOpen)</summary>
        public const string CircuitState = "CircuitState";

        /// <summary>Failure threshold for opening circuit</summary>
        public const string FailureThreshold = "FailureThreshold";

        /// <summary>Success threshold for closing circuit</summary>
        public const string SuccessThreshold = "SuccessThreshold";

        /// <summary>Circuit break duration in milliseconds</summary>
        public const string BreakDurationMs = "BreakDurationMs";

        /// <summary>Current failure count</summary>
        public const string FailureCount = "FailureCount";

        /// <summary>Current success count</summary>
        public const string SuccessCount = "SuccessCount";

        #endregion Circuit Breaker Properties

        #region Strategy Execution

        /// <summary>Indicates if operation is being retried</summary>
        public const string IsRetry = "IsRetry";

        /// <summary>Indicates if all attempts were exhausted</summary>
        public const string AttemptsExhausted = "AttemptsExhausted";

        /// <summary>Indicates if operation was cancelled</summary>
        public const string WasCancelled = "WasCancelled";

        /// <summary>Final execution result (Success, Failed, Cancelled)</summary>
        public const string ExecutionResult = "ExecutionResult";

        #endregion Strategy Execution

        #region Delay Calculations

        /// <summary>Base delay for retry calculations in milliseconds</summary>
        public const string BaseDelayMs = "BaseDelayMs";

        /// <summary>Maximum delay allowed in milliseconds</summary>
        public const string MaxDelayMs = "MaxDelayMs";

        /// <summary>Calculated exponential delay in milliseconds</summary>
        public const string ExponentialDelayMs = "ExponentialDelayMs";

        /// <summary>Indicates if delay was capped at maximum</summary>
        public const string DelayCapped = "DelayCapped";

        /// <summary>Random delay range minimum in milliseconds</summary>
        public const string RandomDelayMinMs = "RandomDelayMinMs";

        /// <summary>Random delay range maximum in milliseconds</summary>
        public const string RandomDelayMaxMs = "RandomDelayMaxMs";

        #endregion Delay Calculations
    }
}