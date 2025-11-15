using System;
using System.Collections.Generic;

namespace WorkflowForge.Extensions.Audit.Options
{
    /// <summary>
    /// Configuration options for Audit middleware.
    /// Controls audit logging behavior and detail level.
    /// Zero-dependency POCO for configuration binding.
    /// </summary>
    public sealed class AuditMiddlewareOptions
    {
        /// <summary>
        /// Default configuration section name for binding from appsettings.json.
        /// This is the default value; users can specify a custom section name when binding.
        /// </summary>
        public const string DefaultSectionName = "WorkflowForge:Extensions:Audit";

        /// <summary>
        /// Gets the configuration section name for this instance.
        /// Can be customized via constructor for non-standard configuration layouts.
        /// </summary>
        public string SectionName { get; }

        /// <summary>
        /// Initializes a new instance with default section name.
        /// </summary>
        public AuditMiddlewareOptions() : this(DefaultSectionName)
        {
        }

        /// <summary>
        /// Initializes a new instance with custom section name.
        /// </summary>
        /// <param name="sectionName">Custom configuration section name.</param>
        public AuditMiddlewareOptions(string sectionName)
        {
            SectionName = sectionName ?? DefaultSectionName;
        }

        /// <summary>
        /// Gets or sets whether audit middleware is enabled.
        /// When true, workflow and operation events are logged to the audit provider.
        /// When false, middleware is not registered.
        /// Default is true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the audit detail level.
        /// Controls how much information is captured in audit entries.
        /// Default is Standard.
        /// </summary>
        public AuditDetailLevel DetailLevel { get; set; } = AuditDetailLevel.Standard;

        /// <summary>
        /// Gets or sets whether to log data payloads in audit entries.
        /// When true, includes operation input/output data (can be very verbose).
        /// When false, only logs metadata and event information.
        /// Default is false.
        /// </summary>
        public bool LogDataPayloads { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to include timestamps in audit entries.
        /// When true, all audit entries include precise timestamps.
        /// Default is true.
        /// </summary>
        public bool IncludeTimestamps { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include user context in audit entries.
        /// When true, captures user/principal information if available.
        /// Default is true.
        /// </summary>
        public bool IncludeUserContext { get; set; } = true;

        /// <summary>
        /// Validates the configuration settings and returns any validation errors.
        /// </summary>
        /// <returns>A list of validation error messages, empty if valid.</returns>
        public IList<string> Validate()
        {
            var errors = new List<string>();
            
            // No validation constraints currently needed for audit options
            // All boolean and enum values are inherently valid
            
            return errors;
        }
    }

    /// <summary>
    /// Defines the level of detail to capture in audit entries.
    /// </summary>
    public enum AuditDetailLevel
    {
        /// <summary>
        /// Minimal audit information (event type, timestamp, workflow/operation name).
        /// </summary>
        Minimal,

        /// <summary>
        /// Standard audit information (includes execution IDs, status, duration).
        /// </summary>
        Standard,

        /// <summary>
        /// Verbose audit information (includes all standard info plus properties, metadata).
        /// </summary>
        Verbose,

        /// <summary>
        /// Complete audit information (includes everything, including data payloads if enabled).
        /// </summary>
        Complete
    }
}



