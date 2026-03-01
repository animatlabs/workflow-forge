using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Resilience;
using WorkflowForge.Extensions.Resilience.Configurations;
using WorkflowForge.Extensions.Resilience.Strategies;
using WF = WorkflowForge;

namespace WorkflowForge.Extensions.Resilience.Tests;

public class RetryWorkflowOperationShould : IDisposable
{
    private readonly IWorkflowFoundry _foundry;

        public RetryWorkflowOperationShould()
    {
        _foundry = WF.WorkflowForge.CreateFoundry("RetryTest");
    }

    public void Dispose()
    {
        (_foundry as IDisposable)?.Dispose();
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullOperation()
    {
        var strategy = new FixedIntervalStrategy(TimeSpan.FromMilliseconds(10), 3);

        Assert.Throws<ArgumentNullException>(() =>
            new RetryWorkflowOperation(null!, strategy));
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullStrategy()
    {
        var operation = new FakeWorkflowOperation();

        Assert.Throws<ArgumentNullException>(() =>
            new RetryWorkflowOperation(operation, null!));
    }

    [Fact]
    public void SetNameFromOperation_GivenValidParameters()
    {
        var operation = new FakeWorkflowOperation { Name = "InnerOp" };
        var strategy = new FixedIntervalStrategy(TimeSpan.FromMilliseconds(10), 3);

        var retryOp = new RetryWorkflowOperation(operation, strategy);

        Assert.Equal("Retry(InnerOp)", retryOp.Name);
        Assert.NotEqual(Guid.Empty, retryOp.Id);
    }

    [Fact]
    public void SetCustomName_GivenCustomName()
    {
        var operation = new FakeWorkflowOperation { Name = "InnerOp" };
        var strategy = new FixedIntervalStrategy(TimeSpan.FromMilliseconds(10), 3);

        var retryOp = new RetryWorkflowOperation(operation, strategy, "CustomRetry");

        Assert.Equal("CustomRetry", retryOp.Name);
    }

    [Fact]
    public async Task ReturnResult_GivenForgeAsyncSuccessfulExecutionOnFirstAttempt()
    {
        const string expectedResult = "success";
        var operation = new FakeWorkflowOperation { Result = expectedResult };
        var strategy = new FixedIntervalStrategy(TimeSpan.FromMilliseconds(10), 3);

        var retryOp = new RetryWorkflowOperation(operation, strategy);

        var result = await retryOp.ForgeAsync(null, _foundry, CancellationToken.None);

        Assert.Equal(expectedResult, result);
        Assert.Equal(1, operation.ForgeAsyncCallCount);
    }

    [Fact]
    public async Task ThrowArgumentNullException_GivenForgeAsyncWithNullFoundry()
    {
        var operation = new FakeWorkflowOperation();
        var strategy = new FixedIntervalStrategy(TimeSpan.FromMilliseconds(10), 3);
        var retryOp = new RetryWorkflowOperation(operation, strategy);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            retryOp.ForgeAsync(null, null!, CancellationToken.None));
    }

    [Fact]
    public async Task SucceedOnSecondAttempt_GivenForgeAsyncRetryBehavior()
    {
        var operation = new FakeWorkflowOperation
        {
            FailCount = 1,
            Result = "success-after-retry"
        };
        var strategy = new FixedIntervalStrategy(TimeSpan.FromMilliseconds(10), 3);

        var retryOp = new RetryWorkflowOperation(operation, strategy);

        var result = await retryOp.ForgeAsync(null, _foundry, CancellationToken.None);

        Assert.Equal("success-after-retry", result);
        Assert.Equal(2, operation.ForgeAsyncCallCount);
    }

    [Fact]
    public async Task ThrowOriginalException_GivenForgeAsyncRetryExhausted()
    {
        var operation = new FakeWorkflowOperation { FailCount = 10 };
        var strategy = new FixedIntervalStrategy(TimeSpan.FromMilliseconds(10), 3);

        var retryOp = new RetryWorkflowOperation(operation, strategy);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            retryOp.ForgeAsync(null, _foundry, CancellationToken.None));

        Assert.Contains("Attempt", ex.Message);
        Assert.Equal(3, operation.ForgeAsyncCallCount);
    }

    [Fact]
    public async Task DelegateToInnerOperation_GivenRestoreAsync()
    {
        var operation = new FakeWorkflowOperation();
        var strategy = new FixedIntervalStrategy(TimeSpan.FromMilliseconds(10), 3);
        var retryOp = new RetryWorkflowOperation(operation, strategy);

        await retryOp.RestoreAsync("output-data", _foundry, CancellationToken.None);

        Assert.True(operation.RestoreAsyncCalled);
        Assert.Equal("output-data", operation.LastRestoreOutput);
    }

    [Fact]
    public void DisposeInnerOperation_GivenDispose()
    {
        var operation = new FakeWorkflowOperation();
        var strategy = new FixedIntervalStrategy(TimeSpan.FromMilliseconds(10), 3);
        var retryOp = new RetryWorkflowOperation(operation, strategy);

        retryOp.Dispose();

        Assert.True(operation.Disposed);
    }

    [Fact]
    public void NotPropagate_GivenDisposeWhenInnerOperationThrows()
    {
        var operation = new FakeWorkflowOperation { ThrowOnDispose = true };
        var strategy = new FixedIntervalStrategy(TimeSpan.FromMilliseconds(10), 3);
        var retryOp = new RetryWorkflowOperation(operation, strategy);

        retryOp.Dispose();
    }

    [Fact]
    public async Task CreateRetryOperation_GivenWithFixedInterval()
    {
        var operation = new FakeWorkflowOperation();
        var interval = TimeSpan.FromSeconds(1);

        var retryOp = RetryWorkflowOperation.WithFixedInterval(operation, interval, maxAttempts: 5);

        Assert.NotNull(retryOp);
        Assert.Equal("Retry(TestOperation)", retryOp.Name);
        var result = await retryOp.ForgeAsync(null, _foundry, CancellationToken.None);
        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task CreateRetryOperation_GivenWithExponentialBackoff()
    {
        var operation = new FakeWorkflowOperation();

        var retryOp = RetryWorkflowOperation.WithExponentialBackoff(operation, maxAttempts: 4);

        Assert.NotNull(retryOp);
        var result = await retryOp.ForgeAsync(null, _foundry, CancellationToken.None);
        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task CreateRetryOperation_GivenWithRandomInterval()
    {
        var operation = new FakeWorkflowOperation();
        var minDelay = TimeSpan.FromMilliseconds(50);
        var maxDelay = TimeSpan.FromMilliseconds(200);

        var retryOp = RetryWorkflowOperation.WithRandomInterval(operation, minDelay, maxDelay, maxAttempts: 3);

        Assert.NotNull(retryOp);
        var result = await retryOp.ForgeAsync(null, _foundry, CancellationToken.None);
        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task CreateRetryOperation_GivenFromSettingsWithFixedIntervalStrategy()
    {
        var operation = new FakeWorkflowOperation();
        var settings = new RetryPolicySettings
        {
            StrategyType = RetryStrategyType.FixedInterval,
            MaxAttempts = 3,
            BaseDelay = TimeSpan.FromMilliseconds(50)
        };

        var retryOp = RetryWorkflowOperation.FromSettings(operation, settings);

        Assert.NotNull(retryOp);
        var result = await retryOp.ForgeAsync(null, _foundry, CancellationToken.None);
        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task CreateRetryOperation_GivenFromSettingsWithExponentialBackoffStrategy()
    {
        var operation = new FakeWorkflowOperation();
        var settings = new RetryPolicySettings
        {
            StrategyType = RetryStrategyType.ExponentialBackoff,
            MaxAttempts = 3,
            BaseDelay = TimeSpan.FromMilliseconds(50),
            MaxDelay = TimeSpan.FromSeconds(5),
            BackoffMultiplier = 2.0
        };

        var retryOp = RetryWorkflowOperation.FromSettings(operation, settings);

        Assert.NotNull(retryOp);
        var result = await retryOp.ForgeAsync(null, _foundry, CancellationToken.None);
        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task CreateRetryOperation_GivenFromSettingsWithRandomIntervalStrategy()
    {
        var operation = new FakeWorkflowOperation();
        var settings = new RetryPolicySettings
        {
            StrategyType = RetryStrategyType.RandomInterval,
            MaxAttempts = 3,
            BaseDelay = TimeSpan.FromMilliseconds(50),
            MaxDelay = TimeSpan.FromMilliseconds(200)
        };

        var retryOp = RetryWorkflowOperation.FromSettings(operation, settings);

        Assert.NotNull(retryOp);
        var result = await retryOp.ForgeAsync(null, _foundry, CancellationToken.None);
        Assert.Equal("ok", result);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenFromSettingsWithNullOperation()
    {
        var settings = new RetryPolicySettings
        {
            StrategyType = RetryStrategyType.FixedInterval,
            MaxAttempts = 3
        };

        Assert.Throws<ArgumentNullException>(() =>
            RetryWorkflowOperation.FromSettings(null!, settings));
    }

    [Fact]
    public void ThrowArgumentNullException_GivenFromSettingsWithNullSettings()
    {
        var operation = new FakeWorkflowOperation();

        Assert.Throws<ArgumentNullException>(() =>
            RetryWorkflowOperation.FromSettings(operation, null!));
    }

    [Fact]
    public void ThrowArgumentException_GivenFromSettingsWithRetryStrategyTypeNone()
    {
        var operation = new FakeWorkflowOperation();
        var settings = new RetryPolicySettings
        {
            StrategyType = RetryStrategyType.None,
            MaxAttempts = 0
        };

        var ex = Assert.Throws<ArgumentException>(() =>
            RetryWorkflowOperation.FromSettings(operation, settings));

        Assert.Contains("RetryStrategyType.None", ex.Message);
    }

    [Fact]
    public async Task CreateRetryOperationWithDefaultExponentialBackoff_GivenCreate()
    {
        var operation = new FakeWorkflowOperation();

        var retryOp = RetryWorkflowOperation.Create(operation, maxAttempts: 5);

        Assert.NotNull(retryOp);
        var result = await retryOp.ForgeAsync(null, _foundry, CancellationToken.None);
        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task CreateRetryOperation_GivenForTransientErrors()
    {
        var operation = new FakeWorkflowOperation();

        var retryOp = RetryWorkflowOperation.ForTransientErrors(operation, maxAttempts: 4);

        Assert.NotNull(retryOp);
        var result = await retryOp.ForgeAsync(null, _foundry, CancellationToken.None);
        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task RetryAccordingToStrategy_GivenForgeAsyncWithCustomStrategy()
    {
        var operation = new FakeWorkflowOperation { FailCount = 2, Result = "final" };
        var strategy = new FixedIntervalStrategy(TimeSpan.FromMilliseconds(5), maxAttempts: 5);

        var retryOp = new RetryWorkflowOperation(operation, strategy);

        var result = await retryOp.ForgeAsync(null, _foundry, CancellationToken.None);

        Assert.Equal("final", result);
        Assert.Equal(3, operation.ForgeAsyncCallCount);
    }

    private sealed class FakeWorkflowOperation : IWorkflowOperation
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; set; } = "TestOperation";
        public object? Result { get; set; } = "ok";
        public int FailCount { get; set; }
        public int ForgeAsyncCallCount { get; private set; }
        public bool RestoreAsyncCalled { get; private set; }
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
}
