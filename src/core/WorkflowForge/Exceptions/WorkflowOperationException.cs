using System;
using System.Runtime.Serialization;

namespace WorkflowForge.Exceptions
{
    /// <summary>
    /// Exception thrown when an operation within a workflow fails during execution.
    /// Provides detailed context about the failing operation for debugging and monitoring.
    /// </summary>
    [Serializable]
    public class WorkflowOperationException : WorkflowForgeException
    {
        /// <summary>Gets the name of the operation that failed.</summary>
        public string? OperationName { get; }

        /// <summary>Gets the unique identifier of the operation that failed.</summary>
        public Guid? OperationId { get; }

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

        /// <summary>Initializes a new instance with serialized data.</summary>
        protected WorkflowOperationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            OperationName = info.GetString(nameof(OperationName));
            OperationId = (Guid?)info.GetValue(nameof(OperationId), typeof(Guid?));
        }

        /// <summary>Sets serialization info for the exception.</summary>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(OperationName), OperationName);
            info.AddValue(nameof(OperationId), OperationId);
        }
    }
}