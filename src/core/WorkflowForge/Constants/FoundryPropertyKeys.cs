namespace WorkflowForge.Constants
{
    /// <summary>
    /// Constants for foundry property keys used throughout the framework.
    /// These are the keys stored in IWorkflowFoundry.Properties dictionary.
    /// </summary>
    internal static class FoundryPropertyKeys
    {
        #region Operation Execution State

        /// <summary>
        /// Well-known property key for the current operation index, set by the foundry
        /// before each middleware pipeline invocation. Middleware can read this to determine
        /// the operation's position in the workflow.
        /// </summary>
        internal const string CurrentOperationIndex = "__wf_current_op_index__";

        /// <summary>
        /// Format string for operation output key. {0} = operation index (int), {1} = operation name (string).
        /// Produces keys like "Operation.0:ValidateOrder.Output".
        /// </summary>
        internal const string OperationOutputFormat = "Operation.{0}:{1}.Output";

        /// <summary>Index of the last successfully completed operation</summary>
        internal const string LastCompletedIndex = "Operation.LastCompletedIndex";

        /// <summary>Name of the last successfully completed operation</summary>
        internal const string LastCompletedName = "Operation.LastCompletedName";

        /// <summary>Id of the last successfully completed operation</summary>
        internal const string LastCompletedId = "Operation.LastCompletedId";

        /// <summary>Index of the last failed operation</summary>
        internal const string LastFailedIndex = "Operation.LastFailedIndex";

        /// <summary>Name of the last failed operation</summary>
        internal const string LastFailedName = "Operation.LastFailedName";

        /// <summary>Id of the last failed operation</summary>
        internal const string LastFailedId = "Operation.LastFailedId";

        #endregion Operation Execution State

        #region Timing

        /// <summary>Duration of operation/workflow execution in milliseconds</summary>
        internal const string TimingDuration = "Timing.Duration";

        /// <summary>Start time of operation/workflow execution</summary>
        internal const string TimingStartTime = "Timing.StartTime";

        /// <summary>End time of operation/workflow execution</summary>
        internal const string TimingEndTime = "Timing.EndTime";

        /// <summary>Property key for elapsed ticks (high-resolution timing).</summary>
        internal const string TimingDurationTicks = "Timing.DurationTicks";

        /// <summary>Property key indicating a timing failure occurred.</summary>
        internal const string TimingFailed = "Timing.Failed";

        #endregion Timing

        #region Error Handling

        /// <summary>Error message from exception</summary>
        internal const string ErrorMessage = "Error.Message";

        /// <summary>Exception type name</summary>
        internal const string ErrorType = "Error.Type";

        /// <summary>Exception instance</summary>
        internal const string ErrorException = "Error.Exception";

        /// <summary>Timestamp when error occurred</summary>
        internal const string ErrorTimestamp = "Error.Timestamp";

        /// <summary>Property key for the error stack trace.</summary>
        internal const string ErrorStackTrace = "Error.StackTrace";

        #endregion Error Handling

        #region Timeout

        /// <summary>Workflow timeout duration</summary>
        internal const string WorkflowTimeout = "Workflow.Timeout";

        /// <summary>Whether workflow timed out</summary>
        internal const string WorkflowTimedOut = "Workflow.TimedOut";

        /// <summary>Workflow timeout cancellation token</summary>
        internal const string WorkflowTimeoutCancellationToken = "Workflow.TimeoutCancellationToken";

        /// <summary>Workflow timeout duration that was exceeded</summary>
        internal const string WorkflowTimeoutDuration = "Workflow.TimeoutDuration";

        /// <summary>Whether operation timed out</summary>
        internal const string OperationTimedOut = "Operation.TimedOut";

        /// <summary>Operation timeout duration that was exceeded</summary>
        internal const string OperationTimeoutDuration = "Operation.TimeoutDuration";

        /// <summary>
        /// Format string for per-operation timeout property keys. {0} = operation index (int), {1} = operation name (string).
        /// Produces keys like "Operation.2:SlowOperation.Timeout".
        /// </summary>
        internal const string OperationTimeoutFormat = "Operation.{0}:{1}.Timeout";

        #endregion Timeout

        #region Correlation

        /// <summary>Correlation ID for request tracking</summary>
        internal const string CorrelationId = "CorrelationId";

        /// <summary>Parent workflow execution ID for nested workflows</summary>
        internal const string ParentWorkflowExecutionId = "ParentWorkflowExecutionId";

        #endregion Correlation

        #region Workflow Metadata

        /// <summary>Workflow name</summary>
        internal const string WorkflowName = "Workflow.Name";

        #endregion Workflow Metadata

        #region Display Values

        /// <summary>Display value used when the actual value is unknown.</summary>
        internal const string UnknownValue = "Unknown";

        /// <summary>Display value used when the actual value is null.</summary>
        internal const string NullDisplayValue = "null";

        #endregion Display Values

        #region Validation

        /// <summary>Validation status</summary>
        internal const string ValidationStatus = "Validation.Status";

        /// <summary>Validation errors</summary>
        internal const string ValidationErrors = "Validation.Errors";

        #endregion Validation
    }
}