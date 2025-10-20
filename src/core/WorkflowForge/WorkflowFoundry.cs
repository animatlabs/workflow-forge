using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Configurations;
using WorkflowForge.Events;
using WorkflowForge.Loggers;

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
        private readonly FoundryConfiguration _configuration;
        private readonly List<IWorkflowOperationMiddleware> _middlewares = new();
        private readonly ISystemTimeProvider _timeProvider;
        private volatile bool _disposed;
        private IWorkflow? _currentWorkflow;

        // ==================================================================================
        // OPERATION LIFECYCLE EVENTS (IOperationLifecycleEvents Implementation)
        // ==================================================================================
        public event EventHandler<OperationStartedEventArgs>? OperationStarted;

        public event EventHandler<OperationCompletedEventArgs>? OperationCompleted;

        public event EventHandler<OperationFailedEventArgs>? OperationFailed;

        /// <inheritdoc />
        public Guid ExecutionId { get; }

        /// <summary>
        /// Gets the foundry configuration.
        /// </summary>
        public FoundryConfiguration Configuration => _configuration;

        /// <inheritdoc />
        public ConcurrentDictionary<string, object?> Properties { get; }

        /// <inheritdoc />
        public IWorkflow? CurrentWorkflow => _currentWorkflow;

        /// <inheritdoc />
        public IWorkflowForgeLogger Logger { get; }

        /// <inheritdoc />
        public IServiceProvider? ServiceProvider { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowFoundry"/> class.
        /// </summary>
        /// <param name="executionId">The unique identifier for this foundry execution instance.</param>
        /// <param name="properties">The foundry properties container.</param>
        /// <param name="configuration">The foundry configuration.</param>
        /// <param name="currentWorkflow">Optional initial workflow to associate with this foundry.</param>
        /// <param name="timeProvider">The time provider to use for timestamps.</param>
        /// <exception cref="ArgumentNullException">Thrown when properties is null.</exception>
        public WorkflowFoundry(
            Guid executionId,
            ConcurrentDictionary<string, object?> properties,
            FoundryConfiguration? configuration = null,
            IWorkflow? currentWorkflow = null,
            ISystemTimeProvider? timeProvider = null)
        {
            ExecutionId = executionId;
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
            _configuration = configuration ?? FoundryConfiguration.Minimal();
            Logger = _configuration.Logger ?? NullLogger.Instance;
            ServiceProvider = _configuration.ServiceProvider;
            _currentWorkflow = currentWorkflow;
            _timeProvider = timeProvider ?? SystemTimeProvider.Instance;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowFoundry"/> class with explicit logger and service provider.
        /// </summary>
        /// <param name="executionId">The unique identifier for this foundry execution instance.</param>
        /// <param name="properties">The foundry properties container.</param>
        /// <param name="logger">The logger for this foundry.</param>
        /// <param name="serviceProvider">Optional service provider for dependency injection.</param>
        /// <param name="currentWorkflow">Optional initial workflow to associate with this foundry.</param>
        /// <param name="timeProvider">The time provider to use for timestamps.</param>
        public WorkflowFoundry(
            Guid executionId,
            ConcurrentDictionary<string, object?> properties,
            IWorkflowForgeLogger logger,
            IServiceProvider? serviceProvider = null,
            IWorkflow? currentWorkflow = null,
            ISystemTimeProvider? timeProvider = null)
            : this(executionId, properties, new FoundryConfiguration
            {
                Logger = logger,
                ServiceProvider = serviceProvider
            }, currentWorkflow, timeProvider)
        {
        }

        /// <inheritdoc />
        public void SetCurrentWorkflow(IWorkflow? workflow)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(WorkflowFoundry));
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
            if (_disposed) throw new ObjectDisposedException(nameof(WorkflowFoundry));
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            lock (_operations)
            {
                _operations.Add(operation);
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
            if (_disposed) throw new ObjectDisposedException(nameof(WorkflowFoundry));
            if (middleware == null) throw new ArgumentNullException(nameof(middleware));

            _middlewares.Add(middleware);
        }

        /// <summary>
        /// Adds multiple middleware components to the execution pipeline.
        /// </summary>
        /// <param name="middlewares">The middleware components to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when middlewares is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the foundry has been disposed.</exception>
        public void AddMiddlewares(IEnumerable<IWorkflowOperationMiddleware> middlewares)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(WorkflowFoundry));
            if (middlewares == null) throw new ArgumentNullException(nameof(middlewares));

            _middlewares.AddRange(middlewares);
        }

        /// <summary>
        /// Removes middleware from the execution pipeline.
        /// </summary>
        /// <param name="middleware">The middleware to remove.</param>
        /// <returns>True if the middleware was found and removed; otherwise, false.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the foundry has been disposed.</exception>
        public bool RemoveMiddleware(IWorkflowOperationMiddleware middleware)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(WorkflowFoundry));
            return _middlewares.Remove(middleware);
        }

        /// <summary>
        /// Gets the number of middleware components in the pipeline.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the foundry has been disposed.</exception>
        public int MiddlewareCount
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException(nameof(WorkflowFoundry));
                return _middlewares.Count;
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
            if (_disposed) throw new ObjectDisposedException(nameof(WorkflowFoundry));

            IWorkflowOperation[] operationsSnapshot;
            lock (_operations)
            {
                operationsSnapshot = _operations.ToArray();
            }

            foreach (var operation in operationsSnapshot)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var operationStartTime = _timeProvider.UtcNow;

                try
                {
                    // 🎯 FIRE: OperationStarted event
                    OperationStarted?.Invoke(this, new OperationStartedEventArgs(operation, this, null));

                    var result = await ExecuteOperationWithMiddleware(operation, null, cancellationToken).ConfigureAwait(false);

                    var operationDuration = _timeProvider.UtcNow - operationStartTime;

                    // 🎯 FIRE: OperationCompleted event
                    OperationCompleted?.Invoke(this, new OperationCompletedEventArgs(
                        operation,
                        this,
                        null,
                        result,
                        TimeSpan.FromMilliseconds(operationDuration.TotalMilliseconds)));
                }
                catch (Exception ex)
                {
                    var operationDuration = _timeProvider.UtcNow - operationStartTime;

                    // 🎯 FIRE: OperationFailed event
                    OperationFailed?.Invoke(this, new OperationFailedEventArgs(
                        operation,
                        this,
                        null,
                        ex,
                        TimeSpan.FromMilliseconds(operationDuration.TotalMilliseconds)));

                    throw;
                }
            }
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
            if (_middlewares.Count == 0)
            {
                // No middleware, execute operation directly
                return await operation.ForgeAsync(inputData, this, cancellationToken).ConfigureAwait(false);
            }

            // ==================================================================================
            // MIDDLEWARE EXECUTION: Russian Doll Pattern (Industry Standard)
            // ==================================================================================
            //
            // Middleware wraps in REVERSE order of addition to create a "Russian Doll" effect.
            // This is intentional and correct - it's how ASP.NET, Express.js, and other
            // frameworks implement middleware pipelines.
            //
            // EXAMPLE:
            // --------
            // If you add middleware in this order:
            //   foundry.AddMiddleware(timingMiddleware);        // Added 1st
            //   foundry.AddMiddleware(errorHandlingMiddleware); // Added 2nd
            //   foundry.AddMiddleware(retryMiddleware);         // Added 3rd
            //
            // Execution flow becomes:
            //   Timing.Start
            //     → ErrorHandling.Start
            //       → Retry.Start
            //         → OPERATION EXECUTES
            //       ← Retry.End
            //     ← ErrorHandling.End
            //   ← Timing.End
            //
            // REVERSE iteration builds the chain from inside-out:
            //   1. Start with: next = operation.ForgeAsync
            //   2. Wrap with retryMiddleware:    next = () => retry.Execute(next)
            //   3. Wrap with errorMiddleware:    next = () => error.Execute(next)
            //   4. Wrap with timingMiddleware:   next = () => timing.Execute(next)
            //
            // Final execution: timing → error → retry → operation → retry → error → timing
            //
            // BEST PRACTICES:
            // ---------------
            // Add middleware in order of desired outer-to-inner wrapping:
            //   1. Observability (Timing, Logging) first     - measures everything
            //   2. Error Handling second                     - catches all errors
            //   3. Retry/Resilience last                     - wraps just the operation
            //
            // This ensures timing includes error handling time, and error handlers
            // can catch retry failures, etc.
            //
            // TECHNICAL DETAILS:
            // ------------------
            // We iterate backwards (_middlewares.Count - 1 down to 0) because:
            // - Last middleware added should wrap first (innermost)
            // - Each iteration wraps the previous 'next' delegate
            // - Results in correct execution order: first added → first executed
            // ==================================================================================

            Func<Task<object?>> next = () => operation.ForgeAsync(inputData, this, cancellationToken);

            for (int i = _middlewares.Count - 1; i >= 0; i--)
            {
                var middleware = _middlewares[i];
                var currentNext = next;
                next = () => middleware.ExecuteAsync(operation, this, inputData, currentNext, cancellationToken);
            }

            return await next().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;
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
                    catch
                    {
                        // Best effort disposal
                    }
                }
                _operations.Clear();
            }
            // Dispose properties
            Properties.Clear();
            GC.SuppressFinalize(this);
        }
    }
}