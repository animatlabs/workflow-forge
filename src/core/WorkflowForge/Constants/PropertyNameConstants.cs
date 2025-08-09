namespace WorkflowForge.Constants
{
    /// <summary>
    /// Centralized property names for core structured logging to ensure consistency
    /// and avoid conflicts with external systems (e.g., Azure OperationId).
    /// Performance metrics are handled by WorkflowForge.Extensions.Observability.Performance.
    /// </summary>
    public static class PropertyNameConstants
    {
        #region Core Execution Properties (Base)

        /// <summary>Execution identifier (base for all execution types)</summary>
        public const string ExecutionId = "ExecutionId";

        /// <summary>Execution name (base for all execution types)</summary>
        public const string ExecutionName = "ExecutionName";

        /// <summary>Execution type (base for all execution types)</summary>
        public const string ExecutionType = "ExecutionType";

        #endregion Core Execution Properties (Base)

        #region Workflow Context

        /// <summary>Workflow execution foundry identifier</summary>
        public const string FoundryExecutionId = "FoundryExecutionId";

        /// <summary>Total number of operations in workflow</summary>
        public const string TotalOperationCount = "TotalOperationCount";

        /// <summary>Parent workflow execution identifier for nested workflows</summary>
        public const string ParentWorkflowExecutionId = "ParentWorkflowExecutionId";

        #endregion Workflow Context

        #region Operation Context

        /// <summary>Operation step index in workflow</summary>
        public const string OperationStepIndex = "OperationStepIndex";

        #endregion Operation Context

        #region Error Context

        /// <summary>Exception type</summary>
        public const string ExceptionType = "ExceptionType";

        /// <summary>Error code</summary>
        public const string ErrorCode = "ErrorCode";

        /// <summary>Error category</summary>
        public const string ErrorCategory = "ErrorCategory";

        #endregion Error Context

        #region Compensation Context

        /// <summary>Compensation operation count</summary>
        public const string CompensationOperationCount = "CompensationOperationCount";

        /// <summary>Compensation success count</summary>
        public const string CompensationSuccessCount = "CompensationSuccessCount";

        /// <summary>Compensation failure count</summary>
        public const string CompensationFailureCount = "CompensationFailureCount";

        #endregion Compensation Context

        #region ForEach Context

        /// <summary>Collection item index in ForEach operation</summary>
        public const string ForEachItemIndex = "ForEachItemIndex";

        /// <summary>Total collection size in ForEach operation</summary>
        public const string ForEachCollectionSize = "ForEachCollectionSize";

        /// <summary>Maximum concurrency for ForEach operation</summary>
        public const string ForEachMaxConcurrency = "ForEachMaxConcurrency";

        #endregion ForEach Context
    }
}