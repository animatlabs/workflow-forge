using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Exceptions;
using WorkflowForge.Extensions;
using WorkflowForge.Extensions.Resilience.Polly.Configurations;
using WorkflowForge.Operations;

namespace WorkflowForge.Extensions.Resilience.Polly
{
    /// <summary>
    /// A Polly-powered retry operation that can wrap any workflow operation with advanced resilience policies.
    /// Provides enterprise-grade fault tolerance and can override base resilience functionality.
    /// </summary>
    public sealed class PollyRetryOperation : WorkflowOperationBase
    {
        private readonly IWorkflowOperation _innerOperation;
        private readonly ResiliencePipeline _pipeline;
        private readonly IWorkflowForgeLogger? _logger;
        private readonly string _name;
        private volatile bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PollyRetryOperation"/> class.
        /// </summary>
        /// <param name="innerOperation">The operation to wrap with Polly resilience.</param>
        /// <param name="pipeline">The Polly resilience pipeline to use.</param>
        /// <param name="logger">Optional logger for operation events.</param>
        /// <param name="name">Optional custom name for the operation.</param>
        public PollyRetryOperation(
            IWorkflowOperation innerOperation,
            ResiliencePipeline pipeline,
            IWorkflowForgeLogger? logger = null,
            string? name = null)
        {
            _innerOperation = innerOperation ?? throw new ArgumentNullException(nameof(innerOperation));
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _logger = logger;
            _name = name ?? $"PollyRetry({innerOperation.Name})";
        }

        /// <inheritdoc />
        public override string Name => _name;

        /// <inheritdoc />
        public override bool SupportsRestore => _innerOperation.SupportsRestore;

        /// <inheritdoc />
        public override async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            try
            {
                _logger?.LogDebug($"Executing operation '{_innerOperation.Name}' with Polly resilience policies");

                return await _pipeline.ExecuteAsync(async (ct) =>
                {
                    return await _innerOperation.ForgeAsync(inputData, foundry, ct).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (BrokenCircuitException ex)
            {
                _logger?.LogWarning(ex, "Circuit breaker is open for operation '{OperationName}'", _innerOperation.Name);
                throw new WorkflowOperationException(
                    $"Circuit breaker is open for operation '{_innerOperation.Name}'",
                    ex,
                    _innerOperation.Name,
                    _innerOperation.Id);
            }
            catch (TimeoutRejectedException ex)
            {
                _logger?.LogError(ex, "Operation '{OperationName}' timed out", _innerOperation.Name);
                throw new WorkflowOperationException(
                    $"Operation '{_innerOperation.Name}' timed out",
                    ex,
                    _innerOperation.Name,
                    _innerOperation.Id);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Operation '{OperationName}' failed after applying Polly policies", _innerOperation.Name);
                throw;
            }
        }

        /// <inheritdoc />
        public override async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (!_innerOperation.SupportsRestore)
            {
                throw new NotSupportedException($"The inner operation '{_innerOperation.Name}' does not support restoration.");
            }

            try
            {
                _logger?.LogDebug($"Restoring operation '{_innerOperation.Name}' with Polly resilience policies");

                await _pipeline.ExecuteAsync(async (ct) =>
                {
                    await _innerOperation.RestoreAsync(outputData, foundry, ct).ConfigureAwait(false);
                    return 0; // Dummy return value for non-generic pipeline
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to restore operation '{OperationName}' even with Polly policies", _innerOperation.Name);
                throw;
            }
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            if (_disposed) return;

            try
            {
                _innerOperation?.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error disposing inner operation '{OperationName}'", _innerOperation?.Name ?? "Unknown");
            }

            _disposed = true;
            base.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PollyRetryOperation));
            }
        }

        #region Factory Methods

        /// <summary>
        /// Creates a Polly retry operation with exponential backoff retry policy.
        /// </summary>
        /// <param name="innerOperation">The operation to wrap.</param>
        /// <param name="maxRetryAttempts">Maximum number of retry attempts.</param>
        /// <param name="baseDelay">Base delay for exponential backoff.</param>
        /// <param name="maxDelay">Maximum delay between retries.</param>
        /// <param name="logger">Optional logger for operation events.</param>
        /// <param name="name">Optional custom name.</param>
        /// <returns>A new Polly retry operation.</returns>
        public static PollyRetryOperation WithRetryPolicy(
            IWorkflowOperation innerOperation,
            int maxRetryAttempts = 3,
            TimeSpan? baseDelay = null,
            TimeSpan? maxDelay = null,
            IWorkflowForgeLogger? logger = null,
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
                        logger?.LogWarning($"Retry attempt {args.AttemptNumber} for operation '{innerOperation.Name}' in {args.RetryDelay.TotalMilliseconds}ms due to: {args.Outcome.Exception?.Message}");
                        return default;
                    }
                })
                .Build();

            return new PollyRetryOperation(innerOperation, pipeline, logger, name);
        }

        /// <summary>
        /// Creates a Polly retry operation with circuit breaker policy.
        /// </summary>
        /// <param name="innerOperation">The operation to wrap.</param>
        /// <param name="failureThreshold">Number of failures before opening the circuit.</param>
        /// <param name="durationOfBreak">Duration to keep the circuit open.</param>
        /// <param name="logger">Optional logger for operation events.</param>
        /// <param name="name">Optional custom name.</param>
        /// <returns>A new Polly retry operation with circuit breaker.</returns>
        public static PollyRetryOperation WithCircuitBreakerPolicy(
            IWorkflowOperation innerOperation,
            int failureThreshold = 5,
            TimeSpan? durationOfBreak = null,
            IWorkflowForgeLogger? logger = null,
            string? name = null)
        {
            var breakDuration = durationOfBreak ?? TimeSpan.FromSeconds(30);

            var pipeline = new ResiliencePipelineBuilder()
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => !(ex is OperationCanceledException)),
                    FailureRatio = failureThreshold / 10.0,
                    MinimumThroughput = 5,
                    BreakDuration = breakDuration,
                    OnOpened = args =>
                    {
                        logger?.LogWarning($"Circuit breaker opened for operation '{innerOperation.Name}' for {breakDuration.TotalSeconds}s");
                        return default;
                    },
                    OnClosed = args =>
                    {
                        logger?.LogInformation($"Circuit breaker closed for operation '{innerOperation.Name}'");
                        return default;
                    }
                })
                .Build();

            return new PollyRetryOperation(innerOperation, pipeline, logger, name);
        }

        /// <summary>
        /// Creates a comprehensive Polly retry operation combining retry, circuit breaker, and timeout.
        /// </summary>
        /// <param name="innerOperation">The operation to wrap.</param>
        /// <param name="settings">Polly settings configuration.</param>
        /// <param name="logger">Optional logger for operation events.</param>
        /// <param name="name">Optional custom name.</param>
        /// <returns>A new comprehensive Polly retry operation.</returns>
        public static PollyRetryOperation WithComprehensivePolicy(
            IWorkflowOperation innerOperation,
            PollySettings settings,
            IWorkflowForgeLogger? logger = null,
            string? name = null)
        {
            var pipelineBuilder = new ResiliencePipelineBuilder();

            // Add timeout if enabled
            if (settings.Timeout.IsEnabled)
            {
                pipelineBuilder.AddTimeout(settings.Timeout.TimeoutDuration);
            }

            // Add retry if enabled
            if (settings.Retry.IsEnabled)
            {
                pipelineBuilder.AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => !(ex is OperationCanceledException || ex is TimeoutRejectedException)),
                    MaxRetryAttempts = settings.Retry.MaxRetryAttempts,
                    Delay = settings.Retry.BaseDelay,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = settings.Retry.UseJitter,
                    OnRetry = args =>
                    {
                        if (settings.EnableDetailedLogging)
                        {
                            logger?.LogWarning($"Retry attempt {args.AttemptNumber} for operation '{innerOperation.Name}' in {args.RetryDelay.TotalMilliseconds}ms due to: {args.Outcome.Exception?.Message}");
                        }
                        return default;
                    }
                });
            }

            // Add circuit breaker if enabled
            if (settings.CircuitBreaker.IsEnabled)
            {
                pipelineBuilder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => !(ex is OperationCanceledException)),
                    FailureRatio = settings.CircuitBreaker.FailureThreshold / 10.0,
                    MinimumThroughput = settings.CircuitBreaker.MinimumThroughput,
                    BreakDuration = settings.CircuitBreaker.DurationOfBreak,
                    OnOpened = args =>
                    {
                        if (settings.EnableDetailedLogging)
                        {
                            logger?.LogWarning($"Circuit breaker opened for operation '{innerOperation.Name}' for {settings.CircuitBreaker.DurationOfBreak.TotalSeconds}s");
                        }
                        return default;
                    },
                    OnClosed = args =>
                    {
                        if (settings.EnableDetailedLogging)
                        {
                            logger?.LogInformation($"Circuit breaker closed for operation '{innerOperation.Name}'");
                        }
                        return default;
                    }
                });
            }

            var pipeline = pipelineBuilder.Build();
            return new PollyRetryOperation(innerOperation, pipeline, logger, name);
        }

        /// <summary>
        /// Creates a Polly retry operation from an existing resilience pipeline.
        /// </summary>
        /// <param name="innerOperation">The operation to wrap.</param>
        /// <param name="pipeline">The Polly resilience pipeline to use.</param>
        /// <param name="logger">Optional logger for operation events.</param>
        /// <param name="name">Optional custom name.</param>
        /// <returns>A new Polly retry operation.</returns>
        public static PollyRetryOperation FromPipeline(
            IWorkflowOperation innerOperation,
            ResiliencePipeline pipeline,
            IWorkflowForgeLogger? logger = null,
            string? name = null)
        {
            return new PollyRetryOperation(innerOperation, pipeline, logger, name);
        }

        /// <summary>
        /// Creates development-friendly Polly retry operation with lenient settings.
        /// </summary>
        /// <param name="innerOperation">The operation to wrap.</param>
        /// <param name="logger">Optional logger for operation events.</param>
        /// <param name="name">Optional custom name.</param>
        /// <returns>A new development-optimized Polly retry operation.</returns>
        public static PollyRetryOperation ForDevelopment(
            IWorkflowOperation innerOperation,
            IWorkflowForgeLogger? logger = null,
            string? name = null)
        {
            var settings = PollySettings.ForDevelopment();
            return WithComprehensivePolicy(innerOperation, settings, logger, name);
        }

        /// <summary>
        /// Creates production-optimized Polly retry operation with strict settings.
        /// </summary>
        /// <param name="innerOperation">The operation to wrap.</param>
        /// <param name="logger">Optional logger for operation events.</param>
        /// <param name="name">Optional custom name.</param>
        /// <returns>A new production-optimized Polly retry operation.</returns>
        public static PollyRetryOperation ForProduction(
            IWorkflowOperation innerOperation,
            IWorkflowForgeLogger? logger = null,
            string? name = null)
        {
            var settings = PollySettings.ForProduction();
            return WithComprehensivePolicy(innerOperation, settings, logger, name);
        }

        /// <summary>
        /// Creates enterprise-grade Polly retry operation with comprehensive policies.
        /// </summary>
        /// <param name="innerOperation">The operation to wrap.</param>
        /// <param name="logger">Optional logger for operation events.</param>
        /// <param name="name">Optional custom name.</param>
        /// <returns>A new enterprise-grade Polly retry operation.</returns>
        public static PollyRetryOperation ForEnterprise(
            IWorkflowOperation innerOperation,
            IWorkflowForgeLogger? logger = null,
            string? name = null)
        {
            var settings = PollySettings.ForEnterprise();
            return WithComprehensivePolicy(innerOperation, settings, logger, name);
        }

        #endregion Factory Methods
    }

    /// <summary>
    /// Extension methods for easily wrapping operations with Polly resilience.
    /// </summary>
    public static class WorkflowOperationPollyExtensions
    {
        /// <summary>
        /// Wraps an operation with Polly retry policy.
        /// </summary>
        /// <param name="operation">The operation to wrap.</param>
        /// <param name="maxRetryAttempts">Maximum retry attempts.</param>
        /// <param name="baseDelay">Base delay for exponential backoff.</param>
        /// <param name="maxDelay">Maximum delay between retries.</param>
        /// <param name="logger">Optional logger.</param>
        /// <returns>A Polly-wrapped operation.</returns>
        public static PollyRetryOperation WithPollyRetry(
            this IWorkflowOperation operation,
            int maxRetryAttempts = 3,
            TimeSpan? baseDelay = null,
            TimeSpan? maxDelay = null,
            IWorkflowForgeLogger? logger = null)
        {
            return PollyRetryOperation.WithRetryPolicy(operation, maxRetryAttempts, baseDelay, maxDelay, logger);
        }

        /// <summary>
        /// Wraps an operation with Polly circuit breaker policy.
        /// </summary>
        /// <param name="operation">The operation to wrap.</param>
        /// <param name="failureThreshold">Failure threshold before opening.</param>
        /// <param name="durationOfBreak">Duration to keep circuit open.</param>
        /// <param name="logger">Optional logger.</param>
        /// <returns>A Polly-wrapped operation.</returns>
        public static PollyRetryOperation WithPollyCircuitBreaker(
            this IWorkflowOperation operation,
            int failureThreshold = 5,
            TimeSpan? durationOfBreak = null,
            IWorkflowForgeLogger? logger = null)
        {
            return PollyRetryOperation.WithCircuitBreakerPolicy(operation, failureThreshold, durationOfBreak, logger);
        }

        /// <summary>
        /// Wraps an operation with comprehensive Polly policies.
        /// </summary>
        /// <param name="operation">The operation to wrap.</param>
        /// <param name="settings">Polly settings configuration.</param>
        /// <param name="logger">Optional logger.</param>
        /// <returns>A comprehensively protected Polly-wrapped operation.</returns>
        public static PollyRetryOperation WithPollyComprehensive(
            this IWorkflowOperation operation,
            PollySettings settings,
            IWorkflowForgeLogger? logger = null)
        {
            return PollyRetryOperation.WithComprehensivePolicy(operation, settings, logger);
        }

        /// <summary>
        /// Wraps an operation with development-friendly Polly policies.
        /// </summary>
        /// <param name="operation">The operation to wrap.</param>
        /// <param name="logger">Optional logger.</param>
        /// <returns>A development-optimized Polly-wrapped operation.</returns>
        public static PollyRetryOperation WithPollyDevelopment(
            this IWorkflowOperation operation,
            IWorkflowForgeLogger? logger = null)
        {
            return PollyRetryOperation.ForDevelopment(operation, logger);
        }

        /// <summary>
        /// Wraps an operation with production-optimized Polly policies.
        /// </summary>
        /// <param name="operation">The operation to wrap.</param>
        /// <param name="logger">Optional logger.</param>
        /// <returns>A production-optimized Polly-wrapped operation.</returns>
        public static PollyRetryOperation WithPollyProduction(
            this IWorkflowOperation operation,
            IWorkflowForgeLogger? logger = null)
        {
            return PollyRetryOperation.ForProduction(operation, logger);
        }

        /// <summary>
        /// Wraps an operation with enterprise-grade Polly policies.
        /// </summary>
        /// <param name="operation">The operation to wrap.</param>
        /// <param name="logger">Optional logger.</param>
        /// <returns>An enterprise-grade Polly-wrapped operation.</returns>
        public static PollyRetryOperation WithPollyEnterprise(
            this IWorkflowOperation operation,
            IWorkflowForgeLogger? logger = null)
        {
            return PollyRetryOperation.ForEnterprise(operation, logger);
        }
    }
}