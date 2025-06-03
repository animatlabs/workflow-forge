using System;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowForge
{
    /// <summary>
    /// Represents a single operation within a workflow that can be forged (executed) in the foundry.
    /// Operations are the building blocks of workflows, each performing a specific task.
    /// In the Workflow Forge metaphor, operations are the tools and processes used to shape raw materials into finished products.
    /// </summary>
    public interface IWorkflowOperation : IDisposable
    {
        /// <summary>
        /// Gets the unique identifier for this operation.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Gets the name of this operation for identification and logging purposes.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a value indicating whether this operation supports restoration/rollback.
        /// Operations that support restoration can undo their effects through RestoreAsync.
        /// </summary>
        bool SupportsRestore { get; }

        /// <summary>
        /// Forges (executes) the operation's primary logic with explicit input data and returns output data.
        /// The orchestrator controls what data flows into and out of each operation.
        /// </summary>
        /// <param name="inputData">The input data for this operation (can be null).</param>
        /// <param name="foundry">The workflow foundry providing execution context and shared services.</param>
        /// <param name="cancellationToken">The cancellation token for cooperative cancellation.</param>
        /// <returns>A task representing the asynchronous operation with optional output data.</returns>
        /// <exception cref="ArgumentNullException">Thrown when foundry is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the operation has been disposed.</exception>
        Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default);

        /// <summary>
        /// Restores (compensates) the operation's effects to undo its changes.
        /// Used for rollback scenarios when subsequent operations fail.
        /// </summary>
        /// <param name="outputData">The data that was produced by this operation during ForgeAsync.</param>
        /// <param name="foundry">The workflow foundry providing execution context and shared services.</param>
        /// <param name="cancellationToken">The cancellation token for cooperative cancellation.</param>
        /// <returns>A task representing the asynchronous restoration.</returns>
        /// <exception cref="NotSupportedException">Thrown if the operation does not support compensation.</exception>
        /// <exception cref="ArgumentNullException">Thrown when foundry is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the operation has been disposed.</exception>
        Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Strongly-typed workflow operation interface for operations with known input/output types.
    /// Provides compile-time type safety for data flow between operations.
    /// </summary>
    /// <typeparam name="TInput">The type of input data this operation expects.</typeparam>
    /// <typeparam name="TOutput">The type of output data this operation produces.</typeparam>
    public interface IWorkflowOperation<TInput, TOutput> : IWorkflowOperation
    {
        /// <summary>
        /// Forges (executes) the operation with strongly-typed input and output.
        /// </summary>
        /// <param name="input">The typed input data for this operation.</param>
        /// <param name="foundry">The workflow foundry providing execution context and shared services.</param>
        /// <param name="cancellationToken">The cancellation token for cooperative cancellation.</param>
        /// <returns>A task representing the asynchronous operation with typed output data.</returns>
        /// <exception cref="ArgumentNullException">Thrown when foundry is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the operation has been disposed.</exception>
        Task<TOutput> ForgeAsync(TInput input, IWorkflowFoundry foundry, CancellationToken cancellationToken = default);

        /// <summary>
        /// Restores (compensates) the operation's effects with typed output data.
        /// </summary>
        /// <param name="output">The typed output data that was produced by this operation.</param>
        /// <param name="foundry">The workflow foundry providing execution context and shared services.</param>
        /// <param name="cancellationToken">The cancellation token for cooperative cancellation.</param>
        /// <returns>A task representing the asynchronous restoration.</returns>
        /// <exception cref="NotSupportedException">Thrown if the operation does not support compensation.</exception>
        /// <exception cref="ArgumentNullException">Thrown when foundry is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the operation has been disposed.</exception>
        Task RestoreAsync(TOutput output, IWorkflowFoundry foundry, CancellationToken cancellationToken = default);
    }
} 
