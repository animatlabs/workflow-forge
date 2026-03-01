using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Resilience.Polly;
using WorkflowForge.Testing;
using Xunit;

namespace WorkflowForge.Extensions.Resilience.Polly.Tests;

public class PollyMiddlewareShould : IDisposable
{
    private readonly IWorkflowFoundry _foundry;
    private readonly TestOperation _operation;

        public PollyMiddlewareShould()
    {
        _foundry = new FakeWorkflowFoundry();
        _operation = new TestOperation();
    }

    public void Dispose()
    {
        (_foundry as IDisposable)?.Dispose();
    }

    [Fact]
    public void ReturnMiddlewareWithCorrectName_GivenWithRetryPolicy()
    {
        var logger = TestNullLogger.Instance;

        var middleware = PollyMiddleware.WithRetryPolicy(logger, maxRetryAttempts: 5);

        Assert.Equal("PollyRetry(attempts:5)", middleware.Name);
    }

    [Fact]
    public void SetName_GivenWithRetryPolicyCustomName()
    {
        var logger = TestNullLogger.Instance;

        var middleware = PollyMiddleware.WithRetryPolicy(logger, name: "CustomRetry");

        Assert.Equal("CustomRetry", middleware.Name);
    }

    [Fact]
    public async Task ReturnResult_GivenExecuteAsyncWithRetryPolicySuccessfulExecution()
    {
        var logger = TestNullLogger.Instance;
        var middleware = PollyMiddleware.WithRetryPolicy(logger);
        const string expectedResult = "test-result";

        Task<object?> Next(CancellationToken _) => Task.FromResult<object?>(expectedResult);

        var result = await middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None);

        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task InvokeNextDelegate_GivenExecuteAsyncWithRetryPolicy()
    {
        var logger = TestNullLogger.Instance;
        var middleware = PollyMiddleware.WithRetryPolicy(logger);
        var nextCalled = false;

        Task<object?> Next(CancellationToken _)
        {
            nextCalled = true;
            return Task.FromResult<object?>("ok");
        }

        await middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task RetryOnFailure_GivenExecuteAsyncWithRetryPolicy()
    {
        var logger = TestNullLogger.Instance;
        var middleware = PollyMiddleware.WithRetryPolicy(
            logger,
            maxRetryAttempts: 3,
            baseDelay: TimeSpan.FromMilliseconds(10),
            maxDelay: TimeSpan.FromMilliseconds(50));
        var attemptCount = 0;

        Task<object?> Next(CancellationToken _)
        {
            attemptCount++;
            if (attemptCount < 3)
                throw new InvalidOperationException("Transient");
            return Task.FromResult<object?>("success");
        }

        var result = await middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None);

        Assert.Equal("success", result);
        Assert.Equal(3, attemptCount);
    }

    [Fact]
    public async Task ThrowException_GivenExecuteAsyncWithRetryPolicyExhaustsRetries()
    {
        var logger = TestNullLogger.Instance;
        var middleware = PollyMiddleware.WithRetryPolicy(
            logger,
            maxRetryAttempts: 2,
            baseDelay: TimeSpan.FromMilliseconds(5),
            maxDelay: TimeSpan.FromMilliseconds(20));

        Task<object?> Next(CancellationToken _) => throw new InvalidOperationException("Persistent");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None));

        Assert.Equal("Persistent", ex.Message);
    }

    [Fact]
    public void ReturnCircuitBreakerMiddlewareWithCorrectName_GivenWithCircuitBreakerPolicy()
    {
        var logger = TestNullLogger.Instance;

        var middleware = PollyMiddleware.WithCircuitBreakerPolicy(logger, failureThreshold: 7);

        Assert.Equal("PollyCircuitBreaker(threshold:7)", middleware.Name);
    }

    [Fact]
    public async Task ReturnResult_GivenExecuteAsyncWithCircuitBreakerPolicy()
    {
        var logger = TestNullLogger.Instance;
        var middleware = PollyMiddleware.WithCircuitBreakerPolicy(
            logger,
            failureThreshold: 5,
            durationOfBreak: TimeSpan.FromSeconds(1));

        Task<object?> Next(CancellationToken _) => Task.FromResult<object?>("ok");

        var result = await middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None);

        Assert.Equal("ok", result);
    }

    [Fact]
    public void ReturnTimeoutMiddlewareWithCorrectName_GivenWithTimeoutPolicy()
    {
        var logger = TestNullLogger.Instance;

        var middleware = PollyMiddleware.WithTimeoutPolicy(logger, TimeSpan.FromSeconds(30));

        Assert.Equal("PollyTimeout(30s)", middleware.Name);
    }

    [Fact]
    public async Task CompleteWithinTimeout_GivenExecuteAsyncWithTimeoutPolicy()
    {
        var logger = TestNullLogger.Instance;
        var middleware = PollyMiddleware.WithTimeoutPolicy(logger, TimeSpan.FromSeconds(5));

        Task<object?> Next(CancellationToken _) => Task.FromResult<object?>("done");

        var result = await middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None);

        Assert.Equal("done", result);
    }

    [Fact]
    public void ReturnComprehensiveMiddlewareWithCorrectName_GivenWithComprehensivePolicy()
    {
        var logger = TestNullLogger.Instance;

        var middleware = PollyMiddleware.WithComprehensivePolicy(logger);

        Assert.Equal("PollyComprehensive", middleware.Name);
    }

    [Fact]
    public async Task ReturnResult_GivenExecuteAsyncWithComprehensivePolicy()
    {
        var logger = TestNullLogger.Instance;
        var middleware = PollyMiddleware.WithComprehensivePolicy(
            logger,
            maxRetryAttempts: 2,
            baseDelay: TimeSpan.FromMilliseconds(5),
            circuitBreakerThreshold: 5,
            circuitBreakerDuration: TimeSpan.FromSeconds(1),
            timeoutDuration: TimeSpan.FromSeconds(10));

        Task<object?> Next(CancellationToken _) => Task.FromResult<object?>("ok");

        var result = await middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None);

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task ThrowBrokenCircuitException_GivenExecuteAsyncWithCircuitBreakerPolicyAfterEnoughFailures()
    {
        var logger = TestNullLogger.Instance;
        var middleware = PollyMiddleware.WithCircuitBreakerPolicy(
            logger,
            failureThreshold: 5,
            durationOfBreak: TimeSpan.FromSeconds(5));

        Task<object?> Next(CancellationToken _) => throw new InvalidOperationException("fail");

        for (int i = 0; i < 5; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None));
        }

        // Use ThrowsAnyAsync: Polly is ILRepacked into the extension, so the exception type
        // may come from a different assembly than the test's Polly reference.
        var ex = await Assert.ThrowsAnyAsync<Exception>(() =>
            middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None));

        Assert.Equal("BrokenCircuitException", ex.GetType().Name);
        Assert.Contains("Circuit breaker is open", ex.Message);
    }

    [Fact]
    public async Task ThrowTimeoutRejectedException_GivenExecuteAsyncWithTimeoutPolicyTimesOut()
    {
        var logger = TestNullLogger.Instance;
        var middleware = PollyMiddleware.WithTimeoutPolicy(logger, TimeSpan.FromMilliseconds(50));

        async Task<object?> Next(CancellationToken ct)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            return null;
        }

        // Use ThrowsAnyAsync: Polly is ILRepacked into the extension, so the exception type
        // may come from a different assembly than the test's Polly reference.
        var ex = await Assert.ThrowsAnyAsync<Exception>(() =>
            middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None));

        Assert.Equal("TimeoutRejectedException", ex.GetType().Name);
    }

    [Fact]
    public async Task PassTokenToNext_GivenExecuteAsyncWithCancellationToken()
    {
        var logger = TestNullLogger.Instance;
        var middleware = PollyMiddleware.WithRetryPolicy(logger);
        using var cts = new CancellationTokenSource();
        var tokenPassed = false;

        Task<object?> Next(CancellationToken ct)
        {
            tokenPassed = ct == cts.Token;
            return Task.FromResult<object?>("ok");
        }

        await middleware.ExecuteAsync(_operation, _foundry, null, Next, cts.Token);

        Assert.True(tokenPassed);
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
}
