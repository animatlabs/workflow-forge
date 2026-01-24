using System;

namespace WorkflowForge.Abstractions
{
    /// <summary>
    /// Provides methods for building the middleware pipeline and operation chain.
    /// This interface represents the builder pattern for constructing workflow execution pipelines.
    /// </summary>
    /// <remarks>
    /// This interface follows the Interface Segregation Principle (ISP), providing only
    /// pipeline-building members. Code that only needs to add operations or middleware can depend
    /// on this interface rather than the full IWorkflowFoundry.
    /// </remarks>
    public interface IWorkflowMiddlewarePipeline
    {
        /// <summary>
        /// Adds an operation to be executed in this foundry.
        /// Operations are executed in the order they are added.
        /// </summary>
        /// <param name="operation">The operation to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when operation is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the foundry has been disposed.</exception>
        void AddOperation(IWorkflowOperation operation);

        /// <summary>
        /// Adds middleware to the execution pipeline.
        /// Middleware wraps operations in a Russian Doll pattern (reverse order execution).
        /// First middleware added = outermost layer of the pipeline.
        /// See /docs/architecture/middleware-pipeline.md for detailed explanation.
        /// </summary>
        /// <param name="middleware">The middleware to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when middleware is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the foundry has been disposed.</exception>
        void AddMiddleware(IWorkflowOperationMiddleware middleware);
    }
}

