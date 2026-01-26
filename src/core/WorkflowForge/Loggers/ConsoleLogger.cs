using System;
using System.Collections.Generic;
using System.Linq;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Loggers
{
    /// <summary>
    /// A simple console logger implementation for WorkflowForge core.
    /// For full structured logging with scope support, use WorkflowForge.Extensions.Logging.Serilog.
    /// </summary>
    public sealed class ConsoleLogger : IWorkflowForgeLogger
    {
        private readonly string _prefix;
        private readonly ISystemTimeProvider _timeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleLogger"/> class.
        /// </summary>
        /// <param name="prefix">Optional prefix for log messages.</param>
        /// <param name="timeProvider">The time provider to use for timestamps.</param>
        public ConsoleLogger(string prefix = "WorkflowForge", ISystemTimeProvider? timeProvider = null)
        {
            _prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
            _timeProvider = timeProvider ?? SystemTimeProvider.Instance;
        }

        /// <inheritdoc />
        public void LogTrace(string message, params object[] args)
        {
            WriteToConsole("TRACE", FormatMessage(message, args), ConsoleColor.DarkGray);
        }

        /// <inheritdoc />
        public void LogTrace(Exception exception, string message, params object[] args)
        {
            WriteToConsole("TRACE", $"{FormatMessage(message, args)} | Exception: {exception}", ConsoleColor.DarkGray);
        }

        /// <inheritdoc />
        public void LogTrace(IDictionary<string, string> properties, string message, params object[] args)
        {
            WriteToConsole("TRACE", FormatMessageWithProperties(message, properties, args), ConsoleColor.DarkGray);
        }

        /// <inheritdoc />
        public void LogTrace(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        {
            WriteToConsole("TRACE", $"{FormatMessageWithProperties(message, properties, args)} | Exception: {exception}", ConsoleColor.DarkGray);
        }

        /// <inheritdoc />
        public void LogDebug(string message, params object[] args)
        {
            WriteToConsole("DEBUG", FormatMessage(message, args), ConsoleColor.Gray);
        }

        /// <inheritdoc />
        public void LogDebug(Exception exception, string message, params object[] args)
        {
            WriteToConsole("DEBUG", $"{FormatMessage(message, args)} | Exception: {exception}", ConsoleColor.Gray);
        }

        /// <inheritdoc />
        public void LogDebug(IDictionary<string, string> properties, string message, params object[] args)
        {
            WriteToConsole("DEBUG", FormatMessageWithProperties(message, properties, args), ConsoleColor.Gray);
        }

        /// <inheritdoc />
        public void LogDebug(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        {
            WriteToConsole("DEBUG", $"{FormatMessageWithProperties(message, properties, args)} | Exception: {exception}", ConsoleColor.Gray);
        }

        /// <inheritdoc />
        public void LogInformation(string message, params object[] args)
        {
            WriteToConsole("INFO", FormatMessage(message, args), ConsoleColor.White);
        }

        /// <inheritdoc />
        public void LogInformation(Exception exception, string message, params object[] args)
        {
            WriteToConsole("INFO", $"{FormatMessage(message, args)} | Exception: {exception}", ConsoleColor.White);
        }

        /// <inheritdoc />
        public void LogInformation(IDictionary<string, string> properties, string message, params object[] args)
        {
            WriteToConsole("INFO", FormatMessageWithProperties(message, properties, args), ConsoleColor.White);
        }

        /// <inheritdoc />
        public void LogInformation(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        {
            WriteToConsole("INFO", $"{FormatMessageWithProperties(message, properties, args)} | Exception: {exception}", ConsoleColor.White);
        }

        /// <inheritdoc />
        public void LogWarning(string message, params object[] args)
        {
            WriteToConsole("WARN", FormatMessage(message, args), ConsoleColor.Yellow);
        }

        /// <inheritdoc />
        public void LogWarning(Exception exception, string message, params object[] args)
        {
            WriteToConsole("WARN", $"{FormatMessage(message, args)} | Exception: {exception}", ConsoleColor.Yellow);
        }

        /// <inheritdoc />
        public void LogWarning(IDictionary<string, string> properties, string message, params object[] args)
        {
            WriteToConsole("WARN", FormatMessageWithProperties(message, properties, args), ConsoleColor.Yellow);
        }

        /// <inheritdoc />
        public void LogWarning(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        {
            WriteToConsole("WARN", $"{FormatMessageWithProperties(message, properties, args)} | Exception: {exception}", ConsoleColor.Yellow);
        }

        /// <inheritdoc />
        public void LogError(string message, params object[] args)
        {
            WriteToConsole("ERROR", FormatMessage(message, args), ConsoleColor.Red);
        }

        /// <inheritdoc />
        public void LogError(Exception exception, string message, params object[] args)
        {
            WriteToConsole("ERROR", $"{FormatMessage(message, args)} | Exception: {exception}", ConsoleColor.Red);
        }

        /// <inheritdoc />
        public void LogError(IDictionary<string, string> properties, string message, params object[] args)
        {
            WriteToConsole("ERROR", FormatMessageWithProperties(message, properties, args), ConsoleColor.Red);
        }

        /// <inheritdoc />
        public void LogError(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        {
            WriteToConsole("ERROR", $"{FormatMessageWithProperties(message, properties, args)} | Exception: {exception}", ConsoleColor.Red);
        }

        /// <inheritdoc />
        public void LogCritical(string message, params object[] args)
        {
            WriteToConsole("CRITICAL", FormatMessage(message, args), ConsoleColor.Magenta);
        }

        /// <inheritdoc />
        public void LogCritical(Exception exception, string message, params object[] args)
        {
            WriteToConsole("CRITICAL", $"{FormatMessage(message, args)} | Exception: {exception}", ConsoleColor.Magenta);
        }

        /// <inheritdoc />
        public void LogCritical(IDictionary<string, string> properties, string message, params object[] args)
        {
            WriteToConsole("CRITICAL", FormatMessageWithProperties(message, properties, args), ConsoleColor.Magenta);
        }

        /// <inheritdoc />
        public void LogCritical(IDictionary<string, string> properties, Exception exception, string message, params object[] args)
        {
            WriteToConsole("CRITICAL", $"{FormatMessageWithProperties(message, properties, args)} | Exception: {exception}", ConsoleColor.Magenta);
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state, IDictionary<string, string>? properties = null)
        {
            // ConsoleLogger doesn't support real scope management.
            // For full scope support, use Serilog extension.
            return EmptyDisposable.Instance;
        }

        private string FormatMessage(string message, params object[] args)
        {
            try
            {
                return args?.Length > 0 ? string.Format(message, args) : message;
            }
            catch
            {
                // Fallback if formatting fails
                return message;
            }
        }

        private string FormatMessageWithProperties(string message, IDictionary<string, string>? properties, params object[] args)
        {
            var formattedMessage = FormatMessage(message, args);

            if (properties != null && properties.Count > 0)
            {
                var propertiesString = string.Join(", ", properties.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                return $"{formattedMessage} | Properties: {propertiesString}";
            }

            return formattedMessage;
        }

        private void WriteToConsole(string level, string message, ConsoleColor color)
        {
            var timestamp = _timeProvider.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var originalColor = Console.ForegroundColor;

            try
            {
                Console.ForegroundColor = color;
                Console.WriteLine($"[{timestamp}] [{_prefix}] [{level}] {message}");
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }

        /// <summary>
        /// Empty disposable for scope operations that don't require cleanup.
        /// </summary>
        private sealed class EmptyDisposable : IDisposable
        {
            public static readonly EmptyDisposable Instance = new();

            private EmptyDisposable()
            { }

            public void Dispose()
            { }
        }
    }
}