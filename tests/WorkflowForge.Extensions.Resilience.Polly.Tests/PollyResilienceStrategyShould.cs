using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Resilience.Abstractions;

namespace WorkflowForge.Extensions.Resilience.Polly.Tests;

public class PollyResilienceStrategyShould
{
    [Fact]
    public void ReturnStrategyWithCorrectName_GivenCreateRetryPolicy()
    {
        var strategy = PollyResilienceStrategy.CreateRetryPolicy(maxRetryAttempts: 5);

        Assert.NotNull(strategy);
        Assert.Equal("PollyRetry(attempts:5)", strategy.Name);
    }

    [Fact]
    public async Task ReturnResult_GivenCreateRetryPolicyExecuteAsyncSuccessfulOperation()
    {
        var strategy = PollyResilienceStrategy.CreateRetryPolicy(maxRetryAttempts: 3);
        const string expected = "result";

        var result = await strategy.ExecuteAsync(() => Task.FromResult(expected), CancellationToken.None);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task Complete_GivenCreateRetryPolicyExecuteAsyncVoidSuccessfulOperation()
    {
        var strategy = PollyResilienceStrategy.CreateRetryPolicy(maxRetryAttempts: 3);
        var executed = false;

        await strategy.ExecuteAsync(() =>
        {
            executed = true;
            return Task.CompletedTask;
        }, CancellationToken.None);

        Assert.True(executed);
    }

    [Fact]
    public async Task RetryOnFailure_GivenCreateRetryPolicyExecuteAsync()
    {
        var strategy = PollyResilienceStrategy.CreateRetryPolicy(
            maxRetryAttempts: 3,
            baseDelay: TimeSpan.FromMilliseconds(10),
            maxDelay: TimeSpan.FromMilliseconds(50));
        var attemptCount = 0;

        var result = await strategy.ExecuteAsync(() =>
        {
            attemptCount++;
            if (attemptCount < 3)
                throw new InvalidOperationException("Transient failure");
            return Task.FromResult("success");
        }, CancellationToken.None);

        Assert.Equal("success", result);
        Assert.Equal(3, attemptCount);
    }

    [Fact]
    public async Task ThrowOriginalException_GivenCreateRetryPolicyExecuteAsyncExhaustsRetries()
    {
        var strategy = PollyResilienceStrategy.CreateRetryPolicy(
            maxRetryAttempts: 2,
            baseDelay: TimeSpan.FromMilliseconds(5),
            maxDelay: TimeSpan.FromMilliseconds(20));
        var attemptCount = 0;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            strategy.ExecuteAsync(() =>
            {
                attemptCount++;
                throw new InvalidOperationException("Persistent failure");
            }, CancellationToken.None));

        Assert.Equal("Persistent failure", ex.Message);
        Assert.Equal(3, attemptCount); // Initial + 2 retries
    }

    [Fact]
    public async Task ThrowArgumentNullException_GivenCreateRetryPolicyExecuteAsyncWithNullOperation()
    {
        var strategy = PollyResilienceStrategy.CreateRetryPolicy();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            strategy.ExecuteAsync((Func<Task>)null!, CancellationToken.None));
    }

    [Fact]
    public async Task ThrowArgumentNullException_GivenCreateRetryPolicyExecuteAsyncGenericWithNullOperation()
    {
        var strategy = PollyResilienceStrategy.CreateRetryPolicy();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            strategy.ExecuteAsync((Func<Task<int>>)null!, CancellationToken.None));
    }

    [Fact]
    public void ReturnCircuitBreakerStrategyWithCorrectName_GivenCreateCircuitBreakerPolicy()
    {
        var strategy = PollyResilienceStrategy.CreateCircuitBreakerPolicy(failureThreshold: 7);

        Assert.NotNull(strategy);
        Assert.Equal("PollyCircuitBreaker(threshold:7)", strategy.Name);
    }

    [Fact]
    public async Task Complete_GivenCreateCircuitBreakerPolicyExecuteAsyncSuccessfulOperation()
    {
        var strategy = PollyResilienceStrategy.CreateCircuitBreakerPolicy(
            failureThreshold: 5,
            durationOfBreak: TimeSpan.FromSeconds(1),
            minimumThroughput: 2);
        var executed = false;

        await strategy.ExecuteAsync(() =>
        {
            executed = true;
            return Task.CompletedTask;
        }, CancellationToken.None);

        Assert.True(executed);
    }

    [Fact]
    public void ReturnStrategyWithCorrectName_GivenCreateTimeoutPolicy()
    {
        var strategy = PollyResilienceStrategy.CreateTimeoutPolicy(TimeSpan.FromSeconds(30));

        Assert.NotNull(strategy);
        Assert.Equal("PollyTimeout(30s)", strategy.Name);
    }

    [Fact]
    public async Task CompleteWithinTimeout_GivenCreateTimeoutPolicyExecuteAsync()
    {
        var strategy = PollyResilienceStrategy.CreateTimeoutPolicy(TimeSpan.FromSeconds(5));
        var result = await strategy.ExecuteAsync(() => Task.FromResult(42), CancellationToken.None);

        Assert.Equal(42, result);
    }

    [Fact]
    public void ReturnStrategyWithCorrectName_GivenCreateComprehensivePolicy()
    {
        var strategy = PollyResilienceStrategy.CreateComprehensivePolicy();

        Assert.NotNull(strategy);
        Assert.Equal("PollyComprehensive", strategy.Name);
    }

    [Fact]
    public async Task Complete_GivenCreateComprehensivePolicyExecuteAsyncSuccessfulOperation()
    {
        var strategy = PollyResilienceStrategy.CreateComprehensivePolicy(
            maxRetryAttempts: 2,
            baseDelay: TimeSpan.FromMilliseconds(5),
            circuitBreakerThreshold: 5,
            circuitBreakerDuration: TimeSpan.FromSeconds(1),
            timeoutDuration: TimeSpan.FromSeconds(10));
        var executed = false;

        await strategy.ExecuteAsync(() =>
        {
            executed = true;
            return Task.CompletedTask;
        }, CancellationToken.None);

        Assert.True(executed);
    }

    [Fact]
    public async Task ReturnFalse_GivenShouldRetryAsync()
    {
        var strategy = PollyResilienceStrategy.CreateRetryPolicy();

        var result = await strategy.ShouldRetryAsync(1, new InvalidOperationException(), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public void ReturnZero_GivenGetRetryDelay()
    {
        var strategy = PollyResilienceStrategy.CreateRetryPolicy();

        var delay = strategy.GetRetryDelay(1, new InvalidOperationException());

        Assert.Equal(TimeSpan.Zero, delay);
    }

    [Fact]
    public async Task NotRetry_GivenCreateRetryPolicyExecuteAsyncOperationCanceledException()
    {
        var strategy = PollyResilienceStrategy.CreateRetryPolicy(maxRetryAttempts: 5);
        var attemptCount = 0;

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            strategy.ExecuteAsync(() =>
            {
                attemptCount++;
                throw new OperationCanceledException();
            }, CancellationToken.None));

        Assert.Equal(1, attemptCount);
    }

    [Fact]
    public async Task ExecuteSuccessfully_GivenCreateRetryPolicyWithLogger()
    {
        var logger = new Mock<IWorkflowForgeLogger>();
        var strategy = PollyResilienceStrategy.CreateRetryPolicy(
            maxRetryAttempts: 2,
            baseDelay: TimeSpan.FromMilliseconds(5),
            maxDelay: TimeSpan.FromMilliseconds(20),
            logger: logger.Object);

        var result = await strategy.ExecuteAsync(() => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task ThrowCircuitBreakerOpenException_GivenExecuteAsyncWhenCircuitOpens()
    {
        var strategy = PollyResilienceStrategy.CreateCircuitBreakerPolicy(
            failureThreshold: 5,
            durationOfBreak: TimeSpan.FromSeconds(1),
            minimumThroughput: 5);

        for (int i = 0; i < 5; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                strategy.ExecuteAsync(() => Task.FromException(new InvalidOperationException("fail")), CancellationToken.None));
        }

        await Assert.ThrowsAsync<CircuitBreakerOpenException>(() =>
            strategy.ExecuteAsync(() => Task.FromException(new InvalidOperationException("blocked")), CancellationToken.None));
    }

    [Fact]
    public async Task ThrowCircuitBreakerOpenException_GivenExecuteAsyncGenericWhenCircuitOpens()
    {
        var strategy = PollyResilienceStrategy.CreateCircuitBreakerPolicy(
            failureThreshold: 5,
            durationOfBreak: TimeSpan.FromSeconds(1),
            minimumThroughput: 5);

        for (int i = 0; i < 5; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                strategy.ExecuteAsync<int>(() => Task.FromException<int>(new InvalidOperationException("fail")), CancellationToken.None));
        }

        await Assert.ThrowsAsync<CircuitBreakerOpenException>(() =>
            strategy.ExecuteAsync<int>(() => Task.FromException<int>(new InvalidOperationException("blocked")), CancellationToken.None));
    }

    [Fact]
    public async Task ThrowTimeoutRejectedException_GivenExecuteAsyncGenericWhenOperationThrowsTimeoutRejectedException()
    {
        var strategy = PollyResilienceStrategy.CreateRetryPolicy();

        var ex = await Assert.ThrowsAnyAsync<Exception>(() =>
            strategy.ExecuteAsync<int>(() =>
            {
                throw new global::Polly.Timeout.TimeoutRejectedException("timed-out");
            }, CancellationToken.None));

        Assert.Equal("TimeoutRejectedException", ex.GetType().Name);
    }

    [Fact]
    public async Task RetryVoidOperationOnFailure_GivenCreateRetryPolicyExecuteAsync()
    {
        var strategy = PollyResilienceStrategy.CreateRetryPolicy(
            maxRetryAttempts: 3,
            baseDelay: TimeSpan.FromMilliseconds(5),
            maxDelay: TimeSpan.FromMilliseconds(20));
        var attemptCount = 0;

        await strategy.ExecuteAsync(() =>
        {
            attemptCount++;
            if (attemptCount < 2)
                throw new InvalidOperationException("Transient");
            return Task.CompletedTask;
        }, CancellationToken.None);

        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task ThrowTimeoutRejectedException_GivenExecuteAsyncVoidWhenOperationThrowsTimeoutRejectedException()
    {
        var strategy = PollyResilienceStrategy.CreateRetryPolicy();

        var ex = await Assert.ThrowsAnyAsync<Exception>(() =>
            strategy.ExecuteAsync(() =>
            {
                throw new global::Polly.Timeout.TimeoutRejectedException("timed-out");
            }, CancellationToken.None));

        Assert.Equal("TimeoutRejectedException", ex.GetType().Name);
    }

    [Fact]
    public async Task ThrowOriginalException_GivenExecuteAsyncVoidExhaustsRetries()
    {
        var strategy = PollyResilienceStrategy.CreateRetryPolicy(
            maxRetryAttempts: 1,
            baseDelay: TimeSpan.FromMilliseconds(5),
            maxDelay: TimeSpan.FromMilliseconds(10));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            strategy.ExecuteAsync(() => throw new InvalidOperationException("fail"), CancellationToken.None));
    }

    [Fact]
    public async Task NotRetryVoid_GivenCreateRetryPolicyExecuteAsyncOperationCanceledException()
    {
        var strategy = PollyResilienceStrategy.CreateRetryPolicy(maxRetryAttempts: 5);
        var attemptCount = 0;

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            strategy.ExecuteAsync(() =>
            {
                attemptCount++;
                throw new OperationCanceledException();
            }, CancellationToken.None));

        Assert.Equal(1, attemptCount);
    }

    [Fact]
    public async Task ReturnResult_GivenCreateComprehensivePolicyWithAllParams()
    {
        var logger = new Mock<IWorkflowForgeLogger>();
        var strategy = PollyResilienceStrategy.CreateComprehensivePolicy(
            maxRetryAttempts: 2,
            baseDelay: TimeSpan.FromMilliseconds(5),
            circuitBreakerThreshold: 5,
            circuitBreakerDuration: TimeSpan.FromSeconds(1),
            timeoutDuration: TimeSpan.FromSeconds(30),
            logger: logger.Object);

        var result = await strategy.ExecuteAsync(() => Task.FromResult(99), CancellationToken.None);

        Assert.Equal(99, result);
    }

    [Fact]
    public void ReturnStrategyWithCorrectName_GivenCreateCircuitBreakerPolicyWithLogger()
    {
        var logger = new Mock<IWorkflowForgeLogger>();

        var strategy = PollyResilienceStrategy.CreateCircuitBreakerPolicy(
            failureThreshold: 3,
            durationOfBreak: TimeSpan.FromSeconds(10),
            minimumThroughput: 2,
            logger: logger.Object);

        Assert.Equal("PollyCircuitBreaker(threshold:3)", strategy.Name);
    }

    [Fact]
    public void ReturnStrategyWithCorrectName_GivenCreateTimeoutPolicyWithLogger()
    {
        var logger = new Mock<IWorkflowForgeLogger>();

        var strategy = PollyResilienceStrategy.CreateTimeoutPolicy(
            TimeSpan.FromSeconds(15),
            logger: logger.Object);

        Assert.Equal("PollyTimeout(15s)", strategy.Name);
    }

    [Fact]
    public async Task ReturnFalse_GivenShouldRetryAsyncWithNullException()
    {
        var strategy = PollyResilienceStrategy.CreateRetryPolicy();

        var result = await strategy.ShouldRetryAsync(1, null, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public void ReturnZero_GivenGetRetryDelayWithNullException()
    {
        var strategy = PollyResilienceStrategy.CreateRetryPolicy();

        var delay = strategy.GetRetryDelay(1, null);

        Assert.Equal(TimeSpan.Zero, delay);
    }

    [Fact]
    public async Task LogWarning_GivenCircuitBreakerOpenWithLogger()
    {
        var logger = new Mock<IWorkflowForgeLogger>();
        var strategy = PollyResilienceStrategy.CreateCircuitBreakerPolicy(
            failureThreshold: 5,
            durationOfBreak: TimeSpan.FromSeconds(1),
            minimumThroughput: 5,
            logger: logger.Object);

        for (var i = 0; i < 5; i++)
        {
            try { await strategy.ExecuteAsync(() => Task.FromException(new InvalidOperationException("fail")), CancellationToken.None); }
            catch { }
        }

        await Assert.ThrowsAsync<CircuitBreakerOpenException>(() =>
            strategy.ExecuteAsync(() => Task.FromException(new InvalidOperationException("blocked")), CancellationToken.None));

        logger.Verify(l => l.LogWarning(
            It.IsAny<Exception>(),
            It.Is<string>(s => s.Contains("Circuit breaker")),
            It.IsAny<object[]>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task LogError_GivenTimeoutRejectedWithLogger()
    {
        var logger = new Mock<IWorkflowForgeLogger>();
        var strategy = PollyResilienceStrategy.CreateRetryPolicy(
            maxRetryAttempts: 1,
            baseDelay: TimeSpan.FromMilliseconds(1),
            logger: logger.Object);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            strategy.ExecuteAsync(() =>
            {
                throw new global::Polly.Timeout.TimeoutRejectedException("timed-out");
            }, CancellationToken.None));

        logger.Verify(l => l.LogError(
            It.IsAny<Exception>(),
            It.IsAny<string>(),
            It.IsAny<object[]>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task LogError_GivenGenericExceptionWithLogger()
    {
        var logger = new Mock<IWorkflowForgeLogger>();
        var strategy = PollyResilienceStrategy.CreateRetryPolicy(
            maxRetryAttempts: 1,
            baseDelay: TimeSpan.FromMilliseconds(1),
            logger: logger.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            strategy.ExecuteAsync(() =>
            {
                throw new InvalidOperationException("generic failure");
            }, CancellationToken.None));

        logger.Verify(l => l.LogError(
            It.IsAny<Exception>(),
            It.IsAny<string>(),
            It.IsAny<object[]>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task LogError_GivenGenericExceptionOnGenericExecuteAsyncWithLogger()
    {
        var logger = new Mock<IWorkflowForgeLogger>();
        var strategy = PollyResilienceStrategy.CreateRetryPolicy(
            maxRetryAttempts: 1,
            baseDelay: TimeSpan.FromMilliseconds(1),
            logger: logger.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            strategy.ExecuteAsync<int>(() =>
            {
                throw new InvalidOperationException("generic failure");
            }, CancellationToken.None));

        logger.Verify(l => l.LogError(
            It.IsAny<Exception>(),
            It.IsAny<string>(),
            It.IsAny<object[]>()), Times.AtLeastOnce);
    }
}
