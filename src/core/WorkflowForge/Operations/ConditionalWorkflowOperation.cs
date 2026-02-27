using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Operations
{
    /// <summary>
    /// Executes different operations based on a condition evaluation with data flow support.
    /// The orchestrator controls data flow and this operation routes it based on conditions.
    /// </summary>
    public sealed class ConditionalWorkflowOperation : WorkflowOperationBase
    {
        private readonly Func<object?, IWorkflowFoundry, CancellationToken, Task<bool>> _condition;
        private readonly IWorkflowOperation _trueOperation;
        private readonly IWorkflowOperation? _falseOperation;
        private volatile bool _disposed;
        private volatile bool _lastConditionResult;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConditionalWorkflowOperation"/> class.
        /// </summary>
        /// <param name="condition">The condition to evaluate. Receives input data for evaluation.</param>
        /// <param name="trueOperation">The operation to execute if the condition is true.</param>
        /// <param name="falseOperation">The optional operation to execute if the condition is false.</param>
        /// <param name="name">Optional name for the operation.</param>
        /// <param name="id">Optional operation ID.</param>
        public ConditionalWorkflowOperation(
            Func<object?, IWorkflowFoundry, CancellationToken, Task<bool>> condition,
            IWorkflowOperation trueOperation,
            IWorkflowOperation? falseOperation = null,
            string? name = null,
            Guid? id = null)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
            _trueOperation = trueOperation ?? throw new ArgumentNullException(nameof(trueOperation));
            _falseOperation = falseOperation;

            Id = id ?? Guid.NewGuid();
            Name = name ?? "ConditionalOperation";
        }

        /// <summary>
        /// Initializes with a simple condition that doesn't use input data.
        /// </summary>
        /// <param name="condition">The simple condition to evaluate.</param>
        /// <param name="trueOperation">The operation to execute if the condition is true.</param>
        /// <param name="falseOperation">The optional operation to execute if the condition is false.</param>
        /// <param name="name">Optional name for the operation.</param>
        /// <param name="id">Optional operation ID.</param>
        public ConditionalWorkflowOperation(
            Func<IWorkflowFoundry, CancellationToken, Task<bool>> condition,
            IWorkflowOperation trueOperation,
            IWorkflowOperation? falseOperation = null,
            string? name = null,
            Guid? id = null)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            _condition = (inputData, foundry, cancellationToken) => condition(foundry, cancellationToken);
            _trueOperation = trueOperation ?? throw new ArgumentNullException(nameof(trueOperation));
            _falseOperation = falseOperation;

            Id = id ?? Guid.NewGuid();
            Name = name ?? "ConditionalOperation";
        }

        /// <summary>
        /// Initializes with a synchronous condition.
        /// </summary>
        /// <param name="condition">The synchronous condition to evaluate.</param>
        /// <param name="trueOperation">The operation to execute if the condition is true.</param>
        /// <param name="falseOperation">The optional operation to execute if the condition is false.</param>
        /// <param name="name">Optional name for the operation.</param>
        /// <param name="id">Optional operation ID.</param>
        public ConditionalWorkflowOperation(
            Func<object?, IWorkflowFoundry, bool> condition,
            IWorkflowOperation trueOperation,
            IWorkflowOperation? falseOperation = null,
            string? name = null,
            Guid? id = null)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            _condition = (inputData, foundry, cancellationToken) => Task.FromResult(condition(inputData, foundry));
            _trueOperation = trueOperation ?? throw new ArgumentNullException(nameof(trueOperation));
            _falseOperation = falseOperation;

            Id = id ?? Guid.NewGuid();
            Name = name ?? "ConditionalOperation";
        }

        /// <inheritdoc />
        public override Guid Id { get; }

        /// <inheritdoc />
        public override string Name { get; }

        /// <inheritdoc />
        protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ConditionalWorkflowOperation));
            if (foundry == null)
                throw new ArgumentNullException(nameof(foundry));

            // Evaluate condition with input data
            _lastConditionResult = await _condition(inputData, foundry, cancellationToken).ConfigureAwait(false);

            if (_lastConditionResult)
            {
                return await _trueOperation.ForgeAsync(inputData, foundry, cancellationToken).ConfigureAwait(false);
            }
            else if (_falseOperation != null)
            {
                return await _falseOperation.ForgeAsync(inputData, foundry, cancellationToken).ConfigureAwait(false);
            }

            return null; // No false operation, return null
        }

        /// <inheritdoc />
        public override async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ConditionalWorkflowOperation));
            if (foundry == null)
                throw new ArgumentNullException(nameof(foundry));

            // Restore based on the last condition result
            if (_lastConditionResult)
            {
                await _trueOperation.RestoreAsync(outputData, foundry, cancellationToken).ConfigureAwait(false);
            }
            else if (_falseOperation != null)
            {
                await _falseOperation.RestoreAsync(outputData, foundry, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                try
                {
                    _trueOperation?.Dispose();
                }
                catch (Exception)
                {
                    // Swallow disposal exceptions
                }

                try
                {
                    _falseOperation?.Dispose();
                }
                catch (Exception)
                {
                    // Swallow disposal exceptions
                }
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Creates a conditional operation with data-aware condition.
        /// </summary>
        /// <param name="condition">The condition that receives input data for evaluation.</param>
        /// <param name="trueOperation">The operation to execute when condition is true.</param>
        /// <param name="falseOperation">The optional operation to execute when condition is false.</param>
        /// <param name="name">Optional name for the operation.</param>
        /// <returns>A new conditional operation instance.</returns>
        public static ConditionalWorkflowOperation CreateDataAware(
            Func<object?, IWorkflowFoundry, CancellationToken, Task<bool>> condition,
            IWorkflowOperation trueOperation,
            IWorkflowOperation? falseOperation = null,
            string? name = null)
        {
            return new ConditionalWorkflowOperation(condition, trueOperation, falseOperation, name);
        }

        /// <summary>
        /// Creates a conditional operation with typed input data.
        /// </summary>
        /// <typeparam name="T">The type of input data.</typeparam>
        /// <param name="condition">The typed condition function.</param>
        /// <param name="trueOperation">The operation to execute when condition is true.</param>
        /// <param name="falseOperation">The optional operation to execute when condition is false.</param>
        /// <param name="name">Optional name for the operation.</param>
        /// <returns>A new conditional operation instance.</returns>
        public static ConditionalWorkflowOperation CreateTyped<T>(
            Func<T, IWorkflowFoundry, CancellationToken, Task<bool>> condition,
            IWorkflowOperation trueOperation,
            IWorkflowOperation? falseOperation = null,
            string? name = null)
        {
            return new ConditionalWorkflowOperation(
                async (inputData, foundry, cancellationToken) =>
                {
                    var typedInput = (T)inputData!;
                    return await condition(typedInput, foundry, cancellationToken).ConfigureAwait(false);
                },
                trueOperation,
                falseOperation,
                name);
        }

        /// <summary>
        /// Creates a conditional operation with simple condition (legacy compatibility).
        /// </summary>
        /// <param name="condition">The simple condition to evaluate.</param>
        /// <param name="trueOperation">The operation to execute when condition is true.</param>
        /// <param name="falseOperation">The optional operation to execute when condition is false.</param>
        /// <param name="name">Optional name for the operation.</param>
        /// <returns>A new conditional operation instance.</returns>
        public static ConditionalWorkflowOperation Create(
            Func<IWorkflowFoundry, bool> condition,
            IWorkflowOperation trueOperation,
            IWorkflowOperation? falseOperation = null,
            string? name = null)
        {
            return new ConditionalWorkflowOperation(
                (inputData, foundry, cancellationToken) => Task.FromResult(condition(foundry)),
                trueOperation,
                falseOperation,
                name);
        }
    }
}
