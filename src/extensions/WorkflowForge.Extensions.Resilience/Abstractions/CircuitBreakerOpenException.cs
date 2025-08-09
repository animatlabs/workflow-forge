using System;

namespace WorkflowForge.Extensions.Resilience.Abstractions
{
    /// <summary>
    /// Exception thrown when a circuit breaker is open.
    /// </summary>
    public sealed class CircuitBreakerOpenException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerOpenException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public CircuitBreakerOpenException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerOpenException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public CircuitBreakerOpenException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}