using System;
using System.Runtime.Serialization;

namespace WorkflowForge.Exceptions
{
    /// <summary>
    /// Exception thrown when foundry data operations fail.
    /// Provides context about data access and manipulation errors.
    /// </summary>
    [Serializable]
    public sealed class FoundryDataException : WorkflowForgeException
    {
        /// <summary>Gets the data key that caused the error, if applicable.</summary>
        public string? DataKey { get; }

        /// <summary>Initializes a new instance with data context.</summary>
        public FoundryDataException(string message, string? dataKey = null) : base(message)
        {
            DataKey = dataKey;
        }

        /// <summary>Initializes a new instance with inner exception and data context.</summary>
        public FoundryDataException(string message, Exception innerException, string? dataKey = null)
            : base(message, innerException)
        {
            DataKey = dataKey;
        }

        /// <summary>Initializes a new instance with serialized data.</summary>
        private FoundryDataException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            DataKey = info.GetString(nameof(DataKey));
        }

        /// <summary>Sets serialization info for the exception.</summary>
#pragma warning disable SYSLIB0051 // Required for .NET Framework 4.8 serialization compatibility

        // Uses obsolete binary serialization APIs intentionally for .NET Framework 4.8 compatibility.
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(DataKey), DataKey);
        }

#pragma warning restore SYSLIB0051
    }
}
