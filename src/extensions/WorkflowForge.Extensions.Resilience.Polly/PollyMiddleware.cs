using System;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Extensions.Resilience.Polly
{
    /// <summary>
    /// Middleware that applies Polly resilience policies to workflow operations.
    /// Provides enterprise-grade fault tolerance with circuit breakers, retries, and timeouts.
    /// </summary>
    public sealed class PollyMiddleware : IWorkflowOperationMiddleware
    {
        private readonly ResiliencePipeline _pipeline;
        private readonly IWorkflowForgeLogger _logger;
        private readonly string _name;

        /// <summary>
        /// Initializes a new instance of the <see cref="PollyMiddleware"/> class.
        /// </summary>
        /// <param name="pipeline">The Polly resilience pipeline to apply.</param>
        /// <param name="logger">The logger for middleware events.</param>
        /// <param name="name">Optional name for the middleware.</param>
        internal PollyMiddleware(ResiliencePipeline pipeline, IWorkflowForgeLogger logger, string? name = null)
        {
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _name = name ?? "PollyMiddleware";
        }

        /// <summary>
        /// Gets the name of the middleware.
        /// </summary>
        public string Name => _name;

        /// <inheritdoc />
        public async Task<object?> ExecuteAsync(
            IWorkflowOperation operation,
            IWorkflowFoundry foundry,
            object? inputData,
            Func<CancellationToken, Task<object?>> next,
            CancellationToken cancellationToken = default)
        {
            using var policyScope = _logger.BeginScope(_name);

            try
            {
                _logger.LogDebug(ResilienceLogMessages.PolicyPipelineExecutionStarted);

                var result = await _pipeline.ExecuteAsync(async (ct) =>
                {
                    return await next(ct).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);

                _logger.LogDebug(ResilienceLogMessages.PolicyPipelineExecutionCompleted);
                return result;
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogWarning(ex, "{Message} (CircuitState: Open, Reason: BrokenCircuitException)", ResilienceLogMessages.CircuitBreakerRejected);
                throw new BrokenCircuitException(
                    $"Circuit breaker is open for operation '{operation.Name}'", ex);
            }
            catch (TimeoutRejectedException ex)
            {
                _logger.LogError(ex, "{Message} (TimedOut: true, Reason: TimeoutRejectedException)", ResilienceLogMessages.OperationTimedOut);
                throw;
            }
            catch (Exception ex)
            {
                var errorProperties = _logger.CreateErrorProperties(ex, "ResiliencePolicy");
                _logger.LogError(errorProperties, ex, ResilienceLogMessages.PolicyPipelineExecutionFailed);
                throw;
            }
        }

        /// <summary>
        /// Creates middleware with a retry policy using exponential backoff.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="maxRetryAttempts">Maximum number of retry attempts.</param>
        /// <param name="baseDelay">Base delay for exponential backoff.</param>
        /// <param name="maxDelay">Maximum delay between retries.</param>
        /// <param name="name">Optional name for the middleware.</param>
        /// <returns>A new Polly middleware instance.</returns>
        public static PollyMiddleware WithRetryPolicy(
            IWorkflowForgeLogger logger,
            int maxRetryAttempts = 3,
            TimeSpan? baseDelay = null,
            TimeSpan? maxDelay = null,
            string? name = null)
        {
            var delay = baseDelay ?? TimeSpan.FromSeconds(1);
            var maxDelayValue = maxDelay ?? TimeSpan.FromSeconds(30);

            var pipeline = new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => !(ex is OperationCanceledException)),
                    MaxRetryAttempts = maxRetryAttempts,
                    Delay = delay,
                    MaxDelay = maxDelayValue,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    OnRetry = args =>
                    {
                        var delayMs = args.RetryDelay.TotalMilliseconds.ToString("F0");
                        var reason = args.Outcome.Exception?.GetType().Name ?? "Unknown";

                        if (args.Outcome.Exception is Exception ex)
                        {
                            logger.LogWarning(ex, "{Message} (Attempt {RetryAttempt} of {MaxRetryAttempts} in {RetryDelayMs}ms) due to: {RetryReason}",
                                ResilienceLogMessages.RetryAttemptStarted, args.AttemptNumber, maxRetryAttempts, delayMs, reason);
                        }
                        else
                        {
                            logger.LogWarning("{Message} (Attempt {RetryAttempt} of {MaxRetryAttempts} in {RetryDelayMs}ms)",
                                ResilienceLogMessages.RetryAttemptStarted, args.AttemptNumber, maxRetryAttempts, delayMs);
                        }
                        return default;
                    }
                })
                .Build();

            return new PollyMiddleware(pipeline, logger, name ?? $"PollyRetry(attempts:{maxRetryAttempts})");
        }

        /// <summary>
        /// Creates middleware with a circuit breaker policy.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="failureThreshold">Number of failures before opening the circuit.</param>
        /// <param name="durationOfBreak">Duration to keep the circuit open.</param>
        /// <param name="name">Optional name for the middleware.</param>
        /// <returns>A new Polly middleware instance.</returns>
        public static PollyMiddleware WithCircuitBreakerPolicy(
            IWorkflowForgeLogger logger,
            int failureThreshold = 5,
            TimeSpan? durationOfBreak = null,
            string? name = null)
        {
            var breakDuration = durationOfBreak ?? TimeSpan.FromSeconds(30);

            var pipeline = new ResiliencePipelineBuilder()
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => !(ex is OperationCanceledException)),
                    FailureRatio = failureThreshold / 10.0, // Convert to ratio
                    MinimumThroughput = 5,
                    BreakDuration = breakDuration,
                    OnOpened = args =>
                    {
                        var delayMs = breakDuration.TotalMilliseconds.ToString("F0");
                        logger.LogWarning("{Message} (State: Open, Threshold: {FailureThreshold}, DurationMs: {BreakDurationMs})",
                            ResilienceLogMessages.CircuitBreakerOpened, failureThreshold, delayMs);
                        return default;
                    },
                    OnClosed = args =>
                    {
                        logger.LogInformation("{Message} (State: Closed)", ResilienceLogMessages.CircuitBreakerReset);
                        return default;
                    },
                    OnHalfOpened = args =>
                    {
                        logger.LogInformation("{Message} (State: HalfOpen)", ResilienceLogMessages.CircuitBreakerHalfOpen);
                        return default;
                    }
                })
                .Build();

            return new PollyMiddleware(pipeline, logger, name ?? $"PollyCircuitBreaker(threshold:{failureThreshold})");
        }

        /// <summary>
        /// Creates middleware with a timeout policy.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="timeout">The timeout duration.</param>
        /// <param name="name">Optional name for the middleware.</param>
        /// <returns>A new Polly middleware instance.</returns>
        public static PollyMiddleware WithTimeoutPolicy(
            IWorkflowForgeLogger logger,
            TimeSpan timeout,
            string? name = null)
        {
            var delayMs = timeout.TotalMilliseconds.ToString("F0");
            logger.LogDebug("{Message} (PolicyType: Timeout, TimeoutMs: {TimeoutMs})", ResilienceLogMessages.TimeoutPolicyApplied, delayMs);

            var pipeline = new ResiliencePipelineBuilder()
                .AddTimeout(timeout)
                .Build();

            return new PollyMiddleware(pipeline, logger, name ?? $"PollyTimeout({timeout.TotalSeconds}s)");
        }

        /// <summary>
        /// Creates comprehensive middleware combining retry, circuit breaker, and timeout policies.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="maxRetryAttempts">Maximum number of retry attempts.</param>
        /// <param name="baseDelay">Base delay for exponential backoff.</param>
        /// <param name="circuitBreakerThreshold">Circuit breaker failure threshold.</param>
        /// <param name="circuitBreakerDuration">Circuit breaker open duration.</param>
        /// <param name="timeoutDuration">Operation timeout duration.</param>
        /// <param name="name">Optional name for the middleware.</param>
        /// <returns>A new comprehensive Polly middleware instance.</returns>
        public static PollyMiddleware WithComprehensivePolicy(
            IWorkflowForgeLogger logger,
            int maxRetryAttempts = 3,
            TimeSpan? baseDelay = null,
            int circuitBreakerThreshold = 5,
            TimeSpan? circuitBreakerDuration = null,
            TimeSpan? timeoutDuration = null,
            string? name = null)
        {
            var delay = baseDelay ?? TimeSpan.FromSeconds(1);
            var breakDuration = circuitBreakerDuration ?? TimeSpan.FromSeconds(30);
            var timeout = timeoutDuration ?? TimeSpan.FromSeconds(10);

            var delayStr = timeout.TotalMilliseconds.ToString("F0");
            logger.LogDebug("{Message} (PolicyType: Comprehensive, MaxRetries: {MaxRetryAttempts}, Threshold: {FailureThreshold}, TimeoutMs: {TimeoutMs})",
                ResilienceLogMessages.ResiliencePolicyApplied, maxRetryAttempts, circuitBreakerThreshold, delayStr);

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
                        var retryDelayMs = args.RetryDelay.TotalMilliseconds.ToString("F0");
                        var reason = args.Outcome.Exception?.GetType().Name ?? "Unknown";

                        if (args.Outcome.Exception is Exception ex)
                        {
                            logger.LogWarning(ex, "{Message} (Attempt {RetryAttempt} of {MaxRetryAttempts} in {RetryDelayMs}ms) due to: {RetryReason}",
                                ResilienceLogMessages.RetryAttemptStarted, args.AttemptNumber, maxRetryAttempts, retryDelayMs, reason);
                        }
                        else
                        {
                            logger.LogWarning("{Message} (Attempt {RetryAttempt} of {MaxRetryAttempts} in {RetryDelayMs}ms)",
                                ResilienceLogMessages.RetryAttemptStarted, args.AttemptNumber, maxRetryAttempts, retryDelayMs);
                        }
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
                        var bDuration = breakDuration.TotalMilliseconds.ToString("F0");
                        logger.LogWarning("{Message} (State: Open, DurationMs: {BreakDurationMs})",
                            ResilienceLogMessages.CircuitBreakerOpened, bDuration);
                        return default;
                    },
                    OnClosed = args =>
                    {
                        logger.LogInformation("{Message} (State: Closed)", ResilienceLogMessages.CircuitBreakerReset);
                        return default;
                    }
                })
                .Build();

            return new PollyMiddleware(pipeline, logger, name ?? "PollyComprehensive");
        }
    }
}
