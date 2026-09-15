using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Extensions.Observability.HealthChecks.Abstractions;
using WorkflowForge.Testing;
using Xunit;

namespace WorkflowForge.Extensions.Observability.HealthChecks.Tests;

public class HealthCheckServiceLifetimeShould
{
    [Fact]
    public async Task NotStackUpChecks_WhenOneOutlastsTheInterval()
    {
        var slow = new SlowHealthCheck(TimeSpan.FromMilliseconds(400));
        using var service = new HealthCheckService(
            TestNullLogger.Instance,
            timeProvider: null,
            checkInterval: TimeSpan.FromMilliseconds(40),
            registerBuiltInHealthChecks: false);
        service.RegisterHealthCheck(slow);

        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (slow.Started == 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25);
        }

        await Task.Delay(500);

        // Without the re-entrancy guard the 40ms timer would have started roughly a dozen
        // overlapping checks in that window.
        Assert.True(slow.Started >= 1, "the periodic health check never fired");
        Assert.Equal(1, slow.Concurrency.Peak);
    }

    [Fact]
    public async Task StopFiring_WhenTheFoundryLeaseEnds()
    {
        var counting = new CountingHealthCheck();
        var foundry = new FakeWorkflowFoundry();

        var service = foundry.CreateHealthCheckService(TimeSpan.FromMilliseconds(30));
        service.RegisterHealthCheck(counting);

        // Timer scheduling granularity differs by runtime, so wait for the first tick rather than
        // assuming one has happened after a fixed delay.
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (counting.Count == 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25);
        }

        Assert.True(counting.Count > 0, "the periodic health check never fired");

        // The service was registered on foundry.Services, so ending the lease disposes its timer.
        foundry.Reset();

        // A tick already in flight may still land, so settle first, then assert no further ticks.
        await Task.Delay(200);
        var afterSettle = counting.Count;

        await Task.Delay(400);

        Assert.Equal(afterSettle, counting.Count);
    }

    [Fact]
    public void BeIdempotent_WhenDisposedTwice()
    {
        var service = new HealthCheckService(
            TestNullLogger.Instance,
            timeProvider: null,
            checkInterval: TimeSpan.FromMilliseconds(50),
            registerBuiltInHealthChecks: false);

        service.Dispose();
        service.Dispose();

        Assert.Throws<ObjectDisposedException>(() => service.RegisterHealthCheck(new CountingHealthCheck()));
    }

    private sealed class ConcurrencyTracker
    {
        private int _current;
        private int _peak;

        public int Peak => Volatile.Read(ref _peak);

        public void Enter()
        {
            var now = Interlocked.Increment(ref _current);
            int observed;
            do
            {
                observed = Volatile.Read(ref _peak);
                if (now <= observed)
                    return;
            }
            while (Interlocked.CompareExchange(ref _peak, now, observed) != observed);
        }

        public void Exit() => Interlocked.Decrement(ref _current);
    }

    private sealed class SlowHealthCheck : IHealthCheck
    {
        private readonly TimeSpan _duration;
        private int _started;

        public SlowHealthCheck(TimeSpan duration) => _duration = duration;

        public string Name => "Slow";

        public string Description => "Health check that takes longer than the poll interval.";

        public int Started => Volatile.Read(ref _started);

        public ConcurrencyTracker Concurrency { get; } = new();

        public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _started);
            Concurrency.Enter();
            try
            {
                await Task.Delay(_duration, cancellationToken);
                return HealthCheckResult.Healthy("slow but fine");
            }
            finally
            {
                Concurrency.Exit();
            }
        }
    }

    private sealed class CountingHealthCheck : IHealthCheck
    {
        private int _count;

        public string Name => "Counting";

        public string Description => "Counts how many times it has been invoked.";

        public int Count => Volatile.Read(ref _count);

        public Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _count);
            return Task.FromResult(HealthCheckResult.Healthy("ok"));
        }
    }
}
