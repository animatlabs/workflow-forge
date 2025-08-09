using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WorkflowForge.Extensions.Resilience.Polly.Configurations
{
    /// <summary>
    /// Configuration settings for Polly integration with WorkflowForge.
    /// Provides comprehensive resilience configuration options.
    /// </summary>
    public sealed class PollySettings : IValidatableObject
    {
        /// <summary>Gets or sets whether Polly resilience is enabled.</summary>
        public bool IsEnabled { get; set; } = true;
        /// <summary>Gets or sets the retry configuration.</summary>
        public PollyRetrySettings Retry { get; set; } = new();
        /// <summary>Gets or sets the circuit breaker configuration.</summary>
        public PollyCircuitBreakerSettings CircuitBreaker { get; set; } = new();
        /// <summary>Gets or sets the timeout configuration.</summary>
        public PollyTimeoutSettings Timeout { get; set; } = new();
        /// <summary>Gets or sets the rate limiter configuration.</summary>
        public PollyRateLimiterSettings RateLimiter { get; set; } = new();
        /// <summary>Gets or sets whether to enable comprehensive policies by default.</summary>
        public bool EnableComprehensivePolicies { get; set; } = false;
        /// <summary>Gets or sets the default tags to apply to all Polly metrics.</summary>
        public Dictionary<string, string> DefaultTags { get; set; } = new();
        /// <summary>Gets or sets whether to enable detailed Polly logging.</summary>
        public bool EnableDetailedLogging { get; set; } = true;

        /// <inheritdoc />
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Retry.IsEnabled)
            {
                if (Retry.MaxRetryAttempts < 0 || Retry.MaxRetryAttempts > 100)
                {
                    results.Add(new ValidationResult("MaxRetryAttempts must be between 0 and 100", new[] { nameof(Retry) + "." + nameof(Retry.MaxRetryAttempts) }));
                }
                if (Retry.BaseDelay < TimeSpan.Zero || Retry.BaseDelay > TimeSpan.FromMinutes(10))
                {
                    results.Add(new ValidationResult("BaseDelay must be between 0 and 10 minutes", new[] { nameof(Retry) + "." + nameof(Retry.BaseDelay) }));
                }
            }
            if (CircuitBreaker.IsEnabled)
            {
                if (CircuitBreaker.FailureThreshold < 1 || CircuitBreaker.FailureThreshold > 1000)
                {
                    results.Add(new ValidationResult("FailureThreshold must be between 1 and 1000", new[] { nameof(CircuitBreaker) + "." + nameof(CircuitBreaker.FailureThreshold) }));
                }
            }
            return results;
        }

        /// <summary>Creates a deep copy of the settings.</summary>
        public PollySettings Clone()
        {
            return new PollySettings
            {
                IsEnabled = IsEnabled,
                Retry = Retry.Clone(),
                CircuitBreaker = CircuitBreaker.Clone(),
                Timeout = Timeout.Clone(),
                RateLimiter = RateLimiter.Clone(),
                EnableComprehensivePolicies = EnableComprehensivePolicies,
                DefaultTags = new Dictionary<string, string>(DefaultTags),
                EnableDetailedLogging = EnableDetailedLogging
            };
        }

        /// <summary>Creates development-friendly settings with lenient policies.</summary>
        public static PollySettings ForDevelopment() => new()
        {
            IsEnabled = true,
            Retry = PollyRetrySettings.ForDevelopment(),
            CircuitBreaker = PollyCircuitBreakerSettings.ForDevelopment(),
            Timeout = PollyTimeoutSettings.ForDevelopment(),
            RateLimiter = PollyRateLimiterSettings.ForDevelopment(),
            EnableComprehensivePolicies = false,
            EnableDetailedLogging = true
        };

        /// <summary>Creates production-optimized settings with strict policies.</summary>
        public static PollySettings ForProduction() => new()
        {
            IsEnabled = true,
            Retry = PollyRetrySettings.ForProduction(),
            CircuitBreaker = PollyCircuitBreakerSettings.ForProduction(),
            Timeout = PollyTimeoutSettings.ForProduction(),
            RateLimiter = PollyRateLimiterSettings.ForProduction(),
            EnableComprehensivePolicies = true,
            EnableDetailedLogging = false
        };

        /// <summary>Creates enterprise-grade settings with comprehensive policies.</summary>
        public static PollySettings ForEnterprise() => new()
        {
            IsEnabled = true,
            Retry = PollyRetrySettings.ForEnterprise(),
            CircuitBreaker = PollyCircuitBreakerSettings.ForEnterprise(),
            Timeout = PollyTimeoutSettings.ForEnterprise(),
            RateLimiter = PollyRateLimiterSettings.ForEnterprise(),
            EnableComprehensivePolicies = true,
            EnableDetailedLogging = true
        };

        /// <summary>Creates minimal settings with basic retry only.</summary>
        public static PollySettings Minimal() => new()
        {
            IsEnabled = true,
            Retry = PollyRetrySettings.Minimal(),
            CircuitBreaker = new PollyCircuitBreakerSettings { IsEnabled = false },
            Timeout = new PollyTimeoutSettings { IsEnabled = false },
            RateLimiter = new PollyRateLimiterSettings { IsEnabled = false },
            EnableComprehensivePolicies = false,
            EnableDetailedLogging = false
        };
    }
}