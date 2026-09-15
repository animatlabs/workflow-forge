using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using WorkflowForge.Abstractions;
using WorkflowForge.Operations;

namespace WorkflowForge.Extensions.Logging.Serilog.Tests
{
    [Collection(ConsoleOutputCollection.Name)]
    public class SerilogWorkflowForgeLoggerShould
    {
        private readonly IWorkflowForgeLogger _logger;

        public SerilogWorkflowForgeLoggerShould()
        {
            _logger = SerilogLoggerFactory.CreateLogger(
                new SerilogLoggerOptions { MinimumLevel = "Verbose", EnableConsoleSink = false });
        }

        // Drives the console sink rather than naming Serilog types directly: Serilog is merged and
        // internalized in Release, which makes those names ambiguous in this project.
        [Fact]
        public void WriteRenderedMessage_GivenLogInformationThroughConsoleSink()
        {
            var originalOut = Console.Out;
            using var captured = new StringWriter(CultureInfo.InvariantCulture);
            Console.SetOut(captured);
            try
            {
                var logger = SerilogLoggerFactory.CreateLogger(new SerilogLoggerOptions
                {
                    MinimumLevel = "Verbose",
                    EnableConsoleSink = true,
                    ConsoleOutputTemplate = "{Level}|{Message:lj}{NewLine}"
                });

                Assert.True(logger.IsEnabled(WorkflowForgeLogLevel.Information));

                logger.LogInformation("Packaging smoke {Item}", "check");
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            var output = captured.ToString();
            Assert.Contains("Information", output, StringComparison.Ordinal);
            Assert.Contains("Packaging smoke check", output, StringComparison.Ordinal);
        }

        [Fact]
        public void NotWriteBelowMinimumLevel_GivenInformationMinimum()
        {
            var originalOut = Console.Out;
            using var captured = new StringWriter(CultureInfo.InvariantCulture);
            Console.SetOut(captured);
            try
            {
                var logger = SerilogLoggerFactory.CreateLogger(new SerilogLoggerOptions
                {
                    MinimumLevel = "Information",
                    EnableConsoleSink = true,
                    ConsoleOutputTemplate = "{Message:lj}{NewLine}"
                });

                Assert.False(logger.IsEnabled(WorkflowForgeLogLevel.Debug));

                logger.LogDebug("suppressed-entry");
                logger.LogWarning("emitted-entry");
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            var output = captured.ToString();
            Assert.DoesNotContain("suppressed-entry", output, StringComparison.Ordinal);
            Assert.Contains("emitted-entry", output, StringComparison.Ordinal);
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

        [Theory]
        [InlineData("Trace", "Verbose")]
        [InlineData("Debug", "Debug")]
        [InlineData("Information", "Information")]
        [InlineData("Warning", "Warning")]
        [InlineData("Error", "Error")]
        [InlineData("Critical", "Fatal")]
        public void EmitTheMessageAtTheMappedLevel_GivenEachLogOverload(string workflowLevel, string serilogLevel)
        {
            var output = CaptureConsole(logger =>
            {
                Invoke(logger, workflowLevel, "plain-message");
                Invoke(logger, workflowLevel, "exception-message", exception: new InvalidOperationException("boom"));
                Invoke(logger, workflowLevel, "properties-message", properties: Props());
                Invoke(logger, workflowLevel, "everything-message", exception: new InvalidOperationException("boom"), properties: Props());
            });

            Assert.Equal(4, CountOccurrences(output, serilogLevel));
            Assert.Contains("plain-message", output, StringComparison.Ordinal);
            Assert.Contains("exception-message", output, StringComparison.Ordinal);
            Assert.Contains("properties-message", output, StringComparison.Ordinal);
            Assert.Contains("everything-message", output, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("Trace")]
        [InlineData("Debug")]
        [InlineData("Information")]
        [InlineData("Warning")]
        [InlineData("Error")]
        [InlineData("Critical")]
        public void AttachTheSuppliedProperties_GivenThePropertyOverloads(string workflowLevel)
        {
            var output = CaptureConsole(
                logger => Invoke(logger, workflowLevel, "with-props", properties: Props()),
                template: "{Key}|{Message:lj}{NewLine}");

            Assert.Contains("Value", output, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("Trace")]
        [InlineData("Debug")]
        [InlineData("Information")]
        [InlineData("Warning")]
        [InlineData("Error")]
        [InlineData("Critical")]
        public void RenderTheException_GivenTheExceptionOverloads(string workflowLevel)
        {
            var output = CaptureConsole(
                logger => Invoke(logger, workflowLevel, "with-exception", exception: new InvalidOperationException("boom")),
                template: "{Message:lj}{NewLine}{Exception}");

            Assert.Contains("InvalidOperationException", output, StringComparison.Ordinal);
            Assert.Contains("boom", output, StringComparison.Ordinal);
        }

        private static Dictionary<string, string> Props() => new() { ["Key"] = "Value" };

        private static int CountOccurrences(string text, string token)
        {
            var count = 0;
            var index = text.IndexOf(token, StringComparison.Ordinal);
            while (index >= 0)
            {
                count++;
                index = text.IndexOf(token, index + token.Length, StringComparison.Ordinal);
            }

            return count;
        }

        private static string CaptureConsole(Action<IWorkflowForgeLogger> act, string template = "{Level}|{Message:lj}{NewLine}")
        {
            var originalOut = Console.Out;
            using var captured = new StringWriter(CultureInfo.InvariantCulture);
            Console.SetOut(captured);
            try
            {
                var logger = SerilogLoggerFactory.CreateLogger(new SerilogLoggerOptions
                {
                    MinimumLevel = "Verbose",
                    EnableConsoleSink = true,
                    ConsoleOutputTemplate = template
                });

                act(logger);
                (logger as IDisposable)?.Dispose();
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            return captured.ToString();
        }

        private static void Invoke(
            IWorkflowForgeLogger logger,
            string workflowLevel,
            string message,
            Exception? exception = null,
            Dictionary<string, string>? properties = null)
        {
            switch (workflowLevel)
            {
                case "Trace":
                    if (properties != null && exception != null) logger.LogTrace(properties, exception, message);
                    else if (properties != null) logger.LogTrace(properties, message);
                    else if (exception != null) logger.LogTrace(exception, message);
                    else logger.LogTrace(message);
                    break;
                case "Debug":
                    if (properties != null && exception != null) logger.LogDebug(properties, exception, message);
                    else if (properties != null) logger.LogDebug(properties, message);
                    else if (exception != null) logger.LogDebug(exception, message);
                    else logger.LogDebug(message);
                    break;
                case "Information":
                    if (properties != null && exception != null) logger.LogInformation(properties, exception, message);
                    else if (properties != null) logger.LogInformation(properties, message);
                    else if (exception != null) logger.LogInformation(exception, message);
                    else logger.LogInformation(message);
                    break;
                case "Warning":
                    if (properties != null && exception != null) logger.LogWarning(properties, exception, message);
                    else if (properties != null) logger.LogWarning(properties, message);
                    else if (exception != null) logger.LogWarning(exception, message);
                    else logger.LogWarning(message);
                    break;
                case "Error":
                    if (properties != null && exception != null) logger.LogError(properties, exception, message);
                    else if (properties != null) logger.LogError(properties, message);
                    else if (exception != null) logger.LogError(exception, message);
                    else logger.LogError(message);
                    break;
                case "Critical":
                    if (properties != null && exception != null) logger.LogCritical(properties, exception, message);
                    else if (properties != null) logger.LogCritical(properties, message);
                    else if (exception != null) logger.LogCritical(exception, message);
                    else logger.LogCritical(message);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(workflowLevel), workflowLevel, "Unknown level");
            }
        }

    }
}
