using System;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Extensions.Resilience.Abstractions
{
    /// <summary>
    /// Event arguments for circuit breaker state changes.
    /// </summary>
    public sealed class CircuitBreakerStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerStateChangedEventArgs"/> class.
        /// </summary>
        /// <param name="previousState">The previous state.</param>
        /// <param name="currentState">The current state.</param>
        /// <param name="reason">The reason for the state change.</param>
        /// <param name="timeProvider">Optional time provider for timestamp.</param>
        public CircuitBreakerStateChangedEventArgs(CircuitBreakerState previousState, CircuitBreakerState currentState, string reason, ISystemTimeProvider? timeProvider = null)
        {
            PreviousState = previousState;
            CurrentState = currentState;
            Reason = reason;
            Timestamp = (timeProvider ?? SystemTimeProvider.Instance).UtcNow;
        }

        /// <summary>
        /// Gets the previous state.
        /// </summary>
        public CircuitBreakerState PreviousState { get; }

        /// <summary>
        /// Gets the current state.
        /// </summary>
        public CircuitBreakerState CurrentState { get; }

        /// <summary>
        /// Gets the reason for the state change.
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// Gets the timestamp of the state change.
        /// </summary>
        public DateTimeOffset Timestamp { get; }
    }
}