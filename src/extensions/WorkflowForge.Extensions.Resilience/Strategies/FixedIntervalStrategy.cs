using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Resilience.Abstractions;

namespace WorkflowForge.Extensions.Resilience.Strategies
{
    /// <summary>
    /// Implements a resilience strategy with fixed intervals between retry attempts.
    /// </summary>
    public sealed class FixedIntervalStrategy : ResilienceStrategyBase
    {
        private readonly TimeSpan _interval;
        private readonly int _maxAttempts;
        private readonly Func<Exception, bool>? _retryPredicate;

        /// <summary>
        /// Initializes a new instance of the <see cref="FixedIntervalStrategy"/> class.
        /// </summary>
        /// <param name="interval">The fixed interval between retry attempts.</param>
        /// <param name="maxAttempts">The maximum number of attempts (including the initial attempt).</param>
        /// <param name="retryPredicate">Optional predicate to determine if an exception should be retried.</param>
        /// <param name="logger">Optional logger for the strategy.</param>
        public FixedIntervalStrategy(
            TimeSpan interval,
            int maxAttempts,
            Func<Exception, bool>? retryPredicate = null,
            IWorkflowForgeLogger? logger = null)
            : base($"FixedInterval(interval:{interval.TotalMilliseconds}ms,attempts:{maxAttempts})", logger)
        {
            if (interval < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(interval), "Interval cannot be negative");
            if (maxAttempts < 1)
                throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Max attempts must be at least 1");

            _interval = interval;
            _maxAttempts = maxAttempts;
            _retryPredicate = retryPredicate;
        }

        /// <inheritdoc />
        public override async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            int attemptNumber = 1;
            Exception? lastException = null;

            while (attemptNumber <= _maxAttempts)
            {
                try
                {
                    Logger?.LogDebug("Executing operation, attempt {AttemptNumber} of {MaxAttempts}", attemptNumber, _maxAttempts);
                    await operation().ConfigureAwait(false);
                    return; // Success
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (await ShouldRetryAsync(attemptNumber, ex, cancellationToken).ConfigureAwait(false))
                    {
                        var delay = GetRetryDelay(attemptNumber, ex);
                        if (delay > TimeSpan.Zero)
                        {
                            Logger?.LogDebug("Delaying {DelayMs}ms before retry", delay.TotalMilliseconds);
                            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                        }
                        attemptNumber++;
                    }
                    else
                    {
                        throw; // Don't retry
                    }
                }
            }

            // All attempts exhausted
            Logger?.LogWarning("All {MaxAttempts} attempts exhausted", _maxAttempts);
            throw lastException ?? new InvalidOperationException("Operation failed after all retry attempts");
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

            Logger?.LogDebug("Using fixed retry delay of {DelayMs}ms for attempt {AttemptNumber}",
                _interval.TotalMilliseconds, attemptNumber);

            return _interval;
        }

        /// <summary>
        /// Creates a default fixed interval strategy with 1-second intervals.
        /// </summary>
        /// <param name="maxAttempts">Maximum number of retry attempts.</param>
        /// <param name="logger">Optional logger for strategy events.</param>
        /// <returns>A new fixed interval strategy.</returns>
        public static FixedIntervalStrategy Default(int maxAttempts = 3, IWorkflowForgeLogger? logger = null)
        {
            return new FixedIntervalStrategy(
                interval: TimeSpan.FromSeconds(1),
                maxAttempts: maxAttempts,
                logger: logger);
        }

        /// <summary>
        /// Creates a fast fixed interval strategy with 100ms intervals.
        /// </summary>
        /// <param name="maxAttempts">Maximum number of retry attempts.</param>
        /// <param name="logger">Optional logger for strategy events.</param>
        /// <returns>A new fixed interval strategy.</returns>
        public static FixedIntervalStrategy Fast(int maxAttempts = 5, IWorkflowForgeLogger? logger = null)
        {
            return new FixedIntervalStrategy(
                interval: TimeSpan.FromMilliseconds(100),
                maxAttempts: maxAttempts,
                logger: logger);
        }

        /// <summary>
        /// Creates a slow fixed interval strategy with 5-second intervals.
        /// </summary>
        /// <param name="maxAttempts">Maximum number of retry attempts.</param>
        /// <param name="logger">Optional logger for strategy events.</param>
        /// <returns>A new fixed interval strategy.</returns>
        public static FixedIntervalStrategy Slow(int maxAttempts = 2, IWorkflowForgeLogger? logger = null)
        {
            return new FixedIntervalStrategy(
                interval: TimeSpan.FromSeconds(5),
                maxAttempts: maxAttempts,
                logger: logger);
        }
    }
}