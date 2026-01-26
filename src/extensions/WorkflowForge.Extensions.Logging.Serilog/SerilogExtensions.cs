using Serilog;
using Serilog.Events;
using System;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Extensions.Logging.Serilog
{
    /// <summary>
    /// Factory methods for creating Serilog-based WorkflowForge loggers
    /// without exposing Serilog types in public APIs.
    /// </summary>
    public static class SerilogLoggerFactory
    {
        /// <summary>
        /// Creates a WorkflowForge logger using the provided options.
        /// </summary>
        /// <param name="options">Serilog logger options.</param>
        /// <returns>A WorkflowForge logger instance.</returns>
        public static IWorkflowForgeLogger CreateLogger(SerilogLoggerOptions? options = null)
        {
            options ??= new SerilogLoggerOptions();
            var level = ParseLevel(options.MinimumLevel);
            var template = options.ConsoleOutputTemplate ?? SerilogLoggerOptions.DefaultConsoleOutputTemplate;

            var configuration = new LoggerConfiguration()
                .MinimumLevel.Is(level);

            if (options.EnableConsoleSink)
            {
                configuration.WriteTo.Console(outputTemplate: template);
            }

            var logger = configuration.CreateLogger();
            return new SerilogWorkflowForgeLogger(logger);
        }

        private static LogEventLevel ParseLevel(string? level)
        {
            if (!string.IsNullOrWhiteSpace(level) && Enum.TryParse(level, true, out LogEventLevel parsed))
            {
                return parsed;
            }

            return LogEventLevel.Information;
        }
    }
}