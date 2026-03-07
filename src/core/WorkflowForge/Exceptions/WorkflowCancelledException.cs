using System;
using System.Runtime.Serialization;

namespace WorkflowForge.Exceptions
{
    /// <summary>
    /// Exception thrown when a workflow execution is cancelled.
    /// Provides specific context for workflow cancellation scenarios.
    /// </summary>
    [Serializable]
    public sealed class WorkflowCancelledException : WorkflowForgeException
    {
        /// <summary>Initializes a new instance with a specified error message.</summary>
        public WorkflowCancelledException(string message) : base(message) { }

        /// <summary>Initializes a new instance with a specified error message and inner exception.</summary>
        public WorkflowCancelledException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>Initializes a new instance with serialized data.</summary>
        private WorkflowCancelledException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
