using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Extensions.Resilience
{
    /// <summary>
    /// Circuit breaker middleware that prevents cascading failures by temporarily blocking operations
    /// when failure rates exceed configured thresholds.
    /// </summary>
    public sealed class CircuitBreakerMiddleware : IWorkflowOperationMiddleware, IDisposable
    {
        private readonly ICircuitBreakerPolicy _policy;
        private readonly IWorkflowForgeLogger? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerMiddleware"/> class.
        /// </summary>
        /// <param name="policy">The circuit breaker policy to use.</param>
        /// <param name="logger">Optional logger for circuit breaker events.</param>
        /// <param name="name">Optional name for the middleware.</param>
        public CircuitBreakerMiddleware(ICircuitBreakerPolicy policy, IWorkflowForgeLogger? logger = null, string? name = null)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _logger = logger;
            Name = name ?? "CircuitBreaker";
        }

        /// <summary>
        /// Gets the name of the middleware for identification purposes.
        /// </summary>
        public string Name { get; }

        /// <inheritdoc />
        public async Task<object?> ExecuteAsync(
            IWorkflowOperation operation,
            IWorkflowFoundry foundry,
            object? inputData,
            Func<Task<object?>> next,
            CancellationToken cancellationToken = default)
        {
            var operationName = operation.Name;
            object? result = null;
            
            try
            {
                await _policy.ExecuteAsync(async () =>
                {
                    foundry.Logger.LogDebug("Executing operation '{OperationName}' through circuit breaker", operationName);
                    result = await next().ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);
                
                _logger?.LogDebug("Operation {OperationName} completed successfully through circuit breaker", operationName);
                return result;
            }
            catch (CircuitBreakerOpenException ex)
            {
                _logger?.LogWarning("Circuit breaker is open for operation {OperationName}: {Message}", 
                    operationName, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Operation {OperationName} failed in circuit breaker", operationName);
                throw;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _policy?.Dispose();
        }
    }
} 
