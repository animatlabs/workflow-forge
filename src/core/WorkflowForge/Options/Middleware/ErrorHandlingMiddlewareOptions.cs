using System.Collections.Generic;

namespace WorkflowForge.Options.Middleware
{
    /// <summary>
    /// Configuration options for error handling middleware.
    /// Controls exception handling behavior.
    /// Zero-dependency POCO for configuration binding.
    /// </summary>
    public sealed class ErrorHandlingMiddlewareOptions : WorkflowForgeOptionsBase
    {
        /// <summary>
        /// Default configuration section name for binding from appsettings.json.
        /// This is the default value; users can specify a custom section name when binding.
        /// </summary>
        public const string DefaultSectionName = "WorkflowForge:Middleware:ErrorHandling";

        /// <summary>
        /// Initializes a new instance with default section name.
        /// </summary>
        public ErrorHandlingMiddlewareOptions() : base(null, DefaultSectionName)
        {
        }

        /// <summary>
        /// Initializes a new instance with custom section name.
        /// </summary>
        /// <param name="sectionName">Custom configuration section name.</param>
        public ErrorHandlingMiddlewareOptions(string sectionName) : base(sectionName, DefaultSectionName)
        {
        }

        /// <summary>
        /// Gets or sets whether to rethrow exceptions after handling.
        /// When true, exceptions are logged and re-thrown (recommended).
        /// When false, exceptions are swallowed and a default value is returned.
        /// Default is true.
        /// </summary>
        public bool RethrowExceptions { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include full stack traces in error logs.
        /// When true, complete stack traces are logged (helpful for debugging).
        /// When false, only exception messages are logged (cleaner logs).
        /// Default is true.
        /// </summary>
        public bool IncludeStackTraces { get; set; } = true;

        /// <inheritdoc />
        public override IList<string> Validate() => new List<string>();

        /// <inheritdoc />
        public override object Clone() => new ErrorHandlingMiddlewareOptions(SectionName)
        {
            Enabled = Enabled,
            RethrowExceptions = RethrowExceptions,
            IncludeStackTraces = IncludeStackTraces
        };
    }
}