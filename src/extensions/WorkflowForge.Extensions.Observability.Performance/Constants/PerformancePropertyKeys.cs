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
        /// Property key marking that the performance middleware has already been registered on the
        /// foundry, so repeated <c>EnablePerformanceMonitoring</c> calls do not register it twice.
        /// </summary>
        internal const string PerformanceMiddleware = "PerformanceMiddleware";
    }
}
