using System;
using System.Collections.Generic;

namespace WorkflowForge.Abstractions
{
    /// <summary>
    /// Interface for logging in WorkflowForge with structured logging support.
    /// </summary>
    public interface IWorkflowForgeLogger
    {
        /// <summary>
        /// Log a message with log level as trace.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Additional arguments for the message.</param>
        void LogTrace(string message, params object[] args);

        /// <summary>
        /// Log a message with exception and log level as trace.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Additional arguments for the message.</param>
        void LogTrace(Exception exception, string message, params object[] args);

        /// <summary>
        /// Log a message with log level as trace and custom properties.
        /// </summary>
        /// <param name="properties">Custom properties for the log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Additional arguments for the message.</param>
        void LogTrace(IDictionary<string, string> properties, string message, params object[] args);

        /// <summary>
        /// Log a message with exception and log level as trace.
        /// </summary>
        /// <param name="properties">Custom properties for the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Additional arguments for the message.</param>
        void LogTrace(IDictionary<string, string> properties, Exception exception, string message, params object[] args);

        /// <summary>
        /// Log a message with log level as debug.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Additional arguments for the message.</param>
        void LogDebug(string message, params object[] args);

        /// <summary>
        /// Log a message with exception and log level as debug.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Additional arguments for the message.</param>
        void LogDebug(Exception exception, string message, params object[] args);

        /// <summary>
        /// Log a message with log level as debug and custom properties.
        /// </summary>
        /// <param name="properties">Custom properties for the log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Additional arguments for the message.</param>
        void LogDebug(IDictionary<string, string> properties, string message, params object[] args);

        /// <summary>
        /// Log a message with exception and log level as debug.
        /// </summary>
        /// <param name="properties">Custom properties for the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Additional arguments for the message.</param>
        void LogDebug(IDictionary<string, string> properties, Exception exception, string message, params object[] args);

        /// <summary>
        /// Log a message with log level as information.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Additional arguments for the message.</param>
        void LogInformation(string message, params object[] args);

        /// <summary>
        /// Log a message with exception and log level as information.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Additional arguments for the message.</param>
        void LogInformation(Exception exception, string message, params object[] args);

        /// <summary>
        /// Log a message with log level as information and custom properties.
        /// </summary>
        /// <param name="properties">Custom properties for the log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Additional arguments for the message.</param>
        void LogInformation(IDictionary<string, string> properties, string message, params object[] args);

        /// <summary>
        /// Log a message with exception and log level as information.
        /// </summary>
        /// <param name="properties">Custom properties for the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Additional arguments for the message.</param>
        void LogInformation(IDictionary<string, string> properties, Exception exception, string message, params object[] args);

        /// <summary>
        /// Log a message with log level as warning.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Additional arguments for the message.</param>
        void LogWarning(string message, params object[] args);

        /// <summary>
        /// Log a message with exception and log level as warning.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Additional arguments for the message.</param>
        void LogWarning(Exception exception, string message, params object[] args);

        /// <summary>
        /// Log a message with log level as warning and custom properties.
        /// </summary>
        /// <param name="properties">Custom properties for the log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Additional arguments for the message.</param>
        void LogWarning(IDictionary<string, string> properties, string message, params object[] args);

        /// <summary>
        /// Log a message with exception and log level as warning.
        /// </summary>
        /// <param name="properties">Custom properties for the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Additional arguments for the message.</param>
        void LogWarning(IDictionary<string, string> properties, Exception exception, string message, params object[] args);

        /// <summary>
        /// Log a message with log level as error.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Additional arguments for the message.</param>
        void LogError(string message, params object[] args);

        /// <summary>
        /// Log a message with exception and log level as error.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Additional arguments for the message.</param>
        void LogError(Exception exception, string message, params object[] args);

        /// <summary>
        /// Log a message with log level as error and custom properties.
        /// </summary>
        /// <param name="properties">Custom properties for the log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Additional arguments for the message.</param>
        void LogError(IDictionary<string, string> properties, string message, params object[] args);

        /// <summary>
        /// Log a message with exception and log level as error.
        /// </summary>
        /// <param name="properties">Custom properties for the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Additional arguments for the message.</param>
        void LogError(IDictionary<string, string> properties, Exception exception, string message, params object[] args);

        /// <summary>
        /// Log a message with log level as critical.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Additional arguments for the message.</param>
        void LogCritical(string message, params object[] args);

        /// <summary>
        /// Log a message with exception and log level as critical.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Additional arguments for the message.</param>
        void LogCritical(Exception exception, string message, params object[] args);

        /// <summary>
        /// Log a message with log level as critical and custom properties.
        /// </summary>
        /// <param name="properties">Custom properties for the log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Additional arguments for the message.</param>
        void LogCritical(IDictionary<string, string> properties, string message, params object[] args);

        /// <summary>
        /// Log a message with exception and log level as critical.
        /// </summary>
        /// <param name="properties">Custom properties for the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Additional arguments for the message.</param>
        void LogCritical(IDictionary<string, string> properties, Exception exception, string message, params object[] args);

        /// <summary>
        /// Begins a scope with both a state and custom properties from a dictionary.
        /// </summary>
        /// <typeparam name="TState">The type of the state object.</typeparam>
        /// <param name="state">The state object.</param>
        /// <param name="properties">Dictionary of custom properties.</param>
        /// <returns><see cref="IDisposable" /> scope.</returns>
        IDisposable BeginScope<TState>(TState state, IDictionary<string, string>? properties = null);
    }
}