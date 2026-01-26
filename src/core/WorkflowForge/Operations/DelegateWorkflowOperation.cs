using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Exceptions;

namespace WorkflowForge.Operations
{
    /// <summary>
    /// A concrete workflow operation that wraps a delegate function.
    /// Use this for simple, ad-hoc operations. For complex business logic,
    /// consider creating a dedicated class that inherits from WorkflowOperationBase.
    /// </summary>
    public sealed class DelegateWorkflowOperation : WorkflowOperationBase
    {
        private readonly Func<object?, IWorkflowFoundry, CancellationToken, Task<object?>> _executeFunc;
        private readonly Func<object?, IWorkflowFoundry, CancellationToken, Task>? _restoreFunc;
        private readonly bool _supportsRestore;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegateWorkflowOperation"/> class.
        /// </summary>
        /// <param name="name">The name of the operation.</param>
        /// <param name="executeFunc">The function to execute for this operation.</param>
        /// <param name="restoreFunc">The optional restoration function.</param>
        public DelegateWorkflowOperation(
            string name,
            Func<object?, IWorkflowFoundry, CancellationToken, Task<object?>> executeFunc,
            Func<object?, IWorkflowFoundry, CancellationToken, Task>? restoreFunc = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _executeFunc = executeFunc ?? throw new ArgumentNullException(nameof(executeFunc));
            _restoreFunc = restoreFunc;
            _supportsRestore = restoreFunc != null;
        }

        /// <summary>
        /// Gets the name of this operation.
        /// </summary>
        public override string Name { get; }

        /// <summary>
        /// Gets a value indicating whether this operation supports restoration.
        /// </summary>
        public override bool SupportsRestore => _supportsRestore;

        /// <summary>
        /// Executes the operation logic.
        /// </summary>
        /// <param name="inputData">The input data for the operation.</param>
        /// <param name="foundry">The workflow foundry providing context and services.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The output data from the operation.</returns>
        protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _executeFunc(inputData, foundry, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                throw new WorkflowOperationException($"Operation '{Name}' failed during execution.", ex);
            }
        }

        /// <summary>
        /// Restores the operation effects if supported.
        /// </summary>
        /// <param name="outputData">The output data to restore.</param>
        /// <param name="foundry">The workflow foundry providing context and services.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the restoration operation.</returns>
        public override async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (!SupportsRestore)
                throw new NotSupportedException($"Operation '{Name}' does not support restoration.");

            try
            {
                await _restoreFunc!(outputData, foundry, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                throw new WorkflowRestoreException(
                    $"Operation '{Name}' failed during restoration.",
                    ex,
                    foundry.ExecutionId,
                    foundry.CurrentWorkflow?.Id,
                    Name);
            }
        }

        /// <summary>
        /// Creates a simple operation from a synchronous function.
        /// </summary>
        /// <param name="name">The operation name.</param>
        /// <param name="func">The synchronous function to execute.</param>
        /// <returns>A new delegate workflow operation.</returns>
        public static DelegateWorkflowOperation FromSync(string name, Func<object?, object?> func)
        {
            return new DelegateWorkflowOperation(name, (input, _, _) => Task.FromResult(func(input)));
        }

        /// <summary>
        /// Creates a simple operation from an asynchronous function.
        /// </summary>
        /// <param name="name">The operation name.</param>
        /// <param name="func">The asynchronous function to execute.</param>
        /// <returns>A new delegate workflow operation.</returns>
        public static DelegateWorkflowOperation FromAsync(string name, Func<object?, Task<object?>> func)
        {
            return new DelegateWorkflowOperation(name, (input, _, _) => func(input));
        }

        /// <summary>
        /// Creates an operation that performs an action without returning data.
        /// </summary>
        /// <param name="name">The operation name.</param>
        /// <param name="action">The action to perform.</param>
        /// <returns>A new delegate workflow operation.</returns>
        public static DelegateWorkflowOperation FromAction(string name, Action<object?> action)
        {
            return new DelegateWorkflowOperation(name, (input, _, _) =>
            {
                action(input);
                return Task.FromResult<object?>(null);
            });
        }

        /// <summary>
        /// Creates an operation that performs an asynchronous action without returning data.
        /// </summary>
        /// <param name="name">The operation name.</param>
        /// <param name="action">The asynchronous action to perform.</param>
        /// <returns>A new delegate workflow operation.</returns>
        public static DelegateWorkflowOperation FromAsyncAction(string name, Func<object?, Task> action)
        {
            return new DelegateWorkflowOperation(name, async (input, _, _) =>
            {
                await action(input).ConfigureAwait(false);
                return null;
            });
        }
    }

    /// <summary>
    /// A strongly-typed workflow operation that wraps a delegate function.
    /// Use this for simple, ad-hoc typed operations. For complex business logic,
    /// consider creating a dedicated class that inherits from WorkflowOperationBase&lt;TInput, TOutput&gt;.
    /// </summary>
    /// <typeparam name="TInput">The input data type.</typeparam>
    /// <typeparam name="TOutput">The output data type.</typeparam>
    public sealed class DelegateWorkflowOperation<TInput, TOutput> : WorkflowOperationBase<TInput, TOutput>
    {
        private readonly Func<TInput, IWorkflowFoundry, CancellationToken, Task<TOutput>> _executeFunc;
        private readonly Func<TOutput, IWorkflowFoundry, CancellationToken, Task>? _restoreFunc;
        private readonly bool _supportsRestore;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegateWorkflowOperation{TInput, TOutput}"/> class.
        /// </summary>
        /// <param name="name">The name of the operation.</param>
        /// <param name="executeFunc">The strongly-typed function to execute.</param>
        /// <param name="restoreFunc">The optional strongly-typed restoration function.</param>
        public DelegateWorkflowOperation(
            string name,
            Func<TInput, IWorkflowFoundry, CancellationToken, Task<TOutput>> executeFunc,
            Func<TOutput, IWorkflowFoundry, CancellationToken, Task>? restoreFunc = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _executeFunc = executeFunc ?? throw new ArgumentNullException(nameof(executeFunc));
            _restoreFunc = restoreFunc;
            _supportsRestore = restoreFunc != null;
        }

        /// <summary>
        /// Gets the name of this operation.
        /// </summary>
        public override string Name { get; }

        /// <summary>
        /// Gets a value indicating whether this operation supports restoration.
        /// </summary>
        public override bool SupportsRestore => _supportsRestore;

        /// <summary>
        /// Executes the strongly-typed operation logic.
        /// </summary>
        /// <param name="input">The typed input data.</param>
        /// <param name="foundry">The workflow foundry providing context and services.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The typed output data.</returns>
        protected override async Task<TOutput> ForgeAsyncCore(TInput input, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _executeFunc(input, foundry, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                throw new WorkflowOperationException($"Operation '{Name}' failed during execution.", ex);
            }
        }

        /// <summary>
        /// Restores the strongly-typed operation effects if supported.
        /// </summary>
        /// <param name="output">The typed output data to restore.</param>
        /// <param name="foundry">The workflow foundry providing context and services.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the restoration operation.</returns>
        public override async Task RestoreAsync(TOutput output, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (!SupportsRestore)
                throw new NotSupportedException($"Operation '{Name}' does not support restoration.");

            try
            {
                await _restoreFunc!(output, foundry, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                throw new WorkflowRestoreException($"Operation '{Name}' failed during restoration.", ex);
            }
        }

        /// <summary>
        /// Creates a strongly-typed operation from a synchronous function.
        /// </summary>
        /// <param name="name">The operation name.</param>
        /// <param name="func">The synchronous function to execute.</param>
        /// <returns>A new strongly-typed delegate workflow operation.</returns>
        public static DelegateWorkflowOperation<TInput, TOutput> FromSync(string name, Func<TInput, TOutput> func)
        {
            return new DelegateWorkflowOperation<TInput, TOutput>(name, (input, _, _) => Task.FromResult(func(input)));
        }

        /// <summary>
        /// Creates a strongly-typed operation from an asynchronous function.
        /// </summary>
        /// <param name="name">The operation name.</param>
        /// <param name="func">The asynchronous function to execute.</param>
        /// <returns>A new strongly-typed delegate workflow operation.</returns>
        public static DelegateWorkflowOperation<TInput, TOutput> FromAsync(string name, Func<TInput, Task<TOutput>> func)
        {
            return new DelegateWorkflowOperation<TInput, TOutput>(name, (input, _, _) => func(input));
        }

        /// <summary>
        /// Creates a strongly-typed operation with access to the workflow foundry.
        /// </summary>
        /// <param name="name">The operation name.</param>
        /// <param name="func">The function with foundry access.</param>
        /// <returns>A new strongly-typed delegate workflow operation.</returns>
        public static DelegateWorkflowOperation<TInput, TOutput> WithFoundry(string name, Func<TInput, IWorkflowFoundry, Task<TOutput>> func)
        {
            return new DelegateWorkflowOperation<TInput, TOutput>(name, (input, foundry, _) => func(input, foundry));
        }
    }

    /// <summary>
    /// Factory class for creating workflow operations.
    /// Provides a fluent API for common operation creation scenarios.
    /// </summary>
    public static class WorkflowOperations
    {
        /// <summary>
        /// Creates a simple synchronous operation.
        /// </summary>
        /// <param name="name">The operation name.</param>
        /// <param name="func">The synchronous function to execute.</param>
        /// <returns>A new delegate workflow operation.</returns>
        public static DelegateWorkflowOperation Create(string name, Func<object?, object?> func)
        {
            return DelegateWorkflowOperation.FromSync(name, func);
        }

        /// <summary>
        /// Creates a simple asynchronous operation.
        /// </summary>
        /// <param name="name">The operation name.</param>
        /// <param name="func">The asynchronous function to execute.</param>
        /// <returns>A new delegate workflow operation.</returns>
        public static DelegateWorkflowOperation CreateAsync(string name, Func<object?, Task<object?>> func)
        {
            return DelegateWorkflowOperation.FromAsync(name, func);
        }

        /// <summary>
        /// Creates a strongly-typed synchronous operation.
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <param name="name">The operation name.</param>
        /// <param name="func">The synchronous function to execute.</param>
        /// <returns>A new strongly-typed delegate workflow operation.</returns>
        public static DelegateWorkflowOperation<TInput, TOutput> Create<TInput, TOutput>(string name, Func<TInput, TOutput> func)
        {
            return DelegateWorkflowOperation<TInput, TOutput>.FromSync(name, func);
        }

        /// <summary>
        /// Creates a strongly-typed asynchronous operation.
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <param name="name">The operation name.</param>
        /// <param name="func">The asynchronous function to execute.</param>
        /// <returns>A new strongly-typed delegate workflow operation.</returns>
        public static DelegateWorkflowOperation<TInput, TOutput> CreateAsync<TInput, TOutput>(string name, Func<TInput, Task<TOutput>> func)
        {
            return DelegateWorkflowOperation<TInput, TOutput>.FromAsync(name, func);
        }

        /// <summary>
        /// Creates an operation that performs an action without returning data.
        /// </summary>
        /// <param name="name">The operation name.</param>
        /// <param name="action">The action to perform.</param>
        /// <returns>A new delegate workflow operation.</returns>
        public static DelegateWorkflowOperation CreateAction(string name, Action<object?> action)
        {
            return DelegateWorkflowOperation.FromAction(name, action);
        }

        /// <summary>
        /// Creates an operation that performs an asynchronous action without returning data.
        /// </summary>
        /// <param name="name">The operation name.</param>
        /// <param name="action">The asynchronous action to perform.</param>
        /// <returns>A new delegate workflow operation.</returns>
        public static DelegateWorkflowOperation CreateAsyncAction(string name, Func<object?, Task> action)
        {
            return DelegateWorkflowOperation.FromAsyncAction(name, action);
        }
    }
}