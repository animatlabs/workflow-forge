using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Exceptions;

namespace WorkflowForge.Operations
{
    /// <summary>
    /// A workflow operation that executes a simple action without returning a result.
    /// Useful for side-effects like logging, notifications, or state changes.
    /// </summary>
    public sealed class ActionWorkflowOperation : WorkflowOperationBase
    {
        private readonly Func<object?, IWorkflowFoundry, CancellationToken, Task> _actionFunc;
        private readonly Func<object?, IWorkflowFoundry, CancellationToken, Task>? _restoreFunc;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionWorkflowOperation"/> class.
        /// </summary>
        /// <param name="name">The name of the action operation.</param>
        /// <param name="actionFunc">The action function to execute.</param>
        /// <param name="restoreFunc">Optional restoration function for compensation.</param>
        public ActionWorkflowOperation(
            string name,
            Func<object?, IWorkflowFoundry, CancellationToken, Task> actionFunc,
            Func<object?, IWorkflowFoundry, CancellationToken, Task>? restoreFunc = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _actionFunc = actionFunc ?? throw new ArgumentNullException(nameof(actionFunc));
            _restoreFunc = restoreFunc;
        }

        /// <summary>
        /// Gets the name of this action operation.
        /// </summary>
        public override string Name { get; }

        /// <summary>
        /// Executes the action and returns the input data unchanged.
        /// </summary>
        /// <param name="input">The input data to pass through.</param>
        /// <param name="foundry">The workflow foundry providing context and services.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The input data unchanged.</returns>
        protected override async Task<object?> ForgeAsyncCore(object? input, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            try
            {
                await _actionFunc(input, foundry, cancellationToken).ConfigureAwait(false);
                return input; // Pass through the input unchanged
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                throw new WorkflowOperationException(
                    $"Action operation '{Name}' failed during execution.",
                    ex,
                    foundry.ExecutionId,
                    foundry.CurrentWorkflow?.Id,
                    Name,
                    Id);
            }
        }

        /// <summary>
        /// Executes the restoration action if supported.
        /// </summary>
        /// <param name="output">The output data for restoration.</param>
        /// <param name="foundry">The workflow foundry providing context and services.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the restoration operation.</returns>
        public override async Task RestoreAsync(object? output, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (_restoreFunc == null)
                return;

            try
            {
                await _restoreFunc(output, foundry, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                throw new WorkflowRestoreException(
                    $"Action operation '{Name}' failed during restoration.",
                    ex,
                    foundry.ExecutionId,
                    foundry.CurrentWorkflow?.Id,
                    Name);
            }
        }
    }

    /// <summary>
    /// A strongly-typed action workflow operation that executes without returning a result.
    /// </summary>
    /// <typeparam name="TInput">The input data type.</typeparam>
    public sealed class ActionWorkflowOperation<TInput> : WorkflowOperationBase<TInput, TInput>
    {
        private readonly Func<TInput, IWorkflowFoundry, CancellationToken, Task> _actionFunc;
        private readonly Func<TInput, IWorkflowFoundry, CancellationToken, Task>? _restoreFunc;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionWorkflowOperation{TInput}"/> class.
        /// </summary>
        /// <param name="name">The name of the action operation.</param>
        /// <param name="actionFunc">The typed action function to execute.</param>
        /// <param name="restoreFunc">Optional typed restoration function for compensation.</param>
        public ActionWorkflowOperation(
            string name,
            Func<TInput, IWorkflowFoundry, CancellationToken, Task> actionFunc,
            Func<TInput, IWorkflowFoundry, CancellationToken, Task>? restoreFunc = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _actionFunc = actionFunc ?? throw new ArgumentNullException(nameof(actionFunc));
            _restoreFunc = restoreFunc;
        }

        /// <summary>
        /// Gets the name of this action operation.
        /// </summary>
        public override string Name { get; }

        /// <summary>
        /// Executes the typed action and returns the input data unchanged.
        /// </summary>
        /// <param name="input">The typed input data to pass through.</param>
        /// <param name="foundry">The workflow foundry providing context and services.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The input data unchanged.</returns>
        protected override async Task<TInput> ForgeAsyncCore(TInput input, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            try
            {
                await _actionFunc(input, foundry, cancellationToken).ConfigureAwait(false);
                return input; // Pass through the input unchanged
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                throw new WorkflowOperationException($"Action operation '{Name}' failed during execution.", ex);
            }
        }

        /// <summary>
        /// Executes the typed restoration action if supported.
        /// </summary>
        /// <param name="output">The typed output data for restoration.</param>
        /// <param name="foundry">The workflow foundry providing context and services.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the restoration operation.</returns>
        public override async Task RestoreAsync(TInput output, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (_restoreFunc == null)
                return;

            try
            {
                await _restoreFunc(output, foundry, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                throw new WorkflowRestoreException($"Action operation '{Name}' failed during restoration.", ex);
            }
        }
    }
}