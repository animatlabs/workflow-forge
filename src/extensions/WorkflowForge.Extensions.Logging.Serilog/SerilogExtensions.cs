using Serilog;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Extensions.Logging.Serilog
{
    /// <summary>
    /// Extension methods for creating Serilog-based WorkflowForge loggers.
    /// NOTE: This extension does NOT work with foundries - it creates loggers for WorkflowSmith.
    /// The foundry's Logger property is immutable and set during construction.
    /// </summary>
    public static class SerilogExtensions
    {
        /// <summary>
        /// Creates a WorkflowForge logger wrapper around a Serilog ILogger.
        /// Use this when creating a WorkflowSmith or WorkflowFoundry.
        /// </summary>
        /// <param name="serilogLogger">The configured Serilog ILogger instance.</param>
        /// <returns>A WorkflowForge logger that wraps Serilog.</returns>
        /// <example>
        /// var logger = Log.Logger.ToWorkflowForgeLogger();
        /// var smith = WorkflowForge.CreateSmith(logger);
        /// </example>
        public static IWorkflowForgeLogger ToWorkflowForgeLogger(this ILogger serilogLogger)
        {
            if (serilogLogger == null) throw new System.ArgumentNullException(nameof(serilogLogger));
            return new SerilogWorkflowForgeLogger(serilogLogger);
        }

        /// <summary>
        /// Creates a WorkflowForge logger wrapper around the global Serilog logger.
        /// </summary>
        /// <returns>A WorkflowForge logger that wraps the global Serilog logger.</returns>
        /// <remarks>
        /// This method uses Log.Logger as the Serilog instance.
        /// Ensure Serilog is properly configured before calling this method.
        /// </remarks>
        public static IWorkflowForgeLogger CreateWorkflowForgeLogger()
        {
            return new SerilogWorkflowForgeLogger(Log.Logger);
        }
    }
}