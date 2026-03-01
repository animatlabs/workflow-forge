using System;
using System.Collections.Generic;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Logging.Serilog;
using Xunit;

namespace WorkflowForge.Extensions.Logging.Serilog.Tests
{
    public class SerilogWorkflowForgeLoggerTests
    {
        private readonly IWorkflowForgeLogger _logger;

        public SerilogWorkflowForgeLoggerTests()
        {
            _logger = SerilogLoggerFactory.CreateLogger(
                new SerilogLoggerOptions { MinimumLevel = "Verbose", EnableConsoleSink = false });
        }

        [Fact]
        public void LogTrace_WithMessage_DoesNotThrow()
        {
            var ex = Record.Exception(() => _logger.LogTrace("Trace message"));
            Assert.Null(ex);
        }

        [Fact]
        public void LogTrace_WithException_DoesNotThrow()
        {
            var ex = Record.Exception(() => _logger.LogTrace(new InvalidOperationException("Test"), "Trace with exception"));
            Assert.Null(ex);
        }

        [Fact]
        public void LogTrace_WithProperties_DoesNotThrow()
        {
            var props = new Dictionary<string, string> { ["Key"] = "Value" };
            var ex = Record.Exception(() => _logger.LogTrace(props, "Trace with properties"));
            Assert.Null(ex);
        }

        [Fact]
        public void LogTrace_WithPropertiesAndException_DoesNotThrow()
        {
            var props = new Dictionary<string, string> { ["Key"] = "Value" };
            var ex = Record.Exception(() => _logger.LogTrace(props, new Exception("Test"), "Trace"));
            Assert.Null(ex);
        }

        [Fact]
        public void LogDebug_WithMessage_DoesNotThrow()
        {
            var ex = Record.Exception(() => _logger.LogDebug("Debug message"));
            Assert.Null(ex);
        }

        [Fact]
        public void LogDebug_WithException_DoesNotThrow()
        {
            var ex = Record.Exception(() => _logger.LogDebug(new Exception("Test"), "Debug with exception"));
            Assert.Null(ex);
        }

        [Fact]
        public void LogDebug_WithProperties_DoesNotThrow()
        {
            var props = new Dictionary<string, string> { ["Key"] = "Value" };
            var ex = Record.Exception(() => _logger.LogDebug(props, "Debug with properties"));
            Assert.Null(ex);
        }

        [Fact]
        public void LogDebug_WithPropertiesAndException_DoesNotThrow()
        {
            var props = new Dictionary<string, string> { ["Key"] = "Value" };
            var ex = Record.Exception(() => _logger.LogDebug(props, new Exception("Test"), "Debug"));
            Assert.Null(ex);
        }

        [Fact]
        public void LogInformation_WithMessage_DoesNotThrow()
        {
            var ex = Record.Exception(() => _logger.LogInformation("Info message"));
            Assert.Null(ex);
        }

        [Fact]
        public void LogInformation_WithException_DoesNotThrow()
        {
            var ex = Record.Exception(() => _logger.LogInformation(new Exception("Test"), "Info with exception"));
            Assert.Null(ex);
        }

        [Fact]
        public void LogInformation_WithProperties_DoesNotThrow()
        {
            var props = new Dictionary<string, string> { ["Key"] = "Value" };
            var ex = Record.Exception(() => _logger.LogInformation(props, "Info with properties"));
            Assert.Null(ex);
        }

        [Fact]
        public void LogInformation_WithPropertiesAndException_DoesNotThrow()
        {
            var props = new Dictionary<string, string> { ["Key"] = "Value" };
            var ex = Record.Exception(() => _logger.LogInformation(props, new Exception("Test"), "Info"));
            Assert.Null(ex);
        }

        [Fact]
        public void LogWarning_WithMessage_DoesNotThrow()
        {
            var ex = Record.Exception(() => _logger.LogWarning("Warning message"));
            Assert.Null(ex);
        }

        [Fact]
        public void LogWarning_WithException_DoesNotThrow()
        {
            var ex = Record.Exception(() => _logger.LogWarning(new Exception("Test"), "Warning with exception"));
            Assert.Null(ex);
        }

        [Fact]
        public void LogWarning_WithProperties_DoesNotThrow()
        {
            var props = new Dictionary<string, string> { ["Key"] = "Value" };
            var ex = Record.Exception(() => _logger.LogWarning(props, "Warning with properties"));
            Assert.Null(ex);
        }

        [Fact]
        public void LogWarning_WithPropertiesAndException_DoesNotThrow()
        {
            var props = new Dictionary<string, string> { ["Key"] = "Value" };
            var ex = Record.Exception(() => _logger.LogWarning(props, new Exception("Test"), "Warning"));
            Assert.Null(ex);
        }

        [Fact]
        public void LogError_WithMessage_DoesNotThrow()
        {
            var ex = Record.Exception(() => _logger.LogError("Error message"));
            Assert.Null(ex);
        }

        [Fact]
        public void LogError_WithException_DoesNotThrow()
        {
            var ex = Record.Exception(() => _logger.LogError(new Exception("Test"), "Error with exception"));
            Assert.Null(ex);
        }

        [Fact]
        public void LogError_WithProperties_DoesNotThrow()
        {
            var props = new Dictionary<string, string> { ["Key"] = "Value" };
            var ex = Record.Exception(() => _logger.LogError(props, "Error with properties"));
            Assert.Null(ex);
        }

        [Fact]
        public void LogError_WithPropertiesAndException_DoesNotThrow()
        {
            var props = new Dictionary<string, string> { ["Key"] = "Value" };
            var ex = Record.Exception(() => _logger.LogError(props, new Exception("Test"), "Error"));
            Assert.Null(ex);
        }

        [Fact]
        public void LogCritical_WithMessage_DoesNotThrow()
        {
            var ex = Record.Exception(() => _logger.LogCritical("Critical message"));
            Assert.Null(ex);
        }

        [Fact]
        public void LogCritical_WithException_DoesNotThrow()
        {
            var ex = Record.Exception(() => _logger.LogCritical(new Exception("Test"), "Critical with exception"));
            Assert.Null(ex);
        }

        [Fact]
        public void LogCritical_WithProperties_DoesNotThrow()
        {
            var props = new Dictionary<string, string> { ["Key"] = "Value" };
            var ex = Record.Exception(() => _logger.LogCritical(props, "Critical with properties"));
            Assert.Null(ex);
        }

        [Fact]
        public void LogCritical_WithPropertiesAndException_DoesNotThrow()
        {
            var props = new Dictionary<string, string> { ["Key"] = "Value" };
            var ex = Record.Exception(() => _logger.LogCritical(props, new Exception("Test"), "Critical"));
            Assert.Null(ex);
        }

        [Fact]
        public void LogInformation_WithFormatArgs_DoesNotThrow()
        {
            var ex = Record.Exception(() => _logger.LogInformation("Hello {0}", "World"));
            Assert.Null(ex);
        }

        [Fact]
        public void BeginScope_WithNullProperties_ReturnsDisposable()
        {
            var scope = _logger.BeginScope("state", null);
            Assert.NotNull(scope);
            scope.Dispose();
        }

        [Fact]
        public void BeginScope_WithProperties_ReturnsDisposable()
        {
            var props = new Dictionary<string, string> { ["Key"] = "Value" };
            var scope = _logger.BeginScope("state", props);
            Assert.NotNull(scope);
            scope.Dispose();
        }

        [Fact]
        public void BeginScope_WithEmptyProperties_ReturnsDisposable()
        {
            var props = new Dictionary<string, string>();
            var scope = _logger.BeginScope("state", props);
            Assert.NotNull(scope);
            scope.Dispose();
        }
    }
}
