using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Threading;
using WorkflowForge.Abstractions;
using WorkflowForge.Loggers;

namespace WorkflowForge.Extensions.Observability.OpenTelemetry
{
    /// <summary>
    /// Comprehensive OpenTelemetry service providing both distributed tracing and metrics
    /// for WorkflowForge applications using industry-standard OpenTelemetry APIs.
    /// </summary>
    public sealed class WorkflowForgeOpenTelemetryService : IDisposable
    {
        private readonly ActivitySource _activitySource;
        private readonly Meter _meter;
        private readonly IWorkflowForgeLogger _logger;

        // Standard WorkflowForge metrics following OpenTelemetry semantic conventions
        private readonly Counter<long> _operationsTotal;

        private readonly Counter<long> _operationErrorsTotal;
        private readonly Histogram<double> _operationDuration;
        private readonly Histogram<long> _operationMemoryAllocations;
        private readonly UpDownCounter<int> _activeOperations;

        // System metrics -- fields are assigned by the Meter SDK and must be kept alive
        // to prevent the observable gauges from being garbage collected.
        [SuppressMessage("CodeQuality", "S4487", Justification = "Observable instruments must be held by reference to prevent garbage collection")]
        private readonly ObservableGauge<long> _memoryUsage;

        [SuppressMessage("CodeQuality", "S4487", Justification = "Observable instruments must be held by reference to prevent garbage collection")]
        private readonly ObservableGauge<long> _gcCollections;

        [SuppressMessage("CodeQuality", "S4487", Justification = "Observable instruments must be held by reference to prevent garbage collection")]
        private readonly ObservableGauge<int> _threadPoolAvailable;

        private volatile bool _disposed;

        /// <summary>
        /// Gets the ActivitySource for creating spans and traces.
        /// </summary>
        public ActivitySource ActivitySource => _activitySource;

        /// <summary>
        /// Gets the Meter for creating metrics instruments.
        /// </summary>
        public Meter Meter => _meter;

        /// <summary>
        /// Gets the service name used for OpenTelemetry resource identification.
        /// </summary>
        public string ServiceName { get; }

        /// <summary>
        /// Initializes a new instance of the WorkflowForgeOpenTelemetryService.
        /// </summary>
        /// <param name="serviceName">The service name for OpenTelemetry identification.</param>
        /// <param name="serviceVersion">The service version.</param>
        /// <param name="logger">The logger instance.</param>
        public WorkflowForgeOpenTelemetryService(string serviceName, string serviceVersion = "1.0.0", IWorkflowForgeLogger? logger = null)
        {
            ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
            _logger = logger ?? NullLogger.Instance;

            // Create ActivitySource for distributed tracing
            _activitySource = new ActivitySource(serviceName, serviceVersion);

            // Create Meter for metrics
            _meter = new Meter(serviceName, serviceVersion);

            // Initialize standard operation metrics following OpenTelemetry semantic conventions
            _operationsTotal = _meter.CreateCounter<long>(
                "workflowforge.operations.total",
                "operation",
                "Total number of operations executed");

            _operationErrorsTotal = _meter.CreateCounter<long>(
                "workflowforge.operations.errors.total",
                "error",
                "Total number of operation errors");

            _operationDuration = _meter.CreateHistogram<double>(
                "workflowforge.operations.duration",
                "s",
                "Duration of operations in seconds");

            _operationMemoryAllocations = _meter.CreateHistogram<long>(
                "workflowforge.operations.memory.allocations",
                "By",
                "Memory allocated during operation execution");

            _activeOperations = _meter.CreateUpDownCounter<int>(
                "workflowforge.operations.active",
                "operation",
                "Number of currently active operations");

            // System metrics
            _memoryUsage = _meter.CreateObservableGauge<long>(
                "workflowforge.process.memory.usage",
                GetMemoryUsage,
                "By",
                "Process memory usage in bytes");

            _gcCollections = _meter.CreateObservableGauge<long>(
                "workflowforge.process.gc.collections.total",
                GetTotalGcCollections,
                "collection",
                "Total garbage collections");

            _threadPoolAvailable = _meter.CreateObservableGauge<int>(
                "workflowforge.process.threadpool.threads.available",
                GetAvailableThreadPoolThreads,
                "thread",
                "Available thread pool threads");

            _logger.LogInformation("OpenTelemetry service initialized for {ServiceName}", serviceName);
        }

        /// <summary>
        /// Starts a new activity (span) for distributed tracing.
        /// </summary>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="kind">The activity kind.</param>
        /// <returns>The started activity or null if not sampled.</returns>
        public Activity? StartActivity(string operationName, ActivityKind kind = ActivityKind.Internal)
        {
            return _activitySource.StartActivity(operationName, kind);
        }

        /// <summary>
        /// Records metrics for an operation execution.
        /// </summary>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="duration">The operation duration.</param>
        /// <param name="success">Whether the operation was successful.</param>
        /// <param name="memoryAllocated">Memory allocated during the operation.</param>
        /// <param name="tags">Additional tags for the metrics.</param>
        public void RecordOperation(
            string operationName,
            TimeSpan duration,
            bool success,
            long memoryAllocated = 0,
            KeyValuePair<string, object?>[]? tags = null)
        {
            if (_disposed)
                return;

            var metricTags = CreateTagsArray(operationName, success, tags);

            // Record operation metrics
            _operationsTotal.Add(1, metricTags);
            _operationDuration.Record(duration.TotalSeconds, metricTags);

            if (memoryAllocated > 0)
            {
                _operationMemoryAllocations.Record(memoryAllocated, metricTags);
            }

            if (!success)
            {
                _operationErrorsTotal.Add(1, metricTags);
            }
        }

        /// <summary>
        /// Increments the active operations counter.
        /// </summary>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="tags">Additional tags for the metric.</param>
        public void IncrementActiveOperations(string operationName, KeyValuePair<string, object?>[]? tags = null)
        {
            if (_disposed)
                return;

            var metricTags = CreateTagsArray(operationName, null, tags);
            _activeOperations.Add(1, metricTags);
        }

        /// <summary>
        /// Decrements the active operations counter.
        /// </summary>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="tags">Additional tags for the metric.</param>
        public void DecrementActiveOperations(string operationName, KeyValuePair<string, object?>[]? tags = null)
        {
            if (_disposed)
                return;

            var metricTags = CreateTagsArray(operationName, null, tags);
            _activeOperations.Add(-1, metricTags);
        }

        /// <summary>
        /// Creates a custom counter metric.
        /// </summary>
        /// <typeparam name="T">The counter value type.</typeparam>
        /// <param name="name">The counter name.</param>
        /// <param name="unit">The unit of measurement.</param>
        /// <param name="description">The counter description.</param>
        /// <returns>The counter instrument.</returns>
        public Counter<T> CreateCounter<T>(string name, string? unit = null, string? description = null)
            where T : struct
        {
            return _meter.CreateCounter<T>(name, unit, description);
        }

        /// <summary>
        /// Creates a custom histogram metric.
        /// </summary>
        /// <typeparam name="T">The histogram value type.</typeparam>
        /// <param name="name">The histogram name.</param>
        /// <param name="unit">The unit of measurement.</param>
        /// <param name="description">The histogram description.</param>
        /// <returns>The histogram instrument.</returns>
        public Histogram<T> CreateHistogram<T>(string name, string? unit = null, string? description = null)
            where T : struct
        {
            return _meter.CreateHistogram<T>(name, unit, description);
        }

        /// <summary>
        /// Creates a custom up-down counter metric.
        /// </summary>
        /// <typeparam name="T">The counter value type.</typeparam>
        /// <param name="name">The counter name.</param>
        /// <param name="unit">The unit of measurement.</param>
        /// <param name="description">The counter description.</param>
        /// <returns>The up-down counter instrument.</returns>
        public UpDownCounter<T> CreateUpDownCounter<T>(string name, string? unit = null, string? description = null)
            where T : struct
        {
            return _meter.CreateUpDownCounter<T>(name, unit, description);
        }

        /// <summary>
        /// Creates a custom observable gauge metric.
        /// </summary>
        /// <typeparam name="T">The gauge value type.</typeparam>
        /// <param name="name">The gauge name.</param>
        /// <param name="observeValue">The function to observe the value.</param>
        /// <param name="unit">The unit of measurement.</param>
        /// <param name="description">The gauge description.</param>
        /// <returns>The observable gauge instrument.</returns>
        public ObservableGauge<T> CreateObservableGauge<T>(
            string name,
            Func<T> observeValue,
            string? unit = null,
            string? description = null)
            where T : struct
        {
            return _meter.CreateObservableGauge<T>(name, observeValue, unit, description);
        }

        private KeyValuePair<string, object?>[] CreateTagsArray(
            string operationName,
            bool? success,
            KeyValuePair<string, object?>[]? additionalTags)
        {
            var tagsList = new List<KeyValuePair<string, object?>>
            {
                new("operation.name", operationName),
                new("service.name", ServiceName)
            };

            if (success.HasValue)
            {
                tagsList.Add(new("operation.success", success.Value));
            }

            if (additionalTags != null)
            {
                tagsList.AddRange(additionalTags);
            }

            return tagsList.ToArray();
        }

        private static long GetMemoryUsage()
        {
            try
            {
                return Process.GetCurrentProcess().WorkingSet64;
            }
            catch
            {
                // Platform API may throw on restricted environments; return zero as safe default
                return 0;
            }
        }

        private static long GetTotalGcCollections()
        {
            try
            {
                return GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);
            }
            catch
            {
                // Platform API may throw on restricted environments; return zero as safe default
                return 0;
            }
        }

        private static int GetAvailableThreadPoolThreads()
        {
            try
            {
                ThreadPool.GetAvailableThreads(out var workerThreads, out var completionPortThreads);
                return workerThreads + completionPortThreads;
            }
            catch
            {
                // Platform API may throw on restricted environments; return zero as safe default
                return 0;
            }
        }

        /// <summary>
        /// Disposes the OpenTelemetry service and releases resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _activitySource?.Dispose();
            _meter?.Dispose();
            GC.SuppressFinalize(this);

            _logger.LogInformation("OpenTelemetry service disposed for {ServiceName}", ServiceName);
        }
    }
}
