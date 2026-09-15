using System;
using System.Collections.Concurrent;
using WorkflowForge.Abstractions;
using WorkflowForge.Loggers;

namespace WorkflowForge.Services
{
    /// <summary>
    /// Thread-safe foundry service bag with dispose-on-replace and dispose-on-clear semantics.
    /// </summary>
    public sealed class FoundryServices : IFoundryServices
    {
        private readonly ConcurrentDictionary<string, object> _services = new();
        private readonly IWorkflowForgeLogger _logger;

        /// <summary>
        /// Initializes a new service bag.
        /// </summary>
        public FoundryServices()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new service bag that reports disposal failures to the supplied logger.
        /// </summary>
        /// <param name="logger">The logger used to report failures thrown by a service's <see cref="IDisposable.Dispose"/>.</param>
        public FoundryServices(IWorkflowForgeLogger? logger)
        {
            _logger = logger ?? NullLogger.Instance;
        }

        /// <inheritdoc />
        public void Set(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            while (true)
            {
                if (_services.TryGetValue(key, out var existing))
                {
                    if (ReferenceEquals(existing, value))
                        return;

                    if (_services.TryUpdate(key, value, existing))
                    {
                        DisposeIfNeeded(key, existing);
                        return;
                    }
                }
                else if (_services.TryAdd(key, value))
                {
                    return;
                }
            }
        }

        /// <inheritdoc />
        public bool TryGet<T>(string key, out T? value)
            where T : class
        {
            if (_services.TryGetValue(key, out var obj) && obj is T typed)
            {
                value = typed;
                return true;
            }

            value = null;
            return false;
        }

        /// <inheritdoc />
        public bool TryRemove(string key, out object? removed)
        {
            if (_services.TryRemove(key, out removed))
            {
                DisposeIfNeeded(key, removed);
                return true;
            }

            removed = null;
            return false;
        }

        /// <inheritdoc />
        public bool ContainsKey(string key) => _services.ContainsKey(key);

        /// <inheritdoc />
        public bool TryAdd(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            return _services.TryAdd(key, value);
        }

        /// <inheritdoc />
        public void DisposeAll()
        {
            while (!_services.IsEmpty)
            {
                foreach (var key in _services.Keys)
                {
                    if (_services.TryRemove(key, out var value))
                    {
                        DisposeIfNeeded(key, value);
                    }
                }
            }
        }

        private void DisposeIfNeeded(string key, object value)
        {
            if (value is not IDisposable disposable)
                return;

            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                // Disposal must not prevent remaining services from being torn down, but the
                // failure is a real teardown defect and must not be silent.
                _logger.LogWarning(ex, "Foundry service '{ServiceKey}' threw during disposal.", key);
            }
        }
    }
}
