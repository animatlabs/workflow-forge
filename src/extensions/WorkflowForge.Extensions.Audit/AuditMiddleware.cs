using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Extensions.Audit
{
    /// <summary>
    /// Middleware that captures audit information for workflow operations.
    /// </summary>
    public sealed class AuditMiddleware : IWorkflowOperationMiddleware
    {
        private readonly IAuditProvider _auditProvider;
        private readonly ISystemTimeProvider _timeProvider;
        private readonly string? _initiatedBy;
        private readonly bool _includeMetadata;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditMiddleware"/> class.
        /// </summary>
        /// <param name="auditProvider">The audit provider for storing audit entries.</param>
        /// <param name="timeProvider">Optional time provider for timestamps.</param>
        /// <param name="initiatedBy">Optional user/system identifier that initiated the workflow.</param>
        /// <param name="includeMetadata">If true, includes foundry properties in audit metadata.</param>
        /// <exception cref="ArgumentNullException">Thrown when auditProvider is null.</exception>
        public AuditMiddleware(
            IAuditProvider auditProvider,
            ISystemTimeProvider? timeProvider = null,
            string? initiatedBy = null,
            bool includeMetadata = false)
        {
            _auditProvider = auditProvider ?? throw new ArgumentNullException(nameof(auditProvider));
            _timeProvider = timeProvider ?? SystemTimeProvider.Instance;
            _initiatedBy = initiatedBy;
            _includeMetadata = includeMetadata;
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
            var metadata = _includeMetadata
                ? new System.Collections.Generic.Dictionary<string, object?>(foundry.Properties)
                : new System.Collections.Generic.Dictionary<string, object?>();

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