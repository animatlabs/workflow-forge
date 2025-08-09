using System;
using System.Collections.Concurrent;

namespace WorkflowForge.Abstractions
{
    /// <summary>
    /// The foundry provides operations with access to workflow properties, logging, and services.
    /// It serves as the execution context for workflow operations, maintaining runtime state and services.
    /// In the WorkflowForge metaphor, the foundry is where raw materials (data) are shaped into finished products (results).
    ///
    /// The foundry maintains a reference to the current workflow being executed and can be reused across multiple workflows
    /// for advanced scenarios like pipeline processing or batch operations.
    /// </summary>
    /// <remarks>
    /// The foundry is the execution environment where operations are performed.
    /// It provides thread-safe access to shared properties, logging capabilities, and dependency injection services.
    /// Each foundry instance represents an execution context that can host one or more workflow executions.
    /// </remarks>
    public interface IWorkflowFoundry : IDisposable
    {
        /// <summary>
        /// Gets the unique identifier for the current foundry execution instance.
        /// This identifier is unique per foundry instance and persists across workflow executions.
        /// </summary>
        Guid ExecutionId { get; }

        /// <summary>
        /// Gets the workflow currently being executed in this foundry.
        /// Can be null if no workflow is currently executing.
        /// This reference is updated when workflows are executed through the foundry.
        /// Access workflow.Id and workflow.Name through this property.
        /// </summary>
        IWorkflow? CurrentWorkflow { get; }

        /// <summary>
        /// Gets the properties dictionary for the foundry.
        /// Thread-safe dictionary for storing and retrieving properties during workflow execution.
        /// Properties persist across multiple workflow executions within the same foundry instance.
        /// Use extension methods for convenient access patterns.
        /// </summary>
        ConcurrentDictionary<string, object?> Properties { get; }

        /// <summary>
        /// Gets the logger for this foundry context.
        /// All logging within the foundry should use this logger for consistent output and correlation.
        /// </summary>
        IWorkflowForgeLogger Logger { get; }

        /// <summary>
        /// Gets the service provider for dependency injection.
        /// Can be null if no dependency injection is configured for this foundry.
        /// </summary>
        IServiceProvider? ServiceProvider { get; }

        /// <summary>
        /// Sets the current workflow being executed in this foundry.
        /// This method is typically called internally by the workflow smith during execution.
        /// </summary>
        /// <param name="workflow">The workflow to set as current. Can be null to clear the current workflow.</param>
        /// <exception cref="ObjectDisposedException">Thrown when the foundry has been disposed.</exception>
        void SetCurrentWorkflow(IWorkflow? workflow);

        /// <summary>
        /// Adds an operation to be executed in this foundry.
        /// </summary>
        /// <param name="operation">The operation to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when operation is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the foundry has been disposed.</exception>
        void AddOperation(IWorkflowOperation operation);

        /// <summary>
        /// Adds middleware to the execution pipeline.
        /// </summary>
        /// <param name="middleware">The middleware to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when middleware is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the foundry has been disposed.</exception>
        void AddMiddleware(IWorkflowOperationMiddleware middleware);
    }
}