using System;
using System.ComponentModel.DataAnnotations;

namespace WorkflowForge.Extensions.Resilience.Polly.Configurations
{
    /// <summary>
    /// Circuit breaker-specific configuration settings.
    /// </summary>
    public sealed class PollyCircuitBreakerSettings
    {
        /// <summary>Gets or sets whether circuit breaker is enabled.</summary>
        public bool IsEnabled { get; set; } = false;

        /// <summary>Gets or sets the failure threshold before opening the circuit.</summary>
        [Range(1, 1000)]
        public int FailureThreshold { get; set; } = 5;

        /// <summary>Gets or sets the duration to keep the circuit open.</summary>
        public TimeSpan DurationOfBreak { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>Gets or sets the minimum throughput required.</summary>
        [Range(1, 100)]
        public int MinimumThroughput { get; set; } = 5;

        /// <summary>Creates a deep clone of the current circuit breaker settings.</summary>
        public PollyCircuitBreakerSettings Clone() => new()
        {
            IsEnabled = IsEnabled,
            FailureThreshold = FailureThreshold,
            DurationOfBreak = DurationOfBreak,
            MinimumThroughput = MinimumThroughput
        };

        /// <summary>Creates development-friendly circuit breaker settings (disabled by default).</summary>
        public static PollyCircuitBreakerSettings ForDevelopment() => new() { IsEnabled = false };

        /// <summary>Creates production-optimized circuit breaker settings with strict thresholds.</summary>
        public static PollyCircuitBreakerSettings ForProduction() => new()
        {
            IsEnabled = true,
            FailureThreshold = 3,
            DurationOfBreak = TimeSpan.FromSeconds(60),
            MinimumThroughput = 5
        };

        /// <summary>Creates enterprise-grade circuit breaker settings with balanced configuration.</summary>
        public static PollyCircuitBreakerSettings ForEnterprise() => new()
        {
            IsEnabled = true,
            FailureThreshold = 5,
            DurationOfBreak = TimeSpan.FromSeconds(30),
            MinimumThroughput = 5
        };
    }
}