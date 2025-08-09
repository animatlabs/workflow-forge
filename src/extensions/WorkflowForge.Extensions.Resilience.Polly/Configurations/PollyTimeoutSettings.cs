using System;

namespace WorkflowForge.Extensions.Resilience.Polly.Configurations
{
    /// <summary>
    /// Timeout-specific configuration settings.
    /// </summary>
    public sealed class PollyTimeoutSettings
    {
        /// <summary>Gets or sets whether timeout is enabled.</summary>
        public bool IsEnabled { get; set; } = false;

        /// <summary>Gets or sets the timeout duration.</summary>
        public TimeSpan TimeoutDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>Creates a deep clone of the current timeout settings.</summary>
        public PollyTimeoutSettings Clone() => new()
        {
            IsEnabled = IsEnabled,
            TimeoutDuration = TimeoutDuration
        };

        /// <summary>Creates development-friendly timeout settings with generous timeouts.</summary>
        public static PollyTimeoutSettings ForDevelopment() => new() { IsEnabled = true, TimeoutDuration = TimeSpan.FromMinutes(2) };

        /// <summary>Creates production-optimized timeout settings with strict timeouts.</summary>
        public static PollyTimeoutSettings ForProduction() => new() { IsEnabled = true, TimeoutDuration = TimeSpan.FromSeconds(10) };

        /// <summary>Creates enterprise-grade timeout settings with balanced timeouts.</summary>
        public static PollyTimeoutSettings ForEnterprise() => new() { IsEnabled = true, TimeoutDuration = TimeSpan.FromSeconds(30) };
    }
}


