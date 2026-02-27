using System;
using System.Runtime.Serialization;

namespace WorkflowForge.Exceptions
{
    /// <summary>
    /// Exception thrown when workflow configuration is invalid or missing.
    /// Helps identify configuration issues during workflow setup and initialization.
    /// </summary>
    [Serializable]
    public sealed class WorkflowConfigurationException : WorkflowForgeException
    {
        /// <summary>Gets the configuration key that caused the error, if applicable.</summary>
        public string? ConfigurationKey { get; }

        /// <summary>Initializes a new instance with configuration context.</summary>
        public WorkflowConfigurationException(string message, string? configurationKey = null) : base(message)
        {
            ConfigurationKey = configurationKey;
        }

        /// <summary>Initializes a new instance with inner exception and configuration context.</summary>
        public WorkflowConfigurationException(string message, Exception innerException, string? configurationKey = null)
            : base(message, innerException)
        {
            ConfigurationKey = configurationKey;
        }

        /// <summary>Initializes a new instance with serialized data.</summary>
        private WorkflowConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ConfigurationKey = info.GetString(nameof(ConfigurationKey));
        }

        /// <summary>Sets serialization info for the exception.</summary>
#pragma warning disable SYSLIB0051 // Required for .NET Framework 4.8 serialization compatibility

        // Uses obsolete binary serialization APIs intentionally for .NET Framework 4.8 compatibility.
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(ConfigurationKey), ConfigurationKey);
        }

#pragma warning restore SYSLIB0051
    }
}
