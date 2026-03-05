using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Exceptions;
using WorkflowForge.Extensions.Resilience.Polly.Options;
using WorkflowForge.Testing;
using TestNullLogger = WorkflowForge.Testing.TestNullLogger;

namespace WorkflowForge.Extensions.Resilience.Polly.Tests;

public class PollyRetryOperationShould : IDisposable
{
    private readonly IWorkflowFoundry _foundry;

    public PollyRetryOperationShould()
    {
        _foundry = new FakeWorkflowFoundry();
    }

    public void Dispose()
    {
        (_foundry as IDisposable)?.Dispose();
    }

    [Fact]
    public void ReturnOperationWithCorrectName_GivenWithRetryPolicy()
    {
        var inner = new FakeWorkflowOperation();
        var op = PollyRetryOperation.WithRetryPolicy(inner, maxRetryAttempts: 5);

        Assert.Equal($"PollyRetry({inner.Name})", op.Name);
    }

    [Fact]
    public void SetName_GivenWithRetryPolicyCustomName()
    {
        var inner = new FakeWorkflowOperation();
        var op = PollyRetryOperation.WithRetryPolicy(inner, name: "CustomPollyRetry");

        Assert.Equal("CustomPollyRetry", op.Name);
    }

    [Fact]
    public async Task ReturnResult_GivenForgeAsyncSuccessfulExecution()
    {
        const string expected = "success";
        var inner = new FakeWorkflowOperation { Result = expected };
        var op = PollyRetryOperation.WithRetryPolicy(inner);

        var result = await op.ForgeAsync(null, _foundry, CancellationToken.None);

        Assert.Equal(expected, result);
        Assert.Equal(1, inner.ForgeAsyncCallCount);
    }

    [Fact]
    public async Task RetryOnFailure_GivenForgeAsync()
    {
        var inner = new FakeWorkflowOperation { FailCount = 2, Result = "ok" };
        var op = PollyRetryOperation.WithRetryPolicy(
            inner,
            maxRetryAttempts: 5,
            baseDelay: TimeSpan.FromMilliseconds(10),
            maxDelay: TimeSpan.FromMilliseconds(50));

        var result = await op.ForgeAsync(null, _foundry, CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Equal(3, inner.ForgeAsyncCallCount);
    }

    [Fact]
    public async Task ThrowOriginalException_GivenForgeAsyncExhaustsRetries()
    {
        var inner = new FakeWorkflowOperation { FailCount = 10 };
        var op = PollyRetryOperation.WithRetryPolicy(
            inner,
            maxRetryAttempts: 2,
            baseDelay: TimeSpan.FromMilliseconds(5),
            maxDelay: TimeSpan.FromMilliseconds(20));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            op.ForgeAsync(null, _foundry, CancellationToken.None));

        Assert.Contains("Attempt", ex.Message);
        Assert.Equal(3, inner.ForgeAsyncCallCount);
    }

    [Fact]
    public async Task ThrowArgumentNullException_GivenForgeAsyncWithNullFoundry()
    {
        var inner = new FakeWorkflowOperation();
        var op = PollyRetryOperation.WithRetryPolicy(inner);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            op.ForgeAsync(null, null!, CancellationToken.None));
    }

    [Fact]
    public async Task DelegateToInnerOperation_GivenRestoreAsync()
    {
        var inner = new FakeWorkflowOperation();
        var op = PollyRetryOperation.WithRetryPolicy(inner);

        await op.RestoreAsync("output-data", _foundry, CancellationToken.None);

        Assert.True(inner.RestoreAsyncCalled);
        Assert.Equal("output-data", inner.LastRestoreOutput);
    }

    [Fact]
    public async Task RetryOnRestoreFailure_GivenRestoreAsync()
    {
        var inner = new FakeWorkflowOperation { RestoreFailCount = 2 };
        var op = PollyRetryOperation.WithRetryPolicy(
            inner,
            maxRetryAttempts: 5,
            baseDelay: TimeSpan.FromMilliseconds(10),
            maxDelay: TimeSpan.FromMilliseconds(50));

        await op.RestoreAsync("data", _foundry, CancellationToken.None);

        Assert.True(inner.RestoreAsyncCalled);
        Assert.Equal(3, inner.RestoreAsyncCallCount);
    }

    [Fact]
    public void DisposeInnerOperation_GivenDispose()
    {
        var inner = new FakeWorkflowOperation();
        var op = PollyRetryOperation.WithRetryPolicy(inner);

        op.Dispose();

        Assert.True(inner.Disposed);
    }

    [Fact]
    public async Task ThrowObjectDisposedException_GivenForgeAsyncAfterDispose()
    {
        var inner = new FakeWorkflowOperation();
        var op = PollyRetryOperation.WithRetryPolicy(inner);
        op.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            op.ForgeAsync(null, _foundry, CancellationToken.None));
    }

    [Fact]
    public async Task ThrowObjectDisposedException_GivenRestoreAsyncAfterDispose()
    {
        var inner = new FakeWorkflowOperation();
        var op = PollyRetryOperation.WithRetryPolicy(inner);
        op.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            op.RestoreAsync(null, _foundry, CancellationToken.None));
    }

    [Fact]
    public void ReturnOperation_GivenWithCircuitBreakerPolicy()
    {
        var inner = new FakeWorkflowOperation();
        var op = PollyRetryOperation.WithCircuitBreakerPolicy(inner, failureThreshold: 5);

        Assert.NotNull(op);
        Assert.NotEqual(Guid.Empty, op.Id);
    }

    [Fact]
    public async Task ReturnResult_GivenWithCircuitBreakerPolicyForgeAsync()
    {
        var inner = new FakeWorkflowOperation { Result = "ok" };
        var op = PollyRetryOperation.WithCircuitBreakerPolicy(
            inner,
            failureThreshold: 5,
            durationOfBreak: TimeSpan.FromSeconds(1));

        var result = await op.ForgeAsync(null, _foundry, CancellationToken.None);

        Assert.Equal("ok", result);
    }

    [Fact]
    public void ReturnOperation_GivenWithComprehensivePolicy()
    {
        var inner = new FakeWorkflowOperation();
        var settings = new PollyMiddlewareOptions
        {
            Retry = { IsEnabled = true, MaxRetryAttempts = 3 },
            CircuitBreaker = { IsEnabled = false },
            Timeout = { IsEnabled = false }
        };

        var op = PollyRetryOperation.WithComprehensivePolicy(inner, settings);

        Assert.NotNull(op);
    }

    [Fact]
    public async Task ReturnResult_GivenWithComprehensivePolicyForgeAsync()
    {
        var inner = new FakeWorkflowOperation { Result = "ok" };
        var settings = new PollyMiddlewareOptions
        {
            Retry = { IsEnabled = true, MaxRetryAttempts = 2 },
            CircuitBreaker = { IsEnabled = false },
            Timeout = { IsEnabled = false }
        };

        var op = PollyRetryOperation.WithComprehensivePolicy(inner, settings);
        var result = await op.ForgeAsync(null, _foundry, CancellationToken.None);

        Assert.Equal("ok", result);
    }

    [Fact]
    public void ReturnWrappedOperation_GivenWithPollyRetryExtension()
    {
        var inner = new FakeWorkflowOperation();

        var op = inner.WithPollyRetry(maxRetryAttempts: 4);

        Assert.NotNull(op);
        Assert.IsType<PollyRetryOperation>(op);
    }

    [Fact]
    public void ReturnWrappedOperation_GivenWithPollyCircuitBreakerExtension()
    {
        var inner = new FakeWorkflowOperation();

        var op = inner.WithPollyCircuitBreaker(failureThreshold: 7);

        Assert.NotNull(op);
        Assert.IsType<PollyRetryOperation>(op);
    }

    [Fact]
    public void ReturnWrappedOperation_GivenWithPollyComprehensiveExtension()
    {
        var inner = new FakeWorkflowOperation();
        var settings = new PollyMiddlewareOptions { Retry = { IsEnabled = true } };

        var op = inner.WithPollyComprehensive(settings);

        Assert.NotNull(op);
        Assert.IsType<PollyRetryOperation>(op);
    }

    [Fact]
    public void NotPropagate_GivenDisposeWhenInnerThrowsOnDispose()
    {
        var inner = new FakeWorkflowOperation { ThrowOnDispose = true };
        var op = PollyRetryOperation.WithRetryPolicy(inner);

        var ex = Record.Exception(() => op.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public async Task ThrowWorkflowOperationException_GivenForgeAsyncWithCircuitBreakerAfterEnoughFailures()
    {
        var inner = new FakeWorkflowOperation { FailCount = 100 };
        var op = PollyRetryOperation.WithCircuitBreakerPolicy(
            inner,
            failureThreshold: 5,
            durationOfBreak: TimeSpan.FromSeconds(5));

        for (int i = 0; i < 5; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                op.ForgeAsync(null, _foundry, CancellationToken.None));
        }

        var ex = await Assert.ThrowsAsync<WorkflowOperationException>(() =>
            op.ForgeAsync(null, _foundry, CancellationToken.None));

        Assert.Contains("Circuit breaker is open", ex.Message);
    }

    [Fact]
    public async Task ThrowWorkflowOperationException_GivenForgeAsyncWhenTimeoutRejected()
    {
        var inner = new FakeWorkflowOperationWithDelay(TimeSpan.FromSeconds(1));
        var settings = new PollyMiddlewareOptions
        {
            Retry = { IsEnabled = false },
            CircuitBreaker = { IsEnabled = false },
            Timeout =
            {
                IsEnabled = true,
                DefaultTimeout = TimeSpan.FromMilliseconds(50)
            }
        };

        var op = PollyRetryOperation.WithComprehensivePolicy(inner, settings);

        var ex = await Assert.ThrowsAsync<WorkflowOperationException>(() =>
            op.ForgeAsync(null, _foundry, CancellationToken.None));

        Assert.Contains("timed out", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReturnResult_GivenWithComprehensivePolicyAllSettingsEnabled()
    {
        var inner = new FakeWorkflowOperation { Result = "comprehensive-ok" };
        var settings = new PollyMiddlewareOptions
        {
            Retry = { IsEnabled = true, MaxRetryAttempts = 2, BaseDelay = TimeSpan.FromMilliseconds(5), UseJitter = true },
            CircuitBreaker = { IsEnabled = true, FailureThreshold = 5, BreakDuration = TimeSpan.FromSeconds(1), MinimumThroughput = 10 },
            Timeout = { IsEnabled = true, DefaultTimeout = TimeSpan.FromSeconds(30) },
            EnableDetailedLogging = true
        };

        var op = PollyRetryOperation.WithComprehensivePolicy(inner, settings, logger: TestNullLogger.Instance);
        var result = await op.ForgeAsync(null, _foundry, CancellationToken.None);

        Assert.Equal("comprehensive-ok", result);
    }

    [Fact]
    public async Task RetryWithDetailedLogging_GivenWithComprehensivePolicyAndTransientFailure()
    {
        var inner = new FakeWorkflowOperation { FailCount = 1, Result = "ok" };
        var settings = new PollyMiddlewareOptions
        {
            Retry = { IsEnabled = true, MaxRetryAttempts = 3, BaseDelay = TimeSpan.FromMilliseconds(5) },
            CircuitBreaker = { IsEnabled = false },
            Timeout = { IsEnabled = false },
            EnableDetailedLogging = true
        };

        var op = PollyRetryOperation.WithComprehensivePolicy(inner, settings, logger: TestNullLogger.Instance);
        var result = await op.ForgeAsync(null, _foundry, CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Equal(2, inner.ForgeAsyncCallCount);
    }

    [Fact]
    public async Task RetryWithoutDetailedLogging_GivenWithComprehensivePolicyDisabledLogging()
    {
        var inner = new FakeWorkflowOperation { FailCount = 1, Result = "ok" };
        var settings = new PollyMiddlewareOptions
        {
            Retry = { IsEnabled = true, MaxRetryAttempts = 3, BaseDelay = TimeSpan.FromMilliseconds(5) },
            CircuitBreaker = { IsEnabled = false },
            Timeout = { IsEnabled = false },
            EnableDetailedLogging = false
        };

        var op = PollyRetryOperation.WithComprehensivePolicy(inner, settings, logger: TestNullLogger.Instance);
        var result = await op.ForgeAsync(null, _foundry, CancellationToken.None);

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task RestoreSuccessfully_GivenRestoreAsyncWithCircuitBreakerPolicy()
    {
        var inner = new FakeWorkflowOperation();
        var op = PollyRetryOperation.WithCircuitBreakerPolicy(inner, failureThreshold: 5, durationOfBreak: TimeSpan.FromSeconds(1));

        await op.RestoreAsync("data", _foundry, CancellationToken.None);

        Assert.True(inner.RestoreAsyncCalled);
    }

    [Fact]
    public async Task ThrowOnRestoreFailure_GivenRestoreAsyncExhaustsRetries()
    {
        var inner = new FakeWorkflowOperation { RestoreFailCount = 100 };
        var op = PollyRetryOperation.WithRetryPolicy(
            inner,
            maxRetryAttempts: 1,
            baseDelay: TimeSpan.FromMilliseconds(5),
            maxDelay: TimeSpan.FromMilliseconds(10),
            logger: TestNullLogger.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            op.RestoreAsync("data", _foundry, CancellationToken.None));
    }

    [Fact]
    public void SetCustomName_GivenWithRetryPolicyWithLogger()
    {
        var inner = new FakeWorkflowOperation();
        var logger = TestNullLogger.Instance;

        var op = PollyRetryOperation.WithRetryPolicy(inner, maxRetryAttempts: 2, logger: logger, name: "CustomOp");

        Assert.Equal("CustomOp", op.Name);
    }

    [Fact]
    public void SetCustomName_GivenWithCircuitBreakerPolicyWithLogger()
    {
        var inner = new FakeWorkflowOperation();

        var op = PollyRetryOperation.WithCircuitBreakerPolicy(inner, logger: TestNullLogger.Instance, name: "CBOp");

        Assert.Equal("CBOp", op.Name);
    }

    [Fact]
    public void SetCustomName_GivenWithComprehensivePolicyWithName()
    {
        var inner = new FakeWorkflowOperation();
        var settings = new PollyMiddlewareOptions { Retry = { IsEnabled = true } };

        var op = PollyRetryOperation.WithComprehensivePolicy(inner, settings, name: "CompOp");

        Assert.Equal("CompOp", op.Name);
    }

    [Fact]
    public void SetDefaultName_GivenWithRetryPolicyNoName()
    {
        var inner = new FakeWorkflowOperation { Name = "InnerOp" };

        var op = PollyRetryOperation.WithRetryPolicy(inner);

        Assert.Equal("PollyRetry(InnerOp)", op.Name);
    }

    private sealed class FakeWorkflowOperation : IWorkflowOperation
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; set; } = "TestOperation";
        public object? Result { get; set; } = "ok";
        public int FailCount { get; set; }
        public int ForgeAsyncCallCount { get; private set; }
        public bool RestoreAsyncCalled { get; private set; }
        public int RestoreFailCount { get; set; }
        public int RestoreAsyncCallCount { get; private set; }
        public object? LastRestoreOutput { get; private set; }
        public bool Disposed { get; private set; }
        public bool ThrowOnDispose { get; set; }

        public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            ForgeAsyncCallCount++;
            if (ForgeAsyncCallCount <= FailCount)
                throw new InvalidOperationException($"Attempt {ForgeAsyncCallCount} failed");
            return Task.FromResult<object?>(Result);
        }

        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            RestoreAsyncCallCount++;
            if (RestoreAsyncCallCount <= RestoreFailCount)
                throw new InvalidOperationException($"Restore attempt {RestoreAsyncCallCount} failed");
            RestoreAsyncCalled = true;
            LastRestoreOutput = outputData;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (ThrowOnDispose)
                throw new InvalidOperationException("Dispose failed");
            Disposed = true;
        }
    }

    private sealed class FakeWorkflowOperationWithDelay : IWorkflowOperation
    {
        private readonly TimeSpan _delay;

        public FakeWorkflowOperationWithDelay(TimeSpan delay)
        {
            _delay = delay;
        }

        public Guid Id { get; } = Guid.NewGuid();
        public string Name => "DelayedOperation";

        public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            await Task.Delay(_delay, cancellationToken);
            return "done";
        }

        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
