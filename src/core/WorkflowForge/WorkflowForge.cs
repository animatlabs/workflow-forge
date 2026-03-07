using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using WorkflowForge.Abstractions;
using WorkflowForge.Exceptions;
using WorkflowForge.Loggers;
using WorkflowForge.Options;

namespace WorkflowForge
{
    /// <summary>
    /// Main entry point for WorkflowForge - the enterprise workflow orchestration framework.
    /// Provides essential factory methods for creating workflow builders and smiths.
    ///
    /// In the WorkflowForge metaphor:
    /// - The Forge is the main factory where workflows are created and configured
    /// - Foundries are execution environments created by smiths
    /// - Builders are the tools for constructing workflows
    /// - Smiths are the skilled craftsmen who manage foundries and forge workflows
    /// </summary>
    /// <remarks>
    /// WorkflowForge follows enterprise-grade patterns:
    /// - Dependency-free core library
    /// - Modular extension architecture
    /// - Thread-safe operations
    /// - Comprehensive error handling
    /// - Performance monitoring capabilities
    /// </remarks>
    public static class WorkflowForge
    {
        /// <summary>
        /// Creates a new workflow builder for defining and constructing workflows.
        /// </summary>
        /// <param name="workflowName">Optional name for the workflow. If not provided, a name can be set later using WithName().</param>
        /// <param name="serviceProvider">Optional service provider for dependency injection. Can be null for standalone usage.</param>
        /// <returns>A new workflow builder instance ready for configuration.</returns>
        /// <exception cref="ArgumentException">Thrown when workflowName is provided but is empty or whitespace.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the builder cannot be created due to system constraints.</exception>
        /// <example>
        /// <code>
        /// // Without name (set later)
        /// var builder = WorkflowForge.CreateWorkflow();
        /// var workflow = builder
        ///     .WithName("ProcessOrder")
        ///     .AddOperation("ValidateOrder", async (f, ct) => ValidateOrder())
        ///     .Build();
        ///
        /// // With name
        /// var builder2 = WorkflowForge.CreateWorkflow("ProcessPayment");
        /// </code>
        /// </example>
        public static WorkflowBuilder CreateWorkflow(string? workflowName = null, IServiceProvider? serviceProvider = null)
        {
            if (workflowName != null && string.IsNullOrWhiteSpace(workflowName))
                throw new ArgumentException("Workflow name cannot be empty or whitespace.", nameof(workflowName));

            try
            {
                var builder = new WorkflowBuilder(serviceProvider);
                return workflowName != null ? builder.WithName(workflowName) : builder;
            }
            catch (Exception ex) when (!(ex is ArgumentException))
            {
                var message = workflowName != null
                    ? $"Failed to create workflow builder for '{workflowName}'. Ensure the system has sufficient resources."
                    : "Failed to create workflow builder. Ensure the system has sufficient resources.";
                throw new WorkflowForgeException(message, ex);
            }
        }

        /// <summary>
        /// Creates a foundry for workflow execution.
        /// </summary>
        /// <param name="workflowName">The name of the workflow.</param>
        /// <param name="logger">Optional logger for the foundry. If null, a null logger will be used.</param>
        /// <param name="initialProperties">Optional initial properties for the foundry. If null, an empty dictionary will be created.</param>
        /// <param name="options">Optional execution options for the foundry.</param>
        /// <returns>A new foundry instance.</returns>
        /// <exception cref="ArgumentException">Thrown when workflowName is null, empty, or whitespace.</exception>
        /// <example>
        /// <code>
        /// // Simple foundry
        /// var foundry1 = WorkflowForge.CreateFoundry("MyWorkflow");
        ///
        /// // With logger
        /// var foundry2 = WorkflowForge.CreateFoundry("MyWorkflow", logger);
        ///
        /// // With initial properties
        /// var properties = new Dictionary&lt;string, object?&gt; { ["UserId"] = "123" };
        /// var foundry3 = WorkflowForge.CreateFoundry("MyWorkflow", null, properties);
        ///
        /// // With both
        /// var foundry4 = WorkflowForge.CreateFoundry("MyWorkflow", logger, properties);
        /// </code>
        /// </example>
        public static IWorkflowFoundry CreateFoundry(
            string workflowName,
            IWorkflowForgeLogger? logger = null,
            IDictionary<string, object?>? initialProperties = null,
            WorkflowForgeOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(workflowName))
                throw new ArgumentException("Workflow name cannot be null, empty, or whitespace.", nameof(workflowName));

            var properties = initialProperties != null
                ? new ConcurrentDictionary<string, object?>(initialProperties)
                : new ConcurrentDictionary<string, object?>();

            return logger != null
                ? new WorkflowFoundry(Guid.NewGuid(), properties, logger, options: options)
                : new WorkflowFoundry(Guid.NewGuid(), properties, options: options);
        }

        /// <summary>
        /// Creates a new workflow smith for executing workflows.
        /// The smith provides foundry management and workflow execution capabilities.
        /// </summary>
        /// <param name="logger">Optional logger for the smith. If null, a null logger will be used.</param>
        /// <param name="serviceProvider">Optional service provider for dependency injection.</param>
        /// <param name="options">Optional WorkflowForge configuration options.</param>
        /// <returns>A new workflow smith instance ready for workflow execution.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the smith cannot be created due to system constraints.</exception>
        /// <example>
        /// <code>
        /// // Simple smith
        /// var smith1 = WorkflowForge.CreateSmith();
        ///
        /// // With logger
        /// var smith2 = WorkflowForge.CreateSmith(logger);
        ///
        /// // With service provider
        /// var smith3 = WorkflowForge.CreateSmith(null, serviceProvider);
        ///
        /// // With options
        /// var options = new WorkflowForgeOptions { MaxConcurrentWorkflows = 10 };
        /// var smith4 = WorkflowForge.CreateSmith(null, null, options);
        ///
        /// // With all parameters
        /// var smith5 = WorkflowForge.CreateSmith(logger, serviceProvider, options);
        /// </code>
        /// </example>
        public static IWorkflowSmith CreateSmith(
            IWorkflowForgeLogger? logger = null,
            IServiceProvider? serviceProvider = null,
            WorkflowForgeOptions? options = null)
        {
            try
            {
                return new WorkflowSmith(logger ?? NullLogger.Instance, serviceProvider, options);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException
                                       && ex is not StackOverflowException
                                       && ex is not System.Threading.ThreadAbortException)
            {
                throw new WorkflowForgeException("Failed to create workflow smith. Ensure the system has sufficient resources.", ex);
            }
        }
    }
}
