using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Extensions.Logging.Serilog.Tests
{
    public class MelWorkflowForgeLoggerShould : IDisposable
    {
        private readonly IWorkflowForgeLogger _logger;
        private readonly List<string> _logged;
        private readonly ILoggerFactory _factory;

        public MelWorkflowForgeLoggerShould()
        {
            _logged = new List<string>();
            _factory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                builder.AddProvider(new CapturingLoggerProvider(_logged));
            });
            _logger = SerilogLoggerFactory.CreateLogger(_factory);
        }

        public void Dispose()
        {
            _factory.Dispose();
        }

        [Fact]
        public void RouteTraceMessage_GivenLogTrace()
        {
            _logger.LogTrace("trace {0}", "arg");

            Assert.Single(_logged);
            Assert.Contains("trace", _logged[0]);
        }

        [Fact]
        public void RouteTraceWithException_GivenLogTraceWithException()
        {
            _logger.LogTrace(new InvalidOperationException("boom"), "trace-ex {0}", 1);

            Assert.Single(_logged);
            Assert.Contains("trace-ex", _logged[0]);
        }

        [Fact]
        public void RouteTraceWithProperties_GivenLogTraceWithProperties()
        {
            var props = new Dictionary<string, string> { ["k"] = "v" };

            _logger.LogTrace(props, "trace-props {0}", 1);

            Assert.Single(_logged);
            Assert.Contains("trace-props", _logged[0]);
        }

        [Fact]
        public void RouteTraceWithPropertiesAndException_GivenLogTraceWithPropertiesAndException()
        {
            var props = new Dictionary<string, string> { ["k"] = "v" };

            _logger.LogTrace(props, new Exception("e"), "trace-both {0}", 1);

            Assert.Single(_logged);
            Assert.Contains("trace-both", _logged[0]);
        }

        [Fact]
        public void RouteDebugMessage_GivenLogDebug()
        {
            _logger.LogDebug("debug {0}", "arg");

            Assert.Single(_logged);
            Assert.Contains("debug", _logged[0]);
        }

        [Fact]
        public void RouteDebugWithException_GivenLogDebugWithException()
        {
            _logger.LogDebug(new Exception("e"), "debug-ex {0}", 1);

            Assert.Single(_logged);
            Assert.Contains("debug-ex", _logged[0]);
        }

        [Fact]
        public void RouteDebugWithProperties_GivenLogDebugWithProperties()
        {
            var props = new Dictionary<string, string> { ["k"] = "v" };

            _logger.LogDebug(props, "debug-props {0}", 1);

            Assert.Single(_logged);
            Assert.Contains("debug-props", _logged[0]);
        }

        [Fact]
        public void RouteDebugWithPropertiesAndException_GivenLogDebugWithPropertiesAndException()
        {
            var props = new Dictionary<string, string> { ["k"] = "v" };

            _logger.LogDebug(props, new Exception("e"), "debug-both {0}", 1);

            Assert.Single(_logged);
            Assert.Contains("debug-both", _logged[0]);
        }

        [Fact]
        public void RouteInformationMessage_GivenLogInformation()
        {
            _logger.LogInformation("info {0}", "arg");

            Assert.Single(_logged);
            Assert.Contains("info", _logged[0]);
        }

        [Fact]
        public void RouteInformationWithException_GivenLogInformationWithException()
        {
            _logger.LogInformation(new Exception("e"), "info-ex {0}", 1);

            Assert.Single(_logged);
            Assert.Contains("info-ex", _logged[0]);
        }

        [Fact]
        public void RouteInformationWithProperties_GivenLogInformationWithProperties()
        {
            var props = new Dictionary<string, string> { ["k"] = "v" };

            _logger.LogInformation(props, "info-props {0}", 1);

            Assert.Single(_logged);
            Assert.Contains("info-props", _logged[0]);
        }

        [Fact]
        public void RouteInformationWithPropertiesAndException_GivenLogInformationWithPropertiesAndException()
        {
            var props = new Dictionary<string, string> { ["k"] = "v" };

            _logger.LogInformation(props, new Exception("e"), "info-both {0}", 1);

            Assert.Single(_logged);
            Assert.Contains("info-both", _logged[0]);
        }

        [Fact]
        public void RouteWarningMessage_GivenLogWarning()
        {
            _logger.LogWarning("warn {0}", "arg");

            Assert.Single(_logged);
            Assert.Contains("warn", _logged[0]);
        }

        [Fact]
        public void RouteWarningWithException_GivenLogWarningWithException()
        {
            _logger.LogWarning(new Exception("e"), "warn-ex {0}", 1);

            Assert.Single(_logged);
            Assert.Contains("warn-ex", _logged[0]);
        }

        [Fact]
        public void RouteWarningWithProperties_GivenLogWarningWithProperties()
        {
            var props = new Dictionary<string, string> { ["k"] = "v" };

            _logger.LogWarning(props, "warn-props {0}", 1);

            Assert.Single(_logged);
            Assert.Contains("warn-props", _logged[0]);
        }

        [Fact]
        public void RouteWarningWithPropertiesAndException_GivenLogWarningWithPropertiesAndException()
        {
            var props = new Dictionary<string, string> { ["k"] = "v" };

            _logger.LogWarning(props, new Exception("e"), "warn-both {0}", 1);

            Assert.Single(_logged);
            Assert.Contains("warn-both", _logged[0]);
        }

        [Fact]
        public void RouteErrorMessage_GivenLogError()
        {
            _logger.LogError("error {0}", "arg");

            Assert.Single(_logged);
            Assert.Contains("error", _logged[0]);
        }

        [Fact]
        public void RouteErrorWithException_GivenLogErrorWithException()
        {
            _logger.LogError(new Exception("e"), "error-ex {0}", 1);

            Assert.Single(_logged);
            Assert.Contains("error-ex", _logged[0]);
        }

        [Fact]
        public void RouteErrorWithProperties_GivenLogErrorWithProperties()
        {
            var props = new Dictionary<string, string> { ["k"] = "v" };

            _logger.LogError(props, "error-props {0}", 1);

            Assert.Single(_logged);
            Assert.Contains("error-props", _logged[0]);
        }

        [Fact]
        public void RouteErrorWithPropertiesAndException_GivenLogErrorWithPropertiesAndException()
        {
            var props = new Dictionary<string, string> { ["k"] = "v" };

            _logger.LogError(props, new Exception("e"), "error-both {0}", 1);

            Assert.Single(_logged);
            Assert.Contains("error-both", _logged[0]);
        }

        [Fact]
        public void RouteCriticalMessage_GivenLogCritical()
        {
            _logger.LogCritical("critical {0}", "arg");

            Assert.Single(_logged);
            Assert.Contains("critical", _logged[0]);
        }

        [Fact]
        public void RouteCriticalWithException_GivenLogCriticalWithException()
        {
            _logger.LogCritical(new Exception("e"), "critical-ex {0}", 1);

            Assert.Single(_logged);
            Assert.Contains("critical-ex", _logged[0]);
        }

        [Fact]
        public void RouteCriticalWithProperties_GivenLogCriticalWithProperties()
        {
            var props = new Dictionary<string, string> { ["k"] = "v" };

            _logger.LogCritical(props, "critical-props {0}", 1);

            Assert.Single(_logged);
            Assert.Contains("critical-props", _logged[0]);
        }

        [Fact]
        public void RouteCriticalWithPropertiesAndException_GivenLogCriticalWithPropertiesAndException()
        {
            var props = new Dictionary<string, string> { ["k"] = "v" };

            _logger.LogCritical(props, new Exception("e"), "critical-both {0}", 1);

            Assert.Single(_logged);
            Assert.Contains("critical-both", _logged[0]);
        }

        [Fact]
        public void ReturnNoOpDisposable_GivenBeginScopeWithNullProperties()
        {
            var scope = _logger.BeginScope("state", null);

            Assert.NotNull(scope);
            scope.Dispose();
        }

        [Fact]
        public void ReturnNoOpDisposable_GivenBeginScopeWithEmptyProperties()
        {
            var scope = _logger.BeginScope("state", new Dictionary<string, string>());

            Assert.NotNull(scope);
            scope.Dispose();
        }

        [Fact]
        public void ReturnDisposable_GivenBeginScopeWithProperties()
        {
            var props = new Dictionary<string, string> { ["k"] = "v" };
            var scope = _logger.BeginScope("state", props);

            Assert.NotNull(scope);
            scope.Dispose();
        }

        private sealed class CapturingLoggerProvider : ILoggerProvider
        {
            private readonly List<string> _logged;

            public CapturingLoggerProvider(List<string> logged) => _logged = logged;

            public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
                => new CapturingLogger(_logged);

            public void Dispose() { }
        }

        private sealed class CapturingLogger : Microsoft.Extensions.Logging.ILogger
        {
            private readonly List<string> _logged;

            public CapturingLogger(List<string> logged) => _logged = logged;

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull
                => new ScopeDisposable();

            public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

            public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                _logged.Add(formatter(state, exception));
            }

            private sealed class ScopeDisposable : IDisposable
            {
                public void Dispose() { }
            }
        }
    }
}
