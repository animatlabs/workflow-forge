using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Resilience;
using WorkflowForge.Extensions.Resilience.Abstractions;
using WF = WorkflowForge;

namespace WorkflowForge.Extensions.Resilience.Tests;

public class CircuitBreakerMiddlewareShould : IDisposable
{
    private readonly IWorkflowFoundry _foundry;
    private readonly TestOperation _operation;

        public CircuitBreakerMiddlewareShould()
    {
        _foundry = WF.WorkflowForge.CreateFoundry("CircuitBreakerTest");
        _operation = new TestOperation();
    }

    public void Dispose()
    {
        (_foundry as IDisposable)?.Dispose();
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullPolicy()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CircuitBreakerMiddleware(null!, null, null));
    }

    [Fact]
    public void SetDefaultName_GivenValidPolicy()
    {
        var policy = new FakeCircuitBreakerPolicy();
        var middleware = new CircuitBreakerMiddleware(policy);

        Assert.Equal("CircuitBreaker", middleware.Name);
        policy.Dispose();
    }

    [Fact]
    public void SetName_GivenCustomName()
    {
        var policy = new FakeCircuitBreakerPolicy();
        var middleware = new CircuitBreakerMiddleware(policy, null, "CustomCircuitBreaker");

        Assert.Equal("CustomCircuitBreaker", middleware.Name);
        policy.Dispose();
    }

    [Fact]
    public async Task ReturnResult_GivenExecuteAsyncSuccessfulExecution()
    {
        var policy = new FakeCircuitBreakerPolicy();
        var middleware = new CircuitBreakerMiddleware(policy);

        const string expectedResult = "test-result";
        Task<object?> Next(CancellationToken _) => Task.FromResult<object?>(expectedResult);

        var result = await middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None);

        Assert.Equal(expectedResult, result);
        Assert.True(policy.ExecuteAsyncCalled);
        policy.Dispose();
    }

    [Fact]
    public async Task InvokeNextDelegate_GivenExecuteAsyncSuccessfulExecution()
    {
        var policy = new FakeCircuitBreakerPolicy();
        var middleware = new CircuitBreakerMiddleware(policy);

        var nextCalled = false;
        Task<object?> Next(CancellationToken _)
        {
            nextCalled = true;
            return Task.FromResult<object?>("ok");
        }

        await middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None);

        Assert.True(nextCalled);
        policy.Dispose();
    }

    [Fact]
    public async Task RethrowException_GivenExecuteAsyncWhenPolicyThrowsCircuitBreakerOpenException()
    {
        var policy = new FakeCircuitBreakerPolicy { ThrowCircuitBreakerOpen = true };
        var middleware = new CircuitBreakerMiddleware(policy);

        Task<object?> Next(CancellationToken _) => Task.FromResult<object?>("result");

        var ex = await Assert.ThrowsAsync<CircuitBreakerOpenException>(() =>
            middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None));

        Assert.Equal("Circuit is open", ex.Message);
        policy.Dispose();
    }

    [Fact]
    public async Task RethrowException_GivenExecuteAsyncWhenPolicyThrowsGeneralException()
    {
        var policy = new FakeCircuitBreakerPolicy { ThrowGenericException = true };
        var middleware = new CircuitBreakerMiddleware(policy);

        Task<object?> Next(CancellationToken _) => Task.FromResult<object?>("result");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None));

        Assert.Equal("Generic failure", ex.Message);
        policy.Dispose();
    }

    [Fact]
    public async Task LogWarning_GivenExecuteAsyncWhenPolicyThrowsCircuitBreakerOpenExceptionWithLogger()
    {
        var policy = new FakeCircuitBreakerPolicy { ThrowCircuitBreakerOpen = true };
        var logger = new Mock<IWorkflowForgeLogger>();
        var middleware = new CircuitBreakerMiddleware(policy, logger.Object);

        Task<object?> Next(CancellationToken _) => Task.FromResult<object?>("result");

        await Assert.ThrowsAsync<CircuitBreakerOpenException>(() =>
            middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None));

        logger.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("Circuit breaker is open")),
            It.Is<object[]>(a => a.Length >= 2)), Times.Once);
        policy.Dispose();
    }

    [Fact]
    public async Task LogError_GivenExecuteAsyncWhenPolicyThrowsGeneralExceptionWithLogger()
    {
        var policy = new FakeCircuitBreakerPolicy { ThrowGenericException = true };
        var logger = new Mock<IWorkflowForgeLogger>();
        var middleware = new CircuitBreakerMiddleware(policy, logger.Object);

        Task<object?> Next(CancellationToken _) => Task.FromResult<object?>("result");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None));

        logger.Verify(l => l.LogError(It.IsAny<Exception>(), It.Is<string>(s => s.Contains("failed in circuit breaker")),
            It.Is<object[]>(a => a.Length >= 1)), Times.Once);
        policy.Dispose();
    }

    [Fact]
    public void DisposePolicy_GivenDispose()
    {
        var policy = new FakeCircuitBreakerPolicy();
        var middleware = new CircuitBreakerMiddleware(policy);

        middleware.Dispose();

        Assert.True(policy.Disposed);
    }

    [Fact]
    public void NotThrow_GivenDisposeCanBeCalledMultipleTimes()
    {
        var policy = new FakeCircuitBreakerPolicy();
        var middleware = new CircuitBreakerMiddleware(policy);

        middleware.Dispose();
        middleware.Dispose();
    }

    [Fact]
    public async Task PassTokenToPolicy_GivenExecuteAsyncWithCancellationToken()
    {
        var policy = new FakeCircuitBreakerPolicy();
        var middleware = new CircuitBreakerMiddleware(policy);
        using var cts = new CancellationTokenSource();
        var tokenPassed = false;

        Task<object?> Next(CancellationToken ct)
        {
            tokenPassed = ct == cts.Token;
            return Task.FromResult<object?>("ok");
        }

        await middleware.ExecuteAsync(_operation, _foundry, null, Next, cts.Token);

        Assert.True(tokenPassed);
        Assert.Equal(cts.Token, policy.LastCancellationToken);
        policy.Dispose();
    }

    private sealed class TestOperation : IWorkflowOperation
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name => "TestOperation";

        public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
            => Task.FromResult<object?>(null);

        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void Dispose() { }
    }

    private sealed class FakeCircuitBreakerPolicy : ICircuitBreakerPolicy
    {
        public bool ExecuteAsyncCalled { get; private set; }
        public bool Disposed { get; private set; }
        public bool ThrowCircuitBreakerOpen { get; set; }
        public bool ThrowGenericException { get; set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public CircuitBreakerState State => CircuitBreakerState.Closed;

#pragma warning disable CS0067
        public event EventHandler<CircuitBreakerStateChangedEventArgs>? StateChanged;
#pragma warning restore CS0067

        public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
        {
            ExecuteAsyncCalled = true;
            LastCancellationToken = cancellationToken;

            if (ThrowCircuitBreakerOpen)
                throw new CircuitBreakerOpenException("Circuit is open");

            if (ThrowGenericException)
                throw new InvalidOperationException("Generic failure");

            await operation().ConfigureAwait(false);
        }

        public void Dispose()
        {
            Disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
