using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowForge.Abstractions
{
    /// <summary>
    /// The foundry provides operations with access to workflow properties, logging, and services.
    /// It serves as the execution context for workflow operations, maintaining runtime state and services.
    /// In the WorkflowForge metaphor, the foundry is where raw materials (data) are shaped into finished products (results).
    ///
    /// <para><strong>Execution Context Pattern:</strong></para>
    /// <para>
    /// The foundry implements the "Ambient Context" pattern, providing operations with access to:
    /// <list type="bullet">
    /// <item><description><strong>Properties</strong>: Thread-safe state management (ConcurrentDictionary) isolated per foundry instance</description></item>
    /// <item><description><strong>Logger</strong>: Structured logging capabilities for operation diagnostics</description></item>
    /// <item><description><strong>ServiceProvider</strong>: Dependency injection for user operations to resolve services</description></item>
    /// <item><description><strong>CurrentWorkflow</strong>: Reference to the workflow being executed</description></item>
    /// </list>
    /// </para>
    ///
    /// <para><strong>Isolation Guarantees:</strong></para>
    /// <para>
    /// Each foundry instance is completely isolated:
    /// <list type="bullet">
    /// <item><description>Unique ExecutionId (Guid.NewGuid())</description></item>
    /// <item><description>Separate Properties dictionary (new ConcurrentDictionary per foundry)</description></item>
    /// <item><description>Independent middleware pipeline</description></item>
    /// <item><description>Isolated event subscriptions</description></item>
    /// </list>
    /// This ensures parallel foundry executions never interfere with each other.
    /// </para>
    ///
    /// <para><strong>Lifecycle:</strong></para>
    /// <para>
    /// Foundries can be reused across multiple workflows for advanced scenarios like pipeline processing
    /// or batch operations. Properties persist across workflow executions within the same foundry instance.
    /// Dispose the foundry when done to release resources.
    /// </para>
    /// <para>
    /// The foundry pipeline is frozen during execution. Operations and middleware
    /// cannot be added or removed while <see cref="ForgeAsync"/> is running.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The foundry is the execution environment where operations are performed.
    /// It provides thread-safe access to shared properties, logging capabilities, and dependency injection services.
    /// Each foundry instance represents an isolated execution context that can host one or more workflow executions.
    ///
    /// <para><strong>Interface Segregation:</strong></para>
    /// <para>
    /// IWorkflowFoundry composes multiple focused interfaces:
    /// <list type="bullet">
    /// <item><description><see cref="IWorkflowExecutionContext"/>: Provides access to execution state, properties, and services</description></item>
    /// <item><description><see cref="IWorkflowMiddlewarePipeline"/>: Provides methods for building the execution pipeline</description></item>
    /// <item><description><see cref="IOperationLifecycleEvents"/>: Provides event subscriptions for operation lifecycle</description></item>
    /// <item><description><see cref="IDisposable"/>: Provides synchronous resource cleanup</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This composition allows code to depend on only the specific capabilities it needs,
    /// following the Interface Segregation Principle (ISP).
    /// </para>
    /// </remarks>
    public interface IWorkflowFoundry : IWorkflowExecutionContext, IWorkflowMiddlewarePipeline, IDisposable, IOperationLifecycleEvents
    {
        /// <summary>
        /// Executes all operations in the foundry.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task ForgeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Replaces the current operations with a new sequence.
        /// </summary>
        /// <param name="operations">The operations to set.</param>
        void ReplaceOperations(IEnumerable<IWorkflowOperation> operations);

        /// <summary>
        /// Gets whether the foundry pipeline is frozen for execution.
        /// </summary>
        bool IsFrozen { get; }
    }
}