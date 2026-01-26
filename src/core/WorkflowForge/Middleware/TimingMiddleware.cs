using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Options.Middleware;

namespace WorkflowForge.Middleware
{
    /// <summary>
    /// Basic timing middleware that stores execution timing in foundry properties.
    /// For advanced performance metrics and logging, use WorkflowForge.Extensions.Observability.Performance.
    /// Can be configured via <see cref="TimingMiddlewareOptions"/> to enable/disable timing collection.
    /// </summary>
    internal sealed class TimingMiddleware : IWorkflowOperationMiddleware
    {
        private readonly TimingMiddlewareOptions _options;
        private readonly ISystemTimeProvider _timeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimingMiddleware"/> class.
        /// </summary>
        /// <param name="options">The timing middleware options.</param>
        /// <param name="timeProvider">The time provider to use for timestamps.</param>
        public TimingMiddleware(
            TimingMiddlewareOptions options,
            ISystemTimeProvider? timeProvider = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _timeProvider = timeProvider ?? SystemTimeProvider.Instance;
        }

        /// <summary>
        /// Initializes a new instance with default options (for backward compatibility).
        /// </summary>
        /// <param name="timeProvider">The time provider to use for timestamps.</param>
        public TimingMiddleware(ISystemTimeProvider? timeProvider = null)
            : this(new TimingMiddlewareOptions(), timeProvider)
        {
        }

        /// <inheritdoc />
        public async Task<object?> ExecuteAsync(
            IWorkflowOperation operation,
            IWorkflowFoundry foundry,
            object? inputData,
            Func<CancellationToken, Task<object?>> next,
            CancellationToken cancellationToken = default)
        {
            // Note: Enabled check is done at registration time (UseDefaultMiddleware)
            // If this middleware is registered, it's enabled - no need for runtime check
            // Note: Operation name is already in logging context via ExecutionName property
            var stopwatch = Stopwatch.StartNew();
            var startTime = _timeProvider.UtcNow;

            try
            {
                var result = await next(cancellationToken).ConfigureAwait(false);

                stopwatch.Stop();
                var elapsedMs = stopwatch.ElapsedMilliseconds;

                // Store timing data based on configuration
                // Use static property names - operation name is in logging context
                if (_options.IncludeDetailedTimings)
                {
                    foundry.Properties["Timing.StartTime"] = startTime;
                    foundry.Properties["Timing.EndTime"] = _timeProvider.UtcNow;
                    foundry.Properties["Timing.Duration"] = elapsedMs;
                    foundry.Properties["Timing.DurationTicks"] = stopwatch.ElapsedTicks;
                }
                else
                {
                    // Basic timing - just duration
                    foundry.Properties["Timing.Duration"] = elapsedMs;
                }

                return result;
            }
            catch (Exception)
            {
                stopwatch.Stop();
                var elapsedMs = stopwatch.ElapsedMilliseconds;

                // Store timing even on failure (helps identify slow failing operations)
                foundry.Properties["Timing.Duration"] = elapsedMs;
                if (_options.IncludeDetailedTimings)
                {
                    foundry.Properties["Timing.StartTime"] = startTime;
                    foundry.Properties["Timing.Failed"] = true;
                }

                throw;
            }
        }
    }
}