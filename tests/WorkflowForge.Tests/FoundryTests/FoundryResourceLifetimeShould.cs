using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Testing;

namespace WorkflowForge.Tests.FoundryTests;

/// <summary>
/// Asserts resource lifetime rather than a Disposed flag: that a service is unreachable after the
/// lease ends, and that a timer-holding service actually stops firing.
/// </summary>
public class FoundryResourceLifetimeShould
{
    [Fact]
    public void ReleaseTheServiceReference_WhenTheLeaseEnds()
    {
        var foundry = new FakeWorkflowFoundry();
        var reference = RegisterService(foundry);

        Assert.True(reference.IsAlive);

        foundry.Reset();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(reference.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference RegisterService(FakeWorkflowFoundry foundry)
    {
        var service = new TickingService();
        foundry.Services.Set("ticker", service);
        return new WeakReference(service);
    }

    [Fact]
    public async Task StopFiringATimer_WhenTheLeaseEnds()
    {
        var foundry = new FakeWorkflowFoundry();
        var service = new TickingService();
        foundry.Services.Set("ticker", service);

        await Task.Delay(120);
        Assert.True(service.Ticks > 0);

        foundry.Reset();
        var ticksAtReset = service.Ticks;

        await Task.Delay(150);

        Assert.Equal(ticksAtReset, service.Ticks);
    }

    [Fact]
    public void KeepTearingDown_WhenAServiceDisposeThrows()
    {
        var foundry = new FakeWorkflowFoundry();
        var healthy = new TickingService();
        foundry.Services.Set("throws", new ThrowingService());
        foundry.Services.Set("healthy", healthy);

        foundry.Reset();

        Assert.True(healthy.Disposed);
    }

    private sealed class TickingService : IDisposable
    {
        private readonly Timer _timer;
        private int _ticks;

        public TickingService()
        {
            _timer = new Timer(_ => Interlocked.Increment(ref _ticks), null, TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(20));
        }

        public int Ticks => Volatile.Read(ref _ticks);

        public bool Disposed { get; private set; }

        public void Dispose()
        {
            Disposed = true;
            _timer.Dispose();
        }
    }

    private sealed class ThrowingService : IDisposable
    {
        public void Dispose() => throw new InvalidOperationException("teardown failed");
    }
}
