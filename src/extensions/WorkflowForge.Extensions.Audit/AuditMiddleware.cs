using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Constants;
using WorkflowForge.Extensions.Audit.Options;

namespace WorkflowForge.Extensions.Audit
{
    /// <summary>
    /// Middleware that captures audit information for workflow operations.
    /// </summary>
    public sealed class AuditMiddleware : IWorkflowOperationMiddleware
    {
        private readonly IAuditProvider _auditProvider;
        private readonly AuditMiddlewareOptions _options;
        private readonly ISystemTimeProvider _timeProvider;
        private readonly string? _initiatedBy;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditMiddleware"/> class.
        /// </summary>
        /// <param name="auditProvider">The audit provider for storing audit entries.</param>
        /// <param name="options">Configuration options for audit behavior.</param>
        /// <param name="timeProvider">Optional time provider for timestamps.</param>
        /// <param name="initiatedBy">Optional user/system identifier that initiated the workflow.</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
        public AuditMiddleware(
            IAuditProvider auditProvider,
            AuditMiddlewareOptions options,
            ISystemTimeProvider? timeProvider = null,
            string? initiatedBy = null)
        {
            _auditProvider = auditProvider ?? throw new ArgumentNullException(nameof(auditProvider));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _timeProvider = timeProvider ?? SystemTimeProvider.Instance;
            _initiatedBy = initiatedBy;
        }

        /// <summary>
        /// Executes the middleware, capturing audit information before and after operation execution.
        /// </summary>
        public async Task<object?> ExecuteAsync(
            IWorkflowOperation operation,
            IWorkflowFoundry foundry,
            object? inputData,
            Func<CancellationToken, Task<object?>> next,
            CancellationToken cancellationToken = default)
        {
            var startTime = _timeProvider.UtcNow;
            var stopwatch = Stopwatch.StartNew();

            // Prefer the live workflow name from the foundry (the framework never writes the
            // WorkflowName property key). Fall back to the property (if a caller set it) then Unknown.
            var workflowName = foundry.CurrentWorkflow?.Name
                ?? (foundry.Properties.TryGetValue(FoundryPropertyKeys.WorkflowName, out var wfName)
                    ? wfName?.ToString()
                    : null)
                ?? FoundryPropertyKeys.UnknownValue;

            // Log operation started
            await WriteAuditEntryAsync(
                foundry.ExecutionId,
                workflowName,
                operation.Name,
                AuditEventType.OperationStarted,
                "Started",
                foundry,
                startTime,
                null,
                null,
                inputData,
                null,
                cancellationToken).ConfigureAwait(false);

            try
            {
                var result = await next(cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();

                // Log operation completed
                await WriteAuditEntryAsync(
                    foundry.ExecutionId,
                    workflowName,
                    operation.Name,
                    AuditEventType.OperationCompleted,
                    "Completed",
                    foundry,
                    _timeProvider.UtcNow,
                    null,
                    stopwatch.ElapsedMilliseconds,
                    inputData,
                    result,
                    cancellationToken).ConfigureAwait(false);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Log operation failed
                await WriteAuditEntryAsync(
                    foundry.ExecutionId,
                    workflowName,
                    operation.Name,
                    AuditEventType.OperationFailed,
                    "Failed",
                    foundry,
                    _timeProvider.UtcNow,
                    ex.Message,
                    stopwatch.ElapsedMilliseconds,
                    inputData,
                    null,
                    cancellationToken).ConfigureAwait(false);

                throw;
            }
        }

        private async Task WriteAuditEntryAsync(
            Guid executionId,
            string workflowName,
            string operationName,
            AuditEventType eventType,
            string status,
            IWorkflowFoundry foundry,
            DateTimeOffset timestamp,
            string? errorMessage,
            long? durationMs,
            object? inputData,
            object? outputData,
            CancellationToken cancellationToken)
        {
            var minimal = _options.DetailLevel == AuditDetailLevel.Minimal;
            var metadata = new System.Collections.Generic.Dictionary<string, object?>();

            if (!minimal)
            {
                if (_options.DetailLevel >= AuditDetailLevel.Verbose)
                {
                    foreach (var prop in foundry.Properties)
                    {
                        metadata[prop.Key] = prop.Value;
                    }
                }

                if (_options.IncludeTimestamps)
                {
                    metadata["AuditTimestamp"] = timestamp;
                }

                if (_options.IncludeUserContext && !string.IsNullOrEmpty(_initiatedBy))
                {
                    metadata["InitiatedBy"] = _initiatedBy;
                }

                if (_options.LogDataPayloads || _options.DetailLevel == AuditDetailLevel.Complete)
                {
                    metadata["InputData"] = inputData?.ToString();
                    metadata["OutputData"] = outputData?.ToString();
                }
            }

            var entry = new AuditEntry(
                executionId,
                workflowName,
                operationName,
                eventType,
                status,
                minimal ? null : _initiatedBy,
                metadata,
                errorMessage,
                minimal ? null : durationMs,
                timestamp);

            try
            {
                await _auditProvider.WriteAuditEntryAsync(entry, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Audit failures should not break workflow execution
                foundry.Logger.LogError("Failed to write audit entry: {ErrorMessage}", ex.Message);
            }
        }
    }
}