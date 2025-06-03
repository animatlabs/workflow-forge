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
        /// <summary>
        /// Gets or sets whether Polly resilience is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the retry configuration.
        /// </summary>
        public PollyRetrySettings Retry { get; set; } = new();

        /// <summary>
        /// Gets or sets the circuit breaker configuration.
        /// </summary>
        public PollyCircuitBreakerSettings CircuitBreaker { get; set; } = new();

        /// <summary>
        /// Gets or sets the timeout configuration.
        /// </summary>
        public PollyTimeoutSettings Timeout { get; set; } = new();

        /// <summary>
        /// Gets or sets the rate limiter configuration.
        /// </summary>
        public PollyRateLimiterSettings RateLimiter { get; set; } = new();

        /// <summary>
        /// Gets or sets whether to enable comprehensive policies by default.
        /// </summary>
        public bool EnableComprehensivePolicies { get; set; } = false;

        /// <summary>
        /// Gets or sets the default tags to apply to all Polly metrics.
        /// </summary>
        public Dictionary<string, string> DefaultTags { get; set; } = new();

        /// <summary>
        /// Gets or sets whether to enable detailed Polly logging.
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = true;

        /// <summary>
        /// Validates the settings configuration.
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // Validate retry settings
            if (Retry.IsEnabled)
            {
                if (Retry.MaxRetryAttempts < 0 || Retry.MaxRetryAttempts > 100)
                {
                    results.Add(new ValidationResult(
                        "MaxRetryAttempts must be between 0 and 100",
                        new[] { nameof(Retry) + "." + nameof(Retry.MaxRetryAttempts) }));
                }

                if (Retry.BaseDelay < TimeSpan.Zero || Retry.BaseDelay > TimeSpan.FromMinutes(10))
                {
                    results.Add(new ValidationResult(
                        "BaseDelay must be between 0 and 10 minutes",
                        new[] { nameof(Retry) + "." + nameof(Retry.BaseDelay) }));
                }
            }

            // Validate circuit breaker settings
            if (CircuitBreaker.IsEnabled)
            {
                if (CircuitBreaker.FailureThreshold < 1 || CircuitBreaker.FailureThreshold > 1000)
                {
                    results.Add(new ValidationResult(
                        "FailureThreshold must be between 1 and 1000",
                        new[] { nameof(CircuitBreaker) + "." + nameof(CircuitBreaker.FailureThreshold) }));
                }
            }

            return results;
        }

        /// <summary>
        /// Creates a deep copy of the settings.
        /// </summary>
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

        /// <summary>
        /// Creates development-friendly settings with lenient policies.
        /// </summary>
        public static PollySettings ForDevelopment()
        {
            return new PollySettings
            {
                IsEnabled = true,
                Retry = PollyRetrySettings.ForDevelopment(),
                CircuitBreaker = PollyCircuitBreakerSettings.ForDevelopment(),
                Timeout = PollyTimeoutSettings.ForDevelopment(),
                RateLimiter = PollyRateLimiterSettings.ForDevelopment(),
                EnableComprehensivePolicies = false,
                EnableDetailedLogging = true
            };
        }

        /// <summary>
        /// Creates production-optimized settings with strict policies.
        /// </summary>
        public static PollySettings ForProduction()
        {
            return new PollySettings
            {
                IsEnabled = true,
                Retry = PollyRetrySettings.ForProduction(),
                CircuitBreaker = PollyCircuitBreakerSettings.ForProduction(),
                Timeout = PollyTimeoutSettings.ForProduction(),
                RateLimiter = PollyRateLimiterSettings.ForProduction(),
                EnableComprehensivePolicies = true,
                EnableDetailedLogging = false
            };
        }

        /// <summary>
        /// Creates enterprise-grade settings with comprehensive policies.
        /// </summary>
        public static PollySettings ForEnterprise()
        {
            return new PollySettings
            {
                IsEnabled = true,
                Retry = PollyRetrySettings.ForEnterprise(),
                CircuitBreaker = PollyCircuitBreakerSettings.ForEnterprise(),
                Timeout = PollyTimeoutSettings.ForEnterprise(),
                RateLimiter = PollyRateLimiterSettings.ForEnterprise(),
                EnableComprehensivePolicies = true,
                EnableDetailedLogging = true
            };
        }

        /// <summary>
        /// Creates minimal settings with basic retry only.
        /// </summary>
        public static PollySettings Minimal()
        {
            return new PollySettings
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

    /// <summary>
    /// Retry-specific configuration settings.
    /// </summary>
    public sealed class PollyRetrySettings
    {
        /// <summary>
        /// Gets or sets whether retry is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts.
        /// </summary>
        [Range(0, 100)]
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the base delay for exponential backoff.
        /// </summary>
        public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the maximum delay between retries.
        /// </summary>
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets whether to use jitter in retry delays.
        /// </summary>
        public bool UseJitter { get; set; } = true;

        /// <summary>
        /// Creates a deep clone of the current retry settings.
        /// </summary>
        /// <returns>A new instance with the same configuration values.</returns>
        public PollyRetrySettings Clone() => new()
        {
            IsEnabled = IsEnabled,
            MaxRetryAttempts = MaxRetryAttempts,
            BaseDelay = BaseDelay,
            MaxDelay = MaxDelay,
            UseJitter = UseJitter
        };

        /// <summary>
        /// Creates development-friendly retry settings with lenient configuration.
        /// </summary>
        /// <returns>Retry settings optimized for development scenarios.</returns>
        public static PollyRetrySettings ForDevelopment() => new()
        {
            IsEnabled = true,
            MaxRetryAttempts = 2,
            BaseDelay = TimeSpan.FromMilliseconds(200),
            MaxDelay = TimeSpan.FromSeconds(5),
            UseJitter = true
        };

        /// <summary>
        /// Creates production-optimized retry settings with balanced configuration.
        /// </summary>
        /// <returns>Retry settings optimized for production scenarios.</returns>
        public static PollyRetrySettings ForProduction() => new()
        {
            IsEnabled = true,
            MaxRetryAttempts = 5,
            BaseDelay = TimeSpan.FromMilliseconds(100),
            MaxDelay = TimeSpan.FromSeconds(10),
            UseJitter = true
        };

        /// <summary>
        /// Creates enterprise-grade retry settings with conservative configuration.
        /// </summary>
        /// <returns>Retry settings optimized for enterprise scenarios.</returns>
        public static PollyRetrySettings ForEnterprise() => new()
        {
            IsEnabled = true,
            MaxRetryAttempts = 3,
            BaseDelay = TimeSpan.FromMilliseconds(500),
            MaxDelay = TimeSpan.FromSeconds(30),
            UseJitter = true
        };

        /// <summary>
        /// Creates minimal retry settings with basic configuration.
        /// </summary>
        /// <returns>Retry settings with minimal retry attempts and delays.</returns>
        public static PollyRetrySettings Minimal() => new()
        {
            IsEnabled = true,
            MaxRetryAttempts = 1,
            BaseDelay = TimeSpan.FromMilliseconds(100),
            MaxDelay = TimeSpan.FromSeconds(1),
            UseJitter = false
        };
    }

    /// <summary>
    /// Circuit breaker-specific configuration settings.
    /// </summary>
    public sealed class PollyCircuitBreakerSettings
    {
        /// <summary>
        /// Gets or sets whether circuit breaker is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the failure threshold before opening the circuit.
        /// </summary>
        [Range(1, 1000)]
        public int FailureThreshold { get; set; } = 5;

        /// <summary>
        /// Gets or sets the duration to keep the circuit open.
        /// </summary>
        public TimeSpan DurationOfBreak { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the minimum throughput required.
        /// </summary>
        [Range(1, 100)]
        public int MinimumThroughput { get; set; } = 5;

        /// <summary>
        /// Creates a deep clone of the current circuit breaker settings.
        /// </summary>
        /// <returns>A new instance with the same configuration values.</returns>
        public PollyCircuitBreakerSettings Clone() => new()
        {
            IsEnabled = IsEnabled,
            FailureThreshold = FailureThreshold,
            DurationOfBreak = DurationOfBreak,
            MinimumThroughput = MinimumThroughput
        };

        /// <summary>
        /// Creates development-friendly circuit breaker settings (disabled by default).
        /// </summary>
        /// <returns>Circuit breaker settings optimized for development scenarios.</returns>
        public static PollyCircuitBreakerSettings ForDevelopment() => new()
        {
            IsEnabled = false
        };

        /// <summary>
        /// Creates production-optimized circuit breaker settings with strict thresholds.
        /// </summary>
        /// <returns>Circuit breaker settings optimized for production scenarios.</returns>
        public static PollyCircuitBreakerSettings ForProduction() => new()
        {
            IsEnabled = true,
            FailureThreshold = 3,
            DurationOfBreak = TimeSpan.FromSeconds(60),
            MinimumThroughput = 5
        };

        /// <summary>
        /// Creates enterprise-grade circuit breaker settings with balanced configuration.
        /// </summary>
        /// <returns>Circuit breaker settings optimized for enterprise scenarios.</returns>
        public static PollyCircuitBreakerSettings ForEnterprise() => new()
        {
            IsEnabled = true,
            FailureThreshold = 5,
            DurationOfBreak = TimeSpan.FromSeconds(30),
            MinimumThroughput = 5
        };
    }

    /// <summary>
    /// Timeout-specific configuration settings.
    /// </summary>
    public sealed class PollyTimeoutSettings
    {
        /// <summary>
        /// Gets or sets whether timeout is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the timeout duration.
        /// </summary>
        public TimeSpan TimeoutDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Creates a deep clone of the current timeout settings.
        /// </summary>
        /// <returns>A new instance with the same configuration values.</returns>
        public PollyTimeoutSettings Clone() => new()
        {
            IsEnabled = IsEnabled,
            TimeoutDuration = TimeoutDuration
        };

        /// <summary>
        /// Creates development-friendly timeout settings with generous timeouts.
        /// </summary>
        /// <returns>Timeout settings optimized for development scenarios.</returns>
        public static PollyTimeoutSettings ForDevelopment() => new()
        {
            IsEnabled = true,
            TimeoutDuration = TimeSpan.FromMinutes(2)
        };

        /// <summary>
        /// Creates production-optimized timeout settings with strict timeouts.
        /// </summary>
        /// <returns>Timeout settings optimized for production scenarios.</returns>
        public static PollyTimeoutSettings ForProduction() => new()
        {
            IsEnabled = true,
            TimeoutDuration = TimeSpan.FromSeconds(10)
        };

        /// <summary>
        /// Creates enterprise-grade timeout settings with balanced timeouts.
        /// </summary>
        /// <returns>Timeout settings optimized for enterprise scenarios.</returns>
        public static PollyTimeoutSettings ForEnterprise() => new()
        {
            IsEnabled = true,
            TimeoutDuration = TimeSpan.FromSeconds(30)
        };
    }

    /// <summary>
    /// Rate limiter-specific configuration settings.
    /// </summary>
    public sealed class PollyRateLimiterSettings
    {
        /// <summary>
        /// Gets or sets whether rate limiting is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the permit limit.
        /// </summary>
        [Range(1, 10000)]
        public int PermitLimit { get; set; } = 10;

        /// <summary>
        /// Gets or sets the time window for rate limiting.
        /// </summary>
        public TimeSpan Window { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Creates a deep clone of the current rate limiter settings.
        /// </summary>
        /// <returns>A new instance with the same configuration values.</returns>
        public PollyRateLimiterSettings Clone() => new()
        {
            IsEnabled = IsEnabled,
            PermitLimit = PermitLimit,
            Window = Window
        };

        /// <summary>
        /// Creates development-friendly rate limiter settings (disabled by default).
        /// </summary>
        /// <returns>Rate limiter settings optimized for development scenarios.</returns>
        public static PollyRateLimiterSettings ForDevelopment() => new()
        {
            IsEnabled = false
        };

        /// <summary>
        /// Creates production-optimized rate limiter settings with reasonable limits.
        /// </summary>
        /// <returns>Rate limiter settings optimized for production scenarios.</returns>
        public static PollyRateLimiterSettings ForProduction() => new()
        {
            IsEnabled = true,
            PermitLimit = 10,
            Window = TimeSpan.FromSeconds(1)
        };

        /// <summary>
        /// Creates enterprise-grade rate limiter settings with higher throughput limits.
        /// </summary>
        /// <returns>Rate limiter settings optimized for enterprise scenarios.</returns>
        public static PollyRateLimiterSettings ForEnterprise() => new()
        {
            IsEnabled = true,
            PermitLimit = 20,
            Window = TimeSpan.FromSeconds(1)
        };
    }
} 