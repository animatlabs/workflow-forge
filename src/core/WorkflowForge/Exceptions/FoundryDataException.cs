using System;
using System.Runtime.Serialization;

namespace WorkflowForge.Exceptions
{
    /// <summary>
    /// Exception thrown when foundry data operations fail.
    /// Provides context about data access and manipulation errors.
    /// </summary>
    [Serializable]
    public class FoundryDataException : WorkflowForgeException
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
        protected FoundryDataException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            DataKey = info.GetString(nameof(DataKey));
        }

        /// <summary>Sets serialization info for the exception.</summary>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(DataKey), DataKey);
        }
    }
}