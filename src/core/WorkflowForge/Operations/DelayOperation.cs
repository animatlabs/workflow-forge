using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Operations
{
    /// <summary>
    /// Simple delay operation for workflow pacing and testing.
    /// Useful for introducing controlled delays in workflow execution.
    /// </summary>
    public sealed class DelayOperation : WorkflowOperationBase
    {
        private readonly TimeSpan _delay;

        /// <inheritdoc />
        public override string Name { get; }

        /// <summary>
        /// Initializes a new delay operation.
        /// </summary>
        /// <param name="delay">The delay duration.</param>
        /// <param name="name">Optional name for the operation.</param>
        public DelayOperation(TimeSpan delay, string? name = null)
        {
            if (delay < TimeSpan.Zero)
                throw new ArgumentException("Delay duration cannot be negative.", nameof(delay));

            _delay = delay;
            Name = name ?? $"Delay {delay.TotalMilliseconds}ms";
        }

        /// <inheritdoc />
        protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (foundry == null)
                throw new ArgumentNullException(nameof(foundry));

            // Create logging properties with comprehensive operation information
            var loggingProperties = new Dictionary<string, string>
            {
                ["DelayMs"] = _delay.TotalMilliseconds.ToString(),
                ["InputType"] = inputData?.GetType().Name ?? "null",
                ["OperationId"] = Id.ToString(),
                ["OperationName"] = Name,
                ["WorkflowId"] = foundry.ExecutionId.ToString(),
                ["WorkflowName"] = foundry.CurrentWorkflow?.Name ?? "Unknown"
            };

            foundry.Logger.LogDebug(loggingProperties, "Starting delay operation");

            await Task.Delay(_delay, cancellationToken).ConfigureAwait(false);

            foundry.Logger.LogDebug(loggingProperties, "Completed delay operation");

            return inputData; // Pass through input data unchanged
        }

        // Uses base RestoreAsync behavior which throws when SupportsRestore is false

        /// <summary>
        /// Creates a delay operation with the specified duration.
        /// </summary>
        /// <param name="milliseconds">The delay duration in milliseconds.</param>
        /// <returns>A delay operation.</returns>
        public static DelayOperation FromMilliseconds(int milliseconds) => new(TimeSpan.FromMilliseconds(milliseconds));

        /// <summary>
        /// Creates a delay operation with the specified duration.
        /// </summary>
        /// <param name="seconds">The delay duration in seconds.</param>
        /// <returns>A delay operation.</returns>
        public static DelayOperation FromSeconds(int seconds) => new(TimeSpan.FromSeconds(seconds));

        /// <summary>
        /// Creates a delay operation with the specified duration.
        /// </summary>
        /// <param name="minutes">The delay duration in minutes.</param>
        /// <returns>A delay operation.</returns>
        public static DelayOperation FromMinutes(int minutes) => new(TimeSpan.FromMinutes(minutes));
    }
}