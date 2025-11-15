using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
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
            Func<Task<object?>> next,
            CancellationToken cancellationToken = default)
        {
            var startTime = _timeProvider.UtcNow;
            var stopwatch = Stopwatch.StartNew();

            var workflowName = foundry.Properties.TryGetValue("Workflow.Name", out var wfName)
                ? wfName?.ToString() ?? "Unknown"
                : "Unknown";

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
                cancellationToken);

            try
            {
                var result = await next();
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
                    cancellationToken);

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
                    cancellationToken);

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
            CancellationToken cancellationToken)
        {
            // Determine what metadata to include based on options
            var metadata = new System.Collections.Generic.Dictionary<string, object?>();

            // Include foundry properties based on detail level
            if (_options.DetailLevel >= AuditDetailLevel.Verbose)
            {
                foreach (var prop in foundry.Properties)
                {
                    metadata[prop.Key] = prop.Value;
                }
            }

            // Add timestamp if configured
            if (_options.IncludeTimestamps)
            {
                metadata["AuditTimestamp"] = timestamp;
            }

            // Add user context if configured
            if (_options.IncludeUserContext && !string.IsNullOrEmpty(_initiatedBy))
            {
                metadata["InitiatedBy"] = _initiatedBy;
            }

            var entry = new AuditEntry(
                executionId,
                workflowName,
                operationName,
                eventType,
                status,
                _initiatedBy,
                metadata,
                errorMessage,
                durationMs,
                timestamp);

            try
            {
                await _auditProvider.WriteAuditEntryAsync(entry, cancellationToken);
            }
            catch (Exception ex)
            {
                // Audit failures should not break workflow execution
                foundry.Logger.LogError($"Failed to write audit entry: {ex.Message}");
            }
        }
    }
}