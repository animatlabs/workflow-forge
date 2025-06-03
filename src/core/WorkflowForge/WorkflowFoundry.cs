using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Middleware;
using WorkflowForge.Loggers;

namespace WorkflowForge
{
    /// <summary>
    /// Core implementation of IWorkflowFoundry - the execution environment for workflows.
    /// Provides thread-safe workflow execution with foundry property management.
    /// Simplified, dependency-free implementation focused on core functionality.
    /// </summary>
    public sealed class WorkflowFoundry : IWorkflowFoundry
    {
        private readonly List<IWorkflowOperation> _operations = new();
        private readonly FoundryConfiguration _configuration;
        private readonly List<IWorkflowOperationMiddleware> _middlewares = new();
        private volatile bool _disposed;
        private IWorkflow? _currentWorkflow;

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
        /// <exception cref="ArgumentNullException">Thrown when properties is null.</exception>
        public WorkflowFoundry(
            Guid executionId,
            ConcurrentDictionary<string, object?> properties,
            FoundryConfiguration? configuration = null,
            IWorkflow? currentWorkflow = null)
        {
            ExecutionId = executionId;
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
            _configuration = configuration ?? FoundryConfiguration.Minimal();
            Logger = _configuration.Logger ?? NullLogger.Instance;
            ServiceProvider = _configuration.ServiceProvider;
            _currentWorkflow = currentWorkflow;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowFoundry"/> class with explicit logger and service provider.
        /// </summary>
        /// <param name="executionId">The unique identifier for this foundry execution instance.</param>
        /// <param name="properties">The foundry properties container.</param>
        /// <param name="logger">The logger for this foundry.</param>
        /// <param name="serviceProvider">Optional service provider for dependency injection.</param>
        /// <param name="currentWorkflow">Optional initial workflow to associate with this foundry.</param>
        public WorkflowFoundry(
            Guid executionId,
            ConcurrentDictionary<string, object?> properties,
            IWorkflowForgeLogger logger,
            IServiceProvider? serviceProvider = null,
            IWorkflow? currentWorkflow = null)
            : this(executionId, properties, new FoundryConfiguration 
            { 
                Logger = logger, 
                ServiceProvider = serviceProvider 
            }, currentWorkflow)
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
                await ExecuteOperationWithMiddleware(operation, null, cancellationToken).ConfigureAwait(false);
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

            // Build middleware chain from end to beginning
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
