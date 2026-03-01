using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Logging.Serilog;
using Xunit;

namespace WorkflowForge.Extensions.Logging.Serilog.Tests
{
    public class SerilogExtensionsShould
    {
        [Fact]
        public void ReturnLogger_GivenCreateLoggerWithNullOptions()
        {
            var logger = SerilogLoggerFactory.CreateLogger((SerilogLoggerOptions?)null);

            Assert.NotNull(logger);
            Assert.IsAssignableFrom<IWorkflowForgeLogger>(logger);
        }

        [Fact]
        public void ReturnLogger_GivenCreateLoggerWithNoOptions()
        {
            var logger = SerilogLoggerFactory.CreateLogger();

            Assert.NotNull(logger);
            Assert.IsAssignableFrom<IWorkflowForgeLogger>(logger);
        }

        [Fact]
        public void ReturnConfiguredLogger_GivenCreateLoggerWithOptions()
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
        public void ReturnLogger_GivenCreateLoggerWithConsoleDisabled()
        {
            var options = new SerilogLoggerOptions { EnableConsoleSink = false };

            var logger = SerilogLoggerFactory.CreateLogger(options);

            Assert.NotNull(logger);
            Assert.IsAssignableFrom<IWorkflowForgeLogger>(logger);
        }

        [Fact]
        public void FilterLowerLevels_GivenCreateLoggerWithWarningLevel()
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
        public void FallBackToInformation_GivenCreateLoggerWithInvalidMinimumLevel()
        {
            var options = new SerilogLoggerOptions { MinimumLevel = "InvalidLevel", EnableConsoleSink = false };

            var logger = SerilogLoggerFactory.CreateLogger(options);

            Assert.NotNull(logger);
            var ex = Record.Exception(() => logger.LogInformation("Should work with fallback level"));
            Assert.Null(ex);
        }

        [Fact]
        public void FallBackToInformation_GivenCreateLoggerWithEmptyMinimumLevel()
        {
            var options = new SerilogLoggerOptions { MinimumLevel = "", EnableConsoleSink = false };

            var logger = SerilogLoggerFactory.CreateLogger(options);

            Assert.NotNull(logger);
            var ex = Record.Exception(() => logger.LogInformation("Should work"));
            Assert.Null(ex);
        }

        [Fact]
        public void FallBackToInformation_GivenCreateLoggerWithWhitespaceMinimumLevel()
        {
            var options = new SerilogLoggerOptions { MinimumLevel = "   ", EnableConsoleSink = false };

            var logger = SerilogLoggerFactory.CreateLogger(options);

            Assert.NotNull(logger);
            var ex = Record.Exception(() => logger.LogInformation("Should work"));
            Assert.Null(ex);
        }

        [Fact]
        public void SupportAllLogLevels_GivenCreateLogger()
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
        public void SupportBeginScope_GivenCreateLogger()
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
        public void HaveExpectedDefaults_GivenSerilogLoggerOptions()
        {
            var options = new SerilogLoggerOptions();

            Assert.Equal("Information", options.MinimumLevel);
            Assert.True(options.EnableConsoleSink);
            Assert.NotNull(options.ConsoleOutputTemplate);
            Assert.Contains("Timestamp", options.ConsoleOutputTemplate);
        }

        [Fact]
        public void UseDefault_GivenCreateLoggerWithNullConsoleOutputTemplate()
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
        public void ReturnLogger_GivenCreateLoggerWithLoggerFactory()
        {
            using var factory = LoggerFactory.Create(builder => builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace));

            var logger = SerilogLoggerFactory.CreateLogger(factory);

            Assert.NotNull(logger);
            Assert.IsAssignableFrom<IWorkflowForgeLogger>(logger);
        }

        [Fact]
        public void ThrowArgumentNullException_GivenCreateLoggerWithNullLoggerFactory()
        {
            Assert.Throws<ArgumentNullException>(() =>
                SerilogLoggerFactory.CreateLogger((Microsoft.Extensions.Logging.ILoggerFactory)null!));
        }

        [Fact]
        public void SupportAllLogLevels_GivenCreateLoggerWithLoggerFactory()
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
        public void SupportExceptions_GivenCreateLoggerWithLoggerFactory()
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
        public void SupportProperties_GivenCreateLoggerWithLoggerFactory()
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
        public void SupportBeginScope_GivenCreateLoggerWithLoggerFactory()
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
        public void ReturnDisposable_GivenCreateLoggerWithLoggerFactoryBeginScopeWithNullProperties()
        {
            using var factory = LoggerFactory.Create(builder => builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace));
            var logger = SerilogLoggerFactory.CreateLogger(factory);

            var scope = logger.BeginScope("state", null);

            Assert.NotNull(scope);
            scope.Dispose();
        }

        [Fact]
        public void ReturnDisposable_GivenCreateLoggerWithLoggerFactoryBeginScopeWithEmptyProperties()
        {
            using var factory = LoggerFactory.Create(builder => builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace));
            var logger = SerilogLoggerFactory.CreateLogger(factory);

            var scope = logger.BeginScope("state", new Dictionary<string, string>());

            Assert.NotNull(scope);
            scope.Dispose();
        }

        [Fact]
        public void RouteLogsThroughFactory_GivenCreateLoggerWithLoggerFactory()
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
        public void RouteErrorsWithExceptions_GivenCreateLoggerWithLoggerFactory()
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
        public void RouteAllLevelsThroughFactory_GivenCreateLoggerWithLoggerFactory()
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
