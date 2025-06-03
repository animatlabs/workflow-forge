using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Loggers;

namespace WorkflowForge.Operations
{
    /// <summary>
    /// Simple logging operation for debugging and workflow tracking.
    /// Provides structured logging with properties for better observability.
    /// </summary>
    public class LoggingOperation : IWorkflowOperation
    {
        private readonly string _message;
        private readonly LogLevel _logLevel;

        /// <inheritdoc />
        public Guid Id { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public bool SupportsRestore => false;

        /// <summary>
        /// Initializes a new logging operation.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="logLevel">The log level to use.</param>
        /// <param name="name">Optional name for the operation.</param>
        public LoggingOperation(string message, LogLevel logLevel = LogLevel.Information, string? name = null)
        {
            _message = message ?? throw new ArgumentNullException(nameof(message));
            _logLevel = logLevel;
            Name = name ?? $"Log: {message}";
        }

        /// <inheritdoc />
        public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (foundry == null)
                throw new ArgumentNullException(nameof(foundry));

            // Create logging properties with input data information
            var loggingProperties = new Dictionary<string, string>
            {
                ["LogLevel"] = _logLevel.ToString(),
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
                case LogLevel.Trace:
                    foundry.Logger.LogTrace(loggingProperties, _message);
                    break;
                case LogLevel.Debug:
                    foundry.Logger.LogDebug(loggingProperties, _message);
                    break;
                case LogLevel.Information:
                    foundry.Logger.LogInformation(loggingProperties, _message);
                    break;
                case LogLevel.Warning:
                    foundry.Logger.LogWarning(loggingProperties, _message);
                    break;
                case LogLevel.Error:
                    foundry.Logger.LogError(loggingProperties, _message);
                    break;
                case LogLevel.Critical:
                    foundry.Logger.LogCritical(loggingProperties, _message);
                    break;
                default:
                    foundry.Logger.LogInformation(loggingProperties, _message);
                    break;
            }

            return Task.FromResult(inputData);
        }

        /// <inheritdoc />
        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Logging operations do not support compensation.");
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // Nothing to dispose
        }

        /// <summary>
        /// Creates a trace-level logging operation.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A logging operation.</returns>
        public static LoggingOperation Trace(string message) => new(message, LogLevel.Trace);

        /// <summary>
        /// Creates a debug-level logging operation.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A logging operation.</returns>
        public static LoggingOperation Debug(string message) => new(message, LogLevel.Debug);

        /// <summary>
        /// Creates an info-level logging operation.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A logging operation.</returns>
        public static LoggingOperation Info(string message) => new(message, LogLevel.Information);

        /// <summary>
        /// Creates a warning-level logging operation.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A logging operation.</returns>
        public static LoggingOperation Warning(string message) => new(message, LogLevel.Warning);

        /// <summary>
        /// Creates an error-level logging operation.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A logging operation.</returns>
        public static LoggingOperation Error(string message) => new(message, LogLevel.Error);

        /// <summary>
        /// Creates a critical-level logging operation.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>A logging operation.</returns>
        public static LoggingOperation Critical(string message) => new(message, LogLevel.Critical);
    }
} 