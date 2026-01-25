using System;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Resilience.Polly.Options;

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
        /// Adds Polly middleware based on configuration settings.
        /// </summary>
        /// <param name="foundry">The foundry to configure.</param>
        /// <param name="settings">The Polly settings to apply.</param>
        /// <returns>The foundry for method chaining.</returns>
        public static IWorkflowFoundry UsePollyFromSettings(
            this IWorkflowFoundry foundry,
            PollyMiddlewareOptions settings)
        {
            if (!settings.Enabled)
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
                    settings.CircuitBreaker.BreakDuration,
                    settings.Timeout.DefaultTimeout);

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
                        settings.Retry.BaseDelay); // MaxDelay doesn't exist, using BaseDelay
                }

                if (settings.CircuitBreaker.IsEnabled)
                {
                    foundry.UsePollyCircuitBreaker(
                        settings.CircuitBreaker.FailureThreshold,
                        settings.CircuitBreaker.BreakDuration);
                }

                if (settings.Timeout.IsEnabled)
                {
                    foundry.UsePollyTimeout(settings.Timeout.DefaultTimeout);
                }

                foundry.Logger.LogInformation(ResilienceLogMessages.IndividualResiliencePoliciesApplied);
            }

            return foundry;
        }

    }
}