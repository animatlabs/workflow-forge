using System;
using System.Collections.Generic;

namespace WorkflowForge.Extensions.Audit
{
    /// <summary>
    /// Represents a single audit log entry for workflow operations.
    /// </summary>
    public sealed class AuditEntry
    {
        /// <summary>
        /// Gets the unique identifier for this audit entry.
        /// </summary>
        public Guid AuditId { get; }

        /// <summary>
        /// Gets the timestamp when the audit entry was created.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Gets the workflow execution ID.
        /// </summary>
        public Guid ExecutionId { get; }

        /// <summary>
        /// Gets the workflow name.
        /// </summary>
        public string WorkflowName { get; }

        /// <summary>
        /// Gets the operation name.
        /// </summary>
        public string OperationName { get; }

        /// <summary>
        /// Gets the audit event type.
        /// </summary>
        public AuditEventType EventType { get; }

        /// <summary>
        /// Gets the user or system that initiated the operation.
        /// </summary>
        public string? InitiatedBy { get; }

        /// <summary>
        /// Gets optional metadata associated with this audit entry.
        /// </summary>
        public IReadOnlyDictionary<string, object?> Metadata { get; }

        /// <summary>
        /// Gets the operation status.
        /// </summary>
        public string Status { get; }

        /// <summary>
        /// Gets the error message if the operation failed.
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// Gets the duration of the operation in milliseconds.
        /// </summary>
        public long? DurationMs { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditEntry"/> class.
        /// </summary>
        public AuditEntry(
            Guid executionId,
            string workflowName,
            string operationName,
            AuditEventType eventType,
            string status,
            string? initiatedBy = null,
            IReadOnlyDictionary<string, object?>? metadata = null,
            string? errorMessage = null,
            long? durationMs = null,
            DateTimeOffset? timestamp = null)
        {
            AuditId = Guid.NewGuid();
            Timestamp = timestamp ?? DateTimeOffset.UtcNow;
            ExecutionId = executionId;
            WorkflowName = workflowName ?? throw new ArgumentNullException(nameof(workflowName));
            OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
            EventType = eventType;
            InitiatedBy = initiatedBy;
            Metadata = metadata ?? new Dictionary<string, object?>();
            Status = status ?? throw new ArgumentNullException(nameof(status));
            ErrorMessage = errorMessage;
            DurationMs = durationMs;
        }
    }
}