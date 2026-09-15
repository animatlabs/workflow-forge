using System;
using System.Collections.Concurrent;
using WorkflowForge.Options;

namespace WorkflowForge.Abstractions
{
    /// <summary>
    /// Provides access to the execution context for workflow operations.
    /// This interface represents the ambient context pattern, giving operations access to
    /// runtime state, properties, logging, and dependency injection services.
    /// </summary>
    /// <remarks>
    /// This interface follows the Interface Segregation Principle (ISP), providing only
    /// context-related members. Operations that only need to read context data can depend
    /// on this interface rather than the full IWorkflowFoundry.
    /// </remarks>
    public interface IWorkflowExecutionContext
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
        /// <para><strong>Isolation:</strong> A foundry lease never shares property data with a concurrent lease.
        /// Concurrent workflow executions therefore cannot collide on property data.</para>
        ///
        /// <para><strong>Lifecycle:</strong> On the default pooled smith path, <c>Properties</c> are cleared after each
        /// workflow. When you pass a caller-owned foundry to <c>ForgeAsync(workflow, foundry)</c>, properties persist
        /// until you dispose that foundry. When you pass a dictionary to <c>ForgeAsync(workflow, data)</c>, the smith
        /// uses it directly and leaves its contents in place so results can be read back. Values in <c>Properties</c>
        /// are never auto-disposed; use <see cref="Services"/> for extension-owned
        /// <see cref="System.IDisposable"/> resources.</para>
        ///
        /// <para>Use extension methods (GetProperty, SetProperty, TryGetProperty) for convenient access patterns.</para>
        /// </summary>
        ConcurrentDictionary<string, object?> Properties { get; }

        /// <summary>
        /// Gets foundry-scoped services (extension capabilities). Disposed when the foundry lease ends.
        /// </summary>
        IFoundryServices Services { get; }

        /// <summary>
        /// Gets the logger for this foundry context.
        /// All logging within the foundry should use this logger for consistent output and correlation.
        /// </summary>
        IWorkflowForgeLogger Logger { get; }

        /// <summary>
        /// Gets the execution options for this foundry.
        /// </summary>
        WorkflowForgeOptions Options { get; }

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
    }
}