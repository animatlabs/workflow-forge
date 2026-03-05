using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Options.Middleware;
using LoggingMiddlewareImpl = WorkflowForge.Middleware.LoggingMiddleware;

namespace WorkflowForge.Tests.Middleware;

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
        var logger = WorkflowForgeLoggers.Null;
        Assert.Throws<ArgumentNullException>(() =>
            new LoggingMiddlewareImpl(logger, null!));
    }

    [Fact]
    public void CreateInstance_GivenLoggerOnly()
    {
        var logger = WorkflowForgeLoggers.Null;
        var middleware = new LoggingMiddlewareImpl(logger);

        Assert.NotNull(middleware);
    }

    [Fact]
    public void CreateInstance_GivenLoggerAndOptions()
    {
        var logger = WorkflowForgeLoggers.Null;
        var options = new LoggingMiddlewareOptions();
        var middleware = new LoggingMiddlewareImpl(logger, options);

        Assert.NotNull(middleware);
    }

    [Fact]
    public async Task ReturnResult_GivenExecuteAsyncWithSuccessfulExecution()
    {
        var logger = WorkflowForgeLoggers.Null;
        var middleware = new LoggingMiddlewareImpl(logger);

        const string expectedResult = "test-result";
        Task<object?> Next(CancellationToken _) => Task.FromResult<object?>(expectedResult);

        var result = await middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None);

        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task LogDataPayloads_GivenOptionsEnabled()
    {
        var logger = WorkflowForgeLoggers.Null;
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
        var logger = WorkflowForgeLoggers.Null;
        var options = new LoggingMiddlewareOptions { LogDataPayloads = true };
        var middleware = new LoggingMiddlewareImpl(logger, options);

        Task<object?> Next(CancellationToken _) => Task.FromResult<object?>(null);

        var result = await middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task RethrowException_GivenExecuteAsyncWhenNextThrows()
    {
        var logger = WorkflowForgeLoggers.Null;
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
}
