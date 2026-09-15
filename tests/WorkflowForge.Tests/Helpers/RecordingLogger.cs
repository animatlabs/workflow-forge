using System;
using System.Collections.Generic;
using System.Linq;
using WorkflowForge.Abstractions;
using WorkflowForge.Operations;

namespace WorkflowForge.Tests.Helpers;

/// <summary>
/// Logger double that records every emitted message and scope so tests can assert on
/// observable logging behaviour rather than on mock call counts.
/// </summary>
internal sealed class RecordingLogger : IWorkflowForgeLogger
{
    public sealed class Entry
    {
        public Entry(WorkflowForgeLogLevel level, string message, Exception? exception, IDictionary<string, string>? properties)
        {
            Level = level;
            Message = message;
            Exception = exception;
            Properties = properties;
        }

        public WorkflowForgeLogLevel Level { get; }

        public string Message { get; }

        public Exception? Exception { get; }

        public IDictionary<string, string>? Properties { get; }
    }

    public List<Entry> Entries { get; } = new();

    public List<string> Scopes { get; } = new();

    public List<IDictionary<string, string>?> ScopeProperties { get; } = new();

    public WorkflowForgeLogLevel EnabledFrom { get; set; } = WorkflowForgeLogLevel.Trace;

    public IEnumerable<Entry> At(WorkflowForgeLogLevel level) => Entries.Where(e => e.Level == level);

    public IEnumerable<string> MessagesAt(WorkflowForgeLogLevel level) => At(level).Select(e => e.Message);

    public bool IsEnabled(WorkflowForgeLogLevel level) => level >= EnabledFrom;

    public IDisposable BeginScope<TState>(TState state, IDictionary<string, string>? properties = null)
    {
        Scopes.Add(state?.ToString() ?? string.Empty);
        ScopeProperties.Add(properties);
        return NoOpScope.Instance;
    }

    private void Add(WorkflowForgeLogLevel level, string message, Exception? exception = null, IDictionary<string, string>? properties = null)
        => Entries.Add(new Entry(level, message, exception, properties));

    public void LogTrace(string message, params object[] args) => Add(WorkflowForgeLogLevel.Trace, message);

    public void LogTrace(Exception exception, string message, params object[] args) => Add(WorkflowForgeLogLevel.Trace, message, exception);

    public void LogTrace(IDictionary<string, string> properties, string message, params object[] args) => Add(WorkflowForgeLogLevel.Trace, message, null, properties);

    public void LogTrace(IDictionary<string, string> properties, Exception exception, string message, params object[] args) => Add(WorkflowForgeLogLevel.Trace, message, exception, properties);

    public void LogDebug(string message, params object[] args) => Add(WorkflowForgeLogLevel.Debug, message);

    public void LogDebug(Exception exception, string message, params object[] args) => Add(WorkflowForgeLogLevel.Debug, message, exception);

    public void LogDebug(IDictionary<string, string> properties, string message, params object[] args) => Add(WorkflowForgeLogLevel.Debug, message, null, properties);

    public void LogDebug(IDictionary<string, string> properties, Exception exception, string message, params object[] args) => Add(WorkflowForgeLogLevel.Debug, message, exception, properties);

    public void LogInformation(string message, params object[] args) => Add(WorkflowForgeLogLevel.Information, message);

    public void LogInformation(Exception exception, string message, params object[] args) => Add(WorkflowForgeLogLevel.Information, message, exception);

    public void LogInformation(IDictionary<string, string> properties, string message, params object[] args) => Add(WorkflowForgeLogLevel.Information, message, null, properties);

    public void LogInformation(IDictionary<string, string> properties, Exception exception, string message, params object[] args) => Add(WorkflowForgeLogLevel.Information, message, exception, properties);

    public void LogWarning(string message, params object[] args) => Add(WorkflowForgeLogLevel.Warning, message);

    public void LogWarning(Exception exception, string message, params object[] args) => Add(WorkflowForgeLogLevel.Warning, message, exception);

    public void LogWarning(IDictionary<string, string> properties, string message, params object[] args) => Add(WorkflowForgeLogLevel.Warning, message, null, properties);

    public void LogWarning(IDictionary<string, string> properties, Exception exception, string message, params object[] args) => Add(WorkflowForgeLogLevel.Warning, message, exception, properties);

    public void LogError(string message, params object[] args) => Add(WorkflowForgeLogLevel.Error, message);

    public void LogError(Exception exception, string message, params object[] args) => Add(WorkflowForgeLogLevel.Error, message, exception);

    public void LogError(IDictionary<string, string> properties, string message, params object[] args) => Add(WorkflowForgeLogLevel.Error, message, null, properties);

    public void LogError(IDictionary<string, string> properties, Exception exception, string message, params object[] args) => Add(WorkflowForgeLogLevel.Error, message, exception, properties);

    public void LogCritical(string message, params object[] args) => Add(WorkflowForgeLogLevel.Critical, message);

    public void LogCritical(Exception exception, string message, params object[] args) => Add(WorkflowForgeLogLevel.Critical, message, exception);

    public void LogCritical(IDictionary<string, string> properties, string message, params object[] args) => Add(WorkflowForgeLogLevel.Critical, message, null, properties);

    public void LogCritical(IDictionary<string, string> properties, Exception exception, string message, params object[] args) => Add(WorkflowForgeLogLevel.Critical, message, exception, properties);

    private sealed class NoOpScope : IDisposable
    {
        public static readonly NoOpScope Instance = new();

        public void Dispose()
        {
        }
    }
}
