using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Constants;

namespace WorkflowForge.Operations
{
    /// <summary>
    /// Executes a workflow operation for each item in a collection with configurable data strategy.
    /// Supports parallel and sequential execution with proper resource management and error handling.
    /// </summary>
    public sealed class ForEachWorkflowOperation : WorkflowOperationBase
    {
        private readonly List<IWorkflowOperation> _operations;
        private readonly ISystemTimeProvider _timeProvider;
        private readonly TimeSpan? _timeout;
        private readonly ForEachDataStrategy _dataStrategy;
        private readonly int? _maxConcurrency;
        private volatile bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ForEachWorkflowOperation"/> class.
        /// </summary>
        /// <param name="operations">The operations to execute in parallel.</param>
        /// <param name="timeout">Optional timeout for the entire foreach operation.</param>
        /// <param name="dataStrategy">Strategy for distributing input data among operations.</param>
        /// <param name="maxConcurrency">Optional maximum number of concurrent operations (throttling).</param>
        /// <param name="name">Optional name for the operation.</param>
        /// <param name="id">Optional operation ID. If null, a new GUID is generated.</param>
        /// <param name="timeProvider">The time provider to use for timestamps.</param>
        /// <exception cref="ArgumentNullException">Thrown when operations is null.</exception>
        /// <exception cref="ArgumentException">Thrown when operations is empty or maxConcurrency is invalid.</exception>
        public ForEachWorkflowOperation(
            IEnumerable<IWorkflowOperation> operations,
            TimeSpan? timeout = null,
            ForEachDataStrategy dataStrategy = ForEachDataStrategy.SharedInput,
            int? maxConcurrency = null,
            string? name = null,
            Guid? id = null,
            ISystemTimeProvider? timeProvider = null)
        {
            if (operations == null)
                throw new ArgumentNullException(nameof(operations));
            if (operations.Any(op => op == null))
                throw new ArgumentException("Operations collection contains null elements.", nameof(operations));

            _operations = operations.ToList();
            if (_operations.Count == 0)
                throw new ArgumentException("At least one operation must be provided.", nameof(operations));

            if (maxConcurrency.HasValue && maxConcurrency.Value <= 0)
                throw new ArgumentException("Max concurrency must be greater than zero.", nameof(maxConcurrency));

            _timeProvider = timeProvider ?? SystemTimeProvider.Instance;

            _timeout = timeout;
            _dataStrategy = dataStrategy;
            _maxConcurrency = maxConcurrency;

            Id = id ?? Guid.NewGuid();
            Name = name ?? $"ForEach[{_operations.Count}]";
        }

        /// <inheritdoc />
        public override Guid Id { get; }

        /// <inheritdoc />
        public override string Name { get; }

        /// <inheritdoc />
        protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ForEachWorkflowOperation));
            if (foundry == null)
                throw new ArgumentNullException(nameof(foundry));

            // Use operation-specific concurrency setting (from constructor)
            var effectiveMaxConcurrency = _maxConcurrency;

            var workflowId = foundry.CurrentWorkflow?.Id ?? Guid.Empty;
            var workflowName = foundry.CurrentWorkflow?.Name ?? FoundryPropertyKeys.UnknownValue;

            foundry.Logger.LogInformation(
                "Starting ForEach operation {OperationName} for workflow {WorkflowName} ({WorkflowId}) with {ChildOperationCount} operations, max concurrency: {EffectiveMaxConcurrency}",
                Name, workflowName, workflowId, _operations.Count, effectiveMaxConcurrency?.ToString() ?? "unlimited");

            object?[] results;
            using var timeoutCts = _timeout.HasValue ? new CancellationTokenSource(_timeout.Value) : null;
            using var combinedCts = _timeout.HasValue
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts!.Token)
                : null;
            var effectiveToken = combinedCts?.Token ?? cancellationToken;

            if (effectiveMaxConcurrency.HasValue)
            {
                // Execute with throttling
                using var semaphore = new SemaphoreSlim(effectiveMaxConcurrency.Value, effectiveMaxConcurrency.Value);
                var tasks = _operations.Select((op, index) =>
                    ForgeOperationWithThrottlingAsync(op, inputData, index, foundry, semaphore, effectiveToken)).ToArray();

                results = await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            else
            {
                // Execute without throttling
                var tasks = _operations.Select((op, index) =>
                    ForgeOperationAsync(op, inputData, index, foundry, effectiveToken)).ToArray();

                results = await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            foundry.Logger.LogInformation(
                "Completed ForEach operation {OperationName} for workflow {WorkflowName} ({WorkflowId}) with {ResultCount} results",
                Name, workflowName, workflowId, results.Length);

            return CombineResults(results);
        }

        /// <inheritdoc />
        public override async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ForEachWorkflowOperation));
            if (foundry == null)
                throw new ArgumentNullException(nameof(foundry));

            // Use operation-specific concurrency setting (from constructor)
            var effectiveMaxConcurrency = _maxConcurrency;

            var workflowId = foundry.CurrentWorkflow?.Id ?? Guid.Empty;
            var workflowName = foundry.CurrentWorkflow?.Name ?? FoundryPropertyKeys.UnknownValue;

            foundry.Logger.LogInformation(
                "Starting restoration for ForEach operation {OperationName} in workflow {WorkflowName} ({WorkflowId})",
                Name, workflowName, workflowId);

            // Extract individual results for restoration
            var individualResults = ExtractIndividualResults(outputData);

            if (effectiveMaxConcurrency.HasValue)
            {
                // Restore with throttling
                using var semaphore = new SemaphoreSlim(effectiveMaxConcurrency.Value, effectiveMaxConcurrency.Value);
                var tasks = _operations.Select((op, index) =>
                    RestoreOperationWithThrottlingAsync(op, GetResultForIndex(individualResults, index), foundry, semaphore, cancellationToken)).ToArray();

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            else
            {
                // Restore without throttling
                var tasks = _operations.Select((op, index) =>
                    RestoreOperationAsync(op, GetResultForIndex(individualResults, index), foundry, cancellationToken)).ToArray();

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            foundry.Logger.LogInformation(
                "Completed restoration for ForEach operation {OperationName} in workflow {WorkflowName} ({WorkflowId})",
                Name, workflowName, workflowId);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                foreach (var operation in _operations)
                {
                    try
                    {
                        operation?.Dispose();
                    }
                    catch (Exception)
                    {
                        // Intentionally swallowed: disposal exceptions must not propagate
                        // to prevent cascading failures during cleanup.
                    }
                }

                _operations.Clear();
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        private async Task<object?> ForgeOperationAsync(IWorkflowOperation operation, object? inputData, int index, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            var operationInput = GetInputForOperation(inputData, index);
            return await operation.ForgeAsync(operationInput, foundry, cancellationToken).ConfigureAwait(false);
        }

        private async Task<object?> ForgeOperationWithThrottlingAsync(IWorkflowOperation operation, object? inputData, int index, IWorkflowFoundry foundry, SemaphoreSlim semaphore, CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await ForgeOperationAsync(operation, inputData, index, foundry, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private static async Task RestoreOperationAsync(IWorkflowOperation operation, object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            await operation.RestoreAsync(outputData, foundry, cancellationToken).ConfigureAwait(false);
        }

        private static async Task RestoreOperationWithThrottlingAsync(IWorkflowOperation operation, object? outputData, IWorkflowFoundry foundry, SemaphoreSlim semaphore, CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await RestoreOperationAsync(operation, outputData, foundry, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private object? GetInputForOperation(object? inputData, int index) => _dataStrategy switch
        {
            ForEachDataStrategy.SharedInput => inputData,
            ForEachDataStrategy.SplitInput => ExtractDataForIndex(inputData, index),
            ForEachDataStrategy.NoInput => null,
            _ => throw new InvalidOperationException($"Unsupported ForEach data strategy: {_dataStrategy}")
        };

        private object? CombineResults(object?[] results)
        {
            return new ForEachResults
            {
                Results = results,
                TotalResults = results.Length,
                Timestamp = _timeProvider.UtcNow
            };
        }

        private static object?[] ExtractIndividualResults(object? outputData)
        {
            if (outputData is ForEachResults forEachResults)
                return forEachResults.Results;

            if (outputData is object[] array)
                return array;

            return new object?[] { outputData };
        }

        private static object? GetResultForIndex(object?[] results, int index)
        {
            return index < results.Length ? results[index] : null;
        }

        private static object? ExtractDataForIndex(object? inputData, int index)
        {
            if (inputData == null)
                return null;

            // Handle array types
            if (inputData is Array array && index < array.Length)
                return array.GetValue(index);

            // Handle generic lists
            if (inputData is IList list && index < list.Count)
                return list[index];

            // Handle enumerable (convert to list for indexing)
            if (inputData is IEnumerable enumerable && enumerable is not string)
            {
                var items = enumerable.Cast<object>().ToList();
                return index < items.Count ? items[index] : null;
            }

            // If it's not a collection, return the input for all operations
            return inputData;
        }

        /// <summary>
        /// Creates a ForEach operation where all operations share the same input data.
        /// </summary>
        /// <param name="operations">The operations to execute.</param>
        /// <param name="timeout">Optional timeout for the entire operation.</param>
        /// <param name="maxConcurrency">Optional maximum number of concurrent operations.</param>
        /// <param name="name">Optional name for the operation.</param>
        /// <returns>A new ForEach operation instance.</returns>
        public static ForEachWorkflowOperation CreateSharedInput(
            IEnumerable<IWorkflowOperation> operations,
            TimeSpan? timeout = null,
            int? maxConcurrency = null,
            string? name = null)
        {
            return new ForEachWorkflowOperation(operations, timeout, ForEachDataStrategy.SharedInput, maxConcurrency, name);
        }

        /// <summary>
        /// Creates a ForEach operation where input data is split among operations.
        /// </summary>
        /// <param name="operations">The operations to execute.</param>
        /// <param name="timeout">Optional timeout for the entire operation.</param>
        /// <param name="maxConcurrency">Optional maximum number of concurrent operations.</param>
        /// <param name="name">Optional name for the operation.</param>
        /// <returns>A new ForEach operation instance.</returns>
        public static ForEachWorkflowOperation CreateSplitInput(
            IEnumerable<IWorkflowOperation> operations,
            TimeSpan? timeout = null,
            int? maxConcurrency = null,
            string? name = null)
        {
            return new ForEachWorkflowOperation(operations, timeout, ForEachDataStrategy.SplitInput, maxConcurrency, name);
        }

        /// <summary>
        /// Creates a ForEach operation where operations receive no input data.
        /// </summary>
        /// <param name="operations">The operations to execute.</param>
        /// <param name="timeout">Optional timeout for the entire operation.</param>
        /// <param name="maxConcurrency">Optional maximum number of concurrent operations.</param>
        /// <param name="name">Optional name for the operation.</param>
        /// <returns>A new ForEach operation instance.</returns>
        public static ForEachWorkflowOperation CreateNoInput(
            IEnumerable<IWorkflowOperation> operations,
            TimeSpan? timeout = null,
            int? maxConcurrency = null,
            string? name = null)
        {
            return new ForEachWorkflowOperation(operations, timeout, ForEachDataStrategy.NoInput, maxConcurrency, name);
        }

        /// <summary>
        /// Creates a simple ForEach operation (legacy compatibility).
        /// </summary>
        /// <param name="operations">The operations to execute.</param>
        /// <returns>A new ForEach operation instance.</returns>
        public static ForEachWorkflowOperation Create(params IWorkflowOperation[] operations)
        {
            return new ForEachWorkflowOperation(operations);
        }

        /// <summary>
        /// Creates a ForEach operation with concurrency throttling.
        /// </summary>
        /// <param name="operations">The operations to execute.</param>
        /// <param name="maxConcurrency">Maximum number of concurrent operations.</param>
        /// <param name="timeout">Optional timeout for the entire operation.</param>
        /// <returns>A new ForEach operation instance.</returns>
        public static ForEachWorkflowOperation CreateWithThrottling(
            IEnumerable<IWorkflowOperation> operations,
            int maxConcurrency,
            TimeSpan? timeout = null)
        {
            return new ForEachWorkflowOperation(operations, timeout, ForEachDataStrategy.SharedInput, maxConcurrency);
        }
    }
}
