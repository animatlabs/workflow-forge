using System;
using System.Collections.Concurrent;

namespace WorkflowForge.Abstractions
{
    /// <summary>
    /// The foundry provides operations with access to workflow properties, logging, and services.
    /// It serves as the execution context for workflow operations, maintaining runtime state and services.
    /// In the WorkflowForge metaphor, the foundry is where raw materials (data) are shaped into finished products (results).
    ///
    /// <para><strong>Execution Context Pattern:</strong></para>
    /// <para>
    /// The foundry implements the "Ambient Context" pattern, providing operations with access to:
    /// <list type="bullet">
    /// <item><description><strong>Properties</strong>: Thread-safe state management (ConcurrentDictionary) isolated per foundry instance</description></item>
    /// <item><description><strong>Logger</strong>: Structured logging capabilities for operation diagnostics</description></item>
    /// <item><description><strong>ServiceProvider</strong>: Dependency injection for user operations to resolve services</description></item>
    /// <item><description><strong>CurrentWorkflow</strong>: Reference to the workflow being executed</description></item>
    /// </list>
    /// </para>
    ///
    /// <para><strong>Isolation Guarantees:</strong></para>
    /// <para>
    /// Each foundry instance is completely isolated:
    /// <list type="bullet">
    /// <item><description>Unique ExecutionId (Guid.NewGuid())</description></item>
    /// <item><description>Separate Properties dictionary (new ConcurrentDictionary per foundry)</description></item>
    /// <item><description>Independent middleware pipeline</description></item>
    /// <item><description>Isolated event subscriptions</description></item>
    /// </list>
    /// This ensures parallel foundry executions never interfere with each other.
    /// </para>
    ///
    /// <para><strong>Lifecycle:</strong></para>
    /// <para>
    /// Foundries can be reused across multiple workflows for advanced scenarios like pipeline processing
    /// or batch operations. Properties persist across workflow executions within the same foundry instance.
    /// Dispose the foundry when done to release resources.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The foundry is the execution environment where operations are performed.
    /// It provides thread-safe access to shared properties, logging capabilities, and dependency injection services.
    /// Each foundry instance represents an isolated execution context that can host one or more workflow executions.
    /// </remarks>
    public interface IWorkflowFoundry : IDisposable, IOperationLifecycleEvents
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
        ///
        /// <para><strong>Isolation:</strong> Each foundry instance gets its OWN ConcurrentDictionary
        /// (created via 'new ConcurrentDictionary&lt;string, object?&gt;()' in WorkflowSmith.CreateFoundry()).
        /// This ensures parallel foundry executions never share or collide on property data.</para>
        ///
        /// <para><strong>Lifecycle:</strong> Properties persist across multiple workflow executions
        /// within the same foundry instance, but are completely isolated from other foundry instances.</para>
        ///
        /// <para>Use extension methods (GetProperty, SetProperty, TryGetProperty) for convenient access patterns.</para>
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