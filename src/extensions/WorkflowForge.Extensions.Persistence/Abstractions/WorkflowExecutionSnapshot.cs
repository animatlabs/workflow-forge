using System;
using System.Collections.Generic;

namespace WorkflowForge.Extensions.Persistence.Abstractions
{
    /// <summary>
    /// Represents a persisted snapshot of a workflow execution.
    /// Consumers are responsible for serialization, storage, and schema evolution.
    /// </summary>
    public sealed class WorkflowExecutionSnapshot
    {
        /// <summary>
        /// The foundry execution identifier associated with this snapshot.
        /// </summary>
        public Guid FoundryExecutionId { get; set; }

        /// <summary>
        /// The workflow identifier associated with this snapshot.
        /// </summary>
        public Guid WorkflowId { get; set; }

        /// <summary>
        /// The workflow name captured for diagnostic purposes.
        /// </summary>
        public string WorkflowName { get; set; } = string.Empty;

        /// <summary>
        /// Index of the next operation to execute.
        /// -1 indicates not started; value N means operation at index N is next.
        /// </summary>
        public int NextOperationIndex { get; set; }

        /// <summary>
        /// Arbitrary key-value state captured from foundry properties.
        /// Only store what is necessary for resumption.
        /// </summary>
        public Dictionary<string, object?> Properties { get; set; } = new();
    }
}