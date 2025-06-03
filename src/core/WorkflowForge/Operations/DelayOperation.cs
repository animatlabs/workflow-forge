using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Loggers;

namespace WorkflowForge.Operations
{
    /// <summary>
    /// Simple delay operation for workflow pacing and testing.
    /// Useful for introducing controlled delays in workflow execution.
    /// </summary>
    public class DelayOperation : IWorkflowOperation
    {
        private readonly TimeSpan _delay;

        /// <inheritdoc />
        public Guid Id { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public bool SupportsRestore => false;

        /// <summary>
        /// Initializes a new delay operation.
        /// </summary>
        /// <param name="delay">The delay duration.</param>
        /// <param name="name">Optional name for the operation.</param>
        public DelayOperation(TimeSpan delay, string? name = null)
        {
            _delay = delay;
            Name = name ?? $"Delay {delay.TotalMilliseconds}ms";
        }

        /// <inheritdoc />
        public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            foundry.Logger.LogDebug(WorkflowLogMessages.DelayOperationStarted);

            await Task.Delay(_delay, cancellationToken).ConfigureAwait(false);
            
            foundry.Logger.LogDebug(WorkflowLogMessages.DelayOperationCompleted);
            
            return inputData; // Pass through input data unchanged
        }

        /// <inheritdoc />
        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Delay operations do not support compensation.");
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // Nothing to dispose
        }

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