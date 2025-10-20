using System;
using System.ComponentModel.DataAnnotations;

namespace WorkflowForge.Extensions.Resilience.Polly.Configurations
{
    /// <summary>
    /// Rate limiter-specific configuration settings.
    /// </summary>
    public sealed class PollyRateLimiterSettings
    {
        /// <summary>Gets or sets whether rate limiting is enabled.</summary>
        public bool IsEnabled { get; set; } = false;

        /// <summary>Gets or sets the permit limit.</summary>
        [Range(1, 10000)]
        public int PermitLimit { get; set; } = 10;

        /// <summary>Gets or sets the time window for rate limiting.</summary>
        public TimeSpan Window { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>Creates a deep clone of the current rate limiter settings.</summary>
        public PollyRateLimiterSettings Clone() => new()
        {
            IsEnabled = IsEnabled,
            PermitLimit = PermitLimit,
            Window = Window
        };

        /// <summary>Creates development-friendly rate limiter settings (disabled by default).</summary>
        public static PollyRateLimiterSettings ForDevelopment() => new() { IsEnabled = false };

        /// <summary>Creates production-optimized rate limiter settings with reasonable limits.</summary>
        public static PollyRateLimiterSettings ForProduction() => new()
        {
            IsEnabled = true,
            PermitLimit = 10,
            Window = TimeSpan.FromSeconds(1)
        };

        /// <summary>Creates enterprise-grade rate limiter settings with higher throughput limits.</summary>
        public static PollyRateLimiterSettings ForEnterprise() => new()
        {
            IsEnabled = true,
            PermitLimit = 20,
            Window = TimeSpan.FromSeconds(1)
        };
    }
}