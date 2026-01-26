namespace WorkflowForge.Extensions.Resilience.Polly
{
    /// <summary>
    /// Resilience-specific log messages for the Polly extension.
    /// These messages complement the core WorkflowLogMessages with resilience patterns.
    /// </summary>
    internal static class ResilienceLogMessages
    {
        #region Policy Configuration

        /// <summary>Message for resilience policy application</summary>
        public const string ResiliencePolicyApplied = "Resilience policy applied to operation";

        /// <summary>Message for comprehensive resilience policies applied</summary>
        public const string ComprehensiveResiliencePoliciesApplied = "Comprehensive resilience policies applied from settings";

        /// <summary>Message for individual resilience policies applied</summary>
        public const string IndividualResiliencePoliciesApplied = "Individual resilience policies applied from settings";

        /// <summary>Message for resilience disabled</summary>
        public const string ResilienceDisabled = "Polly resilience is disabled in settings";

        #endregion Policy Configuration

        #region Retry Messages

        /// <summary>Message for retry attempt</summary>
        public const string RetryAttemptStarted = "Retry attempt initiated due to operation failure";

        /// <summary>Message for retry policy exhausted</summary>
        public const string RetryPolicyExhausted = "Retry policy exhausted after maximum attempts";

        /// <summary>Message for retry policy successful</summary>
        public const string RetryPolicySuccessful = "Operation succeeded after retry attempts";

        #endregion Retry Messages

        #region Circuit Breaker Messages

        /// <summary>Message for circuit breaker opened</summary>
        public const string CircuitBreakerOpened = "Circuit breaker opened due to consecutive failures";

        /// <summary>Message for circuit breaker reset</summary>
        public const string CircuitBreakerReset = "Circuit breaker reset after successful test";

        /// <summary>Message for circuit breaker half-open</summary>
        public const string CircuitBreakerHalfOpen = "Circuit breaker half-open, testing operation";

        /// <summary>Message for circuit breaker rejection</summary>
        public const string CircuitBreakerRejected = "Operation rejected by open circuit breaker";

        #endregion Circuit Breaker Messages

        #region Timeout Messages

        /// <summary>Message for operation timeout</summary>
        public const string OperationTimedOut = "Operation timed out due to timeout policy";

        /// <summary>Message for timeout policy applied</summary>
        public const string TimeoutPolicyApplied = "Timeout policy applied to operation";

        #endregion Timeout Messages

        #region Policy Execution

        /// <summary>Message for policy pipeline execution started</summary>
        public const string PolicyPipelineExecutionStarted = "Polly resilience pipeline execution started";

        /// <summary>Message for policy pipeline execution completed</summary>
        public const string PolicyPipelineExecutionCompleted = "Polly resilience pipeline execution completed";

        /// <summary>Message for policy pipeline execution failed</summary>
        public const string PolicyPipelineExecutionFailed = "Polly resilience pipeline execution failed";

        #endregion Policy Execution
    }
}