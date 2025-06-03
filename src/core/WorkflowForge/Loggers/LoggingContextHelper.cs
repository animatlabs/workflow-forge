using System;
using System.Collections.Generic;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Loggers
{
    /// <summary>
    /// Helper for creating standardized logging contexts with consistent property naming.
    /// Uses simple dictionaries and focuses on essential execution context only.
    /// Performance metrics are handled by WorkflowForge.Extensions.Observability.Performance.
    /// </summary>
    public static class LoggingContextHelper
    {
        /// <summary>
        /// Creates a workflow execution scope with essential workflow properties.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="workflow">The workflow being executed.</param>
        /// <param name="foundry">The workflow foundry containing execution context.</param>
        /// <returns>A disposable logging scope.</returns>
        public static IDisposable CreateWorkflowScope(IWorkflowForgeLogger logger, IWorkflow workflow, IWorkflowFoundry foundry)
        {
            var properties = new Dictionary<string, string>
            {
                [PropertyNames.ExecutionId] = workflow.Id.ToString(),
                [PropertyNames.ExecutionName] = workflow.Name,
                [PropertyNames.ExecutionType] = workflow.GetType().Name,
                [PropertyNames.FoundryExecutionId] = foundry.ExecutionId.ToString(),
                [PropertyNames.TotalOperationCount] = workflow.Operations.Count.ToString()
            };
            
            // Add parent workflow context if available from foundry data
            if (foundry.Properties.TryGetValue("ParentWorkflowExecutionId", out var parentId))
            {
                properties[PropertyNames.ParentWorkflowExecutionId] = parentId?.ToString() ?? string.Empty;
            }

            return logger.BeginScope("WorkflowExecution", properties);
        }

        /// <summary>
        /// Creates an operation execution scope with essential operation properties.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="operation">The operation being executed.</param>
        /// <param name="stepIndex">The step index in the workflow.</param>
        /// <param name="inputData">Optional input data for context.</param>
        /// <returns>A disposable logging scope.</returns>
        public static IDisposable CreateOperationScope(IWorkflowForgeLogger logger, IWorkflowOperation operation, int stepIndex, object? inputData = null)
        {
            var properties = new Dictionary<string, string>
            {
                [PropertyNames.ExecutionId] = operation.Id.ToString(),
                [PropertyNames.ExecutionName] = operation.Name,
                [PropertyNames.ExecutionType] = operation.GetType().Name,
                [PropertyNames.OperationStepIndex] = stepIndex.ToString()
            };

            return logger.BeginScope("OperationExecution", properties);
        }

        /// <summary>
        /// Creates a compensation execution scope.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="operationCount">Number of operations to compensate.</param>
        /// <returns>A disposable logging scope.</returns>
        public static IDisposable CreateCompensationScope(IWorkflowForgeLogger logger, int operationCount)
        {
            var properties = new Dictionary<string, string>
            {
                [PropertyNames.ExecutionId] = Guid.NewGuid().ToString(),
                [PropertyNames.ExecutionName] = "CompensationProcess",
                [PropertyNames.ExecutionType] = "Compensation",
                [PropertyNames.CompensationOperationCount] = operationCount.ToString()
            };

            return logger.BeginScope("CompensationExecution", properties);
        }

        /// <summary>
        /// Creates error properties for exception logging.
        /// </summary>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="category">Optional error category.</param>
        /// <returns>Error properties dictionary.</returns>
        public static Dictionary<string, string> CreateErrorProperties(Exception exception, string? category = null)
        {
            return new Dictionary<string, string>
            {
                [PropertyNames.ExceptionType] = exception.GetType().Name,
                [PropertyNames.ErrorCode] = exception.HResult.ToString(),
                [PropertyNames.ErrorCategory] = category ?? "UnhandledException"
            };
        }

        /// <summary>
        /// Creates compensation result properties.
        /// </summary>
        /// <param name="successCount">Number of successful compensations.</param>
        /// <param name="failureCount">Number of failed compensations.</param>
        /// <returns>Compensation result properties dictionary.</returns>
        public static Dictionary<string, string> CreateCompensationResultProperties(int successCount, int failureCount)
        {
            return new Dictionary<string, string>
            {
                [PropertyNames.CompensationSuccessCount] = successCount.ToString(),
                [PropertyNames.CompensationFailureCount] = failureCount.ToString()
            };
        }

        #region Foundry Data Helpers

        /// <summary>
        /// Sets correlation ID in foundry data for tracking across operations.
        /// This is core foundry data, not logging properties.
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="correlationId">The correlation ID to track.</param>
        public static void SetCorrelationId(IWorkflowFoundry foundry, string correlationId)
        {
            foundry.Properties["CorrelationId"] = correlationId;
        }

        /// <summary>
        /// Gets correlation ID from foundry data.
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <returns>The correlation ID if available.</returns>
        public static string? GetCorrelationId(IWorkflowFoundry foundry)
        {
            return foundry.Properties.TryGetValue("CorrelationId", out var correlationId) 
                ? correlationId?.ToString() 
                : null;
        }

        /// <summary>
        /// Sets parent workflow execution ID for nested workflow tracking.
        /// This is core foundry data for workflow hierarchy.
        /// </summary>
        /// <param name="foundry">The child workflow foundry.</param>
        /// <param name="parentWorkflowExecutionId">The parent workflow execution ID.</param>
        public static void SetParentWorkflowExecutionId(IWorkflowFoundry foundry, string parentWorkflowExecutionId)
        {
            foundry.Properties["ParentWorkflowExecutionId"] = parentWorkflowExecutionId;
        }

        #endregion
    }
} 