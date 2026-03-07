using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkflowForge.Options.Middleware
{
    /// <summary>
    /// Configuration options for logging middleware.
    /// Controls logging behavior and verbosity.
    /// Zero-dependency POCO for configuration binding.
    /// </summary>
    public sealed class LoggingMiddlewareOptions : WorkflowForgeOptionsBase
    {
        /// <summary>
        /// Default configuration section name for binding from appsettings.json.
        /// This is the default value; users can specify a custom section name when binding.
        /// </summary>
        public const string DefaultSectionName = "WorkflowForge:Middleware:Logging";

        private static readonly string[] ValidLogLevels =
        {
            "Trace", "Debug", "Information", "Warning", "Error", "Critical"
        };

        /// <summary>
        /// Initializes a new instance with default section name.
        /// </summary>
        public LoggingMiddlewareOptions() : base(null, DefaultSectionName)
        {
        }

        /// <summary>
        /// Initializes a new instance with custom section name.
        /// </summary>
        /// <param name="sectionName">Custom configuration section name.</param>
        public LoggingMiddlewareOptions(string sectionName) : base(sectionName, DefaultSectionName)
        {
        }

        /// <summary>
        /// Gets or sets the minimum log level for WorkflowForge logging.
        /// Valid values: Trace, Debug, Information, Warning, Error, Critical.
        /// This setting works in conjunction with your logging provider's configuration.
        /// Default is Information.
        /// </summary>
        public string MinimumLevel { get; set; } = "Information";

        /// <summary>
        /// Gets or sets whether to log operation input/output data.
        /// When true, includes data payloads in log messages (can be very verbose).
        /// When false, only logs operation names and execution context.
        /// Default is false.
        /// </summary>
        public bool LogDataPayloads { get; set; } = false;

        /// <inheritdoc />
        public override IList<string> Validate()
        {
            var errors = new List<string>();

            if (!ValidLogLevels.Contains(MinimumLevel, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add($"{SectionName}:MinimumLevel must be one of: {string.Join(", ", ValidLogLevels)} (current value: {MinimumLevel})");
            }

            return errors;
        }

        /// <inheritdoc />
        public override object Clone() => new LoggingMiddlewareOptions(SectionName)
        {
            Enabled = Enabled,
            MinimumLevel = MinimumLevel,
            LogDataPayloads = LogDataPayloads
        };
    }
}