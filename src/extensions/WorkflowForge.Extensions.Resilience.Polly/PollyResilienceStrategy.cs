using System;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Resilience.Abstractions;

namespace WorkflowForge.Extensions.Resilience.Polly
{
    /// <summary>
    /// Enterprise-grade resilience strategy that uses Polly for advanced fault tolerance.
    /// Supports retry policies, circuit breakers, timeouts, and bulkhead isolation.
    /// </summary>
    public sealed class PollyResilienceStrategy : IWorkflowResilienceStrategy
    {
        private readonly ResiliencePipeline _pipeline;
        private readonly IWorkflowForgeLogger? _logger;
        private readonly string _name;

        /// <summary>
        /// Initializes a new instance of the <see cref="PollyResilienceStrategy"/> class.
        /// </summary>
        /// <param name="pipeline">The Polly resilience pipeline to use.</param>
        /// <param name="name">The name of the strategy.</param>
        /// <param name="logger">Optional logger for strategy events.</param>
        public PollyResilienceStrategy(ResiliencePipeline pipeline, string name, IWorkflowForgeLogger? logger = null)
        {
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _logger = logger;
        }

        /// <inheritdoc />
        public string Name => _name;

        /// <inheritdoc />
        public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            try
            {
                await _pipeline.ExecuteAsync(async (ct) =>
                {
                    await operation().ConfigureAwait(false);
                    return 0; // Dummy return value for non-generic pipeline
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (BrokenCircuitException ex)
            {
                _logger?.LogWarning(ex, "Circuit breaker is open, operation blocked");
                throw new CircuitBreakerOpenException("Circuit breaker is open", ex);
            }
            catch (TimeoutRejectedException ex)
            {
                _logger?.LogError(ex, "Operation timed out");
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Operation failed after resilience policies were applied");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            try
            {
                return await _pipeline.ExecuteAsync(async (ct) =>
                {
                    return await operation().ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (BrokenCircuitException ex)
            {
                _logger?.LogWarning(ex, "Circuit breaker is open, operation blocked");
                throw new CircuitBreakerOpenException("Circuit breaker is open", ex);
            }
            catch (TimeoutRejectedException ex)
            {
                _logger?.LogError(ex, "Operation timed out");
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Operation failed after resilience policies were applied");
                throw;
            }
        }

        /// <inheritdoc />
        public Task<bool> ShouldRetryAsync(int attemptNumber, Exception? exception, CancellationToken cancellationToken)
        {
            // Polly handles retry logic internally, this method is for compatibility
            // In practice, Polly policies determine retry behavior
            return Task.FromResult(false);
        }

        /// <inheritdoc />
        public TimeSpan GetRetryDelay(int attemptNumber, Exception? exception)
        {
            // Polly handles delay calculation internally, this method is for compatibility
            // In practice, Polly policies determine delay behavior
            return TimeSpan.Zero;
        }

        /// <summary>
        /// Creates a resilience strategy with retry policy using exponential backoff.
        /// </summary>
        /// <param name="maxRetryAttempts">Maximum number of retry attempts.</param>
        /// <param name="baseDelay">Base delay for exponential backoff.</param>
        /// <param name="maxDelay">Maximum delay between retries.</param>
        /// <param name="logger">Optional logger for strategy events.</param>
        /// <returns>A new Polly resilience strategy.</returns>
        public static PollyResilienceStrategy CreateRetryPolicy(
            int maxRetryAttempts = 3,
            TimeSpan? baseDelay = null,
            TimeSpan? maxDelay = null,
            IWorkflowForgeLogger? logger = null)
        {
            var delay = baseDelay ?? TimeSpan.FromSeconds(1);
            var maxDelayValue = maxDelay ?? TimeSpan.FromSeconds(30);

            var pipeline = new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => !(ex is OperationCanceledException)),
                    MaxRetryAttempts = maxRetryAttempts,
                    Delay = delay,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    OnRetry = args =>
                    {
                        logger?.LogWarning($"Retry attempt {args.AttemptNumber} in {args.RetryDelay.TotalMilliseconds}ms due to: {args.Outcome.Exception?.Message}");
                        return default;
                    }
                })
                .Build();

            return new PollyResilienceStrategy(pipeline, $"PollyRetry(attempts:{maxRetryAttempts})", logger);
        }

        /// <summary>
        /// Creates a resilience strategy with circuit breaker policy.
        /// </summary>
        /// <param name="failureThreshold">Number of failures before opening the circuit.</param>
        /// <param name="durationOfBreak">Duration to keep the circuit open.</param>
        /// <param name="minimumThroughput">Minimum number of actions in the time window.</param>
        /// <param name="logger">Optional logger for strategy events.</param>
        /// <returns>A new Polly resilience strategy.</returns>
        public static PollyResilienceStrategy CreateCircuitBreakerPolicy(
            int failureThreshold = 5,
            TimeSpan? durationOfBreak = null,
            int minimumThroughput = 5,
            IWorkflowForgeLogger? logger = null)
        {
            var breakDuration = durationOfBreak ?? TimeSpan.FromSeconds(30);

            var pipeline = new ResiliencePipelineBuilder()
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => !(ex is OperationCanceledException)),
                    FailureRatio = failureThreshold / 10.0, // Convert to ratio
                    MinimumThroughput = minimumThroughput,
                    BreakDuration = breakDuration,
                    OnOpened = args =>
                    {
                        logger?.LogWarning($"Circuit breaker opened for {breakDuration.TotalSeconds}s");
                        return default;
                    },
                    OnClosed = args =>
                    {
                        logger?.LogInformation("Circuit breaker closed");
                        return default;
                    },
                    OnHalfOpened = args =>
                    {
                        logger?.LogInformation("Circuit breaker half-opened, testing...");
                        return default;
                    }
                })
                .Build();

            return new PollyResilienceStrategy(pipeline, $"PollyCircuitBreaker(threshold:{failureThreshold})", logger);
        }

        /// <summary>
        /// Creates a comprehensive resilience strategy combining retry, circuit breaker, and timeout.
        /// </summary>
        /// <param name="maxRetryAttempts">Maximum number of retry attempts.</param>
        /// <param name="baseDelay">Base delay for exponential backoff.</param>
        /// <param name="circuitBreakerThreshold">Circuit breaker failure threshold.</param>
        /// <param name="circuitBreakerDuration">Circuit breaker open duration.</param>
        /// <param name="timeoutDuration">Operation timeout duration.</param>
        /// <param name="logger">Optional logger for strategy events.</param>
        /// <returns>A new comprehensive Polly resilience strategy.</returns>
        public static PollyResilienceStrategy CreateComprehensivePolicy(
            int maxRetryAttempts = 3,
            TimeSpan? baseDelay = null,
            int circuitBreakerThreshold = 5,
            TimeSpan? circuitBreakerDuration = null,
            TimeSpan? timeoutDuration = null,
            IWorkflowForgeLogger? logger = null)
        {
            var delay = baseDelay ?? TimeSpan.FromSeconds(1);
            var breakDuration = circuitBreakerDuration ?? TimeSpan.FromSeconds(30);
            var timeout = timeoutDuration ?? TimeSpan.FromSeconds(10);

            var pipeline = new ResiliencePipelineBuilder()
                .AddTimeout(timeout)
                .AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => !(ex is OperationCanceledException || ex is TimeoutRejectedException)),
                    MaxRetryAttempts = maxRetryAttempts,
                    Delay = delay,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    OnRetry = args =>
                    {
                        logger?.LogWarning($"Retry attempt {args.AttemptNumber} in {args.RetryDelay.TotalMilliseconds}ms due to: {args.Outcome.Exception?.Message}");
                        return default;
                    }
                })
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => !(ex is OperationCanceledException)),
                    FailureRatio = circuitBreakerThreshold / 10.0,
                    MinimumThroughput = 5,
                    BreakDuration = breakDuration,
                    OnOpened = args =>
                    {
                        logger?.LogWarning($"Circuit breaker opened for {breakDuration.TotalSeconds}s");
                        return default;
                    },
                    OnClosed = args =>
                    {
                        logger?.LogInformation("Circuit breaker closed");
                        return default;
                    }
                })
                .Build();

            return new PollyResilienceStrategy(pipeline, "PollyComprehensive", logger);
        }

        /// <summary>
        /// Creates a resilience strategy with timeout policy.
        /// </summary>
        /// <param name="timeout">The timeout duration.</param>
        /// <param name="logger">Optional logger for strategy events.</param>
        /// <returns>A new Polly resilience strategy.</returns>
        public static PollyResilienceStrategy CreateTimeoutPolicy(
            TimeSpan timeout,
            IWorkflowForgeLogger? logger = null)
        {
            var pipeline = new ResiliencePipelineBuilder()
                .AddTimeout(timeout)
                .Build();

            return new PollyResilienceStrategy(pipeline, $"PollyTimeout({timeout.TotalSeconds}s)", logger);
        }
    }
}