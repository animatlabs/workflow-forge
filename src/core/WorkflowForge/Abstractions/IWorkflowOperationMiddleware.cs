using System;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowForge.Abstractions
{
    /// <summary>
    /// Middleware that executes at the operation level (wraps individual operation execution).
    /// Operation middleware executes INSIDE the foundry pipeline for each operation.
    ///
    /// <para><strong>Execution Order:</strong></para>
    /// <para>
    /// Operation middleware wraps each operation execution in a Russian Doll pattern:
    /// <code>
    /// OperationMiddleware1 (outermost - first added)
    ///   → OperationMiddleware2
    ///     → OperationMiddleware3 (innermost - last added)
    ///       → OPERATION EXECUTES
    ///     ← OperationMiddleware3
    ///   ← OperationMiddleware2
    /// ← OperationMiddleware1
    /// </code>
    /// </para>
    ///
    /// <para><strong>Common Use Cases:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Error Handling</strong>: Catch and log operation exceptions</description></item>
    /// <item><description><strong>Timing</strong>: Measure operation execution time</description></item>
    /// <item><description><strong>Logging</strong>: Log operation start/completion with context</description></item>
    /// <item><description><strong>Validation</strong>: Validate operation inputs/outputs</description></item>
    /// <item><description><strong>Retry Logic</strong>: Retry failed operations with backoff</description></item>
    /// <item><description><strong>Caching</strong>: Cache operation results</description></item>
    /// <item><description><strong>Authorization</strong>: Check permissions before executing operation</description></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="IWorkflowMiddleware"/> which executes once per workflow,
    /// operation middleware executes for EACH operation in the workflow.
    /// This allows fine-grained control over individual operation behavior.
    /// </remarks>
    public interface IWorkflowOperationMiddleware
    {
        /// <summary>
        /// Executes the operation middleware logic.
        /// </summary>
        /// <param name="operation">The operation being executed.</param>
        /// <param name="foundry">The foundry context providing properties, logger, and services.</param>
        /// <param name="inputData">Input data passed to the operation.</param>
        /// <param name="next">
        /// Delegate to invoke the next middleware in the pipeline or the operation itself.
        /// Call this to continue the operation execution. Not calling this will short-circuit the operation.
        /// </param>
        /// <param name="cancellationToken">Cancellation token for the operation execution.</param>
        /// <returns>
        /// A task representing the operation execution result.
        /// The result can be null or any object depending on the operation.
        /// </returns>
        /// <exception cref="OperationCanceledException">Thrown when the operation execution is cancelled.</exception>
        /// <remarks>
        /// <para>Middleware should:</para>
        /// <list type="bullet">
        /// <item><description>Call <paramref name="next"/>() to continue operation execution</description></item>
        /// <item><description>Handle exceptions appropriately (log, transform, or rethrow)</description></item>
        /// <item><description>Clean up resources in finally blocks</description></item>
        /// <item><description>Respect cancellation tokens</description></item>
        /// <item><description>Preserve operation results unless intentionally transforming them</description></item>
        /// </list>
        ///
        /// <para>Example implementation:</para>
        /// <code>
        /// public async Task&lt;object?&gt; ExecuteAsync(
        ///     IWorkflowOperation operation,
        ///     IWorkflowFoundry foundry,
        ///     object? inputData,
        ///     Func&lt;Task&lt;object?&gt;&gt; next,
        ///     CancellationToken cancellationToken)
        /// {
        ///     // Pre-execution logic
        ///     Logger.LogInformation("Starting operation: {Name}", operation.Name);
        ///
        ///     try
        ///     {
        ///         // Execute operation
        ///         var result = await next().ConfigureAwait(false);
        ///
        ///         // Post-execution logic (success)
        ///         Logger.LogInformation("Operation completed: {Name}", operation.Name);
        ///         return result;
        ///     }
        ///     catch (Exception ex)
        ///     {
        ///         // Post-execution logic (failure)
        ///         Logger.LogError(ex, "Operation failed: {Name}", operation.Name);
        ///         throw;
        ///     }
        /// }
        /// </code>
        /// </remarks>
        Task<object?> ExecuteAsync(
            IWorkflowOperation operation,
            IWorkflowFoundry foundry,
            object? inputData,
            Func<Task<object?>> next,
            CancellationToken cancellationToken = default);
    }
}



