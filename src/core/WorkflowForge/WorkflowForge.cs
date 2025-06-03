using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using WorkflowForge.Abstractions;
using WorkflowForge.Operations;
using WorkflowForge.Loggers;
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
        /// <param name="serviceProvider">Optional service provider for dependency injection. Can be null for standalone usage.</param>
        /// <returns>A new workflow builder instance ready for configuration.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the builder cannot be created due to system constraints.</exception>
        /// <example>
        /// <code>
        /// var builder = WorkflowForge.CreateWorkflow();
        /// var workflow = builder
        ///     .WithName("ProcessOrder")
        ///     .AddStep("ValidateOrder", order => ValidateOrder(order))
        ///     .AddStep("ProcessPayment", order => ProcessPayment(order))
        ///     .Build();
        /// </code>
        /// </example>
        public static WorkflowBuilder CreateWorkflow(IServiceProvider? serviceProvider = null)
        {
            try
            {
                return new WorkflowBuilder(serviceProvider);
            }
            catch (Exception ex)
            {
                throw new WorkflowForgeException("Failed to create workflow builder. Ensure the system has sufficient resources.", ex);
            }
        }
        /// <summary>
        /// Creates a new workflow builder with a specified name and optional service provider.
        /// </summary>
        /// <param name="workflowName">The name of the workflow being built. Cannot be null or empty.</param>
        /// <param name="serviceProvider">Optional service provider for dependency injection. Can be null for standalone usage.</param>
        /// <returns>A new workflow builder instance configured with the specified name.</returns>
        /// <exception cref="ArgumentException">Thrown when workflowName is null, empty, or whitespace.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the builder cannot be created due to system constraints.</exception>
        public static WorkflowBuilder CreateWorkflow(string workflowName, IServiceProvider? serviceProvider = null)
        {
            if (string.IsNullOrWhiteSpace(workflowName))
                throw new ArgumentException("Workflow name cannot be null, empty, or whitespace.", nameof(workflowName));
            try
            {
                return (WorkflowBuilder)new WorkflowBuilder(serviceProvider).WithName(workflowName);
            }
            catch (Exception ex) when (!(ex is ArgumentException))
            {
                throw new WorkflowForgeException($"Failed to create workflow builder for '{workflowName}'. Ensure the system has sufficient resources.", ex);
            }
        }

        /// <summary>
        /// Creates a foundry for workflow execution.
        /// </summary>
        /// <param name="workflowName">The name of the workflow.</param>
        /// <returns>A new foundry instance.</returns>
        public static IWorkflowFoundry CreateFoundry(string workflowName)
        {
            if (string.IsNullOrWhiteSpace(workflowName))
                throw new ArgumentException("Workflow name cannot be null, empty, or whitespace.", nameof(workflowName));
                
            return new WorkflowFoundry(
                Guid.NewGuid(), 
                new ConcurrentDictionary<string, object?>(),
                FoundryConfiguration.Minimal());
        }

        /// <summary>
        /// Creates a foundry for workflow execution with a logger.
        /// </summary>
        /// <param name="workflowName">The name of the workflow.</param>
        /// <param name="logger">The logger to use.</param>
        /// <returns>A new foundry instance.</returns>
        public static IWorkflowFoundry CreateFoundry(string workflowName, IWorkflowForgeLogger logger)
        {
            if (string.IsNullOrWhiteSpace(workflowName))
                throw new ArgumentException("Workflow name cannot be null, empty, or whitespace.", nameof(workflowName));
                
            return new WorkflowFoundry(
                Guid.NewGuid(), 
                new ConcurrentDictionary<string, object?>(),
                logger);
        }

        /// <summary>
        /// Creates a foundry for workflow execution with configuration.
        /// </summary>
        /// <param name="workflowName">The name of the workflow.</param>
        /// <param name="configuration">The foundry configuration.</param>
        /// <returns>A new foundry instance.</returns>
        public static IWorkflowFoundry CreateFoundry(string workflowName, FoundryConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(workflowName))
                throw new ArgumentException("Workflow name cannot be null, empty, or whitespace.", nameof(workflowName));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
                
            return new WorkflowFoundry(
                Guid.NewGuid(), 
                new ConcurrentDictionary<string, object?>(),
                configuration);
        }

        /// <summary>
        /// Creates a foundry for workflow execution with initial properties.
        /// </summary>
        /// <param name="workflowName">The name of the workflow.</param>
        /// <param name="initialProperties">The initial properties for the foundry.</param>
        /// <returns>A new foundry instance.</returns>
        public static IWorkflowFoundry CreateFoundryWithData(string workflowName, IDictionary<string, object?> initialProperties)
        {
            if (string.IsNullOrWhiteSpace(workflowName))
                throw new ArgumentException("Workflow name cannot be null, empty, or whitespace.", nameof(workflowName));
            if (initialProperties == null)
                throw new ArgumentNullException(nameof(initialProperties));
                
            var properties = new ConcurrentDictionary<string, object?>(initialProperties);
            return new WorkflowFoundry(
                Guid.NewGuid(), 
                properties,
                FoundryConfiguration.Minimal());
        }

        /// <summary>
        /// Creates a foundry for workflow execution with initial properties and configuration.
        /// </summary>
        /// <param name="workflowName">The name of the workflow.</param>
        /// <param name="initialProperties">The initial properties for the foundry.</param>
        /// <param name="configuration">The foundry configuration.</param>
        /// <returns>A new foundry instance.</returns>
        public static IWorkflowFoundry CreateFoundryWithData(string workflowName, IDictionary<string, object?> initialProperties, FoundryConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(workflowName))
                throw new ArgumentException("Workflow name cannot be null, empty, or whitespace.", nameof(workflowName));
            if (initialProperties == null)
                throw new ArgumentNullException(nameof(initialProperties));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
                
            var properties = new ConcurrentDictionary<string, object?>(initialProperties);
            return new WorkflowFoundry(
                Guid.NewGuid(), 
                properties,
                configuration);
        }

        /// <summary>
        /// Creates a new workflow smith for executing workflows.
        /// The smith provides foundry management and workflow execution capabilities.
        /// </summary>
        /// <param name="logger">Optional logger for the smith. If null, a null logger will be used.</param>
        /// <param name="serviceProvider">Optional service provider for dependency injection.</param>
        /// <returns>A new workflow smith instance ready for workflow execution.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the smith cannot be created due to system constraints.</exception>
        /// <example>
        /// <code>
        /// // For development
        /// var smith = WorkflowForge.CreateSmith(logger);
        /// 
        /// // For production
        /// var foundry = WorkflowForge.CreateFoundry("ProcessOrder", FoundryConfiguration.ForProduction());
        /// 
        /// // For high performance
        /// var foundry = WorkflowForge.CreateFoundry("ProcessBatch", FoundryConfiguration.HighPerformance());
        /// </code>
        /// </example>
        public static IWorkflowSmith CreateSmith(IWorkflowForgeLogger? logger = null, IServiceProvider? serviceProvider = null)
        {
            try
            {
                return new WorkflowSmith(logger ?? NullLogger.Instance, serviceProvider);
            }
            catch (Exception ex)
            {
                throw new WorkflowForgeException("Failed to create workflow smith. Ensure the system has sufficient resources.", ex);
            }
        }
    }
} 
