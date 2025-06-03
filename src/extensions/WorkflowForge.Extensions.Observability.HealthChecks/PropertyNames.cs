using System;

namespace WorkflowForge.Extensions.Observability.HealthChecks
{
    /// <summary>
    /// Property names specific to health checks for structured logging.
    /// These properties extend the core PropertyNames with health monitoring context.
    /// </summary>
    public static class HealthCheckPropertyNames
    {
        #region Health Check Execution
        
        /// <summary>Name of the health check being executed</summary>
        public const string HealthCheckName = "HealthCheckName";
        
        /// <summary>Health check execution duration in milliseconds</summary>
        public const string HealthCheckDurationMs = "HealthCheckDurationMs";
        
        /// <summary>Health check status (Healthy, Degraded, Unhealthy)</summary>
        public const string HealthStatus = "HealthStatus";
        
        /// <summary>Health check description or message</summary>
        public const string HealthCheckDescription = "HealthCheckDescription";
        
        #endregion
        
        #region Health Check Service
        
        /// <summary>Total number of registered health checks</summary>
        public const string TotalHealthChecks = "TotalHealthChecks";
        
        /// <summary>Number of healthy checks</summary>
        public const string HealthyCount = "HealthyCount";
        
        /// <summary>Number of degraded checks</summary>
        public const string DegradedCount = "DegradedCount";
        
        /// <summary>Number of unhealthy checks</summary>
        public const string UnhealthyCount = "UnhealthyCount";
        
        /// <summary>Overall system health status</summary>
        public const string OverallHealthStatus = "OverallHealthStatus";
        
        /// <summary>Health check service monitoring interval in milliseconds</summary>
        public const string MonitoringIntervalMs = "MonitoringIntervalMs";
        
        #endregion
        
        #region System Metrics
        
        /// <summary>Current memory usage in bytes</summary>
        public const string MemoryUsageBytes = "MemoryUsageBytes";
        
        /// <summary>Memory usage percentage</summary>
        public const string MemoryUsagePercent = "MemoryUsagePercent";
        
        /// <summary>Thread pool active worker threads</summary>
        public const string ActiveWorkerThreads = "ActiveWorkerThreads";
        
        /// <summary>Thread pool active completion port threads</summary>
        public const string ActiveCompletionPortThreads = "ActiveCompletionPortThreads";
        
        /// <summary>Garbage collection generation 0 count</summary>
        public const string GcGen0Count = "GcGen0Count";
        
        /// <summary>Garbage collection generation 1 count</summary>
        public const string GcGen1Count = "GcGen1Count";
        
        /// <summary>Garbage collection generation 2 count</summary>
        public const string GcGen2Count = "GcGen2Count";
        
        #endregion
        
        #region Thresholds
        
        /// <summary>Memory usage threshold for warnings</summary>
        public const string MemoryThresholdBytes = "MemoryThresholdBytes";
        
        /// <summary>Thread pool utilization threshold</summary>
        public const string ThreadPoolThreshold = "ThreadPoolThreshold";
        
        /// <summary>Threshold exceeded indicator</summary>
        public const string ThresholdExceeded = "ThresholdExceeded";
        
        #endregion
    }
} 