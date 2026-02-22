using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Resilience.Abstractions;

namespace WorkflowForge.Extensions.Resilience.Strategies
{
    /// <summary>
    /// Implements a resilience strategy with random intervals between retry attempts to reduce thundering herd problems.
    /// </summary>
    [SuppressMessage("Security", "S2245:Random is used for non-security-sensitive retry jitter", Justification = "System.Random is intentionally used for retry delay jitter; cryptographic randomness is not required")]
    public sealed class RandomIntervalStrategy : ResilienceStrategyBase
    {
        private readonly TimeSpan _minInterval;
        private readonly TimeSpan _maxInterval;
        private static readonly Random SeedSource = new Random();
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
            : base($"RandomInterval(min:{minInterval.TotalMilliseconds}ms,max:{maxInterval.TotalMilliseconds}ms,attempts:{maxAttempts})",
                   maxAttempts, retryPredicate, logger)
        {
            if (minInterval < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(minInterval), "Minimum interval cannot be negative");
            if (maxInterval < minInterval)
                throw new ArgumentOutOfRangeException(nameof(maxInterval), "Maximum interval cannot be less than minimum interval");
            if (maxAttempts < 1)
                throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Max attempts must be at least 1");

            _minInterval = minInterval;
            _maxInterval = maxInterval;

            int seed;
            lock (SeedSource)
            { seed = SeedSource.Next(); }
            _random = new Random(seed);
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
        public override Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
        {
            return ExecuteWithRetryAsync(operation, cancellationToken);
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
