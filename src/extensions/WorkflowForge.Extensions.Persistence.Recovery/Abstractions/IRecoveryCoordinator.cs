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
        Task ResumeAsync(
            Func<IWorkflowFoundry> foundryFactory,
            Func<IWorkflow> workflowFactory,
            Guid foundryKey,
            Guid workflowKey,
            CancellationToken cancellationToken = default);

        Task<int> ResumeAllAsync(
            Func<IWorkflowFoundry> foundryFactory,
            Func<IWorkflow> workflowFactory,
            IRecoveryCatalog catalog,
            CancellationToken cancellationToken = default);
    }
}


