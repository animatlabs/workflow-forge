using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Observability.Performance.Constants;

namespace WorkflowForge.Extensions.Observability.Performance
{
    /// <summary>
    /// Operation middleware that records timing, success/failure, and approximate memory allocation
    /// for each operation into a <see cref="FoundryPerformanceStatistics"/> instance.
    /// <para>
    /// Added automatically by <see cref="WorkflowFoundryPerformanceExtensions.EnablePerformanceMonitoring"/>.
    /// In that mode the middleware resolves the active statistics from the foundry services on each
    /// call, so enabling/disabling monitoring is a matter of setting/removing that service — the
    /// middleware simply no-ops while no statistics object is present.
    /// </para>
    /// <para>
    /// For standalone use, construct it with an explicit <see cref="FoundryPerformanceStatistics"/>
    /// and add it via <c>foundry.AddMiddleware(...)</c>; it will always record into that instance.
    /// </para>
    /// </summary>
    public sealed class PerformanceStatisticsMiddleware : IWorkflowOperationMiddleware
    {
        private readonly FoundryPerformanceStatistics? _explicitStatistics;

        /// <summary>
        /// Initializes a middleware that resolves the active statistics from the foundry services
        /// on each call (the mode used by <c>EnablePerformanceMonitoring</c>).
        /// </summary>
        public PerformanceStatisticsMiddleware()
        {
            _explicitStatistics = null;
        }

        /// <summary>
        /// Initializes a middleware that always records into the supplied statistics instance.
        /// </summary>
        /// <param name="statistics">The statistics accumulator to record into.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="statistics"/> is null.</exception>
        public PerformanceStatisticsMiddleware(FoundryPerformanceStatistics statistics)
        {
            _explicitStatistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
        }

        /// <inheritdoc />
        public async Task<object?> ExecuteAsync(
            IWorkflowOperation operation,
            IWorkflowFoundry foundry,
            object? inputData,
            Func<CancellationToken, Task<object?>> next,
            CancellationToken cancellationToken = default)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (next == null) throw new ArgumentNullException(nameof(next));

            var statistics = _explicitStatistics ?? ResolveStatistics(foundry);
            if (statistics == null)
            {
                // Monitoring not active on this foundry: pass through with no overhead.
                return await next(cancellationToken).ConfigureAwait(false);
            }

            var startTimestamp = Stopwatch.GetTimestamp();
            var memoryBefore = GetAllocatedBytesSnapshot();
            var success = false;

            try
            {
                var result = await next(cancellationToken).ConfigureAwait(false);
                success = true;
                return result;
            }
            finally
            {
                var timestampDelta = Stopwatch.GetTimestamp() - startTimestamp;
                var elapsed = TimeSpan.FromSeconds(timestampDelta / (double)Stopwatch.Frequency);
                var memoryDelta = GetAllocatedBytesSnapshot() - memoryBefore;
                if (memoryDelta < 0)
                {
                    memoryDelta = 0;
                }

                statistics.Record(operation.Name, operation.Id.ToString(), elapsed, success, memoryDelta);
            }
        }

        /// <remarks>
        /// Process-wide, so the recorded delta is only indicative and is not attributable to a
        /// single operation when workflows run concurrently.
        /// </remarks>
        private static long GetAllocatedBytesSnapshot() => GC.GetTotalMemory(false);

        private static FoundryPerformanceStatistics? ResolveStatistics(IWorkflowFoundry foundry)
        {
            if (foundry.Services != null
                && foundry.Services.TryGet<FoundryPerformanceStatistics>(PerformancePropertyKeys.PerformanceStatistics, out var fromServices))
            {
                return fromServices;
            }

            // Legacy: statistics may still be on Properties when Services is unavailable.
            return foundry.Properties.TryGetValue(PerformancePropertyKeys.PerformanceStatistics, out var statsObj)
                && statsObj is FoundryPerformanceStatistics statistics
                ? statistics
                : null;
        }
    }
}
