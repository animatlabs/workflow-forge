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
    /// In that mode the middleware resolves the active statistics from the foundry properties on each
    /// call, so enabling/disabling monitoring is a matter of setting/removing that property — the
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
        /// Initializes a middleware that resolves the active statistics from the foundry properties
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

            var stopwatch = Stopwatch.StartNew();
            // GC.GetTotalMemory is a coarse, process-wide approximation (the only allocation API
            // available on netstandard2.0); treated as best-effort and clamped to non-negative.
            var memoryBefore = GC.GetTotalMemory(false);
            var success = false;

            try
            {
                var result = await next(cancellationToken).ConfigureAwait(false);
                success = true;
                return result;
            }
            finally
            {
                stopwatch.Stop();
                var memoryDelta = GC.GetTotalMemory(false) - memoryBefore;
                statistics.Record(operation.Name, operation.Id.ToString(), stopwatch.Elapsed, success, memoryDelta);
            }
        }

        private static FoundryPerformanceStatistics? ResolveStatistics(IWorkflowFoundry foundry)
        {
            return foundry.Properties.TryGetValue(PerformancePropertyKeys.PerformanceStatistics, out var statsObj)
                && statsObj is FoundryPerformanceStatistics statistics
                ? statistics
                : null;
        }
    }
}
