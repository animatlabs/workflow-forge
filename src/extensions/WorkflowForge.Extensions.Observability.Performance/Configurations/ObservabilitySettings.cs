using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WorkflowForge.Extensions.Observability.Performance.Configurations
{
    /// <summary>
    /// Configuration settings for WorkflowForge observability extensions.
    /// Shared configuration section for Performance, Tracing, and HealthChecks extensions.
    /// </summary>
    public sealed class ObservabilitySettings : IValidatableObject
    {
        /// <summary>
        /// Configuration section name for binding.
        /// </summary>
        public const string SectionName = "WorkflowForge:Observability";

        /// <summary>
        /// Gets or sets whether performance metrics collection is enabled.
        /// When true, TimingMiddleware will be automatically registered.
        /// Requires WorkflowForge.Extensions.Observability.Performance package.
        /// Default is true.
        /// </summary>
        public bool EnablePerformance { get; set; } = true;

        /// <summary>
        /// Gets or sets whether distributed tracing is enabled.
        /// When true, tracing components will be automatically configured.
        /// Requires WorkflowForge.Extensions.Observability.OpenTelemetry package.
        /// Default is true.
        /// </summary>
        public bool EnableTracing { get; set; } = true;

        /// <summary>
        /// Gets or sets whether health checks are enabled.
        /// When true, health checks will be automatically registered.
        /// Requires WorkflowForge.Extensions.Observability.HealthChecks package.
        /// Default is true.
        /// </summary>
        public bool EnableHealthChecks { get; set; } = true;

        /// <summary>
        /// Validates the observability settings.
        /// </summary>
        /// <param name="validationContext">The validation context.</param>
        /// <returns>A collection of validation results.</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // All properties are boolean flags - no validation needed
            return new List<ValidationResult>();
        }

        /// <summary>
        /// Creates a deep copy of the current observability settings.
        /// </summary>
        /// <returns>A new ObservabilitySettings instance with copied values.</returns>
        public ObservabilitySettings Clone()
        {
            return new ObservabilitySettings
            {
                EnablePerformance = EnablePerformance,
                EnableTracing = EnableTracing,
                EnableHealthChecks = EnableHealthChecks
            };
        }
    }
}