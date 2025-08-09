using System;
using System.Runtime.Serialization;

namespace WorkflowForge.Exceptions
{
    /// <summary>
    /// Base exception for all WorkflowForge-related errors.
    /// Provides a foundation for structured error handling across the WorkflowForge ecosystem.
    /// </summary>
    [Serializable]
    public class WorkflowForgeException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowForgeException"/> class with a default message.
        /// </summary>
        public WorkflowForgeException() : base("A WorkflowForge operation failed.") { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowForgeException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public WorkflowForgeException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowForgeException"/> class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public WorkflowForgeException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowForgeException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected WorkflowForgeException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}