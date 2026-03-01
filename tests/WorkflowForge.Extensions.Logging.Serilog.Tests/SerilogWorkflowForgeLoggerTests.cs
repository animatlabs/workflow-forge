using System;
using System.Collections.Generic;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Logging.Serilog;
using Xunit;

namespace WorkflowForge.Extensions.Logging.Serilog.Tests
{
    public class SerilogWorkflowForgeLoggerShould
    {
        private readonly IWorkflowForgeLogger _logger;

        public SerilogWorkflowForgeLoggerShould()
        {
            _logger = SerilogLoggerFactory.CreateLogger(
                new SerilogLoggerOptions { MinimumLevel = "Verbose", EnableConsoleSink = false });
        }

        [Fact]
        public void NotThrow_GivenLogTraceWithMessage()
        {
            var ex = Record.Exception(() => _logger.LogTrace("Trace message"));
            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenLogTraceWithException()
        {
            var ex = Record.Exception(() => _logger.LogTrace(new InvalidOperationException("Test"), "Trace with exception"));
            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenLogTraceWithProperties()
        {
            var props = new Dictionary<string, string> { ["Key"] = "Value" };
            var ex = Record.Exception(() => _logger.LogTrace(props, "Trace with properties"));
            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenLogTraceWithPropertiesAndException()
        {
            var props = new Dictionary<string, string> { ["Key"] = "Value" };
            var ex = Record.Exception(() => _logger.LogTrace(props, new Exception("Test"), "Trace"));
            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenLogDebugWithMessage()
        {
            var ex = Record.Exception(() => _logger.LogDebug("Debug message"));
            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenLogDebugWithException()
        {
            var ex = Record.Exception(() => _logger.LogDebug(new Exception("Test"), "Debug with exception"));
            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenLogDebugWithProperties()
        {
            var props = new Dictionary<string, string> { ["Key"] = "Value" };
            var ex = Record.Exception(() => _logger.LogDebug(props, "Debug with properties"));
            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenLogDebugWithPropertiesAndException()
        {
            var props = new Dictionary<string, string> { ["Key"] = "Value" };
            var ex = Record.Exception(() => _logger.LogDebug(props, new Exception("Test"), "Debug"));
            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenLogInformationWithMessage()
        {
            var ex = Record.Exception(() => _logger.LogInformation("Info message"));
            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenLogInformationWithException()
        {
            var ex = Record.Exception(() => _logger.LogInformation(new Exception("Test"), "Info with exception"));
            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenLogInformationWithProperties()
        {
            var props = new Dictionary<string, string> { ["Key"] = "Value" };
            var ex = Record.Exception(() => _logger.LogInformation(props, "Info with properties"));
            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenLogInformationWithPropertiesAndException()
        {
            var props = new Dictionary<string, string> { ["Key"] = "Value" };
            var ex = Record.Exception(() => _logger.LogInformation(props, new Exception("Test"), "Info"));
            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenLogWarningWithMessage()
        {
            var ex = Record.Exception(() => _logger.LogWarning("Warning message"));
            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenLogWarningWithException()
        {
            var ex = Record.Exception(() => _logger.LogWarning(new Exception("Test"), "Warning with exception"));
            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenLogWarningWithProperties()
        {
            var props = new Dictionary<string, string> { ["Key"] = "Value" };
            var ex = Record.Exception(() => _logger.LogWarning(props, "Warning with properties"));
            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenLogWarningWithPropertiesAndException()
        {
            var props = new Dictionary<string, string> { ["Key"] = "Value" };
            var ex = Record.Exception(() => _logger.LogWarning(props, new Exception("Test"), "Warning"));
            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenLogErrorWithMessage()
        {
            var ex = Record.Exception(() => _logger.LogError("Error message"));
            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenLogErrorWithException()
        {
            var ex = Record.Exception(() => _logger.LogError(new Exception("Test"), "Error with exception"));
            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenLogErrorWithProperties()
        {
            var props = new Dictionary<string, string> { ["Key"] = "Value" };
            var ex = Record.Exception(() => _logger.LogError(props, "Error with properties"));
            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenLogErrorWithPropertiesAndException()
        {
            var props = new Dictionary<string, string> { ["Key"] = "Value" };
            var ex = Record.Exception(() => _logger.LogError(props, new Exception("Test"), "Error"));
            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenLogCriticalWithMessage()
        {
            var ex = Record.Exception(() => _logger.LogCritical("Critical message"));
            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenLogCriticalWithException()
        {
            var ex = Record.Exception(() => _logger.LogCritical(new Exception("Test"), "Critical with exception"));
            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenLogCriticalWithProperties()
        {
            var props = new Dictionary<string, string> { ["Key"] = "Value" };
            var ex = Record.Exception(() => _logger.LogCritical(props, "Critical with properties"));
            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenLogCriticalWithPropertiesAndException()
        {
            var props = new Dictionary<string, string> { ["Key"] = "Value" };
            var ex = Record.Exception(() => _logger.LogCritical(props, new Exception("Test"), "Critical"));
            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenLogInformationWithFormatArgs()
        {
            var ex = Record.Exception(() => _logger.LogInformation("Hello {0}", "World"));
            Assert.Null(ex);
        }

        [Fact]
        public void ReturnDisposable_GivenBeginScopeWithNullProperties()
        {
            var scope = _logger.BeginScope("state", null);
            Assert.NotNull(scope);
            scope.Dispose();
        }

        [Fact]
        public void ReturnDisposable_GivenBeginScopeWithProperties()
        {
            var props = new Dictionary<string, string> { ["Key"] = "Value" };
            var scope = _logger.BeginScope("state", props);
            Assert.NotNull(scope);
            scope.Dispose();
        }

        [Fact]
        public void ReturnDisposable_GivenBeginScopeWithEmptyProperties()
        {
            var props = new Dictionary<string, string>();
            var scope = _logger.BeginScope("state", props);
            Assert.NotNull(scope);
            scope.Dispose();
        }
    }
}
