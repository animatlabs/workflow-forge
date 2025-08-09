using System;
using System.Collections.Generic;

namespace WorkflowForge.Configurations
{
    /// <summary>
    /// Primary configuration settings for the WorkflowForge workflow engine.
    /// Provides dependency-free configuration for core WorkflowForge functionality.
    /// Extended settings (metrics, telemetry, health checks) are handled by their respective extension packages.
    /// </summary>
    public sealed class WorkflowForgeConfiguration
    {
        /// <summary>
        /// Configuration section name for binding.
        /// </summary>
        public const string SectionName = "WorkflowForge";

        /// <summary>
        /// Gets or sets whether automatic restoration should be performed when a workflow fails.
        /// Default is true for production resilience and error recovery.
        /// </summary>
        public bool AutoRestore { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of concurrent operations across the entire framework.
        /// This acts as a global limit that overrides operation-specific concurrency settings.
        /// Must be between 1 and 1000. Default is processor count * 2.
        /// </summary>
        public int MaxConcurrentOperations { get; set; } = Environment.ProcessorCount * 2;

        /// <summary>
        /// Validates the configuration settings and returns any validation errors.
        /// </summary>
        /// <returns>A list of validation error messages, empty if valid.</returns>
        public IList<string> Validate()
        {
            var errors = new List<string>();

            // AutoRestore doesn't need validation - it's a simple boolean

            if (MaxConcurrentOperations < 1 || MaxConcurrentOperations > 1000)
            {
                errors.Add("MaxConcurrentOperations must be between 1 and 1000");
            }

            return errors;
        }

        /// <summary>
        /// Creates a deep copy of the current settings.
        /// </summary>
        /// <returns>A new WorkflowForgeSettings instance with copied values.</returns>
        public WorkflowForgeConfiguration Clone()
        {
            return new WorkflowForgeConfiguration
            {
                AutoRestore = AutoRestore,
                MaxConcurrentOperations = MaxConcurrentOperations
            };
        }
    }
}