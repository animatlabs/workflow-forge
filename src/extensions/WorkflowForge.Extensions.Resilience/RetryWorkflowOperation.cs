using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Resilience.Abstractions;
using WorkflowForge.Extensions.Resilience.Configurations;
using WorkflowForge.Extensions.Resilience.Strategies;
using WorkflowForge.Operations;

namespace WorkflowForge.Extensions.Resilience
{
    /// <summary>
    /// Wraps an operation with retry logic for transient failures using resilience strategies.
    /// Essential for building resilient workflows that handle and pass through data.
    /// </summary>
    public sealed class RetryWorkflowOperation : WorkflowOperationBase
    {
        private readonly IWorkflowOperation _operation;
        private readonly IWorkflowResilienceStrategy _retryStrategy;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryWorkflowOperation"/> class.
        /// </summary>
        /// <param name="operation">The operation to retry.</param>
        /// <param name="retryStrategy">The resilience strategy to use for retry logic.</param>
        /// <param name="name">Optional name for the retry operation.</param>
        /// <param name="id">Optional operation ID.</param>
        public RetryWorkflowOperation(
            IWorkflowOperation operation,
            IWorkflowResilienceStrategy retryStrategy,
            string? name = null,
            Guid? id = null)
        {
            _operation = operation ?? throw new ArgumentNullException(nameof(operation));
            _retryStrategy = retryStrategy ?? throw new ArgumentNullException(nameof(retryStrategy));

            Id = id ?? Guid.NewGuid();
            Name = name ?? $"Retry({_operation.Name})";
        }

        /// <inheritdoc />
        public override Guid Id { get; }

        /// <inheritdoc />
        public override string Name { get; }

        /// <inheritdoc />
        protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (foundry == null)
                throw new ArgumentNullException(nameof(foundry));

            // Use the resilience strategy to execute the operation with retry logic
            return await _retryStrategy.ExecuteAsync(async () =>
            {
                // Pass input data through to the wrapped operation and return its output
                return await _operation.ForgeAsync(inputData, foundry, cancellationToken).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            // Don't retry restore operations - they should be idempotent
            // Pass the output data through for restoration
            await _operation.RestoreAsync(outputData, foundry, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    _operation?.Dispose();
                }
                catch (Exception)
                {
                    // Intentionally swallowed: disposal exceptions must not propagate
                    // to prevent cascading failures during cleanup.
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Creates a retry operation with a fixed interval strategy.
        /// </summary>
        /// <param name="operation">The operation to retry.</param>
        /// <param name="interval">The fixed interval between retries.</param>
        /// <param name="maxAttempts">Maximum number of attempts (default: 3).</param>
        /// <param name="retryPredicate">Optional predicate to determine which exceptions to retry.</param>
        /// <param name="name">Optional name for the retry operation.</param>
        /// <param name="logger">Optional logger for retry events.</param>
        /// <returns>A new retry operation instance.</returns>
        public static RetryWorkflowOperation WithFixedInterval(
            IWorkflowOperation operation,
            TimeSpan interval,
            int maxAttempts = 3,
            Func<Exception, bool>? retryPredicate = null,
            string? name = null,
            IWorkflowForgeLogger? logger = null)
        {
            var strategy = new FixedIntervalStrategy(interval, maxAttempts, retryPredicate, logger);
            return new RetryWorkflowOperation(operation, strategy, name);
        }

        /// <summary>
        /// Creates a retry operation with exponential backoff strategy.
        /// </summary>
        /// <param name="operation">The operation to retry.</param>
        /// <param name="baseDelay">Base delay for exponential backoff.</param>
        /// <param name="maxDelay">Maximum delay between retries.</param>
        /// <param name="maxAttempts">Maximum number of attempts (default: 3).</param>
        /// <param name="backoffMultiplier">Multiplier for exponential backoff (default: 2.0).</param>
        /// <param name="retryPredicate">Optional predicate to determine which exceptions to retry.</param>
        /// <param name="enableJitter">Whether to enable jitter in retry delays (default: true).</param>
        /// <param name="name">Optional name for the retry operation.</param>
        /// <param name="logger">Optional logger for retry events.</param>
        /// <returns>A new retry operation instance.</returns>
        public static RetryWorkflowOperation WithExponentialBackoff(
            IWorkflowOperation operation,
            TimeSpan? baseDelay = null,
            TimeSpan? maxDelay = null,
            int maxAttempts = 3,
            double backoffMultiplier = 2.0,
            Func<Exception, bool>? retryPredicate = null,
            bool enableJitter = true,
            string? name = null,
            IWorkflowForgeLogger? logger = null)
        {
            var delay = baseDelay ?? TimeSpan.FromSeconds(1);
            var maxDelayValue = maxDelay ?? TimeSpan.FromSeconds(30);

            var strategy = new ExponentialBackoffStrategy(
                delay, maxDelayValue, maxAttempts, backoffMultiplier,
                retryPredicate, enableJitter, logger);
            return new RetryWorkflowOperation(operation, strategy, name);
        }

        /// <summary>
        /// Creates a retry operation with random interval strategy.
        /// </summary>
        /// <param name="operation">The operation to retry.</param>
        /// <param name="minDelay">Minimum delay between retries.</param>
        /// <param name="maxDelay">Maximum delay between retries.</param>
        /// <param name="maxAttempts">Maximum number of attempts (default: 3).</param>
        /// <param name="retryPredicate">Optional predicate to determine which exceptions to retry.</param>
        /// <param name="name">Optional name for the retry operation.</param>
        /// <param name="logger">Optional logger for retry events.</param>
        /// <returns>A new retry operation instance.</returns>
        public static RetryWorkflowOperation WithRandomInterval(
            IWorkflowOperation operation,
            TimeSpan minDelay,
            TimeSpan maxDelay,
            int maxAttempts = 3,
            Func<Exception, bool>? retryPredicate = null,
            string? name = null,
            IWorkflowForgeLogger? logger = null)
        {
            var strategy = new RandomIntervalStrategy(maxAttempts, minDelay, maxDelay, retryPredicate, logger);
            return new RetryWorkflowOperation(operation, strategy, name);
        }

        /// <summary>
        /// Creates a retry operation from retry policy settings.
        /// </summary>
        /// <param name="operation">The operation to retry.</param>
        /// <param name="settings">The retry policy settings.</param>
        /// <param name="retryPredicate">Optional predicate to determine which exceptions to retry.</param>
        /// <param name="name">Optional name for the retry operation.</param>
        /// <param name="logger">Optional logger for retry events.</param>
        /// <returns>A new retry operation instance.</returns>
        public static RetryWorkflowOperation FromSettings(
            IWorkflowOperation operation,
            RetryPolicySettings settings,
            Func<Exception, bool>? retryPredicate = null,
            string? name = null,
            IWorkflowForgeLogger? logger = null)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            IWorkflowResilienceStrategy strategy = settings.StrategyType switch
            {
                RetryStrategyType.FixedInterval => new FixedIntervalStrategy(
                    settings.BaseDelay, settings.MaxAttempts, retryPredicate, logger),

                RetryStrategyType.ExponentialBackoff => new ExponentialBackoffStrategy(
                    settings.BaseDelay, settings.MaxDelay, settings.MaxAttempts,
                    settings.BackoffMultiplier, retryPredicate, settings.UseJitter, logger),

                RetryStrategyType.RandomInterval => new RandomIntervalStrategy(
                    settings.MaxAttempts, settings.BaseDelay, settings.MaxDelay, retryPredicate, logger),

                RetryStrategyType.None => throw new ArgumentException(
                    "Cannot create retry operation with RetryStrategyType.None", nameof(settings)),

                _ => throw new ArgumentException($"Unsupported retry strategy type: {settings.StrategyType}", nameof(settings))
            };

            return new RetryWorkflowOperation(operation, strategy, name);
        }

        /// <summary>
        /// Creates a retry operation with default exponential backoff settings.
        /// </summary>
        /// <param name="operation">The operation to retry.</param>
        /// <param name="maxAttempts">Maximum number of attempts (default: 3).</param>
        /// <param name="name">Optional name for the retry operation.</param>
        /// <param name="logger">Optional logger for retry events.</param>
        /// <returns>A new retry operation instance.</returns>
        public static RetryWorkflowOperation Create(
            IWorkflowOperation operation,
            int maxAttempts = 3,
            string? name = null,
            IWorkflowForgeLogger? logger = null)
        {
            return WithExponentialBackoff(operation, maxAttempts: maxAttempts, name: name, logger: logger);
        }

        /// <summary>
        /// Creates a retry operation optimized for transient errors.
        /// </summary>
        /// <param name="operation">The operation to retry.</param>
        /// <param name="maxAttempts">Maximum number of attempts (default: 3).</param>
        /// <param name="name">Optional name for the retry operation.</param>
        /// <param name="logger">Optional logger for retry events.</param>
        /// <returns>A new retry operation instance.</returns>
        public static RetryWorkflowOperation ForTransientErrors(
            IWorkflowOperation operation,
            int maxAttempts = 3,
            string? name = null,
            IWorkflowForgeLogger? logger = null)
        {
            var strategy = ExponentialBackoffStrategy.ForTransientErrors(maxAttempts, logger);
            return new RetryWorkflowOperation(operation, strategy, name);
        }
    }

    /// <summary>
    /// Exception thrown when all retry attempts have been exhausted.
    /// </summary>
    public class RetryExhaustedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RetryExhaustedException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The original exception that caused the retries.</param>
        public RetryExhaustedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
