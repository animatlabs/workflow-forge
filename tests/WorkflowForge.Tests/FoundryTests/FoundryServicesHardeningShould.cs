using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowForge.Operations;
using WorkflowForge.Services;

namespace WorkflowForge.Tests.FoundryTests;

public class FoundryServicesHardeningShould
{
    [Fact]
    public void DisposeOnlyTheDisplacedValue_WhenSetRacesConcurrently()
    {
        var services = new FoundryServices();
        var candidates = Enumerable.Range(0, 64).Select(_ => new DisposeTracker()).ToArray();

        Parallel.ForEach(candidates, candidate => services.Set("slot", candidate));

        Assert.True(services.TryGet<DisposeTracker>("slot", out var survivor));
        Assert.NotNull(survivor);
        Assert.False(survivor!.Disposed);
        Assert.Equal(candidates.Length - 1, candidates.Count(c => c.Disposed));
        Assert.All(candidates, c => Assert.True(c.DisposeCount <= 1));
    }

    [Fact]
    public void LeaveNothingBehind_WhenDisposeAllRunsWhileValuesAreAdded()
    {
        var services = new FoundryServices();
        var trackers = Enumerable.Range(0, 200).Select(_ => new DisposeTracker()).ToArray();
        for (var i = 0; i < trackers.Length; i++)
        {
            services.Set("key" + i, trackers[i]);
        }

        services.DisposeAll();

        Assert.All(trackers, t => Assert.True(t.Disposed));
        Assert.False(services.ContainsKey("key0"));
    }

    [Fact]
    public void ReportFailure_WhenAServiceThrowsFromDispose()
    {
        var logger = new RecordingLogger();
        var services = new FoundryServices(logger);
        var survivor = new DisposeTracker();
        services.Set("throws", new ThrowingDisposable());
        services.Set("healthy", survivor);

        services.DisposeAll();

        Assert.True(survivor.Disposed);
        var warnings = logger.At(WorkflowForgeLogLevel.Warning).ToList();
        Assert.Single(warnings);
        Assert.IsType<InvalidOperationException>(warnings[0].Exception);
    }

    [Fact]
    public void NotDisposeRejectedValue_WhenTryAddFindsAnExistingKey()
    {
        var services = new FoundryServices();
        var first = new DisposeTracker();
        var second = new DisposeTracker();

        Assert.True(services.TryAdd("slot", first));
        Assert.False(services.TryAdd("slot", second));

        Assert.False(second.Disposed);
        Assert.False(first.Disposed);
    }

    [Fact]
    public void NotDisposeTwice_WhenTheSameInstanceIsSetAgain()
    {
        var services = new FoundryServices();
        var tracker = new DisposeTracker();

        services.Set("slot", tracker);
        services.Set("slot", tracker);

        Assert.False(tracker.Disposed);
        services.DisposeAll();
        Assert.Equal(1, tracker.DisposeCount);
    }

    private sealed class DisposeTracker : IDisposable
    {
        private int _disposeCount;

        public bool Disposed => System.Threading.Volatile.Read(ref _disposeCount) > 0;

        public int DisposeCount => System.Threading.Volatile.Read(ref _disposeCount);

        public void Dispose() => System.Threading.Interlocked.Increment(ref _disposeCount);
    }

    private sealed class ThrowingDisposable : IDisposable
    {
        public void Dispose() => throw new InvalidOperationException("teardown failed");
    }
}
