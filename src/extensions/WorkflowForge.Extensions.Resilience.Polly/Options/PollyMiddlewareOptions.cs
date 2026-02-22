using System;
using System.Collections.Generic;
using WorkflowForge.Options;

namespace WorkflowForge.Extensions.Resilience.Polly.Options
{
    /// <summary>
    /// Configuration options for Polly resilience middleware.
    /// Provides comprehensive resilience configuration for workflows including retry, circuit breaker, timeout, and rate limiting.
    /// Inherits common options functionality from <see cref="WorkflowForgeOptionsBase"/>.
    /// </summary>
    public sealed class PollyMiddlewareOptions : WorkflowForgeOptionsBase
    {
        /// <summary>
        /// Default configuration section name for binding from appsettings.json.
        /// </summary>
        public const string DefaultSectionName = "WorkflowForge:Extensions:Polly";

        /// <summary>
        /// Initializes a new instance with default section name.
        /// </summary>
        public PollyMiddlewareOptions() : base(null, DefaultSectionName)
        {
        }

        /// <summary>
        /// Initializes a new instance with custom section name.
        /// </summary>
        /// <param name="sectionName">Custom configuration section name.</param>
        public PollyMiddlewareOptions(string sectionName) : base(sectionName, DefaultSectionName)
        {
        }

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

        /// <summary>
        /// Validates the configuration settings and returns any validation errors.
        /// </summary>
        /// <returns>A list of validation error messages, empty if valid.</returns>
        public override IList<string> Validate()
        {
            var errors = new List<string>();
            ValidateRetry(errors);
            ValidateCircuitBreaker(errors);
            ValidateTimeout(errors);
            ValidateRateLimiter(errors);
            return errors;
        }

        private void ValidateRetry(IList<string> errors)
        {
            if (!Retry.IsEnabled)
                return;

            if (Retry.MaxRetryAttempts < 0 || Retry.MaxRetryAttempts > 100)
                errors.Add($"{SectionName}:Retry:MaxRetryAttempts must be between 0 and 100 (current value: {Retry.MaxRetryAttempts})");

            if (Retry.BaseDelay < TimeSpan.Zero || Retry.BaseDelay > TimeSpan.FromMinutes(10))
                errors.Add($"{SectionName}:Retry:BaseDelay must be between 0 and 10 minutes (current value: {Retry.BaseDelay})");
        }

        private void ValidateCircuitBreaker(IList<string> errors)
        {
            if (!CircuitBreaker.IsEnabled)
                return;

            if (CircuitBreaker.FailureThreshold < 1 || CircuitBreaker.FailureThreshold > 1000)
                errors.Add($"{SectionName}:CircuitBreaker:FailureThreshold must be between 1 and 1000 (current value: {CircuitBreaker.FailureThreshold})");

            if (CircuitBreaker.BreakDuration < TimeSpan.Zero || CircuitBreaker.BreakDuration > TimeSpan.FromHours(1))
                errors.Add($"{SectionName}:CircuitBreaker:BreakDuration must be between 0 and 1 hour (current value: {CircuitBreaker.BreakDuration})");
        }

        private void ValidateTimeout(IList<string> errors)
        {
            if (!Timeout.IsEnabled)
                return;

            if (Timeout.DefaultTimeout <= TimeSpan.Zero || Timeout.DefaultTimeout > TimeSpan.FromHours(24))
                errors.Add($"{SectionName}:Timeout:DefaultTimeout must be between 0 and 24 hours (current value: {Timeout.DefaultTimeout})");
        }

        private void ValidateRateLimiter(IList<string> errors)
        {
            if (!RateLimiter.IsEnabled)
                return;

            if (RateLimiter.PermitLimit < 1 || RateLimiter.PermitLimit > 1000000)
                errors.Add($"{SectionName}:RateLimiter:PermitLimit must be between 1 and 1000000 (current value: {RateLimiter.PermitLimit})");
        }

        /// <summary>
        /// Creates a deep copy of this options instance.
        /// </summary>
        /// <returns>A new instance with the same configuration values.</returns>
        public override object Clone()
        {
            return new PollyMiddlewareOptions(SectionName)
            {
                Enabled = Enabled,
                Retry = Retry.Clone(),
                CircuitBreaker = CircuitBreaker.Clone(),
                Timeout = Timeout.Clone(),
                RateLimiter = RateLimiter.Clone(),
                EnableComprehensivePolicies = EnableComprehensivePolicies,
                DefaultTags = new Dictionary<string, string>(DefaultTags),
                EnableDetailedLogging = EnableDetailedLogging
            };
        }
    }

    // Keep nested settings classes in same file for consistency with original

    #region Nested Settings Classes

    /// <summary>
    /// Configuration settings for Polly retry behavior.
    /// </summary>
    public sealed class PollyRetrySettings
    {
        /// <summary>Gets or sets whether retry is enabled.</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>Gets or sets the maximum number of retry attempts.</summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>Gets or sets the base delay between retries.</summary>
        public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>Gets or sets the backoff strategy (Linear, Exponential, etc.).</summary>
        public string BackoffType { get; set; } = "Exponential";

        /// <summary>Gets or sets whether to use jitter for retry delays.</summary>
        public bool UseJitter { get; set; } = true;

        public PollyRetrySettings Clone() => new()
        {
            IsEnabled = IsEnabled,
            MaxRetryAttempts = MaxRetryAttempts,
            BaseDelay = BaseDelay,
            BackoffType = BackoffType,
            UseJitter = UseJitter
        };
    }

    /// <summary>
    /// Configuration settings for Polly circuit breaker.
    /// </summary>
    public sealed class PollyCircuitBreakerSettings
    {
        /// <summary>Gets or sets whether circuit breaker is enabled.</summary>
        public bool IsEnabled { get; set; } = false;

        /// <summary>Gets or sets the failure threshold before breaking.</summary>
        public int FailureThreshold { get; set; } = 5;

        /// <summary>Gets or sets the duration to keep the circuit open.</summary>
        public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>Gets or sets the sampling duration for failure rate calculation.</summary>
        public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>Gets or sets the minimum throughput before circuit can break.</summary>
        public int MinimumThroughput { get; set; } = 10;

        public PollyCircuitBreakerSettings Clone() => new()
        {
            IsEnabled = IsEnabled,
            FailureThreshold = FailureThreshold,
            BreakDuration = BreakDuration,
            SamplingDuration = SamplingDuration,
            MinimumThroughput = MinimumThroughput
        };
    }

    /// <summary>
    /// Configuration settings for Polly timeout.
    /// </summary>
    public sealed class PollyTimeoutSettings
    {
        /// <summary>Gets or sets whether timeout is enabled.</summary>
        public bool IsEnabled { get; set; } = false;

        /// <summary>Gets or sets the default timeout duration.</summary>
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>Gets or sets whether to use optimistic timeout (cooperative cancellation).</summary>
        public bool UseOptimisticTimeout { get; set; } = true;

        public PollyTimeoutSettings Clone() => new()
        {
            IsEnabled = IsEnabled,
            DefaultTimeout = DefaultTimeout,
            UseOptimisticTimeout = UseOptimisticTimeout
        };
    }

    /// <summary>
    /// Configuration settings for Polly rate limiter.
    /// </summary>
    public sealed class PollyRateLimiterSettings
    {
        /// <summary>Gets or sets whether rate limiting is enabled.</summary>
        public bool IsEnabled { get; set; } = false;

        /// <summary>Gets or sets the maximum number of permits.</summary>
        public int PermitLimit { get; set; } = 100;

        /// <summary>Gets or sets the time window for rate limiting.</summary>
        public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>Gets or sets the queue limit for waiting requests.</summary>
        public int QueueLimit { get; set; } = 0;

        public PollyRateLimiterSettings Clone() => new()
        {
            IsEnabled = IsEnabled,
            PermitLimit = PermitLimit,
            Window = Window,
            QueueLimit = QueueLimit
        };
    }

    #endregion Nested Settings Classes
}
