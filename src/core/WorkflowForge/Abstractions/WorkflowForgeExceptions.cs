using System;
using System.Runtime.Serialization;

namespace WorkflowForge
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

    /// <summary>
    /// Exception thrown when an operation within a workflow fails during execution.
    /// Provides detailed context about the failing operation for debugging and monitoring.
    /// </summary>
    [Serializable]
    public class WorkflowOperationException : WorkflowForgeException
    {
        /// <summary>
        /// Gets the name of the operation that failed.
        /// </summary>
        public string? OperationName { get; }

        /// <summary>
        /// Gets the unique identifier of the operation that failed.
        /// </summary>
        public Guid? OperationId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowOperationException"/> class with operation context.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="operationName">The name of the operation that failed.</param>
        /// <param name="operationId">The unique identifier of the operation that failed.</param>
        public WorkflowOperationException(string message, string? operationName = null, Guid? operationId = null)
            : base(message)
        {
            OperationName = operationName;
            OperationId = operationId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowOperationException"/> class with operation context and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        /// <param name="operationName">The name of the operation that failed.</param>
        /// <param name="operationId">The unique identifier of the operation that failed.</param>
        public WorkflowOperationException(string message, Exception innerException, string? operationName = null, Guid? operationId = null)
            : base(message, innerException)
        {
            OperationName = operationName;
            OperationId = operationId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowOperationException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected WorkflowOperationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            OperationName = info.GetString(nameof(OperationName));
            OperationId = (Guid?)info.GetValue(nameof(OperationId), typeof(Guid?));
        }

        /// <summary>
        /// Sets the <see cref="SerializationInfo"/> with information about the exception.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(OperationName), OperationName);
            info.AddValue(nameof(OperationId), OperationId);
        }
    }

    /// <summary>
    /// Exception thrown when workflow configuration is invalid or missing.
    /// Helps identify configuration issues during workflow setup and initialization.
    /// </summary>
    [Serializable]
    public class WorkflowConfigurationException : WorkflowForgeException
    {
        /// <summary>
        /// Gets the configuration key that caused the error, if applicable.
        /// </summary>
        public string? ConfigurationKey { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowConfigurationException"/> class with configuration context.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="configurationKey">The configuration key that caused the error.</param>
        public WorkflowConfigurationException(string message, string? configurationKey = null) : base(message)
        {
            ConfigurationKey = configurationKey;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowConfigurationException"/> class with configuration context and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        /// <param name="configurationKey">The configuration key that caused the error.</param>
        public WorkflowConfigurationException(string message, Exception innerException, string? configurationKey = null)
            : base(message, innerException)
        {
            ConfigurationKey = configurationKey;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowConfigurationException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected WorkflowConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ConfigurationKey = info.GetString(nameof(ConfigurationKey));
        }

        /// <summary>
        /// Sets the <see cref="SerializationInfo"/> with information about the exception.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(ConfigurationKey), ConfigurationKey);
        }
    }

    /// <summary>
    /// Exception thrown when foundry data operations fail.
    /// Provides context about data access and manipulation errors.
    /// </summary>
    [Serializable]
    public class FoundryDataException : WorkflowForgeException
    {
        /// <summary>
        /// Gets the data key that caused the error, if applicable.
        /// </summary>
        public string? DataKey { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FoundryDataException"/> class with data context.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="dataKey">The data key that caused the error.</param>
        public FoundryDataException(string message, string? dataKey = null) : base(message)
        {
            DataKey = dataKey;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FoundryDataException"/> class with data context and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        /// <param name="dataKey">The data key that caused the error.</param>
        public FoundryDataException(string message, Exception innerException, string? dataKey = null)
            : base(message, innerException)
        {
            DataKey = dataKey;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FoundryDataException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected FoundryDataException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            DataKey = info.GetString(nameof(DataKey));
        }

        /// <summary>
        /// Sets the <see cref="SerializationInfo"/> with information about the exception.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(DataKey), DataKey);
        }
    }

    /// <summary>
    /// Exception thrown when a workflow execution is cancelled.
    /// Provides specific context for workflow cancellation scenarios.
    /// </summary>
    [Serializable]
    public class WorkflowCancelledException : WorkflowForgeException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowCancelledException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public WorkflowCancelledException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowCancelledException"/> class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public WorkflowCancelledException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowCancelledException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected WorkflowCancelledException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Exception thrown when workflow restoration/compensation operations fail.
    /// Provides specific context for workflow restoration scenarios.
    /// </summary>
    [Serializable]
    public class WorkflowRestoreException : WorkflowForgeException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowRestoreException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public WorkflowRestoreException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowRestoreException"/> class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public WorkflowRestoreException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowRestoreException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected WorkflowRestoreException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
} 
