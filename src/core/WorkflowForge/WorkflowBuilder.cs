using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Operations;

namespace WorkflowForge
{
    /// <summary>
    /// Fluent builder for constructing workflow definitions.
    /// Provides a clean API for configuring workflow properties and operations.
    /// </summary>
    public sealed class WorkflowBuilder
    {
        private readonly List<IWorkflowOperation> _operations = new();
        private readonly IServiceProvider? _serviceProvider;

        private string? _name;
        private string? _description;
        private string _version;

        /// <summary>
        /// Gets the service provider for testing purposes.
        /// </summary>
        internal IServiceProvider? ServiceProvider => _serviceProvider;

        /// <summary>
        /// Gets the workflow name for testing purposes.
        /// </summary>
        internal string? Name => _name;

        /// <summary>
        /// Gets the workflow description for testing purposes.
        /// </summary>
        internal string? Description => _description;

        /// <summary>
        /// Gets the workflow version for testing purposes.
        /// </summary>
        internal string Version => _version;

        /// <summary>
        /// Gets the read-only list of operations for testing purposes.
        /// </summary>
        internal IReadOnlyList<IWorkflowOperation> Operations => new ReadOnlyCollection<IWorkflowOperation>(_operations);

        /// <summary>
        /// Initializes a new instance of the WorkflowBuilder class.
        /// </summary>
        /// <param name="serviceProvider">Optional service provider for dependency injection.</param>
        internal WorkflowBuilder(IServiceProvider? serviceProvider = null)
        {
            _serviceProvider = serviceProvider;
            _version = "1.0.0";
        }

        /// <summary>
        /// Initializes a new instance of the WorkflowBuilder class with a specified name.
        /// </summary>
        /// <param name="name">The name of the workflow.</param>
        /// <param name="serviceProvider">Optional service provider for dependency injection.</param>
        /// <exception cref="ArgumentException">Thrown when name is null, empty, or whitespace.</exception>
        internal WorkflowBuilder(string name, IServiceProvider? serviceProvider = null)
            : this(serviceProvider)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Workflow name cannot be null, empty, or whitespace.", nameof(name));

            _name = name;
        }

        /// <summary>
        /// Sets the name of the workflow.
        /// This is a human-readable identifier for the workflow instance.
        /// </summary>
        /// <param name="name">The workflow name. Cannot be null, empty, or whitespace.</param>
        /// <returns>The current WorkflowBuilder instance for method chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when name is null, empty, or whitespace.</exception>
        public WorkflowBuilder WithName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Workflow name cannot be null, empty, or whitespace.", nameof(name));

            _name = name;
            return this;
        }

        /// <summary>
        /// Sets an optional description for the workflow.
        /// This provides additional context about the workflow's purpose.
        /// </summary>
        /// <param name="description">The workflow description. Can be null.</param>
        /// <returns>The current WorkflowBuilder instance for method chaining.</returns>
        public WorkflowBuilder WithDescription(string? description)
        {
            _description = description;
            return this;
        }

        /// <summary>
        /// Sets the version of the workflow.
        /// This helps with workflow versioning and change management.
        /// </summary>
        /// <param name="version">The workflow version. Cannot be null, empty, or whitespace.</param>
        /// <returns>The current WorkflowBuilder instance for method chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when version is null, empty, or whitespace.</exception>
        public WorkflowBuilder WithVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentException("Version cannot be null, empty, or whitespace.", nameof(version));

            _version = version;
            return this;
        }

        /// <summary>
        /// Adds a workflow operation to the execution sequence.
        /// Operations are executed in the order they are added.
        /// </summary>
        /// <param name="operation">The operation to add. Cannot be null.</param>
        /// <returns>The current WorkflowBuilder instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when operation is null.</exception>
        public WorkflowBuilder AddOperation(IWorkflowOperation operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            _operations.Add(operation);
            return this;
        }

        /// <summary>
        /// Adds a workflow operation by type using dependency injection.
        /// The operation will be resolved from the service provider when the workflow executes.
        /// </summary>
        /// <typeparam name="T">The type of operation to add. Must implement IWorkflowOperation.</typeparam>
        /// <returns>The current WorkflowBuilder instance for method chaining.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no service provider is available.</exception>
        public WorkflowBuilder AddOperation<T>() where T : class, IWorkflowOperation
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException("Service provider is required for dependency injection operations.");

            var operation = (IWorkflowOperation)_serviceProvider.GetService(typeof(T))
                ?? throw new InvalidOperationException($"Unable to resolve operation of type {typeof(T).Name} from service provider.");

            return AddOperation(operation);
        }

        /// <summary>
        /// Adds an inline asynchronous operation to the workflow.
        /// This provides a quick way to add simple operations without creating separate classes.
        /// </summary>
        /// <param name="name">The name of the operation for identification purposes.</param>
        /// <param name="action">The asynchronous action to execute. Receives the workflow foundry and cancellation token.</param>
        /// <returns>The current WorkflowBuilder instance for method chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when name is null, empty, or whitespace.</exception>
        /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
        public WorkflowBuilder AddOperation(string name, Func<IWorkflowFoundry, CancellationToken, Task> action)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Operation name cannot be null, empty, or whitespace.", nameof(name));

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            // Adapt to ActionWorkflowOperation signature (object?, IWorkflowFoundry, CancellationToken, Task)
            var adaptedAction = new Func<object?, IWorkflowFoundry, CancellationToken, Task>((input, foundry, token) =>
                action(foundry, token));

            var operation = new ActionWorkflowOperation(name, adaptedAction);
            return AddOperation(operation);
        }

        /// <summary>
        /// Adds an inline synchronous operation to the workflow.
        /// This provides a quick way to add simple operations without creating separate classes.
        /// Note: Synchronous operations will be wrapped to run asynchronously.
        /// </summary>
        /// <param name="name">The name of the operation for identification purposes.</param>
        /// <param name="action">The synchronous action to execute. Receives the workflow foundry.</param>
        /// <returns>The current WorkflowBuilder instance for method chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when name is null, empty, or whitespace.</exception>
        /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
        public WorkflowBuilder AddOperation(string name, Action<IWorkflowFoundry> action)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Operation name cannot be null, empty, or whitespace.", nameof(name));

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            // Adapt to ActionWorkflowOperation signature (object?, IWorkflowFoundry, CancellationToken, Task)
            var asyncAction = new Func<object?, IWorkflowFoundry, CancellationToken, Task>((input, foundry, _) =>
            {
                action(foundry);
                return Task.CompletedTask;
            });

            var operation = new ActionWorkflowOperation(name, asyncAction);
            return AddOperation(operation);
        }

        /// <summary>
        /// Builds the configured workflow.
        /// </summary>
        /// <returns>A workflow instance ready for execution.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the workflow configuration is invalid.</exception>
        public IWorkflow Build()
        {
            if (string.IsNullOrWhiteSpace(_name))
                throw new InvalidOperationException("Workflow name is required. Call WithName() before building.");

            if (_operations.Count == 0)
                throw new InvalidOperationException("At least one operation is required. Add operations using AddOperation() before building.");

            return new Workflow(
                name: _name!,
                description: _description,
                version: _version,
                operations: new List<IWorkflowOperation>(_operations),
                properties: new Dictionary<string, object?>()
            );
        }

        /// <summary>
        /// Creates a sequential workflow from the specified operations.
        /// </summary>
        /// <param name="operations">The operations to execute sequentially.</param>
        /// <returns>A workflow that executes operations in sequence.</returns>
        /// <exception cref="ArgumentNullException">Thrown when operations is null.</exception>
        /// <exception cref="ArgumentException">Thrown when operations is empty.</exception>
        public static IWorkflow Sequential(params IWorkflowOperation[] operations)
        {
            if (operations == null) throw new ArgumentNullException(nameof(operations));
            if (operations.Length == 0) throw new ArgumentException("At least one operation is required.", nameof(operations));

            var builder = new WorkflowBuilder()
                .WithName($"Sequential-{Guid.NewGuid():N}");

            foreach (var operation in operations)
            {
                builder.AddOperation(operation);
            }

            return builder.Build();
        }

        /// <summary>
        /// Creates a parallel workflow from the specified operations.
        /// </summary>
        /// <param name="operations">The operations to execute in parallel.</param>
        /// <returns>A workflow that executes operations in parallel.</returns>
        /// <exception cref="ArgumentNullException">Thrown when operations is null.</exception>
        /// <exception cref="ArgumentException">Thrown when operations is empty.</exception>
        public static IWorkflow Parallel(params IWorkflowOperation[] operations)
        {
            if (operations == null) throw new ArgumentNullException(nameof(operations));
            if (operations.Length == 0) throw new ArgumentException("At least one operation is required.", nameof(operations));

            var builder = new WorkflowBuilder()
                .WithName($"Parallel-{Guid.NewGuid():N}")
                .AddOperation(ForEachWorkflowOperation.CreateSharedInput(operations));

            return builder.Build();
        }
    }
}