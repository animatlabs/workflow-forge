namespace WorkflowForge.Extensions.Resilience.Abstractions
{
    /// <summary>
    /// Represents the state of a circuit breaker.
    /// </summary>
    public enum CircuitBreakerState
    {
        /// <summary>
        /// The circuit breaker is closed and operations are allowed.
        /// </summary>
        Closed,

        /// <summary>
        /// The circuit breaker is open and operations are blocked.
        /// </summary>
        Open,

        /// <summary>
        /// The circuit breaker is half-open and testing if operations should be allowed.
        /// </summary>
        HalfOpen
    }
}