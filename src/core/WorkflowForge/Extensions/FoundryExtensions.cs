using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Operations;

namespace WorkflowForge.Extensions
{
    /// <summary>
    /// Extension methods for IWorkflowFoundry to provide convenient operations and properties management.
    /// </summary>
    public static class FoundryExtensions
    {
        /// <summary>
        /// Sets correlation ID in foundry data for tracking across operations.
        /// </summary>
        public static void SetCorrelationId(this IWorkflowFoundry foundry, string correlationId)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            foundry.Properties["CorrelationId"] = correlationId;
        }

        /// <summary>
        /// Gets correlation ID from foundry data.
        /// </summary>
        public static string? GetCorrelationId(this IWorkflowFoundry foundry)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            return foundry.Properties.TryGetValue("CorrelationId", out var correlationId)
                ? correlationId?.ToString()
                : null;
        }

        /// <summary>
        /// Sets parent workflow execution ID for nested workflow tracking.
        /// </summary>
        public static void SetParentWorkflowExecutionId(this IWorkflowFoundry foundry, string parentWorkflowExecutionId)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            foundry.Properties["ParentWorkflowExecutionId"] = parentWorkflowExecutionId;
        }

        /// <summary>
        /// Tries to get a strongly-typed value from the foundry properties.
        /// </summary>
        public static bool TryGetProperty<T>(this IWorkflowFoundry foundry, string key, out T? value)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            if (foundry.Properties.TryGetValue(key, out var obj) && obj is T t)
            {
                value = t;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Gets a strongly-typed value from the foundry properties, or default if not present or of wrong type.
        /// </summary>
        public static T? GetPropertyOrDefault<T>(this IWorkflowFoundry foundry, string key)
        {
            return foundry.TryGetProperty<T>(key, out var value) ? value : default;
        }

        /// <summary>
        /// Gets a strongly-typed value from the foundry properties, or the provided default when missing.
        /// </summary>
        public static T GetPropertyOrDefault<T>(this IWorkflowFoundry foundry, string key, T defaultValue)
        {
            return foundry.TryGetProperty<T>(key, out var value) && value is not null ? value : defaultValue;
        }

        /// <summary>
        /// Sets a value in the foundry properties.
        /// </summary>
        public static IWorkflowFoundry SetProperty(this IWorkflowFoundry foundry, string key, object? value)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            foundry.Properties[key] = value;
            return foundry;
        }

        /// <summary>
        /// Adds operations to the foundry and returns the foundry for method chaining.
        /// </summary>
        /// <param name="foundry">The foundry to add operations to.</param>
        /// <param name="operations">The operations to add.</param>
        /// <returns>The same foundry instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when foundry or operations is null.</exception>
        public static IWorkflowFoundry WithOperations(this IWorkflowFoundry foundry, params IWorkflowOperation[] operations)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (operations == null) throw new ArgumentNullException(nameof(operations));

            foreach (var operation in operations)
            {
                if (operation != null)
                {
                    foundry.AddOperation(operation);
                }
            }

            return foundry;
        }

        /// <summary>
        /// Adds operations to the foundry and returns the foundry for method chaining.
        /// </summary>
        /// <param name="foundry">The foundry to add operations to.</param>
        /// <param name="operations">The operations to add.</param>
        /// <returns>The same foundry instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when foundry or operations is null.</exception>
        public static IWorkflowFoundry WithOperations(this IWorkflowFoundry foundry, IEnumerable<IWorkflowOperation> operations)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (operations == null) throw new ArgumentNullException(nameof(operations));

            foreach (var operation in operations)
            {
                if (operation != null)
                {
                    foundry.AddOperation(operation);
                }
            }

            return foundry;
        }

        /// <summary>
        /// Adds a single operation to the foundry and returns the foundry for method chaining.
        /// </summary>
        /// <param name="foundry">The foundry to add the operation to.</param>
        /// <param name="operation">The operation to add.</param>
        /// <returns>The same foundry instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when foundry or operation is null.</exception>
        public static IWorkflowFoundry WithOperation(this IWorkflowFoundry foundry, IWorkflowOperation operation)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            foundry.AddOperation(operation);
            return foundry;
        }

        /// <summary>
        /// Adds an inline operation to the foundry using a lambda expression.
        /// </summary>
        /// <param name="foundry">The foundry to add the operation to.</param>
        /// <param name="name">The name of the operation.</param>
        /// <param name="action">The action to execute. Takes the foundry as parameter.</param>
        /// <returns>The same foundry instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when foundry, name, or action is null.</exception>
        /// <exception cref="ArgumentException">Thrown when name is empty or whitespace.</exception>
        public static IWorkflowFoundry WithOperation(this IWorkflowFoundry foundry, string name, Func<IWorkflowFoundry, Task> action)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Operation name cannot be null, empty, or whitespace.", nameof(name));
            if (action == null) throw new ArgumentNullException(nameof(action));

            var operation = new DelegateWorkflowOperation(name, async (input, foundry, ct) =>
            {
                await action(foundry);
                return input; // Pass through input data
            });

            foundry.AddOperation(operation);
            return foundry;
        }

        /// <summary>
        /// Adds an inline synchronous operation to the foundry using a lambda expression.
        /// </summary>
        /// <param name="foundry">The foundry to add the operation to.</param>
        /// <param name="name">The name of the operation.</param>
        /// <param name="action">The synchronous action to execute. Takes the foundry as parameter.</param>
        /// <returns>The same foundry instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when foundry, name, or action is null.</exception>
        /// <exception cref="ArgumentException">Thrown when name is empty or whitespace.</exception>
        public static IWorkflowFoundry WithOperation(this IWorkflowFoundry foundry, string name, Action<IWorkflowFoundry> action)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Operation name cannot be null, empty, or whitespace.", nameof(name));
            if (action == null) throw new ArgumentNullException(nameof(action));

            var operation = new DelegateWorkflowOperation(name, (input, foundry, ct) =>
            {
                action(foundry);
                return Task.FromResult(input); // Pass through input data
            });

            foundry.AddOperation(operation);
            return foundry;
        }

        /// <summary>
        /// Sets a value in the foundry's properties and returns the foundry for method chaining.
        /// </summary>
        /// <param name="foundry">The foundry to set the value in.</param>
        /// <param name="key">The key for the value.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>The same foundry instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when foundry is null.</exception>
        /// <exception cref="ArgumentException">Thrown when key is null or empty.</exception>
        public static IWorkflowFoundry WithProperty(this IWorkflowFoundry foundry, string key, object? value)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            foundry.Properties[key] = value;
            return foundry;
        }

        /// <summary>
        /// Executes all operations in the foundry.
        /// This is a convenience method that casts to WorkflowFoundry and calls ForgeAsync.
        /// </summary>
        /// <param name="foundry">The foundry to execute.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the execution.</returns>
        /// <exception cref="ArgumentNullException">Thrown when foundry is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when foundry is not a WorkflowFoundry instance.</exception>
        public static async Task ForgeAsync(this IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));

            if (foundry is WorkflowFoundry workflowFoundry)
            {
                await workflowFoundry.ForgeAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("Foundry must be a WorkflowFoundry instance to use ForgeAsync extension method.");
            }
        }

        /// <summary>
        /// Adds middleware to the foundry and returns the foundry for method chaining.
        /// </summary>
        /// <param name="foundry">The foundry to add middleware to.</param>
        /// <param name="middleware">The middleware to add.</param>
        /// <returns>The same foundry instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when foundry or middleware is null.</exception>
        public static IWorkflowFoundry WithMiddleware(this IWorkflowFoundry foundry, IWorkflowOperationMiddleware middleware)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (middleware == null) throw new ArgumentNullException(nameof(middleware));

            foundry.AddMiddleware(middleware);
            return foundry;
        }
    }
}