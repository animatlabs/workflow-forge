using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Resilience.Abstractions;

namespace WorkflowForge.Extensions.Resilience.Strategies
{
    /// <summary>
    /// Implements an exponential backoff resilience strategy with configurable base delay, multiplier, and jitter.
    /// </summary>
    public sealed class ExponentialBackoffStrategy : ResilienceStrategyBase
    {
        private readonly TimeSpan _baseDelay;
        private readonly TimeSpan _maxDelay;
        private readonly double _backoffMultiplier;
        private readonly bool _enableJitter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExponentialBackoffStrategy"/> class.
        /// </summary>
        /// <param name="baseDelay">The base delay for the first retry attempt.</param>
        /// <param name="maxDelay">The maximum delay between retry attempts.</param>
        /// <param name="maxAttempts">The maximum number of attempts (including the initial attempt).</param>
        /// <param name="backoffMultiplier">The multiplier for the exponential backoff (default: 2.0).</param>
        /// <param name="retryPredicate">Optional predicate to determine if an exception should be retried.</param>
        /// <param name="enableJitter">Whether to enable jitter in the retry delay.</param>
        /// <param name="logger">Optional logger for the strategy.</param>
        public ExponentialBackoffStrategy(
            TimeSpan baseDelay,
            TimeSpan maxDelay,
            int maxAttempts,
            double backoffMultiplier = 2.0,
            Func<Exception, bool>? retryPredicate = null,
            bool enableJitter = true,
            IWorkflowForgeLogger? logger = null)
            : base("ExponentialBackoff", maxAttempts, retryPredicate, logger)
        {
            if (baseDelay < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(baseDelay), "Base delay cannot be negative");
            if (maxDelay < baseDelay)
                throw new ArgumentOutOfRangeException(nameof(maxDelay), "Max delay cannot be less than base delay");
            if (maxAttempts < 1)
                throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Max attempts must be at least 1");
            if (backoffMultiplier <= 1.0)
                throw new ArgumentOutOfRangeException(nameof(backoffMultiplier), "Backoff multiplier must be greater than 1.0");

            _baseDelay = baseDelay;
            _maxDelay = maxDelay;
            _backoffMultiplier = backoffMultiplier;
            _enableJitter = enableJitter;
        }

        /// <inheritdoc />
        public override TimeSpan GetRetryDelay(int attemptNumber, Exception? exception)
        {
            if (attemptNumber <= 1)
                return TimeSpan.Zero;

            // Calculate exponential delay: baseDelay * (multiplier ^ (attemptNumber - 2))
            // attemptNumber - 2 because we want first retry to use base delay
            var retryNumber = attemptNumber - 1;
            var exponentialDelay = TimeSpan.FromMilliseconds(
                _baseDelay.TotalMilliseconds * Math.Pow(_backoffMultiplier, retryNumber - 1));

            // Cap at max delay
            var actualDelay = exponentialDelay > _maxDelay ? _maxDelay : exponentialDelay;

            Logger?.LogDebug("Calculated retry delay for attempt {AttemptNumber}: {DelayMs}ms (exponential: {ExponentialMs}ms, capped: {IsCapped})",
                attemptNumber, actualDelay.TotalMilliseconds, exponentialDelay.TotalMilliseconds, actualDelay == _maxDelay);

            return actualDelay;
        }

        /// <summary>
        /// Creates a default exponential backoff strategy with sensible defaults.
        /// </summary>
        /// <param name="maxAttempts">Maximum number of retry attempts.</param>
        /// <param name="logger">Optional logger for strategy events.</param>
        /// <returns>A new exponential backoff strategy.</returns>
        public static ExponentialBackoffStrategy Default(int maxAttempts = 3, IWorkflowForgeLogger? logger = null)
        {
            return new ExponentialBackoffStrategy(
                baseDelay: TimeSpan.FromMilliseconds(100),
                maxDelay: TimeSpan.FromSeconds(30),
                maxAttempts: maxAttempts,
                logger: logger);
        }

        /// <summary>
        /// Creates an exponential backoff strategy optimized for transient errors.
        /// </summary>
        /// <param name="maxAttempts">Maximum number of retry attempts.</param>
        /// <param name="logger">Optional logger for strategy events.</param>
        /// <returns>A new exponential backoff strategy.</returns>
        public static ExponentialBackoffStrategy ForTransientErrors(int maxAttempts = 3, IWorkflowForgeLogger? logger = null)
        {
            return new ExponentialBackoffStrategy(
                baseDelay: TimeSpan.FromMilliseconds(200),
                maxDelay: TimeSpan.FromSeconds(60),
                maxAttempts: maxAttempts,
                retryPredicate: IsTransientError,
                logger: logger);
        }

        /// <summary>
        /// Determines if an exception represents a transient error that should be retried.
        /// </summary>
        /// <param name="exception">The exception to evaluate.</param>
        /// <returns>True if the exception represents a transient error; otherwise, false.</returns>
        private static bool IsTransientError(Exception exception)
        {
            // Consider common transient error types
            return exception switch
            {
                TimeoutException => true,
                InvalidOperationException when exception.Message.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0 => true,
                InvalidOperationException when exception.Message.IndexOf("network", StringComparison.OrdinalIgnoreCase) >= 0 => true,
                _ => false
            };
        }

        /// <inheritdoc />
        public override Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
        {
            return ExecuteWithRetryAsync(operation, cancellationToken);
        }
    }
}