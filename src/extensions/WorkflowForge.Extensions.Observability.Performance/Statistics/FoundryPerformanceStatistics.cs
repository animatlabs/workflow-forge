using System;
using System.Collections.Generic;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Observability.Performance.Abstractions;

namespace WorkflowForge.Extensions.Observability.Performance
{
    /// <summary>
    /// Thread-safe aggregate of foundry performance statistics, collected across all operations
    /// executed while performance monitoring is enabled. Populated by
    /// <see cref="PerformanceStatisticsMiddleware"/> and retrieved via
    /// <see cref="WorkflowFoundryPerformanceExtensions.GetPerformanceStatistics"/>.
    /// </summary>
    public sealed class FoundryPerformanceStatistics : IFoundryPerformanceStatistics
    {
        private readonly object _lock = new object();
        private readonly ISystemTimeProvider _timeProvider;
        private readonly Dictionary<string, OperationStatistics> _operations =
            new Dictionary<string, OperationStatistics>(StringComparer.Ordinal);

        private int _totalOperations;
        private int _successfulOperations;
        private int _failedOperations;
        private long _totalTicks;
        private long _minTicks = long.MaxValue;
        private long _maxTicks;
        private long _totalMemory;
        private readonly DateTimeOffset _startTime;
        private DateTimeOffset _endTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="FoundryPerformanceStatistics"/> class.
        /// </summary>
        /// <param name="timeProvider">Optional time provider (defaults to the system clock).</param>
        public FoundryPerformanceStatistics(ISystemTimeProvider? timeProvider = null)
        {
            _timeProvider = timeProvider ?? SystemTimeProvider.Instance;
            _startTime = _timeProvider.UtcNow;
            _endTime = _startTime;
        }

        /// <summary>
        /// Records a single operation execution into the aggregate and per-operation statistics.
        /// </summary>
        /// <param name="operationName">The operation name.</param>
        /// <param name="operationId">The operation identifier.</param>
        /// <param name="duration">Execution duration.</param>
        /// <param name="success">Whether the execution succeeded.</param>
        /// <param name="memoryAllocated">Approximate bytes allocated during the execution.</param>
        public void Record(string operationName, string operationId, TimeSpan duration, bool success, long memoryAllocated)
        {
            if (operationName == null) throw new ArgumentNullException(nameof(operationName));
            if (operationId == null) throw new ArgumentNullException(nameof(operationId));

            var ticks = duration.Ticks;
            if (ticks < 0) ticks = 0;
            if (memoryAllocated < 0) memoryAllocated = 0;

            lock (_lock)
            {
                _totalOperations++;
                if (success) _successfulOperations++; else _failedOperations++;
                _totalTicks += ticks;
                if (ticks < _minTicks) _minTicks = ticks;
                if (ticks > _maxTicks) _maxTicks = ticks;
                _totalMemory += memoryAllocated;
                _endTime = _timeProvider.UtcNow;

                if (!_operations.TryGetValue(operationName, out var opStats))
                {
                    opStats = new OperationStatistics(operationName, operationId);
                    _operations[operationName] = opStats;
                }

                opStats.Record(ticks, success, memoryAllocated);
            }
        }

        /// <inheritdoc />
        public int TotalOperations { get { lock (_lock) { return _totalOperations; } } }

        /// <inheritdoc />
        public int SuccessfulOperations { get { lock (_lock) { return _successfulOperations; } } }

        /// <inheritdoc />
        public int FailedOperations { get { lock (_lock) { return _failedOperations; } } }

        /// <inheritdoc />
        public double SuccessRate
        {
            get { lock (_lock) { return _totalOperations == 0 ? 0.0 : (double)_successfulOperations / _totalOperations; } }
        }

        /// <inheritdoc />
        public TimeSpan AverageDuration
        {
            get { lock (_lock) { return _totalOperations == 0 ? TimeSpan.Zero : TimeSpan.FromTicks(_totalTicks / _totalOperations); } }
        }

        /// <inheritdoc />
        public TimeSpan MinimumDuration
        {
            get { lock (_lock) { return _totalOperations == 0 ? TimeSpan.Zero : TimeSpan.FromTicks(_minTicks); } }
        }

        /// <inheritdoc />
        public TimeSpan MaximumDuration
        {
            get { lock (_lock) { return TimeSpan.FromTicks(_maxTicks); } }
        }

        /// <inheritdoc />
        public long TotalMemoryAllocated { get { lock (_lock) { return _totalMemory; } } }

        /// <inheritdoc />
        public long AverageMemoryPerOperation
        {
            get { lock (_lock) { return _totalOperations == 0 ? 0 : _totalMemory / _totalOperations; } }
        }

        /// <inheritdoc />
        public DateTimeOffset StartTime => _startTime;

        /// <inheritdoc />
        public DateTimeOffset EndTime { get { lock (_lock) { return _endTime; } } }

        /// <inheritdoc />
        public TimeSpan TotalDuration { get { lock (_lock) { return _endTime - _startTime; } } }

        /// <inheritdoc />
        public double OperationsPerSecond
        {
            get
            {
                lock (_lock)
                {
                    var seconds = (_endTime - _startTime).TotalSeconds;
                    return seconds <= 0 ? 0.0 : _totalOperations / seconds;
                }
            }
        }

        /// <inheritdoc />
        public IOperationStatistics? GetOperationStatistics(string operationName)
        {
            if (operationName == null) throw new ArgumentNullException(nameof(operationName));
            lock (_lock)
            {
                return _operations.TryGetValue(operationName, out var stats) ? stats : null;
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<IOperationStatistics> GetAllOperationStatistics()
        {
            lock (_lock)
            {
                return new List<IOperationStatistics>(_operations.Values);
            }
        }
    }
}
