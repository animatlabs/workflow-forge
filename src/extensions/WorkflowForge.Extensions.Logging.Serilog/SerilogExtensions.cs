using Serilog;
using WorkflowForge;

namespace WorkflowForge.Extensions.Logging.Serilog
{
    /// <summary>
    /// Extension methods for integrating Serilog with WorkflowForge.
    /// </summary>
    public static class SerilogExtensions
    {
        /// <summary>
        /// Configures WorkflowForge to use Serilog for structured logging.
        /// </summary>
        /// <param name="configuration">The WorkflowForge configuration.</param>
        /// <param name="serilogLogger">The configured Serilog ILogger instance.</param>
        /// <returns>The configuration for method chaining.</returns>
        public static FoundryConfiguration UseSerilog(
            this FoundryConfiguration configuration, 
            ILogger serilogLogger)
        {
            var workflowLogger = new SerilogWorkflowForgeLogger(serilogLogger);
            configuration.Logger = workflowLogger;
            return configuration;
        }

        /// <summary>
        /// Configures WorkflowForge to use the global Serilog logger for structured logging.
        /// </summary>
        /// <param name="configuration">The WorkflowForge configuration.</param>
        /// <returns>The configuration for method chaining.</returns>
        /// <remarks>
        /// This method uses Log.Logger as the Serilog instance. 
        /// Ensure Serilog is properly configured before calling this method.
        /// </remarks>
        public static FoundryConfiguration UseSerilog(
            this FoundryConfiguration configuration)
        {
            return configuration.UseSerilog(Log.Logger);
        }
    }
} 