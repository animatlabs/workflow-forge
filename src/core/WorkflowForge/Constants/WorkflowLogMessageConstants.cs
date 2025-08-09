namespace WorkflowForge.Constants
{
    /// <summary>
    /// Centralized static log message templates for consistent, professional logging.
    /// Messages use structured logging patterns with property-based context.
    /// </summary>
    public static class WorkflowLogMessageConstants
    {
        #region Workflow Lifecycle

        /// <summary>Message for workflow execution start events</summary>
        public const string WorkflowExecutionStarted = "Workflow execution started";

        /// <summary>Message for successful workflow execution completion</summary>
        public const string WorkflowExecutionCompleted = "Workflow execution completed successfully";

        /// <summary>Message for workflow execution failures</summary>
        public const string WorkflowExecutionFailed = "Workflow execution failed";

        /// <summary>Message for workflow execution cancellation</summary>
        public const string WorkflowExecutionCancelled = "Workflow execution cancelled";

        #endregion Workflow Lifecycle

        #region Operation Lifecycle

        /// <summary>Message for operation execution start events</summary>
        public const string OperationExecutionStarted = "Operation execution started";

        /// <summary>Message for successful operation execution completion</summary>
        public const string OperationExecutionCompleted = "Operation execution completed";

        /// <summary>Message for operation execution failures</summary>
        public const string OperationExecutionFailed = "Operation execution failed";

        /// <summary>Message for skipped operation execution</summary>
        public const string OperationExecutionSkipped = "Operation execution skipped";

        #endregion Operation Lifecycle

        #region Compensation

        /// <summary>Message for compensation process start events</summary>
        public const string CompensationProcessStarted = "Compensation process started";

        /// <summary>Message for compensation process completion</summary>
        public const string CompensationProcessCompleted = "Compensation process completed";

        /// <summary>Message for individual compensation action start</summary>
        public const string CompensationActionStarted = "Compensation action started";

        /// <summary>Message for individual compensation action completion</summary>
        public const string CompensationActionCompleted = "Compensation action completed";

        /// <summary>Message for individual compensation action failures</summary>
        public const string CompensationActionFailed = "Compensation action failed";

        /// <summary>Message for skipped compensation actions</summary>
        public const string CompensationActionSkipped = "Compensation action skipped - operation does not support restoration";

        #endregion Compensation

        #region ForEach Operations

        /// <summary>Message for ForEach operation start events</summary>
        public const string ForEachOperationStarted = "ForEach operation started";

        /// <summary>Message for ForEach operation completion</summary>
        public const string ForEachOperationCompleted = "ForEach operation completed";

        /// <summary>Message for ForEach operation failures</summary>
        public const string ForEachOperationFailed = "ForEach operation failed";

        /// <summary>Message for ForEach child operation start events</summary>
        public const string ForEachChildOperationStarted = "ForEach child operation started";

        /// <summary>Message for ForEach child operation completion</summary>
        public const string ForEachChildOperationCompleted = "ForEach child operation completed";

        /// <summary>Message for ForEach child operation failures</summary>
        public const string ForEachChildOperationFailed = "ForEach child operation failed";

        #endregion ForEach Operations

        #region Middleware

        /// <summary>Message for middleware execution start events</summary>
        public const string MiddlewareExecutionStarted = "Middleware execution started";

        /// <summary>Message for middleware execution completion</summary>
        public const string MiddlewareExecutionCompleted = "Middleware execution completed";

        /// <summary>Message for middleware execution failures</summary>
        public const string MiddlewareExecutionFailed = "Middleware execution failed";

        #endregion Middleware

        #region Timing & Performance

        /// <summary>Message for operation timing recording events</summary>
        public const string OperationTimingRecorded = "Operation timing recorded";

        /// <summary>Message for performance threshold exceeded events</summary>
        public const string PerformanceThresholdExceeded = "Performance threshold exceeded";

        /// <summary>Message for memory usage recording events</summary>
        public const string MemoryUsageRecorded = "Memory usage recorded";

        #endregion Timing & Performance

        #region Error Handling

        /// <summary>Message for error handling trigger events</summary>
        public const string ErrorHandlingTriggered = "Error handling triggered";

        /// <summary>Message for error recovery attempt events</summary>
        public const string ErrorRecoveryAttempted = "Error recovery attempted";

        /// <summary>Message for successful error recovery events</summary>
        public const string ErrorRecoverySucceeded = "Error recovery succeeded";

        /// <summary>Message for failed error recovery events</summary>
        public const string ErrorRecoveryFailed = "Error recovery failed";

        #endregion Error Handling

        #region Business Events

        /// <summary>Message for data validation start events</summary>
        public const string DataValidationStarted = "Data validation started";

        /// <summary>Message for data validation completion events</summary>
        public const string DataValidationCompleted = "Data validation completed";

        /// <summary>Message for data validation failure events</summary>
        public const string DataValidationFailed = "Data validation failed";

        /// <summary>Message for business rule evaluation events</summary>
        public const string BusinessRuleEvaluated = "Business rule evaluated";

        /// <summary>Message for external service invocation events</summary>
        public const string ExternalServiceInvoked = "External service invoked";

        /// <summary>Message for external service completion events</summary>
        public const string ExternalServiceCompleted = "External service call completed";

        /// <summary>Message for external service failure events</summary>
        public const string ExternalServiceFailed = "External service call failed";

        #endregion Business Events

        #region Delay Operations

        /// <summary>Message for delay operation start events</summary>
        public const string DelayOperationStarted = "Delay operation started";

        /// <summary>Message for delay operation completion events</summary>
        public const string DelayOperationCompleted = "Delay operation completed";

        #endregion Delay Operations
    }
}