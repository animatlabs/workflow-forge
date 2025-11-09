using System;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Extensions.Audit
{
    /// <summary>
    /// Extension methods for adding audit capabilities to WorkflowForge.
    /// </summary>
    public static class AuditExtensions
    {
        /// <summary>
        /// Enables audit logging for the workflow foundry.
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="auditProvider">The audit provider for storing audit entries.</param>
        /// <param name="timeProvider">Optional time provider for timestamps.</param>
        /// <param name="initiatedBy">Optional user/system identifier that initiated the workflow.</param>
        /// <param name="includeMetadata">If true, includes foundry properties in audit metadata.</param>
        /// <returns>The foundry for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
        public static IWorkflowFoundry EnableAudit(
            this IWorkflowFoundry foundry,
            IAuditProvider auditProvider,
            ISystemTimeProvider? timeProvider = null,
            string? initiatedBy = null,
            bool includeMetadata = false)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (auditProvider == null) throw new ArgumentNullException(nameof(auditProvider));

            var middleware = new AuditMiddleware(auditProvider, timeProvider, initiatedBy, includeMetadata);
            foundry.AddMiddleware(middleware);

            return foundry;
        }

        /// <summary>
        /// Writes a custom audit entry to the audit provider.
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="auditProvider">The audit provider.</param>
        /// <param name="operationName">The name of the operation being audited.</param>
        /// <param name="eventType">The type of audit event.</param>
        /// <param name="status">The status of the operation.</param>
        /// <param name="timeProvider">Optional time provider for timestamps.</param>
        /// <param name="initiatedBy">Optional user/system identifier.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async System.Threading.Tasks.Task WriteCustomAuditAsync(
            this IWorkflowFoundry foundry,
            IAuditProvider auditProvider,
            string operationName,
            AuditEventType eventType,
            string status,
            ISystemTimeProvider? timeProvider = null,
            string? initiatedBy = null)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (auditProvider == null) throw new ArgumentNullException(nameof(auditProvider));
            if (string.IsNullOrWhiteSpace(operationName))
                throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));

            var time = timeProvider ?? SystemTimeProvider.Instance;
            var workflowName = foundry.Properties.TryGetValue("Workflow.Name", out var wfName)
                ? wfName?.ToString() ?? "Unknown"
                : "Unknown";

            var entry = new AuditEntry(
                foundry.ExecutionId,
                workflowName,
                operationName,
                eventType,
                status,
                initiatedBy,
                null,
                null,
                null,
                time.UtcNow);

            await auditProvider.WriteAuditEntryAsync(entry);
        }
    }
}