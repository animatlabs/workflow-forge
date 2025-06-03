using System;

namespace WorkflowForge.Extensions.Observability.Performance
{
    /// <summary>
    /// Property names specific to performance monitoring for structured logging.
    /// These properties extend the core PropertyNames with performance-specific metrics.
    /// </summary>
    public static class PerformancePropertyNames
    {
        #region Performance Metrics
        
        /// <summary>Operation duration in milliseconds</summary>
        public const string DurationMs = "DurationMs";
        
        /// <summary>Peak memory usage in bytes during operation</summary>
        public const string PeakMemoryUsageBytes = "PeakMemoryUsageBytes";
        
        /// <summary>Memory allocated during operation in bytes</summary>
        public const string MemoryAllocatedBytes = "MemoryAllocatedBytes";
        
        /// <summary>CPU usage percentage during operation</summary>
        public const string CpuUsagePercent = "CpuUsagePercent";
        
        /// <summary>Operations per second throughput</summary>
        public const string OperationsPerSecond = "OperationsPerSecond";
        
        /// <summary>Garbage collection count during operation</summary>
        public const string GcCollectionCount = "GcCollectionCount";
        
        #endregion
        
        #region Performance Thresholds
        
        /// <summary>Performance threshold exceeded indicator</summary>
        public const string ThresholdExceeded = "ThresholdExceeded";
        
        /// <summary>Performance threshold type (duration, memory, etc.)</summary>
        public const string ThresholdType = "ThresholdType";
        
        /// <summary>Performance threshold value</summary>
        public const string ThresholdValue = "ThresholdValue";
        
        /// <summary>Actual performance value that exceeded threshold</summary>
        public const string ActualValue = "ActualValue";
        
        #endregion
        
        #region Batch Processing
        
        /// <summary>Batch size for batch operations</summary>
        public const string BatchSize = "BatchSize";
        
        /// <summary>Items processed in batch</summary>
        public const string ItemsProcessed = "ItemsProcessed";
        
        /// <summary>Batch processing success rate</summary>
        public const string BatchSuccessRate = "BatchSuccessRate";
        
        #endregion
    }
} 