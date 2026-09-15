using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowForge.Extensions.Audit
{
    /// <summary>
    /// In-memory audit provider for testing and development.
    /// Not recommended for production use.
    /// </summary>
    /// <remarks>
    /// Entries are stored in insertion order. Once <see cref="MaxEntries"/> entries are held, writing
    /// another discards the oldest so memory use stays bounded.
    /// </remarks>
    public sealed class InMemoryAuditProvider : IAuditProvider
    {
        /// <summary>
        /// Default maximum number of audit entries retained in memory.
        /// </summary>
        public const int DefaultMaxEntries = 10_000;

        private readonly object _sync = new();
        private readonly Queue<AuditEntry> _entries = new();
        private readonly int _maxEntries;

        /// <summary>
        /// Initializes an in-memory audit provider with the default entry cap.
        /// </summary>
        public InMemoryAuditProvider()
            : this(DefaultMaxEntries)
        {
        }

        /// <summary>
        /// Initializes an in-memory audit provider with a custom entry cap.
        /// </summary>
        /// <param name="maxEntries">Maximum entries to retain; the oldest entry is discarded once the cap is reached.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxEntries"/> is less than one.</exception>
        public InMemoryAuditProvider(int maxEntries)
        {
            if (maxEntries < 1)
                throw new ArgumentOutOfRangeException(nameof(maxEntries), maxEntries, "Maximum entries must be at least one.");

            _maxEntries = maxEntries;
        }

        /// <summary>
        /// Gets the maximum number of entries retained.
        /// </summary>
        public int MaxEntries => _maxEntries;

        /// <summary>
        /// Gets a snapshot of all audit entries.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "S2365", Justification = "Intentional snapshot semantics for thread-safe read access; changing to GetEntries() would break consumers")]
        public IReadOnlyList<AuditEntry> Entries
        {
            get
            {
                lock (_sync)
                {
                    return _entries.ToArray();
                }
            }
        }

        /// <summary>
        /// Stores an audit entry in memory.
        /// </summary>
        public Task WriteAuditEntryAsync(AuditEntry entry, CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                while (_entries.Count >= _maxEntries)
                {
                    _entries.Dequeue();
                }

                _entries.Enqueue(entry);
            }

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
        /// Clears all audit entries.
        /// </summary>
        public void Clear()
        {
            lock (_sync)
            {
                _entries.Clear();
            }
        }
    }
}
