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
using WorkflowForge.Middleware;
using WorkflowForge.Services;

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
        private volatile IWorkflow? _currentWorkflow;
        private readonly bool _ownsProperties;
        private FoundryServices _services;
        private int _executionState;
        private volatile bool _isFrozen;
        private IWorkflowOperation[]? _cachedOperations;
        private string[]? _cachedOperationOutputKeys;
        private IWorkflowOperationMiddleware[]? _cachedMiddlewares;
        private readonly OperationMiddlewarePipelineState _middlewarePipelineState = new();
        private string[]? _activeOperationOutputKeys;
        // Written on operation-completion continuations and read by the smith from a different
        // continuation, so every access goes through Volatile/Interlocked.
        private int _currentOperationIndex = -1;
        private int _lastCompletedIndex = -1;
        private readonly object _lastCompletedIdLock = new();
        private Guid _lastCompletedId;

        internal int CurrentOperationIndex => Volatile.Read(ref _currentOperationIndex);

        internal int LastCompletedIndex => Volatile.Read(ref _lastCompletedIndex);

        internal Guid LastCompletedId
        {
            get
            {
                lock (_lastCompletedIdLock)
                {
                    return _lastCompletedId;
                }
            }
        }

        public event EventHandler<OperationStartedEventArgs>? OperationStarted;

        public event EventHandler<OperationCompletedEventArgs>? OperationCompleted;

        public event EventHandler<OperationFailedEventArgs>? OperationFailed;

        /// <inheritdoc />
        public Guid ExecutionId { get; private set; }

        /// <inheritdoc />
        public ConcurrentDictionary<string, object?> Properties { get; }

        /// <inheritdoc />
        public IFoundryServices Services => _services;

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
        /// <param name="ownsProperties">Whether the foundry may clear <paramref name="properties"/> when its lease ends.</param>
        /// <exception cref="ArgumentNullException">Thrown when properties is null.</exception>
        public WorkflowFoundry(
            Guid executionId,
            ConcurrentDictionary<string, object?> properties,
            IWorkflowForgeLogger? logger = null,
            IServiceProvider? serviceProvider = null,
            IWorkflow? currentWorkflow = null,
            ISystemTimeProvider? timeProvider = null,
            WorkflowForgeOptions? options = null,
            bool ownsProperties = true)
        {
            ExecutionId = executionId;
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
            Logger = logger ?? NullLogger.Instance;
            ServiceProvider = serviceProvider;
            _currentWorkflow = currentWorkflow;
            _timeProvider = timeProvider ?? SystemTimeProvider.Instance;
            _options = options?.CloneTyped() ?? new WorkflowForgeOptions();
            _ownsProperties = ownsProperties;
            _services = new FoundryServices(Logger);
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
                InvalidateOperationCaches();
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
                InvalidateOperationCaches();
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
                _cachedMiddlewares = null;
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
                _cachedMiddlewares = null;
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
                var removed = _middlewares.Remove(middleware);
                if (removed)
                {
                    _cachedMiddlewares = null;
                }

                return removed;
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
        /// <exception cref="InvalidOperationException">Thrown when the foundry is already executing.</exception>
        /// <exception cref="AggregateException">
        /// Rethrown when <see cref="WorkflowForgeOptions.ContinueOnError"/> collected operation failures.
        /// </exception>
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
                    EnsureOperationCaches();
                    operationsSnapshot = _cachedOperations!;
                    _activeOperationOutputKeys = _cachedOperationOutputKeys;
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
                _activeOperationOutputKeys = null;
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
                SetCurrentOperationIndex(index);

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
            var outputKey = _activeOperationOutputKeys != null && index < _activeOperationOutputKeys.Length
                ? _activeOperationOutputKeys[index]
                : string.Format(FoundryPropertyKeys.OperationOutputFormat, index, operation.Name);
            Properties[outputKey] = result;
            SetLastCompletedOperation(index, operation.Name, operation.Id);
        }

        private void SetCurrentOperationIndex(int index)
        {
            Volatile.Write(ref _currentOperationIndex, index);
            Properties[FoundryPropertyKeys.CurrentOperationIndex] = index;
        }

        private void SetLastCompletedOperation(int index, string operationName, Guid operationId)
        {
            Volatile.Write(ref _lastCompletedIndex, index);
            lock (_lastCompletedIdLock)
            {
                _lastCompletedId = operationId;
            }

            Properties[FoundryPropertyKeys.LastCompletedIndex] = index;
            Properties[FoundryPropertyKeys.LastCompletedName] = operationName;
            Properties[FoundryPropertyKeys.LastCompletedId] = operationId;
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
            try
            {
                OperationFailed?.Invoke(this, new OperationFailedEventArgs(
                    operation, this, null, ex,
                    TimeSpan.FromMilliseconds(operationDuration.TotalMilliseconds)));
            }
            catch (Exception handlerEx)
            {
                // A throwing event subscriber must not replace or mask the operation's real
                // exception (which is re-thrown below). Log and continue.
                Logger.LogError(Logger.CreateErrorProperties(handlerEx, "OperationFailed"), handlerEx, "OperationFailed event handler error");
            }

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

            // Russian Doll pattern: first middleware added is the outermost layer.
            _middlewarePipelineState.Initialize(this, operation, inputData, middlewareSnapshot);
            return await _middlewarePipelineState.InvokeAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Ends the foundry lease: clears events, operations, middleware and properties, and disposes
        /// everything registered on <see cref="Services"/>. Caller-supplied operations and middleware
        /// are released but not disposed.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            OperationStarted = null;
            OperationCompleted = null;
            OperationFailed = null;

            _disposed = true;

            // Operations and middleware are supplied by the caller and are not owned here;
            // only the references are released. Services are the explicit opt-in for
            // foundry-owned disposables and are the only thing torn down.
            lock (_operations)
            {
                _operations.Clear();
                InvalidateOperationCaches();
            }
            lock (_middlewareLock)
            {
                _middlewares.Clear();
                _cachedMiddlewares = null;
            }

            _services.DisposeAll();

            if (_ownsProperties)
            {
                Properties.Clear();
            }

            ResetOperationTrackingFields();
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
        /// Resets the foundry state so it can be reused from a pool: clears the three lifecycle
        /// events, the operation list, the middleware pipeline and all properties, and disposes
        /// everything registered on <see cref="Services"/>. Caller-supplied operations and
        /// middleware are released but not disposed.
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
            _options = options?.CloneTyped() ?? new WorkflowForgeOptions();
            _currentWorkflow = currentWorkflow;

            OperationStarted = null;
            OperationCompleted = null;
            OperationFailed = null;

            _disposed = false;
            _executionState = 0;
            _isFrozen = false;

            // Clear all per-execution state. This foundry is being reused from a pool, and Dispose
            // is NOT called on the pooled path — so unless we clear here, a previous workflow's
            // Properties (including operation-tracking keys such as LastCompletedIndex and cached
            // operation outputs) leak into the next, unrelated workflow. That both bleeds data
            // across executions and can crash compensation with a stale index when a later, smaller
            // workflow fails.
            lock (_operations)
            {
                _operations.Clear();
                InvalidateOperationCaches();
            }
            lock (_middlewareLock)
            {
                _middlewares.Clear();
                _cachedMiddlewares = null;
            }

            _services.DisposeAll();
            _services = new FoundryServices(logger);
            Properties.Clear();
            ResetOperationTrackingFields();
        }

        private void EnsureOperationCaches()
        {
            if (_cachedOperations != null)
            {
                return;
            }

            _cachedOperations = _operations.ToArray();
            _cachedOperationOutputKeys = BuildOperationOutputKeys(_cachedOperations);
        }

        private void InvalidateOperationCaches()
        {
            _cachedOperations = null;
            _cachedOperationOutputKeys = null;
            _activeOperationOutputKeys = null;
        }

        private static string[] BuildOperationOutputKeys(IWorkflowOperation[] operations)
        {
            var keys = new string[operations.Length];
            for (int i = 0; i < operations.Length; i++)
            {
                keys[i] = string.Format(FoundryPropertyKeys.OperationOutputFormat, i, operations[i].Name);
            }

            return keys;
        }

        private void ResetOperationTrackingFields()
        {
            Volatile.Write(ref _currentOperationIndex, -1);
            Volatile.Write(ref _lastCompletedIndex, -1);
            lock (_lastCompletedIdLock)
            {
                _lastCompletedId = Guid.Empty;
            }

            _activeOperationOutputKeys = null;
        }
    }
}
