using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WorkflowForge.Exceptions
{
    /// <summary>
    /// Exception thrown when an operation within a workflow fails during execution.
    /// Provides detailed context about the failing operation for debugging and monitoring.
    /// </summary>
    [Serializable]
    public sealed class WorkflowOperationException : WorkflowForgeException
    {
        /// <summary>Gets the name of the operation that failed.</summary>
        public string? OperationName { get; }

        /// <summary>Gets the unique identifier of the operation that failed.</summary>
        public Guid? OperationId { get; }

        /// <summary>Gets the execution ID of the foundry where the operation failed.</summary>
        public Guid? ExecutionId { get; }

        /// <summary>Gets the workflow ID where the operation failed.</summary>
        public Guid? WorkflowId { get; }

        /// <summary>Initializes a new instance with operation context.</summary>
        /// <param name="message">Error message.</param>
        /// <param name="operationName">Operation name.</param>
        /// <param name="operationId">Operation identifier.</param>
        public WorkflowOperationException(string message, string? operationName = null, Guid? operationId = null)
            : base(message)
        {
            OperationName = operationName;
            OperationId = operationId;
        }

        /// <summary>Initializes a new instance with inner exception and operation context.</summary>
        public WorkflowOperationException(string message, Exception innerException, string? operationName = null, Guid? operationId = null)
            : base(message, innerException)
        {
            OperationName = operationName;
            OperationId = operationId;
        }

        /// <summary>Initializes a new instance with full context including execution and workflow IDs.</summary>
        /// <param name="message">Error message.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="executionId">The foundry execution ID.</param>
        /// <param name="workflowId">The workflow ID.</param>
        /// <param name="operationName">Operation name.</param>
        /// <param name="operationId">Operation identifier.</param>
        public WorkflowOperationException(
            string message,
            Exception innerException,
            Guid? executionId,
            Guid? workflowId,
            string? operationName,
            Guid? operationId = null)
            : base(FormatMessage(message, executionId, workflowId, operationName), innerException)
        {
            ExecutionId = executionId;
            WorkflowId = workflowId;
            OperationName = operationName;
            OperationId = operationId;
        }

        /// <summary>Initializes a new instance with serialized data.</summary>
        private WorkflowOperationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            OperationName = info.GetString(nameof(OperationName));
            OperationId = (Guid?)info.GetValue(nameof(OperationId), typeof(Guid?));
            ExecutionId = (Guid?)info.GetValue(nameof(ExecutionId), typeof(Guid?));
            WorkflowId = (Guid?)info.GetValue(nameof(WorkflowId), typeof(Guid?));
        }

        /// <summary>Sets serialization info for the exception.</summary>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(OperationName), OperationName);
            info.AddValue(nameof(OperationId), OperationId);
            info.AddValue(nameof(ExecutionId), ExecutionId);
            info.AddValue(nameof(WorkflowId), WorkflowId);
        }

        private static string FormatMessage(string message, Guid? executionId, Guid? workflowId, string? operationName)
        {
            var context = new List<string>();
            if (executionId.HasValue) context.Add($"ExecutionId={executionId}");
            if (workflowId.HasValue) context.Add($"WorkflowId={workflowId}");
            if (!string.IsNullOrEmpty(operationName)) context.Add($"Operation={operationName}");

            return context.Count > 0
                ? $"{message} [{string.Join(", ", context)}]"
                : message;
        }
    }
}