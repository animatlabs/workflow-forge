using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    /// Main orchestration smith that forges workflow operations in sequence.
    /// Supports dependency injection, compensation logic, and comprehensive error handling.
    /// In the WorkflowForge metaphor, the smith is the skilled craftsman who coordinates the forging process.
    ///
    /// Supports both simple pattern (smith manages foundry) and advanced pattern (reusable foundry).
    /// </summary>
    internal sealed class WorkflowSmith : IWorkflowSmith
    {
        private readonly IWorkflowForgeLogger _logger;
        private readonly IServiceProvider? _serviceProvider;
        private readonly WorkflowForgeOptions _options;
        private readonly ISystemTimeProvider _timeProvider;
        private readonly SemaphoreSlim? _concurrencyLimiter;
        private readonly List<IWorkflowMiddleware> _workflowMiddlewares = new();
        private volatile bool _disposed;

        // ==================================================================================
        // WORKFLOW + COMPENSATION LIFECYCLE EVENTS
        // ==================================================================================
        public event EventHandler<WorkflowStartedEventArgs>? WorkflowStarted;

        public event EventHandler<WorkflowCompletedEventArgs>? WorkflowCompleted;

        public event EventHandler<WorkflowFailedEventArgs>? WorkflowFailed;

        public event EventHandler<CompensationTriggeredEventArgs>? CompensationTriggered;

        public event EventHandler<CompensationCompletedEventArgs>? CompensationCompleted;

        public event EventHandler<OperationRestoreStartedEventArgs>? OperationRestoreStarted;

        public event EventHandler<OperationRestoreCompletedEventArgs>? OperationRestoreCompleted;

        public event EventHandler<OperationRestoreFailedEventArgs>? OperationRestoreFailed;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowSmith"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for workflow forging events.</param>
        /// <param name="serviceProvider">Optional service provider for dependency injection.</param>
        /// <param name="options">Optional workflow forge options.</param>
        /// <param name="timeProvider">The time provider to use for timestamps.</param>
        public WorkflowSmith(
            IWorkflowForgeLogger logger,
            IServiceProvider? serviceProvider = null,
            WorkflowForgeOptions? options = null,
            ISystemTimeProvider? timeProvider = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider;
            _timeProvider = timeProvider ?? SystemTimeProvider.Instance;
            _options = options ?? new WorkflowForgeOptions();

            // Validate options
            var validationErrors = _options.Validate();
            if (validationErrors.Count > 0)
            {
                var errorMessage = $"Invalid WorkflowForge options: {string.Join("; ", validationErrors)}";
                _logger.LogError(errorMessage);
                throw new ArgumentException(errorMessage, nameof(options));
            }

            // Initialize concurrency limiter if MaxConcurrentWorkflows is configured (> 0)
            if (_options.MaxConcurrentWorkflows > 0)
            {
                _concurrencyLimiter = new SemaphoreSlim(
                    _options.MaxConcurrentWorkflows,
                    _options.MaxConcurrentWorkflows);

                _logger.LogInformation(
                    $"WorkflowSmith initialized with MaxConcurrentWorkflows={_options.MaxConcurrentWorkflows}");
            }
            else
            {
                _concurrencyLimiter = null; // Unlimited concurrency
            }
        }

        /// <summary>
        /// Initializes a new WorkflowSmith with minimal dependencies (for standalone scenarios).
        /// </summary>
        public WorkflowSmith() : this(NullLogger.Instance)
        {
        }

        /// <inheritdoc />
        public async Task ForgeAsync(IWorkflow workflow, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (workflow == null) throw new ArgumentNullException(nameof(workflow));

            // Simple pattern: create foundry internally
            using var foundry = CreateFoundryFor(workflow);
            await ForgeAsync(workflow, foundry, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task ForgeAsync(
            IWorkflow workflow,
            ConcurrentDictionary<string, object?> data,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (workflow == null) throw new ArgumentNullException(nameof(workflow));
            if (data == null) throw new ArgumentNullException(nameof(data));

            // Create foundry with provided data
            using var foundry = CreateFoundryWithData(data);
            foundry.SetCurrentWorkflow(workflow);
            await ForgeAsync(workflow, foundry, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task ForgeAsync(
            IWorkflow workflow,
            IWorkflowFoundry foundry,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (workflow == null) throw new ArgumentNullException(nameof(workflow));
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));

            // Apply concurrency throttling if configured
            if (_concurrencyLimiter != null)
            {
                await _concurrencyLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    await ExecuteWorkflowAsync(workflow, foundry, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    _concurrencyLimiter.Release();
                }
            }
            else
            {
                // No throttling - execute directly
                await ExecuteWorkflowAsync(workflow, foundry, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Internal workflow execution logic (extracted for semaphore wrapping).
        /// Executes workflow through workflow-level middleware pipeline.
        /// </summary>
        private async Task ExecuteWorkflowAsync(
            IWorkflow workflow,
            IWorkflowFoundry foundry,
            CancellationToken cancellationToken)
        {
            // Advanced pattern: use provided foundry and set current workflow
            foundry.SetCurrentWorkflow(workflow);

            // Build workflow middleware pipeline (Russian Doll pattern)
            Func<Task> workflowExecution = async () =>
            {
                // Create workflow scope using helper
                using var workflowScope = _logger.CreateWorkflowScope(workflow, foundry);

                var startTime = _timeProvider.UtcNow;
                _logger.LogInformation(WorkflowLogMessageConstants.WorkflowExecutionStarted);

                // FIRE: WorkflowStarted event
                WorkflowStarted?.Invoke(this, new WorkflowStartedEventArgs(foundry, _timeProvider.UtcNow));

                try
                {
                    // Route execution through foundry pipeline so operation middlewares are applied
                    if (foundry is WorkflowFoundry workflowFoundry)
                    {
                        workflowFoundry.SetOperations(workflow.Operations);
                    }
                    else
                    {
                        foundry.WithOperations(workflow.Operations);
                    }
                    await foundry.ForgeAsync(cancellationToken).ConfigureAwait(false);

                    // Log workflow completion
                    _logger.LogInformation(WorkflowLogMessageConstants.WorkflowExecutionCompleted);

                    // FIRE: WorkflowCompleted event
                    var duration = _timeProvider.UtcNow - startTime;
                    var finalProperties = new Dictionary<string, object?>(foundry.Properties);
                    WorkflowCompleted?.Invoke(this, new WorkflowCompletedEventArgs(
                        foundry,
                        _timeProvider.UtcNow,
                        finalProperties,
                        duration));
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning(WorkflowLogMessageConstants.WorkflowExecutionCancelled);
                    throw;
                }
                catch (Exception ex)
                {
                    var errorProperties = _logger.CreateErrorProperties(ex, "WorkflowExecution");
                    _logger.LogError(errorProperties, ex, WorkflowLogMessageConstants.WorkflowExecutionFailed);

                    // FIRE: WorkflowFailed event
                    var duration = _timeProvider.UtcNow - startTime;
                    var failedOperationName = foundry.Properties.TryGetValue("Operation.LastFailedName", out var failedNameValue)
                        ? failedNameValue?.ToString() ?? "Unknown"
                        : "Unknown";
                    WorkflowFailed?.Invoke(this, new WorkflowFailedEventArgs(
                        foundry,
                        _timeProvider.UtcNow,
                        ex,
                        failedOperationName,
                        duration));

                    if (workflow.SupportsRestore)
                    {
                        var lastForgedIndex = -1;
                        if (foundry.Properties.TryGetValue("Operation.LastCompletedIndex", out var lastCompletedValue)
                            && lastCompletedValue is int completedIndex)
                        {
                            lastForgedIndex = completedIndex;
                        }

                        var compensationErrors = await CompensateForgedOperationsAsync(
                            workflow.Operations,
                            lastForgedIndex,
                            foundry,
                            cancellationToken).ConfigureAwait(false);

                        if (compensationErrors.Count > 0 && (foundry.Options.FailFastCompensation || foundry.Options.ThrowOnCompensationError))
                        {
                            throw new AggregateException(
                                "Workflow failed and compensation encountered errors.",
                                PrependOriginalException(ex, compensationErrors));
                        }
                    }

                    throw;
                }
            };

            // Wrap with workflow middlewares (Russian Doll pattern - reverse order)
            for (int i = _workflowMiddlewares.Count - 1; i >= 0; i--)
            {
                var middleware = _workflowMiddlewares[i];
                var currentNext = workflowExecution;
                workflowExecution = () => middleware.ExecuteAsync(workflow, foundry, currentNext, cancellationToken);
            }

            // Execute workflow with middleware pipeline
            await workflowExecution().ConfigureAwait(false);
        }

        /// <inheritdoc />
        /// <remarks>
        /// <para><strong>Isolation:</strong> Creates a NEW ConcurrentDictionary for Properties,
        /// ensuring complete isolation between foundry instances. Parallel foundries never share state.</para>
        /// <para><strong>Ambient Services:</strong> The foundry inherits Logger/ServiceProvider from this WorkflowSmith
        /// enabling the Ambient Context pattern where services flow: Smith → Foundry → Operations.</para>
        /// </remarks>
        public IWorkflowFoundry CreateFoundry(IWorkflowForgeLogger? logger = null, IServiceProvider? serviceProvider = null)
        {
            ThrowIfDisposed();

            // NEW ConcurrentDictionary per foundry = complete isolation
            var properties = new ConcurrentDictionary<string, object?>();

            return new WorkflowFoundry(
                Guid.NewGuid(),                                    // Unique execution ID
                properties,                                         // Isolated properties
                logger ?? _logger,                                  // Logger (override or use smith's)
                serviceProvider ?? _serviceProvider,                // ServiceProvider (override or use smith's)
                options: _options.CloneTyped());                    // Clone options to avoid mutation
        }

        /// <inheritdoc />
        /// <remarks>
        /// <para><strong>Isolation:</strong> Creates a NEW ConcurrentDictionary for Properties,
        /// ensuring complete isolation. Each foundry has its own isolated state.</para>
        /// </remarks>
        public IWorkflowFoundry CreateFoundryFor(IWorkflow workflow, IWorkflowForgeLogger? logger = null, IServiceProvider? serviceProvider = null)
        {
            ThrowIfDisposed();
            if (workflow == null) throw new ArgumentNullException(nameof(workflow));

            // NEW ConcurrentDictionary per foundry = complete isolation
            var properties = new ConcurrentDictionary<string, object?>();

            return new WorkflowFoundry(
                Guid.NewGuid(),                                    // Unique execution ID
                properties,                                         // Isolated properties
                logger ?? _logger,                                  // Logger (override or use smith's)
                serviceProvider ?? _serviceProvider,                // ServiceProvider (override or use smith's)
                workflow,
                options: _options.CloneTyped());                    // Pre-associate + options
        }

        /// <inheritdoc />
        public IWorkflowFoundry CreateFoundryWithData(ConcurrentDictionary<string, object?> data, IWorkflowForgeLogger? logger = null, IServiceProvider? serviceProvider = null)
        {
            ThrowIfDisposed();
            if (data == null) throw new ArgumentNullException(nameof(data));

            return new WorkflowFoundry(
                Guid.NewGuid(),
                data,
                logger ?? _logger,
                serviceProvider ?? _serviceProvider,
                options: _options.CloneTyped());
        }

        /// <summary>
        /// Compensates (rolls back) previously forged operations in reverse order.
        /// </summary>
        /// <param name="operations">The list of all operations.</param>
        /// <param name="lastForgedIndex">The index of the last successfully forged operation.</param>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task<IReadOnlyList<Exception>> CompensateForgedOperationsAsync(
            IReadOnlyList<IWorkflowOperation> operations,
            int lastForgedIndex,
            IWorkflowFoundry foundry,
            CancellationToken cancellationToken)
        {
            if (lastForgedIndex < 0) return Array.Empty<Exception>();

            var failFast = GetFailFastCompensation(foundry);
            var errors = new List<Exception>();

            // Create compensation scope using helper
            using var compensationScope = _logger.CreateCompensationScope(lastForgedIndex + 1);

            _logger.LogInformation(WorkflowLogMessageConstants.CompensationProcessStarted);

            // FIRE: CompensationTriggered event
            CompensationTriggered?.Invoke(this, new CompensationTriggeredEventArgs(
                foundry,
                _timeProvider.UtcNow,
                "Operation failed, initiating compensation",
                lastForgedIndex < operations.Count ? operations[lastForgedIndex].Name : "Unknown",
                null));

            int successCount = 0;
            int failureCount = 0;
            var compensationStartTime = _timeProvider.UtcNow;

            // Compensate in reverse order
            for (int i = lastForgedIndex; i >= 0; i--)
            {
                var operation = operations[i];
                if (!operation.SupportsRestore)
                {
                    var skipProperties = new Dictionary<string, string>
                    {
                        [PropertyNameConstants.ExecutionId] = operation.Id.ToString(),
                        [PropertyNameConstants.ExecutionName] = operation.Name,
                        [PropertyNameConstants.ExecutionType] = operation.GetType().Name
                    };

                    _logger.LogDebug(skipProperties, WorkflowLogMessageConstants.CompensationActionSkipped);
                    continue;
                }

                var operationProperties = new Dictionary<string, string>
                {
                    [PropertyNameConstants.ExecutionId] = operation.Id.ToString(),
                    [PropertyNameConstants.ExecutionName] = operation.Name,
                    [PropertyNameConstants.ExecutionType] = operation.GetType().Name
                };

                using var operationScope = _logger.BeginScope("CompensationAction", operationProperties);

                var restoreStartTime = _timeProvider.UtcNow;

                try
                {
                    _logger.LogDebug(WorkflowLogMessageConstants.CompensationActionStarted);

                    // FIRE: OperationRestoreStarted event
                    OperationRestoreStarted?.Invoke(this, new OperationRestoreStartedEventArgs(operation, foundry));

                    object? outputData = null;
                    var outputKey = $"Operation.{operation.Id}.Output";
                    if (foundry.Properties.TryGetValue(outputKey, out var storedOutput))
                    {
                        outputData = storedOutput;
                    }

                    await operation.RestoreAsync(outputData, foundry, cancellationToken).ConfigureAwait(false);

                    _logger.LogDebug(WorkflowLogMessageConstants.CompensationActionCompleted);

                    var restoreDuration = _timeProvider.UtcNow - restoreStartTime;

                    // FIRE: OperationRestoreCompleted event
                    OperationRestoreCompleted?.Invoke(this, new OperationRestoreCompletedEventArgs(
                        operation,
                        foundry,
                        restoreDuration));

                    successCount++;
                }
                catch (Exception compensationEx)
                {
                    var errorProperties = _logger.CreateErrorProperties(compensationEx, "CompensationFailure");

                    _logger.LogError(errorProperties, compensationEx, WorkflowLogMessageConstants.CompensationActionFailed);

                    var restoreDuration = _timeProvider.UtcNow - restoreStartTime;

                    // FIRE: OperationRestoreFailed event
                    OperationRestoreFailed?.Invoke(this, new OperationRestoreFailedEventArgs(
                        operation,
                        foundry,
                        compensationEx,
                        restoreDuration));

                    failureCount++;
                    errors.Add(compensationEx);

                    if (failFast)
                    {
                        break;
                    }
                }
            }

            var completionProperties = _logger.CreateCompensationResultProperties(successCount, failureCount);

            _logger.LogInformation(completionProperties, WorkflowLogMessageConstants.CompensationProcessCompleted);

            var totalCompensationDuration = _timeProvider.UtcNow - compensationStartTime;

            // FIRE: CompensationCompleted event
            CompensationCompleted?.Invoke(this, new CompensationCompletedEventArgs(
                foundry,
                _timeProvider.UtcNow,
                successCount,
                failureCount,
                TimeSpan.FromMilliseconds(totalCompensationDuration.TotalMilliseconds)));

            return errors;
        }

        private static IEnumerable<Exception> PrependOriginalException(Exception original, IReadOnlyList<Exception> compensationErrors)
        {
            yield return original;
            foreach (var error in compensationErrors)
            {
                yield return error;
            }
        }

        private static bool GetFailFastCompensation(IWorkflowFoundry foundry)
        {
            return foundry.Options.FailFastCompensation;
        }

        private IWorkflowFoundry CreateFoundryFor(IWorkflow workflow)
        {
            var properties = new ConcurrentDictionary<string, object?>();

            return new WorkflowFoundry(
                Guid.NewGuid(),
                properties,
                _logger,
                _serviceProvider,
                workflow,
                options: _options.CloneTyped());
        }

        private IWorkflowFoundry CreateFoundryWithData(ConcurrentDictionary<string, object?> properties)
        {
            return new WorkflowFoundry(
                Guid.NewGuid(),
                properties,
                _logger,
                _serviceProvider,
                options: _options.CloneTyped());
        }

        /// <inheritdoc />
        public void AddWorkflowMiddleware(IWorkflowMiddleware middleware)
        {
            ThrowIfDisposed();
            if (middleware == null) throw new ArgumentNullException(nameof(middleware));
            _workflowMiddlewares.Add(middleware);
        }

        /// <summary>
        /// Releases all resources used by the WorkflowSmith.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _logger.LogTrace("WorkflowSmith disposal initiated");

            // Dispose concurrency limiter if it was created
            _concurrencyLimiter?.Dispose();

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(WorkflowSmith));
        }
    }
}