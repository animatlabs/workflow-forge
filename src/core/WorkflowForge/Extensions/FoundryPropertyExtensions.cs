using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Constants;
using WorkflowForge.Operations;

namespace WorkflowForge.Extensions
{
    /// <summary>
    /// Extension methods for IWorkflowFoundry providing property access, operation chaining, and fluent API helpers.
    /// This file focuses on property management and operation building.
    /// For middleware configuration, see <see cref="FoundryMiddlewareExtensions"/>.
    /// </summary>
    public static class FoundryPropertyExtensions
    {
        /// <summary>
        /// Sets correlation ID in foundry data for tracking across operations.
        /// </summary>
        public static void SetCorrelationId(this IWorkflowFoundry foundry, string correlationId)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            foundry.Properties[FoundryPropertyKeys.CorrelationId] = correlationId;
        }

        /// <summary>
        /// Gets correlation ID from foundry data.
        /// </summary>
        public static string? GetCorrelationId(this IWorkflowFoundry foundry)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            return foundry.Properties.TryGetValue(FoundryPropertyKeys.CorrelationId, out var correlationId)
                ? correlationId?.ToString()
                : null;
        }

        /// <summary>
        /// Sets parent workflow execution ID for nested workflow tracking.
        /// </summary>
        public static void SetParentWorkflowExecutionId(this IWorkflowFoundry foundry, string parentWorkflowExecutionId)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            foundry.Properties[FoundryPropertyKeys.ParentWorkflowExecutionId] = parentWorkflowExecutionId;
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
            if (operations.Any(op => op == null))
                throw new ArgumentException("Operations collection contains null elements.", nameof(operations));

            foreach (var operation in operations)
            {
                foundry.AddOperation(operation);
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
            if (operations.Any(op => op == null))
                throw new ArgumentException("Operations collection contains null elements.", nameof(operations));

            foreach (var operation in operations)
            {
                foundry.AddOperation(operation);
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
        /// <param name="restoreAction">Optional asynchronous restoration action for compensation.</param>
        /// <returns>The same foundry instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when foundry, name, or action is null.</exception>
        /// <exception cref="ArgumentException">Thrown when name is empty or whitespace.</exception>
        public static IWorkflowFoundry WithOperation(this IWorkflowFoundry foundry, string name, Func<IWorkflowFoundry, Task> action, Func<IWorkflowFoundry, Task>? restoreAction = null)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Operation name cannot be null, empty, or whitespace.", nameof(name));
            if (action == null) throw new ArgumentNullException(nameof(action));

            Func<object?, IWorkflowFoundry, CancellationToken, Task>? adaptedRestore = restoreAction != null
                ? async (output, f, ct) => await restoreAction(f).ConfigureAwait(false)
                : (Func<object?, IWorkflowFoundry, CancellationToken, Task>?)null;

            var operation = new DelegateWorkflowOperation(name, async (input, f, ct) =>
            {
                await action(f).ConfigureAwait(false);
                return input; // Pass through input data
            }, adaptedRestore);

            foundry.AddOperation(operation);
            return foundry;
        }

        /// <summary>
        /// Adds an inline synchronous operation to the foundry using a lambda expression.
        /// </summary>
        /// <param name="foundry">The foundry to add the operation to.</param>
        /// <param name="name">The name of the operation.</param>
        /// <param name="action">The synchronous action to execute. Takes the foundry as parameter.</param>
        /// <param name="restoreAction">Optional synchronous restoration action for compensation.</param>
        /// <returns>The same foundry instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when foundry, name, or action is null.</exception>
        /// <exception cref="ArgumentException">Thrown when name is empty or whitespace.</exception>
        public static IWorkflowFoundry WithOperation(this IWorkflowFoundry foundry, string name, Action<IWorkflowFoundry> action, Action<IWorkflowFoundry>? restoreAction = null)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Operation name cannot be null, empty, or whitespace.", nameof(name));
            if (action == null) throw new ArgumentNullException(nameof(action));

            Func<object?, IWorkflowFoundry, CancellationToken, Task>? adaptedRestore = restoreAction != null
                ? (output, f, ct) => { restoreAction(f); return Task.CompletedTask; }
            : (Func<object?, IWorkflowFoundry, CancellationToken, Task>?)null;

            var operation = new DelegateWorkflowOperation(name, (input, f, ct) =>
            {
                action(f);
                return Task.FromResult(input); // Pass through input data
            }, adaptedRestore);

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

        #region Operation Output Access (Orchestrator-Level API)

        /// <summary>
        /// Gets the output of a specific operation by its position in the workflow.
        /// This is an orchestrator-level API for workflow composition and test inspection.
        /// Operations should NOT use this to read other operations' outputs -- use the
        /// input parameter of ForgeAsyncCore or foundry.Properties with domain-specific keys instead.
        /// </summary>
        /// <param name="foundry">The foundry to read from.</param>
        /// <param name="operationIndex">The zero-based index of the operation in the workflow.</param>
        /// <param name="operationName">The name of the operation at that index.</param>
        /// <returns>The operation output, or null if not found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when foundry is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when operationIndex is negative.</exception>
        /// <exception cref="ArgumentException">Thrown when operationName is null or empty.</exception>
        public static object? GetOperationOutput(this IWorkflowFoundry foundry, int operationIndex, string operationName)
        {
            if (foundry == null) throw new ArgumentNullException(nameof(foundry));
            if (operationIndex < 0) throw new ArgumentOutOfRangeException(nameof(operationIndex), "Operation index must be non-negative.");
            if (string.IsNullOrWhiteSpace(operationName)) throw new ArgumentException("Operation name cannot be null or empty.", nameof(operationName));

            var key = string.Format(FoundryPropertyKeys.OperationOutputFormat, operationIndex, operationName);
            return foundry.Properties.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// Gets the strongly-typed output of a specific operation by its position in the workflow.
        /// This is an orchestrator-level API for workflow composition and test inspection.
        /// Operations should NOT use this to read other operations' outputs -- use the
        /// input parameter of ForgeAsyncCore or foundry.Properties with domain-specific keys instead.
        /// </summary>
        /// <typeparam name="T">The expected output type.</typeparam>
        /// <param name="foundry">The foundry to read from.</param>
        /// <param name="operationIndex">The zero-based index of the operation in the workflow.</param>
        /// <param name="operationName">The name of the operation at that index.</param>
        /// <returns>The operation output cast to <typeparamref name="T"/>, or default if not found or wrong type.</returns>
        /// <exception cref="ArgumentNullException">Thrown when foundry is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when operationIndex is negative.</exception>
        /// <exception cref="ArgumentException">Thrown when operationName is null or empty.</exception>
        public static T? GetOperationOutput<T>(this IWorkflowFoundry foundry, int operationIndex, string operationName)
        {
            var output = GetOperationOutput(foundry, operationIndex, operationName);
            return output is T typed ? typed : default;
        }

        #endregion Operation Output Access (Orchestrator-Level API)
    }
}