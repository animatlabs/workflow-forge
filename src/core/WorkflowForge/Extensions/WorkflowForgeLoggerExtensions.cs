using System;
using System.Collections.Generic;
using WorkflowForge.Abstractions;
using WorkflowForge.Constants;

namespace WorkflowForge.Extensions
{
    /// <summary>
    /// Extension methods for creating standardized logging scopes and property dictionaries.
    /// </summary>
    public static class WorkflowForgeLoggerExtensions
    {
        /// <summary>
        /// Creates a workflow execution logging scope with standardized properties.
        /// </summary>
        /// <param name="logger">WorkflowForge logger.</param>
        /// <param name="workflow">Workflow being executed.</param>
        /// <param name="foundry">Foundry context.</param>
        /// <returns>A disposable logging scope.</returns>
        public static IDisposable CreateWorkflowScope(this IWorkflowForgeLogger logger, IWorkflow workflow, IWorkflowFoundry foundry)
        {
            var properties = new Dictionary<string, string>
            {
                [PropertyNameConstants.ExecutionId] = workflow.Id.ToString(),
                [PropertyNameConstants.ExecutionName] = workflow.Name,
                [PropertyNameConstants.ExecutionType] = workflow.GetType().Name,
                [PropertyNameConstants.FoundryExecutionId] = foundry.ExecutionId.ToString(),
                [PropertyNameConstants.TotalOperationCount] = workflow.Operations.Count.ToString()
            };

            if (foundry.Properties.TryGetValue(FoundryPropertyKeys.ParentWorkflowExecutionId, out var parentId))
            {
                properties[PropertyNameConstants.ParentWorkflowExecutionId] = parentId?.ToString() ?? string.Empty;
            }

            return logger.BeginScope("WorkflowExecution", properties);
        }

        /// <summary>
        /// Creates an operation execution logging scope with standardized properties.
        /// </summary>
        /// <param name="logger">WorkflowForge logger.</param>
        /// <param name="operation">Operation being executed.</param>
        /// <param name="stepIndex">Step index in the workflow.</param>
        /// <param name="inputData">Optional input data for context.</param>
        /// <returns>A disposable logging scope.</returns>
        public static IDisposable CreateOperationScope(this IWorkflowForgeLogger logger, IWorkflowOperation operation, int stepIndex, object? inputData = null)
        {
            var properties = new Dictionary<string, string>
            {
                [PropertyNameConstants.ExecutionId] = operation.Id.ToString(),
                [PropertyNameConstants.ExecutionName] = operation.Name,
                [PropertyNameConstants.ExecutionType] = operation.GetType().Name,
                [PropertyNameConstants.OperationStepIndex] = stepIndex.ToString()
            };

            return logger.BeginScope("OperationExecution", properties);
        }

        /// <summary>
        /// Creates a compensation logging scope with standardized properties.
        /// </summary>
        /// <param name="logger">WorkflowForge logger.</param>
        /// <param name="operationCount">Number of operations to compensate.</param>
        /// <returns>A disposable logging scope.</returns>
        public static IDisposable CreateCompensationScope(this IWorkflowForgeLogger logger, int operationCount)
        {
            var properties = new Dictionary<string, string>
            {
                [PropertyNameConstants.ExecutionId] = Guid.NewGuid().ToString(),
                [PropertyNameConstants.ExecutionName] = "CompensationProcess",
                [PropertyNameConstants.ExecutionType] = "Compensation",
                [PropertyNameConstants.CompensationOperationCount] = operationCount.ToString()
            };

            return logger.BeginScope("CompensationExecution", properties);
        }

        /// <summary>
        /// Builds standardized error property dictionary for logging exceptions.
        /// </summary>
        /// <param name="_">Unused logger parameter to provide extension method shape.</param>
        /// <param name="exception">Exception to describe.</param>
        /// <param name="category">Optional error category label.</param>
        /// <returns>Dictionary of logging properties.</returns>
        public static Dictionary<string, string> CreateErrorProperties(this IWorkflowForgeLogger _, Exception exception, string? category = null)
        {
            return new Dictionary<string, string>
            {
                [PropertyNameConstants.ExceptionType] = exception.GetType().Name,
                [PropertyNameConstants.ErrorCode] = exception.HResult.ToString(),
                [PropertyNameConstants.ErrorCategory] = category ?? "UnhandledException"
            };
        }

        /// <summary>
        /// Builds standardized compensation result property dictionary.
        /// </summary>
        /// <param name="_">Unused logger parameter to provide extension method shape.</param>
        /// <param name="successCount">Successful compensations.</param>
        /// <param name="failureCount">Failed compensations.</param>
        /// <returns>Dictionary of logging properties.</returns>
        public static Dictionary<string, string> CreateCompensationResultProperties(this IWorkflowForgeLogger _, int successCount, int failureCount)
        {
            return new Dictionary<string, string>
            {
                [PropertyNameConstants.CompensationSuccessCount] = successCount.ToString(),
                [PropertyNameConstants.CompensationFailureCount] = failureCount.ToString()
            };
        }
    }
}