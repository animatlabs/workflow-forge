using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Constants;
using WorkflowForge.Extensions;
using WorkflowForge.Operations;
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
        private readonly WorkflowForgeLogLevel _minimumLevel;

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
            _minimumLevel = WorkflowForgeLogLevelParsing.ParseMinimumLevel(_options.MinimumLevel);
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
            Func<CancellationToken, Task<object?>> next,
            CancellationToken cancellationToken = default)
        {
            // The scope is operation context, not verbosity: it enriches the failure log below and
            // everything the operation logs through next(), so it is always established.
            var middlewareProperties = new Dictionary<string, string>
            {
                [PropertyNameConstants.ExecutionId] = operation?.Id.ToString() ?? FoundryPropertyKeys.UnknownValue,
                [PropertyNameConstants.ExecutionName] = operation?.Name ?? FoundryPropertyKeys.UnknownValue,
                [PropertyNameConstants.ExecutionType] = operation?.GetType().Name ?? FoundryPropertyKeys.UnknownValue
            };

            if (_options.LogDataPayloads && inputData != null)
            {
                middlewareProperties["InputDataType"] = inputData.GetType().Name;
                middlewareProperties["InputData"] = inputData.ToString() ?? FoundryPropertyKeys.NullDisplayValue;
            }

            using var middlewareScope = _logger.BeginScope("MiddlewareExecution", middlewareProperties);

            var traceEnabled = WorkflowForgeLogLevelParsing.IsLevelEnabled(
                _logger,
                WorkflowForgeLogLevel.Trace,
                _minimumLevel);

            if (traceEnabled)
            {
                _logger.LogTrace(WorkflowLogMessageConstants.MiddlewareExecutionStarted);
            }

            try
            {
                var result = await next(cancellationToken).ConfigureAwait(false);

                if (traceEnabled)
                {
                    if (_options.LogDataPayloads && result != null)
                    {
                        var resultProperties = new Dictionary<string, string>
                        {
                            ["ResultType"] = result.GetType().Name,
                            ["Result"] = result.ToString() ?? FoundryPropertyKeys.NullDisplayValue
                        };
                        using var resultScope = _logger.BeginScope("OperationResult", resultProperties);
                        _logger.LogTrace(WorkflowLogMessageConstants.MiddlewareExecutionCompleted);
                    }
                    else
                    {
                        _logger.LogTrace(WorkflowLogMessageConstants.MiddlewareExecutionCompleted);
                    }
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