using System;
using System.ComponentModel.DataAnnotations;

namespace WorkflowForge.Extensions.Resilience.Polly.Configurations
{
    /// <summary>
    /// Retry-specific configuration settings.
    /// </summary>
    public sealed class PollyRetrySettings
    {
        /// <summary>Gets or sets whether retry is enabled.</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>Gets or sets the maximum number of retry attempts.</summary>
        [Range(0, 100)]
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>Gets or sets the base delay for exponential backoff.</summary>
        public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>Gets or sets the maximum delay between retries.</summary>
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>Gets or sets whether to use jitter in retry delays.</summary>
        public bool UseJitter { get; set; } = true;

        /// <summary>Creates a deep clone of the current retry settings.</summary>
        public PollyRetrySettings Clone() => new()
        {
            IsEnabled = IsEnabled,
            MaxRetryAttempts = MaxRetryAttempts,
            BaseDelay = BaseDelay,
            MaxDelay = MaxDelay,
            UseJitter = UseJitter
        };

        /// <summary>Creates development-friendly retry settings with lenient configuration.</summary>
        public static PollyRetrySettings ForDevelopment() => new()
        {
            IsEnabled = true,
            MaxRetryAttempts = 2,
            BaseDelay = TimeSpan.FromMilliseconds(200),
            MaxDelay = TimeSpan.FromSeconds(5),
            UseJitter = true
        };

        /// <summary>Creates production-optimized retry settings with balanced configuration.</summary>
        public static PollyRetrySettings ForProduction() => new()
        {
            IsEnabled = true,
            MaxRetryAttempts = 5,
            BaseDelay = TimeSpan.FromMilliseconds(100),
            MaxDelay = TimeSpan.FromSeconds(10),
            UseJitter = true
        };

        /// <summary>Creates enterprise-grade retry settings with conservative configuration.</summary>
        public static PollyRetrySettings ForEnterprise() => new()
        {
            IsEnabled = true,
            MaxRetryAttempts = 3,
            BaseDelay = TimeSpan.FromMilliseconds(500),
            MaxDelay = TimeSpan.FromSeconds(30),
            UseJitter = true
        };

        /// <summary>Creates minimal retry settings with basic configuration.</summary>
        public static PollyRetrySettings Minimal() => new()
        {
            IsEnabled = true,
            MaxRetryAttempts = 1,
            BaseDelay = TimeSpan.FromMilliseconds(100),
            MaxDelay = TimeSpan.FromSeconds(1),
            UseJitter = false
        };
    }
}


