using System;
using System.Collections.Generic;
using WorkflowForge.Abstractions;
using WorkflowForge.Testing;

namespace WorkflowForge.Tests.Testing;

public class TestNullLoggerShould
{
    private readonly IWorkflowForgeLogger _logger = TestNullLogger.Instance;

    [Fact]
    public void ReturnSameSingleton_GivenInstanceAccess()
    {
        Assert.Same(TestNullLogger.Instance, TestNullLogger.Instance);
    }

    [Fact]
    public void NotThrow_GivenAllTraceOverloads()
    {
        _logger.LogTrace("trace");
        _logger.LogTrace(new InvalidOperationException("e"), "trace {0}", 1);
        _logger.LogTrace(new Dictionary<string, string> { ["k"] = "v" }, "trace {0}", 1);
        _logger.LogTrace(new Dictionary<string, string> { ["k"] = "v" }, new InvalidOperationException("e"), "trace {0}", 1);
    }

    [Fact]
    public void NotThrow_GivenAllDebugOverloads()
    {
        _logger.LogDebug("debug");
        _logger.LogDebug(new InvalidOperationException("e"), "debug {0}", 1);
        _logger.LogDebug(new Dictionary<string, string> { ["k"] = "v" }, "debug {0}", 1);
        _logger.LogDebug(new Dictionary<string, string> { ["k"] = "v" }, new InvalidOperationException("e"), "debug {0}", 1);
    }

    [Fact]
    public void NotThrow_GivenAllInformationOverloads()
    {
        _logger.LogInformation("info");
        _logger.LogInformation(new InvalidOperationException("e"), "info {0}", 1);
        _logger.LogInformation(new Dictionary<string, string> { ["k"] = "v" }, "info {0}", 1);
        _logger.LogInformation(new Dictionary<string, string> { ["k"] = "v" }, new InvalidOperationException("e"), "info {0}", 1);
    }

    [Fact]
    public void NotThrow_GivenAllWarningOverloads()
    {
        _logger.LogWarning("warn");
        _logger.LogWarning(new InvalidOperationException("e"), "warn {0}", 1);
        _logger.LogWarning(new Dictionary<string, string> { ["k"] = "v" }, "warn {0}", 1);
        _logger.LogWarning(new Dictionary<string, string> { ["k"] = "v" }, new InvalidOperationException("e"), "warn {0}", 1);
    }

    [Fact]
    public void NotThrow_GivenAllErrorOverloads()
    {
        _logger.LogError("error");
        _logger.LogError(new InvalidOperationException("e"), "error {0}", 1);
        _logger.LogError(new Dictionary<string, string> { ["k"] = "v" }, "error {0}", 1);
        _logger.LogError(new Dictionary<string, string> { ["k"] = "v" }, new InvalidOperationException("e"), "error {0}", 1);
    }

    [Fact]
    public void NotThrow_GivenAllCriticalOverloads()
    {
        _logger.LogCritical("critical");
        _logger.LogCritical(new InvalidOperationException("e"), "critical {0}", 1);
        _logger.LogCritical(new Dictionary<string, string> { ["k"] = "v" }, "critical {0}", 1);
        _logger.LogCritical(new Dictionary<string, string> { ["k"] = "v" }, new InvalidOperationException("e"), "critical {0}", 1);
    }

    [Fact]
    public void ReturnDisposableScope_GivenBeginScope()
    {
        var scope = _logger.BeginScope("state", new Dictionary<string, string> { ["k"] = "v" });

        Assert.NotNull(scope);
        scope.Dispose();
    }
}
