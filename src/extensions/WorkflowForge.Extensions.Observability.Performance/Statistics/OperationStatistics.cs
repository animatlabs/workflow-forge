using System;
using WorkflowForge.Extensions.Observability.Performance.Abstractions;

namespace WorkflowForge.Extensions.Observability.Performance
{
    /// <summary>
    /// Thread-safe accumulator of performance statistics for a single named operation.
    /// Populated by <see cref="PerformanceStatisticsMiddleware"/>.
    /// </summary>
    public sealed class OperationStatistics : IOperationStatistics
    {
        private readonly object _lock = new object();

        private int _executionCount;
        private int _successfulExecutions;
        private int _failedExecutions;
        private long _totalTicks;
        private long _minTicks = long.MaxValue;
        private long _maxTicks;
        private long _totalMemory;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationStatistics"/> class.
        /// </summary>
        /// <param name="operationName">The operation name.</param>
        /// <param name="operationId">The operation identifier.</param>
        public OperationStatistics(string operationName, string operationId)
        {
            OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
            OperationId = operationId ?? throw new ArgumentNullException(nameof(operationId));
        }

        /// <inheritdoc />
        public string OperationName { get; }

        /// <inheritdoc />
        public string OperationId { get; }

        /// <summary>
        /// Records a single execution of this operation.
        /// </summary>
        /// <param name="durationTicks">Execution duration in ticks (clamped to non-negative).</param>
        /// <param name="success">Whether the execution succeeded.</param>
        /// <param name="memoryAllocated">Approximate bytes allocated during the execution.</param>
        internal void Record(long durationTicks, bool success, long memoryAllocated)
        {
            if (durationTicks < 0) durationTicks = 0;
            if (memoryAllocated < 0) memoryAllocated = 0;

            lock (_lock)
            {
                _executionCount++;
                if (success) _successfulExecutions++; else _failedExecutions++;
                _totalTicks += durationTicks;
                if (durationTicks < _minTicks) _minTicks = durationTicks;
                if (durationTicks > _maxTicks) _maxTicks = durationTicks;
                _totalMemory += memoryAllocated;
            }
        }

        /// <inheritdoc />
        public int ExecutionCount { get { lock (_lock) { return _executionCount; } } }

        /// <inheritdoc />
        public int SuccessfulExecutions { get { lock (_lock) { return _successfulExecutions; } } }

        /// <inheritdoc />
        public int FailedExecutions { get { lock (_lock) { return _failedExecutions; } } }

        /// <inheritdoc />
        public double SuccessRate
        {
            get { lock (_lock) { return _executionCount == 0 ? 0.0 : (double)_successfulExecutions / _executionCount; } }
        }

        /// <inheritdoc />
        public TimeSpan AverageExecutionTime
        {
            get { lock (_lock) { return _executionCount == 0 ? TimeSpan.Zero : TimeSpan.FromTicks(_totalTicks / _executionCount); } }
        }

        /// <inheritdoc />
        public TimeSpan MinimumExecutionTime
        {
            get { lock (_lock) { return _executionCount == 0 ? TimeSpan.Zero : TimeSpan.FromTicks(_minTicks); } }
        }

        /// <inheritdoc />
        public TimeSpan MaximumExecutionTime
        {
            get { lock (_lock) { return TimeSpan.FromTicks(_maxTicks); } }
        }

        /// <inheritdoc />
        public long TotalMemoryAllocated { get { lock (_lock) { return _totalMemory; } } }

        /// <inheritdoc />
        public long AverageMemoryPerExecution
        {
            get { lock (_lock) { return _executionCount == 0 ? 0 : _totalMemory / _executionCount; } }
        }
    }
}
