using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowForge.Extensions.Audit
{
    /// <summary>
    /// In-memory audit provider for testing and development.
    /// Not recommended for production use.
    /// </summary>
    public sealed class InMemoryAuditProvider : IAuditProvider
    {
        private volatile ConcurrentBag<AuditEntry> _entries = new ConcurrentBag<AuditEntry>();

        /// <summary>
        /// Gets a snapshot of all audit entries.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "S2365", Justification = "Intentional snapshot semantics for thread-safe read access; changing to GetEntries() would break consumers")]
        public IReadOnlyList<AuditEntry> Entries => _entries.ToList();

        /// <summary>
        /// Stores an audit entry in memory.
        /// </summary>
        public Task WriteAuditEntryAsync(AuditEntry entry, CancellationToken cancellationToken = default)
        {
            _entries.Add(entry);
            return Task.CompletedTask;
        }

        /// <summary>
        /// No-op for in-memory provider.
        /// </summary>
        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Clears all audit entries by replacing the bag.
        /// </summary>
        public void Clear()
        {
            _entries = new ConcurrentBag<AuditEntry>();
        }
    }
}
