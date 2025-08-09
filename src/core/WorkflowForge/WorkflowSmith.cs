using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Configurations;
using WorkflowForge.Constants;
using WorkflowForge.Extensions;
using WorkflowForge.Loggers;

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
        private readonly FoundryConfiguration _configuration;
        private volatile bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowSmith"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for workflow forging events.</param>
        /// <param name="serviceProvider">Optional service provider for dependency injection.</param>
        /// <param name="configuration">The foundry configuration.</param>
        public WorkflowSmith(IWorkflowForgeLogger logger, IServiceProvider? serviceProvider = null, FoundryConfiguration? configuration = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider;
            _configuration = configuration ?? FoundryConfiguration.Minimal();
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

            // Advanced pattern: use provided foundry and set current workflow
            foundry.SetCurrentWorkflow(workflow);

            // Create workflow scope using helper
            using var workflowScope = _logger.CreateWorkflowScope(workflow, foundry);

            _logger.LogInformation(WorkflowLogMessageConstants.WorkflowExecutionStarted);

            try
            {
                // Route execution through foundry pipeline so middlewares (e.g., persistence, retry, logging) are applied
                foundry.WithOperations(workflow.Operations);
                await foundry.ForgeAsync(cancellationToken).ConfigureAwait(false);

                // Log workflow completion
                _logger.LogInformation(WorkflowLogMessageConstants.WorkflowExecutionCompleted);
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
                throw;
            }
        }

        /// <inheritdoc />
        public IWorkflowFoundry CreateFoundry(IWorkflowForgeLogger? logger = null, IServiceProvider? serviceProvider = null)
        {
            ThrowIfDisposed();
            return new WorkflowFoundry(
                Guid.NewGuid(),
                new ConcurrentDictionary<string, object?>(),
                new FoundryConfiguration
                {
                    Logger = logger ?? _logger,
                    ServiceProvider = serviceProvider ?? _serviceProvider
                });
        }

        /// <inheritdoc />
        public IWorkflowFoundry CreateFoundryFor(IWorkflow workflow, IWorkflowForgeLogger? logger = null, IServiceProvider? serviceProvider = null)
        {
            ThrowIfDisposed();
            if (workflow == null) throw new ArgumentNullException(nameof(workflow));

            return new WorkflowFoundry(
                Guid.NewGuid(),
                new ConcurrentDictionary<string, object?>(),
                new FoundryConfiguration
                {
                    Logger = logger ?? _logger,
                    ServiceProvider = serviceProvider ?? _serviceProvider
                },
                workflow);
        }

        /// <inheritdoc />
        public IWorkflowFoundry CreateFoundryWithData(ConcurrentDictionary<string, object?> data, IWorkflowForgeLogger? logger = null, IServiceProvider? serviceProvider = null)
        {
            ThrowIfDisposed();
            if (data == null) throw new ArgumentNullException(nameof(data));

            return new WorkflowFoundry(
                Guid.NewGuid(),
                data,
                new FoundryConfiguration
                {
                    Logger = logger ?? _logger,
                    ServiceProvider = serviceProvider ?? _serviceProvider
                });
        }

        /// <summary>
        /// Compensates (rolls back) previously forged operations in reverse order.
        /// </summary>
        /// <param name="operations">The list of all operations.</param>
        /// <param name="lastForgedIndex">The index of the last successfully forged operation.</param>
        /// <param name="foundry">The workflow foundry.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task CompensateForgedOperationsAsync(
            IReadOnlyList<IWorkflowOperation> operations,
            int lastForgedIndex,
            IWorkflowFoundry foundry,
            CancellationToken cancellationToken)
        {
            if (lastForgedIndex < 0) return;

            // Create compensation scope using helper
            using var compensationScope = _logger.CreateCompensationScope(lastForgedIndex + 1);

            _logger.LogInformation(WorkflowLogMessageConstants.CompensationProcessStarted);

            int successCount = 0;
            int failureCount = 0;

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

                try
                {
                    _logger.LogDebug(WorkflowLogMessageConstants.CompensationActionStarted);

                    await operation.RestoreAsync(null, foundry, cancellationToken).ConfigureAwait(false);

                    _logger.LogDebug(WorkflowLogMessageConstants.CompensationActionCompleted);
                    successCount++;
                }
                catch (Exception compensationEx)
                {
                    var errorProperties = _logger.CreateErrorProperties(compensationEx, "CompensationFailure");

                    _logger.LogError(errorProperties, compensationEx, WorkflowLogMessageConstants.CompensationActionFailed);
                    failureCount++;
                    // Continue with other compensations even if one fails
                }
            }

            var completionProperties = _logger.CreateCompensationResultProperties(successCount, failureCount);

            _logger.LogInformation(completionProperties, WorkflowLogMessageConstants.CompensationProcessCompleted);
        }

        private IWorkflowFoundry CreateFoundryFor(IWorkflow workflow)
        {
            return new WorkflowFoundry(
                Guid.NewGuid(),
                new ConcurrentDictionary<string, object?>(),
                _configuration,
                workflow);
        }

        private IWorkflowFoundry CreateFoundryWithData(ConcurrentDictionary<string, object?> properties)
        {
            return new WorkflowFoundry(
                Guid.NewGuid(),
                properties,
                _configuration);
        }

        /// <summary>
        /// Releases all resources used by the WorkflowSmith.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _logger.LogTrace("WorkflowSmith disposal initiated");
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(WorkflowSmith));
        }
    }
}