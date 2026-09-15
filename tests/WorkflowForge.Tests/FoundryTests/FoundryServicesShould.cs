using System;
using WorkflowForge.Services;

namespace WorkflowForge.Tests.FoundryTests;

public class FoundryServicesShould
{
    [Fact]
    public void ThrowArgumentException_GivenSetWithNullKey()
    {
        var services = new FoundryServices();
        Assert.Throws<ArgumentException>(() => services.Set(null!, new object()));
    }

    [Fact]
    public void ThrowArgumentException_GivenSetWithEmptyKey()
    {
        var services = new FoundryServices();
        Assert.Throws<ArgumentException>(() => services.Set(string.Empty, new object()));
    }

    [Fact]
    public void ThrowArgumentNullException_GivenSetWithNullValue()
    {
        var services = new FoundryServices();
        Assert.Throws<ArgumentNullException>(() => services.Set("key", null!));
    }

    [Fact]
    public void DisposePrevious_GivenSetReplacesDisposable()
    {
        var services = new FoundryServices();
        var first = new DisposeTracker();
        var second = new DisposeTracker();
        services.Set("slot", first);
        services.Set("slot", second);

        Assert.True(first.Disposed);
        Assert.False(second.Disposed);
    }

    [Fact]
    public void NotDisposePrevious_GivenSetWithSameInstance()
    {
        var services = new FoundryServices();
        var tracker = new DisposeTracker();
        services.Set("slot", tracker);
        services.Set("slot", tracker);

        Assert.False(tracker.Disposed);
    }

    [Fact]
    public void ReturnTypedValue_GivenTryGetWithMatchingType()
    {
        var services = new FoundryServices();
        var tracker = new DisposeTracker();
        services.Set("slot", tracker);

        Assert.True(services.TryGet<DisposeTracker>("slot", out var value));
        Assert.Same(tracker, value);
    }

    [Fact]
    public void ReturnFalse_GivenTryGetWithWrongType()
    {
        var services = new FoundryServices();
        services.Set("slot", new DisposeTracker());

        Assert.False(services.TryGet<string>("slot", out _));
    }

    [Fact]
    public void DisposeAndRemove_GivenTryRemoveExistingKey()
    {
        var services = new FoundryServices();
        var tracker = new DisposeTracker();
        services.Set("slot", tracker);

        Assert.True(services.TryRemove("slot", out var removed));
        Assert.Same(tracker, removed);
        Assert.True(tracker.Disposed);
        Assert.False(services.ContainsKey("slot"));
    }

    [Fact]
    public void ReturnFalse_GivenTryRemoveMissingKey()
    {
        var services = new FoundryServices();
        Assert.False(services.TryRemove("missing", out var removed));
        Assert.Null(removed);
    }

    [Fact]
    public void ReturnTrue_GivenContainsKeyForRegisteredService()
    {
        var services = new FoundryServices();
        services.Set("slot", new object());
        Assert.True(services.ContainsKey("slot"));
    }

    [Fact]
    public void DisposeAllRegistered_GivenDisposeAll()
    {
        var services = new FoundryServices();
        var first = new DisposeTracker();
        var second = new DisposeTracker();
        services.Set("a", first);
        services.Set("b", second);

        services.DisposeAll();

        Assert.True(first.Disposed);
        Assert.True(second.Disposed);
        Assert.False(services.ContainsKey("a"));
        Assert.False(services.ContainsKey("b"));
    }

    [Fact]
    public void NotThrow_GivenDisposeAllWhenDisposableThrows()
    {
        var services = new FoundryServices();
        services.Set("bad", new ThrowOnDispose());
        services.Set("good", new DisposeTracker());

        services.DisposeAll();

        Assert.False(services.ContainsKey("bad"));
        Assert.False(services.ContainsKey("good"));
    }

    [Fact]
    public void NotThrow_GivenSetReplacesDisposableThatThrowsOnDispose()
    {
        var services = new FoundryServices();
        services.Set("slot", new ThrowOnDispose());
        services.Set("slot", new DisposeTracker());
    }

    private sealed class DisposeTracker : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }

    private sealed class ThrowOnDispose : IDisposable
    {
        public void Dispose() => throw new InvalidOperationException("dispose failed");
    }
}
