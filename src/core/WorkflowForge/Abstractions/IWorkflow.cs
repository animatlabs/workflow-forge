using System;
using System.Collections.Generic;

namespace WorkflowForge
{
    /// <summary>
    /// Represents a complete workflow definition with its operations and metadata.
    /// </summary>
    public interface IWorkflow : IDisposable
    {
        /// <summary>
        /// Gets the unique identifier for this workflow.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Gets the name of the workflow.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the optional description of the workflow.
        /// </summary>
        string? Description { get; }

        /// <summary>
        /// Gets the version of the workflow.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Gets the operations that make up this workflow.
        /// </summary>
        IReadOnlyList<IWorkflowOperation> Operations { get; }

        /// <summary>
        /// Gets a value indicating whether this workflow supports restoration (rollback).
        /// </summary>
        bool SupportsRestore { get; }
    }
} 
