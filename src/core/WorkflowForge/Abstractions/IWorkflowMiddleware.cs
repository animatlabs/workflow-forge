using System;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowForge.Abstractions
{
    /// <summary>
    /// Represents middleware that can be applied to individual workflow operations.
    /// Provides cross-cutting concerns like logging, timing, and validation at the operation level.
    /// </summary>
    public interface IWorkflowOperationMiddleware
    {
        /// <summary>
        /// Executes the middleware logic, wrapping the operation execution.
        /// </summary>
        /// <param name="operation">The operation being executed.</param>
        /// <param name="foundry">The workflow foundry context.</param>
        /// <param name="inputData">The input data for the operation.</param>
        /// <param name="next">The next operation execution delegate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation execution.</returns>
        Task<object?> ExecuteAsync(
            IWorkflowOperation operation,
            IWorkflowFoundry foundry,
            object? inputData,
            Func<Task<object?>> next,
            CancellationToken cancellationToken = default);
    }
}