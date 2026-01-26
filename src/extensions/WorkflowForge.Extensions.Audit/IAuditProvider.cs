using System.Threading;
using System.Threading.Tasks;

namespace WorkflowForge.Extensions.Audit
{
    /// <summary>
    /// Defines a provider for storing audit entries.
    /// Implement this interface to integrate with your storage system (database, file, etc.).
    /// </summary>
    public interface IAuditProvider
    {
        /// <summary>
        /// Stores an audit entry asynchronously.
        /// </summary>
        /// <param name="entry">The audit entry to store.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task WriteAuditEntryAsync(AuditEntry entry, CancellationToken cancellationToken = default);

        /// <summary>
        /// Flushes any buffered audit entries to storage.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task FlushAsync(CancellationToken cancellationToken = default);
    }
}