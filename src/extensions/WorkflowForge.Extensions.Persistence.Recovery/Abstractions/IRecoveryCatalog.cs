using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Extensions.Persistence.Abstractions;

namespace WorkflowForge.Extensions.Persistence.Recovery
{
    /// <summary>
    /// Provides read access to pending snapshots that may need recovery.
    /// Implement using your storage semantics (queries, partitions, tags).
    /// </summary>
    public interface IRecoveryCatalog
    {
        /// <summary>
        /// Lists all pending workflow snapshots that are eligible for recovery.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the listing operation.</param>
        /// <returns>A read-only collection of pending snapshots.</returns>
        Task<IReadOnlyList<WorkflowExecutionSnapshot>> ListPendingAsync(CancellationToken cancellationToken = default);
    }
}