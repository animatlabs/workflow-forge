using System;
using System.Collections.Generic;
using WorkflowForge.Abstractions;
using WorkflowForge.Operations;

namespace WorkflowForge.Loggers
{
    /// <summary>
    /// Optional base class for <see cref="IWorkflowForgeLogger"/> implementations with a configurable minimum level.
    /// </summary>
    public abstract class WorkflowForgeLoggerBase : IWorkflowForgeLogger
    {
        /// <summary>
        /// Gets the minimum level this logger emits.
        /// </summary>
        protected virtual WorkflowForgeLogLevel MinimumLevel => WorkflowForgeLogLevel.Trace;

        /// <inheritdoc />
        public virtual bool IsEnabled(WorkflowForgeLogLevel level) => level >= MinimumLevel;

        /// <inheritdoc />
        public abstract void LogTrace(string message, params object[] args);

        /// <inheritdoc />
        public abstract void LogTrace(Exception exception, string message, params object[] args);

        /// <inheritdoc />
        public abstract void LogTrace(IDictionary<string, string> properties, string message, params object[] args);

        /// <inheritdoc />
        public abstract void LogTrace(IDictionary<string, string> properties, Exception exception, string message, params object[] args);

        /// <inheritdoc />
        public abstract void LogDebug(string message, params object[] args);

        /// <inheritdoc />
        public abstract void LogDebug(Exception exception, string message, params object[] args);

        /// <inheritdoc />
        public abstract void LogDebug(IDictionary<string, string> properties, string message, params object[] args);

        /// <inheritdoc />
        public abstract void LogDebug(IDictionary<string, string> properties, Exception exception, string message, params object[] args);

        /// <inheritdoc />
        public abstract void LogInformation(string message, params object[] args);

        /// <inheritdoc />
        public abstract void LogInformation(Exception exception, string message, params object[] args);

        /// <inheritdoc />
        public abstract void LogInformation(IDictionary<string, string> properties, string message, params object[] args);

        /// <inheritdoc />
        public abstract void LogInformation(IDictionary<string, string> properties, Exception exception, string message, params object[] args);

        /// <inheritdoc />
        public abstract void LogWarning(string message, params object[] args);

        /// <inheritdoc />
        public abstract void LogWarning(Exception exception, string message, params object[] args);

        /// <inheritdoc />
        public abstract void LogWarning(IDictionary<string, string> properties, string message, params object[] args);

        /// <inheritdoc />
        public abstract void LogWarning(IDictionary<string, string> properties, Exception exception, string message, params object[] args);

        /// <inheritdoc />
        public abstract void LogError(string message, params object[] args);

        /// <inheritdoc />
        public abstract void LogError(Exception exception, string message, params object[] args);

        /// <inheritdoc />
        public abstract void LogError(IDictionary<string, string> properties, string message, params object[] args);

        /// <inheritdoc />
        public abstract void LogError(IDictionary<string, string> properties, Exception exception, string message, params object[] args);

        /// <inheritdoc />
        public abstract void LogCritical(string message, params object[] args);

        /// <inheritdoc />
        public abstract void LogCritical(Exception exception, string message, params object[] args);

        /// <inheritdoc />
        public abstract void LogCritical(IDictionary<string, string> properties, string message, params object[] args);

        /// <inheritdoc />
        public abstract void LogCritical(IDictionary<string, string> properties, Exception exception, string message, params object[] args);

        /// <inheritdoc />
        public abstract IDisposable BeginScope<TState>(TState state, IDictionary<string, string>? properties = null);
    }
}
