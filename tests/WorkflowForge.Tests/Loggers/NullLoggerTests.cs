using System;
using System.Collections.Generic;
using WorkflowForge.Abstractions;
using WorkflowForge.Loggers;

namespace WorkflowForge.Tests.Loggers;

public class NullLoggerShould
{
    private readonly IWorkflowForgeLogger _logger = WorkflowForgeLoggers.Null;

    [Fact]
    public void NotThrow_GivenLogTraceWithMessage()
    {
        _logger.LogTrace("test message");
    }

    [Fact]
    public void NotThrow_GivenLogTraceWithException()
    {
        _logger.LogTrace(new Exception("err"), "test {0}", "arg");
    }

    [Fact]
    public void NotThrow_GivenLogTraceWithProperties()
    {
        _logger.LogTrace(new Dictionary<string, string> { ["key"] = "val" }, "test {0}", "arg");
    }

    [Fact]
    public void NotThrow_GivenLogTraceWithPropertiesAndException()
    {
        _logger.LogTrace(new Dictionary<string, string> { ["key"] = "val" }, new Exception("err"), "test {0}", "arg");
    }

    [Fact]
    public void NotThrow_GivenLogDebugWithMessage()
    {
        _logger.LogDebug("test message");
    }

    [Fact]
    public void NotThrow_GivenLogDebugWithException()
    {
        _logger.LogDebug(new Exception("err"), "test {0}", "arg");
    }

    [Fact]
    public void NotThrow_GivenLogDebugWithProperties()
    {
        _logger.LogDebug(new Dictionary<string, string> { ["key"] = "val" }, "test {0}", "arg");
    }

    [Fact]
    public void NotThrow_GivenLogDebugWithPropertiesAndException()
    {
        _logger.LogDebug(new Dictionary<string, string> { ["key"] = "val" }, new Exception("err"), "test {0}", "arg");
    }

    [Fact]
    public void NotThrow_GivenLogInformationWithMessage()
    {
        _logger.LogInformation("test message");
    }

    [Fact]
    public void NotThrow_GivenLogInformationWithException()
    {
        _logger.LogInformation(new Exception("err"), "test {0}", "arg");
    }

    [Fact]
    public void NotThrow_GivenLogInformationWithProperties()
    {
        _logger.LogInformation(new Dictionary<string, string> { ["key"] = "val" }, "test {0}", "arg");
    }

    [Fact]
    public void NotThrow_GivenLogInformationWithPropertiesAndException()
    {
        _logger.LogInformation(new Dictionary<string, string> { ["key"] = "val" }, new Exception("err"), "test {0}", "arg");
    }

    [Fact]
    public void NotThrow_GivenLogWarningWithMessage()
    {
        _logger.LogWarning("test message");
    }

    [Fact]
    public void NotThrow_GivenLogWarningWithException()
    {
        _logger.LogWarning(new Exception("err"), "test {0}", "arg");
    }

    [Fact]
    public void NotThrow_GivenLogWarningWithProperties()
    {
        _logger.LogWarning(new Dictionary<string, string> { ["key"] = "val" }, "test {0}", "arg");
    }

    [Fact]
    public void NotThrow_GivenLogWarningWithPropertiesAndException()
    {
        _logger.LogWarning(new Dictionary<string, string> { ["key"] = "val" }, new Exception("err"), "test {0}", "arg");
    }

    [Fact]
    public void NotThrow_GivenLogErrorWithMessage()
    {
        _logger.LogError("test message");
    }

    [Fact]
    public void NotThrow_GivenLogErrorWithException()
    {
        _logger.LogError(new Exception("err"), "test {0}", "arg");
    }

    [Fact]
    public void NotThrow_GivenLogErrorWithProperties()
    {
        _logger.LogError(new Dictionary<string, string> { ["key"] = "val" }, "test {0}", "arg");
    }

    [Fact]
    public void NotThrow_GivenLogErrorWithPropertiesAndException()
    {
        _logger.LogError(new Dictionary<string, string> { ["key"] = "val" }, new Exception("err"), "test {0}", "arg");
    }

    [Fact]
    public void NotThrow_GivenLogCriticalWithMessage()
    {
        _logger.LogCritical("test message");
    }

    [Fact]
    public void NotThrow_GivenLogCriticalWithException()
    {
        _logger.LogCritical(new Exception("err"), "test {0}", "arg");
    }

    [Fact]
    public void NotThrow_GivenLogCriticalWithProperties()
    {
        _logger.LogCritical(new Dictionary<string, string> { ["key"] = "val" }, "test {0}", "arg");
    }

    [Fact]
    public void NotThrow_GivenLogCriticalWithPropertiesAndException()
    {
        _logger.LogCritical(new Dictionary<string, string> { ["key"] = "val" }, new Exception("err"), "test {0}", "arg");
    }

    [Fact]
    public void ReturnDisposable_GivenBeginScope()
    {
        var scope = _logger.BeginScope("testState");
        Assert.NotNull(scope);
        scope.Dispose(); // Should not throw
    }

    [Fact]
    public void ReturnDisposable_GivenBeginScopeWithProperties()
    {
        var scope = _logger.BeginScope("testState", new Dictionary<string, string> { ["key"] = "val" });
        Assert.NotNull(scope);
        scope.Dispose(); // Should not throw
    }
}
