using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Loggers;

namespace WorkflowForge.Middleware
{
    /// <summary>
    /// Basic timing middleware that stores execution timing in foundry properties.
    /// For advanced performance metrics and logging, use WorkflowForge.Extensions.Observability.Performance.
    /// </summary>
    public sealed class TimingMiddleware : IWorkflowOperationMiddleware
    {
        private readonly ISystemTimeProvider _timeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimingMiddleware"/> class.
        /// </summary>
        /// <param name="timeProvider">The time provider to use for timestamps.</param>
        public TimingMiddleware(ISystemTimeProvider? timeProvider = null)
        {
            _timeProvider = timeProvider ?? SystemTimeProvider.Instance;
        }

        /// <inheritdoc />
        public async Task<object?> ExecuteAsync(
            IWorkflowOperation operation,
            IWorkflowFoundry foundry,
            object? inputData,
            Func<Task<object?>> next,
            CancellationToken cancellationToken = default)
        {
            var operationName = operation?.Name ?? "Unknown";
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var result = await next().ConfigureAwait(false);
                
                stopwatch.Stop();
                var elapsedMs = stopwatch.ElapsedMilliseconds;
                
                // Store basic timing data in foundry properties for other components
                foundry.Properties[$"Timing.{operationName}.StartTime"] = _timeProvider.UtcNow.AddMilliseconds(-elapsedMs);
                foundry.Properties[$"Timing.{operationName}.Duration"] = elapsedMs;

                return result;
            }
            catch (Exception)
            {
                stopwatch.Stop();
                var elapsedMs = stopwatch.ElapsedMilliseconds;

                // Store timing even on failure
                foundry.Properties[$"Timing.{operationName}.Duration"] = elapsedMs;

                throw;
            }
        }
    }
} 
