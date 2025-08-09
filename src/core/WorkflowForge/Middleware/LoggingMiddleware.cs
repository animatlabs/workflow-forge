using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Constants;
using WorkflowForge.Extensions;

namespace WorkflowForge.Middleware
{
    /// <summary>
    /// Middleware that logs the execution of workflow operations with corporate messaging.
    /// Uses consistent property naming and focuses on essential context only.
    /// Timing concerns are handled by TimingMiddleware.
    /// </summary>
    internal sealed class LoggingMiddleware : IWorkflowOperationMiddleware
    {
        private readonly IWorkflowForgeLogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingMiddleware"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for operation logging.</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
        public LoggingMiddleware(IWorkflowForgeLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<object?> ExecuteAsync(
            IWorkflowOperation operation,
            IWorkflowFoundry foundry,
            object? inputData,
            Func<Task<object?>> next,
            CancellationToken cancellationToken = default)
        {
            // Create middleware scope with operation context using consistent property names
            var middlewareProperties = new Dictionary<string, string>
            {
                [PropertyNameConstants.ExecutionId] = operation?.Id.ToString() ?? "Unknown",
                [PropertyNameConstants.ExecutionName] = operation?.Name ?? "Unknown",
                [PropertyNameConstants.ExecutionType] = operation?.GetType().Name ?? "Unknown"
            };

            using var middlewareScope = _logger.BeginScope("MiddlewareExecution", middlewareProperties);

            _logger.LogTrace(WorkflowLogMessageConstants.MiddlewareExecutionStarted);

            try
            {
                var result = await next().ConfigureAwait(false);

                _logger.LogTrace(WorkflowLogMessageConstants.MiddlewareExecutionCompleted);

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