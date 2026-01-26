namespace WorkflowForge.Options.Middleware
{
    /// <summary>
    /// Configuration options for timing middleware.
    /// Controls timing data collection behavior.
    /// Zero-dependency POCO for configuration binding.
    /// </summary>
    public sealed class TimingMiddlewareOptions
    {
        /// <summary>
        /// Default configuration section name for binding from appsettings.json.
        /// This is the default value; users can specify a custom section name when binding.
        /// </summary>
        public const string DefaultSectionName = "WorkflowForge:Middleware:Timing";

        /// <summary>
        /// Gets the configuration section name for this instance.
        /// </summary>
        public string SectionName { get; }

        /// <summary>
        /// Initializes a new instance with default section name.
        /// </summary>
        public TimingMiddlewareOptions() : this(DefaultSectionName)
        {
        }

        /// <summary>
        /// Initializes a new instance with custom section name.
        /// </summary>
        /// <param name="sectionName">Custom configuration section name.</param>
        public TimingMiddlewareOptions(string sectionName)
        {
            SectionName = sectionName ?? DefaultSectionName;
        }

        /// <summary>
        /// Gets or sets whether timing middleware is enabled.
        /// When true, operation timing data will be collected and stored in foundry properties.
        /// Disable in production to reduce overhead if timing metrics are not needed.
        /// Default is true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include detailed timing breakdowns.
        /// When true, stores additional timing information like start/end timestamps.
        /// When false, only stores elapsed duration.
        /// Default is false.
        /// </summary>
        public bool IncludeDetailedTimings { get; set; } = false;
    }
}