using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowForge.Abstractions
{
    /// <summary>
    /// Represents a craftsman (smith) that can forge workflows at the foundry.
    /// Smiths are the primary executors that coordinate workflow operations and manage the forging process.
    /// In the WorkflowForge metaphor, smiths are the skilled artisans who transform raw workflows into finished results.
    ///
    /// Supports both simple pattern (smith manages foundry internally) and advanced pattern (reusable foundry).
    /// </summary>
    public interface IWorkflowSmith : IDisposable, IWorkflowLifecycleEvents, ICompensationLifecycleEvents
    {
        /// <summary>
        /// Forges a workflow asynchronously with automatic foundry management.
        /// This is the recommended simple pattern where the smith creates and manages the foundry internally.
        /// </summary>
        /// <param name="workflow">The workflow to forge.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the forging operation.</returns>
        Task ForgeAsync(IWorkflow workflow, CancellationToken cancellationToken = default);

        /// <summary>
        /// Forges a workflow asynchronously using the provided data dictionary.
        /// The smith creates a foundry internally with the provided data.
        /// </summary>
        /// <param name="workflow">The workflow to forge.</param>
        /// <param name="data">The shared data dictionary for workflow execution.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the forging operation.</returns>
        Task ForgeAsync(IWorkflow workflow, ConcurrentDictionary<string, object?> data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Forges a workflow asynchronously using a pre-configured foundry.
        /// This is the advanced pattern for scenarios requiring foundry reuse across multiple workflows.
        /// The foundry's CurrentWorkflow will be set to the provided workflow during execution.
        /// </summary>
        /// <param name="workflow">The workflow to forge.</param>
        /// <param name="foundry">The foundry providing the execution context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the forging operation.</returns>
        Task ForgeAsync(IWorkflow workflow, IWorkflowFoundry foundry, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new foundry for general workflow execution.
        /// The foundry can be reused across multiple workflow executions.
        /// </summary>
        /// <param name="logger">Optional logger for the foundry. If null, uses smith's default logger.</param>
        /// <param name="serviceProvider">Optional service provider for dependency injection.</param>
        /// <returns>A new foundry instance.</returns>
        IWorkflowFoundry CreateFoundry(IWorkflowForgeLogger? logger = null, IServiceProvider? serviceProvider = null);

        /// <summary>
        /// Creates a new foundry specifically configured for a workflow.
        /// This pre-associates the foundry with the workflow but allows for reuse.
        /// </summary>
        /// <param name="workflow">The workflow this foundry will be associated with.</param>
        /// <param name="logger">Optional logger for the foundry. If null, uses smith's default logger.</param>
        /// <param name="serviceProvider">Optional service provider for dependency injection.</param>
        /// <returns>A new foundry instance configured for the workflow.</returns>
        IWorkflowFoundry CreateFoundryFor(IWorkflow workflow, IWorkflowForgeLogger? logger = null, IServiceProvider? serviceProvider = null);

        /// <summary>
        /// Creates a new foundry with initial data.
        /// </summary>
        /// <param name="data">Initial data for the foundry.</param>
        /// <param name="logger">Optional logger for the foundry. If null, uses smith's default logger.</param>
        /// <param name="serviceProvider">Optional service provider for dependency injection.</param>
        /// <returns>A new foundry instance with the provided data.</returns>
        IWorkflowFoundry CreateFoundryWithData(ConcurrentDictionary<string, object?> data, IWorkflowForgeLogger? logger = null, IServiceProvider? serviceProvider = null);
    }
}