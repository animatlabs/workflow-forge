using System;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowForge.Extensions.Persistence.Abstractions
{
    /// <summary>
    /// Abstraction for persisting and restoring workflow execution state.
    /// Implementations are provided by consumers to integrate with their chosen storage.
    /// </summary>
    public interface IWorkflowPersistenceProvider
    {
        /// <summary>
        /// Saves a snapshot representing the current workflow execution state.
        /// </summary>
        /// <param name="snapshot">The execution snapshot to save.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        Task SaveAsync(WorkflowExecutionSnapshot snapshot, CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts to load a previously saved snapshot for the given execution and workflow IDs.
        /// Returns null if no snapshot is available.
        /// </summary>
        /// <param name="foundryExecutionId">The foundry execution identifier.</param>
        /// <param name="workflowId">The workflow identifier.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>The snapshot if found; otherwise, null.</returns>
        Task<WorkflowExecutionSnapshot?> TryLoadAsync(Guid foundryExecutionId, Guid workflowId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a previously saved snapshot for the given execution and workflow IDs.
        /// Implementations may choose to ignore when not found.
        /// </summary>
        /// <param name="foundryExecutionId">The foundry execution identifier.</param>
        /// <param name="workflowId">The workflow identifier.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        Task DeleteAsync(Guid foundryExecutionId, Guid workflowId, CancellationToken cancellationToken = default);
    }
}