using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Extensions.Logging.Serilog
{
    /// <summary>
    /// Adapts <see cref="Microsoft.Extensions.Logging.ILogger"/> to <see cref="IWorkflowForgeLogger"/>,
    /// allowing WorkflowForge to participate in the host application's logging pipeline.
    /// </summary>
    internal sealed class MelWorkflowForgeLogger : IWorkflowForgeLogger
    {
        private readonly Microsoft.Extensions.Logging.ILogger _logger;

        internal MelWorkflowForgeLogger(Microsoft.Extensions.Logging.ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void LogTrace(string message, params object[] args)
            => _logger.LogTrace(message, args);

        public void LogTrace(Exception exception, string message, params object[] args)
            => _logger.LogTrace(exception, message, args);

        public void LogTrace(IDictionary<string, string> properties, string message, params object[] args)
        {
            using (PushScope(properties))
                _logger.LogTrace(message, args);
        }

        public void LogTrace(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        {
            using (PushScope(properties))
                _logger.LogTrace(exception, message, args);
        }

        public void LogDebug(string message, params object[] args)
            => _logger.LogDebug(message, args);

        public void LogDebug(Exception exception, string message, params object[] args)
            => _logger.LogDebug(exception, message, args);

        public void LogDebug(IDictionary<string, string> properties, string message, params object[] args)
        {
            using (PushScope(properties))
                _logger.LogDebug(message, args);
        }

        public void LogDebug(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        {
            using (PushScope(properties))
                _logger.LogDebug(exception, message, args);
        }

        public void LogInformation(string message, params object[] args)
            => _logger.LogInformation(message, args);

        public void LogInformation(Exception exception, string message, params object[] args)
            => _logger.LogInformation(exception, message, args);

        public void LogInformation(IDictionary<string, string> properties, string message, params object[] args)
        {
            using (PushScope(properties))
                _logger.LogInformation(message, args);
        }

        public void LogInformation(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        {
            using (PushScope(properties))
                _logger.LogInformation(exception, message, args);
        }

        public void LogWarning(string message, params object[] args)
            => _logger.LogWarning(message, args);

        public void LogWarning(Exception exception, string message, params object[] args)
            => _logger.LogWarning(exception, message, args);

        public void LogWarning(IDictionary<string, string> properties, string message, params object[] args)
        {
            using (PushScope(properties))
                _logger.LogWarning(message, args);
        }

        public void LogWarning(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        {
            using (PushScope(properties))
                _logger.LogWarning(exception, message, args);
        }

        public void LogError(string message, params object[] args)
            => _logger.LogError(message, args);

        public void LogError(Exception exception, string message, params object[] args)
            => _logger.LogError(exception, message, args);

        public void LogError(IDictionary<string, string> properties, string message, params object[] args)
        {
            using (PushScope(properties))
                _logger.LogError(message, args);
        }

        public void LogError(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        {
            using (PushScope(properties))
                _logger.LogError(exception, message, args);
        }

        public void LogCritical(string message, params object[] args)
            => _logger.LogCritical(message, args);

        public void LogCritical(Exception exception, string message, params object[] args)
            => _logger.LogCritical(exception, message, args);

        public void LogCritical(IDictionary<string, string> properties, string message, params object[] args)
        {
            using (PushScope(properties))
                _logger.LogCritical(message, args);
        }

        public void LogCritical(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        {
            using (PushScope(properties))
                _logger.LogCritical(exception, message, args);
        }

        public IDisposable BeginScope<TState>(TState state, IDictionary<string, string>? properties = null)
        {
            return PushScope(properties);
        }

        private IDisposable PushScope(IDictionary<string, string>? properties)
        {
            if (properties == null || properties.Count == 0)
                return NoOpDisposable.Instance;

            return _logger.BeginScope(properties) ?? NoOpDisposable.Instance;
        }

        private sealed class NoOpDisposable : IDisposable
        {
            internal static readonly NoOpDisposable Instance = new NoOpDisposable();

            public void Dispose()
            { }
        }
    }
}
