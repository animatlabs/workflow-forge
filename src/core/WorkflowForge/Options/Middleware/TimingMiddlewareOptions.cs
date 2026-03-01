using System.Collections.Generic;

namespace WorkflowForge.Options.Middleware
{
    /// <summary>
    /// Configuration options for timing middleware.
    /// Controls timing data collection behavior.
    /// Zero-dependency POCO for configuration binding.
    /// </summary>
    public sealed class TimingMiddlewareOptions : WorkflowForgeOptionsBase
    {
        /// <summary>
        /// Default configuration section name for binding from appsettings.json.
        /// This is the default value; users can specify a custom section name when binding.
        /// </summary>
        public const string DefaultSectionName = "WorkflowForge:Middleware:Timing";

        /// <summary>
        /// Initializes a new instance with default section name.
        /// </summary>
        public TimingMiddlewareOptions() : base(null, DefaultSectionName)
        {
        }

        /// <summary>
        /// Initializes a new instance with custom section name.
        /// </summary>
        /// <param name="sectionName">Custom configuration section name.</param>
        public TimingMiddlewareOptions(string sectionName) : base(sectionName, DefaultSectionName)
        {
        }

        /// <summary>
        /// Gets or sets whether to include detailed timing breakdowns.
        /// When true, stores additional timing information like start/end timestamps.
        /// When false, only stores elapsed duration.
        /// Default is false.
        /// </summary>
        public bool IncludeDetailedTimings { get; set; } = false;

        /// <inheritdoc />
        public override IList<string> Validate() => new List<string>();

        /// <inheritdoc />
        public override object Clone() => new TimingMiddlewareOptions(SectionName)
        {
            Enabled = Enabled,
            IncludeDetailedTimings = IncludeDetailedTimings
        };
    }
}