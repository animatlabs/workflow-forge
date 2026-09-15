using System;

namespace WorkflowForge.Abstractions
{
    /// <summary>
    /// Foundry-scoped services and extension-owned objects with explicit lease lifetime.
    /// Values are disposed when the foundry lease ends, unlike
    /// <see cref="IWorkflowExecutionContext.Properties"/> which holds execution data only.
    /// </summary>
    public interface IFoundryServices
    {
        /// <summary>
        /// Stores a service, disposing any previous value registered under the same key when it implements <see cref="IDisposable"/>.
        /// </summary>
        void Set(string key, object value);

        /// <summary>
        /// Attempts to get a service by key.
        /// </summary>
        bool TryGet<T>(string key, out T? value)
            where T : class;

        /// <summary>
        /// Removes a service, disposing it when it implements <see cref="IDisposable"/>.
        /// The removed instance is returned already disposed.
        /// </summary>
        bool TryRemove(string key, out object? removed);

        /// <summary>
        /// Returns whether a key is registered.
        /// </summary>
        bool ContainsKey(string key);

        /// <summary>
        /// Adds a service if the key is not already present.
        /// A rejected value is not disposed and remains owned by the caller.
        /// </summary>
        /// <returns>True if the value was added; false if the key already existed.</returns>
        bool TryAdd(string key, object value);

        /// <summary>
        /// Disposes every registered service and empties the collection.
        /// Called by the foundry when its lease ends.
        /// </summary>
        void DisposeAll();
    }
}
