namespace WorkflowForge.Extensions.Logging.Serilog
{
    /// <summary>
    /// Configuration options for creating a Serilog-based WorkflowForge logger.
    /// </summary>
    public sealed class SerilogLoggerOptions
    {
        internal const string DefaultConsoleOutputTemplate =
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// Gets or sets the minimum log level.
        /// </summary>
        public string MinimumLevel { get; set; } = "Information";

        /// <summary>
        /// Gets or sets whether to enable the console sink.
        /// </summary>
        public bool EnableConsoleSink { get; set; } = true;

        /// <summary>
        /// Gets or sets the console output template.
        /// </summary>
        public string? ConsoleOutputTemplate { get; set; } = DefaultConsoleOutputTemplate;
    }
}
