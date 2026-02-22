using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Extensions.Resilience.Abstractions
{
    /// <summary>
    /// Base class for resilience strategies that provides common retry logic
    /// including <see cref="ShouldRetryAsync"/> and a shared retry loop.
    /// </summary>
    public abstract class ResilienceStrategyBase : IWorkflowResilienceStrategy
    {
        private readonly string _name;
        private readonly IWorkflowForgeLogger? _logger;

        /// <summary>
        /// Gets the maximum number of attempts (including the initial attempt).
        /// </summary>
        protected int MaxAttempts { get; }

        /// <summary>
        /// Gets the optional predicate to determine if a specific exception should be retried.
        /// </summary>
        protected Func<Exception, bool>? RetryPredicate { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResilienceStrategyBase"/> class.
        /// </summary>
        /// <param name="name">The name of the strategy.</param>
        /// <param name="maxAttempts">The maximum number of attempts (including the initial attempt).</param>
        /// <param name="retryPredicate">Optional predicate to determine if an exception should be retried.</param>
        /// <param name="logger">The optional logger.</param>
        protected ResilienceStrategyBase(
            string name,
            int maxAttempts,
            Func<Exception, bool>? retryPredicate = null,
            IWorkflowForgeLogger? logger = null)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            MaxAttempts = maxAttempts;
            RetryPredicate = retryPredicate;
            _logger = logger;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResilienceStrategyBase"/> class
        /// without retry parameters (for strategies like Polly that handle retry internally).
        /// </summary>
        /// <param name="name">The name of the strategy.</param>
        /// <param name="logger">The optional logger.</param>
        protected ResilienceStrategyBase(string name, IWorkflowForgeLogger? logger = null)
            : this(name, maxAttempts: 1, retryPredicate: null, logger: logger)
        {
        }

        /// <inheritdoc />
        public string Name => _name;

        /// <summary>
        /// Gets the logger for this strategy.
        /// </summary>
        protected IWorkflowForgeLogger? Logger => _logger;

        /// <inheritdoc />
        public abstract Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default);

        /// <inheritdoc />
        public virtual Task<bool> ShouldRetryAsync(int attemptNumber, Exception? exception, CancellationToken cancellationToken)
        {
            if (attemptNumber >= MaxAttempts)
            {
                Logger?.LogDebug("Max attempts {MaxAttempts} reached, not retrying", MaxAttempts);
                return Task.FromResult(false);
            }

            if (exception is OperationCanceledException)
            {
                Logger?.LogDebug("Operation was cancelled, not retrying");
                return Task.FromResult(false);
            }

            if (RetryPredicate != null && exception != null)
            {
                bool shouldRetry = RetryPredicate(exception);
                Logger?.LogDebug("Custom retry predicate returned {ShouldRetry} for exception {ExceptionType}",
                    shouldRetry, exception.GetType().Name);
                return Task.FromResult(shouldRetry);
            }

            return Task.FromResult(true);
        }

        /// <inheritdoc />
        public abstract TimeSpan GetRetryDelay(int attemptNumber, Exception? exception);

        /// <summary>
        /// Shared retry loop used by strategies that implement their own delay calculation
        /// but share the same attempt/retry/delay structure.
        /// </summary>
        protected async Task ExecuteWithRetryAsync(Func<Task> operation, CancellationToken cancellationToken)
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
                    return;
                }
                catch (Exception ex)
                {
                    var shouldRetry = await ShouldRetryAsync(attemptNumber, ex, cancellationToken).ConfigureAwait(false);
                    if (!shouldRetry)
                    {
                        Logger?.LogError(ex, "Attempt {AttemptNumber} failed and will not be retried", attemptNumber);
                        throw;
                    }

                    var delay = GetRetryDelay(attemptNumber, ex);

                    Logger?.LogWarning("Attempt {AttemptNumber} failed, retrying in {DelayMs}ms. Error: {ErrorMessage}",
                        attemptNumber, delay.TotalMilliseconds, ex.Message);

                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }

        /// <inheritdoc />
        public virtual async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
        {
            T result = default!;
            await ExecuteAsync(async () => { result = await operation().ConfigureAwait(false); }, cancellationToken).ConfigureAwait(false);
            return result;
        }
    }
}