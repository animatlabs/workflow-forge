using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Constants;
using WorkflowForge.Extensions;
using WorkflowForge.Options.Middleware;

namespace WorkflowForge.Middleware
{
    /// <summary>
    /// Middleware that logs the execution of workflow operations with structured messaging.
    /// Uses consistent property naming and focuses on essential context.
    /// Timing concerns are handled by TimingMiddleware.
    /// Can be configured via <see cref="LoggingMiddlewareOptions"/> to control verbosity and behavior.
    /// </summary>
    internal sealed class LoggingMiddleware : IWorkflowOperationMiddleware
    {
        private readonly LoggingMiddlewareOptions _options;
        private readonly IWorkflowForgeLogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingMiddleware"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for operation logging.</param>
        /// <param name="options">The logging middleware options.</param>
        /// <exception cref="ArgumentNullException">Thrown when logger or options is null.</exception>
        public LoggingMiddleware(
            IWorkflowForgeLogger logger,
            LoggingMiddlewareOptions options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Initializes a new instance with default options (for backward compatibility).
        /// </summary>
        /// <param name="logger">The logger to use for operation logging.</param>
        public LoggingMiddleware(IWorkflowForgeLogger logger)
            : this(logger, new LoggingMiddlewareOptions())
        {
        }

        /// <inheritdoc />
        public async Task<object?> ExecuteAsync(
            IWorkflowOperation operation,
            IWorkflowFoundry foundry,
            object? inputData,
            Func<Task<object?>> next,
            CancellationToken cancellationToken = default)
        {
            // Note: Enabled check is done at registration time (UseDefaultMiddleware)
            // If this middleware is registered, it's enabled - no need for runtime check

            // Create middleware scope with operation context using consistent property names
            var middlewareProperties = new Dictionary<string, string>
            {
                [PropertyNameConstants.ExecutionId] = operation?.Id.ToString() ?? "Unknown",
                [PropertyNameConstants.ExecutionName] = operation?.Name ?? "Unknown",
                [PropertyNameConstants.ExecutionType] = operation?.GetType().Name ?? "Unknown"
            };

            // Optionally include data payloads if configured
            if (_options.LogDataPayloads && inputData != null)
            {
                middlewareProperties["InputDataType"] = inputData.GetType().Name;
                middlewareProperties["InputData"] = inputData.ToString() ?? "null";
            }

            using var middlewareScope = _logger.BeginScope("MiddlewareExecution", middlewareProperties);

            _logger.LogTrace(WorkflowLogMessageConstants.MiddlewareExecutionStarted);

            try
            {
                var result = await next().ConfigureAwait(false);

                // Log result if configured
                if (_options.LogDataPayloads && result != null)
                {
                    var resultProperties = new Dictionary<string, string>
                    {
                        ["ResultType"] = result.GetType().Name,
                        ["Result"] = result.ToString() ?? "null"
                    };
                    using var resultScope = _logger.BeginScope("OperationResult", resultProperties);
                    _logger.LogTrace(WorkflowLogMessageConstants.MiddlewareExecutionCompleted);
                }
                else
                {
                    _logger.LogTrace(WorkflowLogMessageConstants.MiddlewareExecutionCompleted);
                }

                return result;
            }
            catch (Exception ex)
            {
                var errorProperties = _logger.CreateErrorProperties(ex, "MiddlewareExecution");

                _logger.LogError(errorProperties, ex, WorkflowLogMessageConstants.MiddlewareExecutionFailed);
                throw;
            }
        }
    }
}