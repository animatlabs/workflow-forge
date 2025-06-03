using System;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowForge.Extensions.Resilience
{
    /// <summary>
    /// Interface for circuit breaker policies.
    /// </summary>
    public interface ICircuitBreakerPolicy : IDisposable
    {
        /// <summary>
        /// Gets the current state of the circuit breaker.
        /// </summary>
        CircuitBreakerState State { get; }

        /// <summary>
        /// Executes the specified operation through the circuit breaker.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="CircuitBreakerOpenException">Thrown when the circuit breaker is open.</exception>
        Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default);

        /// <summary>
        /// Occurs when the circuit breaker state changes.
        /// </summary>
        event EventHandler<CircuitBreakerStateChangedEventArgs>? StateChanged;
    }
} 
