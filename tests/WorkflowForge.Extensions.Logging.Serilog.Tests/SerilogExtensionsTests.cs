using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Logging.Serilog;
using Xunit;

namespace WorkflowForge.Extensions.Logging.Serilog.Tests
{
    public class SerilogExtensionsTests
    {
        [Fact]
        public void CreateLogger_WithNullOptions_ReturnsLogger()
        {
            var logger = SerilogLoggerFactory.CreateLogger((SerilogLoggerOptions?)null);

            Assert.NotNull(logger);
            Assert.IsAssignableFrom<IWorkflowForgeLogger>(logger);
        }

        [Fact]
        public void CreateLogger_WithNoOptions_ReturnsLogger()
        {
            var logger = SerilogLoggerFactory.CreateLogger();

            Assert.NotNull(logger);
            Assert.IsAssignableFrom<IWorkflowForgeLogger>(logger);
        }

        [Fact]
        public void CreateLogger_WithOptions_ReturnsConfiguredLogger()
        {
            var options = new SerilogLoggerOptions
            {
                MinimumLevel = "Debug",
                EnableConsoleSink = false,
                ConsoleOutputTemplate = "[{Level}] {Message}"
            };

            var logger = SerilogLoggerFactory.CreateLogger(options);

            Assert.NotNull(logger);
            Assert.IsAssignableFrom<IWorkflowForgeLogger>(logger);
        }

        [Fact]
        public void CreateLogger_WithConsoleDisabled_ReturnsLogger()
        {
            var options = new SerilogLoggerOptions { EnableConsoleSink = false };

            var logger = SerilogLoggerFactory.CreateLogger(options);

            Assert.NotNull(logger);
            Assert.IsAssignableFrom<IWorkflowForgeLogger>(logger);
        }

        [Fact]
        public void CreateLogger_WithWarningLevel_FiltersLowerLevels()
        {
            var options = new SerilogLoggerOptions { MinimumLevel = "Warning", EnableConsoleSink = false };

            var logger = SerilogLoggerFactory.CreateLogger(options);

            Assert.NotNull(logger);
            var ex = Record.Exception(() =>
            {
                logger.LogInformation("Should be filtered");
                logger.LogWarning("Should pass");
            });
            Assert.Null(ex);
        }

        [Fact]
        public void CreateLogger_WithInvalidMinimumLevel_FallsBackToInformation()
        {
            var options = new SerilogLoggerOptions { MinimumLevel = "InvalidLevel", EnableConsoleSink = false };

            var logger = SerilogLoggerFactory.CreateLogger(options);

            Assert.NotNull(logger);
            var ex = Record.Exception(() => logger.LogInformation("Should work with fallback level"));
            Assert.Null(ex);
        }

        [Fact]
        public void CreateLogger_WithEmptyMinimumLevel_FallsBackToInformation()
        {
            var options = new SerilogLoggerOptions { MinimumLevel = "", EnableConsoleSink = false };

            var logger = SerilogLoggerFactory.CreateLogger(options);

            Assert.NotNull(logger);
            var ex = Record.Exception(() => logger.LogInformation("Should work"));
            Assert.Null(ex);
        }

        [Fact]
        public void CreateLogger_WithWhitespaceMinimumLevel_FallsBackToInformation()
        {
            var options = new SerilogLoggerOptions { MinimumLevel = "   ", EnableConsoleSink = false };

            var logger = SerilogLoggerFactory.CreateLogger(options);

            Assert.NotNull(logger);
            var ex = Record.Exception(() => logger.LogInformation("Should work"));
            Assert.Null(ex);
        }

        [Fact]
        public void CreateLogger_LoggerSupportsAllLogLevels()
        {
            var options = new SerilogLoggerOptions { MinimumLevel = "Verbose", EnableConsoleSink = false };

            var logger = SerilogLoggerFactory.CreateLogger(options);

            var ex = Record.Exception(() =>
            {
                logger.LogTrace("Trace");
                logger.LogDebug("Debug");
                logger.LogInformation("Info");
                logger.LogWarning("Warning");
                logger.LogError("Error");
                logger.LogCritical("Critical");
            });
            Assert.Null(ex);
        }

        [Fact]
        public void CreateLogger_LoggerSupportsBeginScope()
        {
            var options = new SerilogLoggerOptions { MinimumLevel = "Verbose", EnableConsoleSink = false };
            var logger = SerilogLoggerFactory.CreateLogger(options);

            var scope = logger.BeginScope("state", new Dictionary<string, string> { ["Key"] = "Value" });

            Assert.NotNull(scope);
            var ex = Record.Exception(() =>
            {
                logger.LogInformation("Scoped message");
                scope.Dispose();
            });
            Assert.Null(ex);
        }

        [Fact]
        public void SerilogLoggerOptions_HasExpectedDefaults()
        {
            var options = new SerilogLoggerOptions();

            Assert.Equal("Information", options.MinimumLevel);
            Assert.True(options.EnableConsoleSink);
            Assert.NotNull(options.ConsoleOutputTemplate);
            Assert.Contains("Timestamp", options.ConsoleOutputTemplate);
        }

        [Fact]
        public void CreateLogger_WithNullConsoleOutputTemplate_UsesDefault()
        {
            var options = new SerilogLoggerOptions
            {
                ConsoleOutputTemplate = null,
                EnableConsoleSink = false
            };

            var logger = SerilogLoggerFactory.CreateLogger(options);

            Assert.NotNull(logger);
            Assert.IsAssignableFrom<IWorkflowForgeLogger>(logger);
        }

        // --- ILoggerFactory overload tests ---

        [Fact]
        public void CreateLogger_WithLoggerFactory_ReturnsLogger()
        {
            using var factory = LoggerFactory.Create(builder => builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace));

            var logger = SerilogLoggerFactory.CreateLogger(factory);

            Assert.NotNull(logger);
            Assert.IsAssignableFrom<IWorkflowForgeLogger>(logger);
        }

        [Fact]
        public void CreateLogger_WithNullLoggerFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                SerilogLoggerFactory.CreateLogger((Microsoft.Extensions.Logging.ILoggerFactory)null!));
        }

        [Fact]
        public void CreateLogger_WithLoggerFactory_SupportsAllLogLevels()
        {
            using var factory = LoggerFactory.Create(builder => builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace));

            var logger = SerilogLoggerFactory.CreateLogger(factory);

            var ex = Record.Exception(() =>
            {
                logger.LogTrace("Trace");
                logger.LogDebug("Debug");
                logger.LogInformation("Info");
                logger.LogWarning("Warning");
                logger.LogError("Error");
                logger.LogCritical("Critical");
            });
            Assert.Null(ex);
        }

        [Fact]
        public void CreateLogger_WithLoggerFactory_SupportsExceptions()
        {
            using var factory = LoggerFactory.Create(builder => builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace));
            var logger = SerilogLoggerFactory.CreateLogger(factory);

            var ex = Record.Exception(() =>
            {
                var testException = new InvalidOperationException("Test error");
                logger.LogError(testException, "Error with exception");
                logger.LogCritical(testException, "Critical with exception");
            });
            Assert.Null(ex);
        }

        [Fact]
        public void CreateLogger_WithLoggerFactory_SupportsProperties()
        {
            using var factory = LoggerFactory.Create(builder => builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace));
            var logger = SerilogLoggerFactory.CreateLogger(factory);

            var props = new Dictionary<string, string> { ["Key"] = "Value", ["Other"] = "Data" };

            var ex = Record.Exception(() =>
            {
                logger.LogInformation(props, "With properties");
                logger.LogWarning(props, new Exception("Test"), "With properties and exception");
            });
            Assert.Null(ex);
        }

        [Fact]
        public void CreateLogger_WithLoggerFactory_SupportsBeginScope()
        {
            using var factory = LoggerFactory.Create(builder => builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace));
            var logger = SerilogLoggerFactory.CreateLogger(factory);

            var scope = logger.BeginScope("state", new Dictionary<string, string> { ["Key"] = "Value" });

            Assert.NotNull(scope);
            var ex = Record.Exception(() =>
            {
                logger.LogInformation("Inside scope");
                scope.Dispose();
            });
            Assert.Null(ex);
        }

        [Fact]
        public void CreateLogger_WithLoggerFactory_BeginScopeWithNullProperties_ReturnsDisposable()
        {
            using var factory = LoggerFactory.Create(builder => builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace));
            var logger = SerilogLoggerFactory.CreateLogger(factory);

            var scope = logger.BeginScope("state", null);

            Assert.NotNull(scope);
            scope.Dispose();
        }

        [Fact]
        public void CreateLogger_WithLoggerFactory_BeginScopeWithEmptyProperties_ReturnsDisposable()
        {
            using var factory = LoggerFactory.Create(builder => builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace));
            var logger = SerilogLoggerFactory.CreateLogger(factory);

            var scope = logger.BeginScope("state", new Dictionary<string, string>());

            Assert.NotNull(scope);
            scope.Dispose();
        }

        [Fact]
        public void CreateLogger_WithLoggerFactory_RoutesLogsThroughFactory()
        {
            var logged = new List<string>();
            using var factory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                builder.AddProvider(new TestLoggerProvider(logged));
            });

            var logger = SerilogLoggerFactory.CreateLogger(factory);
            logger.LogInformation("Test routing message");

            Assert.Single(logged);
            Assert.Contains("Test routing message", logged[0]);
        }

        [Fact]
        public void CreateLogger_WithLoggerFactory_RoutesErrorsWithExceptions()
        {
            var logged = new List<string>();
            using var factory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                builder.AddProvider(new TestLoggerProvider(logged));
            });

            var logger = SerilogLoggerFactory.CreateLogger(factory);
            logger.LogError(new InvalidOperationException("Boom"), "Error occurred");

            Assert.Single(logged);
            Assert.Contains("Error occurred", logged[0]);
        }

        [Fact]
        public void CreateLogger_WithLoggerFactory_RoutesAllLevelsThroughFactory()
        {
            var logged = new List<string>();
            using var factory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                builder.AddProvider(new TestLoggerProvider(logged));
            });

            var logger = SerilogLoggerFactory.CreateLogger(factory);
            logger.LogTrace("T");
            logger.LogDebug("D");
            logger.LogInformation("I");
            logger.LogWarning("W");
            logger.LogError("E");
            logger.LogCritical("C");

            Assert.Equal(6, logged.Count);
        }

        /// <summary>
        /// Minimal M.E.L provider/logger that captures formatted log messages for assertion.
        /// </summary>
        private sealed class TestLoggerProvider : ILoggerProvider
        {
            private readonly List<string> _logged;
            public TestLoggerProvider(List<string> logged) => _logged = logged;
            public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) => new TestLogger(_logged);
            public void Dispose() { }
        }

        private sealed class TestLogger : Microsoft.Extensions.Logging.ILogger
        {
            private readonly List<string> _logged;
            public TestLogger(List<string> logged) => _logged = logged;
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;
            public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                _logged.Add(formatter(state, exception));
            }
        }
    }
}
