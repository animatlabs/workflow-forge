using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Resilience.Polly.Options;

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
        private readonly bool _detailedLogging;
        private readonly IDictionary<string, string>? _scopeTags;

        /// <summary>
        /// Initializes a new instance of the <see cref="PollyMiddleware"/> class.
        /// </summary>
        /// <param name="pipeline">The Polly resilience pipeline to apply.</param>
        /// <param name="logger">The logger for middleware events.</param>
        /// <param name="name">Optional name for the middleware.</param>
        internal PollyMiddleware(ResiliencePipeline pipeline, IWorkflowForgeLogger logger, string? name = null)
            : this(pipeline, logger, name, detailedLogging: true, scopeTags: null)
        {
        }

        private PollyMiddleware(
            ResiliencePipeline pipeline,
            IWorkflowForgeLogger logger,
            string? name,
            bool detailedLogging,
            IDictionary<string, string>? scopeTags)
        {
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _name = name ?? "PollyMiddleware";
            _detailedLogging = detailedLogging;
            _scopeTags = scopeTags != null && scopeTags.Count > 0 ? scopeTags : null;
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
            using var policyScope = _logger.BeginScope(_name, _scopeTags);

            try
            {
                if (_detailedLogging)
                {
                    _logger.LogDebug(ResilienceLogMessages.PolicyPipelineExecutionStarted);
                }

                var result = await _pipeline.ExecuteAsync(async (ct) =>
                {
                    return await next(ct).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);

                if (_detailedLogging)
                {
                    _logger.LogDebug(ResilienceLogMessages.PolicyPipelineExecutionCompleted);
                }

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
        /// Creates middleware whose pipeline is composed from the supplied options.
        /// Only the strategies whose settings are enabled are added, in the order
        /// timeout, retry, circuit breaker.
        /// </summary>
        /// <param name="options">The options describing the pipeline.</param>
        /// <param name="logger">The logger to use.</param>
        /// <returns>A new Polly middleware instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when options or logger is null.</exception>
        public static PollyMiddleware FromOptions(PollyMiddlewareOptions options, IWorkflowForgeLogger logger)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            if (!options.Enabled)
            {
                return new PollyMiddleware(
                    ResiliencePipeline.Empty,
                    logger,
                    "PollyDisabled",
                    options.EnableDetailedLogging,
                    options.DefaultTags);
            }

            if (options.EnableComprehensivePolicies)
            {
                var comprehensive = WithComprehensivePolicy(
                    logger,
                    options.Retry.MaxRetryAttempts,
                    options.Retry.BaseDelay,
                    options.CircuitBreaker.FailureThreshold,
                    options.CircuitBreaker.BreakDuration,
                    options.Timeout.DefaultTimeout);

                return new PollyMiddleware(
                    comprehensive._pipeline,
                    logger,
                    comprehensive._name,
                    options.EnableDetailedLogging,
                    options.DefaultTags);
            }

            var builder = new ResiliencePipelineBuilder();
            var names = new List<string>();

            if (options.Timeout.IsEnabled)
            {
                builder.AddTimeout(options.Timeout.DefaultTimeout);
                names.Add("timeout");
            }

            if (options.Retry.IsEnabled)
            {
                builder.AddRetry(BuildRetryOptions(options.Retry, logger));
                names.Add("retry");
            }

            if (options.CircuitBreaker.IsEnabled)
            {
                builder.AddCircuitBreaker(BuildCircuitBreakerOptions(options.CircuitBreaker, logger));
                names.Add("circuitBreaker");
            }

            var name = names.Count == 0 ? "PollyNoStrategies" : "Polly(" + string.Join(",", names.ToArray()) + ")";

            return new PollyMiddleware(
                builder.Build(),
                logger,
                name,
                options.EnableDetailedLogging,
                options.DefaultTags);
        }

        private static RetryStrategyOptions BuildRetryOptions(PollyRetrySettings settings, IWorkflowForgeLogger logger)
        {
            return new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => !(ex is OperationCanceledException)),
                MaxRetryAttempts = settings.MaxRetryAttempts,
                Delay = settings.BaseDelay,
                BackoffType = ParseBackoffType(settings.BackoffType),
                UseJitter = settings.UseJitter,
                OnRetry = args =>
                {
                    var delayMs = args.RetryDelay.TotalMilliseconds.ToString("F0");
                    logger.LogWarning("{Message} (Attempt {RetryAttempt} of {MaxRetryAttempts} in {RetryDelayMs}ms)",
                        ResilienceLogMessages.RetryAttemptStarted, args.AttemptNumber, settings.MaxRetryAttempts, delayMs);
                    return default;
                }
            };
        }

        private static CircuitBreakerStrategyOptions BuildCircuitBreakerOptions(PollyCircuitBreakerSettings settings, IWorkflowForgeLogger logger)
        {
            return new CircuitBreakerStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => !(ex is OperationCanceledException)),
                FailureRatio = Math.Min(1.0, settings.FailureThreshold / 10.0),
                MinimumThroughput = Math.Max(2, settings.MinimumThroughput),
                SamplingDuration = settings.SamplingDuration,
                BreakDuration = settings.BreakDuration,
                OnOpened = args =>
                {
                    logger.LogWarning("{Message} (State: Open, Threshold: {FailureThreshold})",
                        ResilienceLogMessages.CircuitBreakerOpened, settings.FailureThreshold);
                    return default;
                },
                OnClosed = args =>
                {
                    logger.LogInformation("{Message} (State: Closed)", ResilienceLogMessages.CircuitBreakerReset);
                    return default;
                }
            };
        }

        private static DelayBackoffType ParseBackoffType(string? backoffType)
        {
            if (string.Equals(backoffType, "Linear", StringComparison.OrdinalIgnoreCase))
                return DelayBackoffType.Linear;
            if (string.Equals(backoffType, "Constant", StringComparison.OrdinalIgnoreCase))
                return DelayBackoffType.Constant;

            return DelayBackoffType.Exponential;
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
