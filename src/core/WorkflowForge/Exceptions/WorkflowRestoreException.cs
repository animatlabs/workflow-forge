using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WorkflowForge.Exceptions
{
    /// <summary>
    /// Exception thrown when workflow restoration/compensation operations fail.
    /// Provides specific context for workflow restoration scenarios.
    /// </summary>
    [Serializable]
    public sealed class WorkflowRestoreException : WorkflowForgeException
    {
        /// <summary>Gets the name of the operation that failed restoration.</summary>
        public string? OperationName { get; }

        /// <summary>Gets the execution ID of the foundry where restoration failed.</summary>
        public Guid? ExecutionId { get; }

        /// <summary>Gets the workflow ID where restoration failed.</summary>
        public Guid? WorkflowId { get; }

        /// <summary>Initializes a new instance with a specified error message.</summary>
        public WorkflowRestoreException(string message) : base(message) { }

        /// <summary>Initializes a new instance with a specified error message and inner exception.</summary>
        public WorkflowRestoreException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>Initializes a new instance with full context including execution and workflow IDs.</summary>
        /// <param name="message">Error message.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="executionId">The foundry execution ID.</param>
        /// <param name="workflowId">The workflow ID.</param>
        /// <param name="operationName">Operation name.</param>
        public WorkflowRestoreException(
            string message,
            Exception innerException,
            Guid? executionId,
            Guid? workflowId,
            string? operationName)
            : base(FormatMessage(message, executionId, workflowId, operationName), innerException)
        {
            ExecutionId = executionId;
            WorkflowId = workflowId;
            OperationName = operationName;
        }

        /// <summary>Initializes a new instance with serialized data.</summary>
        private WorkflowRestoreException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            OperationName = info.GetString(nameof(OperationName));
            ExecutionId = (Guid?)info.GetValue(nameof(ExecutionId), typeof(Guid?));
            WorkflowId = (Guid?)info.GetValue(nameof(WorkflowId), typeof(Guid?));
        }

        /// <summary>Sets serialization info for the exception.</summary>
#pragma warning disable SYSLIB0051 // Required for .NET Framework 4.8 serialization compatibility
        // Uses obsolete binary serialization APIs intentionally for .NET Framework 4.8 compatibility.
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(OperationName), OperationName);
            info.AddValue(nameof(ExecutionId), ExecutionId);
            info.AddValue(nameof(WorkflowId), WorkflowId);
        }
#pragma warning restore SYSLIB0051

        private static string FormatMessage(string message, Guid? executionId, Guid? workflowId, string? operationName)
        {
            var context = new List<string>();
            if (executionId.HasValue)
                context.Add($"ExecutionId={executionId}");
            if (workflowId.HasValue)
                context.Add($"WorkflowId={workflowId}");
            if (!string.IsNullOrEmpty(operationName))
                context.Add($"Operation={operationName}");

            return context.Count > 0
                ? $"{message} [{string.Join(", ", context)}]"
                : message;
        }
    }
}
