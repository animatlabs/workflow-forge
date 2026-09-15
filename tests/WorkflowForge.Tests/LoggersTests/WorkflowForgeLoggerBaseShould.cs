using System;
using System.Collections.Generic;
using WorkflowForge.Loggers;
using WorkflowForge.Operations;

namespace WorkflowForge.Tests.LoggersTests;

public class WorkflowForgeLoggerBaseShould
{
    [Fact]
    public void ReturnTrue_GivenIsEnabledAtOrAboveDefaultMinimum()
    {
        var logger = new TestLogger();
        Assert.True(logger.IsEnabled(WorkflowForgeLogLevel.Trace));
        Assert.True(logger.IsEnabled(WorkflowForgeLogLevel.Critical));
    }

    [Fact]
    public void ReturnFalse_GivenIsEnabledBelowConfiguredMinimum()
    {
        var logger = new TestLogger { ConfiguredMinimum = WorkflowForgeLogLevel.Warning };
        Assert.False(logger.IsEnabled(WorkflowForgeLogLevel.Trace));
        Assert.False(logger.IsEnabled(WorkflowForgeLogLevel.Debug));
        Assert.False(logger.IsEnabled(WorkflowForgeLogLevel.Information));
        Assert.True(logger.IsEnabled(WorkflowForgeLogLevel.Warning));
        Assert.True(logger.IsEnabled(WorkflowForgeLogLevel.Error));
    }

    private sealed class TestLogger : WorkflowForgeLoggerBase
    {
        public WorkflowForgeLogLevel ConfiguredMinimum { get; set; } = WorkflowForgeLogLevel.Trace;

        protected override WorkflowForgeLogLevel MinimumLevel => ConfiguredMinimum;

        public override void LogTrace(string message, params object[] args) { }

        public override void LogTrace(Exception exception, string message, params object[] args) { }

        public override void LogTrace(IDictionary<string, string> properties, string message, params object[] args) { }

        public override void LogTrace(IDictionary<string, string> properties, Exception exception, string message, params object[] args) { }

        public override void LogDebug(string message, params object[] args) { }

        public override void LogDebug(Exception exception, string message, params object[] args) { }

        public override void LogDebug(IDictionary<string, string> properties, string message, params object[] args) { }

        public override void LogDebug(IDictionary<string, string> properties, Exception exception, string message, params object[] args) { }

        public override void LogInformation(string message, params object[] args) { }

        public override void LogInformation(Exception exception, string message, params object[] args) { }

        public override void LogInformation(IDictionary<string, string> properties, string message, params object[] args) { }

        public override void LogInformation(IDictionary<string, string> properties, Exception exception, string message, params object[] args) { }

        public override void LogWarning(string message, params object[] args) { }

        public override void LogWarning(Exception exception, string message, params object[] args) { }

        public override void LogWarning(IDictionary<string, string> properties, string message, params object[] args) { }

        public override void LogWarning(IDictionary<string, string> properties, Exception exception, string message, params object[] args) { }

        public override void LogError(string message, params object[] args) { }

        public override void LogError(Exception exception, string message, params object[] args) { }

        public override void LogError(IDictionary<string, string> properties, string message, params object[] args) { }

        public override void LogError(IDictionary<string, string> properties, Exception exception, string message, params object[] args) { }

        public override void LogCritical(string message, params object[] args) { }

        public override void LogCritical(Exception exception, string message, params object[] args) { }

        public override void LogCritical(IDictionary<string, string> properties, string message, params object[] args) { }

        public override void LogCritical(IDictionary<string, string> properties, Exception exception, string message, params object[] args) { }

        public override IDisposable BeginScope<TState>(TState state, IDictionary<string, string>? properties = null) =>
            NullScope.Instance;

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose() { }
        }
    }
}
