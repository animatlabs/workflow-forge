using System;
using System.Collections.Generic;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Loggers;
using WorkflowForge.Operations;

namespace WorkflowForge.Tests.ExtensionsTests;

public class WorkflowForgeLogLevelParsingShould
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ReturnInformation_GivenParseMinimumLevelWithMissingValue(string? minimumLevel)
    {
        Assert.Equal(WorkflowForgeLogLevel.Information, WorkflowForgeLogLevelParsing.ParseMinimumLevel(minimumLevel));
    }

    [Theory]
    [InlineData("warning", WorkflowForgeLogLevel.Warning)]
    [InlineData("ERROR", WorkflowForgeLogLevel.Error)]
    public void ReturnParsedLevel_GivenParseMinimumLevelWithValidName(string minimumLevel, WorkflowForgeLogLevel expected)
    {
        Assert.Equal(expected, WorkflowForgeLogLevelParsing.ParseMinimumLevel(minimumLevel));
    }

    [Fact]
    public void ReturnInformation_GivenParseMinimumLevelWithInvalidName()
    {
        Assert.Equal(WorkflowForgeLogLevel.Information, WorkflowForgeLogLevelParsing.ParseMinimumLevel("not-a-level"));
    }

    [Fact]
    public void ReturnFalse_GivenIsLevelEnabledBelowMinimum()
    {
        var logger = NullLogger.Instance;
        Assert.False(WorkflowForgeLogLevelParsing.IsLevelEnabled(logger, WorkflowForgeLogLevel.Trace, WorkflowForgeLogLevel.Warning));
    }

    [Fact]
    public void ReturnFalse_GivenIsLevelEnabledAtMinimumButLoggerDisabled()
    {
        var logger = NullLogger.Instance;
        Assert.False(WorkflowForgeLogLevelParsing.IsLevelEnabled(logger, WorkflowForgeLogLevel.Warning, WorkflowForgeLogLevel.Warning));
    }

    [Fact]
    public void ReturnTrue_GivenIsLevelEnabledAtOrAboveMinimumAndLoggerEnabled()
    {
        IWorkflowForgeLogger logger = new EnabledTestLogger();
        Assert.True(WorkflowForgeLogLevelParsing.IsLevelEnabled(logger, WorkflowForgeLogLevel.Error, WorkflowForgeLogLevel.Information));
    }

    private sealed class EnabledTestLogger : IWorkflowForgeLogger
    {
        public bool IsEnabled(WorkflowForgeLogLevel level) => true;

        public void LogTrace(string message, params object[] args) { }

        public void LogTrace(Exception exception, string message, params object[] args) { }

        public void LogTrace(IDictionary<string, string> properties, string message, params object[] args) { }

        public void LogTrace(IDictionary<string, string> properties, Exception exception, string message, params object[] args) { }

        public void LogDebug(string message, params object[] args) { }

        public void LogDebug(Exception exception, string message, params object[] args) { }

        public void LogDebug(IDictionary<string, string> properties, string message, params object[] args) { }

        public void LogDebug(IDictionary<string, string> properties, Exception exception, string message, params object[] args) { }

        public void LogInformation(string message, params object[] args) { }

        public void LogInformation(Exception exception, string message, params object[] args) { }

        public void LogInformation(IDictionary<string, string> properties, string message, params object[] args) { }

        public void LogInformation(IDictionary<string, string> properties, Exception exception, string message, params object[] args) { }

        public void LogWarning(string message, params object[] args) { }

        public void LogWarning(Exception exception, string message, params object[] args) { }

        public void LogWarning(IDictionary<string, string> properties, string message, params object[] args) { }

        public void LogWarning(IDictionary<string, string> properties, Exception exception, string message, params object[] args) { }

        public void LogError(string message, params object[] args) { }

        public void LogError(Exception exception, string message, params object[] args) { }

        public void LogError(IDictionary<string, string> properties, string message, params object[] args) { }

        public void LogError(IDictionary<string, string> properties, Exception exception, string message, params object[] args) { }

        public void LogCritical(string message, params object[] args) { }

        public void LogCritical(Exception exception, string message, params object[] args) { }

        public void LogCritical(IDictionary<string, string> properties, string message, params object[] args) { }

        public void LogCritical(IDictionary<string, string> properties, Exception exception, string message, params object[] args) { }

        public IDisposable BeginScope<TState>(TState state, IDictionary<string, string>? properties = null) =>
            new NullScope();

        private sealed class NullScope : IDisposable
        {
            public void Dispose() { }
        }
    }
}
