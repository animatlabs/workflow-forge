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
        Task<IReadOnlyList<WorkflowExecutionSnapshot>> ListPendingAsync(CancellationToken cancellationToken = default);
    }
}


