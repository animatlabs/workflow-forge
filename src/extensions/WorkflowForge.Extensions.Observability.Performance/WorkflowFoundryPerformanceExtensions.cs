using System;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Observability.Performance.Abstractions;
using WorkflowForge.Extensions.Observability.Performance.Constants;

namespace WorkflowForge.Extensions.Observability.Performance
{
    /// <summary>
    /// Extension methods that add performance monitoring capabilities to WorkflowFoundry.
    /// These extensions are available when the WorkflowForge.Extensions.Observability.Performance package is installed.
    /// </summary>
    public static class WorkflowFoundryPerformanceExtensions
    {
        /// <summary>
        /// Gets performance statistics from the foundry.
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <returns>Performance statistics if available; otherwise, null.</returns>
        public static IFoundryPerformanceStatistics? GetPerformanceStatistics(this IWorkflowFoundry foundry)
        {
            if (foundry == null)
                throw new ArgumentNullException(nameof(foundry));

            // A foundry with native support wins; otherwise read the stats recorded by the middleware.
            if (foundry is IPerformanceMonitoredFoundry performanceFoundry)
            {
                return performanceFoundry.GetPerformanceStatistics();
            }

            return foundry.Properties.TryGetValue(PerformancePropertyKeys.PerformanceStatistics, out var statsObj) && statsObj is IFoundryPerformanceStatistics stats
                ? stats
                : null;
        }

        /// <summary>
        /// Enables performance monitoring for the foundry.
        /// This method is available as an extension when WorkflowForge.Extensions.Observability.Performance is installed.
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <returns>True if performance monitoring was enabled; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when foundry is null.</exception>
        public static bool EnablePerformanceMonitoring(this IWorkflowFoundry foundry)
        {
            if (foundry == null)
                throw new ArgumentNullException(nameof(foundry));

            // A foundry with native support handles it directly.
            if (foundry is IPerformanceMonitoredFoundry performanceFoundry)
            {
                return performanceFoundry.EnablePerformanceMonitoring();
            }

            // Standard foundry: store a fresh statistics accumulator under the well-known key. The
            // PerformanceStatisticsMiddleware resolves that key on each operation and records into it.
            foundry.Properties[PerformancePropertyKeys.PerformanceStatistics] = new FoundryPerformanceStatistics();

            // Register the middleware exactly once per foundry, even under concurrent EnablePerformanceMonitoring
            // calls. TryAdd is atomic; a ContainsKey-then-add check would be a race that could register the
            // middleware twice and permanently double-count every operation.
            var middleware = new PerformanceStatisticsMiddleware();
            if (foundry.Properties.TryAdd(PerformancePropertyKeys.PerformanceMiddleware, middleware))
            {
                foundry.AddMiddleware(middleware);
            }

            return true;
        }

        /// <summary>
        /// Disables performance monitoring for the foundry.
        /// This method is available as an extension when WorkflowForge.Extensions.Observability.Performance is installed.
        /// </summary>
        /// <param name="foundry">The workflow foundry.</param>
        /// <returns>True if performance monitoring was disabled; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when foundry is null.</exception>
        public static bool DisablePerformanceMonitoring(this IWorkflowFoundry foundry)
        {
            if (foundry == null)
                throw new ArgumentNullException(nameof(foundry));

            if (foundry is IPerformanceMonitoredFoundry performanceFoundry)
            {
                return performanceFoundry.DisablePerformanceMonitoring();
            }

            // Removing the statistics deactivates recording. The middleware stays registered but
            // becomes a no-op pass-through until monitoring is enabled again. Returns whether
            // monitoring was active.
            return foundry.Properties.TryRemove(PerformancePropertyKeys.PerformanceStatistics, out _);
        }
    }

    /// <summary>
    /// Interface for foundries that support performance monitoring.
    /// Implementations should extend this interface when WorkflowForge.Extensions.Observability.Performance is available.
    /// </summary>
    public interface IPerformanceMonitoredFoundry
    {
        /// <summary>
        /// Gets the performance statistics for this foundry.
        /// </summary>
        /// <returns>Performance statistics or null if monitoring is disabled.</returns>
        IFoundryPerformanceStatistics? GetPerformanceStatistics();

        /// <summary>
        /// Enables performance monitoring for this foundry.
        /// </summary>
        /// <returns>True if monitoring was enabled; otherwise, false.</returns>
        bool EnablePerformanceMonitoring();

        /// <summary>
        /// Disables performance monitoring for this foundry.
        /// </summary>
        /// <returns>True if monitoring was disabled; otherwise, false.</returns>
        bool DisablePerformanceMonitoring();

        /// <summary>
        /// Gets a value indicating whether performance monitoring is currently enabled.
        /// </summary>
        bool IsPerformanceMonitoringEnabled { get; }
    }
}
