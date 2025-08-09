using System;
using System.Collections.Generic;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Loggers
{
    /// <summary>
    /// A null logger implementation that discards all log messages.
    /// For full structured logging, use WorkflowForge.Extensions.Logging.
    /// </summary>
    internal sealed class NullLogger : IWorkflowForgeLogger
    {
        /// <summary>
        /// Gets a singleton instance of the null logger.
        /// </summary>
        public static readonly NullLogger Instance = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="NullLogger"/> class.
        /// </summary>
        private NullLogger()
        { }

        /// <inheritdoc />
        public void LogTrace(string message, params object[] args)
        { }

        /// <inheritdoc />
        public void LogTrace(Exception exception, string message, params object[] args)
        { }

        /// <inheritdoc />
        public void LogTrace(IDictionary<string, string> properties, string message, params object[] args)
        { }

        /// <inheritdoc />
        public void LogTrace(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        { }

        /// <inheritdoc />
        public void LogDebug(string message, params object[] args)
        { }

        /// <inheritdoc />
        public void LogDebug(Exception exception, string message, params object[] args)
        { }

        /// <inheritdoc />
        public void LogDebug(IDictionary<string, string> properties, string message, params object[] args)
        { }

        /// <inheritdoc />
        public void LogDebug(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        { }

        /// <inheritdoc />
        public void LogInformation(string message, params object[] args)
        { }

        /// <inheritdoc />
        public void LogInformation(Exception exception, string message, params object[] args)
        { }

        /// <inheritdoc />
        public void LogInformation(IDictionary<string, string> properties, string message, params object[] args)
        { }

        /// <inheritdoc />
        public void LogInformation(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        { }

        /// <inheritdoc />
        public void LogWarning(string message, params object[] args)
        { }

        /// <inheritdoc />
        public void LogWarning(Exception exception, string message, params object[] args)
        { }

        /// <inheritdoc />
        public void LogWarning(IDictionary<string, string> properties, string message, params object[] args)
        { }

        /// <inheritdoc />
        public void LogWarning(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        { }

        /// <inheritdoc />
        public void LogError(string message, params object[] args)
        { }

        /// <inheritdoc />
        public void LogError(Exception exception, string message, params object[] args)
        { }

        /// <inheritdoc />
        public void LogError(IDictionary<string, string> properties, string message, params object[] args)
        { }

        /// <inheritdoc />
        public void LogError(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        { }

        /// <inheritdoc />
        public void LogCritical(string message, params object[] args)
        { }

        /// <inheritdoc />
        public void LogCritical(Exception exception, string message, params object[] args)
        { }

        /// <inheritdoc />
        public void LogCritical(IDictionary<string, string> properties, string message, params object[] args)
        { }

        /// <inheritdoc />
        public void LogCritical(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        { }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state, IDictionary<string, string>? properties = null) => new EmptyDisposable();

        private sealed class EmptyDisposable : IDisposable
        {
            public void Dispose()
            { }
        }
    }
}