using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Middleware
{
    /// <summary>
    /// Middleware that enforces timeouts on entire workflow execution.
    /// Timeout can be configured globally (constructor) or per-workflow (foundry properties).
    /// </summary>
    /// <remarks>
    /// <para><strong>Timeout Configuration:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Global Default</strong>: Set via constructor parameter, applies to all workflows</description></item>
    /// <item><description><strong>Per-Workflow Override</strong>: Set via foundry.Properties["Workflow.Timeout"], overrides global default</description></item>
    /// <item><description><strong>No Timeout</strong>: TimeSpan.Zero disables timeout enforcement</description></item>
    /// </list>
    ///
    /// <para><strong>Usage Example:</strong></para>
    /// <code>
    /// // Global 5-minute timeout for all workflows
    /// smith.AddWorkflowMiddleware(new WorkflowTimeoutMiddleware(
    ///     TimeSpan.FromMinutes(5), logger));
    ///
    /// // Override for specific workflow
    /// var foundry = smith.CreateFoundry();
    /// foundry.Properties["Workflow.Timeout"] = TimeSpan.FromMinutes(10);
    /// await smith.ForgeAsync(workflow, foundry);
    /// </code>
    /// </remarks>
    public sealed class WorkflowTimeoutMiddleware : IWorkflowMiddleware
    {
        private readonly TimeSpan _defaultTimeout;
        private readonly IWorkflowForgeLogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowTimeoutMiddleware"/> class.
        /// </summary>
        /// <param name="defaultTimeout">
        /// Default timeout for workflow execution. TimeSpan.Zero = no timeout (default).
        /// Can be overridden per-workflow via foundry.Properties["Workflow.Timeout"].
        /// </param>
        /// <param name="logger">Logger for timeout events.</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
        /// <exception cref="ArgumentException">Thrown when defaultTimeout is negative.</exception>
        public WorkflowTimeoutMiddleware(TimeSpan defaultTimeout, IWorkflowForgeLogger logger)
        {
            if (defaultTimeout < TimeSpan.Zero)
            {
                throw new ArgumentException("Timeout cannot be negative. Use TimeSpan.Zero for no timeout.", nameof(defaultTimeout));
            }

            _defaultTimeout = defaultTimeout;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initializes a new instance with no default timeout (must be set per-workflow).
        /// </summary>
        /// <param name="logger">Logger for timeout events.</param>
        public WorkflowTimeoutMiddleware(IWorkflowForgeLogger logger)
            : this(TimeSpan.Zero, logger)
        {
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(
            IWorkflow workflow,
            IWorkflowFoundry foundry,
            Func<Task> next,
            CancellationToken cancellationToken = default)
        {
            if (workflow == null) throw new ArgumentNullException(nameof(workflow));
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (next == null) throw new ArgumentNullException(nameof(next));

            // Check if workflow has custom timeout
            TimeSpan timeout = _defaultTimeout;
            if (foundry.Properties.TryGetValue("Workflow.Timeout", out var customTimeout)
                && customTimeout is TimeSpan ts)
            {
                timeout = ts;
            }

            // TimeSpan.Zero = no timeout enforcement
            if (timeout == TimeSpan.Zero)
            {
                _logger.LogDebug($"Workflow '{workflow.Name}' executing without timeout");
                await next().ConfigureAwait(false);
                return;
            }

            _logger.LogDebug($"Workflow '{workflow.Name}' executing with {timeout.TotalSeconds}s timeout");

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var executionTask = next();
            var timeoutTask = Task.Delay(timeout, cancellationToken);

            var completedTask = await Task.WhenAny(executionTask, timeoutTask).ConfigureAwait(false);
            if (completedTask == timeoutTask)
            {
                timeoutCts.Cancel();
                _ = executionTask.ContinueWith(
                    t => _ = t.Exception,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                var errorMessage = $"Workflow '{workflow.Name}' execution exceeded the configured timeout of {timeout.TotalSeconds} seconds.";
                _logger.LogError(errorMessage);

                foundry.Properties["Workflow.TimedOut"] = true;
                foundry.Properties["Workflow.TimeoutDuration"] = timeout;

                throw new TimeoutException(errorMessage);
            }

            await executionTask.ConfigureAwait(false);
            _logger.LogDebug($"Workflow '{workflow.Name}' completed within timeout ({timeout.TotalSeconds}s)");
        }
    }
}





