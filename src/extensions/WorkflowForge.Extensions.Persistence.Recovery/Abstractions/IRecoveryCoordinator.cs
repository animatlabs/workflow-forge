using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Extensions.Persistence.Recovery
{
    /// <summary>
    /// Coordinates validation and resumption for one or many snapshots.
    /// </summary>
    public interface IRecoveryCoordinator
    {
        /// <summary>
        /// Resumes a single workflow execution from a persisted snapshot identified by the given keys.
        /// </summary>
        /// <param name="foundryFactory">Factory that creates a fresh <see cref="IWorkflowFoundry"/> for the resumed execution.</param>
        /// <param name="workflowFactory">Factory that creates the <see cref="IWorkflow"/> to re-execute.</param>
        /// <param name="foundryKey">The persisted foundry execution identifier.</param>
        /// <param name="workflowKey">The persisted workflow identifier.</param>
        /// <param name="cancellationToken">Token to cancel the resume operation.</param>
        Task ResumeAsync(
            Func<IWorkflowFoundry> foundryFactory,
            Func<IWorkflow> workflowFactory,
            Guid foundryKey,
            Guid workflowKey,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resumes all pending workflow executions returned by the specified <paramref name="catalog"/>.
        /// </summary>
        /// <param name="foundryFactory">Factory that creates a fresh <see cref="IWorkflowFoundry"/> for each resumed execution.</param>
        /// <param name="workflowFactory">Factory that creates the <see cref="IWorkflow"/> to re-execute.</param>
        /// <param name="catalog">The catalog providing pending snapshots.</param>
        /// <param name="cancellationToken">Token to cancel the resume operation.</param>
        /// <returns>The number of successfully resumed executions.</returns>
        Task<int> ResumeAllAsync(
            Func<IWorkflowFoundry> foundryFactory,
            Func<IWorkflow> workflowFactory,
            IRecoveryCatalog catalog,
            CancellationToken cancellationToken = default);
    }
}