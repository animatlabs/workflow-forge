using System;
using System.Runtime.Serialization;

namespace WorkflowForge.Exceptions
{
    /// <summary>
    /// Exception thrown when workflow restoration/compensation operations fail.
    /// Provides specific context for workflow restoration scenarios.
    /// </summary>
    [Serializable]
    public class WorkflowRestoreException : WorkflowForgeException
    {
        /// <summary>Initializes a new instance with a specified error message.</summary>
        public WorkflowRestoreException(string message) : base(message) { }

        /// <summary>Initializes a new instance with a specified error message and inner exception.</summary>
        public WorkflowRestoreException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>Initializes a new instance with serialized data.</summary>
        protected WorkflowRestoreException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}