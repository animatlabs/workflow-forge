using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Loggers;
using WorkflowForge.Operations;
using WorkflowForge.Options.Middleware;
using LoggingMiddlewareImpl = WorkflowForge.Middleware.LoggingMiddleware;

namespace WorkflowForge.Tests.MiddlewareTests;

public class LoggingMiddlewareShould : IDisposable
{
    private readonly IWorkflowFoundry _foundry;
    private readonly TestOperation _operation;

    public LoggingMiddlewareShould()
    {
        _foundry = WorkflowForge.CreateFoundry("LoggingTest");
        _operation = new TestOperation();
    }

    public void Dispose()
    {
        (_foundry as IDisposable)?.Dispose();
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullLogger()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LoggingMiddlewareImpl(null!));
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullOptions()
    {
        var logger = NullLogger.Instance;
        Assert.Throws<ArgumentNullException>(() =>
            new LoggingMiddlewareImpl(logger, null!));
    }

    [Fact]
    public void CreateInstance_GivenLoggerOnly()
    {
        var logger = NullLogger.Instance;
        var middleware = new LoggingMiddlewareImpl(logger);

        Assert.NotNull(middleware);
    }

    [Fact]
    public void CreateInstance_GivenLoggerAndOptions()
    {
        var logger = NullLogger.Instance;
        var options = new LoggingMiddlewareOptions();
        var middleware = new LoggingMiddlewareImpl(logger, options);

        Assert.NotNull(middleware);
    }

    [Fact]
    public async Task ReturnResult_GivenExecuteAsyncWithSuccessfulExecution()
    {
        var logger = NullLogger.Instance;
        var middleware = new LoggingMiddlewareImpl(logger);

        const string expectedResult = "test-result";
        Task<object?> Next(CancellationToken _) => Task.FromResult<object?>(expectedResult);

        var result = await middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None);

        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task LogDataPayloads_GivenOptionsEnabled()
    {
        var logger = NullLogger.Instance;
        var options = new LoggingMiddlewareOptions { LogDataPayloads = true };
        var middleware = new LoggingMiddlewareImpl(logger, options);

        const string inputData = "input-data";
        const string expectedResult = "result-data";
        Task<object?> Next(CancellationToken _) => Task.FromResult<object?>(expectedResult);

        var result = await middleware.ExecuteAsync(_operation, _foundry, inputData, Next, CancellationToken.None);

        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task LogDataPayloads_GivenNullInputAndResult()
    {
        var logger = NullLogger.Instance;
        var options = new LoggingMiddlewareOptions { LogDataPayloads = true };
        var middleware = new LoggingMiddlewareImpl(logger, options);

        Task<object?> Next(CancellationToken _) => Task.FromResult<object?>(null);

        var result = await middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task RethrowException_GivenExecuteAsyncWhenNextThrows()
    {
        var logger = NullLogger.Instance;
        var middleware = new LoggingMiddlewareImpl(logger);

        Task<object?> Next(CancellationToken _) => throw new InvalidOperationException("test error");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None));
    }

    private sealed class TestOperation : IWorkflowOperation
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name => "TestOperation";

        public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
            => Task.FromResult<object?>(null);

        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void Dispose()
        { }
    }

    [Fact]
    public async Task LogOperationFailure_GivenDefaultOptions()
    {
        var logger = new RecordingLogger();
        var middleware = new LoggingMiddlewareImpl(logger, new LoggingMiddlewareOptions());
        var failure = new InvalidOperationException("boom");

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.ExecuteAsync(
            _operation,
            _foundry,
            inputData: null,
            next: _ => throw failure));

        Assert.Contains(logger.Errors, e => ReferenceEquals(e, failure));
    }

    [Fact]
    public async Task EstablishOperationScope_GivenDefaultOptions()
    {
        var logger = new RecordingLogger();
        var middleware = new LoggingMiddlewareImpl(logger, new LoggingMiddlewareOptions());

        await middleware.ExecuteAsync(_operation, _foundry, inputData: null, next: _ => Task.FromResult<object?>(null));

        Assert.Contains("MiddlewareExecution", logger.Scopes);
    }

    [Fact]
    public async Task NotEmitTraceMessages_GivenDefaultMinimumLevel()
    {
        var logger = new RecordingLogger();
        var middleware = new LoggingMiddlewareImpl(logger, new LoggingMiddlewareOptions());

        await middleware.ExecuteAsync(_operation, _foundry, inputData: null, next: _ => Task.FromResult<object?>(null));

        Assert.Empty(logger.TraceMessages);
    }

    [Fact]
    public async Task EmitTraceMessages_GivenTraceMinimumLevel()
    {
        var logger = new RecordingLogger();
        var options = new LoggingMiddlewareOptions { MinimumLevel = "Trace" };
        var middleware = new LoggingMiddlewareImpl(logger, options);

        await middleware.ExecuteAsync(_operation, _foundry, inputData: null, next: _ => Task.FromResult<object?>(null));

        Assert.Equal(2, logger.TraceMessages.Count);
    }

    private sealed class RecordingLogger : IWorkflowForgeLogger
    {
        public System.Collections.Generic.List<string> TraceMessages { get; } = new();

        public System.Collections.Generic.List<Exception> Errors { get; } = new();

        public System.Collections.Generic.List<string> Scopes { get; } = new();

        public bool IsEnabled(WorkflowForgeLogLevel level) => true;

        public IDisposable BeginScope<TState>(TState state, System.Collections.Generic.IDictionary<string, string>? properties = null)
        {
            Scopes.Add(state?.ToString() ?? string.Empty);
            return new NoOpScope();
        }

        public void LogTrace(string message, params object[] args) => TraceMessages.Add(message);

        public void LogTrace(System.Collections.Generic.IDictionary<string, string> properties, string message, params object[] args)
            => TraceMessages.Add(message);

        public void LogTrace(Exception exception, string message, params object[] args) => TraceMessages.Add(message);

        public void LogTrace(System.Collections.Generic.IDictionary<string, string> properties, Exception exception, string message, params object[] args)
            => TraceMessages.Add(message);

        public void LogDebug(string message, params object[] args) { }

        public void LogDebug(System.Collections.Generic.IDictionary<string, string> properties, string message, params object[] args) { }

        public void LogDebug(Exception exception, string message, params object[] args) { }

        public void LogDebug(System.Collections.Generic.IDictionary<string, string> properties, Exception exception, string message, params object[] args) { }

        public void LogInformation(string message, params object[] args) { }

        public void LogInformation(System.Collections.Generic.IDictionary<string, string> properties, string message, params object[] args) { }

        public void LogInformation(Exception exception, string message, params object[] args) { }

        public void LogInformation(System.Collections.Generic.IDictionary<string, string> properties, Exception exception, string message, params object[] args) { }

        public void LogWarning(string message, params object[] args) { }

        public void LogWarning(System.Collections.Generic.IDictionary<string, string> properties, string message, params object[] args) { }

        public void LogWarning(Exception exception, string message, params object[] args) { }

        public void LogWarning(System.Collections.Generic.IDictionary<string, string> properties, Exception exception, string message, params object[] args) { }

        public void LogError(string message, params object[] args) { }

        public void LogError(System.Collections.Generic.IDictionary<string, string> properties, string message, params object[] args) { }

        public void LogError(Exception exception, string message, params object[] args) => Errors.Add(exception);

        public void LogError(System.Collections.Generic.IDictionary<string, string> properties, Exception exception, string message, params object[] args)
            => Errors.Add(exception);

        public void LogCritical(string message, params object[] args) { }

        public void LogCritical(System.Collections.Generic.IDictionary<string, string> properties, string message, params object[] args) { }

        public void LogCritical(Exception exception, string message, params object[] args) { }

        public void LogCritical(System.Collections.Generic.IDictionary<string, string> properties, Exception exception, string message, params object[] args) { }

        private sealed class NoOpScope : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
