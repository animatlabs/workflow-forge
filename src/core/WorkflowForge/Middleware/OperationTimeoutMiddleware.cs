using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Constants;

namespace WorkflowForge.Middleware
{
    /// <summary>
    /// Middleware that enforces timeouts on individual operation execution.
    /// Timeout can be configured globally (constructor) or per-operation (foundry properties).
    /// </summary>
    /// <remarks>
    /// <para><strong>Timeout Configuration:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Global Default</strong>: Set via constructor parameter, applies to all operations</description></item>
    /// <item><description><strong>Per-Operation Override</strong>: Set via foundry.Properties[$"Operation.{index}:{operationName}.Timeout"], overrides global default</description></item>
    /// <item><description><strong>No Timeout</strong>: TimeSpan.Zero disables timeout enforcement</description></item>
    /// </list>
    ///
    /// <para><strong>Usage Example:</strong></para>
    /// <code>
    /// // Global 30-second timeout for all operations
    /// foundry.AddMiddleware(new OperationTimeoutMiddleware(
    ///     TimeSpan.FromSeconds(30), logger));
    ///
    /// // Override for specific operation at index 2 named "SlowOperation"
    /// foundry.Properties["Operation.2:SlowOperation.Timeout"] = TimeSpan.FromMinutes(5);
    /// </code>
    /// </remarks>
    public sealed class OperationTimeoutMiddleware : IWorkflowOperationMiddleware
    {
        private readonly TimeSpan _defaultTimeout;
        private readonly IWorkflowForgeLogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationTimeoutMiddleware"/> class.
        /// </summary>
        /// <param name="defaultTimeout">
        /// Default timeout for operation execution. TimeSpan.Zero = no timeout (default).
        /// Can be overridden per-operation via foundry.Properties[$"Operation.{index}:{operationName}.Timeout"].
        /// </param>
        /// <param name="logger">Logger for timeout events.</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
        /// <exception cref="ArgumentException">Thrown when defaultTimeout is negative.</exception>
        public OperationTimeoutMiddleware(TimeSpan defaultTimeout, IWorkflowForgeLogger logger)
        {
            if (defaultTimeout < TimeSpan.Zero)
            {
                throw new ArgumentException("Timeout cannot be negative. Use TimeSpan.Zero for no timeout.", nameof(defaultTimeout));
            }

            _defaultTimeout = defaultTimeout;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initializes a new instance with no default timeout (must be set per-operation).
        /// </summary>
        /// <param name="logger">Logger for timeout events.</param>
        public OperationTimeoutMiddleware(IWorkflowForgeLogger logger)
            : this(TimeSpan.Zero, logger)
        {
        }

        /// <inheritdoc />
        public async Task<object?> ExecuteAsync(
            IWorkflowOperation operation,
            IWorkflowFoundry foundry,
            object? inputData,
            Func<CancellationToken, Task<object?>> next,
            CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            if (foundry == null)
                throw new ArgumentNullException(nameof(foundry));
            if (next == null)
                throw new ArgumentNullException(nameof(next));

            // Check if operation has custom timeout
            TimeSpan timeout = _defaultTimeout;
            var operationIndex = foundry.Properties.TryGetValue(FoundryPropertyKeys.CurrentOperationIndex, out var indexObj) && indexObj is int idx
                ? idx
                : -1;
            if (operationIndex >= 0)
            {
                var timeoutKey = string.Format(FoundryPropertyKeys.OperationTimeoutFormat, operationIndex, operation.Name);
                if (foundry.Properties.TryGetValue(timeoutKey, out var customTimeout)
                    && customTimeout is TimeSpan ts)
                {
                    timeout = ts;
                }
            }

            // TimeSpan.Zero = no timeout enforcement
            if (timeout == TimeSpan.Zero)
            {
                return await next(cancellationToken).ConfigureAwait(false);
            }

            _logger.LogDebug("Operation {OperationName} executing with {TimeoutSeconds}s timeout", operation.Name, timeout.TotalSeconds);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var effectiveToken = timeoutCts.Token;
            var executionTask = next(effectiveToken);
            var timeoutTask = Task.Delay(timeout, cancellationToken);

            var completedTask = await Task.WhenAny(executionTask, timeoutTask).ConfigureAwait(false);
            if (completedTask == timeoutTask)
            {
                timeoutCts.Cancel();
                _ = executionTask.ContinueWith(
                    t => _ = t.Exception,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                var errorMessage = string.Format("Operation '{0}' execution exceeded the configured timeout of {1} seconds.", operation.Name, timeout.TotalSeconds);
                _logger.LogError("Operation '{OperationName}' execution exceeded the configured timeout of {TimeoutSeconds} seconds.", operation.Name, timeout.TotalSeconds);

                foundry.Properties[FoundryPropertyKeys.OperationTimedOut] = true;
                foundry.Properties[FoundryPropertyKeys.OperationTimeoutDuration] = timeout;

                throw new TimeoutException(errorMessage);
            }

            var result = await executionTask.ConfigureAwait(false);
            _logger.LogDebug("Operation {OperationName} completed within timeout ({TimeoutSeconds}s)", operation.Name, timeout.TotalSeconds);
            return result;
        }
    }
}
