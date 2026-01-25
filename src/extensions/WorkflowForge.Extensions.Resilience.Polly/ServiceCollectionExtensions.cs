using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Resilience.Abstractions;
using WorkflowForge.Extensions.Resilience.Polly.Options;

namespace WorkflowForge.Extensions.Resilience.Polly
{
    /// <summary>
    /// Extension methods for configuring Polly resilience services in dependency injection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Polly resilience services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional configuration action.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddWorkflowForgePolly(
            this IServiceCollection services,
            Action<PollyMiddlewareOptions>? configureOptions = null)
        {
            // Register settings
            var settings = new PollyMiddlewareOptions();
            configureOptions?.Invoke(settings);
            services.TryAddSingleton(settings);

            // Register core Polly services
            services.TryAddSingleton<IPollyResilienceFactory, PollyResilienceFactory>();
            services.TryAddTransient<IWorkflowResilienceStrategy>(provider =>
                provider.GetRequiredService<IPollyResilienceFactory>().CreateDefaultStrategy());

            // Register middleware
            services.TryAddTransient<PollyMiddleware>(provider =>
                CreateMiddlewareFromSettings(provider, settings));

            return services;
        }

        /// <summary>
        /// Adds Polly resilience services using configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration section.</param>
        /// <param name="sectionName">The configuration section name.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddWorkflowForgePolly(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = "WorkflowForge:Polly")
        {
            // Bind configuration
            var settings = new PollyMiddlewareOptions();
            configuration.GetSection(sectionName).Bind(settings);

            return services.AddWorkflowForgePolly(opts =>
            {
                opts.Enabled = settings.Enabled;
                opts.Retry = settings.Retry;
                opts.CircuitBreaker = settings.CircuitBreaker;
                opts.Timeout = settings.Timeout;
                opts.RateLimiter = settings.RateLimiter;
                opts.EnableComprehensivePolicies = settings.EnableComprehensivePolicies;
                opts.DefaultTags = settings.DefaultTags;
                opts.EnableDetailedLogging = settings.EnableDetailedLogging;
            });
        }

        private static PollyMiddleware CreateMiddlewareFromSettings(IServiceProvider provider, PollyMiddlewareOptions settings)
        {
            var logger = provider.GetRequiredService<IWorkflowForgeLogger>();
            var factory = provider.GetRequiredService<IPollyResilienceFactory>();

            if (settings.EnableComprehensivePolicies)
            {
                return PollyMiddleware.WithComprehensivePolicy(
                    logger,
                    settings.Retry.MaxRetryAttempts,
                    settings.Retry.BaseDelay,
                    settings.CircuitBreaker.FailureThreshold,
                    settings.CircuitBreaker.BreakDuration,
                    settings.Timeout.DefaultTimeout);
            }
            else if (settings.Retry.IsEnabled)
            {
                return PollyMiddleware.WithRetryPolicy(
                    logger,
                    settings.Retry.MaxRetryAttempts,
                    settings.Retry.BaseDelay,
                    settings.Retry.BaseDelay); // MaxDelay doesn't exist, using BaseDelay
            }

            // Fallback to simple retry
            return PollyMiddleware.WithRetryPolicy(logger);
        }
    }

    /// <summary>
    /// Factory interface for creating Polly resilience strategies.
    /// </summary>
    internal interface IPollyResilienceFactory
    {
        /// <summary>
        /// Creates a default resilience strategy based on configuration.
        /// </summary>
        /// <returns>A configured resilience strategy.</returns>
        IWorkflowResilienceStrategy CreateDefaultStrategy();

        /// <summary>
        /// Creates a retry strategy with the specified parameters.
        /// </summary>
        /// <param name="maxAttempts">Maximum retry attempts.</param>
        /// <param name="baseDelay">Base delay for exponential backoff.</param>
        /// <param name="maxDelay">Maximum delay between retries.</param>
        /// <returns>A retry resilience strategy.</returns>
        IWorkflowResilienceStrategy CreateRetryStrategy(int maxAttempts, TimeSpan baseDelay, TimeSpan maxDelay);

        /// <summary>
        /// Creates a circuit breaker strategy with the specified parameters.
        /// </summary>
        /// <param name="failureThreshold">Failure threshold before opening.</param>
        /// <param name="durationOfBreak">Duration to keep circuit open.</param>
        /// <param name="minimumThroughput">Minimum throughput required.</param>
        /// <returns>A circuit breaker resilience strategy.</returns>
        IWorkflowResilienceStrategy CreateCircuitBreakerStrategy(int failureThreshold, TimeSpan durationOfBreak, int minimumThroughput);

        /// <summary>
        /// Creates a comprehensive strategy combining multiple policies.
        /// </summary>
        /// <param name="retrySettings">Retry configuration.</param>
        /// <param name="circuitBreakerSettings">Circuit breaker configuration.</param>
        /// <param name="timeoutSettings">Timeout configuration.</param>
        /// <returns>A comprehensive resilience strategy.</returns>
        IWorkflowResilienceStrategy CreateComprehensiveStrategy(
            PollyRetrySettings retrySettings,
            PollyCircuitBreakerSettings circuitBreakerSettings,
            PollyTimeoutSettings timeoutSettings);
    }

    /// <summary>
    /// Default implementation of the Polly resilience factory.
    /// </summary>
    internal sealed class PollyResilienceFactory : IPollyResilienceFactory
    {
        private readonly PollyMiddlewareOptions _settings;
        private readonly IWorkflowForgeLogger _logger;

        public PollyResilienceFactory(PollyMiddlewareOptions settings, IWorkflowForgeLogger logger)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IWorkflowResilienceStrategy CreateDefaultStrategy()
        {
            if (!_settings.Enabled)
            {
                return new NoOpResilienceStrategy();
            }

            if (_settings.EnableComprehensivePolicies)
            {
                return PollyResilienceStrategy.CreateComprehensivePolicy(
                    _settings.Retry.MaxRetryAttempts,
                    _settings.Retry.BaseDelay,
                    _settings.CircuitBreaker.FailureThreshold,
                    _settings.CircuitBreaker.BreakDuration,
                    _settings.Timeout.DefaultTimeout,
                    _logger);
            }

            return PollyResilienceStrategy.CreateRetryPolicy(
                _settings.Retry.MaxRetryAttempts,
                _settings.Retry.BaseDelay,
                _settings.Retry.BaseDelay, // MaxDelay doesn't exist, using BaseDelay
                _logger);
        }

        public IWorkflowResilienceStrategy CreateRetryStrategy(int maxAttempts, TimeSpan baseDelay, TimeSpan maxDelay)
        {
            return PollyResilienceStrategy.CreateRetryPolicy(maxAttempts, baseDelay, maxDelay, _logger);
        }

        public IWorkflowResilienceStrategy CreateCircuitBreakerStrategy(int failureThreshold, TimeSpan durationOfBreak, int minimumThroughput)
        {
            return PollyResilienceStrategy.CreateCircuitBreakerPolicy(failureThreshold, durationOfBreak, minimumThroughput, _logger);
        }

        public IWorkflowResilienceStrategy CreateComprehensiveStrategy(
            PollyRetrySettings retrySettings,
            PollyCircuitBreakerSettings circuitBreakerSettings,
            PollyTimeoutSettings timeoutSettings)
        {
            return PollyResilienceStrategy.CreateComprehensivePolicy(
                retrySettings.MaxRetryAttempts,
                retrySettings.BaseDelay,
                circuitBreakerSettings.FailureThreshold,
                circuitBreakerSettings.BreakDuration,
                timeoutSettings.DefaultTimeout,
                _logger);
        }
    }

    /// <summary>
    /// No-op resilience strategy for when Polly is disabled.
    /// </summary>
    internal sealed class NoOpResilienceStrategy : IWorkflowResilienceStrategy
    {
        public string Name => "NoOp";

        public Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken)
        {
            return operation();
        }

        public Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
        {
            return operation();
        }

        public Task<bool> ShouldRetryAsync(int attemptNumber, Exception? exception, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public TimeSpan GetRetryDelay(int attemptNumber, Exception? exception)
        {
            return TimeSpan.Zero;
        }
    }
}