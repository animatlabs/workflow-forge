namespace WorkflowForge.Extensions.Observability.HealthChecks
{
    /// <summary>
    /// Health check specific log messages for the HealthChecks extension.
    /// These messages complement the core WorkflowLogMessages with health monitoring concerns.
    /// </summary>
    public static class HealthCheckLogMessages
    {
        #region Health Check Service

        /// <summary>Message for health check service started</summary>
        public const string HealthCheckServiceStarted = "Health check service started with periodic monitoring";

        /// <summary>Message for health check service disposed</summary>
        public const string HealthCheckServiceDisposed = "Health check service disposed";

        /// <summary>Message for periodic health check failed</summary>
        public const string PeriodicHealthCheckFailed = "Periodic health check execution failed";

        #endregion Health Check Service

        #region Health Check Registration

        /// <summary>Message for health check registration</summary>
        public const string HealthCheckRegistered = "Health check registered successfully";

        /// <summary>Message for health check unregistration</summary>
        public const string HealthCheckUnregistered = "Health check unregistered successfully";

        /// <summary>Message for health check not found</summary>
        public const string HealthCheckNotFound = "Health check not found for execution";

        /// <summary>Message for no health checks registered</summary>
        public const string NoHealthChecksRegistered = "No health checks registered for execution";

        #endregion Health Check Registration

        #region Health Check Execution

        /// <summary>Message for health check execution started</summary>
        public const string HealthCheckExecutionStarted = "Health check execution started";

        /// <summary>Message for health check execution completed</summary>
        public const string HealthCheckExecutionCompleted = "Health check execution completed";

        /// <summary>Message for health check execution failed</summary>
        public const string HealthCheckExecutionFailed = "Health check execution failed";

        /// <summary>Message for all health checks completed</summary>
        public const string AllHealthChecksCompleted = "All health checks completed";

        /// <summary>Message for health checks completion failed</summary>
        public const string HealthChecksCompletionFailed = "Failed to complete health check execution";

        #endregion Health Check Execution

        #region System Health Status

        /// <summary>Message for system healthy status</summary>
        public const string SystemHealthy = "System health status is healthy";

        /// <summary>Message for system degraded status</summary>
        public const string SystemDegraded = "System health status is degraded";

        /// <summary>Message for system unhealthy status</summary>
        public const string SystemUnhealthy = "System health status is unhealthy";

        #endregion System Health Status

        #region Resource Monitoring

        /// <summary>Message for memory threshold exceeded</summary>
        public const string MemoryThresholdExceeded = "Memory usage threshold exceeded";

        /// <summary>Message for thread pool threshold exceeded</summary>
        public const string ThreadPoolThresholdExceeded = "Thread pool utilization threshold exceeded";

        /// <summary>Message for garbage collection pressure detected</summary>
        public const string GarbageCollectionPressureDetected = "High garbage collection pressure detected";

        /// <summary>Message for resource monitoring healthy</summary>
        public const string ResourceMonitoringHealthy = "Resource monitoring indicates healthy status";

        #endregion Resource Monitoring

        #region Health Check Disposal

        /// <summary>Message for health check disposal error</summary>
        public const string HealthCheckDisposalError = "Error occurred while disposing health check";

        #endregion Health Check Disposal
    }
}