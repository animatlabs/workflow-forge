using System;
using Serilog;
using Serilog.Events;
using WorkflowForge.Abstractions;
using MelLoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

namespace WorkflowForge.Extensions.Logging.Serilog
{
    /// <summary>
    /// Factory methods for creating Serilog-based WorkflowForge loggers.
    /// </summary>
    public static class SerilogLoggerFactory
    {
        /// <summary>
        /// Creates a standalone WorkflowForge logger using the embedded Serilog pipeline.
        /// </summary>
        /// <param name="options">Serilog logger options (minimum level, console sink, template).</param>
        /// <returns>A WorkflowForge logger instance backed by the embedded Serilog.</returns>
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

        /// <summary>
        /// Creates a WorkflowForge logger that delegates to an existing
        /// <see cref="MelLoggerFactory"/>. Use this when your application already
        /// has Serilog (or any provider) wired into <c>Microsoft.Extensions.Logging</c>
        /// via <c>Host.UseSerilog()</c> or similar. All WorkflowForge log output will
        /// flow through the same pipeline, sinks, and <c>appsettings.json</c> configuration
        /// as the rest of your application.
        /// </summary>
        /// <param name="loggerFactory">
        /// The application's <see cref="MelLoggerFactory"/>, typically resolved from DI.
        /// </param>
        /// <returns>A WorkflowForge logger instance backed by Microsoft.Extensions.Logging.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="loggerFactory"/> is <c>null</c>.</exception>
        public static IWorkflowForgeLogger CreateLogger(MelLoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            return new MelWorkflowForgeLogger(loggerFactory.CreateLogger("WorkflowForge"));
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
