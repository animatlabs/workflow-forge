using System;
using System.Collections.Generic;
using Serilog.Context;
using Serilog.Core;
using Serilog.Core.Enrichers;
using Serilog.Events;
using WorkflowForge.Abstractions;
using WorkflowForge.Operations;
using ILogger = Serilog.ILogger;

namespace WorkflowForge.Extensions.Logging.Serilog
{
    /// <summary>
    /// Serilog adapter for WorkflowForge logging that implements IWorkflowForgeLogger.
    /// </summary>
    internal sealed class SerilogWorkflowForgeLogger : IWorkflowForgeLogger, IDisposable
    {
        private readonly ILogger _logger;
        private readonly bool _ownsLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogWorkflowForgeLogger"/> class.
        /// </summary>
        /// <param name="logger">The Serilog ILogger instance.</param>
        /// <param name="ownsLogger">Whether disposing this adapter should dispose <paramref name="logger"/>.</param>
        public SerilogWorkflowForgeLogger(ILogger logger, bool ownsLogger = false)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ownsLogger = ownsLogger;
        }

        /// <summary>
        /// Disposes the underlying Serilog logger when this adapter created it.
        /// </summary>
        public void Dispose()
        {
            if (_ownsLogger && _logger is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        /// <inheritdoc />
        public bool IsEnabled(WorkflowForgeLogLevel level)
            => _logger.IsEnabled(WorkflowForgeSerilogLogLevelMapper.ToSerilogLevel(level));

        /// <inheritdoc />
        public void LogTrace(string message, params object[] args)
        {
            _logger.Verbose(message, args);
        }

        /// <inheritdoc />
        public void LogTrace(Exception exception, string message, params object[] args)
        {
            _logger.Verbose(exception, message, args);
        }

        /// <inheritdoc />
        public void LogTrace(IDictionary<string, string> properties, string message, params object[] args)
        {
            using (PushProperties(properties))
            {
                _logger.Verbose(message, args);
            }
        }

        /// <inheritdoc />
        public void LogTrace(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        {
            using (PushProperties(properties))
            {
                _logger.Verbose(exception, message, args);
            }
        }

        /// <inheritdoc />
        public void LogDebug(string message, params object[] args)
        {
            _logger.Debug(message, args);
        }

        /// <inheritdoc />
        public void LogDebug(Exception exception, string message, params object[] args)
        {
            _logger.Debug(exception, message, args);
        }

        /// <inheritdoc />
        public void LogDebug(IDictionary<string, string> properties, string message, params object[] args)
        {
            using (PushProperties(properties))
            {
                _logger.Debug(message, args);
            }
        }

        /// <inheritdoc />
        public void LogDebug(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        {
            using (PushProperties(properties))
            {
                _logger.Debug(exception, message, args);
            }
        }

        /// <inheritdoc />
        public void LogInformation(string message, params object[] args)
        {
            _logger.Information(message, args);
        }

        /// <inheritdoc />
        public void LogInformation(Exception exception, string message, params object[] args)
        {
            _logger.Information(exception, message, args);
        }

        /// <inheritdoc />
        public void LogInformation(IDictionary<string, string> properties, string message, params object[] args)
        {
            using (PushProperties(properties))
            {
                _logger.Information(message, args);
            }
        }

        /// <inheritdoc />
        public void LogInformation(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        {
            using (PushProperties(properties))
            {
                _logger.Information(exception, message, args);
            }
        }

        /// <inheritdoc />
        public void LogWarning(string message, params object[] args)
        {
            _logger.Warning(message, args);
        }

        /// <inheritdoc />
        public void LogWarning(Exception exception, string message, params object[] args)
        {
            _logger.Warning(exception, message, args);
        }

        /// <inheritdoc />
        public void LogWarning(IDictionary<string, string> properties, string message, params object[] args)
        {
            using (PushProperties(properties))
            {
                _logger.Warning(message, args);
            }
        }

        /// <inheritdoc />
        public void LogWarning(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        {
            using (PushProperties(properties))
            {
                _logger.Warning(exception, message, args);
            }
        }

        /// <inheritdoc />
        public void LogError(string message, params object[] args)
        {
            _logger.Error(message, args);
        }

        /// <inheritdoc />
        public void LogError(Exception exception, string message, params object[] args)
        {
            _logger.Error(exception, message, args);
        }

        /// <inheritdoc />
        public void LogError(IDictionary<string, string> properties, string message, params object[] args)
        {
            using (PushProperties(properties))
            {
                _logger.Error(message, args);
            }
        }

        /// <inheritdoc />
        public void LogError(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        {
            using (PushProperties(properties))
            {
                _logger.Error(exception, message, args);
            }
        }

        /// <inheritdoc />
        public void LogCritical(string message, params object[] args)
        {
            _logger.Fatal(message, args);
        }

        /// <inheritdoc />
        public void LogCritical(Exception exception, string message, params object[] args)
        {
            _logger.Fatal(exception, message, args);
        }

        /// <inheritdoc />
        public void LogCritical(IDictionary<string, string> properties, string message, params object[] args)
        {
            using (PushProperties(properties))
            {
                _logger.Fatal(message, args);
            }
        }

        /// <inheritdoc />
        public void LogCritical(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        {
            using (PushProperties(properties))
            {
                _logger.Fatal(exception, message, args);
            }
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state, IDictionary<string, string>? properties = null)
        {
            return PushProperties(properties);
        }

        private static IDisposable PushProperties(IDictionary<string, string>? properties)
        {
            if (properties == null)
            {
                return LogContext.Push();
            }

            var enrichers = new List<ILogEventEnricher>();
            foreach (var property in properties)
            {
                enrichers.Add(new PropertyEnricher(property.Key, property.Value));
            }
            return LogContext.Push(enrichers.ToArray());
        }
    }
}
