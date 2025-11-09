namespace WorkflowForge.Extensions.Audit
{
    /// <summary>
    /// Defines the types of audit events that can be logged.
    /// </summary>
    public enum AuditEventType
    {
        /// <summary>
        /// Workflow execution started.
        /// </summary>
        WorkflowStarted = 1,

        /// <summary>
        /// Workflow execution completed successfully.
        /// </summary>
        WorkflowCompleted = 2,

        /// <summary>
        /// Workflow execution failed.
        /// </summary>
        WorkflowFailed = 3,

        /// <summary>
        /// Operation execution started.
        /// </summary>
        OperationStarted = 4,

        /// <summary>
        /// Operation execution completed successfully.
        /// </summary>
        OperationCompleted = 5,

        /// <summary>
        /// Operation execution failed.
        /// </summary>
        OperationFailed = 6,

        /// <summary>
        /// Data was modified during operation execution.
        /// </summary>
        DataModified = 7,

        /// <summary>
        /// Validation was performed.
        /// </summary>
        ValidationPerformed = 8,

        /// <summary>
        /// Compensation was triggered.
        /// </summary>
        CompensationTriggered = 9,

        /// <summary>
        /// Custom audit event.
        /// </summary>
        Custom = 100
    }
}