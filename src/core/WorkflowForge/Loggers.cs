using WorkflowForge.Abstractions;
using WorkflowForge.Loggers;

namespace WorkflowForge
{
    /// <summary>
    /// Provides factory methods for creating built-in logger implementations.
    /// Simplifies logger instantiation with convenient static methods.
    /// </summary>
    public static class WorkflowForgeLoggers
    {
        /// <summary>
        /// Gets a null logger that discards all log messages (no-op logger).
        /// Useful for testing scenarios or when logging is not needed.
        /// </summary>
        /// <remarks>
        /// This logger implements <see cref="IWorkflowForgeLogger"/> but performs no actual logging.
        /// All method calls are no-ops with no performance overhead.
        /// </remarks>
        /// <example>
        /// <code>
        /// var smith = WorkflowForge.CreateSmith(WorkflowForgeLoggers.Null);
        /// </code>
        /// </example>
        public static IWorkflowForgeLogger Null => NullLogger.Instance;

        /// <summary>
        /// Creates a simple console logger with the specified prefix.
        /// </summary>
        /// <param name="prefix">The prefix for log messages. Default is "WorkflowForge".</param>
        /// <param name="timeProvider">Optional time provider for timestamps. If not specified, uses SystemTimeProvider.Instance.</param>
        /// <returns>A new console logger instance.</returns>
        /// <remarks>
        /// For production scenarios with structured logging, filtering, and sinks,
        /// use WorkflowForge.Extensions.Logging.Serilog or implement your own <see cref="IWorkflowForgeLogger"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// var logger = WorkflowForgeLoggers.Console("MyApp");
        /// var smith = WorkflowForge.CreateSmith(logger);
        /// </code>
        /// </example>
        public static IWorkflowForgeLogger Console(string prefix = "WorkflowForge", ISystemTimeProvider? timeProvider = null)
        {
            return new ConsoleLogger(prefix, timeProvider);
        }
    }
}