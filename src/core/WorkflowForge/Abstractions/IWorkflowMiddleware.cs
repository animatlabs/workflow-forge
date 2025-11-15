using System;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowForge.Abstractions
{
    /// <summary>
    /// Middleware that executes at the workflow level (wraps entire workflow execution).
    /// Workflow middleware executes OUTSIDE the foundry pipeline and can affect the entire workflow.
    ///
    /// <para><strong>Execution Order:</strong></para>
    /// <para>
    /// Workflow middleware wraps the entire workflow execution, including all operations and operation-level middleware:
    /// <code>
    /// WorkflowMiddleware1 (outermost)
    ///   → WorkflowMiddleware2
    ///     → WORKFLOW START
    ///       → OperationMiddleware1 (per operation)
    ///         → OperationMiddleware2
    ///           → OPERATION EXECUTES
    ///       → Next operation...
    ///     ← WORKFLOW END
    ///   ← WorkflowMiddleware2
    /// ← WorkflowMiddleware1
    /// </code>
    /// </para>
    ///
    /// <para><strong>Common Use Cases:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Workflow Timeouts</strong>: Enforce maximum execution time for entire workflow</description></item>
    /// <item><description><strong>Workflow Logging</strong>: Log workflow start/completion with consolidated metrics</description></item>
    /// <item><description><strong>Workflow Authorization</strong>: Check permissions before allowing workflow execution</description></item>
    /// <item><description><strong>Workflow Caching</strong>: Cache workflow results based on inputs</description></item>
    /// <item><description><strong>Workflow Metrics</strong>: Collect aggregate metrics across all operations</description></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="IWorkflowOperationMiddleware"/> which executes per operation,
    /// workflow middleware executes once per workflow and can short-circuit the entire workflow execution.
    /// This is useful for cross-cutting concerns that apply to the workflow as a whole.
    /// </remarks>
    public interface IWorkflowMiddleware
    {
        /// <summary>
        /// Executes the workflow middleware logic.
        /// </summary>
        /// <param name="workflow">The workflow being executed.</param>
        /// <param name="foundry">The foundry context for the workflow execution.</param>
        /// <param name="next">
        /// Delegate to invoke the next middleware in the pipeline or the workflow execution itself.
        /// Call this to continue the workflow execution. Not calling this will short-circuit the workflow.
        /// </param>
        /// <param name="cancellationToken">Cancellation token for the workflow execution.</param>
        /// <returns>A task representing the workflow execution.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the workflow execution is cancelled.</exception>
        /// <remarks>
        /// <para>Middleware should:</para>
        /// <list type="bullet">
        /// <item><description>Call <paramref name="next"/>() to continue workflow execution</description></item>
        /// <item><description>Handle exceptions appropriately (log, transform, or rethrow)</description></item>
        /// <item><description>Clean up resources in finally blocks</description></item>
        /// <item><description>Respect cancellation tokens</description></item>
        /// </list>
        ///
        /// <para>Example implementation:</para>
        /// <code>
        /// public async Task ExecuteAsync(
        ///     IWorkflow workflow,
        ///     IWorkflowFoundry foundry,
        ///     Func&lt;Task&gt; next,
        ///     CancellationToken cancellationToken)
        /// {
        ///     // Pre-execution logic
        ///     Logger.LogInformation("Starting workflow: {Name}", workflow.Name);
        ///
        ///     try
        ///     {
        ///         // Execute workflow
        ///         await next().ConfigureAwait(false);
        ///
        ///         // Post-execution logic (success)
        ///         Logger.LogInformation("Workflow completed: {Name}", workflow.Name);
        ///     }
        ///     catch (Exception ex)
        ///     {
        ///         // Post-execution logic (failure)
        ///         Logger.LogError(ex, "Workflow failed: {Name}", workflow.Name);
        ///         throw;
        ///     }
        /// }
        /// </code>
        /// </remarks>
        Task ExecuteAsync(
            IWorkflow workflow,
            IWorkflowFoundry foundry,
            Func<Task> next,
            CancellationToken cancellationToken = default);
    }
}