using System;
using WorkflowForge.Testing;

namespace WorkflowForge.Tests.FoundryTests
{
    public class FoundryServicesLeaseShould
    {
        [Fact]
        public void DisposeService_WhenFoundryReset_GivenDisposableInServices()
        {
            var foundry = new FakeWorkflowFoundry();
            var tracker = new DisposeTracker();
            foundry.Services.Set("test", tracker);

            foundry.Reset();

            Assert.True(tracker.Disposed);
        }

        [Fact]
        public void DisposeService_WhenFoundryDisposed_GivenDisposableInServices()
        {
            var foundry = new FakeWorkflowFoundry();
            var tracker = new DisposeTracker();
            foundry.Services.Set("test", tracker);

            foundry.Dispose();

            Assert.True(tracker.Disposed);
        }

        [Fact]
        public void NotReplaceExisting_WhenTryAdd_GivenKeyAlreadyPresent()
        {
            var foundry = new FakeWorkflowFoundry();
            var first = new DisposeTracker();
            var second = new DisposeTracker();

            Assert.True(foundry.Services.TryAdd("slot", first));
            Assert.False(foundry.Services.TryAdd("slot", second));
            Assert.False(second.Disposed);

            foundry.Reset();
            Assert.True(first.Disposed);
        }

        private sealed class DisposeTracker : IDisposable
        {
            public bool Disposed { get; private set; }

            public void Dispose() => Disposed = true;
        }

    }
}
