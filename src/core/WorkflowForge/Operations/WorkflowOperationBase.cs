using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Operations
{
    /// <summary>
    /// Base class for untyped workflow operations providing default behaviors (ID, restore guard, disposal).
    /// </summary>
    public abstract class WorkflowOperationBase : IWorkflowOperation
    {
        /// <inheritdoc />
        public virtual Guid Id { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public abstract string Name { get; }

        /// <inheritdoc />
        public virtual bool SupportsRestore => false;

        /// <inheritdoc />
        public abstract Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default);

        /// <inheritdoc />
        public virtual Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (!SupportsRestore)
                throw new NotSupportedException($"Operation '{Name}' does not support restoration.");

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public virtual void Dispose()
        {
            // Override in derived classes if needed
        }
    }

    /// <summary>
    /// Base class for typed workflow operations providing automatic type conversion for the untyped interface.
    /// </summary>
    public abstract class WorkflowOperationBase<TInput, TOutput> : WorkflowOperationBase, IWorkflowOperation<TInput, TOutput>
    {
        /// <summary>
        /// Strongly-typed implementation of the operation logic.
        /// This is the main method you should override in your custom operations.
        /// </summary>
        /// <param name="input">The typed input data.</param>
        /// <param name="foundry">The workflow foundry providing context and services.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The typed output data.</returns>
        public abstract Task<TOutput> ForgeAsync(TInput input, IWorkflowFoundry foundry, CancellationToken cancellationToken = default);

        /// <summary>
        /// Strongly-typed restoration logic.
        /// Override this method if your operation supports restoration/rollback.
        /// </summary>
        /// <param name="output">The typed output data to restore.</param>
        /// <param name="foundry">The workflow foundry providing context and services.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the restoration operation.</returns>
        public virtual Task RestoreAsync(TOutput output, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (!SupportsRestore)
                throw new NotSupportedException($"Operation '{Name}' does not support restoration.");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Implements the untyped interface by casting to/from the typed interface.
        /// DO NOT OVERRIDE THIS METHOD - it handles type conversion automatically.
        /// </summary>
        public override sealed async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            // Handle type conversion and validation
            TInput typedInput;

            if (typeof(TInput) == typeof(object) || inputData is TInput directCast)
            {
                typedInput = (TInput)inputData!;
            }
            else if (inputData == null && !typeof(TInput).IsValueType)
            {
                typedInput = default(TInput)!;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Operation '{Name}' expects input of type {typeof(TInput).Name} but received {inputData?.GetType().Name ?? "null"}.");
            }

            var result = await ForgeAsync(typedInput, foundry, cancellationToken).ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Implements the untyped restoration interface.
        /// DO NOT OVERRIDE THIS METHOD - it handles type conversion automatically.
        /// </summary>
        public override sealed async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (!SupportsRestore)
                throw new NotSupportedException($"Operation '{Name}' does not support restoration.");

            // Handle type conversion for output data
            TOutput typedOutput;

            if (typeof(TOutput) == typeof(object) || outputData is TOutput directCast)
            {
                typedOutput = (TOutput)outputData!;
            }
            else if (outputData == null && !typeof(TOutput).IsValueType)
            {
                typedOutput = default(TOutput)!;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Operation '{Name}' expects output of type {typeof(TOutput).Name} for restoration but received {outputData?.GetType().Name ?? "null"}.");
            }

            await RestoreAsync(typedOutput, foundry, cancellationToken).ConfigureAwait(false);
        }
    }
}