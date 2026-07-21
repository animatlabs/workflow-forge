namespace WorkflowForge.Extensions.Observability.Performance.Constants
{
    /// <summary>
    /// Well-known property keys used by the Performance extension in foundry properties.
    /// </summary>
    internal static class PerformancePropertyKeys
    {
        /// <summary>Property key for storing performance statistics on the foundry.</summary>
        internal const string PerformanceStatistics = "PerformanceStatistics";

        /// <summary>
        /// Property key for storing the performance middleware instance on the foundry, so that
        /// <c>DisablePerformanceMonitoring</c> can remove it again.
        /// </summary>
        internal const string PerformanceMiddleware = "PerformanceMiddleware";
    }
}