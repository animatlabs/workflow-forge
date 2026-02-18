using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Resilience.Abstractions;

namespace WorkflowForge.Extensions.Resilience.Strategies
{
    /// <summary>
    /// Implements a resilience strategy with random intervals between retry attempts to reduce thundering herd problems.
    /// </summary>
    public sealed class RandomIntervalStrategy : ResilienceStrategyBase
    {
        private readonly TimeSpan _minInterval;
        private readonly TimeSpan _maxInterval;
        private readonly int _maxAttempts;
        private readonly Func<Exception, bool>? _retryPredicate;
        private readonly Random _random;

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomIntervalStrategy"/> class.
        /// </summary>
        /// <param name="maxAttempts">The maximum number of attempts (including the initial attempt).</param>
        /// <param name="minInterval">The minimum interval between retry attempts.</param>
        /// <param name="maxInterval">The maximum interval between retry attempts.</param>
        /// <param name="retryPredicate">Optional predicate to determine if an exception should be retried.</param>
        /// <param name="logger">Optional logger for the strategy.</param>
        public RandomIntervalStrategy(
            int maxAttempts,
            TimeSpan minInterval,
            TimeSpan maxInterval,
            Func<Exception, bool>? retryPredicate = null,
            IWorkflowForgeLogger? logger = null)
            : base($"RandomInterval(min:{minInterval.TotalMilliseconds}ms,max:{maxInterval.TotalMilliseconds}ms,attempts:{maxAttempts})", logger)
        {
            if (minInterval < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(minInterval), "Minimum interval cannot be negative");
            if (maxInterval < minInterval)
                throw new ArgumentOutOfRangeException(nameof(maxInterval), "Maximum interval cannot be less than minimum interval");
            if (maxAttempts < 1)
                throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Max attempts must be at least 1");

            _minInterval = minInterval;
            _maxInterval = maxInterval;
            _maxAttempts = maxAttempts;
            _retryPredicate = retryPredicate;

            // Use thread-safe random with different seed per instance
            _random = new Random(Environment.TickCount + GetHashCode());
        }

        /// <inheritdoc />
        public override Task<bool> ShouldRetryAsync(int attemptNumber, Exception? exception, CancellationToken cancellationToken)
        {
            // Don't retry if we've exceeded max attempts
            if (attemptNumber >= _maxAttempts)
            {
                Logger?.LogDebug("Max attempts ({MaxAttempts}) reached, not retrying", _maxAttempts);
                return Task.FromResult(false);
            }

            // Don't retry cancellation
            if (exception is OperationCanceledException)
            {
                Logger?.LogDebug("Operation was cancelled, not retrying");
                return Task.FromResult(false);
            }

            // Use custom predicate if provided
            if (_retryPredicate != null && exception != null)
            {
                bool shouldRetry = _retryPredicate(exception);
                Logger?.LogDebug("Custom retry predicate returned {ShouldRetry} for exception {ExceptionType}",
                    shouldRetry, exception.GetType().Name);
                return Task.FromResult(shouldRetry);
            }

            // By default, retry all exceptions except cancellation
            return Task.FromResult(true);
        }

        /// <inheritdoc />
        public override TimeSpan GetRetryDelay(int attemptNumber, Exception? exception)
        {
            if (attemptNumber <= 1)
                return TimeSpan.Zero;

            // Generate random delay between min and max intervals
            var minMs = _minInterval.TotalMilliseconds;
            var maxMs = _maxInterval.TotalMilliseconds;
            var randomDelayMs = minMs + (_random.NextDouble() * (maxMs - minMs));
            var delay = TimeSpan.FromMilliseconds(randomDelayMs);

            Logger?.LogDebug("Generated random retry delay of {DelayMs}ms for attempt {AttemptNumber} (range: {MinMs}ms-{MaxMs}ms)",
                delay.TotalMilliseconds, attemptNumber, minMs, maxMs);

            return delay;
        }

        /// <inheritdoc />
        public override async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            var attemptNumber = 0;

            while (true)
            {
                attemptNumber++;
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await operation().ConfigureAwait(false);
                    return; // Success
                }
                catch (Exception ex)
                {
                    var shouldRetry = await ShouldRetryAsync(attemptNumber, ex, cancellationToken).ConfigureAwait(false);
                    if (!shouldRetry)
                    {
                        Logger?.LogError(ex, $"Attempt {attemptNumber} failed and will not be retried");
                        throw;
                    }

                    var delay = GetRetryDelay(attemptNumber, ex);

                    Logger?.LogWarning($"Attempt {attemptNumber} failed, retrying in {delay.TotalMilliseconds}ms. Error: {ex.Message}");

                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a default random interval strategy with reasonable defaults.
        /// </summary>
        /// <param name="maxAttempts">Maximum number of retry attempts.</param>
        /// <param name="logger">Optional logger for strategy events.</param>
        /// <returns>A new random interval strategy.</returns>
        public static RandomIntervalStrategy Default(int maxAttempts = 3, IWorkflowForgeLogger? logger = null)
        {
            return new RandomIntervalStrategy(
                maxAttempts: maxAttempts,
                minInterval: TimeSpan.FromMilliseconds(100),
                maxInterval: TimeSpan.FromMilliseconds(1000),
                logger: logger);
        }

        /// <summary>
        /// Creates a random interval strategy optimized for high-throughput scenarios.
        /// </summary>
        /// <param name="maxAttempts">Maximum number of retry attempts.</param>
        /// <param name="logger">Optional logger for strategy events.</param>
        /// <returns>A new random interval strategy.</returns>
        public static RandomIntervalStrategy HighThroughput(
            int maxAttempts = 5,
            IWorkflowForgeLogger? logger = null)
        {
            return new RandomIntervalStrategy(
                maxAttempts: maxAttempts,
                minInterval: TimeSpan.FromMilliseconds(100),
                maxInterval: TimeSpan.FromMilliseconds(1000),
                logger: logger);
        }

        /// <summary>
        /// Creates a random interval strategy with jitter for exponential backoff scenarios.
        /// </summary>
        /// <param name="baseInterval">The base interval for jitter calculation.</param>
        /// <param name="jitterPercent">The percentage of jitter to apply (0.0 to 1.0).</param>
        /// <param name="maxAttempts">The maximum number of attempts (default: 3).</param>
        /// <param name="logger">Optional logger for the strategy.</param>
        /// <returns>A new random interval strategy instance.</returns>
        public static RandomIntervalStrategy WithJitter(
            TimeSpan baseInterval,
            double jitterPercent = 0.2,
            int maxAttempts = 3,
            IWorkflowForgeLogger? logger = null)
        {
            if (jitterPercent < 0.0 || jitterPercent > 1.0)
                throw new ArgumentOutOfRangeException(nameof(jitterPercent), "Jitter percent must be between 0.0 and 1.0");

            var jitterAmount = TimeSpan.FromMilliseconds(baseInterval.TotalMilliseconds * jitterPercent);
            var minInterval = baseInterval - jitterAmount;
            var maxInterval = baseInterval + jitterAmount;

            // Ensure we don't go below zero
            if (minInterval < TimeSpan.Zero)
                minInterval = TimeSpan.Zero;

            return new RandomIntervalStrategy(
                maxAttempts: maxAttempts,
                minInterval: minInterval,
                maxInterval: maxInterval,
                retryPredicate: null,
                logger: logger);
        }

        /// <summary>
        /// Creates a random interval strategy that reduces thundering herd effects
        /// by introducing random delays between min and max values.
        /// </summary>
        /// <param name="minInterval">Minimum interval between operations.</param>
        /// <param name="maxInterval">Maximum interval between operations.</param>
        /// <returns>A random interval strategy.</returns>
        public static RandomIntervalStrategy Create(TimeSpan minInterval, TimeSpan maxInterval)
        {
            return new RandomIntervalStrategy(maxAttempts: 3, minInterval, maxInterval, retryPredicate: null, logger: null);
        }
    }
}