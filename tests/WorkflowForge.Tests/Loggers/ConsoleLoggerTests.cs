using System;
using System.Collections.Generic;
using System.IO;
using WorkflowForge.Abstractions;
using WorkflowForge.Loggers;

namespace WorkflowForge.Tests.Loggers;

/// <summary>
/// Comprehensive tests for ConsoleLogger covering FormatMessage (via log methods),
/// ConvertTemplateToPositional (via structured templates), all log levels, and edge cases.
/// </summary>
public class ConsoleLoggerTests
{
    #region Constructor

    [Fact]
    public void Constructor_WithPrefix_SetsPrefix()
    {
        var logger = new ConsoleLogger("TestPrefix");
        // Prefix is used in WriteToConsole - verify by logging and capturing output
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(sw);
            logger.LogInformation("Message");
            var output = sw.ToString();
            Assert.Contains("TestPrefix", output);
            Assert.Contains("Message", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Constructor_WithNullPrefix_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ConsoleLogger(null!));
    }

    [Fact]
    public void Constructor_WithCustomTimeProvider_UsesProvidedProvider()
    {
        var fixedTime = new DateTimeOffset(2025, 3, 1, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new Mock<ISystemTimeProvider>();
        timeProvider.Setup(t => t.Now).Returns(fixedTime);
        timeProvider.Setup(t => t.UtcNow).Returns(fixedTime);
        timeProvider.Setup(t => t.Today).Returns(fixedTime.Date);

        var logger = new ConsoleLogger("Test", timeProvider.Object);

        using var sw = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(sw);
            logger.LogInformation("Test");
            var output = sw.ToString();
            Assert.Contains("2025-03-01", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Constructor_WithNullTimeProvider_UsesDefaultProvider()
    {
        var logger = new ConsoleLogger("Test", null);
        Assert.NotNull(logger);
        var ex = Record.Exception(() => logger.LogInformation("test"));
        Assert.Null(ex);
    }

    #endregion

    #region FormatMessage / ConvertTemplateToPositional (via Log methods)

    [Fact]
    public void LogInformation_WithNoArgs_ReturnsMessageAsIs()
    {
        var logger = new ConsoleLogger("Test");
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(sw);
            logger.LogInformation("Simple message");
            var output = sw.ToString();
            Assert.Contains("Simple message", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void LogInformation_WithPositionalArgs_FormatsCorrectly()
    {
        var logger = new ConsoleLogger("Test");
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(sw);
            logger.LogInformation("Value: {0}", 42);
            var output = sw.ToString();
            Assert.Contains("Value: 42", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void LogInformation_WithStructuredTemplate_ConvertsToPositional()
    {
        var logger = new ConsoleLogger("Test");
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(sw);
            logger.LogInformation("User {UserName} did {Action}", "Alice", "login");
            var output = sw.ToString();
            Assert.Contains("Alice", output);
            Assert.Contains("login", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void LogInformation_WithFormatException_ReturnsOriginalMessage()
    {
        var logger = new ConsoleLogger("Test");
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(sw);
            // Invalid format that may cause FormatException - e.g. {0} with wrong number of args
            logger.LogInformation("Message with {0} placeholder", "one", "extra");
            var output = sw.ToString();
            Assert.Contains("Message", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #endregion

    #region Log Levels - Trace

    [Fact]
    public void LogTrace_WithMessage_WritesToConsole()
    {
        var logger = new ConsoleLogger("Test");
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(sw);
            logger.LogTrace("Trace message");
            var output = sw.ToString();
            Assert.Contains("TRACE", output);
            Assert.Contains("Trace message", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void LogTrace_WithException_IncludesException()
    {
        var logger = new ConsoleLogger("Test");
        var ex = new InvalidOperationException("Test error");
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(sw);
            logger.LogTrace(ex, "Trace with exception");
            var output = sw.ToString();
            Assert.Contains("Exception", output);
            Assert.Contains("Test error", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void LogTrace_WithProperties_IncludesProperties()
    {
        var logger = new ConsoleLogger("Test");
        var props = new Dictionary<string, string> { ["Key"] = "Value" };
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(sw);
            logger.LogTrace(props, "Trace with props");
            var output = sw.ToString();
            Assert.Contains("Properties", output);
            Assert.Contains("Key=Value", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #endregion

    #region Log Levels - Debug

    [Fact]
    public void LogDebug_WithMessage_WritesToConsole()
    {
        var logger = new ConsoleLogger("Test");
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(sw);
            logger.LogDebug("Debug message");
            var output = sw.ToString();
            Assert.Contains("DEBUG", output);
            Assert.Contains("Debug message", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void LogDebug_WithException_IncludesException()
    {
        var logger = new ConsoleLogger("Test");
        var ex = new ArgumentException("Arg error");
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(sw);
            logger.LogDebug(ex, "Debug with ex");
            var output = sw.ToString();
            Assert.Contains("Exception", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #endregion

    #region Log Levels - Information

    [Fact]
    public void LogInformation_WithMessage_WritesToConsole()
    {
        var logger = new ConsoleLogger("Test");
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(sw);
            logger.LogInformation("Info message");
            var output = sw.ToString();
            Assert.Contains("INFO", output);
            Assert.Contains("Info message", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void LogInformation_WithProperties_IncludesProperties()
    {
        var logger = new ConsoleLogger("Test");
        var props = new Dictionary<string, string> { ["A"] = "1", ["B"] = "2" };
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(sw);
            logger.LogInformation(props, "Info");
            var output = sw.ToString();
            Assert.Contains("A=1", output);
            Assert.Contains("B=2", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #endregion

    #region Log Levels - Warning

    [Fact]
    public void LogWarning_WithMessage_WritesToConsole()
    {
        var logger = new ConsoleLogger("Test");
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(sw);
            logger.LogWarning("Warning message");
            var output = sw.ToString();
            Assert.Contains("WARN", output);
            Assert.Contains("Warning message", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void LogWarning_WithException_IncludesException()
    {
        var logger = new ConsoleLogger("Test");
        var ex = new Exception("Warn ex");
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(sw);
            logger.LogWarning(ex, "Warn");
            var output = sw.ToString();
            Assert.Contains("Exception", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #endregion

    #region Log Levels - Error

    [Fact]
    public void LogError_WithMessage_WritesToConsole()
    {
        var logger = new ConsoleLogger("Test");
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(sw);
            logger.LogError("Error message");
            var output = sw.ToString();
            Assert.Contains("ERROR", output);
            Assert.Contains("Error message", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void LogError_WithPropertiesAndException_IncludesBoth()
    {
        var logger = new ConsoleLogger("Test");
        var props = new Dictionary<string, string> { ["ErrCode"] = "500" };
        var ex = new InvalidOperationException("Server error");
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(sw);
            logger.LogError(props, ex, "Error occurred");
            var output = sw.ToString();
            Assert.Contains("ErrCode=500", output);
            Assert.Contains("Exception", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #endregion

    #region Log Levels - Critical

    [Fact]
    public void LogCritical_WithMessage_WritesToConsole()
    {
        var logger = new ConsoleLogger("Test");
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(sw);
            logger.LogCritical("Critical message");
            var output = sw.ToString();
            Assert.Contains("CRITICAL", output);
            Assert.Contains("Critical message", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void LogCritical_WithException_IncludesException()
    {
        var logger = new ConsoleLogger("Test");
        var ex = new OutOfMemoryException("OOM");
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(sw);
            logger.LogCritical(ex, "Critical");
            var output = sw.ToString();
            Assert.Contains("Exception", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void LogCritical_WithProperties_IncludesProperties()
    {
        var logger = new ConsoleLogger("Test");
        var props = new Dictionary<string, string> { ["Severity"] = "Critical" };
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(sw);
            logger.LogCritical(props, "Critical event");
            var output = sw.ToString();
            Assert.Contains("Severity=Critical", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #endregion

    #region BeginScope

    [Fact]
    public void BeginScope_ReturnsEmptyDisposable()
    {
        var logger = new ConsoleLogger("Test");
        using var scope = logger.BeginScope("state");
        var ex = Record.Exception(() => scope.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void BeginScope_WithProperties_ReturnsEmptyDisposable()
    {
        var logger = new ConsoleLogger("Test");
        var props = new Dictionary<string, string> { ["Scope"] = "Test" };
        using var scope = logger.BeginScope("state", props);
        var ex = Record.Exception(() => scope.Dispose());
        Assert.Null(ex);
    }

    #endregion

    #region Edge Cases - Empty/Null Properties

    [Fact]
    public void LogTrace_WithEmptyProperties_LogsMessageWithoutPropertiesSection()
    {
        var logger = new ConsoleLogger("Test");
        var emptyProps = new Dictionary<string, string>();
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(sw);
            logger.LogTrace(emptyProps, "Message");
            var output = sw.ToString();
            Assert.Contains("Message", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void LogInformation_WithEmptyProperties_LogsMessageOnly()
    {
        var logger = new ConsoleLogger("Test");
        var emptyProps = new Dictionary<string, string>();
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(sw);
            logger.LogInformation(emptyProps, "Message");
            var output = sw.ToString();
            Assert.Contains("Message", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #endregion
}
