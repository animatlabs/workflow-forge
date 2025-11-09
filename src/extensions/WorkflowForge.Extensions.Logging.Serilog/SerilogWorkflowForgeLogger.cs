using Serilog.Context;
using Serilog.Core;
using Serilog.Core.Enrichers;
using System;
using System.Collections.Generic;
using WorkflowForge.Abstractions;
using ILogger = Serilog.ILogger;

namespace WorkflowForge.Extensions.Logging.Serilog
{
    /// <summary>
    /// Serilog adapter for WorkflowForge logging that implements IWorkflowForgeLogger.
    /// </summary>
    public sealed class SerilogWorkflowForgeLogger : IWorkflowForgeLogger
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogWorkflowForgeLogger"/> class.
        /// </summary>
        /// <param name="logger">The Serilog ILogger instance.</param>
        public SerilogWorkflowForgeLogger(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

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

        private IDisposable PushProperties(IDictionary<string, string>? properties)
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