using System;

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
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));

            return foundry.Properties.TryGetValue("PerformanceStatistics", out var statsObj) && statsObj is IFoundryPerformanceStatistics stats
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
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));

            if (foundry is IPerformanceMonitoredFoundry performanceFoundry)
            {
                return performanceFoundry.EnablePerformanceMonitoring();
            }

            return false;
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
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));

            if (foundry is IPerformanceMonitoredFoundry performanceFoundry)
            {
                return performanceFoundry.DisablePerformanceMonitoring();
            }

            return false;
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
