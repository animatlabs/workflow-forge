using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Constants;
using WorkflowForge.Events;
using WorkflowForge.Extensions;
using WorkflowForge.Loggers;
using WorkflowForge.Options;

namespace WorkflowForge
{
    /// <summary>
    /// Core implementation of IWorkflowFoundry - the execution environment for workflows.
    /// Provides thread-safe workflow execution with foundry property management.
    /// Simplified, dependency-free implementation focused on core functionality.
    /// </summary>
    internal sealed class WorkflowFoundry : IWorkflowFoundry
    {
        private readonly List<IWorkflowOperation> _operations = new();
        private readonly List<IWorkflowOperationMiddleware> _middlewares = new();
        private readonly object _middlewareLock = new();
        private readonly ISystemTimeProvider _timeProvider;
        private WorkflowForgeOptions _options;
        private volatile bool _disposed;
        private IWorkflow? _currentWorkflow;
        private int _executionState;
        private volatile bool _isFrozen;
        private IWorkflowOperation[]? _cachedOperations;
        private IWorkflowOperationMiddleware[]? _cachedMiddlewares;

        public event EventHandler<OperationStartedEventArgs>? OperationStarted;

        public event EventHandler<OperationCompletedEventArgs>? OperationCompleted;

        public event EventHandler<OperationFailedEventArgs>? OperationFailed;

        /// <inheritdoc />
        public Guid ExecutionId { get; private set; }

        /// <inheritdoc />
        public ConcurrentDictionary<string, object?> Properties { get; }

        /// <inheritdoc />
        public IWorkflow? CurrentWorkflow => _currentWorkflow;

        /// <inheritdoc />
        public IWorkflowForgeLogger Logger { get; private set; }

        /// <inheritdoc />
        public WorkflowForgeOptions Options => _options;

        /// <inheritdoc />
        public IServiceProvider? ServiceProvider { get; private set; }

        /// <inheritdoc />
        public bool IsFrozen => _isFrozen;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowFoundry"/> class with explicit logger and service provider.
        /// </summary>
        /// <param name="executionId">The unique identifier for this foundry execution instance.</param>
        /// <param name="properties">The foundry properties container.</param>
        /// <param name="logger">The logger for this foundry.</param>
        /// <param name="serviceProvider">Optional service provider for dependency injection.</param>
        /// <param name="currentWorkflow">Optional initial workflow to associate with this foundry.</param>
        /// <param name="timeProvider">The time provider to use for timestamps.</param>
        /// <param name="options">Optional execution options for this foundry.</param>
        /// <exception cref="ArgumentNullException">Thrown when properties is null.</exception>
        public WorkflowFoundry(
            Guid executionId,
            ConcurrentDictionary<string, object?> properties,
            IWorkflowForgeLogger? logger = null,
            IServiceProvider? serviceProvider = null,
            IWorkflow? currentWorkflow = null,
            ISystemTimeProvider? timeProvider = null,
            WorkflowForgeOptions? options = null)
        {
            ExecutionId = executionId;
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
            Logger = logger ?? NullLogger.Instance;
            ServiceProvider = serviceProvider;
            _currentWorkflow = currentWorkflow;
            _timeProvider = timeProvider ?? SystemTimeProvider.Instance;
            _options = options?.CloneTyped() ?? new WorkflowForgeOptions();
        }

        /// <inheritdoc />
        public void SetCurrentWorkflow(IWorkflow? workflow)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(WorkflowFoundry));
            _currentWorkflow = workflow;
        }

        /// <summary>
        /// Adds an operation to be executed in this foundry.
        /// </summary>
        /// <param name="operation">The operation to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when operation is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the foundry has been disposed.</exception>
        public void AddOperation(IWorkflowOperation operation)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(WorkflowFoundry));
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            ThrowIfFrozen();

            lock (_operations)
            {
                _operations.Add(operation);
            }
        }

        /// <summary>
        /// Replaces the current operations with a new sequence.
        /// </summary>
        /// <param name="operations">The operations to set.</param>
        /// <exception cref="ArgumentNullException">Thrown when operations is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the foundry has been disposed.</exception>
        public void ReplaceOperations(IEnumerable<IWorkflowOperation> operations)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(WorkflowFoundry));
            if (operations == null)
                throw new ArgumentNullException(nameof(operations));
            ThrowIfFrozen();

            lock (_operations)
            {
                _operations.Clear();
                _operations.AddRange(operations);
                _cachedOperations = null;
            }
        }

        /// <summary>
        /// Adds middleware to the execution pipeline.
        /// </summary>
        /// <param name="middleware">The middleware to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when middleware is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the foundry has been disposed.</exception>
        public void AddMiddleware(IWorkflowOperationMiddleware middleware)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(WorkflowFoundry));
            if (middleware == null)
                throw new ArgumentNullException(nameof(middleware));
            ThrowIfFrozen();

            lock (_middlewareLock)
            {
                _middlewares.Add(middleware);
            }
        }

        /// <summary>
        /// Adds multiple middleware components to the execution pipeline.
        /// </summary>
        /// <param name="middlewares">The middleware components to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when middlewares is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the foundry has been disposed.</exception>
        public void AddMiddlewares(IEnumerable<IWorkflowOperationMiddleware> middlewares)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(WorkflowFoundry));
            if (middlewares == null)
                throw new ArgumentNullException(nameof(middlewares));
            ThrowIfFrozen();

            lock (_middlewareLock)
            {
                _middlewares.AddRange(middlewares);
            }
        }

        /// <summary>
        /// Removes middleware from the execution pipeline.
        /// </summary>
        /// <param name="middleware">The middleware to remove.</param>
        /// <returns>True if the middleware was found and removed; otherwise, false.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the foundry has been disposed.</exception>
        public bool RemoveMiddleware(IWorkflowOperationMiddleware middleware)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(WorkflowFoundry));
            ThrowIfFrozen();
            lock (_middlewareLock)
            {
                return _middlewares.Remove(middleware);
            }
        }

        /// <summary>
        /// Gets the number of middleware components in the pipeline.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the foundry has been disposed.</exception>
        public int MiddlewareCount
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(WorkflowFoundry));
                lock (_middlewareLock)
                {
                    return _middlewares.Count;
                }
            }
        }

        /// <summary>
        /// Executes all operations in the foundry.
        /// Operations are executed sequentially in the order they were added, with middleware pipeline applied.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the execution of all operations.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the foundry has been disposed.</exception>
        public async Task ForgeAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(WorkflowFoundry));
            if (Interlocked.Exchange(ref _executionState, 1) == 1)
            {
                throw new InvalidOperationException("Foundry is already executing.");
            }

            _isFrozen = true;

            try
            {
                IWorkflowOperation[] operationsSnapshot;
                lock (_operations)
                {
                    _cachedOperations ??= _operations.ToArray();
                    operationsSnapshot = _cachedOperations;
                }

                var shouldAggregate = Options.ContinueOnError;
                var errors = shouldAggregate
                    ? new List<Exception>()
                    : null;

                object? inputData = null;

                for (int i = 0; i < operationsSnapshot.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    inputData = await ExecuteOperationAtIndexAsync(
                        operationsSnapshot[i], i, inputData, errors, cancellationToken).ConfigureAwait(false);
                }

                if (errors != null && errors.Count > 0)
                {
                    throw new AggregateException("One or more operations failed during execution.", errors);
                }
            }
            finally
            {
                _isFrozen = false;
                Interlocked.Exchange(ref _executionState, 0);
            }
        }

        /// <summary>
        /// Executes a single operation at the given index, handling events, property tracking, and error aggregation.
        /// Returns the (potentially chained) input data for the next operation.
        /// </summary>
        private async Task<object?> ExecuteOperationAtIndexAsync(
            IWorkflowOperation operation,
            int index,
            object? inputData,
            List<Exception>? errors,
            CancellationToken cancellationToken)
        {
            var operationStartTime = _timeProvider.UtcNow;

            try
            {
                InvokeOperationStarted(operation);
                Properties[FoundryPropertyKeys.CurrentOperationIndex] = index;

                var result = await ExecuteOperationWithMiddleware(operation, inputData, cancellationToken).ConfigureAwait(false);
                inputData = ApplyOutputChaining(inputData, result);

                ApplyOperationSuccessProperties(operation, index, result);
                InvokeOperationCompleted(operation, result, operationStartTime);

                return inputData;
            }
            catch (Exception ex)
            {
                return await HandleOperationFailureAsync(operation, index, inputData, ex, operationStartTime, errors).ConfigureAwait(false);
            }
        }

        private object? ApplyOutputChaining(object? inputData, object? result)
        {
            return Options.EnableOutputChaining ? result : inputData;
        }

        private void ApplyOperationSuccessProperties(IWorkflowOperation operation, int index, object? result)
        {
            Properties[string.Format(FoundryPropertyKeys.OperationOutputFormat, index, operation.Name)] = result;
            Properties[FoundryPropertyKeys.LastCompletedIndex] = index;
            Properties[FoundryPropertyKeys.LastCompletedName] = operation.Name;
            Properties[FoundryPropertyKeys.LastCompletedId] = operation.Id;
        }

        private void InvokeOperationStarted(IWorkflowOperation operation)
        {
            try
            {
                OperationStarted?.Invoke(this, new OperationStartedEventArgs(operation, this, null));
            }
            catch (Exception ex)
            {
                Logger.LogError(Logger.CreateErrorProperties(ex, "OperationStarted"), ex, "OperationStarted event handler error");
            }
        }

        private void InvokeOperationCompleted(IWorkflowOperation operation, object? result, DateTimeOffset operationStartTime)
        {
            var operationDuration = _timeProvider.UtcNow - operationStartTime;
            try
            {
                OperationCompleted?.Invoke(this, new OperationCompletedEventArgs(
                    operation, this, null, result,
                    TimeSpan.FromMilliseconds(operationDuration.TotalMilliseconds)));
            }
            catch (Exception ex)
            {
                Logger.LogError(Logger.CreateErrorProperties(ex, "OperationCompleted"), ex, "OperationCompleted event handler error");
            }
        }

        private Task<object?> HandleOperationFailureAsync(
            IWorkflowOperation operation,
            int index,
            object? inputData,
            Exception ex,
            DateTimeOffset operationStartTime,
            List<Exception>? errors)
        {
            Properties[FoundryPropertyKeys.LastFailedIndex] = index;
            Properties[FoundryPropertyKeys.LastFailedName] = operation.Name;
            Properties[FoundryPropertyKeys.LastFailedId] = operation.Id;

            var operationDuration = _timeProvider.UtcNow - operationStartTime;
            OperationFailed?.Invoke(this, new OperationFailedEventArgs(
                operation, this, null, ex,
                TimeSpan.FromMilliseconds(operationDuration.TotalMilliseconds)));

            if (ex is OperationCanceledException)
            {
                throw ex;
            }

            if (errors != null)
            {
                errors.Add(ex);
                return Task.FromResult(inputData);
            }

            throw ex;
        }

        /// <summary>
        /// Executes a single operation through the middleware pipeline.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="inputData">The input data for the operation.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The output data from the operation.</returns>
        private async Task<object?> ExecuteOperationWithMiddleware(
            IWorkflowOperation operation,
            object? inputData,
            CancellationToken cancellationToken)
        {
            IWorkflowOperationMiddleware[] middlewareSnapshot;
            lock (_middlewareLock)
            {
                _cachedMiddlewares ??= _middlewares.ToArray();
                middlewareSnapshot = _cachedMiddlewares;
            }

            if (middlewareSnapshot.Length == 0)
            {
                // No middleware, execute operation directly
                return await operation.ForgeAsync(inputData, this, cancellationToken).ConfigureAwait(false);
            }

            // Russian Doll pattern: Middleware wraps from inside-out (reverse iteration).
            // First middleware added = outermost layer. See /docs/architecture/middleware-pipeline.md

            Func<CancellationToken, Task<object?>> next = token => operation.ForgeAsync(inputData, this, token);

            for (int i = middlewareSnapshot.Length - 1; i >= 0; i--)
            {
                var middleware = middlewareSnapshot[i];
                var currentNext = next;
                next = token => middleware.ExecuteAsync(operation, this, inputData, currentNext, token);
            }

            return await next(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            OperationStarted = null;
            OperationCompleted = null;
            OperationFailed = null;

            _disposed = true;
            // Dispose operations
            lock (_operations)
            {
                foreach (var operation in _operations.OfType<IDisposable>())
                {
                    try
                    {
                        operation.Dispose();
                    }
                    catch (Exception)
                    {
                        // Intentionally swallowed: disposal must not prevent remaining
                        // operations from being cleaned up.
                    }
                }
                _operations.Clear();
                _cachedOperations = null;
            }
            lock (_middlewareLock)
            {
                _middlewares.Clear();
                _cachedMiddlewares = null;
            }
            // Dispose properties
            Properties.Clear();
            GC.SuppressFinalize(this);
        }

        private void ThrowIfFrozen()
        {
            if (_isFrozen)
            {
                throw new InvalidOperationException("Foundry pipeline is frozen during execution.");
            }
        }

        /// <summary>
        /// Resets the foundry state so it can be reused from a pool.
        /// </summary>
        internal void Reset(
            Guid executionId,
            IWorkflowForgeLogger logger,
            IServiceProvider? serviceProvider,
            WorkflowForgeOptions options,
            IWorkflow? currentWorkflow = null)
        {
            ExecutionId = executionId;
            Logger = logger;
            ServiceProvider = serviceProvider;
            _options = options;
            _currentWorkflow = currentWorkflow;

            _disposed = false;
            _executionState = 0;
            _isFrozen = false;

            // Dispose handles full cleanup of collections/properties,
            // so this just needs to reset the flags.
        }
    }
}
