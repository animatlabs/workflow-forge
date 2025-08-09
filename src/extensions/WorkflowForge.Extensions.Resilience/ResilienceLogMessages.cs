namespace WorkflowForge.Extensions.Resilience
{
    /// <summary>
    /// Resilience-specific log messages for the basic Resilience extension.
    /// These messages complement the core WorkflowLogMessages with resilience patterns.
    /// </summary>
    public static class ResilienceLogMessages
    {
        #region Retry Strategy Messages

        /// <summary>Message for retry strategy execution started</summary>
        public const string RetryStrategyExecutionStarted = "Retry strategy execution started";

        /// <summary>Message for retry attempt initiated</summary>
        public const string RetryAttemptInitiated = "Retry attempt initiated after operation failure";

        /// <summary>Message for retry attempt successful</summary>
        public const string RetryAttemptSuccessful = "Operation succeeded after retry attempt";

        /// <summary>Message for retry attempts exhausted</summary>
        public const string RetryAttemptsExhausted = "All retry attempts exhausted, operation failed";

        /// <summary>Message for retry delay started</summary>
        public const string RetryDelayStarted = "Retry delay initiated before next attempt";

        /// <summary>Message for retry predicate evaluation</summary>
        public const string RetryPredicateEvaluated = "Custom retry predicate evaluated for exception";

        /// <summary>Message for operation cancelled during retry</summary>
        public const string OperationCancelledDuringRetry = "Operation was cancelled, retry aborted";

        #endregion Retry Strategy Messages

        #region Circuit Breaker Messages

        /// <summary>Message for circuit breaker execution started</summary>
        public const string CircuitBreakerExecutionStarted = "Circuit breaker execution started";

        /// <summary>Message for circuit breaker execution completed</summary>
        public const string CircuitBreakerExecutionCompleted = "Circuit breaker execution completed successfully";

        /// <summary>Message for circuit breaker open rejection</summary>
        public const string CircuitBreakerOpenRejection = "Operation rejected due to open circuit breaker";

        /// <summary>Message for circuit breaker execution failed</summary>
        public const string CircuitBreakerExecutionFailed = "Operation failed in circuit breaker";

        #endregion Circuit Breaker Messages

        #region Delay Strategy Messages

        /// <summary>Message for exponential delay calculated</summary>
        public const string ExponentialDelayCalculated = "Exponential retry delay calculated";

        /// <summary>Message for fixed delay applied</summary>
        public const string FixedDelayApplied = "Fixed interval retry delay applied";

        /// <summary>Message for random delay generated</summary>
        public const string RandomDelayGenerated = "Random retry delay generated";

        /// <summary>Message for delay capped at maximum</summary>
        public const string DelayCappedAtMaximum = "Retry delay capped at maximum allowed value";

        #endregion Delay Strategy Messages

        #region Strategy Configuration

        /// <summary>Message for strategy configured</summary>
        public const string StrategyConfigured = "Resilience strategy configured successfully";

        /// <summary>Message for maximum attempts reached</summary>
        public const string MaximumAttemptsReached = "Maximum retry attempts reached, not retrying";

        /// <summary>Message for transient error detected</summary>
        public const string TransientErrorDetected = "Transient error detected, retry appropriate";

        /// <summary>Message for non-transient error detected</summary>
        public const string NonTransientErrorDetected = "Non-transient error detected, retry not appropriate";

        #endregion Strategy Configuration
    }
}