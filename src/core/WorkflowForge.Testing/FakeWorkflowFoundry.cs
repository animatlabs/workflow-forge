using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Events;
using WorkflowForge.Options;

namespace WorkflowForge.Testing
{
    /// <summary>
    /// A fake implementation of <see cref="IWorkflowFoundry"/> for unit testing workflow operations.
    /// Provides tracking for assertions and configurable behavior without full workflow infrastructure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this class to test individual workflow operations in isolation. It implements
    /// the full <see cref="IWorkflowFoundry"/> interface with test-friendly behavior.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// var foundry = new FakeWorkflowFoundry();
    /// var operation = new MyCustomOperation();
    /// 
    /// var result = await operation.ForgeAsync(inputData, foundry, CancellationToken.None);
    /// 
    /// Assert.Contains(operation, foundry.ExecutedOperations);
    /// Assert.Equal(expectedValue, foundry.Properties["key"]);
    /// </code>
    /// </para>
    /// </remarks>
    public class FakeWorkflowFoundry : IWorkflowFoundry
    {
        private readonly List<IWorkflowOperation> _operations = new List<IWorkflowOperation>();
        private readonly List<IWorkflowOperationMiddleware> _middlewares = new List<IWorkflowOperationMiddleware>();
        private readonly List<IWorkflowOperation> _executedOperations = new List<IWorkflowOperation>();
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="FakeWorkflowFoundry"/> class.
        /// </summary>
        public FakeWorkflowFoundry()
        {
            ExecutionId = Guid.NewGuid();
            Properties = new ConcurrentDictionary<string, object?>();
            Logger = TestNullLogger.Instance;
            Options = new WorkflowForgeOptions();
        }

        /// <inheritdoc />
        public Guid ExecutionId { get; set; }

        /// <inheritdoc />
        public ConcurrentDictionary<string, object?> Properties { get; }

        /// <inheritdoc />
        public IWorkflow? CurrentWorkflow { get; private set; }

        /// <inheritdoc />
        public IWorkflowForgeLogger Logger { get; set; }

        /// <inheritdoc />
        public WorkflowForgeOptions Options { get; set; }

        /// <inheritdoc />
        public IServiceProvider? ServiceProvider { get; set; }

        /// <inheritdoc />
        public bool IsFrozen { get; private set; }

        /// <summary>
        /// Gets the list of operations that have been added to this foundry.
        /// </summary>
        public IReadOnlyList<IWorkflowOperation> Operations => _operations.AsReadOnly();

        /// <summary>
        /// Gets the list of middleware that have been added to this foundry.
        /// </summary>
        public IReadOnlyList<IWorkflowOperationMiddleware> Middlewares => _middlewares.AsReadOnly();

        /// <summary>
        /// Gets the list of operations that were executed during <see cref="ForgeAsync"/>.
        /// Use this for assertions in tests.
        /// </summary>
        public IReadOnlyList<IWorkflowOperation> ExecutedOperations => _executedOperations.AsReadOnly();

        /// <inheritdoc />
        public event EventHandler<OperationStartedEventArgs>? OperationStarted;

        /// <inheritdoc />
        public event EventHandler<OperationCompletedEventArgs>? OperationCompleted;

        /// <inheritdoc />
        public event EventHandler<OperationFailedEventArgs>? OperationFailed;

        /// <inheritdoc />
        public void SetCurrentWorkflow(IWorkflow? workflow)
        {
            ThrowIfDisposed();
            CurrentWorkflow = workflow;
        }

        /// <inheritdoc />
        public void AddOperation(IWorkflowOperation operation)
        {
            ThrowIfDisposed();
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            _operations.Add(operation);
        }

        /// <inheritdoc />
        public void AddMiddleware(IWorkflowOperationMiddleware middleware)
        {
            ThrowIfDisposed();
            if (middleware == null) throw new ArgumentNullException(nameof(middleware));
            _middlewares.Add(middleware);
        }

        /// <inheritdoc />
        public void AddMiddlewares(IEnumerable<IWorkflowOperationMiddleware> middlewares)
        {
            ThrowIfDisposed();
            if (middlewares == null) throw new ArgumentNullException(nameof(middlewares));
            foreach (var middleware in middlewares)
            {
                AddMiddleware(middleware);
            }
        }

        /// <inheritdoc />
        public void ReplaceOperations(IEnumerable<IWorkflowOperation> operations)
        {
            ThrowIfDisposed();
            if (operations == null) throw new ArgumentNullException(nameof(operations));
            _operations.Clear();
            foreach (var operation in operations)
            {
                _operations.Add(operation);
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// This fake implementation executes all operations sequentially and tracks them
        /// in <see cref="ExecutedOperations"/> for assertions.
        /// </remarks>
        public async Task ForgeAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            IsFrozen = true;

            try
            {
                object? previousOutput = null;

                foreach (var operation in _operations)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var startTime = DateTime.UtcNow;
                    RaiseOperationStarted(operation, previousOutput);

                    try
                    {
                        previousOutput = await operation.ForgeAsync(previousOutput, this, cancellationToken)
                            .ConfigureAwait(false);
                        _executedOperations.Add(operation);
                        var duration = DateTime.UtcNow - startTime;
                        RaiseOperationCompleted(operation, previousOutput, previousOutput, duration);
                    }
                    catch (Exception ex) when (!(ex is OperationCanceledException))
                    {
                        var duration = DateTime.UtcNow - startTime;
                        RaiseOperationFailed(operation, previousOutput, ex, duration);
                        throw;
                    }
                }
            }
            finally
            {
                IsFrozen = false;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _disposed = true;
        }

        /// <summary>
        /// Clears all tracked state including executed operations, properties, and operations list.
        /// Call this to reset the foundry between test cases.
        /// </summary>
        public void Reset()
        {
            _executedOperations.Clear();
            _operations.Clear();
            _middlewares.Clear();
            Properties.Clear();
            CurrentWorkflow = null;
            IsFrozen = false;
        }

        /// <summary>
        /// Tracks an operation as executed. Useful when testing operations directly
        /// without going through <see cref="ForgeAsync"/>.
        /// </summary>
        /// <param name="operation">The operation to track as executed.</param>
        public void TrackExecution(IWorkflowOperation operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            _executedOperations.Add(operation);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(FakeWorkflowFoundry));
        }

        private void RaiseOperationStarted(IWorkflowOperation operation, object? inputData)
        {
            OperationStarted?.Invoke(this, new OperationStartedEventArgs(operation, this, inputData));
        }

        private void RaiseOperationCompleted(IWorkflowOperation operation, object? inputData, object? result, TimeSpan duration)
        {
            OperationCompleted?.Invoke(this, new OperationCompletedEventArgs(operation, this, inputData, result, duration));
        }

        private void RaiseOperationFailed(IWorkflowOperation operation, object? inputData, Exception exception, TimeSpan duration)
        {
            OperationFailed?.Invoke(this, new OperationFailedEventArgs(operation, this, inputData, exception, duration));
        }
    }

    /// <summary>
    /// A null logger implementation for testing that discards all log messages.
    /// </summary>
    public sealed class TestNullLogger : IWorkflowForgeLogger
    {
        /// <summary>
        /// Gets the singleton instance of the test null logger.
        /// </summary>
        public static readonly TestNullLogger Instance = new TestNullLogger();

        private TestNullLogger() { }

        /// <inheritdoc />
        public void LogTrace(string message, params object[] args) { }

        /// <inheritdoc />
        public void LogTrace(Exception exception, string message, params object[] args) { }

        /// <inheritdoc />
        public void LogTrace(IDictionary<string, string> properties, string message, params object[] args) { }

        /// <inheritdoc />
        public void LogTrace(IDictionary<string, string> properties, Exception exception, string message, params object[] args) { }

        /// <inheritdoc />
        public void LogDebug(string message, params object[] args) { }

        /// <inheritdoc />
        public void LogDebug(Exception exception, string message, params object[] args) { }

        /// <inheritdoc />
        public void LogDebug(IDictionary<string, string> properties, string message, params object[] args) { }

        /// <inheritdoc />
        public void LogDebug(IDictionary<string, string> properties, Exception exception, string message, params object[] args) { }

        /// <inheritdoc />
        public void LogInformation(string message, params object[] args) { }

        /// <inheritdoc />
        public void LogInformation(Exception exception, string message, params object[] args) { }

        /// <inheritdoc />
        public void LogInformation(IDictionary<string, string> properties, string message, params object[] args) { }

        /// <inheritdoc />
        public void LogInformation(IDictionary<string, string> properties, Exception exception, string message, params object[] args) { }

        /// <inheritdoc />
        public void LogWarning(string message, params object[] args) { }

        /// <inheritdoc />
        public void LogWarning(Exception exception, string message, params object[] args) { }

        /// <inheritdoc />
        public void LogWarning(IDictionary<string, string> properties, string message, params object[] args) { }

        /// <inheritdoc />
        public void LogWarning(IDictionary<string, string> properties, Exception exception, string message, params object[] args) { }

        /// <inheritdoc />
        public void LogError(string message, params object[] args) { }

        /// <inheritdoc />
        public void LogError(Exception exception, string message, params object[] args) { }

        /// <inheritdoc />
        public void LogError(IDictionary<string, string> properties, string message, params object[] args) { }

        /// <inheritdoc />
        public void LogError(IDictionary<string, string> properties, Exception exception, string message, params object[] args) { }

        /// <inheritdoc />
        public void LogCritical(string message, params object[] args) { }

        /// <inheritdoc />
        public void LogCritical(Exception exception, string message, params object[] args) { }

        /// <inheritdoc />
        public void LogCritical(IDictionary<string, string> properties, string message, params object[] args) { }

        /// <inheritdoc />
        public void LogCritical(IDictionary<string, string> properties, Exception exception, string message, params object[] args) { }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state, IDictionary<string, string>? properties = null)
            => new EmptyDisposable();

        private sealed class EmptyDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}
