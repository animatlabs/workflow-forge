using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Operations
{
    /// <summary>
    /// Base class for untyped workflow operations providing default behaviors (ID, restore guard, disposal).
    /// Includes lifecycle hooks for before/after execution without requiring middleware.
    /// </summary>
    public abstract class WorkflowOperationBase : IWorkflowOperation
    {
        /// <inheritdoc />
        public virtual Guid Id { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public abstract string Name { get; }

        /// <summary>
        /// Called before the operation executes. Override to add setup/initialization logic.
        /// </summary>
        /// <param name="inputData">The input data for the operation.</param>
        /// <param name="foundry">The workflow foundry providing context and services.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected virtual Task OnBeforeExecuteAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
            => Task.CompletedTask;

        /// <summary>
        /// Called after the operation executes successfully. Override to add cleanup/finalization logic.
        /// </summary>
        /// <param name="inputData">The input data that was passed to the operation.</param>
        /// <param name="outputData">The output data produced by the operation.</param>
        /// <param name="foundry">The workflow foundry providing context and services.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected virtual Task OnAfterExecuteAsync(object? inputData, object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
            => Task.CompletedTask;

        /// <summary>
        /// Core operation logic. Implement this method with your operation's business logic.
        /// This is called between OnBeforeExecuteAsync and OnAfterExecuteAsync.
        /// </summary>
        /// <param name="inputData">The input data for the operation.</param>
        /// <param name="foundry">The workflow foundry providing context and services.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The output data from the operation.</returns>
        protected abstract Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken);

        /// <inheritdoc />
        /// <remarks>
        /// This method orchestrates the lifecycle hooks and core execution:
        /// 1. Calls OnBeforeExecuteAsync
        /// 2. Calls ForgeAsyncCore (your operation logic)
        /// 3. Calls OnAfterExecuteAsync
        /// </remarks>
        public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            await OnBeforeExecuteAsync(inputData, foundry, cancellationToken).ConfigureAwait(false);
            var result = await ForgeAsyncCore(inputData, foundry, cancellationToken).ConfigureAwait(false);
            await OnAfterExecuteAsync(inputData, result, foundry, cancellationToken).ConfigureAwait(false);
            return result;
        }

        /// <inheritdoc />
        /// <remarks>
        /// The base implementation is a no-op. Override this method in your operation
        /// to provide compensation/rollback logic. Operations that don't override this
        /// are safely skipped during compensation.
        /// </remarks>
        public virtual Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
            // Override in derived classes if needed
        }
    }

    /// <summary>
    /// Base class for typed workflow operations providing automatic type conversion for the untyped interface.
    /// Includes lifecycle hooks for before/after execution without requiring middleware.
    /// </summary>
    /// <typeparam name="TInput">The type of input data this operation expects.</typeparam>
    /// <typeparam name="TOutput">The type of output data this operation produces.</typeparam>
    public abstract class WorkflowOperationBase<TInput, TOutput> : WorkflowOperationBase, IWorkflowOperation<TInput, TOutput>
    {
        /// <summary>
        /// Called before the operation executes with typed input. Override to add setup/initialization logic.
        /// </summary>
        /// <param name="input">The typed input data for the operation.</param>
        /// <param name="foundry">The workflow foundry providing context and services.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected virtual Task OnBeforeExecuteAsync(TInput input, IWorkflowFoundry foundry, CancellationToken cancellationToken)
            => Task.CompletedTask;

        /// <summary>
        /// Called after the operation executes successfully with typed data. Override to add cleanup/finalization logic.
        /// </summary>
        /// <param name="input">The typed input data that was passed to the operation.</param>
        /// <param name="output">The typed output data produced by the operation.</param>
        /// <param name="foundry">The workflow foundry providing context and services.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected virtual Task OnAfterExecuteAsync(TInput input, TOutput output, IWorkflowFoundry foundry, CancellationToken cancellationToken)
            => Task.CompletedTask;

        /// <summary>
        /// Strongly-typed core operation logic.
        /// This is the main method you should override in your custom operations.
        /// </summary>
        /// <param name="input">The typed input data.</param>
        /// <param name="foundry">The workflow foundry providing context and services.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The typed output data.</returns>
        protected abstract Task<TOutput> ForgeAsyncCore(TInput input, IWorkflowFoundry foundry, CancellationToken cancellationToken);

        /// <summary>
        /// Executes the typed operation with lifecycle hooks.
        /// </summary>
        /// <param name="input">The typed input data.</param>
        /// <param name="foundry">The workflow foundry providing context and services.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The typed output data.</returns>
        public async Task<TOutput> ForgeAsync(TInput input, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            await OnBeforeExecuteAsync(input, foundry, cancellationToken).ConfigureAwait(false);
            var result = await ForgeAsyncCore(input, foundry, cancellationToken).ConfigureAwait(false);
            await OnAfterExecuteAsync(input, result, foundry, cancellationToken).ConfigureAwait(false);
            return result;
        }

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
            return Task.CompletedTask;
        }

        /// <summary>
        /// Implements the untyped interface by casting to/from the typed interface.
        /// DO NOT OVERRIDE THIS METHOD - it handles type conversion automatically.
        /// </summary>
        protected override sealed async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
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

            // Call the typed ForgeAsync which includes typed hooks
            var result = await ForgeAsync(typedInput, foundry, cancellationToken).ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Overrides base hooks to prevent double-calling when typed hooks are used.
        /// </summary>
        protected override Task OnBeforeExecuteAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
            => Task.CompletedTask; // Typed hooks are called in typed ForgeAsync

        /// <summary>
        /// Overrides base hooks to prevent double-calling when typed hooks are used.
        /// </summary>
        protected override Task OnAfterExecuteAsync(object? inputData, object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
            => Task.CompletedTask; // Typed hooks are called in typed ForgeAsync

        /// <summary>
        /// Implements the untyped restoration interface.
        /// DO NOT OVERRIDE THIS METHOD - it handles type conversion automatically.
        /// </summary>
        public override sealed async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
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