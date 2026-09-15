using System.Collections.Generic;
using WorkflowForge.Options;

namespace WorkflowForge.Extensions.Audit.Options
{
    /// <summary>
    /// Configuration options for Audit middleware.
    /// Controls audit logging behavior and detail level.
    /// Inherits common options functionality from <see cref="WorkflowForgeOptionsBase"/>.
    /// </summary>
    public sealed class AuditMiddlewareOptions : WorkflowForgeOptionsBase
    {
        /// <summary>
        /// Default configuration section name for binding from appsettings.json.
        /// </summary>
        public const string DefaultSectionName = "WorkflowForge:Extensions:Audit";

        /// <summary>
        /// Initializes a new instance with default section name.
        /// </summary>
        public AuditMiddlewareOptions() : base(null, DefaultSectionName)
        {
        }

        /// <summary>
        /// Initializes a new instance with custom section name.
        /// </summary>
        /// <param name="sectionName">Custom configuration section name.</param>
        public AuditMiddlewareOptions(string sectionName) : base(sectionName, DefaultSectionName)
        {
        }

        /// <summary>
        /// Gets or sets the audit detail level.
        /// Controls how much information is captured in audit entries.
        /// Default is Standard.
        /// </summary>
        public AuditDetailLevel DetailLevel { get; set; } = AuditDetailLevel.Standard;

        /// <summary>
        /// Gets or sets whether to log data payloads in audit entries.
        /// When true, includes the operation input and output rendered as strings (can be very verbose).
        /// Payloads are also captured when <see cref="DetailLevel"/> is
        /// <see cref="AuditDetailLevel.Complete"/>, and never at
        /// <see cref="AuditDetailLevel.Minimal"/>.
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
        public override IList<string> Validate()
        {
            var errors = new List<string>();

            // No validation constraints currently needed for audit options
            // All boolean and enum values are inherently valid

            return errors;
        }

        /// <summary>
        /// Creates a deep copy of this options instance.
        /// </summary>
        /// <returns>A new instance with the same configuration values.</returns>
        public override object Clone()
        {
            return new AuditMiddlewareOptions(SectionName)
            {
                Enabled = Enabled,
                DetailLevel = DetailLevel,
                LogDataPayloads = LogDataPayloads,
                IncludeTimestamps = IncludeTimestamps,
                IncludeUserContext = IncludeUserContext
            };
        }
    }

    /// <summary>
    /// Defines the level of detail to capture in audit entries.
    /// </summary>
    public enum AuditDetailLevel
    {
        /// <summary>
        /// Event type, timestamp and workflow/operation name only. No metadata, duration or initiator.
        /// </summary>
        Minimal,

        /// <summary>
        /// Adds execution IDs, status, duration, audit timestamp and user context.
        /// </summary>
        Standard,

        /// <summary>
        /// Adds every foundry property to the entry metadata.
        /// </summary>
        Verbose,

        /// <summary>
        /// Adds the operation input and output payloads regardless of
        /// <see cref="AuditMiddlewareOptions.LogDataPayloads"/>.
        /// </summary>
        Complete
    }
}