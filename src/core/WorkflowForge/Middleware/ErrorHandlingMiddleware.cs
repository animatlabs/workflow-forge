using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Constants;
using WorkflowForge.Options.Middleware;

namespace WorkflowForge.Middleware
{
    /// <summary>
    /// Middleware that handles exceptions thrown during operation execution.
    /// Provides centralized error handling with configurable exception handling behavior.
    /// Can be configured via <see cref="ErrorHandlingMiddlewareOptions"/> to control rethrowing and stack trace logging.
    /// </summary>
    internal sealed class ErrorHandlingMiddleware : IWorkflowOperationMiddleware
    {
        private readonly ErrorHandlingMiddlewareOptions _options;
        private readonly IWorkflowForgeLogger _logger;
        private readonly ISystemTimeProvider _timeProvider;
        private readonly object? _defaultReturnValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorHandlingMiddleware"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for error events.</param>
        /// <param name="options">The error handling middleware options.</param>
        /// <param name="defaultReturnValue">Default value to return when swallowing exceptions.</param>
        /// <param name="timeProvider">The time provider to use for timestamps.</param>
        /// <exception cref="ArgumentNullException">Thrown when logger or options is null.</exception>
        public ErrorHandlingMiddleware(
            IWorkflowForgeLogger logger,
            ErrorHandlingMiddlewareOptions options,
            object? defaultReturnValue = null,
            ISystemTimeProvider? timeProvider = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _defaultReturnValue = defaultReturnValue;
            _timeProvider = timeProvider ?? SystemTimeProvider.Instance;
        }

        /// <summary>
        /// Initializes a new instance with default options (for backward compatibility).
        /// </summary>
        /// <param name="logger">The logger to use for error events.</param>
        /// <param name="rethrowExceptions">Whether to re-throw exceptions after logging.</param>
        /// <param name="defaultReturnValue">Default value to return when swallowing exceptions.</param>
        /// <param name="timeProvider">The time provider to use for timestamps.</param>
        public ErrorHandlingMiddleware(
            IWorkflowForgeLogger logger,
            bool rethrowExceptions = true,
            object? defaultReturnValue = null,
            ISystemTimeProvider? timeProvider = null)
            : this(logger, new ErrorHandlingMiddlewareOptions { RethrowExceptions = rethrowExceptions }, defaultReturnValue, timeProvider)
        {
        }

        /// <inheritdoc />
        public async Task<object?> ExecuteAsync(
            IWorkflowOperation operation,
            IWorkflowFoundry foundry,
            object? inputData,
            Func<CancellationToken, Task<object?>> next,
            CancellationToken cancellationToken = default)
        {
            // Note: Enabled check is done at registration time (UseDefaultMiddleware)
            // If this middleware is registered, it's enabled - no need for runtime check

            try
            {
                return await next(cancellationToken).ConfigureAwait(false);
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
                // Note: Operation name is already in logging context via ExecutionName property
                foundry.Properties["Error.Message"] = ex.Message;
                foundry.Properties["Error.OccurredAt"] = _timeProvider.UtcNow;
                foundry.Properties["Error.Type"] = ex.GetType().Name;
                foundry.Properties["Error.Exception"] = ex;

                // Include stack trace based on configuration
                if (_options.IncludeStackTraces)
                {
                    foundry.Properties["Error.StackTrace"] = ex.StackTrace;
                }

                if (_options.RethrowExceptions)
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