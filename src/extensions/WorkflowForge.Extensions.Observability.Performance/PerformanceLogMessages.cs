namespace WorkflowForge.Extensions.Observability.Performance
{
    /// <summary>
    /// Performance-specific log messages for the Performance extension.
    /// These messages complement the core WorkflowLogMessages with performance monitoring concerns.
    /// </summary>
    public static class PerformanceLogMessages
    {
        #region Performance Monitoring

        /// <summary>Message for performance monitoring enablement</summary>
        public const string PerformanceMonitoringEnabled = "Performance monitoring enabled";

        /// <summary>Message for performance monitoring disablement</summary>
        public const string PerformanceMonitoringDisabled = "Performance monitoring disabled";

        /// <summary>Message for performance statistics reset</summary>
        public const string PerformanceStatisticsReset = "Performance statistics reset";

        #endregion Performance Monitoring

        #region Performance Metrics

        /// <summary>Message for operation timing recorded</summary>
        public const string OperationTimingRecorded = "Operation timing metrics recorded";

        /// <summary>Message for memory usage recorded</summary>
        public const string MemoryUsageRecorded = "Memory usage metrics recorded";

        /// <summary>Message for CPU usage recorded</summary>
        public const string CpuUsageRecorded = "CPU usage metrics recorded";

        /// <summary>Message for throughput metrics recorded</summary>
        public const string ThroughputMetricsRecorded = "Throughput metrics recorded";

        #endregion Performance Metrics

        #region Performance Thresholds

        /// <summary>Message for performance threshold exceeded</summary>
        public const string PerformanceThresholdExceeded = "Performance threshold exceeded";

        /// <summary>Message for duration threshold exceeded</summary>
        public const string DurationThresholdExceeded = "Duration threshold exceeded";

        /// <summary>Message for memory threshold exceeded</summary>
        public const string MemoryThresholdExceeded = "Memory threshold exceeded";

        /// <summary>Message for CPU threshold exceeded</summary>
        public const string CpuThresholdExceeded = "CPU usage threshold exceeded";

        /// <summary>Message for throughput threshold breached</summary>
        public const string ThroughputThresholdBreached = "Throughput threshold breached";

        #endregion Performance Thresholds

        #region Performance Analysis

        /// <summary>Message for performance analysis started</summary>
        public const string PerformanceAnalysisStarted = "Performance analysis started";

        /// <summary>Message for performance analysis completed</summary>
        public const string PerformanceAnalysisCompleted = "Performance analysis completed";

        /// <summary>Message for performance degradation detected</summary>
        public const string PerformanceDegradationDetected = "Performance degradation detected";

        /// <summary>Message for performance improvement detected</summary>
        public const string PerformanceImprovementDetected = "Performance improvement detected";

        #endregion Performance Analysis

        #region Batch Processing

        /// <summary>Message for batch processing started</summary>
        public const string BatchProcessingStarted = "Batch processing started";

        /// <summary>Message for batch processing completed</summary>
        public const string BatchProcessingCompleted = "Batch processing completed";

        /// <summary>Message for batch processing failed</summary>
        public const string BatchProcessingFailed = "Batch processing failed";

        /// <summary>Message for batch item processed</summary>
        public const string BatchItemProcessed = "Batch item processed";

        #endregion Batch Processing
    }
}