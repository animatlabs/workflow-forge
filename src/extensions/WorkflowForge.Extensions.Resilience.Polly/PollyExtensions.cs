using Polly;
using System;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Resilience.Polly.Configurations;

namespace WorkflowForge.Extensions.Resilience.Polly
{
    /// <summary>
    /// Extension methods for integrating Polly middleware with WorkflowForge foundries
    /// Provides fluent API for adding enterprise-grade fault tolerance to workflow foundries.
    /// </summary>
    public static class PollyExtensions
    {
        /// <summary>
        /// Adds Polly retry middleware to the foundry with exponential backoff.
        /// </summary>
        /// <param name="foundry">The foundry to configure.</param>
        /// <param name="maxRetryAttempts">Maximum number of retry attempts.</param>
        /// <param name="baseDelay">Base delay for exponential backoff.</param>
        /// <param name="maxDelay">Maximum delay between retries.</param>
        /// <returns>The foundry for method chaining.</returns>
        public static IWorkflowFoundry UsePollyRetry(
            this IWorkflowFoundry foundry,
            int maxRetryAttempts = 3,
            TimeSpan? baseDelay = null,
            TimeSpan? maxDelay = null)
        {
            var middleware = PollyMiddleware.WithRetryPolicy(
                foundry.Logger,
                maxRetryAttempts,
                baseDelay,
                maxDelay);

            foundry.AddMiddleware(middleware);
            return foundry;
        }

        /// <summary>
        /// Adds Polly circuit breaker middleware to the foundry.
        /// </summary>
        /// <param name="foundry">The foundry to configure.</param>
        /// <param name="failureThreshold">Number of failures before opening the circuit.</param>
        /// <param name="durationOfBreak">Duration to keep the circuit open.</param>
        /// <returns>The foundry for method chaining.</returns>
        public static IWorkflowFoundry UsePollyCircuitBreaker(
            this IWorkflowFoundry foundry,
            int failureThreshold = 5,
            TimeSpan? durationOfBreak = null)
        {
            var middleware = PollyMiddleware.WithCircuitBreakerPolicy(
                foundry.Logger,
                failureThreshold,
                durationOfBreak);

            foundry.AddMiddleware(middleware);
            return foundry;
        }

        /// <summary>
        /// Adds Polly timeout middleware to the foundry.
        /// </summary>
        /// <param name="foundry">The foundry to configure.</param>
        /// <param name="timeout">The timeout duration.</param>
        /// <returns>The foundry for method chaining.</returns>
        public static IWorkflowFoundry UsePollyTimeout(
            this IWorkflowFoundry foundry,
            TimeSpan timeout)
        {
            var middleware = PollyMiddleware.WithTimeoutPolicy(
                foundry.Logger,
                timeout);

            foundry.AddMiddleware(middleware);
            return foundry;
        }

        /// <summary>
        /// Adds comprehensive Polly middleware combining retry, circuit breaker, and timeout.
        /// </summary>
        /// <param name="foundry">The foundry to configure.</param>
        /// <param name="maxRetryAttempts">Maximum number of retry attempts.</param>
        /// <param name="baseDelay">Base delay for exponential backoff.</param>
        /// <param name="circuitBreakerThreshold">Circuit breaker failure threshold.</param>
        /// <param name="circuitBreakerDuration">Circuit breaker open duration.</param>
        /// <param name="timeoutDuration">Operation timeout duration.</param>
        /// <returns>The foundry for method chaining.</returns>
        public static IWorkflowFoundry UsePollyComprehensive(
            this IWorkflowFoundry foundry,
            int maxRetryAttempts = 3,
            TimeSpan? baseDelay = null,
            int circuitBreakerThreshold = 5,
            TimeSpan? circuitBreakerDuration = null,
            TimeSpan? timeoutDuration = null)
        {
            var middleware = PollyMiddleware.WithComprehensivePolicy(
                foundry.Logger,
                maxRetryAttempts,
                baseDelay,
                circuitBreakerThreshold,
                circuitBreakerDuration,
                timeoutDuration);

            foundry.AddMiddleware(middleware);
            return foundry;
        }

        /// <summary>
        /// Adds a custom Polly pipeline as middleware to the foundry.
        /// </summary>
        /// <param name="foundry">The foundry to configure.</param>
        /// <param name="pipeline">The custom Polly pipeline to apply.</param>
        /// <param name="name">Optional name for the middleware.</param>
        /// <returns>The foundry for method chaining.</returns>
        public static IWorkflowFoundry UsePollyPipeline(
            this IWorkflowFoundry foundry,
            ResiliencePipeline pipeline,
            string? name = null)
        {
            var middleware = new PollyMiddleware(pipeline, foundry.Logger, name);
            foundry.AddMiddleware(middleware);
            return foundry;
        }

        /// <summary>
        /// Adds Polly middleware based on configuration settings.
        /// </summary>
        /// <param name="foundry">The foundry to configure.</param>
        /// <param name="settings">The Polly settings to apply.</param>
        /// <returns>The foundry for method chaining.</returns>
        public static IWorkflowFoundry UsePollyFromSettings(
            this IWorkflowFoundry foundry,
            PollySettings settings)
        {
            if (!settings.IsEnabled)
            {
                foundry.Logger.LogInformation(ResilienceLogMessages.ResilienceDisabled);
                return foundry;
            }

            if (settings.EnableComprehensivePolicies)
            {
                foundry.UsePollyComprehensive(
                    settings.Retry.MaxRetryAttempts,
                    settings.Retry.BaseDelay,
                    settings.CircuitBreaker.FailureThreshold,
                    settings.CircuitBreaker.DurationOfBreak,
                    settings.Timeout.TimeoutDuration);

                foundry.Logger.LogInformation(ResilienceLogMessages.ComprehensiveResiliencePoliciesApplied);
            }
            else
            {
                // Apply individual policies based on settings
                if (settings.Retry.IsEnabled)
                {
                    foundry.UsePollyRetry(
                        settings.Retry.MaxRetryAttempts,
                        settings.Retry.BaseDelay,
                        settings.Retry.MaxDelay);
                }

                if (settings.CircuitBreaker.IsEnabled)
                {
                    foundry.UsePollyCircuitBreaker(
                        settings.CircuitBreaker.FailureThreshold,
                        settings.CircuitBreaker.DurationOfBreak);
                }

                if (settings.Timeout.IsEnabled)
                {
                    foundry.UsePollyTimeout(settings.Timeout.TimeoutDuration);
                }

                foundry.Logger.LogInformation(ResilienceLogMessages.IndividualResiliencePoliciesApplied);
            }

            return foundry;
        }

        /// <summary>
        /// Adds enterprise-grade resilience configuration with predefined best practices.
        /// Includes retry with exponential backoff, circuit breaker, timeout, and rate limiting.
        /// </summary>
        /// <param name="foundry">The foundry to configure.</param>
        /// <returns>The foundry for method chaining.</returns>
        public static IWorkflowFoundry UsePollyEnterpriseResilience(this IWorkflowFoundry foundry)
        {
            var settings = PollySettings.ForEnterprise();
            return foundry.UsePollyFromSettings(settings);
        }

        /// <summary>
        /// Adds development-friendly resilience configuration with more lenient settings.
        /// </summary>
        /// <param name="foundry">The foundry to configure.</param>
        /// <returns>The foundry for method chaining.</returns>
        public static IWorkflowFoundry UsePollyDevelopmentResilience(this IWorkflowFoundry foundry)
        {
            var settings = PollySettings.ForDevelopment();
            return foundry.UsePollyFromSettings(settings);
        }

        /// <summary>
        /// Adds production-optimized resilience configuration with strict settings.
        /// </summary>
        /// <param name="foundry">The foundry to configure.</param>
        /// <returns>The foundry for method chaining.</returns>
        public static IWorkflowFoundry UsePollyProductionResilience(this IWorkflowFoundry foundry)
        {
            var settings = PollySettings.ForProduction();
            return foundry.UsePollyFromSettings(settings);
        }

        /// <summary>
        /// Adds minimal resilience configuration with basic retry only.
        /// </summary>
        /// <param name="foundry">The foundry to configure.</param>
        /// <returns>The foundry for method chaining.</returns>
        public static IWorkflowFoundry UsePollyMinimalResilience(this IWorkflowFoundry foundry)
        {
            var settings = PollySettings.Minimal();
            return foundry.UsePollyFromSettings(settings);
        }
    }
}