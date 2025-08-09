using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using WorkflowForge.Abstractions;
using WorkflowForge.Loggers;
using WorkflowForge.Constants;

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
        public PollyMiddleware(ResiliencePipeline pipeline, IWorkflowForgeLogger logger, string? name = null)
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
            Func<Task<object?>> next,
            CancellationToken cancellationToken = default)
        {
            var policyProperties = new Dictionary<string, string>
            {
                [ResiliencePropertyNames.PolicyType] = "ResiliencePolicy",
                [ResiliencePropertyNames.PolicyName] = _name,
                [ResiliencePropertyNames.PolicyName] = _name
            };

            using var policyScope = _logger.BeginScope("ResiliencePolicyExecution", policyProperties);

            try
            {
                _logger.LogDebug(ResilienceLogMessages.PolicyPipelineExecutionStarted);

                var result = await _pipeline.ExecuteAsync(async (ct) =>
                {
                    return await next().ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);

                _logger.LogDebug(ResilienceLogMessages.PolicyPipelineExecutionCompleted);
                return result;
            }
            catch (BrokenCircuitException ex)
            {
                var circuitProperties = new Dictionary<string, string>
                {
                    [ResiliencePropertyNames.CircuitState] = "Open",
                    [ResiliencePropertyNames.RetryReason] = nameof(BrokenCircuitException)
                };

                _logger.LogWarning(circuitProperties, ex, ResilienceLogMessages.CircuitBreakerRejected);
                throw new BrokenCircuitException(
                    $"Circuit breaker is open for operation '{operation.Name}'", ex);
            }
            catch (TimeoutRejectedException ex)
            {
                var timeoutProperties = new Dictionary<string, string>
                {
                    [ResiliencePropertyNames.TimedOut] = "true",
                    [ResiliencePropertyNames.RetryReason] = nameof(TimeoutRejectedException)
                };

                _logger.LogError(timeoutProperties, ex, ResilienceLogMessages.OperationTimedOut);
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
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    OnRetry = args =>
                    {
                        var retryProperties = new Dictionary<string, string>
                        {
                            [ResiliencePropertyNames.RetryAttempt] = args.AttemptNumber.ToString(),
                            [ResiliencePropertyNames.MaxRetryAttempts] = maxRetryAttempts.ToString(),
                            [ResiliencePropertyNames.RetryDelayMs] = args.RetryDelay.TotalMilliseconds.ToString("F0"),
                            [ResiliencePropertyNames.RetryReason] = args.Outcome.Exception?.GetType().Name ?? "Unknown"
                        };

                        if (args.Outcome.Exception is Exception ex)
                        {
                            logger.LogWarning(retryProperties, ex, ResilienceLogMessages.RetryAttemptStarted);
                        }
                        else
                        {
                            logger.LogWarning(retryProperties, ResilienceLogMessages.RetryAttemptStarted);
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
                        var openProperties = new Dictionary<string, string>
                        {
                            [ResiliencePropertyNames.CircuitState] = "Open",
                            [ResiliencePropertyNames.BreakDurationMs] = breakDuration.TotalMilliseconds.ToString("F0"),
                            [ResiliencePropertyNames.FailureThreshold] = failureThreshold.ToString()
                        };

                        logger.LogWarning(openProperties, ResilienceLogMessages.CircuitBreakerOpened);
                        return default;
                    },
                    OnClosed = args =>
                    {
                        var closedProperties = new Dictionary<string, string>
                        {
                            [ResiliencePropertyNames.CircuitState] = "Closed"
                        };

                        logger.LogInformation(closedProperties, ResilienceLogMessages.CircuitBreakerReset);
                        return default;
                    },
                    OnHalfOpened = args =>
                    {
                        var halfOpenProperties = new Dictionary<string, string>
                        {
                            [ResiliencePropertyNames.CircuitState] = "HalfOpen"
                        };

                        logger.LogInformation(halfOpenProperties, ResilienceLogMessages.CircuitBreakerHalfOpen);
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
            var timeoutProperties = new Dictionary<string, string>
            {
                [ResiliencePropertyNames.TimeoutMs] = timeout.TotalMilliseconds.ToString("F0"),
                [ResiliencePropertyNames.PolicyType] = "Timeout"
            };

            logger.LogDebug(timeoutProperties, ResilienceLogMessages.TimeoutPolicyApplied);

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

            var comprehensiveProperties = new Dictionary<string, string>
            {
                [ResiliencePropertyNames.PolicyType] = "Comprehensive",
                [ResiliencePropertyNames.MaxRetryAttempts] = maxRetryAttempts.ToString(),
                [ResiliencePropertyNames.FailureThreshold] = circuitBreakerThreshold.ToString(),
                [ResiliencePropertyNames.TimeoutMs] = timeout.TotalMilliseconds.ToString("F0"),
                [ResiliencePropertyNames.PolicyCount] = "3"
            };

            logger.LogDebug(comprehensiveProperties, ResilienceLogMessages.ResiliencePolicyApplied);

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
                        var retryProperties = new Dictionary<string, string>
                        {
                            [ResiliencePropertyNames.RetryAttempt] = args.AttemptNumber.ToString(),
                            [ResiliencePropertyNames.RetryDelayMs] = args.RetryDelay.TotalMilliseconds.ToString("F0"),
                            [ResiliencePropertyNames.RetryReason] = args.Outcome.Exception?.GetType().Name ?? "Unknown"
                        };

                        if (args.Outcome.Exception is Exception ex)
                        {
                            logger.LogWarning(retryProperties, ex, ResilienceLogMessages.RetryAttemptStarted);
                        }
                        else
                        {
                            logger.LogWarning(retryProperties, ResilienceLogMessages.RetryAttemptStarted);
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
                        var openProperties = new Dictionary<string, string>
                        {
                            [ResiliencePropertyNames.CircuitState] = "Open",
                            [ResiliencePropertyNames.BreakDurationMs] = breakDuration.TotalMilliseconds.ToString("F0")
                        };

                        logger.LogWarning(openProperties, ResilienceLogMessages.CircuitBreakerOpened);
                        return default;
                    },
                    OnClosed = args =>
                    {
                        var closedProperties = new Dictionary<string, string>
                        {
                            [ResiliencePropertyNames.CircuitState] = "Closed"
                        };

                        logger.LogInformation(closedProperties, ResilienceLogMessages.CircuitBreakerReset);
                        return default;
                    }
                })
                .Build();

            return new PollyMiddleware(pipeline, logger, name ?? "PollyComprehensive");
        }
    }
}