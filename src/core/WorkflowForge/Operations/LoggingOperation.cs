using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Operations
{
    /// <summary>
    /// Simple logging operation for debugging and workflow tracking.
    /// Provides structured logging with properties for better observability.
    /// </summary>
    public sealed class LoggingOperation : WorkflowOperationBase
    {
        private readonly string _message;
        private readonly WorkflowForgeLogLevel _logLevel;

        /// <inheritdoc />
        public override string Name { get; }

        /// <summary>
        /// Initializes a new logging operation.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="logLevel">The log level to use.</param>
        /// <param name="name">Optional name for the operation.</param>
        public LoggingOperation(string message, WorkflowForgeLogLevel logLevel = WorkflowForgeLogLevel.Information, string? name = null)
        {
            _message = message ?? throw new ArgumentNullException(nameof(message));
            _logLevel = logLevel;
            Name = name ?? $"Log: {message}";
        }

        /// <inheritdoc />
        public override Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (foundry == null)
                throw new ArgumentNullException(nameof(foundry));

            // Create logging properties with input data information
            var loggingProperties = new Dictionary<string, string>
            {
                ["WorkflowForgeLogLevel"] = _logLevel.ToString(),
                ["UserMessage"] = _message,
                ["InputDataType"] = inputData?.GetType().Name ?? "null",
                ["OperationId"] = Id.ToString(),
                ["OperationName"] = Name,
                ["WorkflowId"] = foundry.ExecutionId.ToString(),
                ["WorkflowName"] = foundry.CurrentWorkflow?.Name ?? "Unknown",
                ["InputType"] = inputData?.GetType().Name ?? "null"
            };

            using var loggingScope = foundry.Logger.BeginScope("LoggingOperation", loggingProperties);

            // Log the user's message at the specified level with properties
            switch (_logLevel)
            {
                case WorkflowForgeLogLevel.Trace:
                    foundry.Logger.LogTrace(loggingProperties, _message);
                    break;

                case WorkflowForgeLogLevel.Debug:
                    foundry.Logger.LogDebug(loggingProperties, _message);
                    break;

                case WorkflowForgeLogLevel.Information:
                    foundry.Logger.LogInformation(loggingProperties, _message);
                    break;

                case WorkflowForgeLogLevel.Warning:
                    foundry.Logger.LogWarning(loggingProperties, _message);
                    break;

                case WorkflowForgeLogLevel.Error:
                    foundry.Logger.LogError(loggingProperties, _message);
                    break;

                case WorkflowForgeLogLevel.Critical:
                    foundry.Logger.LogCritical(loggingProperties, _message);
                    break;

                default:
                    foundry.Logger.LogInformation(loggingProperties, _message);
                    break;
            }

            return Task.FromResult(inputData);
        }

        // Uses base RestoreAsync behavior which throws when SupportsRestore is false

        /// <summary>
        /// Creates a trace-level logging operation.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A logging operation.</returns>
        public static LoggingOperation Trace(string message) => new(message, WorkflowForgeLogLevel.Trace);

        /// <summary>
        /// Creates a debug-level logging operation.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A logging operation.</returns>
        public static LoggingOperation Debug(string message) => new(message, WorkflowForgeLogLevel.Debug);

        /// <summary>
        /// Creates an info-level logging operation.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A logging operation.</returns>
        public static LoggingOperation Info(string message) => new(message, WorkflowForgeLogLevel.Information);

        /// <summary>
        /// Creates a warning-level logging operation.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A logging operation.</returns>
        public static LoggingOperation Warning(string message) => new(message, WorkflowForgeLogLevel.Warning);

        /// <summary>
        /// Creates an error-level logging operation.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A logging operation.</returns>
        public static LoggingOperation Error(string message) => new(message, WorkflowForgeLogLevel.Error);

        /// <summary>
        /// Creates a critical-level logging operation.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A logging operation.</returns>
        public static LoggingOperation Critical(string message) => new(message, WorkflowForgeLogLevel.Critical);
    }
}