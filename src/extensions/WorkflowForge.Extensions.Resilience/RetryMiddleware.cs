using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Resilience.Abstractions;
using WorkflowForge.Extensions.Resilience.Strategies;

namespace WorkflowForge.Extensions.Resilience
{
    /// <summary>
    /// Middleware that provides retry logic for workflow operations.
    /// Uses the configured resilience strategy to handle failures and retries.
    /// </summary>
    public sealed class RetryMiddleware : IWorkflowOperationMiddleware
    {
        private readonly IWorkflowForgeLogger _logger;
        private readonly IWorkflowResilienceStrategy _retryStrategy;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryMiddleware"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for retry events.</param>
        /// <param name="retryStrategy">The retry strategy to use.</param>
        public RetryMiddleware(IWorkflowForgeLogger logger, IWorkflowResilienceStrategy retryStrategy)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _retryStrategy = retryStrategy ?? throw new ArgumentNullException(nameof(retryStrategy));
        }

        /// <inheritdoc />
        public async Task<object?> ExecuteAsync(
            IWorkflowOperation operation,
            IWorkflowFoundry foundry,
            object? inputData,
            Func<CancellationToken, Task<object?>> next,
            CancellationToken cancellationToken = default)
        {
            return await _retryStrategy.ExecuteAsync(async () =>
            {
                return await next(cancellationToken).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates retry middleware with a fixed interval strategy.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="interval">The fixed interval between retries.</param>
        /// <param name="maxAttempts">The maximum number of attempts (default: 3).</param>
        /// <param name="retryPredicate">Optional predicate to determine which exceptions to retry.</param>
        /// <returns>A new retry middleware instance.</returns>
        public static RetryMiddleware WithFixedInterval(
            IWorkflowForgeLogger logger,
            TimeSpan interval,
            int maxAttempts = 3,
            Func<Exception, bool>? retryPredicate = null)
        {
            var strategy = new FixedIntervalStrategy(interval, maxAttempts, retryPredicate, logger);
            return new RetryMiddleware(logger, strategy);
        }

        /// <summary>
        /// Creates retry middleware with an exponential backoff strategy.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="initialDelay">The initial delay before the first retry.</param>
        /// <param name="maxDelay">The maximum delay between retries.</param>
        /// <param name="maxAttempts">The maximum number of attempts (default: 3).</param>
        /// <param name="retryPredicate">Optional predicate to determine which exceptions to retry.</param>
        /// <returns>A new retry middleware instance.</returns>
        public static RetryMiddleware WithExponentialBackoff(
            IWorkflowForgeLogger logger,
            TimeSpan initialDelay,
            TimeSpan maxDelay,
            int maxAttempts = 3,
            Func<Exception, bool>? retryPredicate = null)
        {
            var strategy = new ExponentialBackoffStrategy(initialDelay, maxDelay, maxAttempts, 2.0, retryPredicate, true, logger);
            return new RetryMiddleware(logger, strategy);
        }

        /// <summary>
        /// Creates retry middleware with a random interval strategy.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="minDelay">The minimum delay between retries.</param>
        /// <param name="maxDelay">The maximum delay between retries.</param>
        /// <param name="maxAttempts">The maximum number of attempts (default: 3).</param>
        /// <param name="retryPredicate">Optional predicate to determine which exceptions to retry.</param>
        /// <returns>A new retry middleware instance.</returns>
        public static RetryMiddleware WithRandomInterval(
            IWorkflowForgeLogger logger,
            TimeSpan minDelay,
            TimeSpan maxDelay,
            int maxAttempts = 3,
            Func<Exception, bool>? retryPredicate = null)
        {
            var strategy = new RandomIntervalStrategy(maxAttempts, minDelay, maxDelay, retryPredicate, logger);
            return new RetryMiddleware(logger, strategy);
        }

        /// <summary>
        /// Creates retry middleware with default settings (3 attempts, 1 second fixed interval).
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <returns>A new retry middleware instance with default settings.</returns>
        public static RetryMiddleware Default(IWorkflowForgeLogger logger)
        {
            return WithFixedInterval(logger, TimeSpan.FromSeconds(1), 3);
        }
    }
}