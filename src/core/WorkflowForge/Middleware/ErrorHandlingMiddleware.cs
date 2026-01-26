using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Constants;

namespace WorkflowForge.Middleware
{
    /// <summary>
    /// Middleware that handles exceptions thrown during operation execution.
    /// Provides centralized error handling with optional exception swallowing.
    /// </summary>
    internal sealed class ErrorHandlingMiddleware : IWorkflowOperationMiddleware
    {
        private readonly IWorkflowForgeLogger _logger;
        private readonly bool _rethrowExceptions;
        private readonly object? _defaultReturnValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorHandlingMiddleware"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for error events.</param>
        /// <param name="rethrowExceptions">Whether to re-throw exceptions after logging.</param>
        /// <param name="defaultReturnValue">Default value to return when swallowing exceptions.</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
        public ErrorHandlingMiddleware(
            IWorkflowForgeLogger logger,
            bool rethrowExceptions = true,
            object? defaultReturnValue = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _rethrowExceptions = rethrowExceptions;
            _defaultReturnValue = defaultReturnValue;
        }

        /// <inheritdoc />
        public async Task<object?> ExecuteAsync(
            IWorkflowOperation operation,
            IWorkflowFoundry foundry,
            object? inputData,
            Func<Task<object?>> next,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await next().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Re-throw cancellation exceptions without logging as errors
                throw;
            }
            catch (Exception ex)
            {
                // Create error handling scope
                var errorProperties = new Dictionary<string, string>
                {
                    [PropertyNameConstants.ExceptionType] = ex.GetType().Name,
                    [PropertyNameConstants.ErrorCode] = ex.HResult.ToString(),
                    [PropertyNameConstants.ErrorCategory] = GetErrorCategory(ex)
                };

                using var errorScope = _logger.BeginScope("ErrorHandling", errorProperties);

                _logger.LogError(ex, WorkflowLogMessageConstants.ErrorHandlingTriggered);

                // Store error information in foundry properties for potential recovery
                var operationName = operation?.Name ?? "Unknown";
                foundry.Properties[$"Error.{operationName}.Exception"] = ex;
                foundry.Properties[$"Error.{operationName}.Message"] = ex.Message;
                foundry.Properties[$"Error.{operationName}.OccurredAt"] = DateTimeOffset.UtcNow;
                foundry.Properties[$"Error.{operationName}.Type"] = ex.GetType().Name;

                if (_rethrowExceptions)
                {
                    throw;
                }

                var recoveryProperties = new Dictionary<string, string>
                {
                    ["RecoveryAction"] = "ExceptionSwallowed",
                    ["DefaultReturnValue"] = _defaultReturnValue?.ToString() ?? "null"
                };

                _logger.LogWarning(recoveryProperties, WorkflowLogMessageConstants.ErrorRecoveryAttempted);

                return _defaultReturnValue;
            }
        }

        /// <summary>
        /// Categorizes the exception for better error classification.
        /// </summary>
        /// <param name="exception">The exception to categorize.</param>
        /// <returns>The error category.</returns>
        private static string GetErrorCategory(Exception exception)
        {
            return exception switch
            {
                ArgumentException => "ArgumentError",
                InvalidOperationException => "InvalidOperation",
                NotSupportedException => "NotSupported",
                TimeoutException => "Timeout",
                UnauthorizedAccessException => "Security",
                NotImplementedException => "NotImplemented",
                _ => "General"
            };
        }
    }
}